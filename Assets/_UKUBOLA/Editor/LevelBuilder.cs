using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Ukubola.World;

namespace Ukubola.EditorTools
{
    // One rendered mesh per surface type; independent simple BoxColliders avoid
    // non-convex mesh cooking and keep the geometry usable for later navigation.
    internal static class LevelBuilder
    {
        internal const float FloorBottom = -.3f;
        internal const float Thickness = .2f;
        internal struct Solid
        {
            public Vector3 Center, Size;
            public Solid(Vector3 center, Vector3 size) { Center = center; Size = size; }
        }
        internal static List<Solid>[] Plan(LevelDefinition level)
        {
            if (!level.TryValidate(out string reason,out _,out _)) throw new ArgumentException(reason);
            var groups = new[] { new List<Solid>(), new List<Solid>(), new List<Solid>() };
            float s = LevelDefinition.CellSize, top = LevelDefinition.Ceiling;
            for (int z=0; z<level.Depth; z++)
            {
                // Merge contiguous floor segments with equal height along X.
                for (int x=0; x<level.Width; )
                {
                    char tile = level.Tile(x,z);
                    if (!LevelDefinition.Walkable(tile)) { x++; continue; }
                    int start=x; float h=LevelDefinition.Elevation(tile);
                    do { x++; } while (x<level.Width && LevelDefinition.Walkable(level.Tile(x,z)) && LevelDefinition.Elevation(level.Tile(x,z))==h);
                    groups[0].Add(new Solid(new Vector3((start+x)*s*.5f,(h+FloorBottom)*.5f,(z+.5f)*s),new Vector3((x-start)*s,h-FloorBottom,s)));
                }
                // Merge ceilings independently of floor height.
                for (int x=0; x<level.Width; )
                {
                    if (!LevelDefinition.Walkable(level.Tile(x,z))) { x++; continue; }
                    int start=x; do { x++; } while (x<level.Width && LevelDefinition.Walkable(level.Tile(x,z)));
                    groups[1].Add(new Solid(new Vector3((start+x)*s*.5f,top+Thickness*.5f,(z+.5f)*s),new Vector3((x-start)*s,Thickness,s)));
                }
                // Boundary walls straddle cell edges by 10 cm and overlap at corners. Adjacent straight segments meet without coplanar overlap.
                for (int x=0; x<level.Width; x++)
                {
                    if (!LevelDefinition.Walkable(level.Tile(x,z))) continue;
                    foreach (var d in LevelDefinition.Neighbors)
                    {
                        if (LevelDefinition.Walkable(level.Tile(x+d.x,z+d.y))) continue;
                        float offset=s*.5f;
                        groups[2].Add(new Solid(new Vector3((x+.5f)*s+d.x*offset,(top+Thickness+FloorBottom)*.5f,(z+.5f)*s+d.y*offset),
                            new Vector3(d.x==0?s:Thickness,top+Thickness-FloorBottom,d.y==0?s:Thickness)));
                    }
                }
            }
            return groups;
        }
        internal static readonly string[] GroupNames = { "Floors", "Ceilings", "Walls" };
        internal static void Build(Transform root, LevelDefinition level, Material[] materials, string folder, int layer)
        {
            var groups = Plan(level);
            for (int i=0;i<groups.Length;i++)
            {
                string name=GroupNames[i], path=folder+"/"+name+".asset";
                if (AssetDatabase.LoadMainAssetAtPath(path)!=null || System.IO.File.Exists(path)) throw new InvalidOperationException("Generated mesh already exists: "+path+". Follow recovery instructions.");
                var mesh=CreateMesh(name,groups[i]);
                AssetDatabase.CreateAsset(mesh,path);
                var go=new GameObject(name); go.layer=layer; go.transform.SetParent(root,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial=materials[i];
                // Each collider uses the same planned solid as its render geometry.
                for (int j=0;j<groups[i].Count;j++)
                {
                    var solid=groups[i][j]; var box=go.AddComponent<BoxCollider>();
                    box.center=solid.Center; box.size=solid.Size;
                }
            }
        }
        internal static Mesh CreateMesh(string name,List<Solid> solids)
        {
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            void Face(Vector3 center, Vector3 u, Vector3 v)
            {
                int start=vertices.Count;
                vertices.Add(center-u-v); vertices.Add(center+u-v); vertices.Add(center+u+v); vertices.Add(center-u+v);
                Vector3 normal=Vector3.Cross(u,v).normalized;
                for(int i=0;i<4;i++) normals.Add(normal);
                // World-aligned UV at one repeat per 2m; phase 02 materials are not changed.
                Vector3 a=u.normalized,b=v.normalized;
                for(int i=0;i<4;i++) { Vector3 p=vertices[start+i]; uv.Add(new Vector2(Vector3.Dot(p,a)/2f,Vector3.Dot(p,b)/2f)); }
                triangles.Add(start); triangles.Add(start+1); triangles.Add(start+2);
                triangles.Add(start); triangles.Add(start+2); triangles.Add(start+3);
            }
            foreach(var box in solids)
            {
                Vector3 c=box.Center,x=Vector3.right*box.Size.x*.5f,y=Vector3.up*box.Size.y*.5f,z=Vector3.forward*box.Size.z*.5f;
                Face(c+x,-z,y); Face(c-x,z,y); Face(c+y,x,-z); Face(c-y,x,z); Face(c+z,x,y); Face(c-z,-x,y);
            }
            var mesh=new Mesh { name=name, indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.subMeshCount=1; mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
