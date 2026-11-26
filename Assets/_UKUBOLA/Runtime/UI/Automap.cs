using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Ukubola.World;
using Ukubola.Combat;
using Ukubola.Enemies;
using Ukubola.Player;
namespace Ukubola.UI {
 public sealed class Automap:MonoBehaviour {
  [SerializeField]private LevelDefinition level;[SerializeField]private MenuFlow flow;[SerializeField]private RawImage map;[SerializeField]private Text readout;
  [SerializeField]private Health player;[SerializeField]private WeaponController weapons;
  private Texture2D texture;private Color32[] pixels;private bool[,] seen;private bool open;private float next;
  public LevelDefinition Level=>level;public RawImage Map=>map;public Health Player=>player;
  public void Configure(LevelDefinition l,MenuFlow f,RawImage image,Text label,Health hp,WeaponController guns){level=l;flow=f;map=image;readout=label;player=hp;weapons=guns;}
  private void Start(){seen=new bool[level.Width,level.Depth];texture=new Texture2D(level.Width*5,level.Depth*5,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;pixels=new Color32[texture.width*texture.height];map.texture=texture;map.enabled=false;}
  private void Update(){if(texture==null)return;bool playing=flow.Page==MenuPage.Playing&&Time.timeScale>0&&!player.IsDead;var k=Keyboard.current;
   if(!playing)open=false;else if(Application.isFocused&&k!=null&&k.tabKey.wasPressedThisFrame)open=!open;
   map.enabled=playing&&open;var cell=SectorNoise.Cell(player.transform.position);if(cell.x>=0&&cell.x<level.Width&&cell.y>=0&&cell.y<level.Depth)seen[cell.x,cell.y]=true;
   var p=player.GetComponent<Powerup>();var w=weapons.Selected;readout.text=(w?w.Weapon.ToString().ToUpperInvariant():"--")+" | TAB MAP"+(p&&p.Invulnerable?" | INV "+Mathf.CeilToInt(p.Remaining(PowerupKind.Invulnerability)):"")+(p&&p.Berserk?" | BERSERK "+Mathf.CeilToInt(p.Remaining(PowerupKind.Berserk)):"");
   if(!map.enabled||Time.unscaledTime<next)return;next=Time.unscaledTime+.08f;
   var dark=new Color32(12,17,19,242);var floor=new Color32(86,108,104,255);var wall=new Color32(188,153,98,255);
   for(int z=0;z<level.Depth;z++)for(int x=0;x<level.Width;x++)for(int j=0;j<5;j++)for(int i=0;i<5;i++){
    var color=dark;if(seen[x,z]){color=floor;if(i==0&&!LevelDefinition.Walkable(level.Tile(x-1,z))||i==4&&!LevelDefinition.Walkable(level.Tile(x+1,z))||j==0&&!LevelDefinition.Walkable(level.Tile(x,z-1))||j==4&&!LevelDefinition.Walkable(level.Tile(x,z+1)))color=wall;}
    pixels[(z*5+j)*texture.width+x*5+i]=color;
   }
   void Dot(int x,int z){if(x>=0&&x<texture.width&&z>=0&&z<texture.height)pixels[z*texture.width+x]=new Color32(255,231,120,255);}
   int px=Mathf.FloorToInt(player.transform.position.x/LevelDefinition.CellSize*5),pz=Mathf.FloorToInt(player.transform.position.z/LevelDefinition.CellSize*5);var dir=player.transform.forward;
   for(int i=0;i<5;i++)Dot(px+Mathf.RoundToInt(dir.x*i),pz+Mathf.RoundToInt(dir.z*i));Dot(px-1,pz);Dot(px+1,pz);Dot(px,pz-1);Dot(px,pz+1);
   texture.SetPixels32(pixels);texture.Apply(false);
  }
  public bool[] CaptureExploration(){var cells=new bool[level.Width*level.Depth];if(seen!=null)for(int z=0;z<level.Depth;z++)for(int x=0;x<level.Width;x++)cells[z*level.Width+x]=seen[x,z];return cells;}
  public void RestoreExploration(bool[] cells){if(seen==null||cells==null||cells.Length!=level.Width*level.Depth)throw new System.InvalidOperationException("Automap size mismatch");for(int z=0;z<level.Depth;z++)for(int x=0;x<level.Width;x++)seen[x,z]=cells[z*level.Width+x];open=false;map.enabled=false;}
  private void OnDestroy(){if(texture)Destroy(texture);}
 }
}