using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Persistence;
using Ukubola.Player;
using Ukubola.Rendering;
using Ukubola.World;
using Art = Ukubola.EditorTools.UkubolaEvolutionArt;
using Object = UnityEngine.Object;

namespace Ukubola.EditorTools
{
    // Candidate partial: activate alongside the original partial declaration.
    public static partial class UkubolaEvolutionScene
    {
        public const string SlicePath = "Assets/_UKUBOLA/Scenes/Evolution/OutpostSlice.unity";
        private const string SliceMission = "outpost-slice";
        private const string SliceStamp = Art.Root + "/OutpostSlice.scene.sha256.txt";
        private static readonly List<MissionPlacement> slicePlacements = new List<MissionPlacement>();
        private static MissionDefinition sliceDefinition;
        private static Transform sliceGameplay;
        private static void PreserveHudPixelAspect(Canvas canvas)
        {
            foreach(var image in canvas.GetComponentsInChildren<RawImage>(true).Where(i=>i.texture&&(i.name.StartsWith("Digit",StringComparison.Ordinal)||i.name=="Portrait")))
            {
                var rect=image.rectTransform;var parent=rect.parent;int sibling=rect.GetSiblingIndex();
                var slot=Rect(parent,image.name+" Slot",rect.anchorMin,rect.anchorMax);slot.SetSiblingIndex(sibling);
                rect.SetParent(slot,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                fit.aspectRatio=image.name.StartsWith("Digit",StringComparison.Ordinal)?16f/24:image.texture.width/(float)image.texture.height;
            }
        }
        private static T Identity<T>(T entity, string key) where T : PersistentBehaviour
        {
            entity.ConfigureIdentity(SliceMission,key);
            slicePlacements.Add(new MissionPlacement{key=key,prefab=entity.StateKind,position=entity.transform.position,
                yaw=entity.transform.eulerAngles.y,sector=sliceDefinition.sectors.First(s=>s.Contains(entity.transform.position,.25f)).key});
            return entity;
        }
        private static ActionBinding Connect(string key,PersistentBehaviour source,WorldSignal signal,PersistentBehaviour destination,WorldVerb verb,string[] requirements=null,bool once=true)
        {
            var binding=Identity(new GameObject(key).AddComponent<ActionBinding>(),key);
            binding.transform.SetParent(sliceGameplay,false);
            binding.Configure(source,signal,once,new[]{new WorldAction{target=destination,verb=verb,requiredFlags=requirements??Array.Empty<string>()}});
            return binding;
        }
        private static WorldRelay Console(string key,Vector3 position,string label)
        {
            var root=new GameObject(key);root.transform.SetParent(sliceGameplay,false);root.transform.position=position;
            root.layer=world;var relay=Identity(root.AddComponent<WorldRelay>(),key);relay.Configure(false,true,true);
            Art.Box(root.transform,"Terminal body",Vector3.up*.9f,new Vector3(.9f,1.8f,.6f),Art.Surface("0_Console"),true);
            var screen=Art.Box(root.transform,"Display",new Vector3(0,1.3f,-.32f),new Vector3(.65f,.48f,.03f),Art.Solid("ConsoleCyan",new Color(.03f,.6f,.8f),.1f,.4f,2));
            var handle=new GameObject("Control lever").transform;handle.SetParent(root.transform,false);handle.localPosition=new Vector3(.28f,.9f,-.38f);
            Art.Box(handle,"Lever handle",new Vector3(0,.1f,0),new Vector3(.12f,.3f,.12f),Art.Solid("JointMetal",new Color(.08f,.105f,.11f),.55f,.4f));
            root.AddComponent<WorldRelayView>().Configure(relay,screen.GetComponent<Renderer>(),handle);
            Label(root.transform,label,new Vector3(0,2.1f,-.1f),0,.045f,Color.white);
            return relay;
        }
        private static WorldMover SliceDoor(string key,Vector3 position,float width,KeyId required=KeyId.None,bool remoteOnly=false)
        {
            var root=new GameObject(key);root.transform.SetParent(sliceGameplay,false);root.transform.position=position;
            var panel=Art.Box(root.transform,"Rigid door panel",new Vector3(0,1.8f,0),new Vector3(width,3.6f,.28f),Art.Surface("Door"),true);
            var obstacle=root.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=Vector3.up*1.8f;obstacle.size=new Vector3(width,3.6f,.5f);obstacle.carving=true;obstacle.carveOnlyStationary=false;
            var mover=Identity(root.AddComponent<WorldMover>(),key);mover.Configure(panel.transform,panel.GetComponent<BoxCollider>(),new[]{new Vector3(0,1.8f,0),new Vector3(0,5.5f,0)},2.2f,false,true,required,0,obstacle,remoteOnly);
            foreach(float side in new[]{-1f,1f})Art.Box(root.transform,"Access light",new Vector3(side*(width*.5f+.12f),1.8f,-.2f),new Vector3(.12f,3.6f,.1f),Art.Solid(required==KeyId.None?"DoorAmber":"DoorBlue",required==KeyId.None?new Color(1,.3f,.025f):new Color(.02f,.25f,1),.1f,.4f,3));
            if(required!=KeyId.None)Label(root.transform,"BLUE  /  [ I ]",new Vector3(0,3.9f,-.25f),0,.055f,new Color(.2f,.55f,1));
            return mover;
        }
        private static void Partition(Transform geometry,float z,float width,Material wall)
        {
            float gap=width*.5f;
            foreach(float side in new[]{-1f,1f})Art.Box(geometry,"Sector bulkhead",new Vector3(side*(9+gap)*.5f,2.8f,z),new Vector3(9-gap,5.6f,.4f),wall,true);
            Art.Box(geometry,"Door lintel",new Vector3(0,4.7f,z),new Vector3(width,1.8f,.45f),Art.Surface("0_Trim"),true);
        }
        private static WorldPickupState SlicePickup(string key,string name,Vector3 position)
        {
            Pickup(sliceGameplay,name,position);
            var item=sliceGameplay.GetComponentsInChildren<Pickup>().Single(p=>p.name==name&&p.transform.position==position);
            var model=item.transform.Find("Model");
            if(name!="Medikit")
            {
                foreach(Transform old in model.Cast<Transform>().ToArray())Object.DestroyImmediate(old.gameObject);
                var dark=Art.Solid("JointMetal",new Color(.08f,.105f,.11f),.55f,.4f);
                if(name=="Shotgun")
                {
                    var iron=Art.Surface("Weapon");Art.Box(model,"Receiver",new Vector3(-.12f,0,0),new Vector3(.3f,.15f,.12f),iron);
                    Art.Tube(model,"Barrel",new[]{new Vector3(-.1f,.05f,0),new Vector3(.5f,.05f,0)},.035f,dark,8);
                    Art.Box(model,"Stock",new Vector3(-.4f,-.035f,0),new Vector3(.3f,.12f,.15f),dark);Art.Box(model,"Grip",new Vector3(-.15f,-.12f,0),new Vector3(.08f,.2f,.1f),dark);
                }
                else if(name=="GreenArmor")
                {
                    var armor=Art.Surface("Armor",new Color(.5f,.9f,.6f));Art.Box(model,"Vest",Vector3.zero,new Vector3(.55f,.55f,.25f),armor);
                    foreach(float side in new[]{-1f,1f})Art.Box(model,"Shoulder plate",new Vector3(side*.32f,.17f,0),new Vector3(.18f,.2f,.3f),armor);
                    Art.Box(model,"Vest plate",new Vector3(0,.04f,-.14f),new Vector3(.36f,.27f,.04f),dark);
                }
                else if(name=="BlueCard")
                {
                    var blue=Art.Solid("CardBlue",new Color(.02f,.25f,.9f),.2f,.4f,.5f);var yellow=Art.Solid("CardContact",new Color(.9f,.6f,.08f),.8f,.6f);
                    Art.Box(model,"Access card",Vector3.zero,new Vector3(.56f,.3f,.06f),blue);Art.Box(model,"Card symbol I",new Vector3(-.06f,0,-.04f),new Vector3(.05f,.18f,.025f),yellow);
                    Art.Box(model,"Contact chip",new Vector3(.2f,-.08f,-.04f),new Vector3(.1f,.1f,.025f),yellow);
                }
                else if(name=="Backpack")
                {
                    var fabric=Art.Solid("BackpackFabric",new Color(.3f,.38f,.2f),0,.2f);Art.Box(model,"Pack",Vector3.zero,new Vector3(.45f,.58f,.3f),fabric);
                    foreach(float side in new[]{-1f,1f})Art.Box(model,"Strap",new Vector3(side*.14f,0,-.17f),new Vector3(.06f,.5f,.03f),dark);
                    Art.Box(model,"Pocket",new Vector3(0,-.14f,-.2f),new Vector3(.25f,.2f,.12f),fabric);
                }
                else
                {
                    Art.Box(model,"Ammunition tray",new Vector3(0,-.08f,0),new Vector3(.5f,.16f,.24f),dark);
                    var shell=Art.Solid("ShotgunShell",new Color(.65f,.06f,.03f),.15f,.3f);var cap=Art.Solid("ShellBrass",new Color(.8f,.5f,.08f),.8f,.65f);
                    for(int i=0;i<4;i++){float x=-.18f+i*.12f;Art.Tube(model,"Shell",new[]{new Vector3(x,-.04f,0),new Vector3(x,.2f,0)},.045f,shell,8);Art.Box(model,"Brass cap",new Vector3(x,-.03f,0),new Vector3(.1f,.06f,.1f),cap);}
                }
            }
            return Identity(item.gameObject.AddComponent<WorldPickupState>(),key);
        }
        private static WorldActorState SliceEnemy(string key,string type,Vector3 position,Health player,Camera viewer,NoiseEmitter noise,EnemyNavigation navigation)
        {
            Enemy(sliceGameplay,type,position,player,viewer,noise,navigation);
            var brain=sliceGameplay.GetComponentsInChildren<EnemyBrain>().Single(b=>b.transform.position==position);
            brain.gameObject.name=key;
            brain.gameObject.AddComponent<EnemyFaction>().Configure(type=="Guard"?1:2);
            var state=Identity(brain.gameObject.AddComponent<WorldActorState>(),key);state.Configure(brain);return state;
        }
        private static NavMeshData BakeSliceNavigation(Transform geometry)
        {
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.35f;settings.agentHeight=1.7f;settings.agentClimb=.25f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.1f;
            var sources=geometry.GetComponentsInChildren<BoxCollider>().Select(c=>new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=c.transform.localToWorldMatrix*Matrix4x4.Translate(c.center),size=c.size,area=0}).ToList();
            var bounds=new Bounds(new Vector3(0,2,26),new Vector3(24,14,58));string path=Art.Root+"/OutpostSliceNav.asset";var data=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if(!data){data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!data)throw new InvalidOperationException("Slice navigation bake failed.");AssetDatabase.CreateAsset(data,path);}
            else{if(!NavMeshBuilder.UpdateNavMeshData(data,settings,sources,bounds))throw new InvalidOperationException("Slice navigation update failed.");EditorUtility.SetDirty(data);}
            return data;
        }
        public static string BuildOutpostSlice()
        {
            EvolutionManifest.Validate();
            if(File.Exists(SlicePath)&&(!File.Exists(SliceStamp)||File.ReadAllText(SliceStamp).Trim()!=UkubolaAutomation.HashFile(SlicePath)))throw new InvalidOperationException("Unrecorded slice changes are protected.");
            var previous=EditorSceneManager.GetSceneManagerSetup();string staging=Art.Root+"/OutpostSlice.staging.unity";
            try
            {
                var room=EditorSceneManager.OpenScene(RoomPath);if(!EditorSceneManager.SaveScene(room,staging,true))throw new IOException("Cannot stage slice.");
                var scene=EditorSceneManager.OpenScene(staging);world=Layer("World");ui=Layer("UI");viewmodel=Layer("ViewModel");
                // Detach the copied room's bake before making any new lighting data.
                Lightmapping.SetLightingDataAssetForScene(scene,null);Lightmapping.Clear();
                var display=Find<RetroDisplay>().Single();var player=display.WorldCamera.transform.root.GetComponent<Health>();var guns=player.GetComponent<WeaponController>();
                PreserveHudPixelAspect(display.Canvas);
                var geometry=scene.GetRootGameObjects().Single(r=>r.name=="Geometry").transform;
                var props=scene.GetRootGameObjects().Single(r=>r.name=="Presentation").transform;
                foreach(var rail in geometry.Cast<Transform>().Where(t=>t.name=="Walkway Railing"))
                    if(!rail.GetComponent<BoxCollider>())rail.gameObject.AddComponent<BoxCollider>().size=rail.GetComponent<MeshFilter>().sharedMesh.bounds.size;
                foreach(var old in geometry.Cast<Transform>().Where(t=>t.name=="Access Door").ToArray())Object.DestroyImmediate(old.gameObject);
                foreach(var old in props.Cast<Transform>().Where(t=>t.name=="Blue Door Strip"||t.name=="Access Reader"||t.name=="Reader Screen"||t.GetComponent<TextMesh>()&&t.GetComponent<TextMesh>().text=="UKUBOLA").ToArray())Object.DestroyImmediate(old.gameObject);
                foreach(var brain in Find<EnemyBrain>())Object.DestroyImmediate(brain.gameObject);
                foreach(var item in Find<Pickup>())Object.DestroyImmediate(item.gameObject);
                foreach(var t in geometry.Cast<Transform>().Where(t=>t.name=="Hall_End").ToArray())Object.DestroyImmediate(t.gameObject);
                sliceGameplay=new GameObject("Gameplay").transform;slicePlacements.Clear();
                string missionPath=Art.Root+"/OutpostSlice.asset";sliceDefinition=AssetDatabase.LoadAssetAtPath<MissionDefinition>(missionPath);
                if(!sliceDefinition){sliceDefinition=ScriptableObject.CreateInstance<MissionDefinition>();AssetDatabase.CreateAsset(sliceDefinition,missionPath);}
                sliceDefinition.missionId=SliceMission;sliceDefinition.label="The Outpost";sliceDefinition.revision=1;sliceDefinition.sequence=0;sliceDefinition.spawn=new Vector3(0,.03f,2.5f);
                sliceDefinition.sectors=new[]{
                    new MissionSector{key="reception",label="Reception",center=new Vector2(0,4.5f),size=new Vector2(18,9),ceiling=5.6f},
                    new MissionSector{key="warehouse",label="Warehouse",center=new Vector2(0,14.5f),size=new Vector2(18,11),ceiling=5.6f},
                    new MissionSector{key="workshop",label="Maintenance",center=new Vector2(0,24),size=new Vector2(18,8),ceiling=5.6f},
                    new MissionSector{key="depot",label="Coolant Depot",center=new Vector2(0,35.5f),size=new Vector2(18,15),floor=-.6f,ceiling=5.6f},
                    new MissionSector{key="control",label="Control",center=new Vector2(0,47),size=new Vector2(18,8),ceiling=5.6f},
                    new MissionSector{key="catwalk",label="Service Catwalk",center=new Vector2(-7,21),size=new Vector2(3.2f,9.5f),floor=2.425f,ceiling=5.6f,plant=1},
                    new MissionSector{key="lift-landing",label="Upper Workshop",center=new Vector2(6,26.6f),size=new Vector2(2.8f,3),floor=2.425f,ceiling=5.6f,plant=1}};
                sliceDefinition.portals=new[]{
                    new MissionPortal{key="entry",from="reception",to="warehouse",position=new Vector3(0,0,9)},
                    new MissionPortal{key="storage",from="warehouse",to="workshop",position=new Vector3(0,0,20)},
                    new MissionPortal{key="transfer",from="workshop",to="depot",position=new Vector3(0,0,28),width=4.8f},
                    new MissionPortal{key="authorization",from="depot",to="control",position=new Vector3(0,0,43),requiredFlag="blue_access"},
                    new MissionPortal{key="stair",from="warehouse",to="catwalk",position=new Vector3(-7,2.4f,16)},
                    new MissionPortal{key="cargo-lift",from="workshop",to="lift-landing",position=new Vector3(6,2.425f,25.2f),width=2.8f}};
                sliceDefinition.objectives=new[]{"power_online","coolant_drained","transfer_secured"};
                foreach(var probe in Find<ReflectionProbe>())Object.DestroyImmediate(probe.gameObject);
                foreach(var sector in sliceDefinition.sectors.Where(s=>s.plant==0))
                {
                    var probe=new GameObject("Reflection_"+sector.key).AddComponent<ReflectionProbe>();probe.transform.position=new Vector3(sector.center.x,2.5f,sector.center.y);
                    probe.size=new Vector3(sector.size.x+1,6,sector.size.y+1);probe.boxProjection=true;probe.blendDistance=.5f;probe.resolution=128;probe.mode=ReflectionProbeMode.Baked;
                    probe.cullingMask=1<<world;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.025f,.04f,.05f);
                }
                Find<LightProbeGroup>().Single().probePositions=Enumerable.Range(0,13).SelectMany(z=>new[]{-6f,0,6f}.SelectMany(x=>new[]{new Vector3(x,1.5f,z*4+2),new Vector3(x,3.6f,z*4+2)})).ToArray();
                var context=new GameObject("Mission Services").AddComponent<MissionContext>();context.Configure(sliceDefinition);
                var wall=Art.Surface("0_Wall");var floor=Art.Surface("0_Floor");var trim=Art.Surface("0_Trim");
                Partition(geometry,9,3.2f,wall);Partition(geometry,43,3.2f,wall);Partition(geometry,49,3.2f,wall);
                Art.Box(geometry,"Depot base",new Vector3(0,-.78f,38.5f),new Vector3(18,.36f,9),floor,true);
                foreach(float side in new[]{-1f,1f})Art.Box(geometry,"Safe depot route",new Vector3(side*6,-.18f,38.5f),new Vector3(6,.36f,9),floor,true);
                Art.Box(geometry,"Depot approach",new Vector3(0,-.18f,35),new Vector3(18,.36f,2),floor,true);
                Art.Box(geometry,"Control floor",new Vector3(0,-.18f,47),new Vector3(18,.36f,8),floor,true);
                foreach(float z in new[]{36f,41.5f})for(int i=0;i<3;i++)Art.Box(geometry,"Coolant basin step",new Vector3(0,-.1f-i*.2f,z+(z<40?1:-1)*i*.3f),new Vector3(6,.2f,.3f),trim,true);
                Art.Box(geometry,"Depot ceiling",new Vector3(0,5.8f,42.5f),new Vector3(18,.3f,17),Art.Surface("0_Ceiling"),true);
                foreach(float side in new[]{-1f,1f})Art.Box(geometry,"Depot side",new Vector3(side*9.1f,2.8f,42.5f),new Vector3(.3f,5.6f,17),wall,true);
                Art.Box(geometry,"Control end",new Vector3(0,2.8f,51),new Vector3(18,5.6f,.3f),wall,true);
                for(int z=35;z<50;z+=6){var lamp=new GameObject("Depot illumination").AddComponent<Light>();lamp.transform.SetParent(props,false);lamp.transform.position=new Vector3(0,4,z);lamp.type=LightType.Point;lamp.range=11;lamp.intensity=18;lamp.color=z==41?new Color(1,.4f,.13f):new Color(.35f,.72f,1);lamp.lightmapBakeType=LightmapBakeType.Mixed;Art.Box(props,"Depot fixture",new Vector3(0,5.4f,z),new Vector3(2,.12f,.5f),Art.Solid("CyanLight",new Color(.03f,.6f,.85f),.1f,.75f,5));}
                var normal=SliceDoor("reception-door",new Vector3(0,0,9),3.2f);
                var remote=SliceDoor("transfer-door",new Vector3(0,0,28),4.8f,remoteOnly:true);
                var blue=SliceDoor("control-door",new Vector3(0,0,43),3.2f,KeyId.BlueCard);
                var final=SliceDoor("exit-door",new Vector3(0,0,49),3.2f,remoteOnly:true);
                var remoteSwitch=Console("transfer-switch",new Vector3(4,0,24),"TRANSFER ACCESS");Connect("remote-access",remoteSwitch,WorldSignal.Activated,remote,WorldVerb.On);
                var liftRoot=new GameObject("workshop-lift");liftRoot.transform.SetParent(sliceGameplay,false);liftRoot.transform.position=new Vector3(6,0,24);
                var liftPanel=Art.Box(liftRoot.transform,"Lift deck",new Vector3(0,-.125f,0),new Vector3(2.8f,.25f,2.8f),trim,true);
                var lift=Identity(liftRoot.AddComponent<WorldMover>(),"workshop-lift");lift.Configure(liftPanel.transform,liftPanel.GetComponent<BoxCollider>(),new[]{new Vector3(0,-.125f,0),new Vector3(0,2.3f,0)},1,true,true);
                Art.Box(geometry,"Lift upper landing",new Vector3(6,2.3f,26.8f),new Vector3(2.8f,.25f,3),trim,true);
                foreach(float side in new[]{-1f,1f})Art.Box(geometry,"Lift landing rail",new Vector3(6+side*1.35f,3.1f,26.8f),new Vector3(.09f,.09f,3),trim,true);
                Art.Box(geometry,"Catwalk end rail",new Vector3(-7,3.05f,25.5f),new Vector3(3.2f,.09f,.09f),trim,true);
                var lowerCall=Console("lift-lower-call",new Vector3(6,0,22.5f),"CALL / MOVE LIFT");lowerCall.Configure(false,true,false);
                var upperCall=Console("lift-upper-call",new Vector3(6,2.425f,26.2f),"CALL / MOVE LIFT");upperCall.Configure(false,true,false);
                Connect("lower-lift-command",lowerCall,WorldSignal.Activated,lift,WorldVerb.Toggle,once:false);Connect("upper-lift-command",upperCall,WorldSignal.Activated,lift,WorldVerb.Toggle,once:false);
                var basin=new GameObject("coolant-basin");basin.transform.SetParent(sliceGameplay,false);basin.transform.position=new Vector3(0,-.3f,39);basin.layer=world;
                var volume=basin.AddComponent<BoxCollider>();volume.size=new Vector3(6,.6f,4.6f);volume.isTrigger=true;
                var surface=Art.Box(basin.transform,"Coolant surface",new Vector3(0,.3f,0),new Vector3(6,.03f,4.6f),Art.Surface("Toxic",emission:.6f));
                var avoidance=basin.AddComponent<NavMeshObstacle>();avoidance.shape=NavMeshObstacleShape.Box;avoidance.size=new Vector3(6,2,4.6f);avoidance.carving=true;
                var toxic=Identity(basin.AddComponent<WorldHazard>(),"coolant-basin");toxic.Configure(WorldHazardKind.Toxic,volume,surface.transform,surface.GetComponent<Renderer>(),avoidance);
                var pump=Console("coolant-pump",new Vector3(-6,0,40),"DRAIN COOLANT");Connect("pump-drain",pump,WorldSignal.Activated,toxic,WorldVerb.Drain);
                var power=Console("power-generator",new Vector3(-5,0,46),"RESTORE SECTOR POWER");
                var flags=new Dictionary<string,WorldFlag>();foreach(string key in sliceDefinition.objectives){var flag=Identity(new GameObject(key).AddComponent<WorldFlag>(),key);flag.transform.SetParent(sliceGameplay,false);flag.Configure(key);flags.Add(key,flag);}
                Connect("power-objective",power,WorldSignal.Activated,flags["power_online"],WorldVerb.On);Connect("drain-objective",toxic,WorldSignal.Arrived,flags["coolant_drained"],WorldVerb.On);
                var navigation=Find<EnemyNavigation>().Single();navigation.Configure(BakeSliceNavigation(geometry));var noise=Find<NoiseEmitter>().Single();
                noise.gameObject.AddComponent<MissionAcoustics>().Configure(context,new[]{new AcousticLink{portal="entry",door=normal},new AcousticLink{portal="transfer",door=remote},new AcousticLink{portal="authorization",door=blue}});
                SliceEnemy("reception-guard","Guard",new Vector3(-2,0,7),player,display.WorldCamera,noise,navigation);
                SliceEnemy("warehouse-imp","Imp",new Vector3(3,0,17),player,display.WorldCamera,noise,navigation);
                SliceEnemy("workshop-guard","Guard",new Vector3(-3,0,25),player,display.WorldCamera,noise,navigation);
                SliceEnemy("depot-imp","Imp",new Vector3(5,0,37),player,display.WorldCamera,noise,navigation);
                SliceEnemy("depot-guard","Guard",new Vector3(-5,0,41),player,display.WorldCamera,noise,navigation);
                var members=new[]{SliceEnemy("control-guard","Guard",new Vector3(-2,0,47),player,display.WorldCamera,noise,navigation),SliceEnemy("control-imp","Imp",new Vector3(3,0,47),player,display.WorldCamera,noise,navigation)};
                var encounter=Identity(new GameObject("control-encounter").AddComponent<WorldEncounter>(),"control-encounter");encounter.transform.SetParent(sliceGameplay,false);encounter.Configure(members,false);
                var trigger=Identity(new GameObject("control-arrival").AddComponent<WorldTrigger>(),"control-arrival");trigger.transform.SetParent(sliceGameplay,false);trigger.transform.position=new Vector3(0,1,44);trigger.GetComponent<BoxCollider>().size=new Vector3(3,2,1);Connect("release-ambush",trigger,WorldSignal.Entered,encounter,WorldVerb.StartEncounter);
                Connect("encounter-objective",encounter,WorldSignal.Arrived,flags["transfer_secured"],WorldVerb.On);
                foreach(var flag in flags.Values)Connect("exit-after-"+flag.Key,flag,WorldSignal.Activated,final,WorldVerb.On,sliceDefinition.objectives);
                SlicePickup("entry-shotgun","Shotgun",new Vector3(1,0,4));SlicePickup("entry-shells","Shells",new Vector3(2,0,4));SlicePickup("entry-medikit","Medikit",new Vector3(-4,0,7));SlicePickup("warehouse-blue-card","BlueCard",new Vector3(6,0,15));SlicePickup("depot-shells","Shells",new Vector3(0,-.58f,39));
                SlicePickup("lift-armor","GreenArmor",new Vector3(6,2.45f,27.5f));SlicePickup("upper-shells","Shells",new Vector3(-7,2.45f,24));
                var grilleRoot=new GameObject("maintenance-grille");grilleRoot.transform.SetParent(sliceGameplay,false);grilleRoot.transform.position=new Vector3(-7,0,22);
                var grille=Art.Box(grilleRoot.transform,"Maintenance panel",Vector3.up*1.05f,new Vector3(2.8f,2.1f,.2f),wall,true);
                var grilleObstacle=grilleRoot.AddComponent<NavMeshObstacle>();grilleObstacle.shape=NavMeshObstacleShape.Box;grilleObstacle.center=Vector3.up*1.05f;grilleObstacle.size=new Vector3(2.8f,2.1f,.4f);grilleObstacle.carving=true;
                var secretDoor=Identity(grilleRoot.AddComponent<WorldMover>(),"maintenance-grille");secretDoor.Configure(grille.transform,grille.GetComponent<BoxCollider>(),new[]{Vector3.up*1.05f,new Vector3(-3.2f,1.05f,0)},1.5f,false,true,obstacle:grilleObstacle);
                for(int i=0;i<6;i++)Art.Box(grille.transform,"Vent grille",new Vector3(0,-.45f+i*.17f,-.12f),new Vector3(1.8f,.045f,.04f),Art.Solid("JointMetal",new Color(.08f,.105f,.11f),.55f,.4f));
                foreach(float side in new[]{-1f,1f})Art.Box(geometry,"Maintenance alcove side",new Vector3(-7+side*1.5f,1.05f,23.5f),new Vector3(.2f,2.1f,3),trim,true);
                Art.Box(geometry,"Maintenance alcove end",new Vector3(-7,1.05f,25),new Vector3(3.2f,2.1f,.2f),wall,true);
                var secretRoot=new GameObject("maintenance-secret");secretRoot.transform.SetParent(sliceGameplay,false);secretRoot.transform.position=new Vector3(-7,1,23.8f);
                var secret=Identity(secretRoot.AddComponent<WorldSecret>(),"maintenance-secret");secret.GetComponent<BoxCollider>().size=new Vector3(2.4f,2,1.8f);
                SlicePickup("secret-backpack","Backpack",new Vector3(-7,0,24));
                foreach(var location in new[]{new Vector3(-4,0,13),new Vector3(-2,0,14.5f),new Vector3(5,0,39)})
                {
                    string key=location.z==13?"storage-barrel-a":location.z==14.5f?"storage-barrel-b":"depot-barrel";
                    var barrel=Art.Box(sliceGameplay,key,location+Vector3.up*.65f,new Vector3(.9f,1.3f,.9f),Art.Surface("0_Crate"),true);
                    barrel.AddComponent<Health>().Configure(25,null);barrel.AddComponent<ExplosiveBarrel>().Configure(LayerMask.GetMask("World","Enemy","Player"));Identity(barrel.AddComponent<WorldBarrelState>(),key);
                    Art.Box(barrel.transform,"Hazard band",new Vector3(0,.15f,-.46f),new Vector3(.75f,.2f,.02f),Art.Solid("SafetyYellow",new Color(.8f,.48f,.07f),.25f,.4f));
                }
                var exit=Identity(new GameObject("transfer-exit").AddComponent<WorldExit>(),"transfer-exit");exit.transform.SetParent(sliceGameplay,false);exit.transform.position=new Vector3(0,1,50);exit.GetComponent<BoxCollider>().size=new Vector3(3,2,1.2f);exit.Configure(sliceDefinition.objectives);
                var saves=context.gameObject.AddComponent<WorldSaveController>();saves.Configure(context,player,guns,true);context.gameObject.AddComponent<WorldSession>().Configure(saves,exit);
                var cues=Enum.GetNames(typeof(Ukubola.Audio.Cue)).Select(c=>Art.Asset<AudioClip>("Assets/_UKUBOLA/Audio/Phase09/"+c+".wav")).ToArray();
                context.gameObject.AddComponent<WorldAudio>().Configure(context,player,guns,cues,Art.Asset<AudioClip>("Assets/_UKUBOLA/Audio/Phase09/MusicLoop.wav"));
                var identities=Find<PersistentBehaviour>().ToDictionary(e=>e.PersistentId);
                foreach(var placement in slicePlacements)
                {
                    var entity=identities[PersistentBehaviour.StableId(SliceMission,placement.key)];placement.position=entity.transform.position;placement.yaw=entity.transform.eulerAngles.y;
                    placement.sector=sliceDefinition.sectors.Where(s=>s.Contains(placement.position,.25f)).OrderByDescending(s=>s.floor).First().key;
                }
                sliceDefinition.placements=slicePlacements.ToArray();sliceDefinition.Validate();EditorUtility.SetDirty(sliceDefinition);context.RebuildRegistry();
                foreach(var t in Find<Transform>())if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0)throw new InvalidOperationException("Missing slice script: "+t.name);
                foreach(var renderer in geometry.GetComponentsInChildren<MeshRenderer>())GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.ReflectionProbeStatic|StaticEditorFlags.OccluderStatic);
                AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene,SlicePath))throw new IOException("Cannot save slice.");File.WriteAllText(SliceStamp,UkubolaAutomation.HashFile(SlicePath));EvolutionManifest.Record();AssetDatabase.ImportAsset(EvolutionManifest.Path);return SlicePath;
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(previous);if(File.Exists(staging))AssetDatabase.DeleteAsset(staging);}
        }
        public static string BakeOutpostSlice()
        {
            EvolutionManifest.Validate();
            if(!File.Exists(SlicePath)||!File.Exists(SliceStamp)||File.ReadAllText(SliceStamp).Trim()!=UkubolaAutomation.HashFile(SlicePath))throw new InvalidOperationException("Generate and validate the slice before baking.");
            var previous=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(SlicePath);
                var existing=Lightmapping.GetLightingDataAssetForScene(scene);
                if(existing&&!AssetDatabase.GetAssetPath(existing).StartsWith("Assets/_UKUBOLA/Scenes/Evolution/OutpostSlice/",StringComparison.Ordinal))throw new InvalidOperationException("Slice references another scene's lighting data; preserving it.");
                string settingsPath=Art.Root+"/OutpostSliceLighting.asset";var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
                if(!settings){settings=new LightingSettings();AssetDatabase.CreateAsset(settings,settingsPath);}
                settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;
                settings.lightmapResolution=8;settings.lightmapMaxSize=512;settings.directSampleCount=32;settings.indirectSampleCount=64;settings.environmentSampleCount=32;settings.maxBounces=2;settings.mixedBakeMode=MixedLightingMode.Shadowmask;
                Lightmapping.SetLightingSettingsForScene(scene,settings);EditorUtility.SetDirty(settings);
                if(!Lightmapping.Bake()||LightmapSettings.lightmaps.Length==0)throw new InvalidOperationException("Slice bake produced no lightmaps.");
                Art.Folder(Art.Root+"/SliceReflections");
                foreach(var probe in Find<ReflectionProbe>())
                {
                    string output=Art.Root+"/SliceReflections/"+probe.name+".exr";
                    if(!Lightmapping.BakeReflectionProbe(probe,output))throw new InvalidOperationException("Slice reflection bake failed.");
                    AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);probe.bakedTexture=Art.Asset<Cubemap>(output);
                }
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText(SliceStamp,UkubolaAutomation.HashFile(SlicePath));EvolutionManifest.Record();AssetDatabase.ImportAsset(EvolutionManifest.Path);
                return "Slice baked with "+LightmapSettings.lightmaps.Length+" lightmaps and "+Find<ReflectionProbe>().Length+" reflection probes.";
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }
    }
}
