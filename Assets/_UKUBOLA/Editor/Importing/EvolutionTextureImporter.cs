using System;
using UnityEditor;
using UnityEngine;

namespace Ukubola.EditorTools
{
    public sealed class EvolutionTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(assetPath.StartsWith("Assets/_UKUBOLA/Art/Evolution/Textures/",StringComparison.Ordinal))Configure((TextureImporter)assetImporter,assetPath);
        }
        public static void Configure(TextureImporter importer,string path)
        {
            bool normal=path.EndsWith("_Normal.png",StringComparison.Ordinal),mask=path.EndsWith("_Mask.png",StringComparison.Ordinal);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal&&!mask;importer.filterMode=FilterMode.Point;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=512;
            importer.mipmapEnabled=path.Contains("_Base")||normal||mask||path.Contains("_Emission");
            importer.wrapMode=importer.mipmapEnabled?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.alphaIsTransparency=path.EndsWith("Digits.png",StringComparison.Ordinal)||path.EndsWith("Steam.png",StringComparison.Ordinal);
        }
    }
}
