using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Ukubola.World;

namespace Ukubola.Persistence
{
    [Serializable] public sealed class CampaignProgress
    {
        public int kills, enemies, items, pickups, secrets, totalSecrets, missions;
        public float seconds;
        public bool Valid => kills >= 0 && kills <= enemies && items >= 0 && items <= pickups && secrets >= 0 && secrets <= totalSecrets && missions >= 0 && missions <= 3 && SaveData.Range(seconds, 0, SaveData.MaxSeconds);
    }
    [Serializable] public sealed class MissionSnapshot
    {
        public int version = 2, revision, difficulty = 1;
        public string missionId, utc;
        public Vector3 position;
        public float yaw, elapsed, invulnerability, berserk;
        public SavedGear gear, entry;
        public CampaignProgress prior = new CampaignProgress();
        public EntitySnapshot[] entities = Array.Empty<EntitySnapshot>();
        public string[] flags = Array.Empty<string>(), explored = Array.Empty<string>();
        public void Validate()
        {
            void Need(bool value, string reason) { if (!value) throw new InvalidDataException(reason); }
            Need(version == 2 && revision > 0 && !string.IsNullOrWhiteSpace(missionId) && missionId.Length <= 128 && difficulty >= 0 && difficulty <= 2, "Incompatible snapshot identity/version.");
            Need(gear != null && entry != null && gear.Valid() && entry.Valid() && prior != null && prior.Valid, "Invalid saved loadout/progress.");
            Need(MissionDefinition.Finite(position) && SaveData.Range(yaw, 0, 360) && SaveData.Range(elapsed, 0, SaveData.MaxSeconds) && SaveData.Range(invulnerability, 0, 600) && SaveData.Range(berserk, 0, 600), "Invalid player position/timers.");
            Need(entities != null && entities.Length <= 4096 && entities.All(s => s != null && Guid.TryParseExact(s.id, "N", out _) && !string.IsNullOrEmpty(s.kind) && s.kind.Length <= 64 && !string.IsNullOrEmpty(s.json) && s.json.Length <= 65536) && entities.Select(s => s.id).Distinct(StringComparer.Ordinal).Count() == entities.Length, "Invalid entity records.");
            foreach (var list in new[] { flags, explored }) Need(list != null && list.Length <= 4096 && list.All(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 128) && list.Distinct(StringComparer.Ordinal).Count() == list.Length, "Invalid world flags/exploration.");
        }
        public void ValidateMission(MissionContext context)
        {
            Validate(); var mission = context.Definition;
            if (missionId != mission.missionId || revision != mission.revision || !mission.Bounds.Contains(position) || !mission.sectors.Any(s => s.Contains(position, .3f)) || explored.Any(key => !mission.sectors.Any(s => s.key == key)))
                throw new InvalidDataException("Save belongs to a different mission revision or invalid sector.");
            context.ValidateEntities(entities);
        }
    }

    /// <summary>Version 2 only. File location is injectable; v1 is never opened for writing.</summary>
    public sealed class SaveRepository
    {
        [Serializable] private sealed class Envelope { public string payload, sha256; }
        private const int MaxBytes = 8 * 1024 * 1024;
        public string FilePath { get; }
        public SaveRepository(string directory)
        {
            if (!Path.IsPathRooted(directory)) throw new ArgumentException("Save directory must be absolute.");
            FilePath = Path.Combine(Path.GetFullPath(directory), "campaign-v2.json");
        }
        public static SaveRepository Default => new SaveRepository(QaEnvironment.Active ? QaEnvironment.SaveFolder : Path.Combine(Application.persistentDataPath, "UKUBOLA", "Saves"));
        public static string Encode(MissionSnapshot data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data)); data.Validate();
            string payload = JsonUtility.ToJson(data);
            string json = JsonUtility.ToJson(new Envelope { payload = payload, sha256 = SaveStore.Hash(payload) }, true);
            if (Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("Snapshot exceeds size budget.");
            return json;
        }
        public static MissionSnapshot Decode(string json)
        {
            if (string.IsNullOrEmpty(json) || Encoding.UTF8.GetByteCount(json) > MaxBytes) throw new InvalidDataException("Invalid snapshot size.");
            var envelope = JsonUtility.FromJson<Envelope>(json);
            if (envelope == null || string.IsNullOrEmpty(envelope.payload) || envelope.sha256 != SaveStore.Hash(envelope.payload)) throw new InvalidDataException("Snapshot checksum mismatch.");
            var data = JsonUtility.FromJson<MissionSnapshot>(envelope.payload);
            if (data == null) throw new InvalidDataException("Snapshot data missing."); data.Validate(); return data;
        }
        private static MissionSnapshot Read(string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists || file.Length == 0 || file.Length > MaxBytes) throw new InvalidDataException("Missing or oversized snapshot.");
            return Decode(File.ReadAllText(path, Encoding.UTF8));
        }
        public bool TryRead(out MissionSnapshot snapshot, out bool backup, out string reason)
        {
            snapshot = null; backup = false; reason = "NO SAVED GAME";
            foreach (var path in new[] { FilePath, FilePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try { snapshot = Read(path); backup = path != FilePath; reason = null; return true; }
                catch (Exception e) { reason = e.Message; }
            }
            return false;
        }
        public void Write(MissionSnapshot snapshot)
        {
            string json = Encode(snapshot), directory = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(directory);
            string temporary = FilePath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                Read(temporary);
                if (!File.Exists(FilePath)) File.Move(temporary, FilePath);
                else
                {
                    bool valid; try { Read(FilePath); valid = true; } catch { valid = false; }
                    File.Replace(temporary, FilePath, valid ? FilePath + ".bak" : null);
                }
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        public static MissionSnapshot ImportLegacy(SaveData legacy, MissionSnapshot fresh, MissionDefinition mission)
        {
            if (legacy == null || !legacy.Validate(out var reason)) throw new InvalidDataException("Invalid legacy save.");
            if (legacy.stage != mission.sequence || fresh.missionId != mission.missionId || fresh.revision != mission.revision) throw new InvalidDataException("Legacy mission does not match import target.");
            var imported = JsonUtility.FromJson<MissionSnapshot>(JsonUtility.ToJson(fresh));
            imported.gear = JsonUtility.FromJson<SavedGear>(JsonUtility.ToJson(legacy.entry));
            imported.entry = JsonUtility.FromJson<SavedGear>(JsonUtility.ToJson(legacy.entry));
            imported.position = mission.spawn; imported.yaw = Mathf.Repeat(mission.spawnYaw, 360); imported.elapsed = 0;
            imported.invulnerability = imported.berserk = 0;
            imported.prior = new CampaignProgress { kills = legacy.prior.kills, enemies = legacy.prior.enemies, items = legacy.prior.items, pickups = legacy.prior.pickups,
                secrets = legacy.prior.secrets, totalSecrets = legacy.prior.rooms, missions = legacy.prior.rooms, seconds = legacy.prior.seconds };
            imported.utc = DateTime.UtcNow.ToString("O"); imported.Validate(); return imported;
        }
    }
}
