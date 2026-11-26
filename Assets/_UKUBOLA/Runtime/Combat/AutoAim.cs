using UnityEngine;

namespace Ukubola.Combat
{
    public static class AutoAim
    {
        // Vertical assistance only: preserve horizontal heading. A solid blocker is never skipped.
        public static Vector3 Resolve(Vector3 origin, Vector3 forward, float range, int hitMask, int targetMask, GameObject owner)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (horizontal.sqrMagnitude < .5f) return forward.normalized;
            if (HitscanAttack.FirstHit(origin, horizontal, range, hitMask, owner, out var direct) && HitscanAttack.Live(HitscanAttack.Damageable(direct.collider))) return horizontal;
            float best = float.PositiveInfinity;
            Vector3 answer = horizontal;
            foreach (var c in Physics.OverlapSphere(origin, range, targetMask, QueryTriggerInteraction.Ignore))
            {
                if (!c || (owner && c.transform.IsChildOf(owner.transform))) continue;
                var target = HitscanAttack.Damageable(c);
                if (!HitscanAttack.Live(target)) continue;
                Vector3 delta = c.bounds.center - origin;
                float depth = Vector3.Dot(delta, horizontal);
                if (depth <= .05f || depth >= best || depth > range) continue;
                Vector3 flatPoint = origin + horizontal * depth;
                var bounds = c.bounds;
                if (flatPoint.x < bounds.min.x || flatPoint.x > bounds.max.x || flatPoint.z < bounds.min.z || flatPoint.z > bounds.max.z) continue;
                if (!TryDirection(horizontal, depth, delta.y, out Vector3 aim)) continue;
                if (!HitscanAttack.FirstHit(origin, aim, range, hitMask, owner, out var hit) || !object.ReferenceEquals(HitscanAttack.Damageable(hit.collider), target)) continue;
                best = depth; answer = aim;
            }
            return answer;
        }
        public static bool TryDirection(Vector3 forward, float depth, float height, out Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            if (depth <= .05f || direction.sqrMagnitude < .5f || Mathf.Abs(height) > depth * Mathf.Tan(30f * Mathf.Deg2Rad) + .00001f) return false;
            direction = (direction * depth + Vector3.up * height).normalized;
            return true;
        }
    }
}
