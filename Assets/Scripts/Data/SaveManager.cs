using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutFreePets.Data
{
    [Serializable]
    public class LevelProgressRecord
    {
        public int levelIndex;
        public bool isUnlocked;
        public bool isCompleted;
        public int starsEarned; // 0, 1, 2, or 3
        public float bestTime;
    }

    [Serializable]
    public class PlayerGameProgress
    {
        public int highestUnlockedLevel = 1;
        public int totalStarsCollected = 0;
        public List<LevelProgressRecord> levels = new List<LevelProgressRecord>();
    }

    /// <summary>
    /// Professional persistent progress manager.
    /// Tracks level completion, stars earned per level (0..3), and total stars.
    /// Auto-saves to PlayerPrefs via JSON.
    /// </summary>
    public static class SaveManager
    {
        private const string SAVE_KEY = "CutFreePets_PlayerProgress_v1";
        private static PlayerGameProgress cachedProgress;

        public static event Action<int, int> OnLevelScoreUpdated; // levelIndex, starsEarned

        public static PlayerGameProgress Progress
        {
            get
            {
                if (cachedProgress == null) Load();
                return cachedProgress;
            }
        }

        public static void Load()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    cachedProgress = JsonUtility.FromJson<PlayerGameProgress>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] Corrupt save data, resetting: {e.Message}");
                    cachedProgress = CreateDefaultProgress();
                }
            }

            if (cachedProgress == null)
            {
                cachedProgress = CreateDefaultProgress();
            }
        }

        public static void Save()
        {
            if (cachedProgress == null) return;
            string json = JsonUtility.ToJson(cachedProgress);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private static PlayerGameProgress CreateDefaultProgress()
        {
            PlayerGameProgress p = new PlayerGameProgress
            {
                highestUnlockedLevel = 1,
                totalStarsCollected = 0,
                levels = new List<LevelProgressRecord>()
            };

            // Pre-seed 30 levels
            for (int i = 1; i <= 30; i++)
            {
                p.levels.Add(new LevelProgressRecord
                {
                    levelIndex = i,
                    isUnlocked = (i == 1),
                    isCompleted = false,
                    starsEarned = 0,
                    bestTime = 0f
                });
            }

            return p;
        }

        public static LevelProgressRecord GetRecord(int levelIndex)
        {
            var p = Progress;
            var record = p.levels.Find(l => l.levelIndex == levelIndex);
            if (record == null)
            {
                record = new LevelProgressRecord
                {
                    levelIndex = levelIndex,
                    isUnlocked = (levelIndex <= p.highestUnlockedLevel),
                    isCompleted = false,
                    starsEarned = 0,
                    bestTime = 0f
                };
                p.levels.Add(record);
            }
            return record;
        }

        public static int GetStars(int levelIndex)
        {
            return GetRecord(levelIndex).starsEarned;
        }

        public static int GetTotalStars()
        {
            int total = 0;
            foreach (var rec in Progress.levels)
            {
                total += rec.starsEarned;
            }
            Progress.totalStarsCollected = total;
            return total;
        }

        /// <summary>
        /// Records completion of a level with the star count (0 to 3).
        /// Preserves the highest star record achieved for that level.
        /// Unlocks the next level automatically.
        /// </summary>
        public static void SaveLevelResult(int levelIndex, int starsEarned, float completionTime = 0f)
        {
            var record = GetRecord(levelIndex);
            record.isCompleted = true;

            // Keep highest stars earned
            if (starsEarned > record.starsEarned)
            {
                record.starsEarned = Mathf.Clamp(starsEarned, 0, 3);
            }

            if (completionTime > 0f && (record.bestTime <= 0f || completionTime < record.bestTime))
            {
                record.bestTime = completionTime;
            }

            // Unlock next level
            int nextLevel = levelIndex + 1;
            if (nextLevel > Progress.highestUnlockedLevel)
            {
                Progress.highestUnlockedLevel = nextLevel;
            }
            var nextRecord = GetRecord(nextLevel);
            nextRecord.isUnlocked = true;

            GetTotalStars(); // recalculate total
            Save();

            OnLevelScoreUpdated?.Invoke(levelIndex, record.starsEarned);
            Debug.Log($"<color=gold>[SaveManager]</color> Level {levelIndex} completed with {starsEarned} Stars! Total Stars: {Progress.totalStarsCollected}");
        }

        public static void ResetAllProgress()
        {
            cachedProgress = CreateDefaultProgress();
            Save();
            Debug.Log("<color=red>[SaveManager]</color> All player progress reset.");
        }
    }
}
