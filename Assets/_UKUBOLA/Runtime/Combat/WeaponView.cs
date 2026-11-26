using UnityEngine;
using UnityEngine.UI;
using Ukubola.Player;

namespace Ukubola.Combat
{
    [DisallowMultipleComponent]
    public sealed class WeaponView : MonoBehaviour, Ukubola.Rendering.IWeaponPresentation
    {
        [SerializeField] private RawImage image;
        public RawImage Image => image;
        private Ukubola.Rendering.IWeaponPresentation alternative;
        public void SetPresentation(Ukubola.Rendering.IWeaponPresentation value) { alternative=value; if(image && value!=null)image.enabled=false; }
        private WeaponDefinition lastWeapon;
        private Ukubola.Release.ReleaseArt lastArt;
        private Texture2D releaseIdle,releaseFire,releaseRecover;
        private float switchAt,fireAt;
        public void Configure(RawImage target) => image = target;
        public void Present(WeaponDefinition weapon, bool alive, bool firing, float speed, float elapsed)
        {
            if(alternative!=null){alternative.Present(weapon,alive,firing,speed,elapsed);return;}
            if (!image) return;
            image.enabled = alive && weapon != null;
            if (!image.enabled) return;
            Texture2D texture = firing ? weapon.Firing : weapon.Idle;
            var release=Ukubola.Release.ReleasePresentation.Active;
            bool modern=release&&release.Ready&&release.Stage>=0;
            if(modern)
            {
                var art=release.Art;
                if(lastWeapon!=weapon||lastArt!=art){lastWeapon=weapon;lastArt=art;string key=weapon.Weapon.ToString();releaseIdle=art.Texture(key+"_Idle");releaseFire=art.Texture(key+"_Fire");releaseRecover=art.Texture(key+"_Recover");switchAt=elapsed;fireAt=-10;}
                if(firing)fireAt=elapsed;
                Texture2D frame=firing?releaseFire:elapsed-fireAt<.105f?releaseRecover:releaseIdle;
                if(frame)texture=frame;
            }
            if(image.texture!=texture)image.texture=texture;
            float bob = Mathf.Clamp01(speed / 8f);
            var parent = (RectTransform)image.rectTransform.parent;
            float unit = parent.rect.height / 200f;
            image.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(elapsed * 9f) * 2f * bob, -Mathf.Abs(Mathf.Cos(elapsed * 9f)) * 2f * bob - (firing ? 3f : 0f)) * unit;
            if(modern){float raise=1f-Mathf.Clamp01((elapsed-switchAt)/.18f);image.rectTransform.anchoredPosition+=Vector2.down*(raise*30f*unit);}
        }
    }
}
