using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ukubola.World;

namespace Ukubola.EditorTools
{
    public static class UkubolaLevelTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 03/Run Level Self-Tests",false,43)]
        public static void Run()
        {
            if(!UkubolaLevelSetup.CanRun())return;
            var d=ScriptableObject.CreateInstance<LevelDefinition>();
            GameObject temporary=null;int count=0;
            void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);count++;}
            try
            {
                Check(d.TryValidate(out _,out var spawn,out var exit),"Default map connectivity");
                Check(spawn==new Vector2Int(2,2)&&exit==new Vector2Int(16,12),"Default marker coordinates");
                Check(d.Center(spawn)==new Vector3(5,0,5)&&d.Center(exit)==new Vector3(33,.5f,25),"World coordinates");
                var plan=LevelBuilder.Plan(d);
                Check(plan.All(g=>g.Count>0&&g.All(s=>s.Size.x>0&&s.Size.y>0&&s.Size.z>0)),"Positive geometry");
                int cells=0,boundaries=0;
                for(int z=0;z<d.Depth;z++)for(int x=0;x<d.Width;x++)
                {
                    char tile=d.Tile(x,z);if(!LevelDefinition.Walkable(tile))continue;
                    cells++;var center=d.Center(new Vector2Int(x,z));
                    bool Covers(LevelBuilder.Solid s)=>Mathf.Abs(center.x-s.Center.x)<s.Size.x*.5f&&Mathf.Abs(center.z-s.Center.z)<s.Size.z*.5f;
                    Check(plan[0].Count(s=>Covers(s)&&Mathf.Abs(s.Center.y+s.Size.y*.5f-center.y)<.0001f)==1,"Floor coverage "+x+","+z);
                    Check(plan[1].Count(s=>Covers(s)&&Mathf.Abs(s.Center.y-s.Size.y*.5f-LevelDefinition.Ceiling)<.0001f)==1,"Ceiling coverage "+x+","+z);
                    foreach(var n in LevelDefinition.Neighbors)if(!LevelDefinition.Walkable(d.Tile(x+n.x,z+n.y)))
                    {
                        boundaries++;var target=center+new Vector3(n.x,0,n.y)*LevelDefinition.CellSize*.5f;
                        Check(plan[2].Count(s=>Mathf.Abs(s.Center.x-target.x)<.0001f&&Mathf.Abs(s.Center.z-target.z)<.0001f)==1,"Boundary wall "+x+","+z);
                    }
                }
                Check(plan[2].Count==boundaries,"No extra walls");
                Check(Mathf.Abs(plan[0].Sum(s=>s.Size.x*s.Size.z)-cells*4f)<.001f,"Floor total area");
                for(int g=0;g<plan.Length;g++)
                {
                    var mesh=LevelBuilder.CreateMesh("Test",plan[g]);
                    try
                    {
                        Check(mesh.vertexCount==plan[g].Count*24&&mesh.triangles.Length==plan[g].Count*36,"Mesh topology");
                        var v=mesh.vertices;var n=mesh.normals;var t=mesh.triangles;var uv=mesh.uv;
                        for(int k=0;k<t.Length;k+=3)
                        {
                            var a=t[k];var b=t[k+1];var c=t[k+2];
                            var normal=Vector3.Cross(v[b]-v[a],v[c]-v[a]).normalized;
                            Check(Vector3.Dot(normal,n[a])>.999f&&Vector3.Dot(normal,n[b])>.999f&&Vector3.Dot(normal,n[c])>.999f,"Triangle winding/normal");
                            float worldArea=Vector3.Cross(v[b]-v[a],v[c]-v[a]).magnitude*.5f;
                            var u=uv[b]-uv[a];var w=uv[c]-uv[a];float uvArea=Mathf.Abs(u.x*w.y-u.y*w.x)*.5f;
                            Check(Mathf.Abs(uvArea-worldArea/4f)<.001f,"UV scale");
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(mesh);}
                }
                void Invalid(string[] rows,string label){d.SetLayout("TEST",rows);Check(!d.TryValidate(out _,out _,out _),label);}
                Invalid(new[]{"#####","#S.E#","#####"},"Reject 50cm step");
                Invalid(new[]{"#####","#S1E#","##.##","#.#.#","#####"},"Reject disconnected cells");
                Invalid(new[]{"#####","#S1E#","##"},"Reject ragged rows");
                Invalid(new[]{"##.##","#S1E#","#####"},"Reject open perimeter");
                Invalid(new[]{"######","#SS1E#","######"},"Reject duplicate spawn");
                Invalid(new[]{"#####","#S1?#","#####"},"Reject unknown symbol");
                d.SetLayout("TEST",new[]{"#####","#S1E#","#####"});Check(d.TryValidate(out _,out _,out _),"Accept minimum staircase route");
                temporary=new GameObject("UKUBOLA P03 temporary test"){hideFlags=HideFlags.HideAndDontSave};
                var stats=temporary.AddComponent<LevelStats>();int events=0;stats.OnCompleted+=_=>events++;
                stats.Advance(.5f);stats.Advance(1.25f);stats.Advance(-2);stats.Advance(float.NaN);stats.Advance(float.PositiveInfinity);
                Check(Mathf.Abs(stats.ElapsedSeconds-1.75f)<.0001f,"Elapsed time and invalid dt");
                Check(stats.TryComplete()&&stats.Completed&&events==1,"Complete once");
                stats.Advance(10);Check(!stats.TryComplete()&&events==1&&Mathf.Abs(stats.ElapsedSeconds-1.75f)<.0001f,"Completion freezes time and event");
                Debug.Log("[UKUBOLA P03] LEVEL SELF-TESTS PASSED | "+count+" checks.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P03] SELF-TEST FAILED after "+count+" checks: "+ex.Message+"\n"+ex);}
            finally{if(temporary)UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(d);}
        }
    }
}
