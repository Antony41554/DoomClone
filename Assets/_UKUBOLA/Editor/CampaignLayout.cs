using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ukubola.World;
namespace Ukubola.EditorTools
{
    internal sealed class CampaignLayout
    {
        internal readonly int Width,Depth,A,B,Mid,NormalZ,BlueX,RemoteZ;
        internal readonly Vector2Int Spawn,Exit,Secret,Switch,Lift,LiftSwitch,Hazard,Teleport,Destination,BarrelA,BarrelB;
        internal readonly MvpLayout.Placement[] Enemies,Items;
        internal readonly string Id,Label;
        private static Vector2Int C(int x,int z)=>new Vector2Int(x,z);
        private static MvpLayout.Placement P(string name,int x,int z,float yaw=180)=>new MvpLayout.Placement(name,name,x,z,yaw);
        private CampaignLayout(bool core)
        {
            Width=core?25:21;Depth=core?19:17;A=core?8:6;B=core?16:13;Mid=core?10:7;NormalZ=core?14:3;BlueX=core?11:9;RemoteZ=core?3:11;
            Id=core?"E1M3_CORE":"E1M2_FOUNDRY";Label=core?"E1M3 / THE CORE":"E1M2 / THE FOUNDRY";
            Spawn=core?C(2,15):C(2,2);Exit=core?C(22,16):C(18,14);Secret=C(Width-2,1);
            Switch=core?C(14,4):C(11,10);Lift=core?C(2,10):C(2,8);LiftSwitch=core?C(2,9):C(2,9);Hazard=core?C(12,16):C(4,13);Teleport=core?C(10,16):C(10,14);Destination=core?C(1,12):C(1,6);BarrelA=core?C(5,14):C(4,5);BarrelB=core?C(6,14):C(5,5);
            Enemies=core?new[]{P("Guard",5,15,-90),P("Imp",5,11,-90),P("Brute",6,4,180),P("Guard",11,13,90),P("Imp",14,16,-90),P("Watcher",12,7,0),P("Brute",14,5,180),P("Imp",19,4,-90),P("Watcher",21,9,180),P("Brute",20,15,-90)}:
                new[]{P("Guard",4,4,-90),P("Imp",3,12,180),P("Brute",10,4,-90),P("Guard",11,9,180),P("Imp",8,13,0),P("Watcher",17,5,-90),P("Brute",15,12,-90),P("Imp",18,12,180)};
            Items=core?new[]{P("Shotgun",3,15),P("Bullets",1,15),P("GreenArmor",2,14),P("Medikit",4,16),P("Shells",4,15),P("Chaingun",6,13),P("Bullets",3,8),P("Medikit",6,6),P("BlueCard",11,15),P("RocketLauncher",10,13),P("Rockets",13,14),P("Shells",14,13),P("PlasmaRifle",12,8),P("Cells",13,8),P("Medikit",10,5),P("Cells",14,3),P("Medikit",18,4),P("EnergyCannon",20,4),P("Cells",20,6),P("Invulnerability",22,12),P("BlueArmor",23,1),P("Shells",20,16),P("Berserk",21,15),P("Backpack",11,17)}:
                new[]{P("Shotgun",3,2),P("Bullets",1,2),P("GreenArmor",2,3),P("Medikit",3,5),P("Shells",4,2),P("Chaingun",4,6),P("Bullets",5,10),P("Medikit",2,12),P("BlueCard",9,2),P("Rockets",8,2),P("RocketLauncher",10,5),P("Shells",11,5),P("Medikit",8,9),P("PlasmaRifle",10,10),P("Cells",11,12),P("Backpack",8,14),P("Bullets",15,11),P("Medikit",17,13),P("Cells",17,6),P("EnergyCannon",18,5),P("Invulnerability",15,4),P("BlueArmor",19,1),P("Shells",18,9),P("Berserk",16,8)};
        }
        internal static CampaignLayout For(int index){if(index!=1&&index!=2)throw new ArgumentOutOfRangeException(nameof(index));return new CampaignLayout(index==2);}
        internal string[] Rows()
        {
            var rows=Enumerable.Range(0,Depth).Select(z=>Enumerable.Range(0,Width).Select(x=>x==0||z==0||x==Width-1||z==Depth-1||x==A||x==B||z==Mid&&x>A&&x<B?'#':'.').ToArray()).ToArray();
            for(int i=0;i<2;i++){rows[NormalZ+i][A]='.';rows[Mid][BlueX+i]='.';rows[RemoteZ+i][B]='.';}
            // Extra wall islands for distinct sightlines.
            foreach(var p in Width==21?new[]{C(3,9),C(3,10),C(16,7),C(16,9)}:new[]{C(4,6),C(4,7),C(6,10),C(19,10),C(19,11),C(22,6)})rows[p.y][p.x]='#';
            for(int z=Exit.y-1;z<=Exit.y+1;z++)for(int x=Exit.x-1;x<=Exit.x+1;x++)rows[z][x]='1';
            rows[Spawn.y][Spawn.x]='S';rows[Exit.y][Exit.x]='E';return rows.Select(r=>new string(r)).ToArray();
        }
        internal IEnumerable<Vector2Int> Reserved()=>new[]{Spawn,Exit,Switch,Lift,LiftSwitch,Hazard,Teleport,Destination,BarrelA,BarrelB};
        internal HashSet<Vector2Int> Reach(LevelDefinition level,bool normal,bool blue,bool remote,bool secret)
        {
            var blocked=new HashSet<Vector2Int>();for(int i=0;i<2;i++){if(!normal)blocked.Add(C(A,NormalZ+i));if(!blue)blocked.Add(C(BlueX+i,Mid));if(!remote)blocked.Add(C(B,RemoteZ+i));}if(!secret)blocked.Add(Secret);
            var visited=new HashSet<Vector2Int>{Spawn};var queue=new Queue<Vector2Int>();queue.Enqueue(Spawn);
            while(queue.Count>0){var p=queue.Dequeue();foreach(var d in LevelDefinition.Neighbors){var n=p+d;if(blocked.Contains(n)||!LevelDefinition.Walkable(level.Tile(n.x,n.y))||Mathf.Abs(LevelDefinition.Elevation(level.Tile(p.x,p.y))-LevelDefinition.Elevation(level.Tile(n.x,n.y)))>LevelDefinition.MaxStep||!visited.Add(n))continue;queue.Enqueue(n);}}return visited;
        }
        internal void Check(LevelDefinition level)
        {
            if(!level.TryValidate(out string why,out var spawn,out var exit)||spawn!=Spawn||exit!=Exit)throw new InvalidOperationException("Invalid campaign map: "+why);
            var used=new HashSet<Vector2Int>();foreach(var p in Reserved().Concat(Enemies.Select(x=>x.Cell)).Concat(Items.Select(x=>x.Cell)))if(!LevelDefinition.Walkable(level.Tile(p.x,p.y))||!used.Add(p))throw new InvalidOperationException("Blocked/repeated cell: "+p+" in "+Id);
            var key=Items.Single(x=>x.Prefab=="BlueCard").Cell;var first=Reach(level,true,false,false,false);var second=Reach(level,true,true,false,false);var last=Reach(level,true,true,true,false);
            if(!first.Contains(key)||first.Contains(Switch)||first.Contains(Exit)||!second.Contains(Switch)||second.Contains(Exit)||!last.Contains(Exit)||last.Contains(Secret)||!Reach(level,true,true,true,true).Contains(Secret))throw new InvalidOperationException("Campaign gate order is invalid: "+Id);
        }
    }
}
