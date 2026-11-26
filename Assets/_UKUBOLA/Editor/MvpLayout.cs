using System;
using System.Collections.Generic;
using UnityEngine;
using Ukubola.World;
namespace Ukubola.EditorTools
{
    internal static class MvpLayout
    {
        internal sealed class Placement
        {
            internal readonly string Prefab,Name;internal readonly Vector2Int Cell;internal readonly float Yaw;
            internal Placement(string prefab,string name,int x,int z,float yaw=0){Prefab=prefab;Name=name;Cell=new Vector2Int(x,z);Yaw=yaw;}
        }
        internal static readonly Placement[] Enemies={
            new Placement("Guard","Guard - Entry",5,1,-90),new Placement("Imp","Imp - East",15,4,-90),
            new Placement("Guard","Guard - South",6,13,180),new Placement("Imp","Imp - Exit",16,11,180)};
        internal static readonly Placement[] Items={
            new Placement("Shotgun","Shotgun",3,2),new Placement("Bullets","Bullets A",1,2),new Placement("GreenArmor","Green Armor",1,3),
            new Placement("Stimpack","Stimpack A",2,4),new Placement("Medikit","Medikit A",5,5),new Placement("Shells","Shells A",5,3),
            new Placement("Backpack","Backpack",3,9),new Placement("BlueCard","Blue Card",11,2),new Placement("BlueArmor","Secret Blue Armor",17,1),
            new Placement("Bullets","Bullets B",10,4),new Placement("Bullets","Bullets C",2,13),new Placement("Shells","Shells B",12,4),
            new Placement("Shells","Shells C",15,12),new Placement("Medikit","Medikit B",15,3),new Placement("Medikit","Medikit C",4,12),new Placement("Stimpack","Stimpack B",13,11)};
        internal static void Check(LevelDefinition level)
        {
            if(!level.TryValidate(out string reason,out var spawn,out var exit))throw new InvalidOperationException(reason);
            var used=new HashSet<Vector2Int>{spawn,exit};
            foreach(var p in Enemies)CheckCell(level,p,used);
            foreach(var p in Items)CheckCell(level,p,used);
        }
        private static void CheckCell(LevelDefinition level,Placement p,HashSet<Vector2Int> used)
        {if(!LevelDefinition.Walkable(level.Tile(p.Cell.x,p.Cell.y))||!used.Add(p.Cell))throw new InvalidOperationException("Invalid or repeated MVP cell: "+p.Name+" "+p.Cell);}
    }
}
