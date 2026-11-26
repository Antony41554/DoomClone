using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ukubola.EditorTools
{
    public static class UkubolaEvolutionArt
    {
        public const string Root = "Assets/_UKUBOLA/Generated/Evolution";
        public const string Textures = "Assets/_UKUBOLA/Art/Evolution/Textures";
        private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int index = path.LastIndexOf('/'); Folder(path.Substring(0, index));
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(path.Substring(0, index), path.Substring(index + 1)))) throw new InvalidOperationException("Cannot create " + path);
        }
        public static T Asset<T>(string path) where T : UnityEngine.Object
        { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (!value) throw new InvalidOperationException("Missing " + path); return value; }
        public static void ImportTextures()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Textures }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                EvolutionTextureImporter.Configure(importer,path);
                importer.SaveAndReimport();
            }
        }
        public static Material Surface(string key, Color? tint = null, float emission = 0)
        {
            var albedo=Asset<Texture2D>(Textures+"/"+key+"_Base.png");var normal=Asset<Texture2D>(Textures+"/"+key+"_Normal.png");
            var mask=Asset<Texture2D>(Textures+"/"+key+"_Mask.png");var emissive=Asset<Texture2D>(Textures+"/"+key+"_Emission.png");
            Folder(Root + "/Materials"); string materialKey = key + (tint.HasValue ? "_" + ColorUtility.ToHtmlStringRGB(tint.Value) : "") + "_" + emission.ToString("F1", CultureInfo.InvariantCulture);
            string path = Root + "/Materials/" + materialKey + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap",albedo);
            material.SetTexture("_BumpMap",normal); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",mask); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_OcclusionMap",mask); material.EnableKeyword("_OCCLUSIONMAP");
            material.SetTexture("_EmissionMap",emissive);
            material.SetColor("_BaseColor", tint ?? Color.white); material.SetColor("_EmissionColor", Color.white * emission);
            material.globalIlluminationFlags = emission > 0 ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if (emission > 0) material.EnableKeyword("_EMISSION"); else material.DisableKeyword("_EMISSION");
            material.SetFloat("_Smoothness", key.Contains("Floor") ? .95f : .6f); material.SetFloat("_BumpScale", .45f); material.enableInstancing = false;
            EditorUtility.SetDirty(material); return material;
        }
        public static Material Solid(string key, Color color, float metal = .25f, float smooth = .5f, float emission = 0)
        {
            Folder(Root + "/Materials"); string path = Root + "/Materials/" + key + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metal); material.SetFloat("_Smoothness", smooth);
            material.SetColor("_EmissionColor", color * emission);
            material.globalIlluminationFlags = emission > 0 ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            if (emission > 0) material.EnableKeyword("_EMISSION"); else material.DisableKeyword("_EMISSION");
            // The room uses SRP batching. Enable GPU instancing only with a measured
            // renderer contract and its complete pass variants, including shadows.
            material.enableInstancing = false; EditorUtility.SetDirty(material); return material;
        }
        private static Mesh StoreMesh(string key, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int> triangles)
        {
            Folder(Root + "/Meshes"); string path = Root + "/Meshes/" + Ukubola.Persistence.SaveStore.Hash(key).Substring(0, 20) + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created=!mesh;if(created)mesh=new Mesh{name=key};else mesh.Clear();
            mesh.indexFormat=vertices.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16;
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            Unwrapping.GenerateSecondaryUVSet(mesh);if(created)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
            meshes[key] = mesh; return mesh;
        }
        public static Mesh BevelBox(Vector3 size, float bevel = .035f)
        {
            float b = Mathf.Min(bevel, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .2f);
            string key = "bevel:" + size.ToString("F3",CultureInfo.InvariantCulture) + ":" + b.ToString("F3", CultureInfo.InvariantCulture);
            if (meshes.TryGetValue(key, out var cached) && cached) return cached;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>(); var h = size * .5f;
            void Face(Vector3[] points, Vector3 normal)
            {
                if (Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), normal) < 0) Array.Reverse(points);
                int offset = vertices.Count; var axis = Mathf.Abs(normal.y) > .7f ? Vector3.right : Mathf.Abs(normal.x) > .7f ? Vector3.forward : Vector3.right;
                var other = Vector3.Cross(normal, axis).normalized;
                foreach (var point in points) { vertices.Add(point); normals.Add(normal); uv.Add(new Vector2(Vector3.Dot(point, axis), Vector3.Dot(point, other)) * .5f); }
                for (int i = 1; i + 1 < points.Length; i++) { triangles.Add(offset); triangles.Add(offset + i); triangles.Add(offset + i + 1); }
            }
            for (int axis = 0; axis < 3; axis++)
                foreach (float sign in new[] { -1f, 1f })
                {
                    int a = (axis + 1) % 3, c = (axis + 2) % 3; var n = Vector3.zero; n[axis] = sign;
                    var points = new Vector3[4]; int k = 0;
                    foreach (var pair in new[] { new Vector2(-1,-1), new Vector2(1,-1), new Vector2(1,1), new Vector2(-1,1) })
                    { var p = Vector3.zero; p[axis] = sign * h[axis]; p[a] = pair.x * (h[a] - b); p[c] = pair.y * (h[c] - b); points[k++] = p; }
                    Face(points, n);
                }
            for (int axis = 0; axis < 3; axis++)
                foreach (float sa in new[] { -1f, 1f }) foreach (float sb in new[] { -1f, 1f })
                {
                    int a = (axis + 1) % 3, c = (axis + 2) % 3; var points = new Vector3[4]; var n = Vector3.zero; n[a] = sa; n[c] = sb;
                    for (int i = 0; i < 4; i++)
                    { var p = Vector3.zero; p[axis] = (i < 2 ? -1 : 1) * (h[axis] - b); bool first = i == 0 || i == 3; p[a] = sa * (first ? h[a] : h[a] - b); p[c] = sb * (first ? h[c] - b : h[c]); points[i] = p; }
                    Face(points, n.normalized);
                }
            foreach (float x in new[] { -1f, 1f }) foreach (float y in new[] { -1f, 1f }) foreach (float z in new[] { -1f, 1f })
                Face(new[] { new Vector3(x*h.x,y*(h.y-b),z*(h.z-b)), new Vector3(x*(h.x-b),y*h.y,z*(h.z-b)), new Vector3(x*(h.x-b),y*(h.y-b),z*h.z) }, new Vector3(x,y,z).normalized);
            return StoreMesh(key, vertices, normals, uv, triangles);
        }
        public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collision = false, Quaternion? rotation = null)
        {
            var go = new GameObject(name); go.layer = LayerMask.NameToLayer("World"); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localRotation = rotation ?? Quaternion.identity;
            go.AddComponent<MeshFilter>().sharedMesh = BevelBox(size); go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) go.AddComponent<BoxCollider>().size = size;
            return go;
        }
        public static GameObject Tube(Transform parent, string name, Vector3[] points, float radius, Material material, int sides = 8)
        {
            string key = "tube:" + radius.ToString("F3", CultureInfo.InvariantCulture) + ":" + string.Join(";", points.Select(p => p.ToString("F3",CultureInfo.InvariantCulture))) + ":" + sides;
            Mesh mesh;
            if (!meshes.TryGetValue(key, out mesh) || !mesh)
            {
                var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
                for (int i = 0; i < points.Length; i++)
                {
                    var tangent = (points[Mathf.Min(i+1, points.Length-1)] - points[Mathf.Max(0,i-1)]).normalized;
                    var cross = Vector3.Cross(tangent, Mathf.Abs(tangent.y) < .9f ? Vector3.up : Vector3.right).normalized; var up = Vector3.Cross(tangent, cross).normalized;
                    for (int j = 0; j <= sides; j++)
                    { float angle = j * Mathf.PI * 2 / sides; var normal = cross * Mathf.Cos(angle) + up * Mathf.Sin(angle); vertices.Add(points[i] + normal * radius); normals.Add(normal); uv.Add(new Vector2(j / (float)sides, i / 2f)); }
                }
                for (int i = 0; i < points.Length - 1; i++) for (int j = 0; j < sides; j++)
                { int a = i * (sides+1)+j, c = a+sides+1; triangles.AddRange(new[] { a,a+1,c,a+1,c+1,c }); }
                mesh = StoreMesh(key, vertices, normals, uv, triangles);
            }
            var go = new GameObject(name); go.layer = LayerMask.NameToLayer("World"); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material; return go;
        }
        public static GameObject Character(Transform parent, string archetype)
        {
            bool brute = archetype == "Brute", imp = archetype == "Imp", watcher = archetype == "Watcher";
            var body = Surface("Armor", imp ? new Color(2.2f,1.05f,.55f) : brute ? new Color(1.5f,1,.65f) : new Color(1.9f,2.05f,1.9f));
            var dark = Solid("JointMetal", new Color(.08f,.105f,.11f), .55f, .4f);
            var visor = Solid(imp ? "AmberVisor" : "CyanVisor", imp ? new Color(1,.3f,.03f) : new Color(.04f,.75f,.95f), .2f,.65f,3);
            var root = new GameObject(archetype + " Model"); root.transform.SetParent(parent,false);
            var rig = new GameObject("Rig").transform; rig.SetParent(root.transform,false);
            if (watcher)
            {
                Box(rig,"Core",new Vector3(0,1.25f,0),new Vector3(.7f,.7f,.6f),body);
                Box(rig,"Eye",new Vector3(0,1.3f,.32f),new Vector3(.48f,.18f,.08f),visor);
                for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f; Box(rig,"Fin"+i,new Vector3(Mathf.Cos(a)*.6f,1.2f+Mathf.Sin(a)*.35f,0),new Vector3(.22f,.55f,.5f),dark,rotation:Quaternion.Euler(0,0,-a*Mathf.Rad2Deg));}
            }
            else
            {
                float width=brute?1.3f:.87f, scale=brute?1.13f:1;
                Box(rig,"Chest",new Vector3(0,1.18f*scale,0),new Vector3(width,.58f*scale,.48f),body);
                Box(rig,"ChestInset",new Vector3(0,1.22f*scale,.26f),new Vector3(width*.67f,.25f,.06f),dark);
                Box(rig,"Belt",new Vector3(0,.83f*scale,0),new Vector3(width*.85f,.15f,.49f),dark);
                Box(rig,"Head",new Vector3(0,1.72f*scale,0),new Vector3(.56f,.48f,.5f),body);
                Box(rig,"Visor",new Vector3(0,1.75f*scale,.265f),new Vector3(.42f,.12f,.06f),visor);
                Box(rig,"Mouth",new Vector3(0,1.56f*scale,.27f),new Vector3(.22f,.07f,.05f),dark);
                if(!brute)
                {
                    var accent=Solid(imp?"Imp Copper Trim":"Guard Trim",imp?new Color(.5f,.16f,.06f):new Color(.34f,.43f,.39f),.55f,.45f);
                    Box(rig,"Helmet brow",new Vector3(0,1.85f,.28f),new Vector3(.59f,.08f,.09f),accent);
                    Box(rig,"Chin guard",new Vector3(0,1.5f,.25f),new Vector3(.42f,.09f,.14f),accent);
                    foreach(float side in new[]{-1f,1f})
                    {
                        Box(rig,"Visor housing",new Vector3(side*.25f,1.74f,.27f),new Vector3(.06f,.22f,.08f),dark);
                        Box(rig,"Chest plate edge",new Vector3(side*.32f,1.2f,.27f),new Vector3(.07f,.4f,.08f),accent);
                        Box(rig,"Belt pouch",new Vector3(side*.22f,.83f,.29f),new Vector3(.17f,.16f,.16f),accent);
                    }
                    if(!imp)
                    {
                        var mark=Solid("Guard Identification",new Color(.68f,.75f,.66f),.15f,.25f);
                        for(int stripe=0;stripe<3;stripe++)Box(rig,"Unit marking",new Vector3(-.17f+stripe*.045f,1.23f,.305f),new Vector3(.022f,.13f,.015f),mark);
                        Box(rig,"Helmet seam",new Vector3(0,1.965f,0),new Vector3(.045f,.025f,.46f),dark);
                    }
                }
                foreach(float side in new[]{-1f,1f})
                {
                    string limb=side<0?"Left":"Right";
                    var leg=new GameObject(limb+"Leg").transform;leg.SetParent(rig,false);leg.localPosition=new Vector3(side*width*.25f,.7f*scale,0);
                    Box(leg,"Thigh",new Vector3(0,-.14f,0),new Vector3(width*.32f,.34f,.36f),body);
                    Box(leg,"Knee",new Vector3(0,-.34f,.04f),new Vector3(width*.33f,.18f,.4f),dark);
                    Box(leg,"Shin",new Vector3(0,-.49f,0),new Vector3(width*.3f,.27f,.34f),body);
                    Box(leg,"Boot",new Vector3(0,-.64f,.09f),new Vector3(width*.4f,.16f,.56f),dark);
                    var arm=new GameObject(limb+"Arm").transform;arm.SetParent(rig,false);arm.localPosition=new Vector3(side*(width*.5f+.13f),1.43f*scale,0);
                    Box(arm,"Shoulder",Vector3.zero,new Vector3(brute?.47f:.34f,.35f,.52f),body);
                    Box(arm,"Forearm",new Vector3(side*.03f,-.32f,.12f),new Vector3(brute?.41f:.29f,.38f,.38f),body);
                    Box(arm,"Hand",new Vector3(0,-.47f,.26f),new Vector3(.25f,.22f,.24f),imp?body:dark);
                    if(imp) Box(rig,"Horn"+limb,new Vector3(side*.28f,2.03f,0),new Vector3(.13f,.37f,.15f),body,rotation:Quaternion.Euler(0,0,-side*24));
                }
                if(!imp && !brute){Box(rig,"Rifle",new Vector3(.1f,1.06f,.5f),new Vector3(.17f,.2f,.75f),dark); Box(rig,"RifleMuzzle",new Vector3(.1f,1.07f,.9f),new Vector3(.16f,.16f,.2f),body);}
            }
            var animation=root.AddComponent<Animation>(); animation.playAutomatically=false;
            foreach(string state in new[]{"Idle","Walk","Prepare","Attack","Pain","Death"})
            {
                Folder(Root+"/Animations"); string path=Root+"/Animations/"+archetype+"_"+state+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                bool created=!clip;
                {
                    if(created)clip=new AnimationClip();else clip.ClearCurves();
                    clip.name=state;clip.legacy=true;clip.frameRate=12;clip.wrapMode=state=="Idle"||state=="Walk"?WrapMode.Loop:WrapMode.ClampForever;
                    float duration=state=="Walk"?.64f:state=="Attack"?.4f:state=="Pain"?.2f:state=="Death"?.65f:1.2f;
                    AnimationCurve Curve(float start,float middle,float end)=>new AnimationCurve(new Keyframe(0,start),new Keyframe(duration*.5f,middle),new Keyframe(duration,end));
                    clip.SetCurve("Rig",typeof(Transform),"localPosition.y",Curve(0,state=="Walk"?.045f:state=="Death"?.18f:.018f,state=="Death"?.35f:0));
                    if(state=="Death")clip.SetCurve("Rig",typeof(Transform),"localEulerAnglesRaw.x",Curve(0,35,85));
                    if(!watcher)foreach(string limb in new[]{"Left","Right"})
                    {
                        float sign=limb=="Left"?1:-1;
                        clip.SetCurve("Rig/"+limb+"Leg",typeof(Transform),"localEulerAnglesRaw.x",state=="Walk"?Curve(sign*23,-sign*23,sign*23):Curve(0,0,0));
                        clip.SetCurve("Rig/"+limb+"Arm",typeof(Transform),"localEulerAnglesRaw.x",state=="Walk"?Curve(-sign*14,sign*14,-sign*14):state=="Attack"?Curve(-25,-65,-20):state=="Pain"?Curve(0,25,0):Curve(-12,-14,-12));
                    }
                    if(created)AssetDatabase.CreateAsset(clip,path);else EditorUtility.SetDirty(clip);
                }
                animation.AddClip(clip,state);
            }
            return root;
        }
    }
}
