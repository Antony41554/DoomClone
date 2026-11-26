using UnityEngine;

namespace Ukubola.World
{
    public sealed class WorldRelayView:MonoBehaviour
    {
        [SerializeField] private WorldRelay relay;
        [SerializeField] private Renderer screen;
        [SerializeField] private Transform lever;
        private MaterialPropertyBlock properties;
        public void Configure(WorldRelay source,Renderer display,Transform handle)
        {relay=source;screen=display;lever=handle;}
        private void OnEnable(){if(relay)relay.Changed+=Refresh;Refresh();}
        private void OnDisable(){if(relay)relay.Changed-=Refresh;}
        private void Refresh()
        {
            if(!relay)return;
            if(lever)lever.localRotation=Quaternion.Euler(relay.Active?-45:20,0,0);
            if(!screen)return;
            if(properties==null)properties=new MaterialPropertyBlock();screen.GetPropertyBlock(properties);
            Color color=!relay.Powered?new Color(.06f,.08f,.08f):relay.Active?new Color(.12f,.8f,.35f):new Color(.05f,.5f,.8f);
            properties.SetColor("_BaseColor",color);properties.SetColor("_EmissionColor",color*(relay.Powered?2:0));screen.SetPropertyBlock(properties);
        }
    }
}
