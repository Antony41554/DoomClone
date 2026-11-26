using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Ukubola.Release;
namespace Ukubola.EditorTools
{
    public static class UkubolaFinalBuild
    {
        public const string Version = "1.0.0";
        private static string Sha256(string path) { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        internal static void Package(string folder, string zip)
        {
            string temp = zip + ".partial"; try
            {
                using (var file = File.Create(temp)) using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    foreach (var path in Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                    {
                        string relative = path.Substring(folder.Length + 1).Replace('\\', '/'); var entry = archive.CreateEntry(relative, System.IO.Compression.CompressionLevel.Optimal); using (var input = File.OpenRead(path)) using (var output = entry.Open()) input.CopyTo(output);
                    }
                }
                using (var file = File.OpenRead(temp)) using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
                {
                    var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories); if (archive.Entries.Count != files.Length) throw new IOException("Archive entry count mismatch");
                    foreach (var path in files) { var relative = path.Substring(folder.Length + 1).Replace('\\', '/'); var entry = archive.GetEntry(relative); if (entry == null || entry.Length != new FileInfo(path).Length) throw new IOException("Archive entry mismatch: " + relative); using (var input = entry.Open()) using (var sha = SHA256.Create()) { var expected = Sha256(path); var actual = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant(); if (actual != expected) throw new IOException("Archive content mismatch: " + relative); } }
                }
                File.Move(temp, zip); File.WriteAllText(zip + ".sha256", Sha256(zip) + "  " + Path.GetFileName(zip) + Environment.NewLine);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        [MenuItem("Tools/UKUBOLA/Phase 15/Build Windows Final Release", false, 157)]
        public static void Build()
        {
            if (!UkubolaFinalSetup.CanRun()) return; string oldVersion = PlayerSettings.bundleVersion; string folder = null;
            try
            {
                UkubolaFinalSetup.Check(EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 && BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64), "Select Windows x64 in Build Profiles and install Windows Build Support");
                var art = AssetDatabase.LoadAssetAtPath<ReleaseArt>(UkubolaFinalSetup.ArtPath); UkubolaFinalSetup.ValidateAll(art);
                // Keep the existing company/product identifiers: changing them would relocate saves and PlayerPrefs.
                PlayerSettings.bundleVersion = Version;
                folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "UKUBOLA_" + Version + "_Windows_x64_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")); Directory.CreateDirectory(folder);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = UkubolaFinalSetup.Paths, target = BuildTarget.StandaloneWindows64, locationPathName = Path.Combine(folder, "UKUBOLA.exe"), options = BuildOptions.None });
                UkubolaFinalSetup.Check(report.summary.result == BuildResult.Succeeded && File.Exists(Path.Combine(folder, "UKUBOLA.exe")) && Directory.Exists(Path.Combine(folder, "UKUBOLA_Data")), "Windows build failed; inspect Console");
                File.WriteAllText(Path.Combine(folder, "README_JUGAR.txt"), "UKUBOLA " + Version + " - RETRO MODERN / Windows x64\r\n\r\nExtrae todo el ZIP antes de jugar. Ejecuta UKUBOLA.exe.\r\nWASD: caminar | Shift: correr | Ratón: girar | clic izquierdo: disparar\r\n1-7: armas | E o Espacio: usar | Tab: automapa | Esc: pausa\r\nSAVE GAME: guardar; CONTINUE: recuperar. Opciones: audio, sensibilidad, ventana y FX.\r\nSi hay bajo rendimiento, elige FX: CLASSIC. El modo ULTRA usa mayor resolución y postprocesado cuando HDR está disponible.\r\nNo es necesario instalar Unity. Conserva todos los archivos del ejecutable.\r\nRecursos visuales y audio originales generados para UKUBOLA; no incluye recursos de DOOM.\r\nGuardado local: AppData/LocalLow/<CompanyName>/<ProductName>/UKUBOLA/Saves.\r\n");
                File.WriteAllText(Path.Combine(folder, "BUILD_INFO.txt"), "UKUBOLA " + Version + "\r\nUnity: " + Application.unityVersion + "\r\nPlatform: Windows x64\r\nUTC: " + DateTime.UtcNow.ToString("O") + "\r\nScenes: " + string.Join(", ", UkubolaFinalSetup.Paths) + "\r\nBuild bytes: " + report.summary.totalSize + "\r\n");
                string zip = folder + ".zip"; Package(folder, zip);
                Debug.Log("[UKUBOLA P15] WINDOWS RELEASE SUCCEEDED | " + zip + " | SHA256 " + Sha256(zip)); EditorUtility.RevealInFinder(zip);
            }
            catch (Exception ex) { Debug.LogError("[UKUBOLA P15] BUILD/PACKAGE FAILED: " + ex + (folder != null ? "\nBuild folder: " + folder : "")); }
            finally { PlayerSettings.bundleVersion = oldVersion; }
        }
    }
}
