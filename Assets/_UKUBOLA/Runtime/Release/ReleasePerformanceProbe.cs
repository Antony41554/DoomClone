#if UNITY_EDITOR
using System;
using UnityEngine;
using Ukubola.UI;
namespace Ukubola.Release
{
    // Editor-only sample: excludes menus and paused frames. This is not a standalone benchmark.
    public sealed class ReleasePerformanceProbe:MonoBehaviour
    {
        private readonly float[] milliseconds=new float[300];
        private int count;
        private MenuFlow flow;
        private ReleasePresentation release;
        private void Awake(){flow=GetComponent<MenuFlow>();release=GetComponent<ReleasePresentation>();}
        private void Update(){if(!flow||!release||!release.Ready||flow.Page!=MenuPage.Playing||Time.timeScale<=0||!Application.isFocused)return;float ms=Time.unscaledDeltaTime*1000f;if(ms<=0||float.IsNaN(ms)||float.IsInfinity(ms))return;milliseconds[count++]=ms;if(count<milliseconds.Length)return;
            float sum=0;foreach(float n in milliseconds)sum+=n;Array.Sort(milliseconds);float mean=sum/milliseconds.Length;Debug.Log($"[UKUBOLA P15] EDITOR PERFORMANCE | {ReleaseQuality.Label(release.Quality)} {release.ActualResolution} | 300 frames | average {mean:F2} ms ({1000f/mean:F1} FPS) | p95 {milliseconds[284]:F2} ms | editor overhead included.");Destroy(this);
        }
    }
}
#endif
