using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ukubola.Release;
using Ukubola.UI;
using Ukubola.Campaign;
using Ukubola.Persistence;
using Ukubola.Rendering;
namespace Ukubola.EditorTools
{
    public static class UkubolaFinalTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 15/Run Final Self-Tests",false,154)]public static void Run()
        {
            if(!UkubolaFinalSetup.CanRun())return;int checks=0;string temp=Path.Combine(Path.GetTempPath(),"UKUBOLA_P15_TEST_"+Guid.NewGuid().ToString("N"));Mesh box=null,cylinder=null,quad=null;GameObject root=null;Material spark=null,smoke=null;
            void Assert(bool b,string message){if(!b)throw new InvalidOperationException(message);checks++;}
            try{
                var a=UkubolaFinalSetup.GenerateArt();UkubolaFinalSetup.ValidateArt(a);
                for(int i=-500;i<=500;i++){int q=ReleaseQuality.Normalize(i);var size=ReleaseQuality.Size(i);Assert(q>=0&&q<=2&&size.x*5==size.y*8&&size.x>=320&&size.x<=640,"Quality range and 8:5 RT");Assert(ReleaseQuality.ParticleLimit(i)<=ReleaseEffects.Capacity&&ReleaseQuality.LightLimit(i)<=8,"Visual budget");}
                foreach(var s in a.Sprites)Assert(s&&s.texture&&s.texture.filterMode==FilterMode.Point&&s.rect.width>0&&s.rect.height>0,"Sprite import: "+s.name);
                foreach(var t in a.Textures)Assert(t&&t.filterMode==FilterMode.Point,"Texture import: "+t.name);
                foreach(var clip in a.Music.Concat(a.Effects).Concat(a.Weapons))Assert(clip&&clip.samples>0&&clip.channels==1,"Audio import: "+clip.name);
                box=ReleaseDecoration.BoxMesh();cylinder=ReleaseDecoration.CylinderMesh();quad=ReleaseDecoration.QuadMesh();
                Assert(box.vertexCount==24&&box.triangles.Length==36,"Box mesh");Assert(cylinder.vertexCount==160&&cylinder.triangles.Length==192,"Cylinder mesh");Assert(quad.vertexCount==4&&quad.triangles.Length==6,"Quad mesh");
                foreach(var mesh in new[]{box,cylinder}){var verts=mesh.vertices;var indices=mesh.triangles;for(int j=0;j<indices.Length;j+=3){var p=verts[indices[j]];var q=verts[indices[j+1]];var r=verts[indices[j+2]];var cross=Vector3.Cross(q-p,r-p);Assert(cross.sqrMagnitude>1e-10f&&Vector3.Dot(cross,(p+q+r)/3f)>0,"Outward mesh triangle");}}
                root=new GameObject("P15 temporary FX test"){hideFlags=HideFlags.HideAndDontSave};spark=new Material(a.Particle);smoke=new Material(a.Particle);var fx=new ReleaseEffects(root.transform,quad,spark,smoke);
                foreach(int quality in new[]{0,1,2}){int limit=ReleaseQuality.ParticleLimit(quality);fx.SetLimit(limit);for(int i=0;i<120;i++)fx.Burst(Vector3.zero,Color.red,1);Assert(fx.ActiveCount==limit,"Bounded pool");Assert(root.GetComponentsInChildren<Collider>(true).Length==0,"Particles have no colliders");fx.Clear();Assert(fx.ActiveCount==0,"Clear pool");}fx.Dispose();
                string folder=Path.Combine(temp,"files"),zip=Path.Combine(temp,"test.zip");Directory.CreateDirectory(Path.Combine(folder,"sub"));File.WriteAllText(Path.Combine(folder,"a.txt"),"UKUBOLA test");File.WriteAllBytes(Path.Combine(folder,"sub","b.bin"),Enumerable.Range(0,256).Select(i=>(byte)i).ToArray());UkubolaFinalBuild.Package(folder,zip);Assert(File.Exists(zip)&&File.Exists(zip+".sha256"),"Verified test package");Assert(!File.Exists(zip+".partial"),"No partial archive");
                Debug.Log("[UKUBOLA P15] FINAL SELF-TESTS PASSED | "+checks+" checks.");
            }catch(Exception ex){Debug.LogError("[UKUBOLA P15] SELF-TEST FAILED: "+ex);}
            finally{if(root)UnityEngine.Object.DestroyImmediate(root);if(spark)UnityEngine.Object.DestroyImmediate(spark);if(smoke)UnityEngine.Object.DestroyImmediate(smoke);if(box)UnityEngine.Object.DestroyImmediate(box);if(cylinder)UnityEngine.Object.DestroyImmediate(cylinder);if(quad)UnityEngine.Object.DestroyImmediate(quad);if(Directory.Exists(temp))Directory.Delete(temp,true);}
        }
        [MenuItem("Tools/UKUBOLA/Phase 15/Measure 300 Gameplay Frames (Play Mode)",false,156)]public static void Measure()
        {
            var r=ReleasePresentation.Active;if(!EditorApplication.isPlaying||!r||r.Stage<0||!r.Ready){Debug.LogWarning("[UKUBOLA P15] Start a mission before measuring performance.");return;}
            if(!r.GetComponent<ReleasePerformanceProbe>()){r.gameObject.AddComponent<ReleasePerformanceProbe>();Debug.Log("[UKUBOLA P15] Collecting 300 active gameplay frames; focus Game and play normally.");}
        }
        [MenuItem("Tools/UKUBOLA/Phase 15/Check Presentation (Play Mode)",false,155)]public static void Live()
        {
            if(!EditorApplication.isPlaying){Debug.LogWarning("[UKUBOLA P15] Enter Play first.");return;}
            try{
                var r=ReleasePresentation.Active;UkubolaFinalSetup.Check(r&&r.Ready,"Release not active or incomplete");var s=SceneManager.GetActiveScene();var f=UkubolaFinalSetup.Find<MenuFlow>(s).Single();
                if(r.Stage>=0){
                    UkubolaFinalSetup.Check(r.PresentationRoot&&r.PresentationRoot.GetComponentsInChildren<Collider>(true).Length==0,"Presentation added a collider");
                    UkubolaFinalSetup.Check(r.ParticleCount<=ReleaseQuality.ParticleLimit(r.Quality),"Particle budget exceeded");
                    var size=r.ActualResolution;var requested=ReleaseQuality.Size(r.Quality);UkubolaFinalSetup.Check(size==requested||size==ReleaseQuality.Size(0),"Unexpected render target size");
                    var retro=UkubolaFinalSetup.Find<RetroDisplay>(s).Single();UkubolaFinalSetup.Check(retro.WorldCamera.targetTexture==retro.Image.texture&&Mathf.Abs(retro.WorldCamera.aspect-RetroDisplay.DisplayAspect)<.001f,"Retro target mismatch");
                    UkubolaFinalSetup.Check(UkubolaFinalSetup.Find<Camera>(s).Count(x=>x.isActiveAndEnabled)==2&&UkubolaFinalSetup.Find<AudioListener>(s).Count(x=>x.isActiveAndEnabled)==1,"Wrong camera/listener count");
                    if(f.Page==MenuPage.Paused){var stage=f.GetComponent<CampaignStage>();var data=SaveWorld.Capture(stage,f.Hud);SaveWorld.Check(SaveStore.Decode(SaveStore.Encode(data)),stage,f.Hud);Debug.Log("[UKUBOLA P15] SAVE SNAPSHOT ROUND TRIP PASSED | no production save changed.");}
                    Debug.Log("[UKUBOLA P15] LIVE PRESENTATION PASSED | "+ReleaseQuality.Label(r.Quality)+" "+size+" | props="+r.DecorationCount+" | FX="+r.ParticleCount);
                }else Debug.Log("[UKUBOLA P15] TITLE PRESENTATION PASSED");
            }catch(Exception ex){Debug.LogError("[UKUBOLA P15] LIVE CHECK FAILED: "+ex);}
        }
    }
}
