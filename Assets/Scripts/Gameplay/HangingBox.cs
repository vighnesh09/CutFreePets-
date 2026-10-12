using System;
using System.Collections.Generic;
using CutFreePets.Rope;
using UnityEngine;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Reusable Hanging Box component (pet crate / candy / cage placeholder).
    /// Features:
    /// - Suspended by one or multiple ropes
    /// - Physical Rigidbody2D with realistic mass, drag, and gravity
    /// - Falls immediately when all supporting ropes are cut
    /// - Draggable for interactive physics & tension testing
    /// - Collision detection on landing or hitting boundaries
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class HangingBox : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float boxMass = 2.0f;
        [SerializeField] private float gravityScale = 2.2f;
        [SerializeField] private float linearDrag = 0.4f;
        [SerializeField] private float angularDrag = 0.8f;

        [Header("Visuals")]
        [SerializeField] private Vector2 boxSize = new Vector2(0.85f, 0.85f);
        [SerializeField] private Color boxColor = new Color(0.95f, 0.55f, 0.22f); // Vibrant orange crate
        [SerializeField] private SpriteRenderer boxRenderer;

        [Header("Rope Attach Points")]
        [Tooltip("Local offset where ropes bind to this box.")]
        [SerializeField] private Vector3 tieOffset = new Vector3(0f, 0.4f, 0f);

        // Connected ropes tracking
        private readonly List<VerletRope> connectedRopes = new List<VerletRope>();
        private Rigidbody2D rb2D;
        private Collider2D col2D;
        private bool isFreeFalling = false;

        // Events
        public event Action OnAllRopesCut;
        public event Action<Collision2D> OnLanded;

        public Rigidbody2D Rigidbody => rb2D;
        public Vector3 TieWorldPosition => transform.TransformPoint(tieOffset);
        public bool IsFreeFalling => isFreeFalling;
        public int ActiveRopeCount => connectedRopes.Count;

        private void Awake()
        {
            // Clean up any legacy prototype objects if they linger in the scene
            GameObject legacyDemo = GameObject.Find("CutTheRope_Demo");
            if (legacyDemo != null) Destroy(legacyDemo);

            rb2D = GetComponent<Rigidbody2D>();
            col2D = GetComponent<Collider2D>();
            ConfigurePhysics();
            SetupBoxVisuals();
        }

        private void Reset()
        {
            ConfigurePhysics();
            SetupBoxVisuals();
        }

        private void ConfigurePhysics()
        {
            if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.mass = boxMass;
                rb2D.gravityScale = gravityScale;
                rb2D.linearDamping = linearDrag;
                rb2D.angularDamping = angularDrag;
                rb2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            if (col2D == null) col2D = GetComponent<Collider2D>();
            if (col2D is BoxCollider2D boxCol)
            {
                boxCol.size = boxSize;
            }
        }

        public void SetupBoxVisuals()
        {
            if (boxRenderer == null) boxRenderer = GetComponent<SpriteRenderer>();
            if (boxRenderer == null) boxRenderer = gameObject.AddComponent<SpriteRenderer>();

            // Generate clean square sprite if none assigned
            if (boxRenderer.sprite == null)
            {
                boxRenderer.sprite = GetOrCreateSquareSprite();
            }

            boxRenderer.color = boxColor;
            boxRenderer.sortingOrder = 6;
            transform.localScale = new Vector3(boxSize.x, boxSize.y, 1f);
        }

        private Sprite GetOrCreateSquareSprite()
        {
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Clean border bevel
                    bool isBorder = (x <= 1 || x >= 30 || y <= 1 || y >= 30);
                    pixels[y * 32 + x] = isBorder ? new Color(0.8f, 0.45f, 0.15f) : Color.white;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        /// <summary>
        /// Registers a rope that is holding this box.
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

            // If no more ropes are holding the box, it enters full freefall
            if (connectedRopes.Count == 0 && !isFreeFalling)
            {
                isFreeFalling = true;
                OnAllRopesCut?.Invoke();
                Debug.Log("<color=orange>[HangingBox]</color> All ropes cut! Box is falling freely.");
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            OnLanded?.Invoke(collision);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(TieWorldPosition, 0.08f);
        }
    }
}
