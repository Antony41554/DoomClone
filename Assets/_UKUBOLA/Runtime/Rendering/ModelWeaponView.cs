using UnityEngine;
using Ukubola.Combat;

namespace Ukubola.Rendering
{
    public sealed class ModelWeaponView : MonoBehaviour, IWeaponPresentation
    {
        [SerializeField] private WeaponView source;
        [SerializeField] private WeaponController controller;
        [SerializeField] private Transform mount;
        [SerializeField] private GameObject[] models;
        [SerializeField] private Light flash;
        [SerializeField] private GameObject muzzle;
        private int shots = -1, selected = -1;
        private float shotAt = -10, switchedAt;
        public void Configure(WeaponView adapter, WeaponController guns, Transform root, GameObject[] values, Light light, GameObject effect)
        { source = adapter; controller = guns; mount = root; models = values; flash = light; muzzle = effect; }
        private void Start() { source.SetPresentation(this); }
        private void OnDestroy() {if(source)source.SetPresentation(null);}
        public void Present(WeaponDefinition definition, bool alive, bool firing, float speed, float elapsed)
        {
            int id = definition ? (int)definition.Weapon : -1;
            if (selected != id) { selected = id; switchedAt = elapsed; for (int i = 0; i < models.Length; i++) if (models[i]) models[i].SetActive(alive && i == id); }
            if (id >= 0 && id < models.Length && models[id]) models[id].SetActive(alive);
            if (controller.Shots != shots) { if (shots >= 0) shotAt = elapsed; shots = controller.Shots; }
            float kick = Mathf.Exp(-Mathf.Max(0, elapsed - shotAt) * 22f); float movement = Mathf.Clamp01(speed / 8f);
            float raise = 1 - Mathf.Clamp01((elapsed - switchedAt) / .22f);
            mount.localPosition = new Vector3(Mathf.Sin(elapsed * 9) * .017f * movement, -.2f - Mathf.Abs(Mathf.Cos(elapsed * 9)) * .016f * movement - raise * .7f, .4f - kick * .09f);
            mount.localRotation = Quaternion.Euler(-kick * 6, 0, 0);
            bool lit = alive && elapsed - shotAt < .065f && definition && definition.UsesAmmo;
            if (flash) flash.intensity = lit ? 3.5f : 0;
            if (muzzle) { muzzle.SetActive(lit); muzzle.transform.localRotation = Quaternion.Euler(0, 0, controller.Shots * 47); }
        }
    }
}
