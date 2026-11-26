using UnityEngine;
using Ukubola.Combat;
namespace Ukubola.Enemies {
 public sealed class EnemyFaction:MonoBehaviour {
  [SerializeField] private int faction=1;
  public int Faction=>faction;
  public void Configure(int id)=>faction=id;
  public static bool Hostile(int a,int b)=>a>0&&b>0&&a!=b;
  public bool Retaliates(GameObject source){var other=source?source.GetComponent<EnemyFaction>():null;return other&&other!=this&&Hostile(faction,other.faction);}
 }
}