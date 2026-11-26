using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ukubola.EditorTools
{
    // Original 5 x 7 block lettering. Opaque geometry uses the world's depth
    // buffer, avoiding the legacy dynamic GUI font material in 3D signs.
    internal static class UkubolaPixelSigns
    {
        private static readonly Dictionary<char,string> Glyphs=new Dictionary<char,string>
        {
            ['A']="01110/10001/10001/11111/10001/10001/10001",['B']="11110/10001/10001/11110/10001/10001/11110",
            ['C']="01111/10000/10000/10000/10000/10000/01111",['D']="11110/10001/10001/10001/10001/10001/11110",
            ['E']="11111/10000/10000/11110/10000/10000/11111",['F']="11111/10000/10000/11110/10000/10000/10000",
            ['G']="01111/10000/10000/10111/10001/10001/01111",['H']="10001/10001/10001/11111/10001/10001/10001",
            ['I']="11111/00100/00100/00100/00100/00100/11111",['J']="00111/00010/00010/00010/10010/10010/01100",
            ['K']="10001/10010/10100/11000/10100/10010/10001",['L']="10000/10000/10000/10000/10000/10000/11111",
            ['M']="10001/11011/10101/10101/10001/10001/10001",['N']="10001/11001/11001/10101/10011/10011/10001",
            ['O']="01110/10001/10001/10001/10001/10001/01110",['P']="11110/10001/10001/11110/10000/10000/10000",
            ['Q']="01110/10001/10001/10001/10101/10010/01101",['R']="11110/10001/10001/11110/10100/10010/10001",
            ['S']="01111/10000/10000/01110/00001/00001/11110",['T']="11111/00100/00100/00100/00100/00100/00100",
            ['U']="10001/10001/10001/10001/10001/10001/01110",['V']="10001/10001/10001/10001/10001/01010/00100",
            ['W']="10001/10001/10001/10101/10101/11011/10001",['X']="10001/10001/01010/00100/01010/10001/10001",
            ['Y']="10001/10001/01010/00100/00100/00100/00100",['Z']="11111/00001/00010/00100/01000/10000/11111",
            ['0']="01110/10001/10011/10101/11001/10001/01110",['1']="00100/01100/00100/00100/00100/00100/01110",
            ['2']="01110/10001/00001/00010/00100/01000/11111",['3']="11110/00001/00001/01110/00001/00001/11110",
            ['4']="00010/00110/01010/10010/11111/00010/00010",['5']="11111/10000/10000/11110/00001/00001/11110",
            ['6']="01110/10000/10000/11110/10001/10001/01110",['7']="11111/00001/00010/00100/01000/01000/01000",
            ['8']="01110/10001/10001/01110/10001/10001/01110",['9']="01110/10001/10001/01111/00001/00001/01110",
            ['-']="00000/00000/00000/11111/00000/00000/00000",['/']="00001/00001/00010/00100/01000/10000/10000",
            ['>']="10000/01000/00100/00010/00100/01000/10000",['[']="01110/01000/01000/01000/01000/01000/01110",
            [']']="01110/00010/00010/00010/00010/00010/01110",[':']="00000/00100/00100/00000/00100/00100/00000"
        };
        public static GameObject Create(Transform parent,string text,Vector3 position,float yaw,float height,Color color,float maxWidth)
        {
            string[] lines=text.ToUpperInvariant().Split('\n');int columns=lines.Max(s=>s.Length);
            float pixel=Mathf.Min(height/7,maxWidth/Mathf.Max(1,columns*6-1));
            string key="pixel-sign-v1:"+text+":"+height.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+maxWidth.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
            string path=UkubolaEvolutionArt.Root+"/Meshes/"+Ukubola.Persistence.SaveStore.Hash(key).Substring(0,20)+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                var vertices=new List<Vector3>();var triangles=new List<int>();
                for(int line=0;line<lines.Length;line++)for(int c=0;c<lines[line].Length;c++)
                {
                    char letter=lines[line][c];if(letter==' ')continue;
                    if(!Glyphs.TryGetValue(letter,out string glyph))throw new InvalidOperationException("Missing industrial glyph: "+letter);
                    var rows=glyph.Split('/');
                    for(int y=0;y<7;y++)for(int x=0;x<5;x++)if(rows[y][x]=='1')
                    {
                        float left=(c*6+x-(lines[line].Length*6-1)*.5f)*pixel;
                        float top=((lines.Length*9-2)*.5f-line*9-y)*pixel;int index=vertices.Count;
                        vertices.AddRange(new[]{new Vector3(left,top-pixel,0),new Vector3(left,top,0),new Vector3(left+pixel,top,0),new Vector3(left+pixel,top-pixel,0)});
                        triangles.AddRange(new[]{index,index+1,index+2,index,index+2,index+3});
                    }
                }
                mesh=new Mesh{name="Industrial lettering: "+text};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                mesh.uv=new Vector2[vertices.Count];AssetDatabase.CreateAsset(mesh,path);
            }
            var go=new GameObject("Industrial Sign");go.layer=LayerMask.NameToLayer("World");go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=UkubolaEvolutionArt.Solid("Sign_"+ColorUtility.ToHtmlStringRGB(color),color,0,.2f,.6f);
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;
        }
    }
}
