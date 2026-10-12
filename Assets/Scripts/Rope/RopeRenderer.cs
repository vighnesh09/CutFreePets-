using System.Collections.Generic;
using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Renders a VerletRope using Catmull-Rom spline smoothing, dynamic width thinning on stretch,
    /// tension vibration, and responsive visual feedback.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RopeRenderer : MonoBehaviour
    {
        [Header("Rendering Components")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Material ropeMaterial;

        [Header("Appearance")]
        [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        [SerializeField] private float textureTileFactor = 2.0f; // Texture wraps per unit length

        private VerletRope rope;
        private readonly List<Vector3> smoothedPoints = new List<Vector3>();
        private MaterialPropertyBlock propertyBlock;
        private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int MainColorProp = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            propertyBlock = new MaterialPropertyBlock();
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.numCapVertices = 4;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            if (ropeMaterial != null)
            {
                lineRenderer.material = ropeMaterial;
            }
            else if (lineRenderer.sharedMaterial == null)
            {
                // Assign standard URP unlit or lit material if available
                Shader defaultShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                if (defaultShader != null)
                {
                    lineRenderer.material = new Material(defaultShader);
                }
            }
        }

        public void Initialize(VerletRope targetRope)
        {
            rope = targetRope;
        }

        public void CopySettingsFrom(RopeRenderer source)
        {
            if (source == null) return;
            ropeMaterial = source.ropeMaterial;
            widthCurve = source.widthCurve;
            textureTileFactor = source.textureTileFactor;

            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (source.lineRenderer != null && lineRenderer != null)
            {
                lineRenderer.sharedMaterial = source.lineRenderer.sharedMaterial;
                lineRenderer.colorGradient = source.lineRenderer.colorGradient;
                lineRenderer.numCornerVertices = source.lineRenderer.numCornerVertices;
                lineRenderer.numCapVertices = source.lineRenderer.numCapVertices;
            }
        }

        private void LateUpdate()
        {
            if (rope == null || rope.Particles == null || rope.Particles.Count < 2) return;

            UpdateSpline();
            UpdateVisualTension();
        }

        /// <summary>
        /// Generates silky smooth Catmull-Rom spline points through raw particle nodes.
        /// </summary>
        private void UpdateSpline()
        {
            var particles = rope.Particles;
            int count = particles.Count;
            if (count < 2) return;

            int subdivisions = rope.Config != null ? Mathf.Max(1, rope.Config.splineSubdivisions) : 4;
            smoothedPoints.Clear();

            // Calculate tension jitter if over-stretched
            float tensionRatio = rope.CurrentTensionRatio;
            float tensionNorm = 0f;
            if (rope.Config != null && rope.Config.maxTensionThreshold > 1.0f)
            {
                tensionNorm = Mathf.Clamp01((tensionRatio - 1.0f) / (rope.Config.maxTensionThreshold - 1.0f));
            }

            float vibrationMagnitude = (rope.Config != null ? rope.Config.tensionVibrationAmount : 0.015f) * tensionNorm;

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 p0 = i > 0 ? particles[i - 1].Position : particles[i].Position;
                Vector3 p1 = particles[i].Position;
                Vector3 p2 = particles[i + 1].Position;
                Vector3 p3 = i < count - 2 ? particles[i + 2].Position : particles[i + 1].Position;

                for (int step = 0; step < subdivisions; step++)
                {
                    float t = (float)step / subdivisions;
                    Vector3 point = EvaluateCatmullRom(p0, p1, p2, p3, t);

                    // Add high tension strain vibration
                    if (vibrationMagnitude > 0.0001f && i > 0 && i < count - 2)
                    {
                        float seed = (Time.time * 45f) + (i * 3.7f) + t;
                        Vector3 jitter = new Vector3(
                            Mathf.Sin(seed),
                            Mathf.Cos(seed * 1.3f),
                            0f
                        ) * vibrationMagnitude;
                        point += jitter;
                    }

                    smoothedPoints.Add(point);
                }
            }

            // Add final endpoint
            smoothedPoints.Add(particles[count - 1].Position);

            // Apply to LineRenderer
            lineRenderer.positionCount = smoothedPoints.Count;
            lineRenderer.SetPositions(smoothedPoints.ToArray());
        }

        /// <summary>
        /// Adjusts thickness and color in response to stretch tension.
        /// </summary>
        private void UpdateVisualTension()
        {
            if (rope.Config == null) return;

            float tensionRatio = rope.CurrentTensionRatio;
            float maxThreshold = rope.Config.maxTensionThreshold;
            float tensionNorm = Mathf.Clamp01((tensionRatio - 1.0f) / Mathf.Max(0.01f, maxThreshold - 1.0f));

            // 1. Thickness thinning under tension (Poisson's ratio effect)
            float baseWidth = rope.Config.baseWidth;
            float stretchFactor = Mathf.Lerp(1.0f, rope.Config.minStretchWidthFactor, tensionNorm);
            float currentWidth = baseWidth * stretchFactor;

            lineRenderer.startWidth = currentWidth;
            lineRenderer.endWidth = currentWidth;
            lineRenderer.widthCurve = widthCurve;

            // 2. Color gradient / tint response
            Color targetColor = Color.Lerp(rope.Config.normalColor, rope.Config.highTensionColor, tensionNorm);
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;

            // 3. Texture UV tiling across length
            if (lineRenderer.material != null)
            {
                lineRenderer.material.mainTextureScale = new Vector2(rope.CurrentTotalLength * textureTileFactor, 1f);
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
    }
}
