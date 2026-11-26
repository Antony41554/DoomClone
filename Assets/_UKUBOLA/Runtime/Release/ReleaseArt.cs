using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ukubola.Release
{
    // Serialized references prevent runtime Resources lookups and include shaders in player builds.
    public sealed class ReleaseArt : ScriptableObject
    {
        public Texture2D[] Textures;
        public Sprite[] Sprites;
        public AudioClip[] Music, Effects, Weapons;
        public Shader Surface, Particle;
        public VolumeProfile Post;
        public string[] SceneStamps;
        private Dictionary<string,Texture2D> textures;
        private Dictionary<string,Sprite> sprites;
        private void OnEnable(){textures=null;sprites=null;}
        public Texture2D Texture(string key){if(textures==null){textures=new Dictionary<string,Texture2D>(StringComparer.Ordinal);foreach(var t in Textures)if(t)textures[t.name]=t;}return textures.TryGetValue(key,out var value)?value:null;}
        public Sprite Sprite(string key){if(sprites==null){sprites=new Dictionary<string,Sprite>(StringComparer.Ordinal);foreach(var s in Sprites)if(s)sprites[s.name]=s;}return sprites.TryGetValue(key,out var value)?value:null;}
    }
    public static class ReleaseQuality
    {
        private const string Key="UKUBOLA.P15.Quality";
        public const int Default=1;
        public static int Normalize(int value)=>Mathf.Clamp(value,0,2);
        public static int Read()=>Normalize(PlayerPrefs.GetInt(Key,Default));
        public static void Write(int value){PlayerPrefs.SetInt(Key,Normalize(value));PlayerPrefs.Save();}
        public static Vector2Int Size(int q)=>Normalize(q)==0?new Vector2Int(320,200):Normalize(q)==1?new Vector2Int(480,300):new Vector2Int(640,400);
        public static string Label(int q)=>Normalize(q)==0?"CLASSIC":Normalize(q)==1?"MODERN":"ULTRA";
        public static int LightLimit(int q)=>Normalize(q)==0?4:8;
        public static int ParticleLimit(int q)=>Normalize(q)==0?24:Normalize(q)==1?48:64;
    }
}
