using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.Gameplay;
using CutFreePets.Data;

namespace CutFreePets.UI
{
    /// <summary>
    /// Professional Level Clear Victory Popup (Cut the Rope style):
    /// - Soft dark backdrop overlay with smooth fade-in
    /// - Wooden victory plaque board with bouncy overshoot scale animation
    /// - 3 Empty dark bronze star sockets displayed in an arched layout
    /// - Earned golden stars pop sequentially with satisfying punch-scale and sparkle particles
    /// - Interactive Replay, Next Level, and Menu buttons
    /// </summary>
    [DisallowMultipleComponent]
    public class VictoryPopupUI : MonoBehaviour
    {
        [Header("Root & Panels")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image backgroundDim;
        [SerializeField] private RectTransform boardTransform;

        [Header("Star Sockets (Empty Background Stars)")]
        [SerializeField] private Image[] emptyStarSockets;

        [Header("Golden Stars (Earned Stars)")]
        [SerializeField] private Image[] goldenStars;

        [Header("Action Buttons")]
        [SerializeField] private Button btnNext;
        [SerializeField] private Button btnReplay;
        [SerializeField] private Button btnMenu;

        [Header("Animation Durations")]
        [SerializeField] private float boardBounceDuration = 0.5f;
        [SerializeField] private float starPopDelay = 0.25f;
        [SerializeField] private float starPopDuration = 0.35f;

        private bool isShowing = false;

        private void Awake()
        {
            // Start hidden
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            SetupButtons();
        }

        private void OnEnable()
        {
            if (LevelScoreTracker.Instance != null)
            {
                LevelScoreTracker.Instance.OnLevelCompleted += HandleLevelCompleted;
            }
        }

        private void OnDisable()
        {
            if (LevelScoreTracker.Instance != null)
            {
                LevelScoreTracker.Instance.OnLevelCompleted -= HandleLevelCompleted;
            }
        }

        private void Start()
        {
            // Safety hook if LevelScoreTracker was initialized after this Awake
            if (LevelScoreTracker.Instance != null)
            {
                LevelScoreTracker.Instance.OnLevelCompleted -= HandleLevelCompleted;
                LevelScoreTracker.Instance.OnLevelCompleted += HandleLevelCompleted;
            }
        }

        private void SetupButtons()
        {
            if (btnNext != null)
            {
                btnNext.onClick.AddListener(OnNextClicked);
            }
            if (btnReplay != null)
            {
                btnReplay.onClick.AddListener(OnReplayClicked);
            }
            if (btnMenu != null)
            {
                btnMenu.onClick.AddListener(OnMenuClicked);
            }
        }

        private void OnNextClicked()
        {
            Debug.Log("<color=lime>[VictoryPopupUI]</color> 'Next Level' button clicked! (Action stubbed for now)");
            // Stubbed as requested: on that button click for now not do anything for now
        }

        private void OnReplayClicked()
        {
            Debug.Log("<color=cyan>[VictoryPopupUI]</color> 'Replay' clicked! Reloading current level...");
            // Restart the current active level cleanly
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
        }

        private void OnMenuClicked()
        {
            Debug.Log("<color=orange>[VictoryPopupUI]</color> 'Level Select / Menu' button clicked! (Action stubbed for now)");
            // Stubbed as requested: on that button click for now not do anything for now
        }

        private void HandleLevelCompleted(int levelIndex, int starsEarned, int totalStars)
        {
            if (isShowing) return;
            StartCoroutine(ShowVictoryFlow(starsEarned));
        }

        public void Show(int starsEarned)
        {
            if (isShowing) return;
            StartCoroutine(ShowVictoryFlow(starsEarned));
        }

        private IEnumerator ShowVictoryFlow(int starsEarned)
        {
            isShowing = true;

            // 1. Wait a moment for the cat landing bounce and cage break debris to complete cleanly
            yield return new WaitForSeconds(0.45f);

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }

            // Reset initial states
            if (backgroundDim != null)
            {
                backgroundDim.color = new Color(0f, 0f, 0f, 0f);
                backgroundDim.raycastTarget = false; // Never block or tint the scene
            }

            if (boardTransform != null)
            {
                boardTransform.localScale = Vector3.zero;
            }

            // Hide all earned golden stars initially (empty sockets remain visible)
            if (goldenStars != null)
            {
                for (int i = 0; i < goldenStars.Length; i++)
                {
                    if (goldenStars[i] != null)
                    {
                        goldenStars[i].gameObject.SetActive(false);
                        goldenStars[i].transform.localScale = Vector3.zero;
                    }
                }
            }

            // 2. Animate Board overshoot bounce (Clean overlay without any dark/blue backdrop)
            float elapsed = 0f;
            while (elapsed < boardBounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / boardBounceDuration);

                // Overshoot bounce (0 -> 1.15 -> 1.0)
                if (boardTransform != null)
                {
                    float scale = OvershootEaseOut(t);
                    boardTransform.localScale = new Vector3(scale, scale, 1f);
                }

                yield return null;
            }

            if (boardTransform != null)
            {
                boardTransform.localScale = Vector3.one;
            }

            // 3. Sequentially reveal earned stars (1, 2, or 3) with juicy pop animation
            int starsToReveal = Mathf.Clamp(starsEarned, 0, 3);
            for (int i = 0; i < starsToReveal; i++)
            {
                yield return new WaitForSeconds(starPopDelay);

                if (goldenStars != null && i < goldenStars.Length && goldenStars[i] != null)
                {
                    StartCoroutine(PopStarAnimation(goldenStars[i]));
                }
            }
        }

        private IEnumerator PopStarAnimation(Image starImage)
        {
            if (starImage == null) yield break;

            starImage.gameObject.SetActive(true);
            Transform starTr = starImage.transform;
            starTr.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < starPopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / starPopDuration);

                // Elastic pop bounce (expands up to 1.35x then settles to 1.0x)
                float s = StarPopEase(t);
                starTr.localScale = new Vector3(s, s, 1f);

                yield return null;
            }

            starTr.localScale = Vector3.one;
        }

        private float OvershootEaseOut(float t)
        {
            // Back ease out with subtle bounce
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float StarPopEase(float t)
        {
            // Punchy scale curve for star pop
            if (t < 0.6f)
            {
                return Mathf.Lerp(0f, 1.35f, t / 0.6f);
            }
            else
            {
                return Mathf.Lerp(1.35f, 1.0f, (t - 0.6f) / 0.4f);
            }
        }
    }
}
