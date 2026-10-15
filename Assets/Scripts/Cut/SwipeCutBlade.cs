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
        [SerializeField] private float minSwipeDistance = 0.04f;

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

        private void Start()
        {
            ConfigureBladeTrail();
        }

        private void ConfigureBladeTrail()
        {
            if (trailRenderer == null) return;

            // Fruit Ninja style: very fast, sleek, clean katana slash
            trailRenderer.time = 0.12f; // Snappy, clean cut lifetime
            trailRenderer.emitting = false;
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.sortingOrder = 50; // Ensure trail renders above backgrounds, ropes, and UI
            trailRenderer.numCornerVertices = 10;
            trailRenderer.numCapVertices = 10;

            // Fruit Ninja Katana Blade Width Profile:
            // Razor leading tip (0.0), quick slender sharp blade body, fading to needle tip (1.0)
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0.00f, 0.40f, 0.0f, 0.0f));  // Crisp sharp cutting point
            curve.AddKey(new Keyframe(0.15f, 0.70f, 0.0f, 0.0f));  // Slender blade edge
            curve.AddKey(new Keyframe(0.50f, 0.40f, -0.6f, -0.6f));// Thinning taper
            curve.AddKey(new Keyframe(1.00f, 0.00f, -0.4f, 0.0f)); // Vanishing needle point
            trailRenderer.widthCurve = curve;
            trailRenderer.widthMultiplier = 0.14f; // Sleek, thin professional blade (not chunky)

            // Assign high quality 2D Unlit shader for URP to prevent any magenta/pink artifacts
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                Material bladeMat = new Material(shader);
                bladeMat.color = Color.white;
                trailRenderer.material = bladeMat;
            }

            // Fruit Ninja authentic gleaming katana steel / ice slash:
            // Pure piercing white blade edge -> Crisp ice-silver glow -> Translucent cyan-silver fade (zero fire/orange)
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 1f, 1f), 0.0f),         // Pure bright white blade core
                    new GradientColorKey(new Color(0.92f, 0.98f, 1f), 0.35f),  // Crisp polished silver-white
                    new GradientColorKey(new Color(0.70f, 0.88f, 1f), 0.75f),  // Cool steel katana tint
                    new GradientColorKey(new Color(0.50f, 0.75f, 0.98f), 1.0f) // Soft fading edge
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.85f, 0.4f),
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

            // Require an actual swipe movement across the screen
            if (distance >= minSwipeDistance)
            {
                SliceRopesInPath(lastInputPos, worldPos);
                lastInputPos = worldPos;
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

                // Authentic Cut the Rope slicing: Requires swipe line segment to geometrically cross the rope
                if (rope.CutBetweenPoints(pStart, pEnd))
                {
                    SpawnSnipSparks(pEnd);
                    Debug.Log("<color=yellow>[CutFreePets]</color> Rope sliced cleanly by swipe!");
                }
            }
        }

        private void SpawnSnipSparks(Vector3 position)
        {
            // Cut the Rope iconic burst of golden scissor sparks at the severed point
            Sprite sparkSprite = Resources.Load<Sprite>("Snip_Spark");
#if UNITY_EDITOR
            if (sparkSprite == null)
            {
                sparkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rope/Snip_Spark.png");
            }
#endif
            Shader sparkShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            Material sparkMat = sparkShader != null ? new Material(sparkShader) : null;

            for (int i = 0; i < 7; i++)
            {
                GameObject spark = new GameObject("SnipSpark");
                spark.transform.position = position;
                SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
                if (sparkSprite != null) sr.sprite = sparkSprite;
                if (sparkMat != null) sr.material = sparkMat;
                sr.color = new Color(1f, 0.92f, 0.45f, 1f);
                sr.sortingOrder = 60;
                spark.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.14f, 0.24f);

                float angle = UnityEngine.Random.Range(0f, 360f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                Rigidbody2D rb = spark.AddComponent<Rigidbody2D>();
                rb.gravityScale = 1.2f;
                rb.linearVelocity = dir * UnityEngine.Random.Range(3.5f, 6.0f);

                Destroy(spark, 0.35f);
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
