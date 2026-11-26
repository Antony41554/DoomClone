using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Ukubola.EditorTools
{
    public static class UkubolaMvpBuild
    {
        [MenuItem("Tools/UKUBOLA/Phase 11/Build Windows MVP",false,115)]
        public static void Build()
        {
            if(!UkubolaMvpSetup.CanRun())return;
            try
            {
                if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64||!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))
                    throw new InvalidOperationException("Activate Windows x86_64 in File > Build Profiles and install Windows Build Support if needed.");
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                UkubolaMvpValidator.CheckBuildList();
                UkubolaMvpValidator.CheckMission(EditorSceneManager.OpenScene(UkubolaMvpSetup.Mission,OpenSceneMode.Single));
                UkubolaMvpValidator.CheckTitle(EditorSceneManager.OpenScene(UkubolaMvpSetup.Title,OpenSceneMode.Single));
                string parent=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Builds");
                string folder=Path.Combine(parent,"UKUBOLA_MVP_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
                if(Directory.Exists(folder))throw new IOException("Build folder already exists: "+folder);Directory.CreateDirectory(folder);
                var options=new BuildPlayerOptions{scenes=new[]{UkubolaMvpSetup.Title,UkubolaMvpSetup.Mission},target=BuildTarget.StandaloneWindows64,locationPathName=Path.Combine(folder,"UKUBOLA.exe"),options=BuildOptions.None};
                var report=BuildPipeline.BuildPlayer(options);
                if(report==null||report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Windows build did not succeed. See Console. Output: "+folder);
                Debug.Log("[UKUBOLA P11] WINDOWS BUILD SUCCEEDED | "+options.locationPathName);EditorUtility.RevealInFinder(folder);
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P11] BUILD FAILED: "+ex);}
        }
    }
}
