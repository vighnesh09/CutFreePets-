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
        public const string GAME_SCENE_NAME = "GameScene";
        public const string LEVEL_SCENE_PREFIX = "Level_";

        public static int CurrentLevelIndex { get; set; } = 0;

        static GameManager()
        {
            MobileOptimizationBootstrapper.InitializeMobileSettings();
        }

        /// <summary>
        /// Loads the designated Level (e.g. Level 1).
        /// If GameScene (Architecture 2) is active, swaps level prefab in 0.01s.
        /// Otherwise loads GameScene or falls back to legacy scenes gracefully.
        /// </summary>
        public static void LoadLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;

            // 1. If GameScene is already active, instant 0.01s swap!
            if (SceneManager.GetActiveScene().name == GAME_SCENE_NAME)
            {
                if (LevelSystem.LevelLoader.Instance != null)
                {
                    LevelSystem.LevelLoader.Instance.LoadLevel(levelIndex);
                    return;
                }
            }

            // 2. If GameScene is available in build, load GameScene
            if (Application.CanStreamedLevelBeLoaded(GAME_SCENE_NAME))
            {
                SceneManager.LoadScene(GAME_SCENE_NAME);
                return;
            }

            // 3. Fallback to legacy scene if GameScene not in build
            string targetScene = $"{LEVEL_SCENE_PREFIX}{levelIndex}";
            if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else if (Application.CanStreamedLevelBeLoaded("SampleScene"))
            {
                SceneManager.LoadScene("SampleScene");
            }
            else if (SceneManager.sceneCountInBuildSettings > 1)
            {
                SceneManager.LoadScene(1);
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
        /// Restarts the currently active level (0.01s instant restart in Architecture 2).
        /// </summary>
        public static void RestartCurrentLevel()
        {
            if (LevelSystem.LevelLoader.Instance != null)
            {
                LevelSystem.LevelLoader.Instance.RestartCurrentLevel();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
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
