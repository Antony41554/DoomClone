using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ukubola
{
    /// <summary>Explicit opt-in QA storage. Normal players retain their existing paths and PlayerPrefs.</summary>
    public static class QaEnvironment
    {
        [Serializable] private sealed class Preferences { public List<Value> values = new List<Value>(); }
        [Serializable] private sealed class Value { public string key; public float value; }
        private static string root;
        private static bool initialized;
        private static Preferences preferences;
        public static string Root { get { Initialize(); return root; } }
        public static bool Active => !string.IsNullOrEmpty(Root);
        public static string SaveFolder => Path.Combine(Root, "Saves");
        public static IDisposable Isolate(string absoluteRoot)
        {
            Initialize();
            if (!Path.IsPathRooted(absoluteRoot)) throw new ArgumentException("QA root must be absolute.");
            string candidate = Path.GetFullPath(absoluteRoot);
            string production = Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if ((candidate.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).StartsWith(production, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("QA storage must be outside production storage.");
            Directory.CreateDirectory(candidate);
            var scope = new Isolation(root, preferences);
            root = candidate; preferences = null;
            return scope;
        }
        private sealed class Isolation : IDisposable
        {
            private readonly string previousRoot; private readonly Preferences previousPreferences; private bool disposed;
            public Isolation(string value, Preferences values) { previousRoot = value; previousPreferences = values; }
            public void Dispose() { if (disposed) return; disposed = true; root = previousRoot; preferences = previousPreferences; }
        }
        public static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
        private static void Initialize()
        {
            if (initialized) return;
            root = Argument("-ukubola-qa-root") ?? Environment.GetEnvironmentVariable("UKUBOLA_QA_ROOT");
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(root))
            {
                string marker = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "UkubolaAutomation", "qa-root.txt");
                if (File.Exists(marker)) root = File.ReadAllText(marker).Trim();
            }
#endif
            if (string.IsNullOrWhiteSpace(root)) { root = null; initialized = true; return; }
            if (!Path.IsPathRooted(root)) throw new InvalidOperationException("QA root must be absolute.");
            root = Path.GetFullPath(root);
            if ((root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).StartsWith(Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QA root cannot be the production data directory.");
            Directory.CreateDirectory(root);
            initialized = true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { root = null; initialized = false; preferences = null; }
        private static Preferences Read()
        {
            if (preferences != null) return preferences;
            string path = Path.Combine(Root, "preferences.json");
            preferences = File.Exists(path) ? JsonUtility.FromJson<Preferences>(File.ReadAllText(path)) : new Preferences();
            if (preferences == null || preferences.values == null) preferences = new Preferences();
            return preferences;
        }
        public static float GetFloat(string key, float fallback)
        {
            if (!Active) return PlayerPrefs.GetFloat(key, fallback);
            var entry = Read().values.Find(v => v.key == key);
            return entry == null ? fallback : entry.value;
        }
        public static int GetInt(string key, int fallback) => Active ? Mathf.RoundToInt(GetFloat(key, fallback)) : PlayerPrefs.GetInt(key, fallback);
        public static void SetFloat(string key, float value)
        {
            if (!Active) { PlayerPrefs.SetFloat(key, value); return; }
            var entry = Read().values.Find(v => v.key == key);
            if (entry == null) { entry = new Value { key = key }; preferences.values.Add(entry); }
            entry.value = value;
        }
        public static void SetInt(string key, int value) { if (Active) SetFloat(key, value); else PlayerPrefs.SetInt(key, value); }
        public static void SavePreferences()
        {
            if (!Active) { PlayerPrefs.Save(); return; }
            File.WriteAllText(Path.Combine(Root, "preferences.json"), JsonUtility.ToJson(Read(), true));
        }
    }
}
