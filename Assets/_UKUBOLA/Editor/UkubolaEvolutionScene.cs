using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.UI;
using Ukubola.World;
using Art = Ukubola.EditorTools.UkubolaEvolutionArt;
using Object = UnityEngine.Object;

namespace Ukubola.EditorTools
{
    public static class UkubolaEvolutionScene
    {
        public const string RoomPath = "Assets/_UKUBOLA/Scenes/Evolution/ArtRoom.unity";
        private const string Stamp = Art.Root + "/ArtRoom.scene.sha256.txt";
        private static int world, ui, viewmodel;
        public static string BakeArtRoom()
        {
            EvolutionManifest.Validate();
            if (!File.Exists(RoomPath) || !File.Exists(Stamp) || File.ReadAllText(Stamp).Trim() != UkubolaAutomation.HashFile(RoomPath)) throw new InvalidOperationException("Generate and validate the room before baking; manual changes are protected.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(RoomPath);
                string path = Art.Root + "/Lighting.asset"; var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
                if (!settings) { settings = new LightingSettings(); AssetDatabase.CreateAsset(settings,path); }
                settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;
                settings.lightmapResolution=8;settings.lightmapMaxSize=512;settings.directSampleCount=32;settings.indirectSampleCount=64;settings.environmentSampleCount=32;settings.maxBounces=2;settings.mixedBakeMode=MixedLightingMode.Shadowmask;
                Lightmapping.lightingSettings=settings;EditorUtility.SetDirty(settings);
                if(!Lightmapping.Bake() || LightmapSettings.lightmaps.Length==0)throw new InvalidOperationException("Lighting bake produced no lightmaps.");
                foreach(var probe in Find<ReflectionProbe>())
                {
                    probe.mode=ReflectionProbeMode.Baked;
                    string output=Art.Root+"/IndustrialReflection.exr";
                    if(!Lightmapping.BakeReflectionProbe(probe,output))throw new InvalidOperationException("Reflection bake failed.");
                    AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);probe.bakedTexture=Art.Asset<Cubemap>(output);
                }
                ValidateArtRoom();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                File.WriteAllText(Stamp,UkubolaAutomation.HashFile(RoomPath));EvolutionManifest.Record();AssetDatabase.ImportAsset(EvolutionManifest.Path);
                return "Baked lightmaps: "+LightmapSettings.lightmaps.Length+"; "+RoomPath;
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }
        private static T[] Find<T>() where T : Component => UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static int Layer(string name)
        {
            int existing = LayerMask.NameToLayer(name); if (existing >= 0) return existing;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); var layers = tags.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++) if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layers.GetArrayElementAtIndex(i).stringValue = name; tags.ApplyModifiedPropertiesWithoutUndo(); return i; }
            throw new InvalidOperationException("No free layer for " + name);
        }
        public static string BuildArtRoom()
        {
            EvolutionManifest.Validate();
            var previous = EditorSceneManager.GetSceneManagerSetup();
            string staging = Art.Root + "/ArtRoom.staging.unity";
            try
            {
                Art.Folder("Assets/_UKUBOLA/Scenes/Evolution"); Art.Folder(Art.Root); Art.ImportTextures();
                if (File.Exists(RoomPath) && (!File.Exists(Stamp) || File.ReadAllText(Stamp).Trim() != UkubolaAutomation.HashFile(RoomPath))) throw new InvalidOperationException("Generated ArtRoom has unrecorded edits; preserving it.");
                world = Layer("World"); ui = Layer("UI"); viewmodel = Layer("ViewModel");
                var legacy = EditorSceneManager.OpenScene(Ukubola.Persistence.SaveCampaignPaths.Missions[0]);
                if (!EditorSceneManager.SaveScene(legacy, staging, true)) throw new IOException("Cannot create presentation scene copy.");
                var scene = EditorSceneManager.OpenScene(staging);
                var display = Find<RetroDisplay>().Single(); var player = display.WorldCamera.transform.root.gameObject; var weapons = player.GetComponent<WeaponController>();
                var preserved = new[] { player, display.transform.root.gameObject, display.OutputCamera.transform.root.gameObject };
                foreach (var root in scene.GetRootGameObjects()) if (!preserved.Contains(root)) Object.DestroyImmediate(root);
                foreach (var component in Find<MonoBehaviour>())
                    if (component is ClassicHud || component is Automap || component.GetType().Name.EndsWith("Diagnostics", StringComparison.Ordinal)) Object.DestroyImmediate(component);
                foreach (Transform child in display.Image.transform.Cast<Transform>().ToArray())
                    if (weapons.View.Image.transform != child && !weapons.View.Image.transform.IsChildOf(child)) Object.DestroyImmediate(child.gameObject);
                display.enabled = false; display.WorldCamera.transform.localRotation = Quaternion.identity;
                player.transform.SetPositionAndRotation(new Vector3(0,.03f,2.5f), Quaternion.identity);
                display.WorldCamera.cullingMask = ~(1 << ui | 1 << viewmodel);
                display.WorldCamera.farClipPlane = 90; display.WorldCamera.fieldOfView = 72;
                var profile = AssetDatabase.LoadAssetAtPath<PresentationProfile>(Art.Root + "/Presentation.asset");
                if (!profile) { profile = ScriptableObject.CreateInstance<PresentationProfile>(); AssetDatabase.CreateAsset(profile, Art.Root + "/Presentation.asset"); }
                BuildRenderProfiles(profile);
                var modern = display.gameObject.AddComponent<ModernDisplay>(); modern.Configure(display, profile);
                var geometry = new GameObject("Geometry").transform;
                var props = new GameObject("Presentation").transform;
                BuildArchitecture(geometry, props);
                BuildLighting(props);
                BuildAtmosphere(props);
                BuildWeaponCamera(display, player, weapons);
                BuildHud(display, player.GetComponent<Health>(), weapons);
                var navigation = new GameObject("Navigation").AddComponent<EnemyNavigation>(); navigation.Configure(BakeNavigation(geometry));
                var noise = new GameObject("Combat Noise").AddComponent<NoiseEmitter>(); noise.Configure(weapons);
                Enemy(props, "Guard", new Vector3(-2.2f,0,10), player.GetComponent<Health>(), display.WorldCamera, noise, navigation);
                Enemy(props, "Imp", new Vector3(2.8f,0,21), player.GetComponent<Health>(), display.WorldCamera, noise, navigation);
                Pickup(props, "Medikit", new Vector3(-4.3f,0,7)); Pickup(props, "BlueCard", new Vector3(5.8f,0,14)); Pickup(props,"Shells",new Vector3(3,0,6));
                RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.3f,.35f,.37f); RenderSettings.ambientEquatorColor = new Color(.19f,.23f,.25f); RenderSettings.ambientGroundColor = new Color(.09f,.105f,.12f);
                RenderSettings.fog = true; RenderSettings.fogColor = new Color(.025f,.045f,.06f); RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .014f;
                RenderSettings.skybox = null; RenderSettings.reflectionIntensity = .8f;
                var probe = new GameObject("Industrial reflection probe").AddComponent<ReflectionProbe>(); probe.transform.position = new Vector3(0,2.5f,15); probe.size = new Vector3(20,8,36); probe.boxProjection = true; probe.resolution = 128; probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.cullingMask = 1 << world; probe.clearFlags = ReflectionProbeClearFlags.SolidColor; probe.backgroundColor = new Color(.025f,.04f,.05f);
                foreach (var r in geometry.GetComponentsInChildren<MeshRenderer>()) GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccluderStatic);
                var points = new GameObject("Light probes").AddComponent<LightProbeGroup>();
                points.probePositions = Enumerable.Range(0,7).SelectMany(z => new[] { new Vector3(-5,1.5f,z*4+2), new Vector3(0,1.5f,z*4+2),new Vector3(5,1.5f,z*4+2),new Vector3(0,4,z*4+2) }).ToArray();
                AssetDatabase.SaveAssets(); ValidateArtRoom(); EditorSceneManager.SaveScene(scene, RoomPath);
                File.WriteAllText(Stamp, UkubolaAutomation.HashFile(RoomPath)); AssetDatabase.Refresh();EvolutionManifest.Record();AssetDatabase.ImportAsset(EvolutionManifest.Path);
                return RoomPath;
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); if (File.Exists(staging)) AssetDatabase.DeleteAsset(staging); }
        }
        private static void BuildArchitecture(Transform geometry, Transform props)
        {
            var wall = Art.Surface("0_Wall"); var floor = Art.Surface("0_Floor"); var ceiling = Art.Surface("0_Ceiling"); var trim = Art.Surface("0_Trim"); var crate = Art.Surface("0_Crate");
            var black = Art.Solid("DeepMetal", new Color(.035f,.05f,.055f), .65f,.6f); var safety = Art.Solid("SafetyYellow", new Color(.8f,.48f,.07f), .25f,.4f);
            var cyan = Art.Solid("CyanLight", new Color(.03f,.6f,.85f), .1f,.75f,5); var amber = Art.Solid("AmberLight", new Color(1,.18f,.035f), .1f,.6f,4);
            Art.Box(geometry,"Floor_Main",new Vector3(0,-.18f,14),new Vector3(18,.36f,28),floor,true);
            Art.Box(geometry,"Floor_Corridor",new Vector3(0,-.18f,31),new Vector3(4.8f,.36f,6),floor,true);
            Art.Box(geometry,"Ceiling_Main",new Vector3(0,5.85f,14),new Vector3(18,.3f,28),ceiling,true);
            Art.Box(geometry,"Wall_Back",new Vector3(0,2.8f,-.18f),new Vector3(18,5.6f,.36f),wall,true);
            for(int z=2;z<28;z+=4) foreach(float side in new[]{-1f,1f})
            {
                Art.Box(geometry,"Wall_Panel",new Vector3(side*9.1f,2.8f,z),new Vector3(.3f,5.6f,4),wall,true);
                Art.Box(geometry,"Wall_LowerRail",new Vector3(side*8.85f,.55f,z),new Vector3(.24f,1.1f,3.9f),trim,true);
                Art.Box(geometry,"Panel_Column",new Vector3(side*8.8f,2.8f,z-1.9f),new Vector3(.42f,5.6f,.22f),trim,true);
                Art.Box(geometry,"Wall_HighRail",new Vector3(side*8.8f,4.6f,z),new Vector3(.4f,.18f,4),black);
                Art.Box(props,"Wall_Lamp",new Vector3(side*8.6f,4.2f,z),new Vector3(.1f,.18f,1.4f),z%8==2?cyan:amber);
            }
            foreach(float side in new[]{-1f,1f})
            {
                Art.Box(geometry,"Portal_Side",new Vector3(side*5.7f,2.8f,28),new Vector3(6.6f,5.6f,.4f),wall,true);
                Art.Box(geometry,"Hall_Wall",new Vector3(side*2.55f,2.5f,31),new Vector3(.3f,5,6),wall,true);
            }
            Art.Box(geometry,"Portal_Header",new Vector3(0,5.25f,28),new Vector3(4.8f,1.1f,.5f),trim,true);
            Art.Box(geometry,"Hall_Ceiling",new Vector3(0,5.2f,31),new Vector3(5,.35f,6),ceiling,true);
            Art.Box(geometry,"Hall_End",new Vector3(0,2.5f,34),new Vector3(5,5,.3f),wall,true);
            for(int z=5;z<30;z+=8)
            {
                Art.Box(geometry,"Ceiling_Crossbeam",new Vector3(0,5.35f,z),new Vector3(17.5f,.45f,.55f),trim);
                foreach(float side in new[]{-1f,1f})
                {
                    Art.Box(geometry,"Beam_Brace",new Vector3(side*7.8f,4.8f,z),new Vector3(1.6f,.3f,.6f),trim,rotation:Quaternion.Euler(0,0,side*40));
                    Art.Box(props,"Ceiling_Fixture",new Vector3(side*3.5f,5.32f,z),new Vector3(2.4f,.22f,.8f),black);
                    Art.Box(props,"Ceiling_Light",new Vector3(side*3.5f,5.19f,z),new Vector3(2.1f,.05f,.55f),cyan);
                }
            }
            foreach(float side in new[]{-1f,1f})
            {
                Art.Tube(props,"Main coolant pipe",new[]{new Vector3(side*8.45f,3.5f,1),new Vector3(side*8.45f,3.5f,27)},.18f,trim,12);
                Art.Tube(props,"Power cable",Enumerable.Range(0,17).Select(i=>new Vector3(side*6.7f,5.2f-Mathf.Sin(i*Mathf.PI/16)*.8f,5+i*.65f)).ToArray(),.065f,black);
            }
            foreach(var p in new[]{new Vector3(-5.7f,.8f,4.6f),new Vector3(-6.1f,2.4f,4.8f),new Vector3(6.6f,.65f,5.5f),new Vector3(6.8f,.8f,18.5f),new Vector3(6.8f,2.4f,18.5f),new Vector3(-6.3f,.8f,22)})
                Art.Box(geometry,"Cargo Container",p,new Vector3(2.1f,p.y==.65f?1.3f:1.6f,1.7f),crate,true);
            var door = Art.Surface("Door",new Color(.6f,.7f,.9f));
            Art.Box(geometry,"Access Door",new Vector3(8.75f,2,14),new Vector3(.18f,4,3.6f),door,true);
            foreach(float side in new[]{-1f,1f})Art.Box(props,"Blue Door Strip",new Vector3(8.62f,2,14+side*1.9f),new Vector3(.12f,4.2f,.13f),cyan);
            Art.Box(props,"Access Reader",new Vector3(8.5f,1.4f,16.4f),new Vector3(.25f,.55f,.36f),trim);
            Art.Box(props,"Reader Screen",new Vector3(8.35f,1.46f,16.4f),new Vector3(.03f,.32f,.24f),cyan);
            Label(props,"UKUBOLA",new Vector3(8.63f,4.6f,14),90,.1f,Color.white);
            Label(props,"THE OUTPOST\nSECTOR B-2  >",new Vector3(-8.62f,2.5f,7),-90,.08f,new Color(.65f,.74f,.73f));
            Label(props,"B-02  /  TRANSFER",new Vector3(0,4.35f,27.7f),0,.08f,new Color(.8f,.77f,.6f));
            foreach(float x in new[]{-2.7f,2.7f})for(int z=9;z<25;z+=2)Art.Box(props,"Lane Marker",new Vector3(x,.008f,z),new Vector3(.12f,.012f,.65f),safety);
            for(int i=0;i<5;i++)Art.Box(props,"Vent Slat",new Vector3(-8.63f,1.6f+i*.1f,19),new Vector3(.08f,.045f,1.5f),black);
            // Foreground structural depth and an elevated service route, with separate simple collision.
            foreach(float side in new[]{-1f,1f})
            {
                Art.Box(geometry,"Service Column",new Vector3(side*5.8f,2.8f,11),new Vector3(.65f,5.6f,.85f),trim,true);
                Art.Box(props,"Column Amber Indicator",new Vector3(side*5.8f,3.6f,10.55f),new Vector3(.42f,.12f,.06f),amber);
            }
            Art.Box(geometry,"Maintenance Walkway",new Vector3(-7,2.3f,20.75f),new Vector3(3.2f,.25f,9.5f),trim,true);
            Art.Box(geometry,"Walkway Railing",new Vector3(-5.45f,3.05f,21),new Vector3(.08f,.09f,9),safety);
            for(int z=17;z<=25;z+=2)Art.Box(geometry,"Railing Post",new Vector3(-5.45f,2.7f,z),new Vector3(.08f,.8f,.08f),trim);
            for(int i=0;i<12;i++)Art.Box(geometry,"Service Stair",new Vector3(-7,(i+1)*.2f-.1f,12.1f+i*.35f),new Vector3(2.5f,.2f,.35f),trim,true);
        }
        private static void BuildRenderProfiles(PresentationProfile profile)
        {
            Art.Folder(Art.Root+"/Rendering");profile.pipelines=new UniversalRenderPipelineAsset[3];
            for(int q=0;q<3;q++)
            {
                string rendererPath=Art.Root+"/Rendering/Renderer_"+q+".asset",pipelinePath=Art.Root+"/Rendering/Pipeline_"+q+".asset";
                if(!File.Exists(rendererPath)&&!AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset",rendererPath))throw new IOException("Cannot create independent renderer.");
                if(!File.Exists(pipelinePath)&&!AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset",pipelinePath))throw new IOException("Cannot create independent pipeline.");
                var renderer=Art.Asset<UniversalRendererData>(rendererPath);
                foreach(var feature in renderer.rendererFeatures)
                {
                    feature.SetActive(q>0);
                    if(feature.GetType().Name=="ScreenSpaceAmbientOcclusion")
                    {var featureData=new SerializedObject(feature);featureData.FindProperty("m_Settings.Downsample").boolValue=q==1;featureData.FindProperty("m_Settings.Intensity").floatValue=.3f;featureData.ApplyModifiedPropertiesWithoutUndo();}
                    EditorUtility.SetDirty(feature);
                }
                var pipeline=Art.Asset<UniversalRenderPipelineAsset>(pipelinePath);var data=new SerializedObject(pipeline);
                var renderers=data.FindProperty("m_RendererDataList");renderers.arraySize=1;renderers.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
                data.FindProperty("m_DefaultRendererIndex").intValue=0;data.FindProperty("m_AdditionalLightShadowsSupported").boolValue=q>0;
                data.FindProperty("m_SupportsLightLayers").boolValue=false;data.FindProperty("m_EnableLODCrossFade").boolValue=false;
                // Unity initializes each material's ordinary ShadowCaster pass even in
                // a punctual-light-only room. Keep that pass through the High profile
                // and retain the receiver variants URP selects for every profile.
                data.FindProperty("m_MainLightShadowsSupported").boolValue=q==2;
                data.FindProperty("m_AdditionalLightsShadowmapResolution").intValue=new[]{512,1024,2048}[q];data.ApplyModifiedPropertiesWithoutUndo();
                pipeline.msaaSampleCount=1;pipeline.renderScale=1;pipeline.shadowDistance=new[]{14f,24f,36f}[q];pipeline.supportsHDR=true;
                profile.pipelines[q]=pipeline;EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(pipeline);
            }
            EditorUtility.SetDirty(profile);
        }
        private static void Label(Transform parent,string text,Vector3 position,float yaw,float size,Color color)
        {
            var go=new GameObject("Industrial Sign");go.layer=world;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var mesh=go.AddComponent<TextMesh>();mesh.text=text;mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");mesh.fontSize=64;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=color;go.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;
        }
        private static void BuildLighting(Transform parent)
        {
            for(int z=5;z<30;z+=8)foreach(float side in new[]{-1f,1f})
            {
                var go=new GameObject("Cyan overhead illumination");go.transform.SetParent(parent,false);go.transform.position=new Vector3(side*3.5f,4.95f,z);go.transform.rotation=Quaternion.Euler(90,0,0);
                var light=go.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=105;light.innerSpotAngle=65;light.range=12;light.intensity=4.5f;light.color=new Color(.32f,.73f,1);light.shadows=z==5?LightShadows.Soft:LightShadows.None;light.lightmapBakeType=LightmapBakeType.Mixed;
            }
            foreach(var p in new[]{new Vector3(-7,2.2f,5),new Vector3(6.8f,2.6f,20),new Vector3(0,3.7f,29)})
            {var go=new GameObject("Amber industrial fill");go.transform.SetParent(parent,false);go.transform.position=p;var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=10;light.intensity=6;light.color=new Color(1,.32f,.09f);light.lightmapBakeType=LightmapBakeType.Mixed;}
            foreach(float side in new[]{-1f,1f})for(int z=9;z<28;z+=8)
            {var go=new GameObject("Cyan wall wash");go.transform.SetParent(parent,false);go.transform.position=new Vector3(side*7,3.2f,z);var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=3;light.color=new Color(.12f,.65f,1);light.lightmapBakeType=LightmapBakeType.Mixed;}
            foreach(var p in new[]{new Vector3(-1,3.3f,7),new Vector3(2,3.3f,18)})
            {var go=new GameObject("Combat visibility fill");go.transform.SetParent(parent,false);go.transform.position=p;var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=11;light.intensity=12;light.color=new Color(.7f,.8f,.85f);light.lightmapBakeType=LightmapBakeType.Mixed;}
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art.Root+"/Post.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Art.Root+"/Post.asset");var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.3f);bloom.threshold.Override(1.1f);var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);var grade=profile.Add<ColorAdjustments>(true);grade.contrast.Override(12);foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);}
            var volume=new GameObject("Industrial grading").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
        }
        private static void BuildAtmosphere(Transform parent)
        {
            string path=Art.Root+"/Materials/Steam.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,path);}
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
            material.SetTexture("_BaseMap",Art.Asset<Texture2D>(Art.Textures+"/Steam.png"));material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);
            foreach(var p in new[]{new Vector3(-8,2.8f,8),new Vector3(7,4.6f,23)})
            {
                var go=new GameObject("Cooling steam");go.transform.SetParent(parent,false);go.transform.position=p;go.transform.rotation=Quaternion.Euler(-75,0,0);
                var ps=go.AddComponent<ParticleSystem>();var main=ps.main;main.duration=6;main.loop=true;main.startLifetime=3.5f;main.startSpeed=.45f;main.startSize=.8f;main.startColor=new Color(.45f,.58f,.64f,.16f);main.maxParticles=24;main.simulationSpace=ParticleSystemSimulationSpace.World;main.useUnscaledTime=false;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=18;shape.radius=.12f;var emission=ps.emission;emission.rateOverTime=5;
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,2));
                var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
                var particleRenderer=ps.GetComponent<ParticleSystemRenderer>();particleRenderer.sharedMaterial=material;particleRenderer.enableGPUInstancing=false;
            }
        }
        private static void BuildWeaponCamera(RetroDisplay display,GameObject player,WeaponController weapons)
        {
            var cam=new GameObject("Viewmodel Camera").AddComponent<Camera>();cam.transform.SetParent(display.WorldCamera.transform,false);cam.cullingMask=1<<viewmodel;cam.nearClipPlane=.01f;cam.farClipPlane=5;cam.fieldOfView=58;
            var data=cam.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Overlay;display.WorldCamera.GetUniversalAdditionalCameraData().cameraStack.Add(cam);
            var mount=new GameObject("Weapon Mount").transform;mount.SetParent(cam.transform,false);mount.localPosition=new Vector3(0,-.28f,.4f);
            var iron=Art.Surface("Weapon");var dark=Art.Solid("Weapon Dark",new Color(.055f,.065f,.07f),.7f,.6f);var skin=Art.Solid("Glove",new Color(.27f,.25f,.18f),.05f,.25f);
            var models=new GameObject[8];
            for(int i=0;i<8;i++)
            {
                var root=new GameObject(((WeaponId)i).ToString());root.transform.SetParent(mount,false);models[i]=root;
                Art.Box(root.transform,"Receiver",new Vector3(0,0,.24f),new Vector3(i==5?.22f:.14f,.14f,i==3?.58f:.4f),iron);
                Art.Box(root.transform,"Top Rail",new Vector3(0,.09f,.25f),new Vector3(.045f,.025f,.25f),dark);
                Art.Box(root.transform,"Rear Sight Left",new Vector3(-.042f,.105f,.095f),new Vector3(.025f,.04f,.045f),dark);
                Art.Box(root.transform,"Rear Sight Right",new Vector3(.042f,.105f,.095f),new Vector3(.025f,.04f,.045f),dark);
                Art.Box(root.transform,"Receiver Plate",new Vector3(0,.073f,.25f),new Vector3(.13f,.012f,.3f),iron);
                foreach(float side in new[]{-1f,1f})Art.Box(root.transform,"Receiver Pin",new Vector3(side*.072f,.025f,.19f),new Vector3(.008f,.025f,.025f),dark);
                float length=i==3?.72f:i==5?.6f:.46f;
                Art.Tube(root.transform,"Barrel",new[]{new Vector3(0,.02f,.2f),new Vector3(0,.02f,length)},i==5?.11f:.04f,dark,8);
                Art.Box(root.transform,"Grip",new Vector3(0,-.12f,.15f),new Vector3(.1f,.2f,.12f),dark,rotation:Quaternion.Euler(-15,0,0));
                foreach(float side in new[]{-1f,1f})
                { Art.Box(root.transform,"Arm",new Vector3(side*.13f,-.18f,-.02f),new Vector3(.12f,.14f,.4f),skin,rotation:Quaternion.Euler(0,-side*15,0));Art.Box(root.transform,"Hand",new Vector3(side*.075f,-.09f,.14f),new Vector3(.12f,.14f,.16f),skin); }
                if(i==3)Art.Box(root.transform,"Pump",new Vector3(0,-.045f,.46f),new Vector3(.17f,.12f,.24f),iron);
                if(i==4)for(int b=0;b<5;b++){float a=b*Mathf.PI*2/5;Art.Tube(root.transform,"Rotary Barrel",new[]{new Vector3(Mathf.Cos(a)*.08f,Mathf.Sin(a)*.08f,.3f),new Vector3(Mathf.Cos(a)*.08f,Mathf.Sin(a)*.08f,.75f)},.025f,dark);}
                if(i==1)Art.Box(root.transform,"Chainsaw Blade",new Vector3(0,.02f,.65f),new Vector3(.06f,.22f,.6f),iron);
                if(i>=6){var glow=Art.Solid("Weapon Energy",new Color(.03f,.7f,1),.2f,.6f,3);for(int b=0;b<4;b++)Art.Box(root.transform,"Energy Coil",new Vector3(0,.04f,.3f+b*.08f),new Vector3(.2f,.16f,.03f),glow);}
                if(i==0){foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name!="Hand"&&r.name!="Arm"))r.enabled=false;}
                root.SetActive(i==2);
            }
            var muzzle=Art.Box(mount,"Muzzle flash",new Vector3(0,.02f,.9f),new Vector3(.2f,.2f,.03f),Art.Solid("Muzzle Emission",new Color(1,.5f,.08f),0,0,8));muzzle.SetActive(false);
            var light=new GameObject("Muzzle point light").AddComponent<Light>();light.transform.SetParent(display.WorldCamera.transform,false);light.transform.localPosition=Vector3.forward*.7f;light.type=LightType.Point;light.range=5;light.color=new Color(1,.5f,.1f);light.intensity=0;
            var fill=new GameObject("Viewmodel fill").AddComponent<Light>();fill.transform.SetParent(cam.transform,false);fill.transform.localPosition=new Vector3(-.25f,.25f,.2f);fill.type=LightType.Point;fill.range=2;fill.intensity=.35f;fill.color=new Color(1,.78f,.55f);fill.cullingMask=1<<viewmodel;
            foreach(var t in mount.GetComponentsInChildren<Transform>(true))t.gameObject.layer=viewmodel;
            player.AddComponent<ModelWeaponView>().Configure(weapons.View,weapons,mount,models,light,muzzle);
        }
        private static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=ui;var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static Text Text(Transform parent,string name,string value,int size,Color color,Vector2 min,Vector2 max)
        {var r=Rect(parent,name,min,max);var t=r.gameObject.AddComponent<Text>();t.text=value;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.fontStyle=FontStyle.Bold;t.alignment=TextAnchor.MiddleCenter;t.color=color;t.raycastTarget=false;return t;}
        private static void BuildHud(RetroDisplay display,Health health,WeaponController weapons)
        {
            var canvas=display.Canvas;var scaler=canvas.GetComponent<CanvasScaler>();if(!scaler)scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var root=Rect(canvas.transform,"Evolution HUD",Vector2.zero,Vector2.one);
            var bar=Rect(root,"Status Bar",Vector2.zero,new Vector2(1,.15f));
            var panel=Art.Asset<Texture2D>(Art.Textures+"/HudPanel.png");var digits=Art.Asset<Texture2D>(Art.Textures+"/Digits.png");
            RawImage[] Counter(string name,string label,float left,float right,Color color)
            {
                var box=Rect(bar,name,new Vector2(left,0),new Vector2(right,1));var image=box.gameObject.AddComponent<RawImage>();image.texture=panel;image.raycastTarget=false;
                Text(box,"Label",label,18,new Color(.7f,.7f,.6f),new Vector2(0,.67f),Vector2.one);
                var values=new RawImage[3];for(int i=0;i<3;i++){var r=Rect(box,"Digit"+i,new Vector2(.16f+i*.22f,.06f),new Vector2(.36f+i*.22f,.65f));values[i]=r.gameObject.AddComponent<RawImage>();values[i].texture=digits;values[i].color=color;values[i].raycastTarget=false;}return values;
            }
            var ammo=Counter("Ammo","AMMO",0,.22f,new Color(1,.68f,.15f));var hp=Counter("Health","HEALTH",.22f,.44f,new Color(1,.12f,.08f));
            var armor=Counter("Armor","ARMOR",.56f,.78f,new Color(.05f,.85f,1));
            var face=Rect(bar,"Face",new Vector2(.44f,0),new Vector2(.56f,1));face.gameObject.AddComponent<RawImage>().texture=panel;
            var portrait=Rect(face,"Portrait",new Vector2(.15f,.1f),new Vector2(.85f,.9f));portrait.gameObject.AddComponent<RawImage>().texture=Art.Asset<Texture2D>("Assets/_UKUBOLA/Art/Phase15/UI/Face_00.png");
            var keys=Text(bar,"Keys","ACCESS",19,new Color(.1f,.8f,1),new Vector2(.79f,.1f),new Vector2(.99f,.9f));
            var message=Text(root,"Interaction","E / SPACE : USE",17,new Color(.75f,.83f,.8f),new Vector2(.2f,.17f),new Vector2(.8f,.23f));
            Text(root,"Crosshair","+",19,new Color(.8f,.86f,.8f),new Vector2(.485f,.485f),new Vector2(.515f,.515f));
            Text(root,"Mission","UKUBOLA  /  THE OUTPOST",16,new Color(.6f,.7f,.7f),new Vector2(.66f,.94f),new Vector2(.98f,.99f));
            root.gameObject.AddComponent<EvolutionHud>().Configure(health,weapons,ammo,hp,armor,message,keys);
        }
        private static NavMeshData BakeNavigation(Transform geometry)
        {
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.35f;settings.agentHeight=1.7f;settings.agentClimb=.25f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.1f;
            var sources=geometry.GetComponentsInChildren<BoxCollider>().Select(c=>new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=c.transform.localToWorldMatrix*Matrix4x4.Translate(c.center),size=c.size,area=0}).ToList();
            var bounds=new Bounds(new Vector3(0,2,17),new Vector3(24,12,40));string path=Art.Root+"/ArtRoomNav.asset";var data=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if(!data){data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!data)throw new InvalidOperationException("Navigation bake failed.");AssetDatabase.CreateAsset(data,path);}
            else{if(!NavMeshBuilder.UpdateNavMeshData(data,settings,sources,bounds))throw new InvalidOperationException("Navigation update failed.");EditorUtility.SetDirty(data);}
            return data;
        }
        private static void Enemy(Transform parent,string name,Vector3 position,Health player,Camera viewer,NoiseEmitter noise,EnemyNavigation nav)
        {
            var definition=AssetDatabase.FindAssets(name+" t:EnemyDefinition").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<EnemyDefinition>).First(d=>d.name==name);
            var root=new GameObject(name);root.SetActive(false);root.layer=LayerMask.NameToLayer("Enemy");root.transform.SetParent(parent,false);root.transform.position=position;root.transform.rotation=Quaternion.Euler(0,180,0);
            root.AddComponent<Health>().Configure(definition.Health,null);var body=root.AddComponent<CapsuleCollider>();body.height=1.9f;body.center=Vector3.up*.95f;body.radius=.4f;
            var agent=root.AddComponent<NavMeshAgent>();agent.enabled=false;agent.agentTypeID=NavMesh.GetSettingsByIndex(0).agentTypeID;agent.radius=.35f;agent.height=1.7f;agent.speed=definition.Speed;agent.acceleration=12;agent.stoppingDistance=1.8f;
            root.AddComponent<EnemyMotor>();
            var projectile=AssetDatabase.FindAssets("t:GameObject",new[]{"Assets/_UKUBOLA/Generated/Phase05"}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault(p=>p.GetComponent<Projectile>());
            if(!projectile)projectile=AssetDatabase.LoadAssetAtPath<GameObject>(UkubolaCombatSetup.ProjectilePath);
            root.AddComponent<EnemyAttack>().Configure(projectile.GetComponent<Projectile>(),LayerMask.GetMask("World","Player","Enemy"));
            var model=Art.Character(root.transform,name);root.AddComponent<EnemyModelView>().Configure(model.GetComponent<Animation>());
            var brain=root.AddComponent<EnemyBrain>();brain.SetDefinition(definition);brain.Bind(player,viewer,noise,nav);root.SetActive(true);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.layer;
        }
        private static void Pickup(Transform parent,string name,Vector3 position)
        {
            var definition=AssetDatabase.FindAssets(name+" t:PickupDefinition").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<PickupDefinition>).First(d=>d.Sprite&&d.Sprite.name.Replace("_Pickup","")==name);
            var root=new GameObject(name);root.layer=LayerMask.NameToLayer("Pickup");root.transform.SetParent(parent,false);root.transform.position=position;
            var pickup=root.AddComponent<Pickup>();pickup.Configure(definition,null);var body=root.GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;var trigger=root.GetComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.5f;trigger.center=Vector3.up*.4f;
            var model=new GameObject("Model").transform;model.SetParent(root.transform,false);model.localPosition=Vector3.up*.4f;
            if(name=="Medikit")
            {
                var white=Art.Solid("Medical White",new Color(.8f,.83f,.78f),.1f,.45f);var red=Art.Solid("Medical Red",new Color(.75f,.04f,.03f),.1f,.4f);
                Art.Box(model,"Case",Vector3.zero,new Vector3(.7f,.43f,.25f),white);Art.Box(model,"CrossVertical",new Vector3(0,0,-.14f),new Vector3(.12f,.29f,.025f),red);Art.Box(model,"CrossHorizontal",new Vector3(0,0,-.14f),new Vector3(.32f,.11f,.025f),red);
            }
            else
            {
                var metal=Art.Surface("Armor");var blue=Art.Solid(name+" Color",name=="BlueCard"?new Color(.025f,.2f,.85f):new Color(.7f,.4f,.12f),.4f,.6f,.5f);
                Art.Box(model,"Body",Vector3.zero,new Vector3(.56f,.3f,.19f),blue);Art.Box(model,"Inset",new Vector3(0,0,-.11f),new Vector3(.36f,.15f,.03f),metal);
            }
            root.AddComponent<PickupPresentation>().Configure(pickup,model);
        }
        public static void ValidateArtRoom()
        {
            var display=Find<ModernDisplay>();if(display.Length!=1||!display[0].Profile)throw new InvalidOperationException("Missing modern presentation.");
            if(Find<EnemyModelView>().Length!=2||Find<EnemyModelView>().Any(v=>!v.IsValid))throw new InvalidOperationException("Character animation validation failed.");
            foreach(var renderer in Find<MeshRenderer>())if(!renderer.sharedMaterial||!renderer.sharedMaterial.shader.isSupported)throw new InvalidOperationException("Invalid scene material: "+renderer.name);
            foreach(var t in Find<Transform>())if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0)throw new InvalidOperationException("Missing script: "+t.name);
        }
    }
}
