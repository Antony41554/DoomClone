using UnityEngine;
namespace Ukubola.Combat {
 public enum PowerupKind { Invulnerability, Berserk }
 [DisallowMultipleComponent] public sealed class Powerup:MonoBehaviour {
  private float invulnerableUntil,berserkUntil;
  private static float Now=>Application.isPlaying?Time.time:0f;
  public bool Invulnerable=>Remaining(PowerupKind.Invulnerability)>0;
  public bool Berserk=>Remaining(PowerupKind.Berserk)>0;
  public float Remaining(PowerupKind kind)=>Mathf.Max(0,(kind==PowerupKind.Invulnerability?invulnerableUntil:berserkUntil)-Now);
  public static float Extend(float now,float old,float duration)=>Mathf.Max(old,now+Mathf.Max(0,duration));
  public bool Grant(PowerupKind kind,float seconds){if(!(seconds>0)||float.IsInfinity(seconds)||!System.Enum.IsDefined(typeof(PowerupKind),kind))return false;var hp=GetComponent<Health>();if(!hp||hp.IsDead)return false;float previous=kind==PowerupKind.Invulnerability?invulnerableUntil:berserkUntil;float next=Extend(Now,previous,seconds);if(next<=previous)return false;if(kind==PowerupKind.Invulnerability)invulnerableUntil=next;else berserkUntil=next;return true;}
  public void Clear(){invulnerableUntil=berserkUntil=0;}
 }
}