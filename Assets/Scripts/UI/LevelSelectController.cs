using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.Core;
using CutFreePets.Data;

namespace CutFreePets.UI
{
    /// <summary>
    /// Master controller for the Level Select Screen:
    /// - Displays total stars collected across all levels (e.g. "⭐ 28 / 60")
    /// - Dynamic grid populated from SaveManager player progress
    /// - Shows star badges achieved for each unlocked level
    /// - Back button smoothly returning to Home Screen
    /// - Clicking any unlocked level launches that level immediately
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelSelectController : MonoBehaviour
    {
        [Header("Header Elements")]
        [SerializeField] private Button btnBack;
        [SerializeField] private Text txtTotalStars;
        [SerializeField] private RectTransform headerPanel;

        [Header("Level Grid")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private int totalLevelsToDisplay = 20;

        [Header("Sprites")]
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private Sprite cardWoodSprite;
        [SerializeField] private Sprite lockIconSprite;

        private void Awake()
        {
            SaveManager.Load();

            if (btnBack != null)
            {
                btnBack.onClick.AddListener(OnBackClicked);
            }
        }

        private void Start()
        {
            PopulateGrid();
            UpdateTotalStars();
        }

        public void PopulateGrid()
        {
            if (gridContainer == null) return;

            // Clear any placeholder children
            for (int i = gridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(gridContainer.GetChild(i).gameObject);
            }

            var progress = SaveManager.Progress;
            int totalStarsMax = totalLevelsToDisplay * 3;

            for (int lvl = 1; lvl <= totalLevelsToDisplay; lvl++)
            {
                var record = SaveManager.GetRecord(lvl);
                bool unlocked = (lvl == 1) || record.isUnlocked || (lvl <= progress.highestUnlockedLevel);
                int stars = record.starsEarned;

                GameObject cardObj = CreateCardInstance(lvl, unlocked, stars);
                cardObj.transform.SetParent(gridContainer, false);
            }
        }

        private GameObject CreateCardInstance(int levelIndex, bool unlocked, int stars)
        {
            GameObject cardObj = new GameObject($"Card_Level_{levelIndex:D2}");
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(180f, 180f);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = cardWoodSprite;
            cardImg.type = Image.Type.Simple;
            cardImg.preserveAspect = true;
            cardImg.color = unlocked ? Color.white : new Color(0.65f, 0.65f, 0.65f, 0.95f);

            Button cardBtn = cardObj.AddComponent<Button>();
            ColorBlock cb = cardBtn.colors;
            cb.highlightedColor = new Color(1f, 0.95f, 0.85f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cardBtn.colors = cb;

            // Level Number Text
            GameObject numObj = new GameObject("Text_LevelNum");
            numObj.transform.SetParent(cardObj.transform, false);
            RectTransform nRect = numObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.5f, 0.5f);
            nRect.anchorMax = new Vector2(0.5f, 0.5f);
            nRect.pivot = new Vector2(0.5f, 0.5f);
            nRect.anchoredPosition = new Vector2(0f, 12f);
            nRect.sizeDelta = new Vector2(140f, 70f);

            Text nTxt = numObj.AddComponent<Text>();
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nTxt.fontSize = 54;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.color = new Color(0.38f, 0.20f, 0.08f); // rich warm chocolate brown
            nTxt.alignment = TextAnchor.MiddleCenter;
            nTxt.text = levelIndex.ToString();

            // Stars Container at bottom of card
            GameObject starsObj = new GameObject("Stars_Container");
            starsObj.transform.SetParent(cardObj.transform, false);
            RectTransform sRect = starsObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.5f);
            sRect.anchorMax = new Vector2(0.5f, 0.5f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(0f, -44f);
            sRect.sizeDelta = new Vector2(140f, 36f);

            HorizontalLayoutGroup hlg = starsObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 6f;
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
                siRect.sizeDelta = new Vector2(30f, 30f);

                Image simg = sIcon.AddComponent<Image>();
                simg.preserveAspect = true;
                starImgs[s] = simg;
            }

            // Lock Overlay (shown when level is locked)
            GameObject lockObj = new GameObject("Lock_Overlay");
            lockObj.transform.SetParent(cardObj.transform, false);
            RectTransform lRect = lockObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 0.5f);
            lRect.anchorMax = new Vector2(0.5f, 0.5f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.anchoredPosition = Vector2.zero;
            lRect.sizeDelta = new Vector2(85f, 85f);

            Image lockImg = lockObj.AddComponent<Image>();
            lockImg.sprite = lockIconSprite;
            lockImg.preserveAspect = true;

            // Attach and initialize LevelCardUI script
            LevelCardUI cardUI = cardObj.AddComponent<LevelCardUI>();
            cardUI.Init(cardBtn, cardImg, nTxt, starsObj, starImgs, lockObj);

            // Setup card logic
            cardUI.Setup(levelIndex, unlocked, stars, starFilledSprite, starEmptySprite, OnLevelCardSelected);

            return cardObj;
        }

        private void OnLevelCardSelected(int levelIndex)
        {
            Debug.Log($"<color=lime>[LevelSelect]</color> Starting Level {levelIndex}!");
            GameManager.LoadLevel(levelIndex);
        }

        private void OnBackClicked()
        {
            Debug.Log("<color=cyan>[LevelSelect]</color> Back button clicked! Returning to Home Screen...");
            GameManager.LoadHomeScreen();
        }

        public void UpdateTotalStars()
        {
            if (txtTotalStars != null)
            {
                int currentTotal = SaveManager.GetTotalStars();
                int maxStars = totalLevelsToDisplay * 3;
                txtTotalStars.text = $"{currentTotal} / {maxStars}";
            }
        }
    }
}
