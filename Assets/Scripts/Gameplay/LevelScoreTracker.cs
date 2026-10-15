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
            currentLevel = CutFreePets.Core.GameManager.CurrentLevelIndex > 0 
                ? CutFreePets.Core.GameManager.CurrentLevelIndex 
                : currentLevel;
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

        private void HandleStarCollected(StarCollectible star)
        {
            starsCollectedThisRun = Mathf.Clamp(starsCollectedThisRun + 1, 0, 3);
            OnStarsChanged?.Invoke(starsCollectedThisRun);
            Debug.Log($"<color=gold>[LevelScoreTracker]</color> Star Collected! {starsCollectedThisRun}/3 Stars");
        }

        private void HandleCageLanded(Collision2D collision)
        {
            // Save to persistent progress
            SaveManager.SaveLevelResult(currentLevel, starsCollectedThisRun);
            int totalStars = SaveManager.GetTotalStars();

            OnLevelCompleted?.Invoke(currentLevel, starsCollectedThisRun, totalStars);
            Debug.Log($"<color=cyan>[LevelScoreTracker]</color> LEVEL {currentLevel} WON! Stars: {starsCollectedThisRun}/3 | Total Stars: {totalStars}");
        }

        public void SetCurrentLevel(int levelIndex)
        {
            currentLevel = levelIndex;
            starsCollectedThisRun = 0;
            OnStarsChanged?.Invoke(0);
        }
    }
}
