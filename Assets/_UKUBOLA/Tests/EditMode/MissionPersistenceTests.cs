using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Campaign;
using Ukubola.Persistence;
using Ukubola.World;

namespace Ukubola.Tests
{
    public sealed class MissionPersistenceTests
    {
        private Scene scene, original;
        private MissionDefinition mission;
        private MissionContext context;
        private string directory;
        [SetUp] public void SetUp()
        {
            original = SceneManager.GetActiveScene(); scene = EditorSceneManager.NewPreviewScene();
            mission = ScriptableObject.CreateInstance<MissionDefinition>(); mission.missionId = "TEST_OUTPOST"; mission.sequence = 0; mission.revision = 1; mission.spawn = Vector3.zero;
            mission.sectors = new[] { new MissionSector { key = "room", label = "Test", center = Vector2.zero, size = new Vector2(12, 12), floor = 0, ceiling = 5 } };
            context = NewObject("Services").AddComponent<MissionContext>(); context.Configure(mission);
            directory = Path.Combine(Path.GetTempPath(), "UkubolaV2Tests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        }
        [TearDown] public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene); if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            UnityEngine.Object.DestroyImmediate(mission);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        private GameObject NewObject(string label) { var go = new GameObject(label); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        private WorldRelay Relay(string key)
        {
            var relay = NewObject(key).AddComponent<WorldRelay>(); relay.ConfigureIdentity(mission.missionId, key); return relay;
        }
        private ActionBinding Connect(string key, WorldRelay source, WorldRelay target)
        {
            var binding = NewObject(key).AddComponent<ActionBinding>(); binding.ConfigureIdentity(mission.missionId, key);
            binding.Configure(source, WorldSignal.Activated, true, new[] { new WorldAction { target = target, verb = WorldVerb.On } }); return binding;
        }
        private MissionSnapshot Snapshot() => new MissionSnapshot { missionId = mission.missionId, revision = mission.revision, position = mission.spawn,
            gear = SavedGear.From(Loadout.Fresh()), entry = SavedGear.From(Loadout.Fresh()), entities = context.CaptureEntities(), utc = DateTime.UtcNow.ToString("O") };
        [Test] public void RenameAndReorderDoNotChangePersistenceIdentity()
        {
            var a = Relay("a"); var b = Relay("b"); context.RebuildRegistry(); a.Execute(WorldVerb.On, 0);
            var snapshot = Snapshot(); a.name = "Renamed by designer"; a.transform.SetAsLastSibling(); a.Execute(WorldVerb.Off, 0); b.Execute(WorldVerb.On, 0);
            context.RebuildRegistry(); context.RestoreEntities(snapshot.entities.Reverse().ToArray());
            Assert.That(a.Active, Is.True); Assert.That(b.Active, Is.False);
        }
        [Test] public void RestoringMechanismsDoesNotReplayActions()
        {
            var a = Relay("a"); var b = Relay("b"); var binding = Connect("binding", a, b); context.RebuildRegistry();
            a.Execute(WorldVerb.On, 0); Assert.That(b.Active && binding.Fired, Is.True); var snapshot = Snapshot();
            b.Execute(WorldVerb.Off, 0); int changes = 0; b.Changed += () => changes++;
            context.RestoreEntities(snapshot.entities); Assert.That(b.Active, Is.True); Assert.That(changes, Is.EqualTo(1));
            b.Execute(WorldVerb.Off, 0); a.Execute(WorldVerb.Off, 0); a.Execute(WorldVerb.On, 0);
            Assert.That(b.Active, Is.False, "A restored one-shot binding must not award/activate again.");
        }
        [Test] public void InvalidEntitySetIsRejectedBeforeAnyMutation()
        {
            var a = Relay("a"); Relay("b"); context.RebuildRegistry(); var states = context.CaptureEntities();
            states[1].kind = "unknown"; Assert.Throws<InvalidOperationException>(() => context.RestoreEntities(states)); Assert.That(a.Active, Is.False);
        }
        [Test] public void DuplicateIdentitiesAreRejected()
        {
            Relay("same"); Relay("same"); Assert.Throws<InvalidOperationException>(() => context.RebuildRegistry());
        }
        [Test] public void CyclicMechanismsAreRejected()
        {
            var a = Relay("a"); var b = Relay("b"); Connect("ab", a, b); Connect("ba", b, a);
            Assert.Throws<InvalidOperationException>(() => context.RebuildRegistry());
        }
        [Test] public void SectorGraphMustBeConnected()
        {
            mission.sectors = mission.sectors.Concat(new[] { new MissionSector { key = "isolated", center = new Vector2(20, 0) } }).ToArray();
            Assert.Throws<InvalidOperationException>(() => mission.Validate());
        }
        [Test] public void CorruptPrimaryUsesBackupAndNeverOverwritesItWithCorruption()
        {
            Relay("a"); context.RebuildRegistry(); var repo = new SaveRepository(directory); var snapshot = Snapshot();
            File.WriteAllText(Path.Combine(directory, "campaign-v1.json"), "legacy sentinel"); repo.Write(snapshot); snapshot.elapsed = 10; repo.Write(snapshot);
            File.WriteAllText(repo.FilePath, "corrupt"); Assert.That(repo.TryRead(out var recovered, out var backup, out _), Is.True);
            Assert.That(backup && recovered.elapsed == 0, Is.True); repo.Write(snapshot);
            Assert.That(SaveRepository.Decode(File.ReadAllText(repo.FilePath + ".bak")).elapsed, Is.Zero);
            Assert.That(File.ReadAllText(Path.Combine(directory, "campaign-v1.json")), Is.EqualTo("legacy sentinel"));
            Assert.That(Directory.GetFiles(directory, "*.tmp-*").Length, Is.Zero);
        }
        [Test] public void UnsupportedVersionAndInvalidNumbersAreRejectedWithoutReplacingSave()
        {
            context.RebuildRegistry(); var repo = new SaveRepository(directory); var snapshot = Snapshot(); repo.Write(snapshot); string originalBytes = File.ReadAllText(repo.FilePath);
            snapshot.version = 1; Assert.Throws<InvalidDataException>(() => repo.Write(snapshot)); snapshot.version = 2; snapshot.position.x = float.NaN;
            Assert.Throws<InvalidDataException>(() => repo.Write(snapshot)); Assert.That(File.ReadAllText(repo.FilePath), Is.EqualTo(originalBytes));
        }
        [Test] public void RevisionMismatchDoesNotAlterWorld()
        {
            var a = Relay("a"); context.RebuildRegistry(); var snapshot = Snapshot(); snapshot.revision++;
            Assert.Throws<InvalidDataException>(() => snapshot.ValidateMission(context)); Assert.That(a.Active, Is.False);
        }
        [Test] public void LegacyImportRestartsAtEntryInsteadOfTransplantingOldPosition()
        {
            context.RebuildRegistry(); var legacy = LegacyFixture(); Assert.That(legacy.Validate(out _), Is.True);
            string before = SaveStore.Encode(legacy); var imported = SaveRepository.ImportLegacy(legacy, Snapshot(), mission);
            Assert.That(imported.gear.hp, Is.EqualTo(legacy.entry.hp)); Assert.That(imported.position, Is.EqualTo(mission.spawn)); Assert.That(imported.elapsed, Is.Zero);
            Assert.That(SaveStore.Encode(legacy), Is.EqualTo(before)); Assert.That(imported.gear.hp, Is.Not.EqualTo(legacy.current.hp));
        }
        private static SaveData LegacyFixture()
        {
            var entry = SavedGear.From(Loadout.Fresh()); var current = SavedGear.From(Loadout.Fresh()); current.hp = 73;
            return new SaveData { version = 1, stage = 0, entry = entry, current = current, prior = SavedTotals.From(default), width = 3, depth = 3,
                explored = new bool[9], position = Vector3.one, layout = SaveStore.Hash("legacy test"), utc = DateTime.UtcNow.ToString("O"),
                enemies = Enumerable.Range(0, 6).Select(i => new SavedEnemy { name = "enemy" + i, hp = 20, max = 20, position = Vector3.one, rival = -1 }).ToArray(),
                pickups = Enumerable.Range(0, 25).Select(i => new SavedItem { name = "item" + i }).ToArray(),
                doors = Enumerable.Range(0, 4).Select(i => new SavedDoor { name = "door" + i }).ToArray(),
                switches = Enumerable.Range(0, 2).Select(i => new SavedSwitch { name = "switch" + i }).ToArray(),
                barrels = Enumerable.Range(0, 2).Select(i => new SavedBarrel { name = "barrel" + i, hp = 20, max = 20 }).ToArray() };
        }
    }
}
