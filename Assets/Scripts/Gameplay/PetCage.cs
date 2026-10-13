using System;
using System.Collections.Generic;
using CutFreePets.Rope;
using UnityEngine;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Professional Pet Cage component:
    /// - Suspended by 1 or more ropes
    /// - High visibility thin-wire design so pet inside is clearly visible
    /// - Dedicated Pet Mount slot for cat/dog animation
    /// - Physical Rigidbody2D that swings and drops upon rope cutting
    /// - Handles disappearing effect on safe platform landing
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PetCage : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float cageMass = 1.5f;
        [SerializeField] private float gravityScale = 1.35f; // Smooth, authentic casual physics fall
        [SerializeField] private float linearDrag = 0.15f;
        [SerializeField] private float angularDrag = 0.5f;

        [Header("Cage Dimensions")]
        // Exact pixel bounds of Pet_Cage_Sprite (3.22m wide x 3.31m high at 256 PPU)
        [SerializeField] private Vector2 cageSize = new Vector2(3.22f, 3.31f);

        [Header("Rope Attach Point")]
        [Tooltip("Top ring local position where ropes hook onto the cage.")]
        [SerializeField] private Vector3 topRingAttachOffset = new Vector3(0f, 1.65f, 0f);

        [Header("Pet Slot")]
        [Tooltip("Slot inside the cage where the animated pet sits.")]
        [SerializeField] private Transform petSlot;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer cageRenderer;
        [SerializeField] private Sprite cageSprite;

        [Header("Idle Hanging Sway (Organic Pendulum)")]
        [Tooltip("Enables subtle organic sway while suspended on rope (Cut the Rope feel).")]
        [SerializeField] private bool enableIdleSway = true;
        [SerializeField] private float idleSwayAngle = 2.4f; // Very gentle +/- 2.4 degrees
        [SerializeField] private float idleSwaySpeed = 1.35f; // Calm natural pendulum frequency
        [SerializeField] private float inAirTiltFactor = 4.0f; // Soft organic tilt during free fall

        private float swayTimer = 0f;
        private float currentSwayZ = 0f;
        private Quaternion baseRotation = Quaternion.identity;

        // Connected ropes tracking
        private readonly List<VerletRope> connectedRopes = new List<VerletRope>();
        private Rigidbody2D rb2D;
        private Collider2D col2D;
        private bool isFreeFalling = false;
        private bool isLanded = false;

        // Events
        public event Action OnAllRopesCut;
        public event Action<Collision2D> OnLandedOnGoal;
        public event Action<Collision2D> OnLandedOnHazard;

        public Rigidbody2D Rigidbody => rb2D;
        public Vector3 AttachWorldPosition => transform.TransformPoint(topRingAttachOffset);
        public Transform PetSlot => petSlot != null ? petSlot : transform;
        public bool IsFreeFalling => isFreeFalling;
        public bool IsLanded => isLanded;
        public int ActiveRopeCount => connectedRopes.Count;

        private void Awake()
        {
            // Auto clean legacy test objects
            GameObject legacyDemo = GameObject.Find("CutTheRope_Demo");
            if (legacyDemo != null) Destroy(legacyDemo);

            rb2D = GetComponent<Rigidbody2D>();
            col2D = GetComponent<Collider2D>();
            ConfigurePhysics();
            SetupVisuals();
            EnsurePetSlot();
        }

        private void Reset()
        {
            ConfigurePhysics();
            SetupVisuals();
            EnsurePetSlot();
        }

        private void ConfigurePhysics()
        {
            if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.mass = cageMass;
                rb2D.gravityScale = gravityScale;
                rb2D.linearDamping = linearDrag;
                rb2D.angularDamping = angularDrag;
                rb2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (col2D == null) col2D = GetComponent<Collider2D>();
            if (col2D is BoxCollider2D boxCol)
            {
                boxCol.size = cageSize;
                boxCol.offset = new Vector2(0f, 0f);
            }
        }

        [Header("Back Wall Visual")]
        [SerializeField] private SpriteRenderer backRenderer;
        [SerializeField] private Sprite backSprite;

        [Header("Cage Breaking Debris on Landing")]
        [SerializeField] private bool enableBreakShatter = true;
        [SerializeField] private float debrisExplodeForce = 4.2f;
        [SerializeField] private Sprite topHandleSprite;
        [SerializeField] private Sprite doorBarsSprite;
        [SerializeField] private Sprite leftPillarSprite;
        [SerializeField] private Sprite rightPillarSprite;
        [SerializeField] private Sprite baseTraySprite;
        [SerializeField] private Sprite groundShadowSprite;
        [SerializeField] private Sprite sparkSprite;
        [SerializeField] private Sprite[] debrisSprites;

        public void SetupVisuals()
        {
            if (cageRenderer == null) cageRenderer = GetComponent<SpriteRenderer>();
            if (cageRenderer == null) cageRenderer = gameObject.AddComponent<SpriteRenderer>();

#if UNITY_EDITOR
            if (cageSprite == null)
            {
                cageSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Pet_Cage_Golden.png")
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Pet_Cage_Front.png");
            }
#endif
            EnsureDebrisSpritesLoaded();

            if (cageSprite != null)
            {
                cageRenderer.sprite = cageSprite;
                cageRenderer.color = Color.white;
            }
            cageRenderer.sortingOrder = 8; // Front bars and crate frame

            transform.localScale = Vector3.one;
        }

        public void EnsureDebrisSpritesLoaded()
        {
#if UNITY_EDITOR
            if (topHandleSprite == null)
                topHandleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Top_Handle.png");
            if (doorBarsSprite == null)
                doorBarsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Door_Bars.png");
            if (leftPillarSprite == null)
                leftPillarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Left_Pillar.png");
            if (rightPillarSprite == null)
                rightPillarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Right_Pillar.png");
            if (baseTraySprite == null)
                baseTraySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Base_Tray.png");
            if (groundShadowSprite == null)
                groundShadowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Cat_Ground_Shadow.png");
            if (sparkSprite == null)
                sparkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Debris/Snip_Spark.png");
#endif
            // Runtime Resources fallback
            if (topHandleSprite == null) topHandleSprite = Resources.Load<Sprite>("Debris/Debris_Top_Handle");
            if (doorBarsSprite == null) doorBarsSprite = Resources.Load<Sprite>("Debris/Debris_Door_Bars");
            if (leftPillarSprite == null) leftPillarSprite = Resources.Load<Sprite>("Debris/Debris_Left_Pillar");
            if (rightPillarSprite == null) rightPillarSprite = Resources.Load<Sprite>("Debris/Debris_Right_Pillar");
            if (baseTraySprite == null) baseTraySprite = Resources.Load<Sprite>("Debris/Debris_Base_Tray");
            if (groundShadowSprite == null) groundShadowSprite = Resources.Load<Sprite>("Cat_Ground_Shadow");
            if (sparkSprite == null) sparkSprite = Resources.Load<Sprite>("Debris/Snip_Spark");

            // Guaranteed procedural fallback so debris visuals CAN NEVER be null
            if (topHandleSprite == null) topHandleSprite = CreateFallbackDebrisSprite(new Color(0.95f, 0.78f, 0.25f), 64, 40);
            if (doorBarsSprite == null) doorBarsSprite = CreateFallbackDebrisSprite(new Color(0.90f, 0.72f, 0.20f), 24, 80);
            if (leftPillarSprite == null) leftPillarSprite = CreateFallbackDebrisSprite(new Color(0.85f, 0.65f, 0.18f), 40, 100);
            if (rightPillarSprite == null) rightPillarSprite = CreateFallbackDebrisSprite(new Color(0.85f, 0.65f, 0.18f), 40, 100);
            if (baseTraySprite == null) baseTraySprite = CreateFallbackDebrisSprite(new Color(0.78f, 0.55f, 0.15f), 120, 32);

            debrisSprites = new Sprite[] {
                topHandleSprite,
                doorBarsSprite,
                leftPillarSprite,
                rightPillarSprite,
                baseTraySprite
            };
        }

        private Sprite CreateFallbackDebrisSprite(Color col, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = col;
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private void EnsurePetSlot()
        {
            if (petSlot == null)
            {
                Transform existing = transform.Find("Pet_Slot");
                if (existing != null)
                {
                    petSlot = existing;
                    if (!isLanded)
                    {
                        petSlot.localPosition = new Vector3(0f, -0.28f, 0f);
                    }
                }
                else
                {
                    GameObject slotObj = new GameObject("Pet_Slot");
                    slotObj.transform.parent = transform;
                    // Position seated squarely on cage base without floating
                    slotObj.transform.localPosition = new Vector3(0f, -0.28f, 0f);
                    slotObj.transform.localRotation = Quaternion.identity;
                    slotObj.transform.localScale = Vector3.one;
                    petSlot = slotObj.transform;
                }
            }
            else if (!isLanded)
            {
                petSlot.localPosition = new Vector3(0f, -0.28f, 0f);
            }
        }

        private void Update()
        {
            if (isLanded) return;

            if (!isFreeFalling && enableIdleSway && connectedRopes.Count > 0)
            {
                // Organic breathing pendulum sway while hanging
                swayTimer += Time.deltaTime * idleSwaySpeed;
                // Double harmonic (primary wave + subtle breathing secondary wave)
                float targetAngle = (Mathf.Sin(swayTimer) * 0.75f + Mathf.Sin(swayTimer * 0.5f) * 0.25f) * idleSwayAngle;
                currentSwayZ = Mathf.Lerp(currentSwayZ, targetAngle, Time.deltaTime * 3.5f);
                transform.rotation = Quaternion.Euler(0f, 0f, currentSwayZ);
            }
            else if (isFreeFalling && rb2D != null)
            {
                // In free-fall: organic aerodynamic tilting based on horizontal drift/velocity
                float targetFallTilt = Mathf.Clamp(-rb2D.linearVelocity.x * inAirTiltFactor, -10f, 10f);
                currentSwayZ = Mathf.Lerp(currentSwayZ, targetFallTilt, Time.deltaTime * 4.5f);
                transform.rotation = Quaternion.Euler(0f, 0f, currentSwayZ);
            }
        }

        private void FixedUpdate()
        {
            if (isLanded) return;

            // When hanging, add subtle gentle pendulum lateral push so the rope slightly moves with the cage
            if (!isFreeFalling && enableIdleSway && rb2D != null && connectedRopes.Count > 0)
            {
                float lateralPush = Mathf.Cos(swayTimer) * 0.18f;
                rb2D.AddForce(new Vector2(lateralPush, 0f), ForceMode2D.Force);
            }
        }

        /// <summary>
        /// Registers a rope holding this cage.
        /// </summary>
        public void AttachRope(VerletRope rope)
        {
            if (rope == null || connectedRopes.Contains(rope)) return;

            connectedRopes.Add(rope);
            rope.OnCut += HandleRopeCut;
        }

        private void HandleRopeCut(VerletRope rope, int segmentIndex, Vector3 cutPoint)
        {
            if (connectedRopes.Contains(rope))
            {
                connectedRopes.Remove(rope);
                rope.OnCut -= HandleRopeCut;
            }

            // Smoothly fade severed rope remnants to visibility 0 and destroy them
            StartCoroutine(FadeAndRemoveSeveredRopes(rope));

            if (connectedRopes.Count == 0 && !isFreeFalling)
            {
                isFreeFalling = true;
                OnAllRopesCut?.Invoke();
                Debug.Log("<color=orange>[PetCage]</color> All ropes severed! Cage is falling.");
            }
        }

        private System.Collections.IEnumerator FadeAndRemoveSeveredRopes(VerletRope cutRope)
        {
            // Give severed piece a frame to spawn
            yield return null;

            // 1. TOP REMNANT (attached to ceiling hanger/peg):
            // Fade its alpha to 0 over 0.8s so it gently disappears from the ceiling
            if (cutRope != null)
            {
                var topRopeRenderer = cutRope.GetComponent<RopeRenderer>();
                var topLr = cutRope.GetComponent<LineRenderer>();
                var topKnots = cutRope.GetComponentsInChildren<SpriteRenderer>();

                float duration = 0.8f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Clamp01(1f - (elapsed / duration));

                    if (topRopeRenderer != null) topRopeRenderer.FadeAlpha = alpha;
                    if (topLr != null)
                    {
                        Color sc = topLr.startColor; sc.a = alpha; topLr.startColor = sc;
                        Color ec = topLr.endColor; ec.a = alpha; topLr.endColor = ec;
                    }
                    foreach (var sr in topKnots)
                    {
                        if (sr != null) { Color c = sr.color; c.a = alpha; sr.color = c; }
                    }

                    yield return null;
                }

                if (cutRope != null && cutRope.gameObject != null)
                {
                    Destroy(cutRope.gameObject);
                }
            }
        }

        /// <summary>
        /// Called when the cage touches the landing platform.
        /// Shatters open into beautiful scattering parts with physics,
        /// creates a soft floor shadow, and firmly grounds the cat onto the platform floor!
        /// </summary>
        public void BreakAndFreePet(Collision2D collision = null)
        {
            if (isLanded) return;
            isLanded = true;

            // Ensure all debris resources are ready
            EnsureDebrisSpritesLoaded();

            // Hide intact cage visuals and disable collision/simulation
            if (cageRenderer != null) cageRenderer.enabled = false;
            if (backRenderer != null) backRenderer.enabled = false;
            if (col2D != null) col2D.enabled = false;
            if (rb2D != null) rb2D.simulated = false;

            // Instantly disappear any remaining severed rope pieces when cage breaks
            VerletRope[] remainingRopes = FindObjectsByType<VerletRope>(FindObjectsSortMode.None);
            foreach (var r in remainingRopes)
            {
                if (r != null && (r.name.Contains("SeveredPiece") || r.IsCut))
                {
                    Destroy(r.gameObject);
                }
            }

            // Determine accurate platform surface height
            float platformTopY = -2.30f;
            if (collision != null && collision.collider != null)
            {
                platformTopY = collision.collider.bounds.max.y;
            }
            else
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 6f);
                if (hit.collider != null)
                {
                    platformTopY = hit.collider.bounds.max.y;
                }
            }

            Vector3 cagePos = transform.position;

            // 1. SPAWN ANATOMICAL DEBRIS PIECES BURSTING OPEN
            if (enableBreakShatter)
            {
                // Top Handle: pops straight up and tumbles
                SpawnDebrisPiece(topHandleSprite, cagePos + new Vector3(0f, 1.45f, 0f), 0.70f,
                    new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), 4.2f), UnityEngine.Random.Range(180f, 260f));

                // Left Pillar: bursts outward left and up with CCW rotation
                SpawnDebrisPiece(leftPillarSprite, cagePos + new Vector3(-0.95f, 0.10f, 0f), 0.60f,
                    new Vector2(-2.8f, 2.0f), -320f);

                // Right Pillar: bursts outward right and up with CW rotation
                SpawnDebrisPiece(rightPillarSprite, cagePos + new Vector3(0.95f, 0.10f, 0f), 0.60f,
                    new Vector2(2.8f, 2.0f), 320f);

                // Door Bars: pops forward/down
                SpawnDebrisPiece(doorBarsSprite, cagePos + new Vector3(0f, 0.05f, 0f), 0.60f,
                    new Vector2(UnityEngine.Random.Range(-0.6f, 0.6f), 1.8f), 180f);

                // Base Tray: falls onto platform surface
                SpawnDebrisPiece(baseTraySprite, cagePos + new Vector3(0f, -1.35f, 0f), 0.65f,
                    new Vector2(UnityEngine.Random.Range(-0.3f, 0.3f), 0.8f), 60f);

                // 2. SPAWN 8 GOLDEN STAR SPARKLES IN A 360 DEGREE BURST
                if (sparkSprite != null)
                {
                    for (int s = 0; s < 8; s++)
                    {
                        float angle = s * (360f / 8f) + UnityEngine.Random.Range(-12f, 12f);
                        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                        SpawnSparkPiece(sparkSprite, cagePos + new Vector3(dir.x * 0.25f, dir.y * 0.25f, 0f), dir * 4.4f);
                    }
                }
            }

            // 3. SPAWN GROUND CONTACT SHADOW ON PLATFORM
            if (groundShadowSprite != null)
            {
                GameObject shadowObj = new GameObject("Cat_Ground_Shadow");
                shadowObj.transform.position = new Vector3(cagePos.x, platformTopY + 0.02f, 0f);
                shadowObj.transform.localScale = new Vector3(1.7f, 0.42f, 1f);

                SpriteRenderer srShadow = shadowObj.AddComponent<SpriteRenderer>();
                srShadow.sprite = groundShadowSprite;
                srShadow.color = new Color(0f, 0f, 0f, 0.45f);
                srShadow.sortingOrder = 4; // Between platform (3) and cat (6)
            }

            // 4. FIRMLY GROUND CAT ONTO PLATFORM (ZERO FLOATING)
            if (petSlot != null)
            {
                petSlot.SetParent(null, true);

                // Paws reach 0.85 units below center. Placing center at platformTopY + 0.86f
                // seats paws cleanly on the platform floor surface without sinking into it!
                float targetCatY = platformTopY + 0.86f;
                Vector3 targetGroundedPos = new Vector3(cagePos.x, targetCatY, 0f);
                petSlot.position = targetGroundedPos;

                CatAnimationPlayer cat = petSlot.GetComponentInChildren<CatAnimationPlayer>();
                if (cat != null)
                {
                    cat.SetMood(CatMood.Happy);
                    StartCoroutine(GroundedLandingBounce(cat.transform, targetGroundedPos));
                }
            }

            // 5. CAMERA IMPACT MICRO-SHAKE
            StartCoroutine(CameraImpactShake(0.06f, 0.16f));

            if (collision != null)
            {
                OnLandedOnGoal?.Invoke(collision);
            }

            Debug.Log("<color=green>[PetCage]</color> CAGE SHATTERED OPEN! Cat solidly grounded on platform with contact shadow.");
        }

        private void SpawnDebrisPiece(Sprite spr, Vector3 startPos, float scale, Vector2 velocity, float angularVel)
        {
            if (spr == null) return;

            GameObject debObj = new GameObject($"Debris_{spr.name}");
            debObj.transform.position = startPos;
            debObj.transform.rotation = transform.rotation;
            debObj.transform.localScale = transform.localScale * scale;

            SpriteRenderer sr = debObj.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = 9; // In front of platform and cat

            Rigidbody2D debRb = debObj.AddComponent<Rigidbody2D>();
            debRb.gravityScale = 2.2f;
            debRb.linearVelocity = velocity;
            debRb.angularVelocity = angularVel;

            StartCoroutine(FadeAndDestroyDebris(debObj, sr, 0.85f, 0.45f));
        }

        private void SpawnSparkPiece(Sprite spr, Vector3 startPos, Vector2 velocity)
        {
            if (spr == null) return;

            GameObject sparkObj = new GameObject("Debris_Spark");
            sparkObj.transform.position = startPos;
            sparkObj.transform.localScale = Vector3.one * 0.55f;

            SpriteRenderer ssr = sparkObj.AddComponent<SpriteRenderer>();
            ssr.sprite = spr;
            ssr.sortingOrder = 11;
            ssr.color = new Color(1f, 0.92f, 0.35f, 1f);

            Rigidbody2D srb = sparkObj.AddComponent<Rigidbody2D>();
            srb.gravityScale = 0.8f;
            srb.linearVelocity = velocity;
            srb.angularVelocity = UnityEngine.Random.Range(-400f, 400f);

            StartCoroutine(FadeAndDestroyDebris(sparkObj, ssr, 0.45f, 0.35f));
        }

        /// <summary>
        /// Grounded cartoon squash & stretch impact bounce.
        /// Critically keeps the cat's feet planted firmly on the platform (no hovering in mid-air).
        /// </summary>
        private System.Collections.IEnumerator GroundedLandingBounce(Transform catTransform, Vector3 groundPos)
        {
            Vector3 baseScale = new Vector3(0.9f, 0.9f, 1f);
            float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (catTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Position stays firmly grounded
                catTransform.position = groundPos;

                // Dynamic cartoon squash & stretch:
                // Phase 1 (0 -> 0.15s): Squash down upon landing impact
                // Phase 2 (0.15 -> 0.30s): Rebound stretch up happily
                // Phase 3 (0.30 -> 0.42s): Settle cleanly to base scale
                float squashWave = Mathf.Sin(t * Mathf.PI * 2f) * Mathf.Exp(-t * 3.5f);
                float scaleX = baseScale.x * (1f + squashWave * 0.14f);
                float scaleY = baseScale.y * (1f - squashWave * 0.14f);
                catTransform.localScale = new Vector3(scaleX, scaleY, baseScale.z);

                yield return null;
            }

            if (catTransform != null)
            {
                catTransform.position = groundPos;
                catTransform.localScale = baseScale;
            }
        }

        private System.Collections.IEnumerator CameraImpactShake(float intensity, float duration)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3 origPos = cam.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentIntensity = Mathf.Lerp(intensity, 0f, elapsed / duration);
                cam.transform.position = origPos + (Vector3)(UnityEngine.Random.insideUnitCircle * currentIntensity);
                yield return null;
            }

            if (cam != null) cam.transform.position = origPos;
        }

        private System.Collections.IEnumerator FadeAndDestroyDebris(GameObject debObj, SpriteRenderer sr, float delay, float fadeDuration)
        {
            yield return new WaitForSeconds(delay);
            float elapsed = 0f;
            Color c = sr != null ? sr.color : Color.white;
            Vector3 initScale = debObj != null ? debObj.transform.localScale : Vector3.one;

            while (elapsed < fadeDuration)
            {
                if (sr == null || debObj == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float alpha = Mathf.Lerp(c.a, 0f, t);
                sr.color = new Color(c.r, c.g, c.b, alpha);
                debObj.transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                yield return null;
            }
            if (debObj != null) Destroy(debObj);
        }

        public void OpenAndDisappear()
        {
            BreakAndFreePet();
        }

        /// <summary>
        /// Called on failed landing/crash.
        /// </summary>
        public void BreakOnCrash()
        {
            BreakAndFreePet();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Landing detection when hitting platform
            if (collision.gameObject.name.Contains("Platform") || collision.gameObject.CompareTag("Finish"))
            {
                BreakAndFreePet(collision);
            }
            else if (collision.gameObject.name.Contains("Ground") || collision.gameObject.name.Contains("Hazard"))
            {
                BreakAndFreePet(collision);
                OnLandedOnHazard?.Invoke(collision);
            }
            else
            {
                // Fallback: If landing on top of any collider
                if (collision.contactCount > 0 && collision.GetContact(0).normal.y > 0.45f)
                {
                    BreakAndFreePet(collision);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(AttachWorldPosition, 0.08f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, cageSize);
        }
    }
}
