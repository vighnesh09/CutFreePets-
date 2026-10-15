using UnityEngine;

namespace CutFreePets.Gameplay
{
    public enum SlopeDirection
    {
        LeftToRight, // High on the left, slopes down to the right
        RightToLeft  // High on the right, slopes down to the left
    }

    /// <summary>
    /// Component: ObstacleSlopedSlide
    /// A dedicated modular wooden slide ramp with golden brass fittings.
    /// Can be oriented as a Left-to-Right or Right-to-Left slope with a single toggle.
    /// Automatically manages visual flipping and high-precision sliding physics collision.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ObstacleSlopedSlide : MonoBehaviour
    {
        [Header("Slope Configuration")]
        [SerializeField] private SlopeDirection direction = SlopeDirection.LeftToRight;
        [SerializeField] private PhysicsMaterial2D slidePhysicsMaterial;

        private SpriteRenderer spriteRenderer;
        private PolygonCollider2D polygonCollider;

        public SlopeDirection Direction
        {
            get => direction;
            set
            {
                if (direction != value)
                {
                    direction = value;
                    ApplyConfiguration();
                }
            }
        }

        private void Awake()
        {
            ApplyConfiguration();
        }

        private void OnValidate()
        {
            ApplyConfiguration();
        }

        public void ApplyConfiguration()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            // Ensure sprite assigned
            if (spriteRenderer.sprite == null)
            {
#if UNITY_EDITOR
                spriteRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Obstacle_Sloped_Slide.png");
#endif
            }
            spriteRenderer.sortingOrder = 5;

            // Flip sprite depending on slope direction
            bool isRightToLeft = (direction == SlopeDirection.RightToLeft);
            spriteRenderer.flipX = isRightToLeft;

            // Configure polygon collider
            if (polygonCollider == null) polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider == null) polygonCollider = gameObject.AddComponent<PolygonCollider2D>();

            // Setup smooth sliding physics material
            if (slidePhysicsMaterial == null)
            {
#if UNITY_EDITOR
                slidePhysicsMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/Smooth_Slide_Material.physicsMaterial2D");
#endif
            }
            if (slidePhysicsMaterial != null)
            {
                polygonCollider.sharedMaterial = slidePhysicsMaterial;
            }

            UpdateColliderPoints(isRightToLeft);
        }

        private void UpdateColliderPoints(bool flipX)
        {
            if (polygonCollider == null) return;

            // 4-point convex wedge with uninterrupted top slide surface
            Vector2[] pts = new Vector2[]
            {
                new Vector2(-1.70f, 1.25f),  // Top-left peak
                new Vector2(2.15f, -0.40f),  // Top-right exit
                new Vector2(2.15f, -1.18f),  // Bottom-right base
                new Vector2(-2.15f, -1.18f)  // Bottom-left base
            };

            if (flipX)
            {
                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i].x = -pts[i].x;
                }
                // Reverse vertex order to preserve clockwise winding
                System.Array.Reverse(pts);
            }

            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, pts);
        }
    }
}
