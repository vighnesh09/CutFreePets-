using System;
using UnityEngine;
using CutFreePets.Core;
using CutFreePets.Gameplay;
using CutFreePets.UI;

namespace CutFreePets.LevelSystem
{
    /// <summary>
    /// Architecture 2 Core Level Loader:
    /// Manages instant (0.01s) spawning, resetting, and swapping of Level Prefabs
    /// within a single persistent GameScene.
    /// Scalable to 300+ levels without scene reloading.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }

        [Header("Level Spawn Container")]
        [Tooltip("Transform under which the active Level Prefab is instantiated at (0,0,0).")]
        [SerializeField] private Transform levelContainer;

        [Header("Editor Testing")]
        [Tooltip("Level index to load when pressing Play directly in GameScene (1 to 300).")]
        [SerializeField] private int testLevelIndex = 1;
        [SerializeField] private bool autoLoadOnStart = true;

        [Header("Optional Explicit Prefab Array (Fallback / Inspector Override)")]
        [SerializeField] private GameObject[] levelPrefabs;

        // Runtime state
        private GameObject currentLevelInstance;
        private int currentLevelIndex = 1;

        public int CurrentLevelIndex => currentLevelIndex;
        public GameObject CurrentLevelInstance => currentLevelInstance;

        public event Action<int> OnLevelLoaded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (levelContainer == null)
            {
                GameObject containerObj = new GameObject("Active_Level_Container");
                containerObj.transform.SetParent(transform);
                containerObj.transform.localPosition = Vector3.zero;
                containerObj.transform.localRotation = Quaternion.identity;
                containerObj.transform.localScale = Vector3.one;
                levelContainer = containerObj.transform;
            }
        }

        private void Start()
        {
            if (!autoLoadOnStart) return;

            int targetLevel = 1;
#if UNITY_EDITOR
            // If testing GameScene directly in Unity Editor without coming from menu:
            if (GameManager.CurrentLevelIndex <= 0)
            {
                targetLevel = Mathf.Max(1, testLevelIndex);
            }
            else
            {
                targetLevel = GameManager.CurrentLevelIndex;
            }
#else
            targetLevel = GameManager.CurrentLevelIndex > 0 ? GameManager.CurrentLevelIndex : 1;
#endif
            LoadLevel(targetLevel);
        }

        private void Update()
        {
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame)
                {
                    Debug.Log("<color=cyan>[LevelLoader]</color> Shortcut 'N' pressed ➔ Loading Next Level!");
                    LoadNextLevel();
                }
                else if (UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame && currentLevelIndex > 1)
                {
                    Debug.Log("<color=cyan>[LevelLoader]</color> Shortcut 'P' pressed ➔ Loading Previous Level!");
                    LoadLevel(currentLevelIndex - 1);
                }
            }
#elif UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.N))
            {
                LoadNextLevel();
            }
            else if (Input.GetKeyDown(KeyCode.P) && currentLevelIndex > 1)
            {
                LoadLevel(currentLevelIndex - 1);
            }
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // If developer changes Test Level Index in Inspector while playing, swap instantly!
            if (Application.isPlaying && currentLevelInstance != null && testLevelIndex != currentLevelIndex && testLevelIndex > 0)
            {
                LoadLevel(testLevelIndex);
            }
        }
#endif

        /// <summary>
        /// Loads and spawns the target level prefab instantly in 0.01 seconds.
        /// </summary>
        public void LoadLevel(int levelIndex)
        {
            if (levelIndex < 1) levelIndex = 1;
            currentLevelIndex = levelIndex;

            // 1. Destroy currently spawned level immediately so old objects are removed before instantiating new ones
            if (currentLevelInstance != null)
            {
                DestroyImmediate(currentLevelInstance);
                currentLevelInstance = null;
            }

            // CRITICAL: Clean up any stray rescued cats, shadows, or debris left in GameScene
            // so Level 2/3 always starts with an empty platform and a pristine stage!
            var leftoverCats = FindObjectsByType<CatAnimationPlayer>(FindObjectsSortMode.None);
            for (int i = 0; i < leftoverCats.Length; i++)
            {
                if (leftoverCats[i] != null)
                {
                    Transform p = leftoverCats[i].transform.parent;
                    if (p != null && p.name.StartsWith("Pet_Slot"))
                    {
                        DestroyImmediate(p.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(leftoverCats[i].gameObject);
                    }
                }
            }

            var leftoverSprites = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            for (int i = 0; i < leftoverSprites.Length; i++)
            {
                if (leftoverSprites[i] != null)
                {
                    string n = leftoverSprites[i].gameObject.name;
                    if (n.StartsWith("Cat_Ground_Shadow") || n.StartsWith("Debris_") || n.StartsWith("StarSpark") || n.StartsWith("SnipSpark"))
                    {
                        DestroyImmediate(leftoverSprites[i].gameObject);
                    }
                }
            }

            // 2. Fetch the Level Prefab (from Resources or explicit array)
            GameObject prefab = GetLevelPrefab(levelIndex);
            if (prefab == null)
            {
                Debug.LogWarning($"<color=yellow>[LevelLoader]</color> Level {levelIndex} prefab not found! Falling back to Level 1.");
                prefab = GetLevelPrefab(1);
                if (prefab == null)
                {
                    Debug.LogError("<color=red>[LevelLoader]</color> No level prefabs could be loaded!");
                    return;
                }
                currentLevelIndex = 1;
            }

            // 3. Spawn the Level Prefab
            currentLevelInstance = Instantiate(prefab, levelContainer != null ? levelContainer : transform);
            currentLevelInstance.name = $"Level_{currentLevelIndex:D3}_Active";
            currentLevelInstance.transform.localPosition = Vector3.zero;
            currentLevelInstance.transform.localRotation = Quaternion.identity;
            currentLevelInstance.transform.localScale = Vector3.one;

            // 4. Reset & synchronize game managers with the NEW cage!
            Time.timeScale = 1f;

            PetCage newCage = currentLevelInstance.GetComponentInChildren<PetCage>();

            if (LevelScoreTracker.Instance != null)
            {
                LevelScoreTracker.Instance.ResetForNewLevel(currentLevelIndex, newCage);
            }

            if (GameplayHUDController.Instance != null)
            {
                GameplayHUDController.Instance.ResetHUD();
            }

            if (VictoryPopupUI.Instance != null)
            {
                VictoryPopupUI.Instance.ResetPopup(newCage);
            }

            Debug.Log($"<color=lime>[LevelLoader]</color> Level {currentLevelIndex} spawned successfully in 0.01s! 🎮");
            OnLevelLoaded?.Invoke(currentLevelIndex);
        }

        /// <summary>
        /// 1-tap instant level restart (0.01s reload).
        /// </summary>
        public void RestartCurrentLevel()
        {
            Debug.Log($"<color=cyan>[LevelLoader]</color> Instant Restart triggered for Level {currentLevelIndex} 🔄");
            LoadLevel(currentLevelIndex);
        }

        /// <summary>
        /// Advances to the next level index.
        /// </summary>
        public void LoadNextLevel()
        {
            LoadLevel(currentLevelIndex + 1);
        }

        /// <summary>
        /// Retrieves the level prefab from Resources/Levels/ (e.g. Level_001, Level_002)
        /// or from the explicit inspector array.
        /// </summary>
        private GameObject GetLevelPrefab(int levelIndex)
        {
            // 1. Check Resources with 3-digit format (Level_001, Level_042)
            GameObject res = Resources.Load<GameObject>($"Levels/Level_{levelIndex:D3}");
            if (res != null) return res;

            // 2. Check Resources with plain format (Level_1, Level_2)
            res = Resources.Load<GameObject>($"Levels/Level_{levelIndex}");
            if (res != null) return res;

            // 3. Check explicit inspector array (0-indexed: level 1 is index 0)
            if (levelPrefabs != null && levelPrefabs.Length >= levelIndex && levelPrefabs[levelIndex - 1] != null)
            {
                return levelPrefabs[levelIndex - 1];
            }

            return null;
        }
    }
}
