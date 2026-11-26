using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace Ukubola.EditorTools
{
    /// <summary>Only these declared outputs belong to the evolution generator.</summary>
    public static class EvolutionManifest
    {
        public const string Path = UkubolaEvolutionArt.Root + "/GENERATION_MANIFEST.json";
        [Serializable] private sealed class Entry { public string path, sha256; }
        [Serializable] private sealed class Manifest { public int version=1; public string generator="UkubolaEvolutionScene + UkubolaEvolutionArt", unity; public string[] parameters; public Entry[] normalizedSources; public Entry[] files; }
        private static bool Owned(string p) => p.StartsWith(UkubolaEvolutionArt.Root+"/",StringComparison.Ordinal)||p==UkubolaEvolutionScene.RoomPath||p==UkubolaEvolutionScene.RoomPath+".meta"||p.StartsWith("Assets/_UKUBOLA/Scenes/Evolution/ArtRoom/",StringComparison.Ordinal)||p==UkubolaEvolutionScene.SlicePath||p==UkubolaEvolutionScene.SlicePath+".meta"||p.StartsWith("Assets/_UKUBOLA/Scenes/Evolution/OutpostSlice/",StringComparison.Ordinal);
        public static void Validate()
        {
            if(!File.Exists(Path))return;
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path));
            if(manifest==null||manifest.version!=1||manifest.files==null)throw new InvalidOperationException("Invalid generation manifest.");
            foreach(var file in manifest.files)
                if(!Owned(file.path)||!File.Exists(file.path)||UkubolaAutomation.HashFile(file.path)!=file.sha256)
                    throw new InvalidOperationException("Generated output was changed or removed; preserving all outputs: "+file.path);
        }
        public static void Record()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var files=Directory.GetFiles(UkubolaEvolutionArt.Root,"*",SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/_UKUBOLA/Scenes/Evolution","*",SearchOption.AllDirectories))
                .Select(p=>p.Replace('\\','/')).Where(p=>Owned(p)&&p!=Path&&p!=Path+".meta"&&!p.Contains(".staging.")).OrderBy(p=>p,StringComparer.Ordinal);
            var sources=new[]{"Assets/_UKUBOLA/Editor/UkubolaEvolutionArt.cs","Assets/_UKUBOLA/Editor/UkubolaEvolutionScene.cs","Assets/_UKUBOLA/Editor/UkubolaEvolutionSlice.cs","Assets/_UKUBOLA/Editor/UkubolaPixelSigns.cs","Assets/_UKUBOLA/Editor/Importing/EvolutionTextureImporter.cs","Tools/Generate-UkubolaArt.py"};
            var manifest=new Manifest{unity=Application.unityVersion,parameters=new[]{"grid=.25m","world=640x360 medium","art seed=1600..2005","room=18x28m","slice=18x51m","lightmaps=8 texels/m 512","characters=Guard,Imp","source hashes=UTF8 without BOM, LF"},normalizedSources=sources.Select(p=>new Entry{path=p,sha256=Ukubola.Persistence.SaveStore.Hash(File.ReadAllText(p).Replace("\r\n","\n"))}).ToArray(),files=files.Select(p=>new Entry{path=p,sha256=UkubolaAutomation.HashFile(p)}).ToArray()};
            File.WriteAllText(Path,JsonUtility.ToJson(manifest,true));
        }
        public static void RestoreKnownBuildPrefilters()
        {
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path));
            const string pattern=@"(?m)^  (?<key>m_Prefilter\w+|m_Light2DPrefilteringMode):[^\r\n]*";
            var original=Regex.Matches(File.ReadAllText("Assets/Settings/PC_RPAsset.asset"),pattern).Cast<Match>().ToDictionary(m=>m.Groups["key"].Value,m=>m.Value);
            var source=new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/PC_RPAsset.asset"));
            foreach(var file in manifest.files)
            {
                if(File.Exists(file.path)&&UkubolaAutomation.HashFile(file.path)==file.sha256)continue;
                if(!Regex.IsMatch(file.path,@"^Assets/_UKUBOLA/Generated/Evolution/Rendering/Pipeline_[012]\.asset$"))throw new InvalidOperationException("Unexpected changed output; preserving it: "+file.path);
                string candidate=Regex.Replace(File.ReadAllText(file.path),pattern,m=>original[m.Groups["key"].Value]);
                if(Ukubola.Persistence.SaveStore.Hash(candidate)!=file.sha256)throw new InvalidOperationException("Changes exceed known URP build prefilter metadata; preserving "+file.path);
                var destination=new SerializedObject(AssetDatabase.LoadMainAssetAtPath(file.path));
                foreach(string key in original.Keys)destination.CopyFromSerializedProperty(source.FindProperty(key));
                destination.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.SaveAssets();Validate();
        }
    }
}
