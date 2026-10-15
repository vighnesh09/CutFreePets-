using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CutFreePets.Rope;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Component: PetCage
    /// Master Coordinator for the pet cage:
    /// - Tracks connected ropes and initiates free-fall when all ropes are severed
    /// - Coordinates with CageHangingPhysics for pendulum sway
    /// - Coordinates with CageBreakEffect for shattering debris and pet freeing upon landing
    /// - Holds the pet mount slot for animated characters
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PetCage : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float cageMass = 1.5f;
        [SerializeField] private float gravityScale = 1.35f;
        [SerializeField] private float linearDrag = 0.15f;
        [SerializeField] private float angularDrag = 0.5f;

        [Header("Cage Dimensions")]
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

        // Subcomponents
        private CageHangingPhysics hangingPhysics;
        private CageBreakEffect breakEffect;

        // Internal State
        private readonly List<VerletRope> connectedRopes = new List<VerletRope>();
        private Rigidbody2D rb2D;
        private Collider2D col2D;
        private bool isFreeFalling = false;
        private bool isLanded = false;

        // Public Events
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
            rb2D = GetComponent<Rigidbody2D>();
            col2D = GetComponent<Collider2D>();
            hangingPhysics = GetComponent<CageHangingPhysics>() ?? gameObject.AddComponent<CageHangingPhysics>();
            breakEffect = GetComponent<CageBreakEffect>() ?? gameObject.AddComponent<CageBreakEffect>();

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
            cageRenderer.sortingOrder = 8;
            transform.localScale = Vector3.one;
        }

        public void EnsureDebrisSpritesLoaded()
        {
            if (breakEffect == null) breakEffect = GetComponent<CageBreakEffect>() ?? gameObject.AddComponent<CageBreakEffect>();
            breakEffect.EnsureDebrisSpritesLoaded();
        }

        private void EnsurePetSlot()
        {
            if (petSlot == null)
            {
                Transform existing = transform.Find("Pet_Slot");
                if (existing != null)
                {
                    petSlot = existing;
                    if (!isLanded) petSlot.localPosition = new Vector3(0f, -0.28f, 0f);
                }
                else
                {
                    GameObject slotObj = new GameObject("Pet_Slot");
                    slotObj.transform.parent = transform;
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

            if (hangingPhysics != null)
            {
                hangingPhysics.UpdateSway(isLanded, isFreeFalling, connectedRopes.Count);
            }

            // Proximity landing failsafe for Ground_Platform at ~ -3.60f
            if (isFreeFalling && transform.position.y <= -2.10f)
            {
                BreakAndFreePet();
            }
        }

        private void FixedUpdate()
        {
            if (isLanded) return;

            if (hangingPhysics != null)
            {
                hangingPhysics.FixedUpdatePush(isLanded, isFreeFalling, connectedRopes.Count);
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

            StartCoroutine(FadeAndRemoveSeveredRopes(rope));

            if (connectedRopes.Count == 0 && !isFreeFalling && !isLanded)
            {
                isFreeFalling = true;
                OnAllRopesCut?.Invoke();
                Debug.Log("<color=cyan>[PetCage]</color> ALL ROPES CUT! Cage dropped into free-fall.");
            }
        }

        private IEnumerator FadeAndRemoveSeveredRopes(VerletRope rope)
        {
            yield return new WaitForSeconds(0.40f);
            if (rope != null && (rope.name.Contains("SeveredPiece") || rope.IsCut))
            {
                Destroy(rope.gameObject);
            }
        }

        /// <summary>
        /// Breaks open cage and frees pet upon landing.
        /// </summary>
        public void BreakAndFreePet(Collision2D collision = null)
        {
            if (isLanded) return;
            isLanded = true;

            // Hide cage visuals and collision
            if (cageRenderer != null) cageRenderer.enabled = false;
            if (col2D != null) col2D.enabled = false;
            if (rb2D != null) rb2D.simulated = false;

            // Clean up remaining rope pieces
            VerletRope[] remainingRopes = FindObjectsByType<VerletRope>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var r in remainingRopes)
            {
                if (r != null && (r.name.Contains("SeveredPiece") || r.IsCut))
                {
                    Destroy(r.gameObject);
                }
            }

            // Determine platform surface height
            float platformTopY = -3.60f;
            if (collision != null && collision.collider != null)
            {
                platformTopY = collision.collider.bounds.max.y;
            }
            else
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 8f);
                if (hit.collider != null) platformTopY = hit.collider.bounds.max.y;
            }

            // Execute modular shatter and pet grounding effect
            if (breakEffect == null) breakEffect = GetComponent<CageBreakEffect>() ?? gameObject.AddComponent<CageBreakEffect>();
            breakEffect.PlayBreakSequence(transform.position, transform.rotation, platformTopY, petSlot);

            OnLandedOnGoal?.Invoke(collision);
        }

        public void OpenAndDisappear()
        {
            BreakAndFreePet();
        }

        public void BreakOnCrash()
        {
            BreakAndFreePet();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.name.Contains("Platform") || collision.gameObject.CompareTag("Finish"))
            {
                BreakAndFreePet(collision);
            }
            else if (collision.gameObject.name.Contains("Ground") || collision.gameObject.name.Contains("Hazard"))
            {
                BreakAndFreePet(collision);
                OnLandedOnHazard?.Invoke(collision);
            }
            else if (collision.contactCount > 0 && collision.GetContact(0).normal.y > 0.45f)
            {
                BreakAndFreePet(collision);
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
