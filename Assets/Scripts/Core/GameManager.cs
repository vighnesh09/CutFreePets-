using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutFreePets.Core
{
    /// <summary>
    /// Central game navigation and level loader manager.
    /// Manages scene transitions between Home Screen and Levels,
    /// and makes adding future levels (Level 2, 3, etc.) effortless.
    /// </summary>
    public static class GameManager
    {
        public const string HOME_SCENE_NAME = "HomeScreen";
        public const string LEVEL_SELECT_SCENE_NAME = "LevelSelect";
        public const string LEVEL_SCENE_PREFIX = "Level_";

        public static int CurrentLevelIndex { get; private set; } = 1;

        static GameManager()
        {
            MobileOptimizationBootstrapper.InitializeMobileSettings();
        }

        /// <summary>
        /// Loads the designated Level (e.g. Level 1).
        /// If a specific scene "Level_X" does not exist yet in Build Settings,
        /// gracefully falls back to SampleScene or the current active level scene.
        /// </summary>
        public static void LoadLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            string targetScene = $"{LEVEL_SCENE_PREFIX}{levelIndex}";

            if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else if (Application.CanStreamedLevelBeLoaded("SampleScene"))
            {
                SceneManager.LoadScene("SampleScene");
            }
            else
            {
                // Fallback to scene index 1
                if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    SceneManager.LoadScene(1);
                }
            }
        }

        /// <summary>
        /// Loads the Level Select / Grid screen.
        /// </summary>
        public static void LoadLevelSelect()
        {
            if (Application.CanStreamedLevelBeLoaded(LEVEL_SELECT_SCENE_NAME))
            {
                SceneManager.LoadScene(LEVEL_SELECT_SCENE_NAME);
            }
            else
            {
                LoadHomeScreen();
            }
        }

        /// <summary>
        /// Loads the Home Screen menu.
        /// </summary>
        public static void LoadHomeScreen()
        {
            if (Application.CanStreamedLevelBeLoaded(HOME_SCENE_NAME))
            {
                SceneManager.LoadScene(HOME_SCENE_NAME);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        /// <summary>
        /// Restarts the currently active scene.
        /// </summary>
        public static void RestartCurrentLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Advances to the next level index.
        /// </summary>
        public static void LoadNextLevel()
        {
            LoadLevel(CurrentLevelIndex + 1);
        }

        /// <summary>
        /// Quits the application cleanly.
        /// </summary>
        public static void QuitGame()
        {
            Debug.Log("<color=red>[GameManager]</color> Quitting application...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
