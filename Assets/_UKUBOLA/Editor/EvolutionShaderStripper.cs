using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;

namespace Ukubola.EditorTools
{
    // This scope is enabled only by the one-scene art preview builder. Campaign builds
    // must declare their own feature contract; they never inherit these exclusions.
    public sealed class EvolutionShaderStripper : IPreprocessShaders
    {
        public static int Removed {get=>SessionState.GetInt("UKUBOLA.Shaders.Removed",0);private set=>SessionState.SetInt("UKUBOLA.Shaders.Removed",value);}
        public static int Kept {get=>SessionState.GetInt("UKUBOLA.Shaders.Kept",0);private set=>SessionState.SetInt("UKUBOLA.Shaders.Kept",value);}
        public static int ForwardVertex=>SessionState.GetInt("UKUBOLA.Shaders.ForwardVertex",0);
        public static int ForwardFragment=>SessionState.GetInt("UKUBOLA.Shaders.ForwardFragment",0);
        public static void ResetCounts(){Removed=Kept=0;SessionState.SetInt("UKUBOLA.Shaders.ForwardVertex",0);SessionState.SetInt("UKUBOLA.Shaders.ForwardFragment",0);}
        private static readonly string[] MaterialKeywords={"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP","_EMISSION"};
        private static HashSet<int> materialMasks=new HashSet<int>();
        public static string MaterialMasks
        {
            get{string value=SessionState.GetString("UKUBOLA.Shaders.MaterialMasks","");return value.Length>0?value:File.Exists(UkubolaAutomation.ArtShaderMarker)?File.ReadAllText(UkubolaAutomation.ArtShaderMarker):"";}
        }
        public static void ConfigureMaterials(IEnumerable<Material> materials)
        {
            materialMasks.Clear();
            foreach(var material in materials.Where(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"))
            {int mask=0;for(int i=0;i<MaterialKeywords.Length;i++)if(material.IsKeywordEnabled(MaterialKeywords[i]))mask|=1<<i;materialMasks.Add(mask);}
            SessionState.SetString("UKUBOLA.Shaders.MaterialMasks",string.Join(",",materialMasks.OrderBy(x=>x)));
            Directory.CreateDirectory(UkubolaAutomation.Queue);File.WriteAllText(UkubolaAutomation.ArtShaderMarker,string.Join(",",materialMasks.OrderBy(x=>x)));
        }
        public int callbackOrder => int.MaxValue;
        private static readonly string[] Unused = {
            "DYNAMICLIGHTMAP_ON",
            "_SCREEN_SPACE_REFLECTION","_SCREEN_SPACE_IRRADIANCE","_DBUFFER_MRT1","_DBUFFER_MRT2","_DBUFFER_MRT3",
        };
        public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> variants)
        {
            if(!UkubolaAutomation.ArtPreviewBuildActive)return;
            if(materialMasks.Count==0)foreach(string value in MaterialMasks.Split(','))if(int.TryParse(value,out int parsedMask))materialMasks.Add(parsedMask);
            var keywords=Unused.Select(shader.keywordSpace.FindKeyword).Where(k=>k.isValid).ToArray();
            bool filterMaterials=shader.name=="Universal Render Pipeline/Lit"&&snippet.passName=="ForwardLit"&&snippet.shaderType==ShaderType.Fragment&&materialMasks.Count>0;
            var local=filterMaterials?MaterialKeywords.Select(shader.keywordSpace.FindKeyword).ToArray():null;
            int before=variants.Count;
            for(int i=variants.Count-1;i>=0;i--)
            {
                int mask=0;if(filterMaterials)for(int bit=0;bit<local.Length;bit++)if(local[bit].isValid&&variants[i].shaderKeywordSet.IsEnabled(local[bit]))mask|=1<<bit;
                if(keywords.Any(k=>variants[i].shaderKeywordSet.IsEnabled(k))||filterMaterials&&!materialMasks.Contains(mask))variants.RemoveAt(i);
            }
            Removed+=before-variants.Count;Kept+=variants.Count;
            if(shader.name=="Universal Render Pipeline/Lit"&&snippet.passName=="ForwardLit")
            {
                if(snippet.shaderType==ShaderType.Vertex)SessionState.SetInt("UKUBOLA.Shaders.ForwardVertex",ForwardVertex+variants.Count);
                if(snippet.shaderType==ShaderType.Fragment)SessionState.SetInt("UKUBOLA.Shaders.ForwardFragment",ForwardFragment+variants.Count);
            }
            if(shader.name=="Universal Render Pipeline/Lit"&&snippet.passName=="ForwardLit")Debug.Log("[UKUBOLA shader] "+snippet.passName+" / "+snippet.shaderType+" : "+before+" -> "+variants.Count+"; local mask filter="+filterMaterials+"; masks="+MaterialMasks);
        }
    }
}
