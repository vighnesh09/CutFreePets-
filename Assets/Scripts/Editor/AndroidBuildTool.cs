#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace CutFreePets.Editor
{
    public static class AndroidBuildTool
    {
        [MenuItem("Cut Free Pets/Build Android APK", priority = 100)]
        public static void BuildAndroidApk()
        {
            // 1. Ensure Android target is selected
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("<color=yellow>[Build]</color> Switching active build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // 2. Set Bundle Identifier & App Name
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.cutfreegames.cutfreepets");
            PlayerSettings.productName = "Cut Free Pets";
            PlayerSettings.companyName = "CutFreeGames";

            // 3. Set Mobile Portrait Orientation
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // 4. Ensure we build standalone APK (not Google Play App Bundle .aab)
            EditorUserBuildSettings.buildAppBundle = false;

            // 5. Gather Scenes in Build Settings
            string[] scenes = new string[]
            {
                "Assets/Scenes/HomeScreen.unity",
                "Assets/Scenes/LevelSelect.unity",
                "Assets/Scenes/SampleScene.unity"
            };

            // 6. Target Output Directory
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string buildsDir = Path.Combine(projectRoot, "Builds");
            if (!Directory.Exists(buildsDir))
            {
                Directory.CreateDirectory(buildsDir);
            }

            string apkPath = Path.Combine(buildsDir, "CutFreePets.apk");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"<color=cyan>[Build]</color> Starting Android APK build to: {apkPath} ...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[Build SUCCESS]</color> APK created! Size: {summary.totalSize / (1024 * 1024)} MB");
                EditorUtility.RevealInFinder(apkPath);
                EditorUtility.DisplayDialog(
                    "Android APK Build Succeeded!",
                    $"APK successfully generated at:\n{apkPath}\n\nSize: {summary.totalSize / (1024 * 1024)} MB\n\nYou can now transfer this file to your Android phone or share it!",
                    "Open Folder"
                );
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"<color=red>[Build FAILED]</color> Android build failed with {summary.totalErrors} errors.");
                EditorUtility.DisplayDialog("Build Failed", "Android APK build failed. Check the Console window for details.", "OK");
            }
        }
    }
}
#endif
