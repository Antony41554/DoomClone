using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
namespace Ukubola.Persistence
{
    public static class SaveStore
    {
        [Serializable] private sealed class Envelope { public string payload,sha256; }
        private const int MaxBytes=1024*1024;
        public static string Folder=>Path.Combine(Application.persistentDataPath,"UKUBOLA","Saves");
        public static string FilePath=>Path.Combine(Folder,"campaign-v1.json");
        public static SaveData Pending {get;set;}
        public static bool AutoSavePending {get;set;}
        public static string Notice {get;set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void Reset(){Pending=null;AutoSavePending=false;Notice=null;}
        public static string Hash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        public static string Encode(SaveData value)
        {
            if(value==null)throw new ArgumentNullException(nameof(value));if(!value.Validate(out var reason))throw new InvalidDataException(reason);
            string payload=JsonUtility.ToJson(value);return JsonUtility.ToJson(new Envelope{payload=payload,sha256=Hash(payload)},true);
        }
        public static SaveData Decode(string json)
        {
            if(string.IsNullOrEmpty(json)||Encoding.UTF8.GetByteCount(json)>MaxBytes)throw new InvalidDataException("Invalid save size");
            var envelope=JsonUtility.FromJson<Envelope>(json);
            if(envelope==null||string.IsNullOrEmpty(envelope.payload)||envelope.sha256!=Hash(envelope.payload))throw new InvalidDataException("Save checksum mismatch");
            var data=JsonUtility.FromJson<SaveData>(envelope.payload);if(data==null)throw new InvalidDataException("Missing save data");if(!data.Validate(out var reason))throw new InvalidDataException(reason);return data;
        }
        private static SaveData Read(string path){var info=new FileInfo(path);if(!info.Exists||info.Length<=0||info.Length>MaxBytes)throw new InvalidDataException("Missing or oversized save");return Decode(File.ReadAllText(path,Encoding.UTF8));}
        public static bool TryRead(string path,out SaveData data,out bool backup,out string reason)
        {
            data=null;backup=false;reason="NO SAVED GAME";
            if(File.Exists(path)){try{data=Read(path);reason=null;return true;}catch(Exception e){reason=e.Message;}}
            if(File.Exists(path+".bak")){try{data=Read(path+".bak");backup=true;reason=null;return true;}catch(Exception e){reason=e.Message;}}
            return false;
        }
        public static bool Exists(string path)=>File.Exists(path)||File.Exists(path+".bak");
        public static void Write(string path,SaveData value)
        {
            string json=Encode(value);if(Encoding.UTF8.GetByteCount(json)>MaxBytes)throw new InvalidDataException("Save too large");
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp-"+Guid.NewGuid().ToString("N");
            try
            {
                byte[] bytes=new UTF8Encoding(false).GetBytes(json);
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                Read(temp); // Verify the actual written bytes before replacing a valid slot.
                if(File.Exists(path))
                {
                    bool valid;try{Read(path);valid=true;}catch{valid=false;}
                    // Never overwrite a good backup with a corrupt primary.
                    File.Replace(temp,path,valid?path+".bak":null);
                }
                else File.Move(temp,path);
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
