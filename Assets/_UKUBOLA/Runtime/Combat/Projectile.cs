using UnityEngine;

namespace Ukubola.Combat
{
    // Reusable point projectile; swept ray prevents passing through thin walls at low frame rates.
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour
    {
        private GameObject owner;
        private Vector3 direction;
        private float remaining, speed;
        private int mask, damage;
        private bool launched;
        private float blastRadius;
        private float nextTrail;
        private Color trailColor=new Color(1f,.48f,.18f);
        public void LaunchAdvanced(GameObject source,Vector3 heading,int collisionMask,float velocity,float range,int amount,float radius)
        {Launch(source,heading,collisionMask,velocity,range,amount);blastRadius=Mathf.Max(0,radius);}
        public void PresentAdvanced(Color tint,float scale){trailColor=tint;var r=GetComponent<Renderer>();if(r){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetColor("_BaseColor",tint);r.SetPropertyBlock(b);}transform.localScale*=scale;}
        public void Launch(GameObject source, Vector3 heading, int collisionMask, float velocity, float range, int amount)
        {
            blastRadius=0; owner = source; direction = heading.normalized; mask = collisionMask; speed = velocity; remaining = range; damage = amount;
            if (direction.sqrMagnitude < .5f || !(speed > 0f) || !(remaining > 0f) || damage <= 0) { Destroy(gameObject); return; }
            launched = true;
        }
        public static float Travel(float speed, float dt, float remaining) => Mathf.Max(0f, Mathf.Min(remaining, speed * dt));
        private void Update()
        {
            if (!launched) return;
            float distance = Travel(speed, Time.deltaTime, remaining);
            if (distance <= 0f) return;
            if (HitscanAttack.FirstHit(transform.position, direction, distance, mask, owner, out var hit))
            {
                launched = false;
                var target = HitscanAttack.Damageable(hit.collider);
                if(blastRadius<=0)Ukubola.Release.ReleasePresentation.Impact(hit.point,hit.normal,HitscanAttack.Live(target));
                if (HitscanAttack.Live(target)) target.TakeDamage(new DamageInfo(damage, owner, DamageType.Projectile, hit.point));
                if(blastRadius>0){Explosion.Apply(hit.point-direction*.03f,blastRadius,damage,mask,owner);transform.position=hit.point-direction*.06f;transform.localScale=Vector3.one*Mathf.Min(blastRadius,2.5f);PresentAdvanced(new Color(1f,.75f,.2f),1f);Destroy(gameObject,.12f);return;}
                Destroy(gameObject); return;
            }
            transform.position += direction * distance; remaining -= distance;
            if(Time.time>=nextTrail){nextTrail=Time.time+.055f;Ukubola.Release.ReleasePresentation.ProjectileTrail(transform.position,trailColor);}
            if (remaining <= .0001f) { launched = false; Destroy(gameObject); }
        }
    }
}
