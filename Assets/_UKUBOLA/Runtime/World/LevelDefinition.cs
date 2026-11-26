using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ukubola.World
{
    // Rows map to +Z, columns to +X. # = solid void, . = floor, S = spawn,
    // 1 = +0.25m, 2 and E = +0.50m (exit on upper platform).
    public sealed class LevelDefinition : ScriptableObject
    {
        public const float CellSize = 2f;
        public const float Ceiling = 3.2f;
        public const float MaxStep = .25f;
        [SerializeField] private string levelId = "LEVEL_LAB";
        [SerializeField] private string[] rows = {
            "###################",
            "#.....####........#",
            "#.S...####........#",
            "#.................#",
            "#.................#",
            "#.....####........#",
            "###..#####........#",
            "###..########..####",
            "###..########..####",
            "#.......#####..####",
            "#.......##........#",
            "#.........11111111#",
            "#.........222222E2#",
            "#.......##22222222#",
            "###################"
        };
        public string LevelId => levelId;
        public int Width => rows != null && rows.Length > 0 && rows[0] != null ? rows[0].Length : 0;
        public int Depth => rows == null ? 0 : rows.Length;
        public char Tile(int x, int z) => z >= 0 && z < Depth && rows[z] != null && x >= 0 && x < rows[z].Length ? rows[z][x] : '#';
        public static bool Walkable(char tile) => tile == '.' || tile == 'S' || tile == '1' || tile == '2' || tile == 'E';
        public static float Elevation(char tile) => tile == '1' ? .25f : tile == '2' || tile == 'E' ? .5f : 0f;
        public Vector3 Center(Vector2Int cell) => new Vector3((cell.x + .5f) * CellSize, Elevation(Tile(cell.x, cell.y)), (cell.y + .5f) * CellSize);
        public static readonly Vector2Int[] Neighbors = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        public void SetLayout(string id, string[] layout) { levelId = id; rows = layout == null ? null : (string[])layout.Clone(); }
        public bool TryValidate(out string reason, out Vector2Int spawn, out Vector2Int exit)
        {
            reason = null; spawn = exit = default;
            if (string.IsNullOrWhiteSpace(levelId) || Width < 3 || Depth < 3 || Width > 64 || Depth > 64)
            { reason = "Invalid level ID or map size (3..64 cells per side)."; return false; }
            int startCount = 0, exitCount = 0, walkCount = 0;
            for (int z = 0; z < Depth; z++)
            {
                if (rows[z] == null || rows[z].Length != Width) { reason = "Map rows have different lengths."; return false; }
                for (int x = 0; x < Width; x++)
                {
                    char tile = Tile(x,z);
                    if (tile != '#' && !Walkable(tile)) { reason = "Unsupported tile at " + x + "," + z; return false; }
                    if ((x == 0 || z == 0 || x == Width-1 || z == Depth-1) && tile != '#') { reason = "Map must have a solid perimeter."; return false; }
                    if (Walkable(tile)) walkCount++;
                    if (tile == 'S') { startCount++; spawn = new Vector2Int(x,z); }
                    if (tile == 'E') { exitCount++; exit = new Vector2Int(x,z); }
                }
            }
            if (startCount != 1 || exitCount != 1) { reason = "Exactly one S and one E required."; return false; }
            var visited = new HashSet<Vector2Int> { spawn };
            var queue = new Queue<Vector2Int>(); queue.Enqueue(spawn);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var direction in Neighbors)
                {
                    var next = current + direction; char a = Tile(current.x,current.y), b = Tile(next.x,next.y);
                    if (Walkable(b) && Mathf.Abs(Elevation(a)-Elevation(b)) <= MaxStep && visited.Add(next)) queue.Enqueue(next);
                }
            }
            if (!visited.Contains(exit) || visited.Count != walkCount) { reason = "Some walkable cells are unreachable using 25 cm steps."; return false; }
            return true;
        }
    }
}
