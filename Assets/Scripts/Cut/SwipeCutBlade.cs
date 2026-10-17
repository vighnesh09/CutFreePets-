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
    public enum BladeTrailTheme
    {
        GoldenSunlight,  // Cut the Rope iconic warm golden sparkle (Treehouse & Secret Garden theme)
        ElectricCyan,    // Cut the Rope vibrant luminous cyan glow
        PrismEmerald     // Secret Garden fresh lime-emerald glow
    }

    [RequireComponent(typeof(TrailRenderer))]
    public class SwipeCutBlade : MonoBehaviour
    {
        [Header("Cut the Rope Blade Trail")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private BladeTrailTheme trailTheme = BladeTrailTheme.GoldenSunlight;
        [Tooltip("Lifetime of the glowing blade trail. Cut the Rope standard is 0.20s.")]
        [SerializeField] private float trailLifetime = 0.20f;
        [Tooltip("Leading width of the blade trail at the finger/cursor.")]
        [SerializeField] private float bladeWidth = 0.28f;
        [SerializeField] private float minSwipeDistance = 0.06f;

        [Header("Swipe Sparkles")]
        [Tooltip("Emit luminous micro-sparks along the swipe trajectory.")]
        [SerializeField] private bool emitSwipeSparks = true;

        private Camera mainCamera;
        private Vector3 lastInputPos;
        private bool isSwiping = false;
        private float lastSparkSpawnTime = 0f;

        private static Texture2D cachedSoftGlowTexture;
        private static Sprite cachedSparkSprite;
        private static Material cachedSparkMaterial;

        private void Awake()
        {
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            mainCamera = Camera.main;
            minSwipeDistance = Mathf.Max(0.04f, minSwipeDistance);
            ConfigureBladeTrail();
        }

        private void Start()
        {
            ConfigureBladeTrail();
        }

        public void SetTrailTheme(BladeTrailTheme newTheme)
        {
            trailTheme = newTheme;
            ConfigureBladeTrail();
        }

        private void ConfigureBladeTrail()
        {
            if (trailRenderer == null) return;

            // Cut the Rope style: 0.20s smooth luminous fading trail with high-resolution vertex sampling
            trailRenderer.time = trailLifetime;
            trailRenderer.emitting = false;
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.sortingOrder = 70; // Always render on top of ropes, platforms, and backgrounds
            trailRenderer.numCornerVertices = 16;
            trailRenderer.numCapVertices = 16;
            trailRenderer.minVertexDistance = 0.025f; // Silky smooth curves during fast swipes

            // Cut the Rope Blade Tapering Profile:
            // Bold leading head at finger touch (1.0), gentle body curve (0.85 -> 0.45), tapering to fine needle tip (0.0)
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0.00f, 1.00f, 0.0f, -0.4f));   // Bold leading cut head
            curve.AddKey(new Keyframe(0.20f, 0.85f, -0.6f, -0.6f));  // Smooth body
            curve.AddKey(new Keyframe(0.55f, 0.45f, -0.9f, -0.9f));  // Tapering tail
            curve.AddKey(new Keyframe(1.00f, 0.00f, -0.5f, 0.0f));   // Pinpoint needle tail
            trailRenderer.widthCurve = curve;
            trailRenderer.widthMultiplier = bladeWidth;

            // Assign URP 2D Unlit Shader with procedural soft feathered glow texture
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                Material bladeMat = new Material(shader);
                Texture2D softTexture = GetOrCreateSoftGlowTexture();
                bladeMat.mainTexture = softTexture;
                if (bladeMat.HasProperty("_BaseMap")) bladeMat.SetTexture("_BaseMap", softTexture);
                bladeMat.color = Color.white;
                trailRenderer.material = bladeMat;
            }

            // Build Cut the Rope Luminous Glowing Gradients
            Gradient gradient = new Gradient();
            switch (trailTheme)
            {
                case BladeTrailTheme.ElectricCyan:
                    gradient.SetKeys(
                        new GradientColorKey[] {
                            new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.00f), // Brilliant white core
                            new GradientColorKey(new Color(0.40f, 0.95f, 1.00f), 0.25f), // Electric neon cyan (#66F2FF)
                            new GradientColorKey(new Color(0.00f, 0.70f, 0.95f), 0.65f), // Luminous azure (#00B2F2)
                            new GradientColorKey(new Color(0.00f, 0.40f, 0.85f), 1.00f)  // Deep sapphire fade
                        },
                        new GradientAlphaKey[] {
                            new GradientAlphaKey(1.00f, 0.00f),
                            new GradientAlphaKey(0.88f, 0.35f),
                            new GradientAlphaKey(0.45f, 0.75f),
                            new GradientAlphaKey(0.00f, 1.00f)
                        }
                    );
                    break;

                case BladeTrailTheme.PrismEmerald:
                    gradient.SetKeys(
                        new GradientColorKey[] {
                            new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.00f), // White core
                            new GradientColorKey(new Color(0.85f, 1.00f, 0.40f), 0.25f), // Fresh lime sparkle
                            new GradientColorKey(new Color(0.46f, 0.85f, 0.15f), 0.65f), // Vibrant apple green
                            new GradientColorKey(new Color(0.20f, 0.65f, 0.10f), 1.00f)  // Soft meadow green
                        },
                        new GradientAlphaKey[] {
                            new GradientAlphaKey(1.00f, 0.00f),
                            new GradientAlphaKey(0.88f, 0.35f),
                            new GradientAlphaKey(0.45f, 0.75f),
                            new GradientAlphaKey(0.00f, 1.00f)
                        }
                    );
                    break;

                default: // GoldenSunlight (Official Cut the Rope / Secret Garden Palette)
                    gradient.SetKeys(
                        new GradientColorKey[] {
                            new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.00f), // Pure bright white core
                            new GradientColorKey(new Color(1.00f, 0.92f, 0.40f), 0.20f), // Radiant sunny yellow (#FFEA66)
                            new GradientColorKey(new Color(1.00f, 0.65f, 0.10f), 0.60f), // Warm golden honey amber (#FFA61A)
                            new GradientColorKey(new Color(1.00f, 0.38f, 0.05f), 1.00f)  // Sunset orange edge (#FF610D)
                        },
                        new GradientAlphaKey[] {
                            new GradientAlphaKey(1.00f, 0.00f), // Crisp leading head
                            new GradientAlphaKey(0.92f, 0.35f), // Luminous body
                            new GradientAlphaKey(0.45f, 0.75f), // Soft fading tail
                            new GradientAlphaKey(0.00f, 1.00f)  // Smooth dissipation at 0.20s
                        }
                    );
                    break;
            }

            trailRenderer.colorGradient = gradient;
        }

        private static Texture2D GetOrCreateSoftGlowTexture()
        {
            if (cachedSoftGlowTexture != null) return cachedSoftGlowTexture;

            int width = 64;
            int height = 32;
            cachedSoftGlowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cachedSoftGlowTexture.name = "BladeSoftGlow_Procedural";
            cachedSoftGlowTexture.wrapMode = TextureWrapMode.Clamp;
            cachedSoftGlowTexture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                // Distance from center along vertical axis [-1, 1]
                float v = (y / (float)(height - 1)) * 2f - 1f;
                // Cosine gaussian bell curve for feathered glowing edges (no polygonal quad lines)
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(v));
                alpha = Mathf.Pow(alpha, 1.5f);

                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float capAlpha = 1f;
                    if (u < 0.12f)
                    {
                        capAlpha = Mathf.SmoothStep(0f, 1f, u / 0.12f);
                    }

                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha * capAlpha);
                }
            }

            cachedSoftGlowTexture.SetPixels(pixels);
            cachedSoftGlowTexture.Apply();
            return cachedSoftGlowTexture;
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Do not swipe or slice when game is paused
            if (Time.timeScale <= 0f)
            {
                if (isSwiping) EndSwipe();
                return;
            }

            bool isPressed = false;
            bool wasPressedThisFrame = false;
            bool wasReleasedThisFrame = false;
            Vector2 screenPosition = Vector2.zero;

            // 1. Try New Input System
#if ENABLE_INPUT_SYSTEM
            // Priority 1: Mouse (Desktop & Editor)
            if (Mouse.current != null && (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame))
            {
                screenPosition = Mouse.current.position.ReadValue();
                isPressed = Mouse.current.leftButton.isPressed;
                wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
                wasReleasedThisFrame = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            // Priority 2: Touchscreen (Mobile device / active touch)
            else if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasPressedThisFrame || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                isPressed = Touchscreen.current.primaryTouch.press.isPressed;
                wasPressedThisFrame = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                wasReleasedThisFrame = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
            // Priority 3: Pointer fallback (Stylus/Pen)
            else if (Pointer.current != null && (Pointer.current.press.isPressed || Pointer.current.press.wasPressedThisFrame || Pointer.current.press.wasReleasedThisFrame))
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
            // Deliberately do NOT cut on initial touch/click.
            // Slicing requires an active swipe motion across the rope to prevent accidental cuts when clicking beside or near the rope.
        }

        private void ContinueSwipe(Vector3 worldPos)
        {
            transform.position = worldPos;
            float distance = Vector3.Distance(worldPos, lastInputPos);

            // Require an actual swipe movement across the screen
            if (distance >= minSwipeDistance)
            {
                SliceRopesInPath(lastInputPos, worldPos);
                TrySpawnSwipeSpark(worldPos);
                lastInputPos = worldPos;
            }
        }

        private void TrySpawnSwipeSpark(Vector3 pos)
        {
            if (!emitSwipeSparks) return;
            if (Time.unscaledTime - lastSparkSpawnTime < 0.038f) return;
            lastSparkSpawnTime = Time.unscaledTime;

            GameObject spark = new GameObject("SwipeSpark");
            spark.transform.position = pos + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.04f);
            SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();

            sr.sprite = GetSparkSprite();
            Material mat = GetSparkMaterial();
            if (mat != null) sr.material = mat;

            Color sparkCol;
            switch (trailTheme)
            {
                case BladeTrailTheme.ElectricCyan:
                    sparkCol = new Color(0.40f, 0.95f, 1.00f, 0.95f);
                    break;
                case BladeTrailTheme.PrismEmerald:
                    sparkCol = new Color(0.75f, 1.00f, 0.30f, 0.95f);
                    break;
                default:
                    sparkCol = new Color(1.00f, 0.92f, 0.45f, 0.95f);
                    break;
            }

            sr.color = sparkCol;
            sr.sortingOrder = 75;
            spark.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.09f, 0.15f);

            StartCoroutine(AnimateSwipeSpark(spark));
        }

        private System.Collections.IEnumerator AnimateSwipeSpark(GameObject spark)
        {
            if (spark == null) yield break;
            Transform t = spark.transform;
            SpriteRenderer sr = spark.GetComponent<SpriteRenderer>();
            Vector3 startScale = t != null ? t.localScale : Vector3.zero;
            Color startColor = sr != null ? sr.color : Color.white;
            float dur = 0.22f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / dur;
                if (t != null) t.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
                if (sr != null) sr.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, p));
                yield return null;
            }

            if (spark != null) Destroy(spark);
        }

        private static Sprite GetSparkSprite()
        {
            if (cachedSparkSprite != null) return cachedSparkSprite;
            cachedSparkSprite = Resources.Load<Sprite>("Debris/Snip_Spark")
                             ?? Resources.Load<Sprite>("Snip_Spark");
#if UNITY_EDITOR
            if (cachedSparkSprite == null)
            {
                cachedSparkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rope/Snip_Spark.png")
                                 ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Debris/Snip_Spark.png");
            }
#endif
            return cachedSparkSprite;
        }

        private static Material GetSparkMaterial()
        {
            if (cachedSparkMaterial != null) return cachedSparkMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader != null) cachedSparkMaterial = new Material(shader);
            return cachedSparkMaterial;
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

                // Authentic Cut the Rope slicing: requires an actual swipe cutting across the rope
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
            Sprite sparkSprite = GetSparkSprite();
            Material sparkMat = GetSparkMaterial();

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
