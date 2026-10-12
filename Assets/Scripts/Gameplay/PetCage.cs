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
        [SerializeField] private Sprite[] debrisSprites;
        [SerializeField] private float debrisExplodeForce = 3.8f;

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
            if (debrisSprites == null || debrisSprites.Length == 0)
            {
                string[] debrisPaths = new string[] {
                    "Assets/Art/Environment/Debris/Debris_Top_Handle.png",
                    "Assets/Art/Environment/Debris/Debris_Door_Bars.png",
                    "Assets/Art/Environment/Debris/Debris_Left_Pillar.png",
                    "Assets/Art/Environment/Debris/Debris_Right_Pillar.png",
                    "Assets/Art/Environment/Debris/Debris_Base_Tray.png"
                };
                List<Sprite> list = new List<Sprite>();
                foreach (var p in debrisPaths)
                {
                    Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (s != null) list.Add(s);
                }
                debrisSprites = list.ToArray();
            }
#endif
            if (cageSprite != null)
            {
                cageRenderer.sprite = cageSprite;
                cageRenderer.color = Color.white;
            }
            cageRenderer.sortingOrder = 8; // Front bars and crate frame

            transform.localScale = Vector3.one;
        }

        private void EnsurePetSlot()
        {
            if (petSlot == null)
            {
                Transform existing = transform.Find("Pet_Slot");
                if (existing != null)
                {
                    petSlot = existing;
                }
                else
                {
                    GameObject slotObj = new GameObject("Pet_Slot");
                    slotObj.transform.parent = transform;
                    slotObj.transform.localPosition = new Vector3(0f, -0.15f, 0f);
                    slotObj.transform.localRotation = Quaternion.identity;
                    slotObj.transform.localScale = Vector3.one;
                    petSlot = slotObj.transform;
                }
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

            if (connectedRopes.Count == 0 && !isFreeFalling)
            {
                isFreeFalling = true;
                OnAllRopesCut?.Invoke();
                Debug.Log("<color=orange>[PetCage]</color> All ropes severed! Cage is falling.");
            }
        }

        /// <summary>
        /// Called when the cage touches the landing platform.
        /// Shatters and breaks open the cage with physical scattering debris,
        /// freeing the cat onto the platform!
        /// </summary>
        public void BreakAndFreePet(Collision2D collision = null)
        {
            if (isLanded) return;
            isLanded = true;

            // Hide intact cage visuals
            if (cageRenderer != null) cageRenderer.enabled = false;
            if (backRenderer != null) backRenderer.enabled = false;
            if (col2D != null) col2D.enabled = false;
            if (rb2D != null) rb2D.simulated = false;

            // Spawn dynamic scattering debris pieces
            if (enableBreakShatter && debrisSprites != null && debrisSprites.Length > 0)
            {
                Vector2[] scatterDirections = new Vector2[] {
                    new Vector2(0f, 1.25f),      // Top handle (upwards)
                    new Vector2(0.4f, 0.95f),    // Door bars (front/up)
                    new Vector2(-1.25f, 0.85f),  // Left pillar (left/up)
                    new Vector2(1.25f, 0.85f),   // Right pillar (right/up)
                    new Vector2(0f, 0.35f)       // Base tray (gentle pop)
                };

                for (int i = 0; i < debrisSprites.Length; i++)
                {
                    Sprite deb = debrisSprites[i];
                    if (deb == null) continue;

                    GameObject debObj = new GameObject($"Debris_{deb.name}");
                    debObj.transform.position = transform.position;
                    debObj.transform.rotation = transform.rotation;
                    debObj.transform.localScale = transform.localScale;

                    SpriteRenderer sr = debObj.AddComponent<SpriteRenderer>();
                    sr.sprite = deb;
                    sr.sortingOrder = 8;

                    Rigidbody2D debRb = debObj.AddComponent<Rigidbody2D>();
                    debRb.gravityScale = 1.6f;
                    Vector2 dir = (i < scatterDirections.Length) ? scatterDirections[i] : UnityEngine.Random.insideUnitCircle.normalized;
                    debRb.linearVelocity = dir * debrisExplodeForce + new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), 0f);
                    debRb.angularVelocity = UnityEngine.Random.Range(-180f, 180f);

                    StartCoroutine(FadeAndDestroyDebris(debObj, sr, 0.8f));
                }
            }

            // Free the cat on the platform and switch mood to Happy
            if (petSlot != null)
            {
                petSlot.SetParent(null, true);
                CatAnimationPlayer cat = petSlot.GetComponentInChildren<CatAnimationPlayer>();
                if (cat != null)
                {
                    cat.SetMood(CatMood.Happy);
                }
            }

            if (collision != null)
            {
                OnLandedOnGoal?.Invoke(collision);
            }

            Debug.Log("<color=green>[PetCage]</color> CAGE BROKE OPEN! Debris scattered and pet is freed!");
        }

        private System.Collections.IEnumerator FadeAndDestroyDebris(GameObject debObj, SpriteRenderer sr, float delay)
        {
            yield return new WaitForSeconds(delay);
            float duration = 0.6f;
            float elapsed = 0f;
            Color c = sr != null ? sr.color : Color.white;
            while (elapsed < duration)
            {
                if (sr == null || debObj == null) yield break;
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                sr.color = new Color(c.r, c.g, c.b, alpha);
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
