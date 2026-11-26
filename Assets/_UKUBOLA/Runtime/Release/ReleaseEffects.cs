using System;
using UnityEngine;

namespace Ukubola.Release
{
    // Fixed-size pool, no colliders, Rigidbody, agents, entity IDs or gameplay callbacks.
    public sealed class ReleaseEffects : IDisposable
    {
        public const int Capacity=64;
        private sealed class Particle { public Transform Transform; public MeshRenderer Renderer; public Vector3 Velocity; public Color Color; public float Life,Duration,Size,Growth,Gravity; }
        private readonly Particle[] particles=new Particle[Capacity];
        private readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        private int cursor;
        private readonly System.Random random=new System.Random(15015);
        private float Value()=>(float)random.NextDouble();
        private Vector3 RandomVector(){var p=new Vector3(Value()*2-1,Value()*2-1,Value()*2-1);return p.sqrMagnitude>1?p.normalized:p;}
        private int limit=48;
        public int ActiveCount { get { int n=0;foreach(var p in particles)if(p.Life>0)n++;return n;} }
        public ReleaseEffects(Transform root,Mesh mesh,Material material,Material smoke)
        {
            for(int i=0;i<Capacity;i++){var g=new GameObject("FX "+i);g.layer=root.gameObject.layer;g.transform.SetParent(root,false);var mf=g.AddComponent<MeshFilter>();mf.sharedMesh=mesh;var mr=g.AddComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;mr.enabled=false;particles[i]=new Particle{Transform=g.transform,Renderer=mr};}
            this.spark=material;this.smoke=smoke;
        }
        private readonly Material spark,smoke;
        public void SetLimit(int n){limit=Mathf.Clamp(n,1,Capacity);for(int i=limit;i<Capacity;i++){particles[i].Life=0;particles[i].Renderer.enabled=false;}cursor%=limit;}
        private void Spawn(Vector3 position,Vector3 velocity,Color tint,float life,float size,float growth,float gravity,bool cloudy)
        {
            var p=particles[cursor];cursor=(cursor+1)%limit;p.Transform.position=position;p.Velocity=velocity;p.Color=tint;p.Life=p.Duration=life;p.Size=size;p.Growth=growth;p.Gravity=gravity;p.Renderer.sharedMaterial=cloudy?smoke:spark;p.Renderer.enabled=true;block.Clear();block.SetColor("_BaseColor",tint);p.Renderer.SetPropertyBlock(block);p.Transform.localScale=Vector3.one*size;
        }
        public void Impact(Vector3 point,Vector3 normal,Color tint,bool flesh)
        {
            for(int i=0;i<4;i++)Spawn(point+normal*.04f,(normal+RandomVector()*.9f)*1.7f,tint,.22f+Value()*.13f,.10f,.15f,2.4f,false);
            Spawn(point+normal*.06f,Vector3.up*.24f,new Color(.45f,.48f,.49f,.40f),.42f,.16f,.85f,0,true);
        }
        public void Burst(Vector3 point,Color tint,float size)
        {
            Spawn(point,Vector3.zero,new Color(tint.r*2.7f,tint.g*2.7f,tint.b*2.7f,.92f),.25f,size*.6f,size*3f,0,false);
            for(int i=0;i<12;i++)Spawn(point,RandomVector()*3f+Vector3.up*.8f,tint,.40f+Value()*.35f,.16f,.24f,2.8f,false);
            for(int i=0;i<5;i++)Spawn(point+RandomVector()*.3f,Vector3.up*.6f+RandomVector()*.6f,new Color(.24f,.29f,.30f,.45f),1.1f,size*.35f,size*.8f,0,true);
        }
        public void Trail(Vector3 p,Color tint){Spawn(p,Vector3.zero,new Color(tint.r*1.6f,tint.g*1.6f,tint.b*1.6f,.7f),.18f,.12f,.35f,0,false);}
        public void Update(Camera camera,float dt)
        {
            for(int i=0;i<limit;i++){var p=particles[i];if(p.Life<=0)continue;p.Life-=dt;if(p.Life<=0){p.Renderer.enabled=false;continue;}p.Velocity+=Vector3.down*p.Gravity*dt;p.Transform.position+=p.Velocity*dt;p.Transform.rotation=camera.transform.rotation;p.Size+=p.Growth*dt;p.Transform.localScale=Vector3.one*p.Size;var c=p.Color;c.a*=Mathf.Clamp01(p.Life/p.Duration);block.Clear();block.SetColor("_BaseColor",c);p.Renderer.SetPropertyBlock(block);}
        }
        public void Clear(){foreach(var p in particles){p.Life=0;if(p.Renderer)p.Renderer.enabled=false;}cursor=0;}
        public void Dispose(){Clear();}
    }
}
