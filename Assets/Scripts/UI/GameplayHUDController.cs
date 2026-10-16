using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using CutFreePets.Core;
using CutFreePets.Gameplay;
using UnityEngine.SceneManagement;

namespace CutFreePets.UI
{
    /// <summary>
    /// Professional In-Game HUD Controller (Cut the Rope / Angry Birds style):
    /// - Top-Right Quick-Restart Button 🔄 (1-tap instant level reload with haptic click)
    /// - Top-Left Pause Button ⏸️ (opens pause dialog, pauses physics/timeScale)
    /// - Pause Modal with Resume ▶️, Restart 🔄, and Level Select 📋 buttons
    /// - Smooth unscaled animations for UI dialog pop-in
    /// - Android Back Button / Keyboard Escape support
    /// - Automatically hides when Level Victory is reached
    /// - Auto-builds UI on any Canvas if references are not serialized
    /// </summary>
    [DisallowMultipleComponent]
    public class GameplayHUDController : MonoBehaviour
    {
        public static GameplayHUDController Instance { get; private set; }

        [Header("Top HUD Buttons")]
        [SerializeField] private Button btnQuickRestart;
        [SerializeField] private Button btnPause;

        [Header("Pause Modal Elements")]
        [SerializeField] private GameObject pauseModalRoot;
        [SerializeField] private RectTransform pauseBoardTransform;
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnModalRestart;
        [SerializeField] private Button btnModalMenu;

        [Header("Sprites (Loaded from Resources if unassigned)")]
        [SerializeField] private Sprite spritePause;
        [SerializeField] private Sprite spriteRestart;
        [SerializeField] private Sprite spriteResume;
        [SerializeField] private Sprite spriteMenu;
        [SerializeField] private Sprite spriteBoard;

        private bool isPaused = false;
        private bool isLevelCompleted = false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeInGameplayScenes()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == GameManager.HOME_SCENE_NAME || sceneName == GameManager.LEVEL_SELECT_SCENE_NAME)
            {
                return;
            }

            if (Instance == null && FindAnyObjectByType<GameplayHUDController>() == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    canvas.gameObject.AddComponent<GameplayHUDController>();
                }
            }
        }

        private Canvas targetCanvas;

        private void Awake()
        {
            Instance = this;
            EnsureCanvasAndReferences();
            WireButtons();

            if (pauseModalRoot != null)
            {
                pauseModalRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (LevelScoreTracker.Instance != null)
            {
                LevelScoreTracker.Instance.OnLevelCompleted += HandleLevelCompleted;
            }

            PetCage cage = FindAnyObjectByType<PetCage>();
            if (cage != null)
            {
                cage.OnLandedOnGoal += HandleCageLanded;
            }
        }

        private void OnDisable()
        {
            if (LevelScoreTracker.Instance != null)
            {
                LevelScoreTracker.Instance.OnLevelCompleted -= HandleLevelCompleted;
            }

            PetCage cage = FindAnyObjectByType<PetCage>();
            if (cage != null)
            {
                cage.OnLandedOnGoal -= HandleCageLanded;
            }

            // Always guarantee timeScale is restored
            Time.timeScale = 1f;
        }

        private void Start()
        {
            Time.timeScale = 1f;
        }

        private void Update()
        {
            // Do nothing if level already won
            if (isLevelCompleted) return;

            // Handle Escape / Android Back button
            bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapePressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                escapePressed = true;
            }
#endif

            if (escapePressed)
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }

            // PC Quick restart shortcut: 'R' key
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                QuickRestart();
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R))
            {
                QuickRestart();
            }
#endif
        }

        public void QuickRestart()
        {
            if (isLevelCompleted) return;

            Debug.Log("<color=cyan>[GameplayHUD]</color> Quick Restart 🔄 triggered!");
            HapticManager.PlayRopeCutHaptic();
            Time.timeScale = 1f;
            GameManager.RestartCurrentLevel();
        }

        public void PauseGame()
        {
            if (isLevelCompleted || isPaused) return;

            isPaused = true;
            Time.timeScale = 0f;
            HapticManager.PlayRopeCutHaptic();

            if (pauseModalRoot != null)
            {
                pauseModalRoot.SetActive(true);
            }

            if (pauseBoardTransform != null)
            {
                StopAllCoroutines();
                StartCoroutine(AnimateModalPopIn(pauseBoardTransform));
            }

            Debug.Log("<color=yellow>[GameplayHUD]</color> Game Paused ⏸️");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = 1f;
            HapticManager.PlayRopeCutHaptic();

            if (pauseModalRoot != null)
            {
                pauseModalRoot.SetActive(false);
            }

            Debug.Log("<color=lime>[GameplayHUD]</color> Game Resumed ▶️");
        }

        public void OnMenuClicked()
        {
            Time.timeScale = 1f;
            HapticManager.PlayRopeCutHaptic();
            GameManager.LoadLevelSelect();
        }

        private void HandleLevelCompleted(int levelIndex, int starsEarned, int totalStars)
        {
            HideHUDOnVictory();
        }

        private void HandleCageLanded(Collision2D collision)
        {
            HideHUDOnVictory();
        }

        private void HideHUDOnVictory()
        {
            isLevelCompleted = true;
            if (btnQuickRestart != null) btnQuickRestart.gameObject.SetActive(false);
            if (btnPause != null) btnPause.gameObject.SetActive(false);
            if (pauseModalRoot != null) pauseModalRoot.SetActive(false);
        }

        private void WireButtons()
        {
            if (btnQuickRestart != null)
            {
                btnQuickRestart.onClick.RemoveAllListeners();
                btnQuickRestart.onClick.AddListener(QuickRestart);
            }

            if (btnPause != null)
            {
                btnPause.onClick.RemoveAllListeners();
                btnPause.onClick.AddListener(PauseGame);
            }

            if (btnResume != null)
            {
                btnResume.onClick.RemoveAllListeners();
                btnResume.onClick.AddListener(ResumeGame);
            }

            if (btnModalRestart != null)
            {
                btnModalRestart.onClick.RemoveAllListeners();
                btnModalRestart.onClick.AddListener(QuickRestart);
            }

            if (btnModalMenu != null)
            {
                btnModalMenu.onClick.RemoveAllListeners();
                btnModalMenu.onClick.AddListener(OnMenuClicked);
            }
        }

        private IEnumerator AnimateModalPopIn(RectTransform tr)
        {
            tr.localScale = Vector3.zero;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Overshoot bounce curve
                float scale = Mathf.Sin(t * Mathf.PI * 0.5f) + 0.1f * Mathf.Sin(t * Mathf.PI);
                tr.localScale = Vector3.one * scale;
                yield return null;
            }

            tr.localScale = Vector3.one;
        }

        /// <summary>
        /// Auto-builds the HUD elements on the active Canvas if not serialized in the scene.
        /// </summary>
        private void EnsureCanvasAndReferences()
        {
            targetCanvas = GetComponentInParent<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = FindAnyObjectByType<Canvas>();
            }

            // Load sprites from Resources if missing
            if (spritePause == null) spritePause = Resources.Load<Sprite>("UI/UI_Btn_Pause");
            if (spriteRestart == null) spriteRestart = Resources.Load<Sprite>("UI/UI_Btn_Replay");
            if (spriteResume == null) spriteResume = Resources.Load<Sprite>("UI/UI_Btn_Play");
            if (spriteMenu == null) spriteMenu = Resources.Load<Sprite>("UI/UI_Btn_Menu");
            if (spriteBoard == null) spriteBoard = Resources.Load<Sprite>("UI/UI_Win_Board");

            if (targetCanvas == null) return;

            Transform canvasTr = targetCanvas.transform;

            // 1. Build Top HUD Container if not present
            Transform hudContainer = canvasTr.Find("Gameplay_HUD");
            if (hudContainer == null)
            {
                GameObject hudObj = new GameObject("Gameplay_HUD", typeof(RectTransform));
                hudObj.transform.SetParent(canvasTr, false);
                hudObj.transform.SetSiblingIndex(0); // Under victory dialog
                RectTransform hudRect = hudObj.GetComponent<RectTransform>();
                hudRect.anchorMin = Vector2.zero;
                hudRect.anchorMax = Vector2.one;
                hudRect.offsetMin = Vector2.zero;
                hudRect.offsetMax = Vector2.zero;
                hudContainer = hudObj.transform;

                // Add SafeAreaFitter
                var fitter = hudObj.AddComponent<SafeAreaFitter>();
            }

            // 2. Build Quick Restart (Top-Right)
            if (btnQuickRestart == null)
            {
                Transform existingRestart = hudContainer.Find("Btn_QuickRestart");
                if (existingRestart != null)
                {
                    btnQuickRestart = existingRestart.GetComponent<Button>();
                }
                else
                {
                    btnQuickRestart = CreateRoundButton(hudContainer, "Btn_QuickRestart", spriteRestart,
                        new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-60f, -60f), new Vector2(115f, 115f), new Vector2(1f, 1f));
                }
            }

            // 3. Build Pause Button (Top-Left)
            if (btnPause == null)
            {
                Transform existingPause = hudContainer.Find("Btn_Pause");
                if (existingPause != null)
                {
                    btnPause = existingPause.GetComponent<Button>();
                }
                else
                {
                    btnPause = CreateRoundButton(hudContainer, "Btn_Pause", spritePause,
                        new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -60f), new Vector2(115f, 115f), new Vector2(0f, 1f));
                }
            }

            // 4. Build Pause Modal if not present
            if (pauseModalRoot == null)
            {
                Transform existingModal = canvasTr.Find("Pause_Modal");
                if (existingModal != null)
                {
                    pauseModalRoot = existingModal.gameObject;
                    pauseBoardTransform = existingModal.Find("Board")?.GetComponent<RectTransform>();
                    btnResume = existingModal.Find("Board/Btn_Resume")?.GetComponent<Button>();
                    btnModalRestart = existingModal.Find("Board/Btn_Restart")?.GetComponent<Button>();
                    btnModalMenu = existingModal.Find("Board/Btn_Menu")?.GetComponent<Button>();
                }
                else
                {
                    BuildPauseModal(canvasTr);
                }
            }
        }

        private Button CreateRoundButton(Transform parent, string name, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Vector2 pivot)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            rt.pivot = pivot;

            Image img = obj.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;

            Button btn = obj.GetComponent<Button>();
            btn.targetGraphic = img;

            // Casual game button color transition
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.95f, 0.85f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = cb;

            return btn;
        }

        private void BuildPauseModal(Transform canvasTr)
        {
            // Modal Root Dim Overlay
            GameObject modalObj = new GameObject("Pause_Modal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            modalObj.transform.SetParent(canvasTr, false);
            modalObj.transform.SetSiblingIndex(canvasTr.childCount - 1);

            RectTransform rootRt = modalObj.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Image dimImg = modalObj.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.68f);
            dimImg.raycastTarget = true; // Blocks touches to gameplay

            pauseModalRoot = modalObj;

            // Wooden Board Container
            GameObject boardObj = new GameObject("Board", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            boardObj.transform.SetParent(modalObj.transform, false);

            pauseBoardTransform = boardObj.GetComponent<RectTransform>();
            pauseBoardTransform.anchorMin = new Vector2(0.5f, 0.5f);
            pauseBoardTransform.anchorMax = new Vector2(0.5f, 0.5f);
            pauseBoardTransform.anchoredPosition = Vector2.zero;
            pauseBoardTransform.sizeDelta = new Vector2(680f, 480f);
            pauseBoardTransform.pivot = new Vector2(0.5f, 0.5f);

            Image boardImg = boardObj.GetComponent<Image>();
            boardImg.sprite = spriteBoard;
            boardImg.preserveAspect = false;
            boardImg.color = Color.white;

            // Title Text: "PAUSED"
            GameObject titleObj = new GameObject("Text_Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            titleObj.transform.SetParent(boardObj.transform, false);

            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, 110f);
            titleRt.sizeDelta = new Vector2(500f, 80f);

            Text titleText = titleObj.GetComponent<Text>();
            titleText.text = "PAUSED";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 54;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.92f, 0.65f, 1f); // Warm sunny gold

            Outline outline = titleObj.GetComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.18f, 0.05f, 0.95f); // Rich timber outline
            outline.effectDistance = new Vector2(3f, -3f);

            // Button Row: Resume ▶️, Restart 🔄, Menu 📋
            float btnY = -45f;
            Vector2 btnSize = new Vector2(145f, 145f);

            btnResume = CreateRoundButton(boardObj.transform, "Btn_Resume", spriteResume,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-170f, btnY), btnSize, new Vector2(0.5f, 0.5f));

            btnModalRestart = CreateRoundButton(boardObj.transform, "Btn_Restart", spriteRestart,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, btnY), btnSize, new Vector2(0.5f, 0.5f));

            btnModalMenu = CreateRoundButton(boardObj.transform, "Btn_Menu", spriteMenu,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170f, btnY), btnSize, new Vector2(0.5f, 0.5f));
        }
    }
}
