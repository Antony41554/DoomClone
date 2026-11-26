using System;
using UnityEngine;

namespace Ukubola.Combat
{
    public static class HitscanAttack
    {
        // RaycastAll has unspecified order. Select the closest non-owner solid hit.
        public static bool FirstHit(Vector3 origin, Vector3 direction, float range, int mask, GameObject owner, out RaycastHit closest)
        {
            closest = default;
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, direction, range, mask, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider || (owner && (hit.collider.transform == owner.transform || hit.collider.transform.IsChildOf(owner.transform)))) continue;
                if (hit.distance < nearest) { nearest = hit.distance; closest = hit; }
            }
            return closest.collider != null;
        }
        public static IDamageable Damageable(Collider collider) => collider ? collider.GetComponentInParent<IDamageable>() : null;
        public static bool Live(IDamageable target) => target != null && (!(target is UnityEngine.Object obj) || obj != null) && !target.IsDead;
        public static Vector3 SpreadDirection(Vector3 forward, float degrees) => Quaternion.AngleAxis(degrees, Vector3.up) * forward.normalized;
        public static int RollDamage(System.Random random) => 5 * random.Next(1, 4);
        public static float RollSpread(System.Random random, float maximum) => (float)(random.NextDouble() * 2.0 - 1.0) * maximum;
        public static bool Ready(double now, double next) => now >= next;
        public static double NextShot(double now, float interval) => now + interval; // No catch-up bursts after a slow frame.
    }
}
