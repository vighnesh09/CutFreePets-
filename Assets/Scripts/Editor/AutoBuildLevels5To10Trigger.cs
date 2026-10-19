#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CutFreePets.Editor
{
    /// <summary>
    /// Automatic one-shot trigger that runs BatchBuildLevels(5, 10) on editor compile
    /// if any of the target level prefabs are not yet generated.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoBuildLevels5To10Trigger
    {
        static AutoBuildLevels5To10Trigger()
        {
            EditorApplication.delayCall += CheckAndBuild;
        }

        private static void CheckAndBuild()
        {
            bool anyMissing = false;
            for (int i = 5; i <= 10; i++)
            {
                string resPath = $"Assets/Resources/Levels/Level_{i:D3}.prefab";
                if (!File.Exists(resPath))
                {
                    anyMissing = true;
                    break;
                }
            }

            if (anyMissing)
            {
                Debug.Log("<color=cyan>[AutoBuildLevels5To10Trigger]</color> Triggering batch build for Levels 5 to 10...");
                BibleLevelGenerator.BatchBuildLevels(5, 10);
            }
        }
    }
}
#endif
