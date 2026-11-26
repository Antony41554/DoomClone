using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
using Ukubola.Audio;
namespace Ukubola.UI
{
    [DefaultExecutionOrder(200)]
    public sealed class ClassicHud : MonoBehaviour
    {
        [SerializeField] private WorldDiagnostics world;
        [SerializeField] private LevelStats stats;
        [SerializeField] private GameAudio audio;
        [SerializeField] private Text ammo,health,armor,reserves,blue,yellow,red,notice,status;
        [SerializeField] private RawImage face;
        [SerializeField] private Image damageFlash;
        [SerializeField] private Texture2D[] faces;
        [SerializeField] private bool diagnosticsEnabled = true;
        public bool DiagnosticsEnabled { get => diagnosticsEnabled; set => diagnosticsEnabled = value; }
        private Health hp; private PlayerInventory bag; private WeaponController weapons;
        private float painUntil,fireUntil,messageUntil;
        private int shots; private bool found; private string interaction,result;
        public WorldDiagnostics World=>world; public LevelStats Stats=>stats; public GameAudio Audio=>audio;
        public RawImage Face=>face; public Texture2D[] Faces=>faces; public Image Flash=>damageFlash;
        public Text[] Labels=>new[]{ammo,health,armor,reserves,blue,yellow,red,notice,status};
        public void Configure(WorldDiagnostics w,LevelStats s,GameAudio a,Text[] labels,RawImage portrait,Image flash,Texture2D[] textures)
        {world=w;stats=s;audio=a;ammo=labels[0];health=labels[1];armor=labels[2];reserves=labels[3];blue=labels[4];yellow=labels[5];red=labels[6];notice=labels[7];status=labels[8];face=portrait;damageFlash=flash;faces=textures;}
        private void Awake(){hp=world.Source.Player;bag=hp.GetComponent<PlayerInventory>();weapons=world.Source.Weapons;}
        private void OnEnable(){hp.Damaged+=OnDamage;hp.Revived+=OnRevive;foreach(var p in world.Source.Pickups)p.Taken+=OnPickup;stats.OnCompleted+=OnComplete;}
        private void OnDisable(){if(hp){hp.Damaged-=OnDamage;hp.Revived-=OnRevive;}if(world)foreach(var p in world.Source.Pickups)if(p)p.Taken-=OnPickup;if(stats)stats.OnCompleted-=OnComplete;}
        private void Start(){Baseline();Show(diagnosticsEnabled?"UKUBOLA | E: USE / F8: AI":"UKUBOLA | E: USE / ESC: PAUSE",3f);}
        private void Baseline(){shots=weapons.Shots;found=world.Secret.Found;interaction=world.Interactor.LastMessage;result=weapons.LastResult;painUntil=fireUntil=0;}
        public void RefreshAfterLoad(){Baseline();Show("GAME LOADED");}
        private void Show(string value,float seconds=2.5f){notice.text=value;messageUntil=Time.unscaledTime+seconds;}
        private void OnDamage(Health _,DamageInfo d,DamageResult r){if(r.Applied){painUntil=Time.unscaledTime+.4f;if(r.Killed)Show(diagnosticsEnabled?"YOU DIED | F9: RESET":"YOU DIED",4f);}}
        private void OnRevive(Health _){painUntil=fireUntil=0;}
        private void OnPickup(Pickup p)=>Show("PICKED UP: "+p.Definition.DisplayName.ToUpperInvariant());
        private void OnComplete(LevelStats s)=>Show($"LEVEL COMPLETED | {s.ElapsedSeconds:F1}s",5f);
        private void LateUpdate()
        {
            float now=Time.unscaledTime; bool reset=diagnosticsEnabled&&Application.isFocused&&Keyboard.current!=null&&Keyboard.current.f9Key.wasPressedThisFrame;
            if(reset){Baseline();Show("RESET COMPLETE");}
            else{
                if(weapons.Shots>shots)fireUntil=now+.16f;shots=weapons.Shots;
                if(weapons.LastResult!=result){result=weapons.LastResult;if(result=="No ammo")Show("NO AMMO");}
                if(world.Interactor.LastMessage!=interaction){interaction=world.Interactor.LastMessage;Show(interaction.ToUpperInvariant());}
                if(world.Secret.Found&&!found)Show("SECRET FOUND");found=world.Secret.Found;
            }
            var selected=weapons.Selected;ammo.text=selected&&!selected.UsesAmmo?"--":(selected?bag.Count(selected.Ammo):0).ToString();health.text=hp.Current+"%";armor.text=hp.Armor.Points+"%";
            reserves.text=$"B {bag.Count(AmmoType.Bullets)}/{bag.Capacity(AmmoType.Bullets)}\nS {bag.Count(AmmoType.Shells)}/{bag.Capacity(AmmoType.Shells)}\nR {bag.Count(AmmoType.Rockets)}/{bag.Capacity(AmmoType.Rockets)}\nC {bag.Count(AmmoType.Cells)}/{bag.Capacity(AmmoType.Cells)}";
            blue.text=HudRules.KeyLabel(bag.Keys,KeyId.BlueCard,KeyId.BlueSkull);yellow.text=HudRules.KeyLabel(bag.Keys,KeyId.YellowCard,KeyId.YellowSkull);red.text=HudRules.KeyLabel(bag.Keys,KeyId.RedCard,KeyId.RedSkull);
            var exp=now<painUntil?FaceExpression.Pain:now<fireUntil?FaceExpression.Fire:HudRules.IdleFace(now);
            face.texture=faces[HudRules.FaceIndex(hp.Current,hp.MaxHealth,exp)];damageFlash.color=new Color(.8f,.04f,.02f,Mathf.Clamp01((painUntil-now)/.4f)*.24f);
            int kills=0,items=0;bool ai=false;foreach(var e in world.Source.Enemies){if(e.State==Ukubola.Enemies.EnemyState.Dead)kills++;if(!e.Suspended)ai=true;}foreach(var p in world.Source.Pickups)if(p.Collected)items++;
            status.text=$"K {kills}/{world.Source.Enemies.Length}  I {items}/{world.Source.Pickups.Length}  S {(world.Secret.Found?1:0)}/1"+(diagnosticsEnabled?$"  {(ai?"AI ON":"AI OFF")}  M {(audio.MusicEnabled?"ON":"OFF")}  FX {(audio.EffectsEnabled?"ON":"OFF")}":"");
            if(hp.IsDead)notice.text=diagnosticsEnabled?"YOU DIED | F9: RESET":"YOU DIED";else if(now>=messageUntil)notice.text="";
            var backdrop=notice.transform.parent.GetComponent<Image>();if(backdrop)backdrop.enabled=notice.text.Length>0;
        }
    }
}
