using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.Core;
using CutFreePets.Data;

namespace CutFreePets.UI
{
    /// <summary>
    /// Professional Home Screen Controller:
    /// - Big glossy cartoon Title Header with gentle idle breathing animation
    /// - Animated happy cat playing on the rug with animated frames
    /// - Giant glossy green PLAY button with pulsating heart-beat bounce
    /// - SETTINGS button opening a cozy modal (Music, Sound, Reset Data, Close)
    /// - QUIT button with clean application exit confirmation
    /// - Displays total stars collected in the player's journey
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeScreenController : MonoBehaviour
    {
        [Header("Main Buttons")]
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnQuit;

        [Header("Title & Logo")]
        [SerializeField] private RectTransform titleLogoTransform;

        [Header("Settings Dialog Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button btnCloseSettings;
        [SerializeField] private Button btnResetProgress;
        [SerializeField] private Toggle toggleSound;
        [SerializeField] private Toggle toggleMusic;

        [Header("Quit Confirmation Modal")]
        [SerializeField] private GameObject quitConfirmPanel;
        [SerializeField] private Button btnConfirmQuit;
        [SerializeField] private Button btnCancelQuit;

        [Header("Animated Cat Display")]
        [SerializeField] private Image catDisplayImage;
        [SerializeField] private Sprite[] happyCatFrames;
        [SerializeField] private float catFps = 10f;

        [Header("Stars Display")]
        [SerializeField] private Text txtTotalStars;

        private int currentCatFrame = 0;
        private float catFrameTimer = 0f;

        private void Awake()
        {
            // Load persistent player progress
            SaveManager.Load();

            // Auto-load cat frames if not serialized
            if (happyCatFrames == null || happyCatFrames.Length == 0)
            {
#if UNITY_EDITOR
                var list = new System.Collections.Generic.List<Sprite>();
                for (int f = 0; f < 60; f++)
                {
                    Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/UI/Home/Cat_Showcase/showcase_{f:D2}.png");
                    if (s == null)
                    {
                        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/cat/Frames_Happy/happy_{f:D2}.png");
                    }
                    if (s != null) list.Add(s);
                }
                happyCatFrames = list.ToArray();
#endif
            }

            if (catDisplayImage != null && happyCatFrames != null && happyCatFrames.Length > 0)
            {
                catDisplayImage.sprite = happyCatFrames[0];
                catDisplayImage.color = Color.white;
            }

            // Wire Buttons
            if (btnPlay != null) btnPlay.onClick.AddListener(OnPlayClicked);
            if (btnSettings != null) btnSettings.onClick.AddListener(OnSettingsClicked);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuitClicked);

            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(OnCloseSettingsClicked);
            if (btnResetProgress != null) btnResetProgress.onClick.AddListener(OnResetProgressClicked);

            if (btnConfirmQuit != null) btnConfirmQuit.onClick.AddListener(OnConfirmQuitClicked);
            if (btnCancelQuit != null) btnCancelQuit.onClick.AddListener(OnCancelQuitClicked);

            // Hide modals initially
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);

            UpdateTotalStarsDisplay();
        }

        private void Start()
        {
            StartCoroutine(AnimateTitleBreathing());
            StartCoroutine(AnimatePlayButtonPulse());
        }

        private void Update()
        {
            // Animate Cat playing on the rug if frames are assigned
            if (happyCatFrames != null && happyCatFrames.Length > 0 && catDisplayImage != null)
            {
                catFrameTimer += Time.deltaTime;
                if (catFrameTimer >= 1f / Mathf.Max(catFps, 1f))
                {
                    catFrameTimer = 0f;
                    currentCatFrame = (currentCatFrame + 1) % happyCatFrames.Length;
                    catDisplayImage.sprite = happyCatFrames[currentCatFrame];
                }
            }
        }

        private void OnPlayClicked()
        {
            Debug.Log("<color=lime>[HomeScreen]</color> Play button pressed! Opening Level Select screen...");
            GameManager.LoadLevelSelect();
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void OnCloseSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OnResetProgressClicked()
        {
            SaveManager.ResetAllProgress();
            UpdateTotalStarsDisplay();
            Debug.Log("<color=yellow>[HomeScreen]</color> Progress reset by player!");
        }

        private void OnQuitClicked()
        {
            if (quitConfirmPanel != null)
            {
                quitConfirmPanel.SetActive(true);
            }
            else
            {
                GameManager.QuitGame();
            }
        }

        private void OnConfirmQuitClicked()
        {
            GameManager.QuitGame();
        }

        private void OnCancelQuitClicked()
        {
            if (quitConfirmPanel != null)
            {
                quitConfirmPanel.SetActive(false);
            }
        }

        private void UpdateTotalStarsDisplay()
        {
            if (txtTotalStars != null)
            {
                int totalStars = SaveManager.GetTotalStars();
                txtTotalStars.text = $"{totalStars}";
            }
        }

        private IEnumerator AnimateTitleBreathing()
        {
            if (titleLogoTransform == null) yield break;

            Vector3 baseScale = titleLogoTransform.localScale;
            float timer = 0f;

            while (true)
            {
                timer += Time.deltaTime * 1.8f;
                float wave = Mathf.Sin(timer) * 0.04f;
                titleLogoTransform.localScale = new Vector3(baseScale.x * (1f + wave), baseScale.y * (1f - wave), baseScale.z);
                yield return null;
            }
        }

        private IEnumerator AnimatePlayButtonPulse()
        {
            if (btnPlay == null) yield break;

            Transform tr = btnPlay.transform;
            Vector3 baseScale = tr.localScale;
            float timer = 0f;

            while (true)
            {
                timer += Time.deltaTime * 2.8f;
                // Double harmonic gentle casual heartbeat pulse
                float pulse = (Mathf.Sin(timer) * 0.6f + Mathf.Sin(timer * 2f) * 0.4f) * 0.07f;
                tr.localScale = baseScale * (1f + pulse);
                yield return null;
            }
        }
    }
}
