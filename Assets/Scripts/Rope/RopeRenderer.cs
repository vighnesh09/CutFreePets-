using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Professional Cut the Rope visual renderer with braided texture tiling,
    /// dynamic Catmull-Rom spline curves, stretch strain thinning & trembling,
    /// and end knot ring attached to the suspended object.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RopeRenderer : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Material ropeMaterial;

        [Header("Braided Texture & Width")]
        [Tooltip("Base thickness of the rope when relaxed.")]
        [SerializeField] private float baseThickness = 0.075f;

        [Tooltip("Thickness ratio when under maximum tension (gets thinner under tension).")]
        [Range(0.3f, 1f)]
        [SerializeField] private float minStretchThicknessRatio = 0.65f;

        [Tooltip("Number of braided texture repetitions per world unit.")]
        [SerializeField] private float textureRepeatPerUnit = 5.5f;

        [Header("End Knot Visuals")]
        [Tooltip("Show tied knot ring at the end anchor.")]
        [SerializeField] private bool showEndKnot = true;
        [SerializeField] private Sprite knotSprite;

        [Header("Spline Smoothing")]
        [Range(2, 10)]
        [SerializeField] private int splineSubdivisions = 6;

        private VerletRope rope;
        private readonly System.Collections.Generic.List<Vector3> smoothedPoints = new System.Collections.Generic.List<Vector3>();
        private GameObject endKnotInstance;
        private SpriteRenderer endKnotRenderer;
        private float fadeAlpha = 1.0f;

        public bool ShowEndKnot { get => showEndKnot; set => showEndKnot = value; }
        public float FadeAlpha { get => fadeAlpha; set => fadeAlpha = Mathf.Clamp01(value); }
        public float BaseThickness { get => baseThickness; set => baseThickness = value; }

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
            LoadDefaultAssetsIfMissing();
        }

        private void LoadDefaultAssetsIfMissing()
        {
#if UNITY_EDITOR
            if (ropeMaterial == null)
            {
                ropeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Rope_Material.mat");
            }
            if (knotSprite == null)
            {
                knotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rope/Knot_Ring_Albedo.png");
                if (knotSprite == null)
                {
                    foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Rope/Knot_Ring_Albedo.png"))
                    {
                        if (asset is Sprite s)
                        {
                            knotSprite = s;
                            break;
                        }
                    }
                }
            }
#endif
            if (ropeMaterial != null && lineRenderer != null)
            {
                lineRenderer.material = ropeMaterial;
            }
        }

        private void ConfigureLineRenderer()
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.numCornerVertices = 6;
            lineRenderer.numCapVertices = 6;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        public void Initialize(VerletRope targetRope)
        {
            rope = targetRope;
            if (rope != null && rope.Config != null)
            {
                baseThickness = rope.Config.baseWidth;
                minStretchThicknessRatio = rope.Config.minStretchWidthFactor;
            }
            CreateEndAccessories();
        }

        private void CreateEndAccessories()
        {
            if (showEndKnot && knotSprite != null && endKnotInstance == null)
            {
                endKnotInstance = new GameObject($"{gameObject.name}_EndKnot");
                endKnotInstance.transform.parent = transform;
                endKnotRenderer = endKnotInstance.AddComponent<SpriteRenderer>();
                endKnotRenderer.sprite = knotSprite;
                endKnotRenderer.sortingOrder = 7;
                endKnotInstance.transform.localScale = Vector3.one * 0.38f;
            }
        }

        public void CopySettingsFrom(RopeRenderer source, bool preserveEndKnot = false)
        {
            if (source == null) return;
            baseThickness = source.baseThickness;
            minStretchThicknessRatio = source.minStretchThicknessRatio;
            textureRepeatPerUnit = source.textureRepeatPerUnit;
            splineSubdivisions = source.splineSubdivisions;
            showEndKnot = preserveEndKnot;
            knotSprite = source.knotSprite;
            ropeMaterial = source.ropeMaterial;

            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null && source.lineRenderer != null)
            {
                lineRenderer.sharedMaterial = source.lineRenderer.sharedMaterial;
                lineRenderer.textureMode = LineTextureMode.Tile;
                lineRenderer.alignment = LineAlignment.View;
            }
        }

        private void LateUpdate()
        {
            if (rope == null || rope.Particles == null || rope.Particles.Count < 2) return;

            UpdateSplineMesh();
            UpdateTensionFeedback();
            UpdateKnotPosition();
        }

        private void UpdateSplineMesh()
        {
            var particles = rope.Particles;
            int count = particles.Count;
            if (count < 2) return;

            smoothedPoints.Clear();

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 p0 = i > 0 ? particles[i - 1].Position : (particles[0].Position + (particles[0].Position - particles[1].Position));
                Vector3 p1 = particles[i].Position;
                Vector3 p2 = particles[i + 1].Position;
                Vector3 p3 = i < count - 2 ? particles[i + 2].Position : (particles[count - 1].Position + (particles[count - 1].Position - particles[count - 2].Position));

                for (int step = 0; step < splineSubdivisions; step++)
                {
                    float t = (float)step / splineSubdivisions;
                    Vector3 point = EvaluateCatmullRom(p0, p1, p2, p3, t);
                    smoothedPoints.Add(point);
                }
            }

            smoothedPoints.Add(particles[count - 1].Position);

            lineRenderer.positionCount = smoothedPoints.Count;
            lineRenderer.SetPositions(smoothedPoints.ToArray());
        }

        private void UpdateTensionFeedback()
        {
            float tensionRatio = rope.CurrentTensionRatio;
            float tensionNormalized = Mathf.Clamp01((tensionRatio - 1.0f) / 0.25f);

            // Dynamic thickness thinning under load
            float currentThickness = Mathf.Lerp(baseThickness, baseThickness * minStretchThicknessRatio, tensionNormalized);
            lineRenderer.startWidth = currentThickness;
            lineRenderer.endWidth = currentThickness;

            // Cut the Rope color tinting: Warm golden brown when normal, subtle reddish warning when strained
            Color normalColor = rope.Config != null ? rope.Config.normalColor : new Color(0.92f, 0.78f, 0.58f);
            Color strainColor = rope.Config != null ? rope.Config.highTensionColor : new Color(1f, 0.45f, 0.35f);
            Color blendedColor = Color.Lerp(normalColor, strainColor, tensionNormalized);
            blendedColor.a *= fadeAlpha;

            lineRenderer.startColor = blendedColor;
            lineRenderer.endColor = blendedColor;

            // Texture UV tiling along length
            if (lineRenderer.material != null)
            {
                float totalLength = rope.CurrentTotalLength;
                lineRenderer.material.mainTextureScale = new Vector2(totalLength * textureRepeatPerUnit, 1f);
            }
        }

        private void UpdateKnotPosition()
        {
            if (!showEndKnot || endKnotInstance == null || rope.Particles == null || rope.Particles.Count < 2) return;

            if (endKnotRenderer != null)
            {
                Color kc = endKnotRenderer.color;
                kc.a = fadeAlpha;
                endKnotRenderer.color = kc;
            }

            Vector3 endPos = rope.Particles[rope.Particles.Count - 1].Position;
            Vector3 prevPos = rope.Particles[rope.Particles.Count - 2].Position;
            endKnotInstance.transform.position = new Vector3(endPos.x, endPos.y, endPos.z - 0.01f);

            Vector3 dir = (endPos - prevPos).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                endKnotInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }
        }

        private Vector3 EvaluateCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private void OnDestroy()
        {
            if (endKnotInstance != null) Destroy(endKnotInstance);
        }
    }
}
