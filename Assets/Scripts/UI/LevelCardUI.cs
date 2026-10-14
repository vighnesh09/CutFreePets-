using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.Core;
using CutFreePets.Data;

namespace CutFreePets.UI
{
    /// <summary>
    /// Individual Level Card in the Level Select Grid:
    /// - Displays Level Number (e.g. "1", "2")
    /// - Displays 3 star sockets (filled or empty based on player record)
    /// - Shows golden padlock icon & darkened overlay if locked
    /// - Interactive punch bounce animation on tap
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelCardUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Text txtLevelNumber;
        [SerializeField] private GameObject starsContainer;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private GameObject lockOverlay;

        private int levelIndex = 1;
        private bool isUnlocked = false;
        private int starsEarned = 0;

        public void Init(Button btn, Image bg, Text lvlText, GameObject starsCont, Image[] stars, GameObject lockObj)
        {
            cardButton = btn;
            cardBackground = bg;
            txtLevelNumber = lvlText;
            starsContainer = starsCont;
            starImages = stars;
            lockOverlay = lockObj;
        }

        public void Setup(int level, bool unlocked, int stars, Sprite filledStar, Sprite emptyStar, Action<int> onSelected)
        {
            levelIndex = level;
            isUnlocked = unlocked;
            starsEarned = Mathf.Clamp(stars, 0, 3);
            starFilledSprite = filledStar;
            starEmptySprite = emptyStar;

            if (txtLevelNumber != null)
            {
                txtLevelNumber.text = levelIndex.ToString();
                txtLevelNumber.gameObject.SetActive(isUnlocked);
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isUnlocked);
            }

            if (starsContainer != null)
            {
                starsContainer.SetActive(isUnlocked);
            }

            // Fill stars
            if (isUnlocked && starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] != null)
                    {
                        starImages[i].sprite = (i < starsEarned) ? starFilledSprite : starEmptySprite;
                        starImages[i].color = (i < starsEarned) ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                    }
                }
            }

            if (cardButton != null)
            {
                cardButton.interactable = isUnlocked;
                cardButton.onClick.RemoveAllListeners();
                if (isUnlocked)
                {
                    cardButton.onClick.AddListener(() =>
                    {
                        StartCoroutine(AnimateTapAndPlay(onSelected));
                    });
                }
            }
        }

        private IEnumerator AnimateTapAndPlay(Action<int> onSelected)
        {
            Transform tr = transform;
            Vector3 originalScale = tr.localScale;

            // Squash & stretch punch
            float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(1f, 0.90f, t);
                tr.localScale = originalScale * scale;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0.90f, 1.05f, t);
                tr.localScale = originalScale * scale;
                yield return null;
            }

            tr.localScale = originalScale;
            onSelected?.Invoke(levelIndex);
        }
    }
}
