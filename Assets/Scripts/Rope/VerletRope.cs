using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Professional Cut the Rope physics component.
    /// Handles:
    /// - Verlet physics with tunable elasticity & high-accuracy solver
    /// - Two-way Rigidbody coupling (tension spring pulling on connected objects)
    /// - 2D & 3D obstacle collision (wraps around pegs, posts, pins)
    /// - Real-time continuous tension & stretch measurement
    /// - Instant segment cutting with dual physical falling remnants
    /// - Reusable everywhere across levels and prefabs
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RopeRenderer))]
    public class VerletRope : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RopeConfig config;

        [Header("Anchors")]
        [Tooltip("Fixed anchor transform (e.g. wall pin, ceiling peg).")]
        [SerializeField] private Transform startAnchor;

        [Tooltip("Connected target transform (e.g. cage, pet, candy, weight).")]
        [SerializeField] private Transform endAnchor;

        [SerializeField] private Vector3 startOffset = Vector3.zero;
        [SerializeField] private Vector3 endOffset = Vector3.zero;

        [Header("Anchor Rigidbody Physics Coupling")]
        [Tooltip("Enable reactive spring tension force onto connected Rigidbodies (3D and 2D).")]
        [SerializeField] private bool applyTensionForces = true;
        [SerializeField] private float springStiffness = 160f;
        [SerializeField] private float springDamping = 22f;

        [Header("State")]
        [SerializeField] private bool pinStart = true;
        [SerializeField] private bool pinEnd = true;
        [SerializeField] private bool isCut = false;

        // Particle simulation chain
        private readonly List<RopeParticle> particles = new List<RopeParticle>();
        private float segmentRestLength;
        private float initialTotalRestLength;
        private float currentTotalLength;
        private float currentTensionRatio = 1.0f;

        // Rigidbody caches (both 3D and 2D support)
        private Rigidbody startRb3D;
        private Rigidbody endRb3D;
        private Rigidbody2D startRb2D;
        private Rigidbody2D endRb2D;
        private DistanceJoint2D distanceJoint;
        private RopeRenderer ropeRenderer;

        // Non-alloc collision buffer
        private readonly Collider[] colliders3D = new Collider[8];
        private readonly Collider2D[] colliders2D = new Collider2D[8];

        // Events
        public event Action<VerletRope, int, Vector3> OnCut;
        public event Action<float> OnTensionChanged;

        #region Public Properties

        public RopeConfig Config { get => config; set => config = value; }
        public Transform StartAnchor { get => startAnchor; set => startAnchor = value; }
        public Transform EndAnchor { get => endAnchor; set => endAnchor = value; }
        public bool IsCut => isCut;
        public DistanceJoint2D Joint => distanceJoint;
        public float CurrentTensionRatio => currentTensionRatio;
        public float CurrentTotalLength => currentTotalLength;
        public float InitialTotalRestLength => initialTotalRestLength;
        public IReadOnlyList<RopeParticle> Particles => particles;

        public Vector3 StartPosition => startAnchor != null ? startAnchor.TransformPoint(startOffset) : (particles.Count > 0 ? particles[0].Position : transform.position);
        public Vector3 EndPosition => endAnchor != null ? endAnchor.TransformPoint(endOffset) : (particles.Count > 0 ? particles[particles.Count - 1].Position : transform.position);

        #endregion

        private void Awake()
        {
            ropeRenderer = GetComponent<RopeRenderer>();
            if (config == null)
            {
#if UNITY_EDITOR
                config = UnityEditor.AssetDatabase.LoadAssetAtPath<RopeConfig>("Assets/Settings/DefaultRopeConfig.asset");
#endif
                if (config == null) config = ScriptableObject.CreateInstance<RopeConfig>();
            }

            CacheConnectedBodies();
        }

        private void Start()
        {
            if (particles.Count == 0 && (startAnchor != null || endAnchor != null))
            {
                InitializeRope();
            }
            else if (distanceJoint == null && !isCut)
            {
                CacheConnectedBodies();
                SetupDistanceJoint();
            }
        }

        public void CacheConnectedBodies()
        {
            if (startAnchor != null)
            {
                startRb3D = startAnchor.GetComponentInParent<Rigidbody>();
                startRb2D = startAnchor.GetComponentInParent<Rigidbody2D>();
            }

            if (endAnchor != null)
            {
                endRb3D = endAnchor.GetComponentInParent<Rigidbody>();
                endRb2D = endAnchor.GetComponentInParent<Rigidbody2D>();
            }
        }

        /// <summary>
        /// Universal setup method to bind a rope between any two transforms programmatically or in prefabs.
        /// </summary>
        public void Setup(Transform startTransform, Transform endTransform, RopeConfig customConfig = null)
        {
            startAnchor = startTransform;
            endAnchor = endTransform;
            if (customConfig != null) config = customConfig;
            CacheConnectedBodies();
            InitializeRope();
        }

        /// <summary>
        /// Generates the initial particle chain between anchors with natural catenary sag.
        /// </summary>
        public void InitializeRope()
        {
            particles.Clear();

            Vector3 start = StartPosition;
            Vector3 end = EndPosition;
            float straightDistance = Vector3.Distance(start, end);

            int segments = config != null ? Mathf.Max(4, config.segmentCount) : 18;
            float slack = config != null ? config.slackMultiplier : 1.0f;
            float totalLength = straightDistance * slack;

            segmentRestLength = totalLength / (segments - 1);
            initialTotalRestLength = totalLength;

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / (segments - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);

                // Initial catenary sag when loose
                if (slack > 1.0f && i > 0 && i < segments - 1)
                {
                    float sag = Mathf.Sin(t * Mathf.PI) * (straightDistance * (slack - 1.0f));
                    pos += Vector3.down * sag;
                }

                float radius = config != null ? config.particleRadius : 0.04f;
                particles.Add(new RopeParticle(pos, radius));
            }

            particles[0].IsPinned = pinStart;
            particles[particles.Count - 1].IsPinned = pinEnd;

            if (ropeRenderer == null) ropeRenderer = GetComponent<RopeRenderer>();
            ropeRenderer?.Initialize(this);

            SetupDistanceJoint();
        }

        /// <summary>
        /// Configures native 2D DistanceJoint with maxDistanceOnly for authentic rigid pendulum swing.
        /// When one of two ropes is severed, this joint allows Box2D to swing the cage along a true circular arc.
        /// </summary>
        public void SetupDistanceJoint()
        {
            if (!Application.isPlaying) return;
            if (isCut || endRb2D == null || endRb2D.bodyType != RigidbodyType2D.Dynamic) return;

            if (distanceJoint != null)
            {
                Destroy(distanceJoint);
                distanceJoint = null;
            }

            distanceJoint = endRb2D.gameObject.AddComponent<DistanceJoint2D>();
            distanceJoint.enableCollision = false;
            distanceJoint.maxDistanceOnly = false;
            distanceJoint.autoConfigureDistance = false;

            // Anchor local to end body
            Vector3 endWorldPos = EndPosition;
            Vector2 localAnchor = endRb2D.transform.InverseTransformPoint(endWorldPos);
            distanceJoint.anchor = localAnchor;

            // Connected body or world anchor
            Vector3 startWorldPos = StartPosition;
            if (startRb2D != null)
            {
                distanceJoint.connectedBody = startRb2D;
                distanceJoint.connectedAnchor = startRb2D.transform.InverseTransformPoint(startWorldPos);
            }
            else
            {
                distanceJoint.connectedBody = null;
                distanceJoint.connectedAnchor = (Vector2)startWorldPos;
            }

            float dist = Vector2.Distance(startWorldPos, endWorldPos);
            distanceJoint.distance = Mathf.Max(0.1f, dist);
        }

        private void FixedUpdate()
        {
            if (distanceJoint != null && startAnchor != null && startRb2D == null)
            {
                distanceJoint.connectedAnchor = (Vector2)StartPosition;
            }

            if (particles.Count < 2) return;

            int substeps = config != null ? Mathf.Max(1, config.physicsSubsteps) : 3;
            float dt = Time.fixedDeltaTime / substeps;
            Vector3 gravity = config != null ? config.gravity : new Vector3(0f, -9.81f, 0f);
            float damping = config != null ? config.damping : 0.986f;
            int iterations = config != null ? config.constraintIterations : 18;
            bool enableCol = config == null || config.enableCollision;
            float bendStiffness = config != null ? config.bendingStiffness : 0.08f;

            for (int s = 0; s < substeps; s++)
            {
                // 1. Anchor tracking
                if (pinStart && startAnchor != null)
                {
                    particles[0].Position = StartPosition;
                    particles[0].PreviousPosition = StartPosition;
                }

                if (pinEnd && endAnchor != null)
                {
                    particles[particles.Count - 1].Position = EndPosition;
                    particles[particles.Count - 1].PreviousPosition = EndPosition;
                }

                // 2. Gravity & Verlet Step
                for (int i = 0; i < particles.Count; i++)
                {
                    RopeParticle p = particles[i];
                    if (!p.IsPinned)
                    {
                        p.Acceleration += gravity;
                        p.UpdateVerlet(dt, damping);
                    }
                }

                // 3. Distance & Bending constraints loop
                for (int iter = 0; iter < iterations; iter++)
                {
                    SolveDistanceConstraints();

                    if (bendStiffness > 0.001f)
                    {
                        SolveBendingConstraints(bendStiffness);
                    }

                    if (enableCol)
                    {
                        SolveCollisions();
                    }
                }
            }

            // 4. Measure stretch tension and apply physical pull force to connected object
            MeasureTension();
            if (applyTensionForces && !isCut)
            {
                ApplyConnectedForces();
            }
        }

        private void SolveDistanceConstraints()
        {
            float targetRest = segmentRestLength;
            float compliance = config != null ? (1f - config.elasticity) : 0.985f;

            for (int i = 0; i < particles.Count - 1; i++)
            {
                RopeParticle p1 = particles[i];
                RopeParticle p2 = particles[i + 1];

                Vector3 delta = p2.Position - p1.Position;
                float currentDist = delta.magnitude;

                if (currentDist <= 0.0001f) continue;

                float diff = (currentDist - targetRest) / currentDist;
                Vector3 correction = delta * (0.5f * diff * compliance);

                if (!p1.IsPinned && !p2.IsPinned)
                {
                    p1.Position += correction;
                    p2.Position -= correction;
                }
                else if (!p1.IsPinned && p2.IsPinned)
                {
                    p1.Position += correction * 2f;
                }
                else if (p1.IsPinned && !p2.IsPinned)
                {
                    p2.Position -= correction * 2f;
                }
            }
        }

        /// <summary>
        /// Prevents unnatural sharp folding and adds organic rope springiness (Cut the Rope feel).
        /// </summary>
        private void SolveBendingConstraints(float bendStiffness)
        {
            float doubleRest = segmentRestLength * 2f;

            for (int i = 0; i < particles.Count - 2; i++)
            {
                RopeParticle p1 = particles[i];
                RopeParticle p3 = particles[i + 2];

                Vector3 delta = p3.Position - p1.Position;
                float dist = delta.magnitude;

                if (dist <= 0.0001f || dist >= doubleRest) continue;

                float diff = (dist - doubleRest) / dist;
                Vector3 correction = delta * (0.5f * diff * bendStiffness);

                if (!p1.IsPinned && !p3.IsPinned)
                {
                    p1.Position += correction * 0.5f;
                    p3.Position -= correction * 0.5f;
                }
            }
        }

        private void SolveCollisions()
        {
            float radius = config != null ? config.particleRadius : 0.04f;
            float skin = config != null ? config.skinWidth : 0.01f;
            LayerMask mask = config != null ? config.collisionMask : ~0;

            for (int i = 0; i < particles.Count; i++)
            {
                RopeParticle p = particles[i];
                if (p.IsPinned) continue;

                // 3D Obstacle collision
                int hits3D = Physics.OverlapSphereNonAlloc(p.Position, radius + skin, colliders3D, mask, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hits3D; h++)
                {
                    Collider col = colliders3D[h];
                    if (col == null) continue;
                    if (startAnchor != null && (col.transform == startAnchor || col.transform.IsChildOf(startAnchor) || startAnchor.IsChildOf(col.transform))) continue;
                    if (endAnchor != null && (col.transform == endAnchor || col.transform.IsChildOf(endAnchor) || endAnchor.IsChildOf(col.transform))) continue;
                    if (startRb3D != null && col.attachedRigidbody == startRb3D) continue;
                    if (endRb3D != null && col.attachedRigidbody == endRb3D) continue;

                    Vector3 closest = col.ClosestPoint(p.Position);
                    Vector3 pushDir = p.Position - closest;
                    float pushDist = pushDir.magnitude;
                    float requiredDist = radius + skin;

                    if (pushDist < requiredDist)
                    {
                        Vector3 normal = pushDist > 0.0001f ? (pushDir / pushDist) : Vector3.up;
                        p.Position = closest + normal * requiredDist;

                        // Friction damping along surface
                        Vector3 vel = p.Position - p.PreviousPosition;
                        vel -= Vector3.Dot(vel, normal) * normal;
                        p.PreviousPosition = p.Position - vel * 0.9f;
                    }
                }

                // 2D Obstacle collision (if project uses 2D colliders)
                ContactFilter2D filter = new ContactFilter2D { layerMask = mask, useLayerMask = true, useTriggers = false };
                int hits2D = Physics2D.OverlapCircle(p.Position, radius + skin, filter, colliders2D);
                for (int h = 0; h < hits2D; h++)
                {
                    Collider2D col2D = colliders2D[h];
                    if (col2D == null) continue;
                    if (startAnchor != null && (col2D.transform == startAnchor || col2D.transform.IsChildOf(startAnchor) || startAnchor.IsChildOf(col2D.transform))) continue;
                    if (endAnchor != null && (col2D.transform == endAnchor || col2D.transform.IsChildOf(endAnchor) || endAnchor.IsChildOf(col2D.transform))) continue;
                    if (startRb2D != null && col2D.attachedRigidbody == startRb2D) continue;
                    if (endRb2D != null && col2D.attachedRigidbody == endRb2D) continue;

                    Vector2 closest2D = col2D.ClosestPoint(p.Position);
                    Vector2 pushDir2D = (Vector2)p.Position - closest2D;
                    float pushDist2D = pushDir2D.magnitude;
                    float requiredDist = radius + skin;

                    if (pushDist2D < requiredDist)
                    {
                        Vector2 normal2D = pushDist2D > 0.0001f ? (pushDir2D / pushDist2D) : Vector2.up;
                        p.Position = (Vector3)(closest2D + normal2D * requiredDist);

                        Vector3 vel = p.Position - p.PreviousPosition;
                        vel -= Vector3.Dot(vel, normal2D) * (Vector3)normal2D;
                        p.PreviousPosition = p.Position - vel * 0.9f;
                    }
                }
            }
        }

        private void MeasureTension()
        {
            float len = 0f;
            for (int i = 0; i < particles.Count - 1; i++)
            {
                len += Vector3.Distance(particles[i].Position, particles[i + 1].Position);
            }

            currentTotalLength = len;
            currentTensionRatio = initialTotalRestLength > 0.0001f ? currentTotalLength / initialTotalRestLength : 1f;
            OnTensionChanged?.Invoke(currentTensionRatio);
        }

        private void ApplyConnectedForces()
        {
            if (distanceJoint != null)
            {
                // DistanceJoint2D provides authentic inelastic pendulum constraint natively
                // Rigidbody2D linear and angular damping handle natural stabilization smoothly
                return;
            }

            float stretch = currentTotalLength - initialTotalRestLength;
            if (stretch <= 0.001f || particles.Count < 2) return;

            // Soft progressive spring response clamped to prevent force spikes
            float forceMag = Mathf.Clamp(stretch * springStiffness, 0f, 80f);

            // Apply to 3D End Rigidbody
            if (endRb3D != null && !endRb3D.isKinematic)
            {
                Vector3 pullDir = (particles[particles.Count - 2].Position - particles[particles.Count - 1].Position).normalized;
                Vector3 dampForce = -endRb3D.linearVelocity * springDamping;
                endRb3D.AddForceAtPosition((pullDir * forceMag) + dampForce, EndPosition, ForceMode.Force);
            }

            // Apply to 2D End Rigidbody
            if (endRb2D != null && endRb2D.bodyType == RigidbodyType2D.Dynamic)
            {
                Vector2 pullDir2D = (Vector2)(particles[particles.Count - 2].Position - particles[particles.Count - 1].Position).normalized;
                Vector2 dampForce2D = -endRb2D.linearVelocity * springDamping;
                endRb2D.AddForceAtPosition((pullDir2D * forceMag) + dampForce2D, EndPosition, ForceMode2D.Force);
            }
        }

        /// <summary>
        /// Cuts the rope when a swipe intersection passes through segment (a, b).
        /// </summary>
        public bool CutBetweenPoints(Vector2 swipeStart, Vector2 swipeEnd)
        {
            if (isCut || particles.Count < 2) return false;

            for (int i = 0; i < particles.Count - 1; i++)
            {
                Vector2 pA = particles[i].Position;
                Vector2 pB = particles[i + 1].Position;

                if (LineSegmentsIntersect(swipeStart, swipeEnd, pA, pB, out Vector2 intersection))
                {
                    CutSegment(i, intersection);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Cuts the rope at nearest point to world position within cut tolerance.
        /// </summary>
        public bool CutAtPoint(Vector3 worldPoint, float tolerance = 0.35f)
        {
            if (isCut || particles.Count < 2) return false;

            int bestSegment = -1;
            float minDist = float.MaxValue;
            Vector3 bestPoint = Vector3.zero;

            for (int i = 0; i < particles.Count - 1; i++)
            {
                Vector3 pA = particles[i].Position;
                Vector3 pB = particles[i + 1].Position;
                Vector3 closest = ClosestPointOnSegment(pA, pB, worldPoint);
                float dist = Vector3.Distance(closest, worldPoint);

                if (dist < minDist)
                {
                    minDist = dist;
                    bestSegment = i;
                    bestPoint = closest;
                }
            }

            if (bestSegment != -1 && minDist <= tolerance)
            {
                CutSegment(bestSegment, bestPoint);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Splits the rope into two dynamic falling segments.
        /// </summary>
        public void CutSegment(int segmentIndex, Vector3 cutPoint)
        {
            if (isCut || segmentIndex < 0 || segmentIndex >= particles.Count - 1) return;

            if (distanceJoint != null)
            {
                Destroy(distanceJoint);
                distanceJoint = null;
            }

            isCut = true;
            OnCut?.Invoke(this, segmentIndex, cutPoint);

            // Separate into top half (retained) and bottom half (spawned as severed piece)
            List<RopeParticle> bottomHalf = new List<RopeParticle>();
            RopeParticle cutA = new RopeParticle(cutPoint, config != null ? config.particleRadius : 0.04f);
            RopeParticle cutB = new RopeParticle(cutPoint, config != null ? config.particleRadius : 0.04f);

            for (int i = segmentIndex + 1; i < particles.Count; i++)
            {
                bottomHalf.Add(particles[i]);
            }

            // Top half stays attached to peg, falls freely from cut with natural snap recoil
            int removeCount = particles.Count - (segmentIndex + 1);
            particles.RemoveRange(segmentIndex + 1, removeCount);
            particles.Add(cutA);
            pinEnd = false; // severed end of top half falls freely
            isCut = true;   // mark original rope as cut to preserve knots without duplication
            Destroy(gameObject, 2.5f); // Fade out top remnant after swinging

            // Impart natural whip recoil impulse to severed ends
            Vector3 cutDirection = (particles.Count > 1) 
                ? (particles[particles.Count - 1].Position - particles[particles.Count - 2].Position).normalized 
                : Vector3.down;
            Vector3 recoilImpulse = -cutDirection * 0.08f + Vector3.right * UnityEngine.Random.Range(-0.04f, 0.04f);
            cutA.PreviousPosition = cutA.Position - recoilImpulse;

            // Spawn bottom severed piece attached to the box!
            if (bottomHalf.Count > 0)
            {
                cutB.PreviousPosition = cutB.Position + (cutDirection * 0.06f + Vector3.right * UnityEngine.Random.Range(-0.03f, 0.03f));
                SpawnSeveredPiece(cutB, bottomHalf);
            }


            initialTotalRestLength = segmentRestLength * Mathf.Max(1, particles.Count - 1);
        }

        private void SpawnSeveredPiece(RopeParticle cutNodeB, List<RopeParticle> remainingNodes)
        {
            GameObject severed = new GameObject($"{gameObject.name}_SeveredPiece");
            severed.transform.position = cutNodeB.Position;

            VerletRope severedRope = severed.AddComponent<VerletRope>();
            severedRope.config = config;
            severedRope.pinStart = false; // severed cut point is completely free
            severedRope.pinEnd = true;    // attached firmly to the box / endAnchor!
            severedRope.startAnchor = null;
            severedRope.endAnchor = endAnchor;
            severedRope.endOffset = endOffset;
            severedRope.applyTensionForces = false; // no longer pulling on box once severed
            severedRope.isCut = true;

            severedRope.particles.Clear();
            severedRope.particles.Add(cutNodeB);
            severedRope.particles.AddRange(remainingNodes);
            // Ensure last particle is marked pinned to follow box
            severedRope.particles[severedRope.particles.Count - 1].IsPinned = true;
            severedRope.segmentRestLength = segmentRestLength;
            severedRope.initialTotalRestLength = segmentRestLength * (severedRope.particles.Count - 1);

            RopeRenderer originalRenderer = GetComponent<RopeRenderer>();
            RopeRenderer newRenderer = severed.GetComponent<RopeRenderer>();
            if (originalRenderer != null && newRenderer != null)
            {
                newRenderer.CopySettingsFrom(originalRenderer, preserveEndKnot: false);
                newRenderer.Initialize(severedRope);
                originalRenderer.TransferKnotsToSevered(newRenderer, remainingNodes);
            }

            // Auto cleanup severed piece after falling with the box
            Destroy(severed, 2.5f);
        }

        private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float sqr = ab.sqrMagnitude;
            if (sqr < 0.0001f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / sqr);
            return a + t * ab;
        }

        private bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
        {
            intersection = Vector2.zero;
            float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);
            if (Mathf.Abs(d) < 0.0001f) return false;

            float u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
            float v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

            if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
            {
                intersection = p1 + u * (p2 - p1);
                return true;
            }

            return false;
        }

        private void OnDestroy()
        {
            if (distanceJoint != null)
            {
                Destroy(distanceJoint);
                distanceJoint = null;
            }
        }

        private void OnDisable()
        {
            if (distanceJoint != null)
            {
                distanceJoint.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (distanceJoint != null)
            {
                distanceJoint.enabled = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (particles == null || particles.Count < 2)
            {
                if (startAnchor != null && endAnchor != null)
                {
                    Gizmos.color = new Color(0.9f, 0.75f, 0.5f, 0.8f);
                    Gizmos.DrawLine(StartPosition, EndPosition);
                }
                return;
            }

            Gizmos.color = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01((currentTensionRatio - 1f) / 0.25f));
            for (int i = 0; i < particles.Count - 1; i++)
            {
                Gizmos.DrawLine(particles[i].Position, particles[i + 1].Position);
            }
        }
    }
}
