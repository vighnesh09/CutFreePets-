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
    /// Master controller for the Candy Crush style Adventure Map Level Screen:
    /// - Continuous vertical winding organic trail through the forest
    /// - 30 interactive tactile wooden medallion level nodes (supports infinite scaling)
    /// - Real-time progress tracking & star badges (0..3) with SaveManager integration
    /// - Distinct pulsating golden halo on the current playable level
    /// - Stepping stones generated along the winding curve between consecutive levels
    /// - Floating compass/target button to instantly focus on current playable level
    /// - Fixed header bar with circular carved back button, screen title, and total stars badge
    /// - Toast banner for locked level feedback
    /// </summary>
    [DisallowMultipleComponent]
    public class AdventureMapController : MonoBehaviour
    {
        [Header("Header Navigation")]
        [SerializeField] private Button btnBack;
        [SerializeField] private Text txtTitle;
        [SerializeField] private Text txtTotalStars;
        [SerializeField] private Button btnFocusCurrent;

        [Header("Map Scroll & Content")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private Transform pathContainer;
        [SerializeField] private Transform nodesContainer;

        [Header("Toast Message Banner")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text txtToastMessage;

        [Header("Configuration")]
        [SerializeField] private int totalLevels = 30;
        [SerializeField] private float verticalSpacing = 200f; // vertical distance between levels
        [SerializeField] private float horizontalAmplitude = 270f; // path curve wave width

        [Header("Sprites")]
        [SerializeField] private Sprite nodeUnlockedSprite;
        [SerializeField] private Sprite nodeLockedSprite;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private Sprite lockIconSprite;
        [SerializeField] private Sprite steppingStoneSprite;
        [SerializeField] private Sprite currentGlowSprite;

        private List<AdventureLevelNodeUI> createdNodes = new List<AdventureLevelNodeUI>();
        private Coroutine toastCoroutine;
        private int currentPlayableLevel = 1;

        private void Awake()
        {
            SaveManager.Load();

            if (btnBack != null)
            {
                btnBack.onClick.AddListener(OnBackClicked);
            }

            if (btnFocusCurrent != null)
            {
                btnFocusCurrent.onClick.AddListener(() => FocusOnLevel(currentPlayableLevel, true));
            }

            if (toastPanel != null)
            {
                toastPanel.SetActive(false);
            }
        }

        private void Start()
        {
            BuildAdventureMap();
            UpdateTotalStarsDisplay();

            // Auto-focus camera on current playable level on open
            StartCoroutine(DelayedInitialFocus());
        }

        private IEnumerator DelayedInitialFocus()
        {
            yield return new WaitForEndOfFrame();
            FocusOnLevel(currentPlayableLevel, false);
        }

        public void BuildAdventureMap()
        {
            if (nodesContainer == null || pathContainer == null || mapContent == null) return;

            // Clear old children safely in both Edit mode and Play mode
            for (int i = nodesContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = nodesContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            for (int i = pathContainer.childCount - 1; i >= 0; i--)
            {
                GameObject child = pathContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            createdNodes.Clear();

            var progress = SaveManager.Progress;
            currentPlayableLevel = Mathf.Clamp(progress.highestUnlockedLevel, 1, totalLevels);

            // Total height needed for map content
            float bottomPadding = 260f;
            float topPadding = 380f;
            float totalHeight = bottomPadding + (totalLevels - 1) * verticalSpacing + topPadding;
            mapContent.sizeDelta = new Vector2(mapContent.sizeDelta.x, totalHeight);

            // Ensure containers align with mapContent bottom-up coordinate space
            RectTransform nrt = nodesContainer.GetComponent<RectTransform>();
            if (nrt != null)
            {
                nrt.anchorMin = new Vector2(0.5f, 0f);
                nrt.anchorMax = new Vector2(0.5f, 0f);
                nrt.pivot = new Vector2(0.5f, 0f);
                nrt.anchoredPosition = Vector2.zero;
                nrt.sizeDelta = mapContent.sizeDelta;
            }

            RectTransform prt = pathContainer.GetComponent<RectTransform>();
            if (prt != null)
            {
                prt.anchorMin = new Vector2(0.5f, 0f);
                prt.anchorMax = new Vector2(0.5f, 0f);
                prt.pivot = new Vector2(0.5f, 0f);
                prt.anchoredPosition = Vector2.zero;
                prt.sizeDelta = mapContent.sizeDelta;
            }

            // 1. Calculate positions for each level node
            List<Vector2> levelPositions = new List<Vector2>();
            for (int i = 0; i < totalLevels; i++)
            {
                int lvlNum = i + 1;
                // Organic winding wave pattern (Candy Crush style sine wave with organic phase)
                float angle = i * 0.95f;
                float x = Mathf.Sin(angle) * horizontalAmplitude;
                float y = bottomPadding + (i * verticalSpacing);
                levelPositions.Add(new Vector2(x, y));
            }

            // 2. Spawn Stepping Stones connecting consecutive levels
            int stonesPerSegment = 5;
            for (int i = 0; i < totalLevels - 1; i++)
            {
                Vector2 p0 = levelPositions[i];
                Vector2 p1 = levelPositions[i + 1];

                for (int s = 1; s <= stonesPerSegment; s++)
                {
                    float t = s / (float)(stonesPerSegment + 1);
                    float cx = Mathf.Lerp(p0.x, p1.x, t);
                    float cy = Mathf.Lerp(p0.y, p1.y, t);

                    // Add slight natural curve offset
                    float curveOffset = Mathf.Sin(t * Mathf.PI) * 20f;
                    Vector2 stonePos = new Vector2(cx + curveOffset, cy);

                    SpawnSteppingStone(stonePos, (i + 1) < currentPlayableLevel);
                }
            }

            // 3. Spawn Level Nodes
            for (int i = 0; i < totalLevels; i++)
            {
                int lvlNum = i + 1;
                var record = SaveManager.GetRecord(lvlNum);
                bool unlocked = (lvlNum == 1) || record.isUnlocked || (lvlNum <= progress.highestUnlockedLevel);
                bool isCurrent = (lvlNum == currentPlayableLevel);
                int stars = record.starsEarned;

                CreateLevelNode(lvlNum, levelPositions[i], unlocked, isCurrent, stars);
            }
        }

        private void SpawnSteppingStone(Vector2 pos, bool isCompletedPath)
        {
            GameObject stone = new GameObject("SteppingStone");
            stone.transform.SetParent(pathContainer, false);
            RectTransform rt = stone.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(48f, 48f);

            Image img = stone.AddComponent<Image>();
            img.sprite = steppingStoneSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            // Completed trail is illuminated warm cream; upcoming trail is soft stone
            img.color = isCompletedPath ? new Color(1f, 0.96f, 0.82f, 0.95f) : new Color(0.82f, 0.78f, 0.72f, 0.80f);
        }

        private GameObject CreateLevelNode(int levelNum, Vector2 pos, bool unlocked, bool isCurrent, int stars)
        {
            GameObject nodeObj = new GameObject($"Node_Level_{levelNum:D2}");
            nodeObj.transform.SetParent(nodesContainer, false);

            RectTransform nRect = nodeObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.5f, 0f);
            nRect.anchorMax = new Vector2(0.5f, 0f);
            nRect.pivot = new Vector2(0.5f, 0.5f);
            nRect.anchoredPosition = pos;
            nRect.sizeDelta = new Vector2(170f, 170f);

            // Pulsating Aura Glow behind current level
            GameObject glowObj = new GameObject("Current_Glow");
            glowObj.transform.SetParent(nodeObj.transform, false);
            RectTransform gRect = glowObj.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0.5f, 0.5f);
            gRect.anchorMax = new Vector2(0.5f, 0.5f);
            gRect.pivot = new Vector2(0.5f, 0.5f);
            gRect.anchoredPosition = Vector2.zero;
            gRect.sizeDelta = new Vector2(260f, 260f);
            Image glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = currentGlowSprite;
            glowImg.preserveAspect = true;
            glowImg.raycastTarget = false;
            glowImg.color = new Color(1f, 0.88f, 0.25f, 0.85f);
            glowObj.SetActive(isCurrent);

            // Medallion Image
            Image medallionImg = nodeObj.AddComponent<Image>();
            medallionImg.sprite = unlocked ? nodeUnlockedSprite : nodeLockedSprite;
            medallionImg.preserveAspect = true;
            medallionImg.raycastTarget = true;

            // Button
            Button btn = nodeObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1f, 0.96f, 0.88f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = cb;

            // Level Number Text
            GameObject numObj = new GameObject("Text_Number");
            numObj.transform.SetParent(nodeObj.transform, false);
            RectTransform numRect = numObj.AddComponent<RectTransform>();
            numRect.anchoredPosition = new Vector2(0f, 4f);
            numRect.sizeDelta = new Vector2(120f, 80f);

            Text nTxt = numObj.AddComponent<Text>();
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nTxt.fontSize = 58;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.color = new Color(0.32f, 0.18f, 0.05f); // Rich dark chocolate oak
            nTxt.alignment = TextAnchor.MiddleCenter;
            nTxt.raycastTarget = false;
            nTxt.text = levelNum.ToString();

            // Stars Container (above the medallion node)
            GameObject starsObj = new GameObject("Stars_Container");
            starsObj.transform.SetParent(nodeObj.transform, false);
            RectTransform sRect = starsObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 92f);
            sRect.sizeDelta = new Vector2(140f, 44f);

            HorizontalLayoutGroup hlg = starsObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 4f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Image[] starImgs = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                GameObject sIcon = new GameObject($"Star_{s + 1}");
                sIcon.transform.SetParent(starsObj.transform, false);
                RectTransform siRect = sIcon.AddComponent<RectTransform>();
                // Arch arrangement: middle star slightly larger
                float starSize = (s == 1) ? 36f : 30f;
                siRect.sizeDelta = new Vector2(starSize, starSize);

                Image simg = sIcon.AddComponent<Image>();
                simg.preserveAspect = true;
                simg.raycastTarget = false;
                starImgs[s] = simg;
            }

            // Lock Overlay (shown when locked)
            GameObject lockObj = new GameObject("Lock_Overlay");
            lockObj.transform.SetParent(nodeObj.transform, false);
            RectTransform lRect = lockObj.AddComponent<RectTransform>();
            lRect.anchoredPosition = Vector2.zero;
            lRect.sizeDelta = new Vector2(85f, 85f);

            Image lockImg = lockObj.AddComponent<Image>();
            lockImg.sprite = lockIconSprite;
            lockImg.preserveAspect = true;
            lockImg.raycastTarget = false;

            // Wire AdventureLevelNodeUI
            AdventureLevelNodeUI nodeUI = nodeObj.AddComponent<AdventureLevelNodeUI>();
            nodeUI.Init(btn, medallionImg, nTxt, lockObj, lockImg, starsObj, starImgs, glowObj);
            nodeUI.Setup(levelNum, unlocked, isCurrent, stars, nodeUnlockedSprite, nodeLockedSprite, starFilledSprite, starEmptySprite, OnLevelSelected, ShowLockedMessage);

            createdNodes.Add(nodeUI);
            return nodeObj;
        }

        private void OnLevelSelected(int levelIndex)
        {
            Debug.Log($"<color=lime>[AdventureMap]</color> Launching Level {levelIndex}!");
            GameManager.LoadLevel(levelIndex);
        }

        private void ShowLockedMessage(string message)
        {
            if (toastPanel == null || txtToastMessage == null) return;

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(AnimateToast(message));
        }

        private IEnumerator AnimateToast(string message)
        {
            txtToastMessage.text = message;
            toastPanel.SetActive(true);
            Transform tr = toastPanel.transform;
            tr.localScale = new Vector3(0.8f, 0.8f, 1f);

            // Pop in
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, elapsed / 0.15f);
                yield return null;
            }

            yield return new WaitForSeconds(2.2f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.8f, 0.8f, 1f), elapsed / 0.2f);
                yield return null;
            }
            toastPanel.SetActive(false);
        }

        public void FocusOnLevel(int levelIndex, bool smooth = true)
        {
            if (scrollRect == null || mapContent == null) return;

            int targetIndex = Mathf.Clamp(levelIndex - 1, 0, totalLevels - 1);
            float totalScrollable = mapContent.rect.height - scrollRect.viewport.rect.height;
            if (totalScrollable <= 0) return;

            float bottomPadding = 260f;
            float targetY = bottomPadding + (targetIndex * verticalSpacing);
            // Center the target in the viewport
            float viewportHalf = scrollRect.viewport.rect.height * 0.5f;
            float desiredScrollY = Mathf.Clamp(targetY - viewportHalf, 0f, totalScrollable);
            float normalizedPos = desiredScrollY / totalScrollable;

            if (smooth)
            {
                StopCoroutine("SmoothScrollRoutine");
                StartCoroutine(SmoothScrollRoutine(normalizedPos));
            }
            else
            {
                scrollRect.verticalNormalizedPosition = normalizedPos;
            }
        }

        private IEnumerator SmoothScrollRoutine(float targetNormalizedPos)
        {
            float start = scrollRect.verticalNormalizedPosition;
            float duration = 0.45f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, targetNormalizedPos, t);
                yield return null;
            }
            scrollRect.verticalNormalizedPosition = targetNormalizedPos;
        }

        private void OnBackClicked()
        {
            Debug.Log("<color=cyan>[AdventureMap]</color> Returning to Home Screen...");
            GameManager.LoadHomeScreen();
        }

        public void UpdateTotalStarsDisplay()
        {
            if (txtTotalStars != null)
            {
                int currentStars = SaveManager.GetTotalStars();
                int maxStars = totalLevels * 3;
                txtTotalStars.text = $"{currentStars} / {maxStars}";
            }
        }
    }
}
