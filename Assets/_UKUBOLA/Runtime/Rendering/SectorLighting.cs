using UnityEngine;
namespace Ukubola.Rendering {
 [RequireComponent(typeof(SpriteRenderer))] public sealed class SectorLighting:MonoBehaviour {
  private SpriteRenderer sprite;private Color original;
  public static float Brightness(Vector3 p)=>p.z>=20f?.55f:p.x>=20f?.76f:1f;
  private void Awake(){sprite=GetComponent<SpriteRenderer>();original=sprite.color;}
  private void LateUpdate(){float light=Brightness(transform.position);sprite.color=new Color(original.r*light,original.g*light,original.b*light,original.a);}
  private void OnDisable(){if(sprite)sprite.color=original;}
 }
}