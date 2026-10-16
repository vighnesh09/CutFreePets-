using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CutFreePets.Rope
{
    /// <summary>
    /// Component: RopeRenderer
    /// Renders a vibrant, realistic cartoon braided hemp rope using a smooth Catmull-Rom spline
    /// and maintains physical tied rope knots along the rope length that divide naturally upon cutting.
    /// Fully compliant with URP 2D and Cut the Rope aesthetics.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public class RopeRenderer : MonoBehaviour
    {
        [System.Serializable]
        public class BodyKnot
        {
            public RopeParticle boundParticle;
            public GameObject instance;
            public SpriteRenderer renderer;
        }

        [Header("Components")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Material ropeMaterial;

        [Header("Braided Texture & Width")]
        [Tooltip("Base thickness of the rope when relaxed.")]
        [SerializeField] private float baseThickness = 0.135f;

        [Tooltip("Thickness ratio when under maximum tension (gets thinner under tension).")]
        [Range(0.3f, 1f)]
        [SerializeField] private float minStretchThicknessRatio = 0.72f;

        [Header("Knots Along Rope")]
        [Tooltip("Number of tied knots spaced along the length of the rope.")]
        [Range(0, 5)]
        [SerializeField] private int knotCount = 2;

        [Tooltip("Sprite for tied knots placed along the rope.")]
        [SerializeField] private Sprite bodyKnotSprite;

        [Tooltip("Scale multiplier for the tied knots.")]
        [SerializeField] private float knotScale = 0.27f;

        [Header("Spline Smoothing")]
        [Range(2, 10)]
        [SerializeField] private int splineSubdivisions = 6;

        // Internal State
        private VerletRope rope;
        private readonly List<Vector3> smoothedPoints = new List<Vector3>();
        private readonly List<BodyKnot> bodyKnots = new List<BodyKnot>();
        private float fadeAlpha = 1.0f;

        public float FadeAlpha { get => fadeAlpha; set => fadeAlpha = Mathf.Clamp01(value); }
        public float BaseThickness { get => baseThickness; set => baseThickness = value; }
        public int KnotCount { get => knotCount; set { knotCount = value; RebuildBodyKnots(); } }

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
            LoadDefaultAssetsIfMissing();
            CleanupLegacyEndKnots();
        }

        private void Start()
        {
            if (bodyKnots.Count == 0 && (rope == null || !rope.IsCut))
            {
                RebuildBodyKnots();
            }
        }

        private void CleanupLegacyEndKnots()
        {
            // Remove legacy Knot_Ring_Albedo end accessories if any exist
            Transform legacyKnot = transform.Find($"{gameObject.name}_EndKnot");
            if (legacyKnot != null) Destroy(legacyKnot.gameObject);
        }

        private void LoadDefaultAssetsIfMissing()
        {
            if (bodyKnotSprite == null)
            {
                bodyKnotSprite = Resources.Load<Sprite>("Rope_Body_Knot");
#if UNITY_EDITOR
                if (bodyKnotSprite == null)
                {
                    bodyKnotSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rope/Rope_Body_Knot.png");
                }
#endif
            }

            // Always prioritize precompiled, serialized Rope_Material asset for 100% cross-platform parity
            if (ropeMaterial == null)
            {
                ropeMaterial = Resources.Load<Material>("Rope_Material");
#if UNITY_EDITOR
                if (ropeMaterial == null)
                {
                    ropeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Rope_Material.mat");
                }
#endif
            }

            if (lineRenderer != null)
            {
                Material mat = null;
                if (ropeMaterial != null)
                {
                    mat = new Material(ropeMaterial);
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Sprites/Default");
                    if (shader != null) mat = new Material(shader);
                }

                Texture2D ropeTex = Resources.Load<Texture2D>("Rope_Braided_Albedo");
#if UNITY_EDITOR
                if (ropeTex == null)
                {
                    ropeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Rope/Rope_Braided_Albedo.png");
                }
#endif
                if (mat != null)
                {
                    if (ropeTex != null)
                    {
                        ropeTex.wrapMode = TextureWrapMode.Repeat;
                        mat.mainTexture = ropeTex;
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", ropeTex);
                    }
                    mat.color = Color.white;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                    lineRenderer.material = mat;
                }
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
            lineRenderer.sortingOrder = 8;
            lineRenderer.startWidth = baseThickness;
            lineRenderer.endWidth = baseThickness;
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
        }

        public void Initialize(VerletRope targetRope)
        {
            rope = targetRope;
            if (rope != null && rope.Config != null)
            {
                baseThickness = Mathf.Max(0.06f, rope.Config.baseWidth);
                minStretchThicknessRatio = rope.Config.minStretchWidthFactor;
                knotScale = baseThickness * 2.0f;
            }
            LoadDefaultAssetsIfMissing();

            // Crucial: Only build fresh knots if this is an uncut original rope!
            // Severed pieces inherit their physical knots via TransferKnotsToSevered.
            if (rope != null && !rope.IsCut && bodyKnots.Count == 0)
            {
                RebuildBodyKnots();
            }
        }

        public void RebuildBodyKnots()
        {
            // Clean up existing knot instances
            for (int i = 0; i < bodyKnots.Count; i++)
            {
                if (bodyKnots[i].instance != null) Destroy(bodyKnots[i].instance);
            }
            bodyKnots.Clear();

            if (knotCount <= 0 || bodyKnotSprite == null || rope == null || rope.Particles == null || rope.Particles.Count < 3) return;

            var particles = rope.Particles;
            for (int k = 0; k < knotCount; k++)
            {
                float fraction = (float)(k + 1) / (knotCount + 1);
                int particleIndex = Mathf.Clamp(Mathf.RoundToInt(fraction * (particles.Count - 1)), 1, particles.Count - 2);
                RopeParticle p = particles[particleIndex];

                GameObject knotObj = new GameObject($"{gameObject.name}_Knot_{k + 1}");
                knotObj.transform.parent = transform;
                knotObj.transform.localScale = Vector3.one * knotScale;

                SpriteRenderer sr = knotObj.AddComponent<SpriteRenderer>();
                sr.sprite = bodyKnotSprite;
                sr.sortingOrder = 9;
                sr.color = Color.white;
                Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                                   ?? Shader.Find("Sprites/Default");
                if (spriteShader != null)
                {
                    sr.material = new Material(spriteShader);
                }

                bodyKnots.Add(new BodyKnot
                {
                    boundParticle = p,
                    instance = knotObj,
                    renderer = sr
                });
            }
        }

        /// <summary>
        /// Transfers physical knots that belong to the severed lower piece, preventing duplication.
        /// </summary>
        public void TransferKnotsToSevered(RopeRenderer newRenderer, List<RopeParticle> bottomHalfNodes)
        {
            if (newRenderer == null || bottomHalfNodes == null) return;

            for (int i = bodyKnots.Count - 1; i >= 0; i--)
            {
                var knot = bodyKnots[i];
                if (knot == null || knot.instance == null || knot.boundParticle == null) continue;

                if (bottomHalfNodes.Contains(knot.boundParticle))
                {
                    // Transfer ownership to severed piece
                    knot.instance.transform.parent = newRenderer.transform;
                    newRenderer.bodyKnots.Add(knot);
                    bodyKnots.RemoveAt(i);
                }
            }
        }

        public void CopySettingsFrom(RopeRenderer source, bool preserveEndKnot = false)
        {
            if (source == null) return;
            baseThickness = source.baseThickness;
            minStretchThicknessRatio = source.minStretchThicknessRatio;
            splineSubdivisions = source.splineSubdivisions;
            knotCount = source.knotCount;
            bodyKnotSprite = source.bodyKnotSprite;
            knotScale = source.knotScale;
            ropeMaterial = source.ropeMaterial;

            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null && source.lineRenderer != null)
            {
                lineRenderer.sharedMaterial = source.lineRenderer.sharedMaterial;
                lineRenderer.textureMode = LineTextureMode.Tile;
                lineRenderer.alignment = LineAlignment.View;
                lineRenderer.sortingOrder = 8;
            }
        }

        private void LateUpdate()
        {
            if (rope == null || rope.Particles == null || rope.Particles.Count < 2) return;

            UpdateSplineMesh();
            UpdateTensionFeedback();
            UpdateBodyKnots();
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

            // Pure white tint ensures 100% color consistency between rope and knot
            Color ropeColor = Color.white;
            ropeColor.a = fadeAlpha;

            lineRenderer.startColor = ropeColor;
            lineRenderer.endColor = ropeColor;
        }

        private void UpdateBodyKnots()
        {
            if (bodyKnots.Count == 0 || rope == null || rope.Particles == null || rope.Particles.Count < 2) return;

            var particles = rope.Particles;
            for (int k = 0; k < bodyKnots.Count; k++)
            {
                var knot = bodyKnots[k];
                if (knot == null || knot.instance == null || knot.boundParticle == null) continue;

                Vector3 pos = knot.boundParticle.Position;
                knot.instance.transform.position = new Vector3(pos.x, pos.y, pos.z - 0.015f);

                int idx = -1;
                for (int pIdx = 0; pIdx < particles.Count; pIdx++)
                {
                    if (particles[pIdx] == knot.boundParticle)
                    {
                        idx = pIdx;
                        break;
                    }
                }

                Vector3 tangent = Vector3.down;
                if (idx >= 0)
                {
                    Vector3 prev = idx > 0 ? particles[idx - 1].Position : knot.boundParticle.Position;
                    Vector3 next = idx < particles.Count - 1 ? particles[idx + 1].Position : knot.boundParticle.Position;
                    Vector3 diff = next - prev;
                    if (diff.sqrMagnitude > 0.0001f) tangent = diff.normalized;
                }

                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                knot.instance.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
                knot.instance.transform.localScale = Vector3.one * knotScale;

                if (knot.renderer != null)
                {
                    Color c = Color.white;
                    c.a = fadeAlpha;
                    knot.renderer.color = c;
                }
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
            for (int i = 0; i < bodyKnots.Count; i++)
            {
                if (bodyKnots[i].instance != null) Destroy(bodyKnots[i].instance);
            }
            bodyKnots.Clear();
        }
    }
}
