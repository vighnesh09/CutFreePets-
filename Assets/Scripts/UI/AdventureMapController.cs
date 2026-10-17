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
        private List<Vector2> levelPositions = new List<Vector2>();
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

            EnsureSpritesLoaded();
        }

        private void EnsureSpritesLoaded()
        {
#if UNITY_EDITOR
            if (nodeUnlockedSprite == null)
                nodeUnlockedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Node_Wood_Unlocked.png");
            if (nodeLockedSprite == null)
                nodeLockedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Node_Wood_Locked.png");
            if (starFilledSprite == null)
                starFilledSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");
            if (starEmptySprite == null)
                starEmptySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Star_Empty.png");
            if (lockIconSprite == null)
                lockIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Level_Lock_Icon.png");
            if (steppingStoneSprite == null)
                steppingStoneSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Path_SteppingStone.png");
            if (currentGlowSprite == null)
                currentGlowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Node_Current_Glow.png");
#endif
        }

        private void Start()
        {
            EnsureSpritesLoaded();
            BuildAdventureMap();
            UpdateTotalStarsDisplay();

            // Auto-focus camera on current playable level on open
            StartCoroutine(DelayedInitialFocus());
        }

        private IEnumerator DelayedInitialFocus()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            FocusOnLevel(currentPlayableLevel, false);
        }

        public void BuildAdventureMap()
        {
            EnsureSpritesLoaded();
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

            // Total height matching clean 1080x6800 map texture
            float totalHeight = 6800f;
            mapContent.sizeDelta = new Vector2(1080f, totalHeight);

            // Clamp scroll bounds so no empty background or camera backdrop ever appears
            if (scrollRect != null)
            {
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            // Sync background image size if present
            Transform bgChild = mapContent.Find("Adventure_Background");
            if (bgChild != null)
            {
                RectTransform bgRt = bgChild.GetComponent<RectTransform>();
                if (bgRt != null)
                {
                    bgRt.sizeDelta = Vector2.zero;
                    bgRt.anchorMin = Vector2.zero;
                    bgRt.anchorMax = Vector2.one;
                }
                Image bgImage = bgChild.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.raycastTarget = false;
                }
            }

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

            // 1. Precise road-calibrated positions for each of the 30 levels (matching the clean background road)
            Vector2[] calibratedPositions = new Vector2[]
            {
                new Vector2(   68.0f,   260.0f), // Level 01 (road entrance center)
                new Vector2(  -35.0f,   460.0f), // Level 02 (curves towards left-center)
                new Vector2( -165.0f,   660.0f), // Level 03 (deep left bend near snowshoes/sign)
                new Vector2(  -45.0f,   860.0f), // Level 04 (winding back to center)
                new Vector2( -120.0f,  1060.0f), // Level 05 (left turn between pines)
                new Vector2(    0.0f,  1260.0f), // Level 06 (center path near ice pond)
                new Vector2( -185.0f,  1460.0f), // Level 07 (left path around ice lake)
                new Vector2( -175.0f,  1660.0f), // Level 08 (lake embankment)
                new Vector2(  -14.0f,  1860.0f), // Level 09 (transition out of winter)
                new Vector2(  -37.0f,  2060.0f), // Level 10 (summer gateway)
                new Vector2(  -66.0f,  2260.0f), // Level 11 (entering flower meadow)
                new Vector2(   16.0f,  2460.0f), // Level 12 (path center)
                new Vector2(  -20.0f,  2660.0f), // Level 13 (natural trail leading to bridge)
                new Vector2(  115.0f,  2860.0f), // Level 14 (turn towards wooden bridge)
                new Vector2(   30.0f,  3060.0f), // Level 15 (riverbank bridge entrance)
                new Vector2(    0.0f,  3260.0f), // Level 16 (on the wooden bridge)
                new Vector2(   10.0f,  3460.0f), // Level 17 (bridge exit onto trail)
                new Vector2( -105.0f,  3660.0f), // Level 18 (campsite bend)
                new Vector2(   95.0f,  3860.0f), // Level 19 (waterfall curve)
                new Vector2(   65.0f,  4060.0f), // Level 20 (river trail)
                new Vector2(   45.0f,  4260.0f), // Level 21 (foot of the mountain)
                new Vector2(  128.5f,  4460.0f), // Level 22 (mountain pass right)
                new Vector2(   47.0f,  4660.0f), // Level 23 (mountain pass center)
                new Vector2( -169.5f,  4860.0f), // Level 24 (cliff dodge left)
                new Vector2(  112.5f,  5060.0f), // Level 25 (switchback right)
                new Vector2(  106.0f,  5260.0f), // Level 26 (alpine ascent)
                new Vector2(  -87.5f,  5460.0f), // Level 27 (crystal crag)
                new Vector2(   51.5f,  5660.0f), // Level 28 (summit stairs approach)
                new Vector2(  -93.0f,  5860.0f), // Level 29 (final staircase)
                new Vector2(   39.0f,  6060.0f)  // Level 30 (star altar summit)
            };

            levelPositions.Clear();
            for (int i = 0; i < totalLevels; i++)
            {
                if (i < calibratedPositions.Length)
                {
                    levelPositions.Add(calibratedPositions[i]);
                }
                else
                {
                    float angle = i * 0.95f;
                    float x = Mathf.Sin(angle) * horizontalAmplitude;
                    float y = 260.0f + (i * verticalSpacing);
                    levelPositions.Add(new Vector2(x, y));
                }
            }

            // 2. Spawn Stepping Stones connecting consecutive levels along smooth road tangents
            int stonesPerSegment = 4;
            for (int i = 0; i < totalLevels - 1; i++)
            {
                Vector2 p0 = levelPositions[i];
                Vector2 p1 = levelPositions[i + 1];

                // Compute tangents for natural organic road curve
                Vector2 prevP = (i > 0) ? levelPositions[i - 1] : p0 - (p1 - p0);
                Vector2 nextP = (i < totalLevels - 2) ? levelPositions[i + 2] : p1 + (p1 - p0);
                Vector2 m0 = (p1 - prevP) * 0.5f;
                Vector2 m1 = (nextP - p0) * 0.5f;

                for (int s = 1; s <= stonesPerSegment; s++)
                {
                    // Distribute stones evenly along trail between node boundaries
                    float t = Mathf.Lerp(0.24f, 0.76f, (float)(s - 1) / (stonesPerSegment - 1));
                    float t2 = t * t;
                    float t3 = t2 * t;

                    // Cubic Hermite Spline calculation
                    float h00 = 2 * t3 - 3 * t2 + 1;
                    float h10 = t3 - 2 * t2 + t;
                    float h01 = -2 * t3 + 3 * t2;
                    float h11 = t3 - t2;

                    Vector2 stonePos = h00 * p0 + h10 * m0 + h01 * p1 + h11 * m1;
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
            rt.sizeDelta = new Vector2(52f, 52f);

            Image img = stone.AddComponent<Image>();
            img.sprite = steppingStoneSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            // Completed trail is illuminated warm glowing cream; upcoming trail is subtle cobblestone
            img.color = isCompletedPath 
                ? new Color(1f, 0.98f, 0.88f, 0.95f) 
                : new Color(1f, 1f, 1f, 0.70f);
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
            nRect.sizeDelta = new Vector2(160f, 150f);

            // Pulsating Aura Glow behind current level
            GameObject glowObj = new GameObject("Current_Glow");
            glowObj.transform.SetParent(nodeObj.transform, false);
            RectTransform gRect = glowObj.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0.5f, 0.5f);
            gRect.anchorMax = new Vector2(0.5f, 0.5f);
            gRect.pivot = new Vector2(0.5f, 0.5f);
            gRect.anchoredPosition = Vector2.zero;
            gRect.sizeDelta = new Vector2(240f, 230f);
            Image glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = currentGlowSprite;
            glowImg.preserveAspect = true;
            glowImg.raycastTarget = false;
            glowImg.color = new Color(1f, 0.88f, 0.25f, 0.85f);
            glowObj.SetActive(isCurrent);

            // Medallion / 3D Pedestal Button Image
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

            // Level Number Text (Centered on the top face of the 3D button)
            GameObject numObj = new GameObject("Text_Number");
            numObj.transform.SetParent(nodeObj.transform, false);
            RectTransform numRect = numObj.AddComponent<RectTransform>();
            numRect.anchoredPosition = new Vector2(0f, -8f);
            numRect.sizeDelta = new Vector2(110f, 70f);

            Text nTxt = numObj.AddComponent<Text>();
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nTxt.fontSize = 54;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.color = Color.white;
            nTxt.alignment = TextAnchor.MiddleCenter;
            nTxt.raycastTarget = false;
            nTxt.text = levelNum.ToString();

            // Outline for crisp 3D embossed look
            Outline outline = numObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.12f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2.5f);

            // 3-Star Crown (Arching proudly over top of the pedestal button, exactly like the reference!)
            GameObject starsObj = new GameObject("Stars_Container");
            starsObj.transform.SetParent(nodeObj.transform, false);
            RectTransform sRect = starsObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 48f);
            sRect.sizeDelta = new Vector2(140f, 52f);

            // Individual Star Placement in an arch:
            // Star 1 (Left): x = -36, y = -6, size = 38x38
            // Star 2 (Middle): x = 0, y = 6, size = 46x46 (highest & biggest)
            // Star 3 (Right): x = 36, y = -6, size = 38x38
            Image[] starImgs = new Image[3];
            Vector2[] starOffsets = new Vector2[] { new Vector2(-36f, -6f), new Vector2(0f, 6f), new Vector2(36f, -6f) };
            float[] starSizes = new float[] { 38f, 46f, 38f };

            for (int s = 0; s < 3; s++)
            {
                GameObject sIcon = new GameObject($"Star_{s + 1}");
                sIcon.transform.SetParent(starsObj.transform, false);
                RectTransform siRect = sIcon.AddComponent<RectTransform>();
                siRect.anchorMin = new Vector2(0.5f, 0.5f);
                siRect.anchorMax = new Vector2(0.5f, 0.5f);
                siRect.pivot = new Vector2(0.5f, 0.5f);
                siRect.anchoredPosition = starOffsets[s];
                siRect.sizeDelta = new Vector2(starSizes[s], starSizes[s]);

                Image simg = sIcon.AddComponent<Image>();
                simg.preserveAspect = true;
                simg.raycastTarget = false;
                starImgs[s] = simg;
            }

            // Lock Overlay (shown when locked - badge at upper right of button)
            GameObject lockObj = new GameObject("Lock_Overlay");
            lockObj.transform.SetParent(nodeObj.transform, false);
            RectTransform lRect = lockObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 0.5f);
            lRect.anchorMax = new Vector2(0.5f, 0.5f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.anchoredPosition = new Vector2(38f, 28f);
            lRect.sizeDelta = new Vector2(46f, 46f);

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
            if (Resources.Load<GameObject>($"Levels/Level_{levelIndex:D3}") == null &&
                Resources.Load<GameObject>($"Levels/Level_{levelIndex}") == null)
            {
                ShowLockedMessage("More adventurous levels coming soon! 🌟");
                return;
            }

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

            float targetY = (levelPositions != null && targetIndex < levelPositions.Count) 
                ? levelPositions[targetIndex].y 
                : (260.0f + targetIndex * verticalSpacing);

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
