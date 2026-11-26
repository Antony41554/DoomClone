using System.Collections.Generic;
using UnityEngine;
using Ukubola.World;
namespace Ukubola.Enemies {
 public sealed class SectorNoise:MonoBehaviour {
  [SerializeField] private LevelDefinition level;[SerializeField]private LayerMask worldMask;
  public LevelDefinition Level=>level;
  public void Configure(LevelDefinition map,int mask){level=map;worldMask=mask;}
  public static Vector2Int Cell(Vector3 position)=>new Vector2Int(Mathf.FloorToInt(position.x/LevelDefinition.CellSize),Mathf.FloorToInt(position.z/LevelDefinition.CellSize));
  public bool CanReach(Vector3 from,Vector3 to,float range){
   if(!level||Vector3.Distance(from,to)>range)return false;var start=Cell(from);var end=Cell(to);
   if(!LevelDefinition.Walkable(level.Tile(start.x,start.y))||!LevelDefinition.Walkable(level.Tile(end.x,end.y)))return false;
   var visited=new HashSet<Vector2Int>{start};var queue=new Queue<(Vector2Int cell,int steps)>();queue.Enqueue((start,0));
   while(queue.Count>0){var current=queue.Dequeue();if(current.cell==end)return true;if((current.steps+1)*LevelDefinition.CellSize>range)continue;
    foreach(var d in LevelDefinition.Neighbors){var next=current.cell+d;if(visited.Contains(next)||!LevelDefinition.Walkable(level.Tile(next.x,next.y)))continue;
     var a=level.Center(current.cell)+Vector3.up*1.1f;var b=level.Center(next)+Vector3.up*1.1f;
     if(Physics.CheckSphere(a,.06f,worldMask,QueryTriggerInteraction.Ignore)||Physics.CheckSphere(b,.06f,worldMask,QueryTriggerInteraction.Ignore)||Physics.Linecast(a,b,worldMask,QueryTriggerInteraction.Ignore))continue;
     visited.Add(next);queue.Enqueue((next,current.steps+1));}
   }return false;
  }
 }
}