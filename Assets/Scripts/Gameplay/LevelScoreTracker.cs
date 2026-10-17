using System;
using System.Collections.Generic;
using UnityEngine;
using CutFreePets.Data;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Core gameplay manager that tracks:
    /// - Current level index (1 to 30)
    /// - Realtime stars collected in current run (0 to 3)
    /// - Level completion on cage landing
    /// - SaveManager integration for permanent progress & total stars
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelScoreTracker : MonoBehaviour
    {
        public static LevelScoreTracker Instance { get; private set; }

        [Header("Current Level")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int starsCollectedThisRun = 0;

        public int CurrentLevel => currentLevel;
        public int StarsCollectedThisRun => starsCollectedThisRun;

        public event Action<int> OnStarsChanged; // current star count (0..3)
        public event Action<int, int, int> OnLevelCompleted; // levelIndex, starsEarned, totalStars

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SaveManager.Load();
            string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (activeSceneName.StartsWith("Level_") && int.TryParse(activeSceneName.Substring(6), out int parsedLevel))
            {
                currentLevel = parsedLevel;
            }
            else if (CutFreePets.Core.GameManager.CurrentLevelIndex > 0)
            {
                currentLevel = CutFreePets.Core.GameManager.CurrentLevelIndex;
            }
            starsCollectedThisRun = 0;
        }

        private void OnEnable()
        {
            StarCollectible.OnStarCollected += HandleStarCollected;
            
            // Listen to PetCage landing
            PetCage cage = FindAnyObjectByType<PetCage>();
            if (cage != null)
            {
                cage.OnLandedOnGoal += HandleCageLanded;
            }
        }

        private void Start()
        {
            // Safety hook in case PetCage was instantiated after OnEnable
            PetCage cage = FindAnyObjectByType<PetCage>();
            if (cage != null)
            {
                cage.OnLandedOnGoal -= HandleCageLanded;
                cage.OnLandedOnGoal += HandleCageLanded;
            }
        }

        private void OnDisable()
        {
            StarCollectible.OnStarCollected -= HandleStarCollected;

            PetCage cage = FindAnyObjectByType<PetCage>();
            if (cage != null)
            {
                cage.OnLandedOnGoal -= HandleCageLanded;
            }
        }

        private bool isLevelFinished = false;

        private void HandleStarCollected(StarCollectible star)
        {
            starsCollectedThisRun = Mathf.Clamp(starsCollectedThisRun + 1, 0, 3);
            OnStarsChanged?.Invoke(starsCollectedThisRun);
            Debug.Log($"<color=gold>[LevelScoreTracker]</color> Star Collected! {starsCollectedThisRun}/3 Stars");
        }

        private void HandleCageLanded(Collision2D collision)
        {
            NotifyCageLanded();
        }

        /// <summary>
        /// Guaranteed fail-safe trigger for level completion called directly by PetCage or its events.
        /// </summary>
        public void NotifyCageLanded()
        {
            if (isLevelFinished) return;
            isLevelFinished = true;

            // Save to persistent progress
            SaveManager.SaveLevelResult(currentLevel, starsCollectedThisRun);
            int totalStars = SaveManager.GetTotalStars();

            OnLevelCompleted?.Invoke(currentLevel, starsCollectedThisRun, totalStars);
            Debug.Log($"<color=cyan>[LevelScoreTracker]</color> LEVEL {currentLevel} WON! Stars: {starsCollectedThisRun}/3 | Total Stars: {totalStars}");

            // Direct trigger to VictoryPopupUI as fail-safe backup
            if (CutFreePets.UI.VictoryPopupUI.Instance != null)
            {
                CutFreePets.UI.VictoryPopupUI.Instance.Show(starsCollectedThisRun);
            }
        }

        public void SetCurrentLevel(int levelIndex)
        {
            currentLevel = levelIndex;
            starsCollectedThisRun = 0;
            isLevelFinished = false;
            OnStarsChanged?.Invoke(0);
        }

        public void ResetForNewLevel(int levelIndex, PetCage explicitCage = null)
        {
            SetCurrentLevel(levelIndex);

            PetCage cage = explicitCage != null ? explicitCage : FindAnyObjectByType<PetCage>();
            if (cage != null)
            {
                cage.OnLandedOnGoal -= HandleCageLanded;
                cage.OnLandedOnGoal += HandleCageLanded;
            }
        }
    }
}
