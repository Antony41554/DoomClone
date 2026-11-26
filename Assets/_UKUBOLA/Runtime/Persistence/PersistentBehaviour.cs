using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Ukubola.World;

namespace Ukubola.Persistence
{
    [Serializable] public sealed class EntitySnapshot { public string id, kind, json; }
    public interface IPersistentEntity
    {
        string PersistentId { get; }
        string StateKind { get; }
        int RestoreOrder { get; }
        EntitySnapshot CaptureEntity();
        void ValidateEntity(EntitySnapshot state);
        void RestoreEntity(EntitySnapshot state);
    }
    public enum WorldSignal { Activated, Collected, Killed, Entered, Arrived }
    public enum WorldVerb { Off, On, Toggle, MoveToStop, Drain, Cool, StartEncounter, AdvanceObjective }
    public interface IWorldActionTarget { bool Accepts(WorldVerb verb); void Execute(WorldVerb verb, float value); }

    public abstract class PersistentBehaviour : MonoBehaviour, IPersistentEntity
    {
        [SerializeField] private string persistentId;
        public string PersistentId => persistentId;
        public abstract string StateKind { get; }
        public virtual int RestoreOrder => 100;
        public MissionContext Context { get; private set; }
        public event Action<WorldSignal> Signalled;
        public void BindContext(MissionContext context) => Context = context;
        public void ConfigureIdentity(string mission, string semanticKey) => persistentId = StableId(mission, semanticKey);
        public static string StableId(string mission, string key)
        {
            if (string.IsNullOrWhiteSpace(mission) || string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Stable identities need mission and semantic key.");
            using (var hash = SHA256.Create()) { var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(mission.Length + ":" + mission + key)); var guid = new byte[16]; Array.Copy(bytes, guid, 16); return new Guid(guid).ToString("N"); }
        }
        protected void Emit(WorldSignal signal) { if (!Context || !Context.Restoring) Signalled?.Invoke(signal); }
        protected EntitySnapshot State(object data) => new EntitySnapshot { id = PersistentId, kind = StateKind, json = JsonUtility.ToJson(data) };
        protected T Read<T>(EntitySnapshot state)
        {
            if (state == null || state.id != PersistentId || state.kind != StateKind || string.IsNullOrEmpty(state.json) || state.json.Length > 65536) throw new InvalidOperationException("Entity state mismatch: " + name);
            return JsonUtility.FromJson<T>(state.json);
        }
        public abstract EntitySnapshot CaptureEntity();
        public abstract void ValidateEntity(EntitySnapshot state);
        public abstract void RestoreEntity(EntitySnapshot state);
    }
}
