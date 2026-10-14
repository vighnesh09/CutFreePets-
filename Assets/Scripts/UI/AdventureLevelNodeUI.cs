using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.Core;
using CutFreePets.Data;

namespace CutFreePets.UI
{
    /// <summary>
    /// Adventure Map Level Node (Candy Crush Style Winding Trail Node):
    /// - Tactile wooden medallion with golden outer rim
    /// - High-contrast bold level number for playable levels
    /// - Pulsating radiant golden aura on the CURRENT playable level
    /// - 3 mini star sockets showing stars achieved
    /// - Smooth squash-and-stretch tap feedback on press
    /// - Padlock icon with friendly shake animation when tapped while locked
    /// </summary>
    [DisallowMultipleComponent]
    public class AdventureLevelNodeUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button nodeButton;
        [SerializeField] private Image medallionImage;
        [SerializeField] private Text txtLevelNumber;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Image lockImage;
        [SerializeField] private GameObject starsContainer;
        [SerializeField] private Image[] starImages;
        [SerializeField] private GameObject currentPulseGlow;

        private int levelIndex = 1;
        private bool isUnlocked = false;
        private bool isCurrentLevel = false;
        private int starsEarned = 0;
        private Action<int> onSelectedCallback;
        private Action<string> onLockedTappedCallback;

        public int LevelIndex => levelIndex;
        public bool IsUnlocked => isUnlocked;
        public bool IsCurrentLevel => isCurrentLevel;

        public void Init(Button btn, Image medallion, Text numText, GameObject lockObj, Image lockImg, GameObject starsCont, Image[] stars, GameObject pulseGlow)
        {
            nodeButton = btn;
            medallionImage = medallion;
            txtLevelNumber = numText;
            lockOverlay = lockObj;
            lockImage = lockImg;
            starsContainer = starsCont;
            starImages = stars;
            currentPulseGlow = pulseGlow;
        }

        private Coroutine pulseCoroutine;
        private Coroutine actionCoroutine;

        public void Setup(
            int level,
            bool unlocked,
            bool isCurrent,
            int stars,
            Sprite unlockedSprite,
            Sprite lockedSprite,
            Sprite starFilled,
            Sprite starEmpty,
            Action<int> onSelected,
            Action<string> onLockedTapped)
        {
            levelIndex = level;
            isUnlocked = unlocked;
            isCurrentLevel = isCurrent;
            starsEarned = Mathf.Clamp(stars, 0, 3);
            onSelectedCallback = onSelected;
            onLockedTappedCallback = onLockedTapped;

            if (medallionImage != null)
            {
                medallionImage.sprite = isUnlocked ? unlockedSprite : lockedSprite;
            }

            if (txtLevelNumber != null)
            {
                txtLevelNumber.text = levelIndex.ToString();
                txtLevelNumber.gameObject.SetActive(true);
                
                // Crisp cartoon 3D typography:
                // Unlocked: Pure bright white (#FFFFFF) with dark warm chocolate drop shadow outline (#3D1E04)
                // Locked: Polished silvery cream (#F0F4F8) with deep charcoal-slate outline (#1B2228)
                txtLevelNumber.color = isUnlocked 
                    ? Color.white 
                    : new Color(0.94f, 0.96f, 0.98f);

                Outline ol = txtLevelNumber.GetComponent<Outline>();
                if (ol != null)
                {
                    ol.effectColor = isUnlocked 
                        ? new Color(0.24f, 0.12f, 0.02f, 0.95f) 
                        : new Color(0.11f, 0.13f, 0.16f, 0.95f);
                    ol.effectDistance = new Vector2(2f, -2.5f);
                }

                // Centered level number
                RectTransform numRect = txtLevelNumber.rectTransform;
                if (numRect != null)
                {
                    numRect.anchoredPosition = new Vector2(0f, 2f);
                    numRect.sizeDelta = new Vector2(130f, 80f);
                }
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(!isUnlocked);
            }

            if (starsContainer != null)
            {
                starsContainer.SetActive(isUnlocked);
            }

            // Fill Star badges
            if (isUnlocked && starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] != null)
                    {
                        starImages[i].sprite = (i < starsEarned) ? starFilled : starEmpty;
                        starImages[i].color = (i < starsEarned) ? Color.white : new Color(1f, 1f, 1f, 0.40f);
                    }
                }
            }

            // Pulse glow on current level
            if (currentPulseGlow != null)
            {
                currentPulseGlow.SetActive(isCurrentLevel);
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                    pulseCoroutine = null;
                }
                if (isCurrentLevel)
                {
                    pulseCoroutine = StartCoroutine(AnimatePulseGlow());
                }
            }

            if (nodeButton != null)
            {
                nodeButton.onClick.RemoveAllListeners();
                nodeButton.onClick.AddListener(OnNodeClicked);
            }
        }

        private void OnNodeClicked()
        {
            if (isUnlocked)
            {
                if (actionCoroutine != null) StopCoroutine(actionCoroutine);
                actionCoroutine = StartCoroutine(AnimateTapAndLaunch());
            }
            else
            {
                if (actionCoroutine != null) StopCoroutine(actionCoroutine);
                actionCoroutine = StartCoroutine(AnimateLockedShake());
                onLockedTappedCallback?.Invoke($"Complete Level {levelIndex - 1} to unlock this adventure!");
            }
        }

        private IEnumerator AnimateTapAndLaunch()
        {
            Transform tr = transform;
            Vector3 originalScale = Vector3.one;

            // Punch squash
            float duration = 0.10f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tr.localScale = Vector3.Lerp(originalScale, originalScale * 0.88f, t);
                yield return null;
            }

            // Punch bounce
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tr.localScale = Vector3.Lerp(originalScale * 0.88f, originalScale * 1.10f, t);
                yield return null;
            }

            tr.localScale = originalScale;
            onSelectedCallback?.Invoke(levelIndex);
        }

        private IEnumerator AnimateLockedShake()
        {
            Transform tr = transform;
            Vector3 originalPos = tr.localPosition;

            float duration = 0.30f;
            float elapsed = 0f;
            float frequency = 35f;
            float magnitude = 12f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                float xOffset = Mathf.Sin(elapsed * frequency) * magnitude * decay;
                tr.localPosition = originalPos + new Vector3(xOffset, 0f, 0f);
                yield return null;
            }

            tr.localPosition = originalPos;
        }

        private IEnumerator AnimatePulseGlow()
        {
            if (currentPulseGlow == null) yield break;
            Transform glowTr = currentPulseGlow.transform;
            Image glowImg = currentPulseGlow.GetComponent<Image>();

            while (isCurrentLevel)
            {
                float t = (Mathf.Sin(Time.time * 3.5f) + 1f) * 0.5f; // 0..1
                glowTr.localScale = Vector3.Lerp(new Vector3(1.0f, 1.0f, 1f), new Vector3(1.28f, 1.28f, 1f), t);
                if (glowImg != null)
                {
                    Color c = glowImg.color;
                    c.a = Mathf.Lerp(0.45f, 0.90f, t);
                    glowImg.color = c;
                }
                yield return null;
            }
        }
    }
}
