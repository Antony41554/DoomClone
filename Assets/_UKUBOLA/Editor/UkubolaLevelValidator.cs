using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;
using Setup=Ukubola.EditorTools.UkubolaLevelSetup;

namespace Ukubola.EditorTools
{
    public static class UkubolaLevelValidator
    {
        [MenuItem("Tools/UKUBOLA/Phase 03/Validate Level Lab",false,42)]
        public static void Validate()
        {
            if(!Setup.CanRun())return;
            try
            {
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                CheckScene(Setup.OpenInternal());
                Debug.Log("[UKUBOLA P03] VALIDATION PASSED | 0 errors. Play checks still required.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P03] VALIDATION FAILED: "+ex.Message);}
        }
        internal static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
        private static bool Near(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<.000001f;
        internal static void CheckScene(Scene scene)
        {
            Check(scene.IsValid()&&scene.isLoaded&&scene.path==Setup.Lab,"Incorrect LevelLab scene.");
            var definition=Setup.Require<LevelDefinition>(Setup.Data);
            Check(definition.TryValidate(out string reason,out var spawnCell,out var exitCell),reason);
            var plan=LevelBuilder.Plan(definition);
            var roots=scene.GetRootGameObjects();
            Check(!roots.Any(r=>Setup.RemovedRoots.Contains(r.name)),"Old diagnosis geometry remains in LevelLab.");
            var geometry=roots.Single(r=>r.name=="Level Geometry");
            int world=LayerMask.NameToLayer("World"),playerLayer=LayerMask.NameToLayer("Player"),interact=LayerMask.NameToLayer("Interactable");
            Check(world>=0&&playerLayer>=0&&interact>=0&&!Physics.GetIgnoreLayerCollision(world,playerLayer)&&!Physics.GetIgnoreLayerCollision(interact,playerLayer),"Required physics layers do not collide.");
            Check(geometry.activeInHierarchy&&geometry.layer==world&&Near(geometry.transform.position,Vector3.zero)&&Near(geometry.transform.lossyScale,Vector3.one)&&Quaternion.Angle(geometry.transform.rotation,Quaternion.identity)<.01f,"Level geometry root changed.");
            Check(geometry.transform.childCount==3,"Expected three surface groups.");
            string[] materialNames={"Floor","Ceiling","Wall"};
            for(int i=0;i<3;i++)
            {
                var child=geometry.transform.Find(LevelBuilder.GroupNames[i]);
                Check(child&&child.gameObject.activeInHierarchy&&child.gameObject.layer==world&&Near(child.localPosition,Vector3.zero)&&Near(child.localScale,Vector3.one)&&Quaternion.Angle(child.localRotation,Quaternion.identity)<.01f,"Geometry group transform or layer invalid.");
                var filter=child.GetComponent<MeshFilter>();var renderer=child.GetComponent<MeshRenderer>();
                Check(filter&&filter.sharedMesh&&AssetDatabase.GetAssetPath(filter.sharedMesh)==Setup.Generated+"/"+LevelBuilder.GroupNames[i]+".asset","Missing saved mesh.");
                var mesh=filter.sharedMesh;
                Check(mesh.vertexCount==plan[i].Count*24&&mesh.GetIndexCount(0)==(uint)(plan[i].Count*36)&&mesh.uv.Length==mesh.vertexCount,"Unexpected mesh topology or UVs.");
                Check(renderer&&renderer.enabled&&renderer.sharedMaterial&&AssetDatabase.GetAssetPath(renderer.sharedMaterial)==Setup.Generated+"/"+materialNames[i]+".mat","Missing surface material.");
                var mat=renderer.sharedMaterial;var tex=mat.GetTexture("_BaseMap");
                Check(mat.shader&&mat.shader.name=="Universal Render Pipeline/Unlit"&&mat.shader.isSupported&&!ShaderUtil.ShaderHasError(mat.shader)&&tex&&tex.filterMode==FilterMode.Point&&tex.wrapMode==TextureWrapMode.Repeat&&mat.GetTextureScale("_BaseMap")==Vector2.one&&mat.GetTextureOffset("_BaseMap")==Vector2.zero,"Surface material has incorrect shader, texture or tiling.");
                var colliders=child.GetComponents<BoxCollider>();
                Check(colliders.Length==plan[i].Count,"Collider count mismatch: "+child.name);
                for(int j=0;j<colliders.Length;j++)Check(colliders[j].enabled&&!colliders[j].isTrigger&&Near(colliders[j].center,plan[i][j].Center)&&Near(colliders[j].size,plan[i][j].Size),"Collider mismatch: "+child.name+" #"+j);
                // Check stored mesh positions and normals against the same deterministic construction.
                var expected=LevelBuilder.CreateMesh("Check",plan[i]);
                try{Check(mesh.vertices.SequenceEqual(expected.vertices)&&mesh.triangles.SequenceEqual(expected.triangles)&&mesh.normals.SequenceEqual(expected.normals)&&mesh.uv.SequenceEqual(expected.uv),"Saved mesh differs from the level definition: "+child.name);}
                finally{UnityEngine.Object.DestroyImmediate(expected);}
            }
            var motor=Setup.Find<PlayerMotor>(scene).Single();var view=motor.GetComponent<PlayerView>();var input=motor.GetComponent<PlayerInputReader>();var cc=motor.GetComponent<CharacterController>();
            Check(motor.isActiveAndEnabled&&motor.Config&&motor.Config.IsValid&&view&&view.isActiveAndEnabled&&input&&input.isActiveAndEnabled&&cc&&cc.enabled&&motor.gameObject.layer==playerLayer&&cc.stepOffset>=LevelDefinition.MaxStep&&cc.height<=LevelDefinition.Ceiling-.5f&&cc.radius<.9f,"Player or controller settings invalid.");
            var spawn=Setup.Find<PlayerSpawn>(scene).Single();
            Check(spawn.isActiveAndEnabled&&Near(spawn.Position,definition.Center(spawnCell))&&Near(motor.transform.position,spawn.Position+Vector3.up*.03f)&&Quaternion.Angle(motor.transform.rotation,spawn.Rotation)<.01f,"Player spawn or prefab position invalid.");
            var display=Setup.Find<RetroDisplay>(scene).Single();var cam=display.WorldCamera;var output=display.OutputCamera;
            Check(display.isActiveAndEnabled&&cam&&output&&cam!=output&&view.PlayerCamera==cam&&cam.isActiveAndEnabled&&output.isActiveAndEnabled&&cam.CompareTag("MainCamera")&&!output.CompareTag("MainCamera"),"Retro cameras missing or incorrect.");
            Check(Setup.Find<Camera>(scene).Count(c=>c.isActiveAndEnabled)==2&&Setup.Find<AudioListener>(scene).Count(a=>a.isActiveAndEnabled)==1&&cam.GetComponent<AudioListener>()&&cam.GetComponent<AudioListener>().isActiveAndEnabled,"Incorrect camera or AudioListener count.");
            Check(display.Target&&display.Target.width==320&&display.Target.height==200&&display.Target.filterMode==FilterMode.Point&&cam.targetTexture==display.Target&&output.targetTexture==null&&output.cullingMask==0&&output.clearFlags==CameraClearFlags.SolidColor&&output.backgroundColor==Color.black,"Retro target or output camera invalid.");
            Check((cam.cullingMask&(1<<world))!=0&&(cam.cullingMask&(1<<interact))!=0,"Camera cannot see level or exit.");
            foreach(var camera in new[]{cam,output})
            {
                var data=camera.GetComponent<UniversalAdditionalCameraData>();
                Check(data&&data.renderType==CameraRenderType.Base&&!data.renderPostProcessing&&data.antialiasing==AntialiasingMode.None&&!camera.allowHDR&&!camera.allowMSAA&&!camera.allowDynamicResolution,"Retro camera settings invalid.");
            }
            Check(display.Canvas&&display.Canvas.isActiveAndEnabled&&display.Canvas.renderMode==RenderMode.ScreenSpaceOverlay&&display.Image&&display.Image.isActiveAndEnabled&&display.Image.texture==display.Target,"Retro canvas or image missing.");
            var context=Setup.Find<LevelContext>(scene).Single();var stats=Setup.Find<LevelStats>(scene).Single();var exit=Setup.Find<LevelExit>(scene).Single();
            Check(context.isActiveAndEnabled&&context.LevelId==definition.LevelId&&stats.isActiveAndEnabled&&stats.gameObject==context.gameObject,"Level context or stats invalid.");
            var entityErrors=context.RebuildRegistry();Check(entityErrors.Count==0,string.Join("\n",entityErrors));
            Check(Setup.Find<GameSession>(scene).Count(s=>s.isActiveAndEnabled)==1,"Missing active GameSession.");
            var box=exit.GetComponent<BoxCollider>();var rb=exit.GetComponent<Rigidbody>();var entity=exit.GetComponent<EntityId>();
            Check(exit.isActiveAndEnabled&&exit.Stats==stats&&exit.Indicator&&exit.Indicator.enabled&&exit.Indicator.gameObject.activeInHierarchy&&exit.Indicator.sharedMaterial&&AssetDatabase.GetAssetPath(exit.Indicator.sharedMaterial)==Setup.Generated+"/Exit.mat"&&Near(exit.transform.position,definition.Center(exitCell))&&exit.gameObject.layer==interact,"Exit references or position invalid.");
            Check(box&&box.enabled&&box.isTrigger&&Near(box.center,new Vector3(0,.9f,0))&&Near(box.size,new Vector3(1.5f,1.8f,1.5f))&&rb&&rb.isKinematic&&!rb.useGravity&&rb.detectCollisions&&entity&&entity.IsValid,"Exit trigger, Rigidbody or ID invalid.");
            Check(exit.GetComponentsInChildren<Collider>(true).Length==1,"Exit visual must not block the player.");
            Check(Setup.Find<BoxCollider>(scene).Length==plan.Sum(g=>g.Count)+1&&Setup.Find<MeshCollider>(scene).Length==0,"Unexpected solid colliders in LevelLab.");
            foreach(var t in Setup.Find<Transform>(scene))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
        }
    }
}
