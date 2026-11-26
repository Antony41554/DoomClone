using System.Collections.Generic;
using UnityEngine;
using Ukubola.World;

namespace Ukubola.Release
{
    public sealed class ReleaseDecoration
    {
        public struct Fixture { public Vector3 Position; public Color Color; public float Range,Strength,Phase; }
        public readonly List<Fixture> Lights=new List<Fixture>();
        public int Count {get;private set;}
        private readonly Transform root;
        private readonly Mesh cube,quad;
        private readonly Material wall,metal,glow,beam;
        private readonly Color accent;
        public ReleaseDecoration(Transform parent,Mesh box,Mesh plane,Material wall,Material metal,Material glow,Material beam,Color accent)
        {root=parent;cube=box;quad=plane;this.wall=wall;this.metal=metal;this.glow=glow;this.beam=beam;this.accent=accent;}
        public static Mesh BoxMesh()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            void Face(Vector3 n,Vector3 u,Vector3 w){int i=v.Count;Vector3 c=n*.5f;v.Add(c-u*.5f-w*.5f);v.Add(c+u*.5f-w*.5f);v.Add(c+u*.5f+w*.5f);v.Add(c-u*.5f+w*.5f);uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});tris.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
            Face(Vector3.forward,Vector3.right,Vector3.up);Face(Vector3.back,Vector3.left,Vector3.up);Face(Vector3.right,Vector3.back,Vector3.up);Face(Vector3.left,Vector3.forward,Vector3.up);Face(Vector3.up,Vector3.right,Vector3.back);Face(Vector3.down,Vector3.right,Vector3.forward);
            var m=new Mesh{name="P15 decorative box"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tris,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static Mesh QuadMesh(){var m=new Mesh{name="P15 FX quad"};m.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};m.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};m.triangles=new[]{0,1,2,0,2,3};m.RecalculateNormals();m.RecalculateBounds();return m;}
        public static Mesh CylinderMesh()
        {
            const int segments=16;var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int i=0;i<segments;i++){float a=2*Mathf.PI*i/segments,b=2*Mathf.PI*(i+1)/segments;var p=new Vector3(.5f*Mathf.Cos(a),0,.5f*Mathf.Sin(a));var q=new Vector3(.5f*Mathf.Cos(b),0,.5f*Mathf.Sin(b));int j=v.Count;
                v.Add(p-Vector3.up*.5f);v.Add(p+Vector3.up*.5f);v.Add(q+Vector3.up*.5f);v.Add(q-Vector3.up*.5f);uv.AddRange(new[]{new Vector2((float)i/segments,0),new Vector2((float)i/segments,1),new Vector2((float)(i+1)/segments,1),new Vector2((float)(i+1)/segments,0)});tr.AddRange(new[]{j,j+1,j+2,j,j+2,j+3});
                j=v.Count;v.Add(Vector3.up*.5f);v.Add(q+Vector3.up*.5f);v.Add(p+Vector3.up*.5f);uv.AddRange(new[]{new Vector2(.5f,.5f),new Vector2(q.x+.5f,q.z+.5f),new Vector2(p.x+.5f,p.z+.5f)});tr.AddRange(new[]{j,j+1,j+2});
                j=v.Count;v.Add(Vector3.down*.5f);v.Add(p-Vector3.up*.5f);v.Add(q-Vector3.up*.5f);uv.AddRange(new[]{new Vector2(.5f,.5f),new Vector2(p.x+.5f,p.z+.5f),new Vector2(q.x+.5f,q.z+.5f)});tr.AddRange(new[]{j,j+1,j+2});}
            var mesh=new Mesh{name="P15 Barrel"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private void Add(string name,Mesh mesh,Material mat,Vector3 position,Quaternion rotation,Vector3 scale)
        {var g=new GameObject(name);g.layer=root.gameObject.layer;g.transform.SetParent(root,false);g.transform.SetPositionAndRotation(position,rotation);g.transform.localScale=scale;g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;Count++;}
        public void Build(LevelDefinition map,IReadOnlyList<Vector3> avoid,int stage)
        {
            // Lamps and panels stay flush to existing walls and above the walking surface.
            // No props on doorways, pickups, actors, exit, secret or switches.
            for(int z=1;z<map.Depth-1;z++)for(int x=1;x<map.Width-1;x++){
                if(!LevelDefinition.Walkable(map.Tile(x,z))||(x*11+z*7+stage*3)%4!=0||Lights.Count>=48)continue;
                var cell=new Vector2Int(x,z);Vector3 c=map.Center(cell);bool skip=false;foreach(var p in avoid)if((new Vector2(c.x-p.x,c.z-p.z)).sqrMagnitude<2.8f*2.8f){skip=true;break;}if(skip)continue;
                foreach(var d in LevelDefinition.Neighbors){if(LevelDefinition.Walkable(map.Tile(x+d.x,z+d.y)))continue;var outward=new Vector3(d.x,0,d.y);var p=c+outward*.82f;Quaternion q=Quaternion.LookRotation(-outward,Vector3.up);
                    Add("Fixture housing",cube,metal,p+Vector3.up*2.28f,q,new Vector3(.78f,.25f,.12f));
                    Add("Luminous strip",cube,glow,p-outward*.08f+Vector3.up*2.28f,q,new Vector3(.61f,.08f,.035f));
                    Add("Service panel",cube,wall,p+Vector3.up*1.35f,q,new Vector3(.63f,.65f,.08f));
                    if((x+z)%3==0){Add("Conduit A",cube,metal,p+q*new Vector3(-.64f,1.27f,0),q,new Vector3(.075f,2.4f,.075f));Add("Conduit B",cube,metal,p+q*new Vector3(-.48f,1.27f,0),q,new Vector3(.075f,2.4f,.075f));}
                    // Transparent textured light column; not a true volumetric raymarch.
                    if((x+z)%2==0)Add("Haze shaft",quad,beam,p-outward*.16f+Vector3.up*1.8f,q,new Vector3(.78f,1.3f,1));
                    Lights.Add(new Fixture{Position=p-outward*.28f+Vector3.up*2.12f,Color=accent,Range=5.7f,Strength=2.4f,Phase=x*.75f+z*1.13f});break;
                }
            }
        }
    }
}
