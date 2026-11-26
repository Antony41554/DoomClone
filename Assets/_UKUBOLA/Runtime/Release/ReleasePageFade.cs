using UnityEngine;
namespace Ukubola.Release
{
    [RequireComponent(typeof(CanvasGroup))]public sealed class ReleasePageFade:MonoBehaviour
    {
        private CanvasGroup group;
        private float entered;
        private void Awake(){group=GetComponent<CanvasGroup>();entered=Time.unscaledTime;group.alpha=.4f;}
        private void Update(){group.alpha=Mathf.Lerp(.4f,1f,Mathf.Clamp01((Time.unscaledTime-entered)/.16f));if(group.alpha>=1f)enabled=false;}
    }
}
