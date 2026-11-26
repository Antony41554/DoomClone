using System;
using UnityEditor;
using UnityEngine;
using Ukubola.Player;

namespace Ukubola.EditorTools
{
    public static class UkubolaMovementTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 01/Run Movement Self-Tests", false, 23)]
        public static void Run()
        {
            if (!UkubolaPlayerSetup.CanRun()) return;
            try
            {
                foreach (int fps in new[] { 30, 60, 120 })
                {
                    float dt = 1f / fps;
                    Vector3 velocity = Vector3.zero;
                    Vector3 walk = Simulate(ref velocity, Vector3.forward * 5f, 28f, fps, dt);
                    Near(walk.z, 5f - 25f / 56f, "walk displacement at " + fps);
                    Near(velocity.z, 5f, "walk speed at " + fps);
                    Vector3 stop = Simulate(ref velocity, Vector3.zero, 24f, fps, dt);
                    Near(stop.z, 25f / 48f, "braking distance at " + fps);
                    Near(velocity.magnitude, 0f, "stopped speed at " + fps);
                    Vector3 run = Simulate(ref velocity, Vector3.forward * 8f, 28f, fps, dt);
                    Near(run.z, 8f - 64f / 56f, "run displacement at " + fps);
                    Near(velocity.magnitude, 8f, "run speed at " + fps);
                    velocity = Vector3.zero;
                    Vector2 diagonal = Vector2.ClampMagnitude(Vector2.one, 1f);
                    Vector3 desired = new Vector3(diagonal.x, 0f, diagonal.y) * 5f;
                    Simulate(ref velocity, desired, 28f, fps, dt);
                    Near(velocity.magnitude, 5f, "diagonal speed at " + fps);
                    Near(velocity.x, velocity.z, "diagonal symmetry at " + fps);
                    Vector3 reverse = Simulate(ref velocity, -desired, 28f, fps, dt);
                    Near(Vector3.Dot(reverse, desired.normalized), -5f + 100f / 56f, "reversal displacement at " + fps);
                    Near((velocity + desired).magnitude, 0f, "reversal final velocity at " + fps);
                }
                Vector3 v = Vector3.right;
                Near(PlayerMotor.IntegrateHorizontal(ref v, Vector3.zero, 24f, 0f).magnitude, 0f, "zero delta time");
                Near(v.x, 1f, "zero delta time preserves velocity");
                Debug.Log("[UKUBOLA P01] MOVEMENT SELF-TESTS PASSED | 32 checks (30/60/120 FPS + zero dt). Numerical tests only; Play tests still required.");
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P01] MOVEMENT SELF-TESTS FAILED: " + ex.Message); }
        }

        private static Vector3 Simulate(ref Vector3 velocity, Vector3 target, float rate, int count, float dt)
        {
            Vector3 position = Vector3.zero;
            for (int i = 0; i < count; i++) position += PlayerMotor.IntegrateHorizontal(ref velocity, target, rate, dt);
            return position;
        }
        private static void Near(float actual, float expected, string label)
        {
            if (float.IsNaN(actual) || Mathf.Abs(actual - expected) > 0.002f)
                throw new InvalidOperationException(label + ": expected " + expected + ", got " + actual);
        }
    }
}
