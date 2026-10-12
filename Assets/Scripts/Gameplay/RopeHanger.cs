using UnityEngine;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Reusable Ceiling/Wall Hanger peg component for hanging ropes.
    /// Features:
    /// - Acts as fixed or swingable anchor point
    /// - Auto-aligns visual disc/pin towards camera in 2D / 2.5D
    /// - Visual hook / peg styling with customizable colors and radius
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class RopeHanger : MonoBehaviour
    {
        [Header("Hanger Visuals")]
        [SerializeField] private SpriteRenderer pegRenderer;
        [SerializeField] private float pegRadius = 0.35f;
        [SerializeField] private Color pegColor = new Color(0.85f, 0.65f, 0.45f); // Warm natural wood

        [Header("Anchor Settings")]
        [SerializeField] private Vector3 ropeAttachOffset = Vector3.zero;

        public Vector3 AttachWorldPosition => transform.TransformPoint(ropeAttachOffset);
        public float PegRadius => pegRadius;

        private void Awake()
        {
            SetupVisuals();
        }

        private void Reset()
        {
            SetupVisuals();
        }

        public void SetupVisuals()
        {
            if (pegRenderer == null)
            {
                pegRenderer = GetComponent<SpriteRenderer>();
                if (pegRenderer == null)
                {
                    pegRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

#if UNITY_EDITOR
            if (pegRenderer.sprite == null)
            {
                pegRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rope/Peg_Disc_Albedo.png");
                if (pegRenderer.sprite == null)
                {
                    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Rope/Peg_Disc_Albedo.png"))
                    {
                        if (asset is Sprite s)
                        {
                            pegRenderer.sprite = s;
                            break;
                        }
                    }
                }
            }
#endif
            if (pegRenderer != null)
            {
                pegRenderer.color = Color.white; // Render the crisp golden disc texture exactly as authored
                pegRenderer.sortingOrder = 10;
                transform.localScale = Vector3.one * (pegRadius * 2f);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(AttachWorldPosition, 0.08f);
        }
    }
}
