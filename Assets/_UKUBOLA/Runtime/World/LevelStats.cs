using System;
using UnityEngine;

namespace Ukubola.World
{
    [DisallowMultipleComponent]
    public sealed class LevelStats : MonoBehaviour
    {
        public float ElapsedSeconds { get; private set; }
        public bool Completed { get; private set; }
        public event Action<LevelStats> OnCompleted;
        private void Update() => Advance(Time.deltaTime);
        public void Advance(float dt)
        {
            if (!Completed && dt > 0f && !float.IsInfinity(dt)) ElapsedSeconds += dt;
        }
        public void RestoreElapsed(float value)
        { if(!Completed && value>=0 && !float.IsInfinity(value))ElapsedSeconds=value; }
        public bool TryComplete()
        {
            if (Completed) return false;
            Completed = true;
            OnCompleted?.Invoke(this);
            return true;
        }
    }
}
