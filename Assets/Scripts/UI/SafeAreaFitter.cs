using UnityEngine;
using UnityEngine.UI;

namespace CutFreePets.UI
{
    /// <summary>
    /// Professional Mobile Safe Area Fitter.
    /// Handles camera punch-holes, notches, and home gesture pills on modern Android & iOS devices.
    /// Automatically conforms RectTransforms so UI elements never get covered by screen cutouts.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class SafeAreaFitter : MonoBehaviour
    {
        public enum FitMode
        {
            Auto,          // Automatically detects if full-screen panel or anchored bar
            AnchorMinMax,  // Full-screen panels (stretches anchors to match safe area)
            OffsetPadding  // Anchored header/footer bars (offsets position to avoid cutout)
        }

        [Header("Fitting Configuration")]
        [SerializeField] private FitMode fitMode = FitMode.Auto;
        [SerializeField] private bool conformTop = true;
        [SerializeField] private bool conformBottom = true;
        [SerializeField] private bool conformLeft = true;
        [SerializeField] private bool conformRight = true;

        [Header("Editor Simulation")]
        [SerializeField] private bool simulateInEditor = false;
        [SerializeField] private float simTopCutoutHeight = 88f;    // Simulated notch/punch-hole (pixels)
        [SerializeField] private float simBottomPillHeight = 44f;   // Simulated bottom gesture pill (pixels)

        private RectTransform targetRect;
        private Canvas targetCanvas;
        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private Vector2 baseAnchoredPosition;
        private bool hasStoredBasePos = false;

        private void Awake()
        {
            targetRect = GetComponent<RectTransform>();
            targetCanvas = GetComponentInParent<Canvas>();
            if (targetRect != null && !hasStoredBasePos)
            {
                baseAnchoredPosition = targetRect.anchoredPosition;
                hasStoredBasePos = true;
            }
            ApplySafeArea();
        }

        private void OnEnable()
        {
            if (targetRect != null && !hasStoredBasePos)
            {
                baseAnchoredPosition = targetRect.anchoredPosition;
                hasStoredBasePos = true;
            }
            ApplySafeArea();
        }

        private void Update()
        {
            Rect currentSafeArea = GetSafeArea();
            Vector2Int currentScreen = new Vector2Int(Screen.width, Screen.height);

            if (currentSafeArea != lastSafeArea || currentScreen != lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (targetRect == null) targetRect = GetComponent<RectTransform>();
            if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();

            Rect safeArea = GetSafeArea();
            float screenW = Screen.width;
            float screenH = Screen.height;

            if (screenW <= 0 || screenH <= 0) return;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int((int)screenW, (int)screenH);

            FitMode activeMode = fitMode;
            if (activeMode == FitMode.Auto)
            {
                // If anchored full screen (0,0 to 1,1), use AnchorMinMax
                bool isFullScreen = Mathf.Approximately(targetRect.anchorMin.x, 0f) &&
                                   Mathf.Approximately(targetRect.anchorMin.y, 0f) &&
                                   Mathf.Approximately(targetRect.anchorMax.x, 1f) &&
                                   Mathf.Approximately(targetRect.anchorMax.y, 1f);
                activeMode = isFullScreen ? FitMode.AnchorMinMax : FitMode.OffsetPadding;
            }

            if (activeMode == FitMode.AnchorMinMax)
            {
                Vector2 min = targetRect.anchorMin;
                Vector2 max = targetRect.anchorMax;

                if (conformLeft) min.x = safeArea.xMin / screenW;
                if (conformRight) max.x = safeArea.xMax / screenW;
                if (conformBottom) min.y = safeArea.yMin / screenH;
                if (conformTop) max.y = safeArea.yMax / screenH;

                targetRect.anchorMin = min;
                targetRect.anchorMax = max;
                targetRect.offsetMin = Vector2.zero;
                targetRect.offsetMax = Vector2.zero;
            }
            else // OffsetPadding mode for top/bottom anchored bars
            {
                // Calculate canvas scale factor to convert screen pixels into UI units
                float scaleFactor = (targetCanvas != null) ? targetCanvas.scaleFactor : 1f;
                if (scaleFactor <= 0.001f) scaleFactor = 1f;

                float topInset = (screenH - safeArea.yMax) / scaleFactor;
                float bottomInset = safeArea.yMin / scaleFactor;

                if (!hasStoredBasePos)
                {
                    baseAnchoredPosition = targetRect.anchoredPosition;
                    hasStoredBasePos = true;
                }

                // If anchored to top
                if (targetRect.anchorMin.y >= 0.8f && targetRect.anchorMax.y >= 0.8f)
                {
                    if (conformTop)
                    {
                        Vector2 pos = baseAnchoredPosition;
                        pos.y -= topInset;
                        targetRect.anchoredPosition = pos;
                    }
                }
                // If anchored to bottom
                else if (targetRect.anchorMin.y <= 0.2f && targetRect.anchorMax.y <= 0.2f)
                {
                    if (conformBottom)
                    {
                        Vector2 pos = baseAnchoredPosition;
                        pos.y += bottomInset;
                        targetRect.anchoredPosition = pos;
                    }
                }
            }
        }

        private Rect GetSafeArea()
        {
#if UNITY_EDITOR
            if (simulateInEditor)
            {
                float topCutout = simTopCutoutHeight;
                float bottomCutout = simBottomPillHeight;
                return new Rect(0, bottomCutout, Screen.width, Screen.height - topCutout - bottomCutout);
            }
#endif
            return Screen.safeArea;
        }
    }
}
