using UnityEngine;
using UnityEngine.UI;
namespace Ukubola.Release
{
    [RequireComponent(typeof(RawImage))]public sealed class ReleaseMenuMotion:MonoBehaviour
    {
        private RawImage image;
        private void Awake(){image=GetComponent<RawImage>();}
        private void Update(){float a=.87f+.07f*Mathf.Sin(Time.unscaledTime*1.3f);image.color=new Color(.72f,.86f,.87f,a);}
    }
}
