using UnityEngine;
using Ukubola.Combat;

namespace Ukubola.Rendering
{
    public sealed class ModelWeaponView : MonoBehaviour, IWeaponPresentation, IWeaponRestorePresentation
    {
        [SerializeField] private WeaponView source;
        [SerializeField] private WeaponController controller;
        [SerializeField] private Transform mount;
        [SerializeField] private GameObject[] models;
        [SerializeField] private Light flash;
        [SerializeField] private GameObject muzzle;
        private int shots = -1, selected = -1;
        private float shotAt = -10, switchedAt;
        private int lastFired=-1;
        private Transform[] mechanisms,supportHands;
        private Vector3[] mechanismRest,handRest;
        private void CacheMechanisms()
        {
            if(mechanisms!=null)return;
            mechanisms=new Transform[models.Length];supportHands=new Transform[models.Length];mechanismRest=new Vector3[models.Length];handRest=new Vector3[models.Length];
            for(int i=0;i<models.Length;i++)if(models[i])
            {
                mechanisms[i]=models[i].transform.Find(i==2?"Slide":i==3?"Pump":"No cycling part");supportHands[i]=models[i].transform.Find("Support Hand");
                if(mechanisms[i])mechanismRest[i]=mechanisms[i].localPosition;if(supportHands[i])handRest[i]=supportHands[i].localPosition;
            }
        }
        public void Configure(WeaponView adapter, WeaponController guns, Transform root, GameObject[] values, Light light, GameObject effect)
        { source = adapter; controller = guns; mount = root; models = values; flash = light; muzzle = effect; }
        private void Start() {CacheMechanisms();source.SetPresentation(this);}
        private void OnDestroy() {if(source)source.SetPresentation(null);}
        public void ResetTransient()
        {
            CacheMechanisms();lastFired=-1;shots=controller.Shots;selected=controller.Selected?(int)controller.Selected.Weapon:-1;shotAt=switchedAt=-10;
            mount.localPosition=new Vector3(0,-.2f,.4f);mount.localRotation=Quaternion.identity;
            for(int i=0;i<models.Length;i++)if(models[i])models[i].SetActive(i==selected&&!controller.GetComponent<Health>().IsDead);
            if(flash)flash.intensity=0;if(muzzle)muzzle.SetActive(false);
            for(int i=0;i<mechanisms.Length;i++){if(mechanisms[i])mechanisms[i].localPosition=mechanismRest[i];if(supportHands[i])supportHands[i].localPosition=handRest[i];}
        }
        public void Present(WeaponDefinition definition, bool alive, bool firing, float speed, float elapsed)
        {
            int id = definition ? (int)definition.Weapon : -1;
            if (selected != id) { selected = id; switchedAt = elapsed; for (int i = 0; i < models.Length; i++) if (models[i]) models[i].SetActive(alive && i == id); }
            if (id >= 0 && id < models.Length && models[id]) models[id].SetActive(alive);
            if (controller.Shots != shots) { if (shots >= 0){shotAt = elapsed;lastFired=id;} shots = controller.Shots; }
            float kick = Mathf.Exp(-Mathf.Max(0, elapsed - shotAt) * 22f); float movement = Mathf.Clamp01(speed / 8f);
            float raise = 1 - Mathf.Clamp01((elapsed - switchedAt) / .22f);
            mount.localPosition = new Vector3(Mathf.Sin(elapsed * 9) * .017f * movement, -.2f - Mathf.Abs(Mathf.Cos(elapsed * 9)) * .016f * movement - raise * .7f, .4f - kick * .09f);
            mount.localRotation = Quaternion.Euler(-kick * 6, 0, 0);
            CacheMechanisms();
            for(int i=0;i<mechanisms.Length;i++)if(mechanisms[i])
            {
                float stroke=0;
                if(alive&&id==i&&lastFired==i&&definition)
                {
                    float t=Mathf.Clamp01((elapsed-shotAt)/Mathf.Max(.1f,definition.Interval));
                    stroke=i==2?Mathf.Sin(Mathf.Clamp01(t/.45f)*Mathf.PI)*.045f:i==3?Mathf.Sin(Mathf.Clamp01((t-.18f)/.7f)*Mathf.PI)*.12f:0;
                }
                mechanisms[i].localPosition=mechanismRest[i]-Vector3.forward*stroke;
                if(supportHands[i])supportHands[i].localPosition=handRest[i]-Vector3.forward*stroke;
            }
            bool lit = alive && lastFired==id && elapsed - shotAt < .065f && definition && definition.UsesAmmo;
            if (flash) flash.intensity = lit ? 3.5f : 0;
            if (muzzle) { muzzle.SetActive(lit); muzzle.transform.localRotation = Quaternion.Euler(0, 0, controller.Shots * 47); }
        }
    }
}
