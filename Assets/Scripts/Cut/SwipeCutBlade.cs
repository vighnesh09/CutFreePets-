using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using CutFreePets.Rope;

namespace CutFreePets.Cut
{
    /// <summary>
    /// Pure touch and mouse swipe cut controller supporting both Unity's New Input System and legacy Input.
    /// Features:
    /// - Direct swipe cutting (no scissor icons, pure instant blade slicing)
    /// - Continuous line-segment collision detection (never misses fast swipes)
    /// - Supports both mobile touch (multi-touch supported) and mouse input
    /// - Juicy glowing slash blade trail effect
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class SwipeCutBlade : MonoBehaviour
    {
        [Header("Blade Slash Trail")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private float trailTime = 0.16f;
        [SerializeField] private float startWidth = 0.18f;
        [SerializeField] private float endWidth = 0.01f;
        [SerializeField] private float minSwipeDistance = 0.03f;

        [Header("Colors & Glow")]
        [SerializeField] private Color bladeCoreColor = new Color(1f, 1f, 0.85f, 1f); // Bright white-gold core
        [SerializeField] private Color bladeGlowColor = new Color(1f, 0.55f, 0.1f, 0.8f); // Warm orange glow

        private Camera mainCamera;
        private Vector3 lastInputPos;
        private bool isSwiping = false;

        private void Awake()
        {
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            mainCamera = Camera.main;
            ConfigureBladeTrail();
        }

        private void ConfigureBladeTrail()
        {
            trailRenderer.time = trailTime;
            trailRenderer.startWidth = startWidth;
            trailRenderer.endWidth = endWidth;
            trailRenderer.emitting = false;
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;

            // Cut the Rope luminous glowing swipe gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(bladeCoreColor, 0.0f),
                    new GradientColorKey(bladeGlowColor, 0.6f),
                    new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.85f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trailRenderer.colorGradient = gradient;
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            bool isPressed = false;
            bool wasPressedThisFrame = false;
            bool wasReleasedThisFrame = false;
            Vector2 screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            // Check Touchscreen first
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                var touch = Touchscreen.current.touches[0];
                screenPosition = touch.position.ReadValue();
                isPressed = touch.press.isPressed;
                wasPressedThisFrame = touch.press.wasPressedThisFrame;
                wasReleasedThisFrame = touch.press.wasReleasedThisFrame;
            }
            // Otherwise use Mouse / Pointer
            else if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
                isPressed = Mouse.current.leftButton.isPressed;
                wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                wasReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else if (Pointer.current != null)
            {
                screenPosition = Pointer.current.position.ReadValue();
                isPressed = Pointer.current.press.isPressed;
                wasPressedThisFrame = Pointer.current.press.wasPressedThisFrame;
                wasReleasedThisFrame = Pointer.current.press.wasReleasedThisFrame;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                screenPosition = t.position;
                isPressed = (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary);
                wasPressedThisFrame = (t.phase == TouchPhase.Began);
                wasReleasedThisFrame = (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
            }
            else
            {
                screenPosition = Input.mousePosition;
                isPressed = Input.GetMouseButton(0);
                wasPressedThisFrame = Input.GetMouseButtonDown(0);
                wasReleasedThisFrame = Input.GetMouseButtonUp(0);
            }
#endif

            Vector3 worldPos = GetWorldPosition(screenPosition);

            if (wasPressedThisFrame)
            {
                StartSwipe(worldPos);
            }
            else if (isPressed && isSwiping)
            {
                ContinueSwipe(worldPos);
            }
            else if (wasReleasedThisFrame && isSwiping)
            {
                EndSwipe();
            }
        }

        private void StartSwipe(Vector3 worldPos)
        {
            isSwiping = true;
            lastInputPos = worldPos;
            transform.position = worldPos;
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }

        private void ContinueSwipe(Vector3 worldPos)
        {
            transform.position = worldPos;
            float distance = Vector3.Distance(worldPos, lastInputPos);

            if (distance >= minSwipeDistance)
            {
                SliceRopesInPath(lastInputPos, worldPos);
                lastInputPos = worldPos;
            }
        }

        private void EndSwipe()
        {
            isSwiping = false;
            trailRenderer.emitting = false;
        }

        private void SliceRopesInPath(Vector3 pStart, Vector3 pEnd)
        {
            var ropes = FindObjectsByType<VerletRope>();
            for (int i = 0; i < ropes.Length; i++)
            {
                VerletRope rope = ropes[i];
                if (rope == null || rope.IsCut) continue;

                if (rope.CutBetweenPoints(pStart, pEnd))
                {
                    Debug.Log("<color=yellow>[CutTheRope]</color> Rope sliced by touch swipe!");
                }
            }
        }

        private Vector3 GetWorldPosition(Vector2 screenPos)
        {
            Vector3 sp = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z));
            Vector3 world = mainCamera.ScreenToWorldPoint(sp);
            world.z = 0f;
            return world;
        }
    }
}
