using UnityEngine;
using Ukubola.Player;

namespace Ukubola.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField] private LevelStats stats;
        [SerializeField] private Renderer indicator;
        public LevelStats Stats => stats;
        public Renderer Indicator => indicator;
        public void Configure(LevelStats value, Renderer visual) { stats = value; indicator = visual; }
        private void Start()
        {
            if (!stats || !indicator || !GetComponent<BoxCollider>().isTrigger || !GetComponent<Rigidbody>().isKinematic)
            { Debug.LogError("[UKUBOLA P03] Invalid exit configuration.", this); enabled = false; }
        }
        private void OnTriggerEnter(Collider other) => CheckPlayer(other);
        private void OnTriggerStay(Collider other) => CheckPlayer(other);
        private void CheckPlayer(Collider other)
        {
            if (!isActiveAndEnabled || !stats || stats.Completed || !other || !other.GetComponentInParent<PlayerMotor>()) return;
            // Optional mission gate; legacy labs without it retain their original behavior.
            var mission = GetComponent<MissionObjective>();
            if (mission && !mission.TryExit(other)) return;
            if (!stats.TryComplete()) return;
            if (indicator)
            {
                var block = new MaterialPropertyBlock();
                indicator.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(1f,.65f,.12f));
                indicator.SetPropertyBlock(block);
            }
            Debug.Log($"[UKUBOLA P03] LEVEL COMPLETED | {stats.ElapsedSeconds:F2} seconds.", this);
        }
    }
}
