#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CutFreePets.Gameplay;
using CutFreePets.Rope;

namespace CutFreePets.Editor
{
    public static class PrefabExportUtility
    {
        [MenuItem("Cut Free Pets/Export Core Prefabs from Scene", false, 10)]
        public static void ExportCorePrefabs()
        {
            // Ensure directories exist
            EnsureFolderExists("Assets/Prefabs/Gameplay");
            EnsureFolderExists("Assets/Prefabs/Rope");

            // 1. Export PetCage Prefab
            GameObject cageObj = GameObject.Find("Pet_Cage");
            if (cageObj != null)
            {
                string cagePath = "Assets/Prefabs/Gameplay/Pet_Cage.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(cageObj, cagePath, InteractionMode.AutomatedAction);
                Debug.Log($"<color=green>[PrefabExport]</color> Saved Pet_Cage to {cagePath}");
            }
            else
            {
                Debug.LogWarning("[PrefabExport] 'Pet_Cage' GameObject not found in current scene.");
            }

            // 2. Export Ground_Platform Prefab
            GameObject platformObj = GameObject.Find("Ground_Platform");
            if (platformObj != null)
            {
                string platformPath = "Assets/Prefabs/Gameplay/Ground_Platform.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(platformObj, platformPath, InteractionMode.AutomatedAction);
                Debug.Log($"<color=green>[PrefabExport]</color> Saved Ground_Platform to {platformPath}");
            }
            else
            {
                Debug.LogWarning("[PrefabExport] 'Ground_Platform' GameObject not found in current scene.");
            }

            // 3. Export TopMiddle_Hanger Prefab
            GameObject hangerObj = GameObject.Find("TopMiddle_Hanger");
            if (hangerObj != null)
            {
                string hangerPath = "Assets/Prefabs/Rope/Rope_Hanger.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(hangerObj, hangerPath, InteractionMode.AutomatedAction);
                Debug.Log($"<color=green>[PrefabExport]</color> Saved Rope_Hanger to {hangerPath}");
            }
            else
            {
                Debug.LogWarning("[PrefabExport] 'TopMiddle_Hanger' GameObject not found in current scene.");
            }

            // 4. Export Star_Collectible Prefab
            GameObject starObj = GameObject.Find("Star_Collectible_01") ?? GameObject.Find("Star_Collectible");
            if (starObj != null)
            {
                string starPath = "Assets/Prefabs/Gameplay/Star_Collectible.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(starObj, starPath, InteractionMode.AutomatedAction);
                Debug.Log($"<color=green>[PrefabExport]</color> Saved Star_Collectible to {starPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Core Prefabs Exported",
                "Successfully converted existing scene objects into clean, reusable Prefabs in Assets/Prefabs/!\n\n" +
                "• Assets/Prefabs/Gameplay/Pet_Cage.prefab\n" +
                "• Assets/Prefabs/Gameplay/Ground_Platform.prefab\n" +
                "• Assets/Prefabs/Gameplay/Star_Collectible.prefab\n" +
                "• Assets/Prefabs/Rope/Rope_Hanger.prefab",
                "Awesome"
            );
        }

        private static void EnsureFolderExists(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
#endif
