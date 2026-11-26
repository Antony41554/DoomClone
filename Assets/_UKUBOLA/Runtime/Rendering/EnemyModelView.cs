using UnityEngine;
using Ukubola.Enemies;

namespace Ukubola.Rendering
{
    public sealed class EnemyModelView : MonoBehaviour, IEnemyPresentation
    {
        [SerializeField] private Animation animationView;
        private EnemyState last = (EnemyState)(-1);
        private string clip;
        private float started;
        public bool IsValid => animationView && animationView.GetClip("Idle") && animationView.GetClip("Walk") && animationView.GetClip("Attack") && animationView.GetClip("Pain") && animationView.GetClip("Death");
        public void Configure(Animation value) => animationView = value;
        public void Bind(Camera viewer) { }
        public void Present(EnemyState state, bool moving, float time)
        {
            if (!IsValid) return;
            string next = state == EnemyState.Dead ? "Death" : state == EnemyState.Pain ? "Pain" : state == EnemyState.Attack ? "Attack" : moving ? "Walk" : "Idle";
            if (next != clip || state != last) { animationView.Stop(); clip = next; last = state; started = time; animationView.Play(clip); animationView.enabled = false; }
            var track = animationView[clip]; float elapsed = Mathf.Floor(Mathf.Max(0, time - started) * 12f) / 12f;
            track.time = clip == "Walk" || clip == "Idle" ? Mathf.Repeat(elapsed, track.length) : Mathf.Min(elapsed, track.length);
            track.enabled = true; track.weight = 1; animationView.Sample();
        }
    }
}
