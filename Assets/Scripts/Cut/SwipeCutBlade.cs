using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using CutFreePets.Rope;

namespace CutFreePets.Cut
{
    /// <summary>
    /// Touch and mouse swipe cut controller.
    /// Dual input system support: works seamlessly with both New Input System and Legacy Input Manager.
    /// Features:
    /// - Direct swipe cutting with interpolated multi-sample line collision
    /// - High visual priority glowing blade trail (always visible on top)
    /// - Immediate and path-based slice triggers
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class SwipeCutBlade : MonoBehaviour
    {
        [Header("Blade Slash Trail")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private float trailTime = 0.2f;
        [SerializeField] private float startWidth = 0.22f;
        [SerializeField] private float endWidth = 0.02f;
        [SerializeField] private float minSwipeDistance = 0.015f;

        [Header("Colors & Glow")]
        [SerializeField] private Color bladeCoreColor = new Color(1f, 1f, 0.85f, 1f); // Bright white-gold core
        [SerializeField] private Color bladeGlowColor = new Color(1f, 0.55f, 0.1f, 0.85f); // Warm orange glow

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
            if (trailRenderer == null) return;

            trailRenderer.time = trailTime;
            trailRenderer.startWidth = startWidth;
            trailRenderer.endWidth = endWidth;
            trailRenderer.emitting = false;
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.sortingOrder = 50; // Ensure trail renders above backgrounds, ropes, and UI

            // Assign default particle/trail material if none assigned
            if (trailRenderer.material == null || trailRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null)
                {
                    trailRenderer.material = new Material(shader);
                }
            }

            // Cut the Rope luminous glowing swipe gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(bladeCoreColor, 0.0f),
                    new GradientColorKey(bladeGlowColor, 0.5f),
                    new GradientColorKey(new Color(1.0f, 0.25f, 0.05f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
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

            // 1. Try New Input System
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                var touch = Touchscreen.current.touches[0];
                screenPosition = touch.position.ReadValue();
                isPressed = touch.press.isPressed;
                wasPressedThisFrame = touch.press.wasPressedThisFrame;
                wasReleasedThisFrame = touch.press.wasReleasedThisFrame;
            }
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
#endif

            // 2. Hybrid Fallback: If New Input didn't register or is disabled, check Legacy Input Manager (when enabled)
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!isPressed && !wasPressedThisFrame && !wasReleasedThisFrame)
            {
                if (Input.touchCount > 0)
                {
                    Touch t = Input.GetTouch(0);
                    screenPosition = t.position;
                    isPressed = (t.phase == UnityEngine.TouchPhase.Began || t.phase == UnityEngine.TouchPhase.Moved || t.phase == UnityEngine.TouchPhase.Stationary);
                    wasPressedThisFrame = (t.phase == UnityEngine.TouchPhase.Began);
                    wasReleasedThisFrame = (t.phase == UnityEngine.TouchPhase.Ended || t.phase == UnityEngine.TouchPhase.Canceled);
                }
                else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
                {
                    screenPosition = Input.mousePosition;
                    isPressed = Input.GetMouseButton(0);
                    wasPressedThisFrame = Input.GetMouseButtonDown(0);
                    wasReleasedThisFrame = Input.GetMouseButtonUp(0);
                }
            }
#endif

            Vector3 worldPos = GetWorldPosition(screenPosition);

            if (wasPressedThisFrame)
            {
                StartSwipe(worldPos);
                // Immediately check direct touch point slicing
                SliceRopesAtPoint(worldPos);
            }
            else if (isPressed)
            {
                if (!isSwiping)
                {
                    StartSwipe(worldPos);
                }
                ContinueSwipe(worldPos);
            }
            else if (wasReleasedThisFrame || (!isPressed && isSwiping))
            {
                EndSwipe();
            }
        }

        private void StartSwipe(Vector3 worldPos)
        {
            isSwiping = true;
            lastInputPos = worldPos;
            transform.position = worldPos;
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }
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
            else
            {
                SliceRopesAtPoint(worldPos);
            }
        }

        private void EndSwipe()
        {
            isSwiping = false;
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }
        }

        private void SliceRopesInPath(Vector3 pStart, Vector3 pEnd)
        {
            var ropes = FindObjectsByType<VerletRope>(FindObjectsInactive.Exclude);
            for (int i = 0; i < ropes.Length; i++)
            {
                VerletRope rope = ropes[i];
                if (rope == null || rope.IsCut) continue;

                // Try segment intersection first, then high-tolerance point slice along the swipe vector
                if (rope.CutBetweenPoints(pStart, pEnd) ||
                    rope.CutAtPoint(pEnd, 0.65f) ||
                    rope.CutAtPoint(Vector3.Lerp(pStart, pEnd, 0.5f), 0.65f))
                {
                    Debug.Log("<color=yellow>[CutFreePets]</color> Rope sliced by touch swipe!");
                }
            }
        }

        private void SliceRopesAtPoint(Vector3 point)
        {
            var ropes = FindObjectsByType<VerletRope>(FindObjectsInactive.Exclude);
            for (int i = 0; i < ropes.Length; i++)
            {
                VerletRope rope = ropes[i];
                if (rope == null || rope.IsCut) continue;

                if (rope.CutAtPoint(point, 0.65f))
                {
                    Debug.Log("<color=yellow>[CutFreePets]</color> Rope sliced at touch point!");
                }
            }
        }

        private Vector3 GetWorldPosition(Vector2 screenPos)
        {
            float distToCamera = mainCamera.orthographic ? 10f : Mathf.Abs(mainCamera.transform.position.z);
            Vector3 sp = new Vector3(screenPos.x, screenPos.y, distToCamera);
            Vector3 world = mainCamera.ScreenToWorldPoint(sp);
            world.z = 0f;
            return world;
        }
    }
}
