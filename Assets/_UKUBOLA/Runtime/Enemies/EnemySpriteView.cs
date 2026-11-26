using UnityEngine;
using Ukubola.Rendering;
namespace Ukubola.Enemies
{
    public sealed class EnemySpriteView : MonoBehaviour, IEnemyPresentation
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Billboard billboard;
        [SerializeField] private Sprite[] idle, walk;
        [SerializeField] private Sprite attack, pain, dead;
        private Camera viewer;
        private Sprite[] releaseIdle,releaseWalk,releaseAttack;
        private Sprite[] releasePain,releaseDead;
        private bool release;
        private EnemyState previousState;
        private float stateStart;
        public void SetReleaseFrames(Sprite[] a,Sprite[] b,Sprite[] c,Sprite[] hurt,Sprite[] corpse)
        {
            bool Valid(Sprite[] arr,int count){if(arr==null||arr.Length!=count)return false;foreach(var f in arr)if(!f)return false;return true;}
            release=Valid(a,8)&&Valid(b,32)&&Valid(c,32)&&Valid(hurt,16)&&Valid(corpse,32);
            if(!release)return;releaseIdle=a;releaseWalk=b;releaseAttack=c;releasePain=hurt;releaseDead=corpse;
        }
        public bool IsValid => visual && billboard && ValidFrames(idle) && ValidFrames(walk) && attack && pain && dead;
        private static bool ValidFrames(Sprite[] frames) { if(frames==null || frames.Length!=8)return false; foreach(var s in frames) if(!s)return false; return true; }
        public void Configure(SpriteRenderer renderer,Billboard facing,Sprite[] a,Sprite[] b,Sprite strike,Sprite hurt,Sprite corpse)
        { visual=renderer; billboard=facing; idle=a; walk=b; attack=strike; pain=hurt; dead=corpse; visual.sprite=idle[0]; }
        public void Bind(Camera camera) { viewer=camera; billboard.Configure(camera); }
        public void Present(EnemyState state,bool moving,float time)
        {
            if(!IsValid || !viewer) return;
            Vector3 relative=transform.InverseTransformDirection(viewer.transform.position-transform.position);
            int index=DirectionalSprite.IndexFromAngle(Mathf.Atan2(relative.x,relative.z)*Mathf.Rad2Deg);
            Sprite frame=state==EnemyState.Dead?dead:state==EnemyState.Pain?pain:state==EnemyState.Attack?attack:moving && Mathf.FloorToInt(time*5f)%2==1?walk[index]:idle[index];
            if(release)
            {
                if(previousState!=state){previousState=state;stateStart=state==EnemyState.Dead&&Time.timeScale<=0?time-1f:time;}
                int step=Mathf.FloorToInt(Mathf.Max(0,time)*9f)%4;
                int strike=Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0,time-stateStart)*12f),0,3);
                int painFrame=Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0,time-stateStart)*13f),0,1);
                int deathFrame=Time.timeScale<=0?3:Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0,time-stateStart)*10f),0,3);
                frame=state==EnemyState.Dead?releaseDead[index*4+deathFrame]:state==EnemyState.Pain?releasePain[index*2+painFrame]:state==EnemyState.Attack?releaseAttack[index*4+strike]:moving?releaseWalk[index*4+step]:releaseIdle[index];
            }
            if(visual.sprite!=frame) visual.sprite=frame;
        }
    }
}
