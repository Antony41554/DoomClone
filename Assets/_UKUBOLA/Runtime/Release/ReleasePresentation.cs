using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Ukubola.UI;
using Ukubola.World;
using Ukubola.Rendering;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
using Ukubola.Persistence;

namespace Ukubola.Release
{
    // Optional presentation layer. The four P14 scene paths, IDs, geometry, gameplay and save format stay unchanged.
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class ReleasePresentation : MonoBehaviour
    {
        [SerializeField] private ReleaseArt art;
        [SerializeField] private int stage=-1;
        public ReleaseArt Art=>art;
        public int Stage=>stage;
        public static ReleasePresentation Active {get;private set;}
        private MenuFlow flow;
        private Image powerTint;
        private Powerup powers;
        private RetroDisplay retro;
        private Camera cameraView;
        private GameObject visualRoot;
        private Mesh cube,quad,cylinder;
        private readonly List<Material> materials=new List<Material>();
        private readonly Dictionary<Renderer,Material> oldMaterials=new Dictionary<Renderer,Material>();
        private readonly Dictionary<MeshFilter,Mesh> oldMeshes=new Dictionary<MeshFilter,Mesh>();
        private ReleaseDecoration decor;
        private ReleaseEffects effects;
        private RenderTexture rendered;
        private UniversalAdditionalCameraData cameraData;
        [SerializeField] private Volume volume;
        public Volume PostVolume=>volume;
        public GameObject PresentationRoot=>visualRoot;
        private bool ready,oldHdr,oldPost;
        private LayerMask oldVolumeMask;
        private int quality;
        private Color accent;
        private readonly Vector4[] lights=new Vector4[8],colors=new Vector4[8];
        private readonly ReleaseDecoration.Fixture[] selectedLights=new ReleaseDecoration.Fixture[8];
        private readonly float[] selectedDistances=new float[8];
        private Vector3 flashPosition;
        private Color flashColor;
        private float flashUntil,flashDuration,flashRange,flashStrength;
        private float nextLights;
        private bool subscribed;
        public int ParticleCount=>effects==null?0:effects.ActiveCount;
        public int DecorationCount=>decor==null?0:decor.Count;
        public int Quality=>quality;
        public bool Ready=>ready;
        public Vector2Int ActualResolution=>cameraView&&cameraView.targetTexture?new Vector2Int(cameraView.targetTexture.width,cameraView.targetTexture.height):Vector2Int.zero;
        public void Configure(ReleaseArt value,int index,Volume post){art=value;stage=index;volume=post;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetStatics(){Active=null;}
        private void Awake()
        {
            flow=GetComponent<MenuFlow>();if(!flow||!art||!art.Surface||!art.Particle||!art.Post||art.Music==null||art.Music.Length!=3){Debug.LogWarning("[UKUBOLA P15] Visual art unavailable; original presentation retained.",this);enabled=false;return;}
            Active=this;quality=ReleaseQuality.Read();
            if(stage<0){if(flow.TitleMusic&&art.Music[0])flow.TitleMusic.clip=art.Music[0];ready=true;return;}
            if(stage>=0&&stage<3&&flow.Hud&&flow.Hud.Audio&&art.Music[stage])
            {var audio=flow.Hud.Audio;audio.Music.clip=art.Music[stage];if(art.Effects!=null&&art.Effects.Length==16&&art.Weapons!=null&&art.Weapons.Length==8){audio.Configure(audio.World,audio.Stats,audio.Music,audio.Local,audio.Spatial,art.Effects);audio.ConfigureAdvancedClips(art.Weapons);}}
        }
        private T[] Find<T>()where T:Component=>gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
        private Material Surface(string texture,Color tint,float emission=0,float metal=.25f)
        {
            var m=new Material(art.Surface){name="P15 "+texture};m.SetTexture("_BaseMap",art.Texture(texture)??Texture2D.whiteTexture);m.SetColor("_BaseColor",tint);m.SetFloat("_Emission",emission);m.SetFloat("_Metal",metal);materials.Add(m);return m;
        }
        private Material Particle(string texture,Color tint)
        {var m=new Material(art.Particle){name="P15 "+texture};m.SetTexture("_BaseMap",art.Texture(texture));m.SetColor("_BaseColor",tint);materials.Add(m);return m;}
        private Material StageMaterial(string name,Color tint,float emission=0,float metal=.25f)=>Surface(stage+"_"+name,tint,emission,metal);
        private void Assign(Renderer r,Material mat){if(!oldMaterials.ContainsKey(r))oldMaterials[r]=r.sharedMaterial;r.sharedMaterial=mat;}
        private void SetRenderer(Transform t,Material mat){if(!t)throw new InvalidOperationException("Missing geometry group");foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))Assign(r,mat);}
        private void RestoreGeometry(){foreach(var kv in oldMaterials)if(kv.Key)kv.Key.sharedMaterial=kv.Value;foreach(var kv in oldMeshes)if(kv.Key)kv.Key.sharedMesh=kv.Value;}

        private void Start()
        {
            if(stage<0)return;
            // Assets are checked by the editor. Failure here is non-fatal for gameplay.
            try{SetupMission();ready=true;Debug.Log("[UKUBOLA P15] RETRO MODERN READY | "+ReleaseQuality.Label(quality)+" | "+ActualResolution+" | props "+DecorationCount,this);}
            catch(Exception ex){Debug.LogWarning("[UKUBOLA P15] Presentation initialization incomplete: "+ex,this);if(effects!=null)effects.Clear();RestoreRenderTarget();RestoreGeometry();if(volume)volume.enabled=false;if(visualRoot)visualRoot.SetActive(false);ready=false;}
        }
        private void SetupMission()
        {
            retro=Find<RetroDisplay>().Single();cameraView=retro.WorldCamera;cameraData=cameraView.GetUniversalAdditionalCameraData();oldHdr=cameraView.allowHDR;oldPost=cameraData.renderPostProcessing;oldVolumeMask=cameraData.volumeLayerMask;
            var hud=flow.Hud;var w=hud.World;var source=w.Source;var map=SaveWorld.Map(hud).Level;
            accent=stage==0?new Color(.25f,.85f,.73f):stage==1?new Color(1f,.43f,.13f):new Color(.95f,.22f,.37f);
            var rootScene=gameObject.scene.GetRootGameObjects().Single(g=>g.name=="Level Geometry");
            var floor=StageMaterial("Floor",Color.white,0,.34f);var ceiling=StageMaterial("Ceiling",new Color(.77f,.83f,.84f),0,.10f);var wall=StageMaterial("Wall",Color.white,0,.32f);
            SetRenderer(rootScene.transform.Find("Floors"),floor);SetRenderer(rootScene.transform.Find("Ceilings"),ceiling);SetRenderer(rootScene.transform.Find("Walls"),wall);
            var normal=StageMaterial("Door",Color.white,.08f,.42f);var blue=StageMaterial("BlueDoor",Color.white,.13f,.42f);var remote=StageMaterial("RemoteDoor",Color.white,.13f,.42f);
            foreach(var d in w.Doors){var mat=d.name.IndexOf("Secret",StringComparison.OrdinalIgnoreCase)>=0?wall:d.SwitchOnly?remote:d.Required!=KeyId.None?blue:normal;SetRenderer(d.Panel,mat);}
            // Keep the secret panel indistinguishable from its surrounding wall.
            foreach(var r in Find<MeshRenderer>())if(r.transform.parent&&r.transform.parent.name.IndexOf("Secret",StringComparison.OrdinalIgnoreCase)>=0&&r.name=="Solid")Assign(r,wall);
            var switchMat=StageMaterial("Switch",Color.white,.10f,.3f);foreach(var sw in w.Switches)SetRenderer(sw.transform,switchMat);
            SetRenderer(w.Lift.Platform,StageMaterial("Lift",Color.white,.02f,.5f));SetRenderer(w.Zone.transform,StageMaterial("Hazard",Color.white,.60f,0));
            var teleport=StageMaterial("Teleport",Color.white,1.9f,.1f);SetRenderer(w.Teleport.transform,teleport);SetRenderer(w.Teleport.Destination,teleport);
            cylinder=ReleaseDecoration.CylinderMesh();var barrelMat=StageMaterial("Barrel",Color.white,.01f,.35f);
            foreach(var b in w.Barrels){SetRenderer(b.transform,barrelMat);var mf=b.GetComponent<MeshFilter>();if(mf){oldMeshes[mf]=mf.sharedMesh;mf.sharedMesh=cylinder;}b.GetComponent<Health>().Died+=BarrelDeath;}
            foreach(var p in source.Pickups){var key="Item_"+p.Definition.Sprite.name.Replace("_Pickup","");var sprite=art.Sprite(key);if(sprite)p.Visual.sprite=sprite;p.Taken+=PickupEffect;}
            foreach(var e in source.Enemies){string n=e.Definition.name;var view=e.GetComponent<EnemySpriteView>();var idle=Enumerable.Range(0,8).Select(i=>art.Sprite(n+"_Idle_"+i)).ToArray();var walk=Enumerable.Range(0,32).Select(i=>art.Sprite(n+"_Walk_"+(i/4)+"_"+(i%4))).ToArray();var attack=Enumerable.Range(0,32).Select(i=>art.Sprite(n+"_Attack_"+(i/4)+"_"+(i%4))).ToArray();var pain=Enumerable.Range(0,16).Select(i=>art.Sprite(n+"_Pain_"+(i/2)+"_"+(i%2))).ToArray();var dead=Enumerable.Range(0,32).Select(i=>art.Sprite(n+"_Dead_"+(i/4)+"_"+(i%4))).ToArray();view.SetReleaseFrames(idle,walk,attack,pain,dead);}
            var faces=Enumerable.Range(0,16).Select(i=>art.Texture("Face_"+i.ToString("00"))).ToArray();if(faces.All(x=>x))hud.Configure(w,hud.Stats,hud.Audio,hud.Labels,hud.Face,hud.Flash,faces);
            var bar=hud.GetComponent<RawImage>();if(bar)bar.texture=art.Texture("StatusBar");else{var target=Find<RawImage>().FirstOrDefault(x=>x.name=="Classic Status Bar");if(target)target.texture=art.Texture("StatusBar");}
            foreach(var label in hud.Labels){if(!label)continue;label.fontStyle=FontStyle.Bold;}
            // Keep key color semantics; use warmer high-contrast digits for the three large counters.
            for(int i=0;i<3;i++)hud.Labels[i].color=new Color32(241,191,112,255);
            hud.Labels[3].color=new Color32(173,224,216,255);
            var tintGo=new GameObject("P15 Power Tint",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));var tintRect=(RectTransform)tintGo.transform;tintRect.SetParent(retro.Image.transform,false);tintRect.SetAsFirstSibling();tintRect.anchorMin=Vector2.zero;tintRect.anchorMax=Vector2.one;tintRect.offsetMin=tintRect.offsetMax=Vector2.zero;powerTint=tintGo.GetComponent<Image>();powerTint.raycastTarget=false;powerTint.color=Color.clear;powers=source.Player.GetComponent<Powerup>();
            var mapImage=SaveWorld.Map(hud).Map;mapImage.color=new Color(1,.97f,.94f,1);
            visualRoot=new GameObject("Phase15 Presentation (runtime only)");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visualRoot,gameObject.scene);visualRoot.layer=LayerMask.NameToLayer("World");
            cube=ReleaseDecoration.BoxMesh();quad=ReleaseDecoration.QuadMesh();
            var metal=StageMaterial("Ceiling",new Color(.53f,.6f,.62f),0,.6f);var glow=Surface("",accent,5f,0);var beam=Particle("Glow",new Color(accent.r,accent.g,accent.b,.13f));
            decor=new ReleaseDecoration(visualRoot.transform,cube,quad,wall,metal,glow,beam,accent);
            var avoid=new List<Vector3>();avoid.Add(source.Spawn.Position);avoid.AddRange(source.Enemies.Select(e=>e.transform.position));avoid.AddRange(source.Pickups.Select(p=>p.transform.position));avoid.AddRange(w.Doors.Select(d=>d.transform.position));avoid.AddRange(w.Switches.Select(s=>s.transform.position));avoid.AddRange(w.Barrels.Select(b=>b.transform.position));avoid.Add(w.Lift.transform.position);avoid.Add(w.Zone.transform.position);avoid.Add(w.Teleport.transform.position);avoid.Add(w.Teleport.Destination.position);avoid.Add(w.Secret.transform.position);avoid.AddRange(Find<LevelExit>().Select(x=>x.transform.position));
            decor.Build(map,avoid,stage);
            effects=new ReleaseEffects(visualRoot.transform,quad,Particle("Spark",Color.white),Particle("Smoke",Color.white));
            if(!volume)throw new InvalidOperationException("Missing release Volume; run Setup Final Release");
            Explosion.Occurred+=ExplosionEffect;subscribed=true;
            Shader.SetGlobalVector("_UK15Atmosphere",stage==0?new Vector4(.69f,.81f,.83f,0):stage==1?new Vector4(.85f,.68f,.54f,0):new Vector4(.69f,.61f,.82f,0));
            Shader.SetGlobalVector("_UK15Fog",stage==0?new Vector4(.035f,.065f,.07f,.030f):stage==1?new Vector4(.085f,.044f,.023f,.036f):new Vector4(.065f,.025f,.045f,.042f));
            ApplyQuality(quality);UpdateLights();
        }
        public void ApplyQuality(int value)
        {
            quality=ReleaseQuality.Normalize(value);if(!cameraView||!retro)return;effects?.SetLimit(ReleaseQuality.ParticleLimit(quality));
            RestoreRenderTarget();
            var resolution=ReleaseQuality.Size(quality);var urp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;bool hdr=quality>0&&urp&&urp.supportsHDR&&SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
            if(quality>0){var rt=new RenderTexture(resolution.x,resolution.y,24,hdr?RenderTextureFormat.ARGBHalf:RenderTextureFormat.ARGB32){name="P15 Runtime RT",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,antiAliasing=1,useMipMap=false,autoGenerateMips=false};
                if(rt.Create()){rendered=rt;cameraView.targetTexture=rt;retro.Image.texture=rt;}else{Destroy(rt);Debug.LogWarning("[UKUBOLA P15] Render texture creation failed; retained classic resolution.");}}
            cameraView.aspect=RetroDisplay.DisplayAspect;cameraView.allowHDR=hdr&&rendered;cameraData.renderPostProcessing=hdr&&rendered;cameraData.volumeLayerMask=oldVolumeMask.value|(1<<volume.gameObject.layer);if(volume)volume.enabled=hdr&&rendered;
            if(quality>0&&!hdr)Debug.LogWarning("[UKUBOLA P15] HDR not available; bloom disabled, other art retained.");nextLights=0;
        }
        private void RestoreRenderTarget()
        {
            if(cameraView&&retro){cameraView.targetTexture=retro.Target;retro.Image.texture=retro.Target;cameraView.aspect=RetroDisplay.DisplayAspect;cameraView.allowHDR=oldHdr;if(cameraData){cameraData.renderPostProcessing=oldPost;cameraData.volumeLayerMask=oldVolumeMask;}}
            if(rendered){rendered.Release();Destroy(rendered);rendered=null;}
        }
        private void UpdateLights()
        {
            int limit=ReleaseQuality.LightLimit(quality);Vector3 eye=cameraView.transform.position;for(int i=0;i<8;i++){selectedDistances[i]=float.PositiveInfinity;lights[i]=colors[i]=Vector4.zero;}
            foreach(var f in decor.Lights){float distance=(eye-f.Position).sqrMagnitude;for(int j=0;j<limit;j++){if(distance>=selectedDistances[j])continue;for(int k=limit-1;k>j;k--){selectedDistances[k]=selectedDistances[k-1];selectedLights[k]=selectedLights[k-1];}selectedDistances[j]=distance;selectedLights[j]=f;break;}}
            for(int i=0;i<limit;i++){if(float.IsInfinity(selectedDistances[i]))continue;var f=selectedLights[i];float flicker=.91f+.09f*Mathf.Sin(Time.time*3.4f+f.Phase);lights[i]=new Vector4(f.Position.x,f.Position.y,f.Position.z,f.Range);colors[i]=new Vector4(f.Color.r,f.Color.g,f.Color.b,f.Strength*flicker);}
            lights[0]=new Vector4(eye.x,eye.y,eye.z,6f);colors[0]=new Vector4(.70f,.83f,.9f,.55f);
            if(Time.time<flashUntil){float f=Mathf.Clamp01((flashUntil-Time.time)/flashDuration);int i=limit-1;lights[i]=new Vector4(flashPosition.x,flashPosition.y,flashPosition.z,flashRange);colors[i]=new Vector4(flashColor.r,flashColor.g,flashColor.b,flashStrength*f);}
            Shader.SetGlobalVectorArray("_UK15Lights",lights);Shader.SetGlobalVectorArray("_UK15Colors",colors);
        }
        private void LateUpdate(){if(!ready||stage<0||!cameraView)return;if(powerTint&&powers)powerTint.color=powers.Invulnerable?new Color(.12f,.80f,.67f,.075f):powers.Berserk?new Color(.88f,.16f,.13f,.075f):Color.clear;if(Time.timeScale>0){effects.Update(cameraView,Mathf.Min(Time.deltaTime,.05f));if(Time.time>=nextLights){UpdateLights();nextLights=Time.time+.08f;}}else effects.Update(cameraView,0);}
        private void Flash(Vector3 p,Color c,float range,float strength,float duration){flashPosition=p;flashColor=c;flashRange=range;flashStrength=strength;flashDuration=duration;flashUntil=Time.time+duration;nextLights=0;}
        private void Burst(Vector3 p,Color c,float size){if(!ready||Time.timeScale<=0||effects==null)return;effects.Burst(p,c,size);Flash(p,c,4.5f,4.4f,.4f);}
        private void ExplosionEffect(Vector3 p)=>Burst(p,new Color(1f,.47f,.14f),1.2f);
        private void BarrelDeath(Health hp,DamageInfo _)=>ExplosionEffect(hp.transform.position);
        private void PickupEffect(Pickup p)=>Burst(p.transform.position+Vector3.up*.5f,new Color(.32f,1f,.72f),.25f);
        public static void Shot(WeaponController weapon,Vector3 origin){var r=Active;if(!r||!r.ready||r.stage<0||!weapon.Selected||weapon.Selected.Mode==WeaponMode.Melee)return;r.Flash(origin+weapon.AimCamera.transform.forward*.65f,new Color(1f,.57f,.19f),3.5f,2.8f,.095f);}
        public static void Impact(Vector3 point,Vector3 normal,bool living){var r=Active;if(!r||!r.ready||r.effects==null||Time.timeScale<=0)return;r.effects.Impact(point,normal,living?new Color(.80f,.20f,.14f,.95f):new Color(2.2f,1.45f,.43f,1f),living);}
        public static void Teleported(Vector3 position){var r=Active;if(r)r.Burst(position+Vector3.up*.6f,new Color(.15f,.9f,1f),.7f);}
        public static void ProjectileTrail(Vector3 p,Color tint){var r=Active;if(r&&r.ready&&r.effects!=null&&Time.timeScale>0)r.effects.Trail(p,tint);}
        private void OnDestroy()
        {
            if(subscribed)Explosion.Occurred-=ExplosionEffect;
            if(volume)volume.enabled=false;
            if(flow&&flow.Hud){var w=flow.Hud.World;if(w){foreach(var b in w.Barrels)if(b){var h=b.GetComponent<Health>();if(h)h.Died-=BarrelDeath;}foreach(var p in w.Source.Pickups)if(p)p.Taken-=PickupEffect;}}
            effects?.Dispose();RestoreRenderTarget();RestoreGeometry();if(visualRoot)Destroy(visualRoot);foreach(var m in materials)if(m)Destroy(m);if(cube)Destroy(cube);if(quad)Destroy(quad);if(cylinder)Destroy(cylinder);
            if(Active==this){Active=null;Shader.SetGlobalVectorArray("_UK15Lights",new Vector4[8]);Shader.SetGlobalVectorArray("_UK15Colors",new Vector4[8]);}
        }
    }
}
