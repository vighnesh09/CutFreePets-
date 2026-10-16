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
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.cutfreegames.cutfreepets");
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

            // 4b. Ensure Active Input Handling is set to 1 (Input System Package)
            // In Unity 6 Android Player, setting "Both" (2) triggers a fatal build exception.
            try
            {
                var settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (settingsAssets != null && settingsAssets.Length > 0)
                {
                    SerializedObject playerSettingsObj = new SerializedObject(settingsAssets[0]);
                    SerializedProperty inputProp = playerSettingsObj.FindProperty("activeInputHandler");
                    if (inputProp != null && inputProp.intValue != 1)
                    {
                        Debug.Log($"<color=yellow>[Build]</color> Setting activeInputHandler from {inputProp.intValue} to 1 (Input System Package) for Android build compatibility.");
                        inputProp.intValue = 1;
                        playerSettingsObj.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Build] Note on activeInputHandler check: {ex.Message}");
            }

            // 5. Gather All Scenes Dynamically (Ensure Level 1, Level 2, Level 3 and all future levels are included)
            System.Collections.Generic.List<string> sceneList = new System.Collections.Generic.List<string>();

            // Always Home first (index 0) so the APK launches directly into HomeScreen
            string homeScene = "Assets/Scenes/HomeScreen.unity";
            if (File.Exists(homeScene)) sceneList.Add(homeScene);

            // Level Select second (index 1)
            string levelSelectScene = "Assets/Scenes/LevelSelect.unity";
            if (File.Exists(levelSelectScene)) sceneList.Add(levelSelectScene);

            // Level 1
            string level1Scene = "Assets/Scenes/Level_1.unity";
            if (File.Exists(level1Scene)) sceneList.Add(level1Scene);

            // Numbered Levels (Level_2, Level_3, etc.)
            for (int i = 2; i <= 30; i++)
            {
                string levelPath = $"Assets/Scenes/Level_{i}.unity";
                if (File.Exists(levelPath) && !sceneList.Contains(levelPath))
                {
                    sceneList.Add(levelPath);
                }
            }

            // Include SampleScene for backward compatibility / fallback
            string sampleScene = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(sampleScene) && !sceneList.Contains(sampleScene))
            {
                sceneList.Add(sampleScene);
            }

            // Any other enabled scenes from EditorBuildSettings
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path) && !sceneList.Contains(s.path))
                {
                    sceneList.Add(s.path);
                }
            }

            // Synchronize EditorBuildSettings so the Editor's Build Settings window always stays 100% updated
            System.Collections.Generic.List<EditorBuildSettingsScene> buildSettingsScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var p in sceneList)
            {
                buildSettingsScenes.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = buildSettingsScenes.ToArray();

            Debug.Log($"<color=cyan>[Build]</color> Packaging {sceneList.Count} scenes into Android APK: {string.Join(", ", sceneList)}");

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
                scenes = sceneList.ToArray(),
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
