using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Professional Verlet-based physics rope with obstacle collision, dynamic tension,
    /// stretch feedback, and clean cutting support (Cut the Rope feel).
    /// </summary>
    [RequireComponent(typeof(RopeRenderer))]
    public class VerletRope : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RopeConfig config;

        [Header("Anchors")]
        [Tooltip("Fixed anchor or origin transform (e.g., ceiling peg, wall hook).")]
        [SerializeField] private Transform startAnchor;

        [Tooltip("End anchor or tied object transform (e.g., animal harness, weight).")]
        [SerializeField] private Transform endAnchor;

        [Tooltip("Anchor offsets in local space.")]
        [SerializeField] private Vector3 startOffset = Vector3.zero;
        [SerializeField] private Vector3 endOffset = Vector3.zero;

        [Header("State")]
        [SerializeField] private bool pinStart = true;
        [SerializeField] private bool pinEnd = true;
        [SerializeField] private bool isCut = false;

        // Simulation nodes
        private readonly List<RopeParticle> particles = new List<RopeParticle>();
        private float segmentRestLength;
        private float initialTotalRestLength;
        private float currentTotalLength;
        private float currentTensionRatio; // 1.0 = rest length, >1.0 = stretched
        private Rigidbody startRigidbody;
        private Rigidbody endRigidbody;
        private RopeRenderer ropeRenderer;

        // Collision cache buffer to avoid allocations
        private readonly Collider[] hitColliders = new Collider[8];

        // Events
        public event Action<VerletRope, int, Vector3> OnCut;
        public event Action<float> OnTensionChanged;

        #region Public Properties

        public RopeConfig Config => config;
        public Transform StartAnchor { get => startAnchor; set => startAnchor = value; }
        public Transform EndAnchor { get => endAnchor; set => endAnchor = value; }
        public bool IsCut => isCut;
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
                config = ScriptableObject.CreateInstance<RopeConfig>();
            }

            CacheRigidbodies();
        }

        private void Start()
        {
            if (particles.Count == 0)
            {
                InitializeRope();
            }
        }

        private void CacheRigidbodies()
        {
            if (startAnchor != null) startRigidbody = startAnchor.GetComponentInParent<Rigidbody>();
            if (endAnchor != null) endRigidbody = endAnchor.GetComponentInParent<Rigidbody>();
        }

        /// <summary>
        /// Initializes particle chain between anchors.
        /// </summary>
        public void InitializeRope()
        {
            particles.Clear();

            Vector3 start = StartPosition;
            Vector3 end = EndPosition;
            float distance = Vector3.Distance(start, end);

            int count = Mathf.Max(3, config.segmentCount);
            float totalLength = distance * config.slackMultiplier;
            segmentRestLength = totalLength / (count - 1);
            initialTotalRestLength = totalLength;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                // Slight initial natural sag curve for visual polish
                Vector3 initialPos = Vector3.Lerp(start, end, t);
                if (config.slackMultiplier > 1f && i > 0 && i < count - 1)
                {
                    float sagFactor = Mathf.Sin(t * Mathf.PI);
                    initialPos += Vector3.down * (distance * (config.slackMultiplier - 1f) * sagFactor);
                }

                RopeParticle p = new RopeParticle(initialPos, config.particleRadius);
                particles.Add(p);
            }

            UpdatePinnedStates();
            ropeRenderer?.Initialize(this);
        }

        /// <summary>
        /// Explicit setup for spawning ropes dynamically in code or levels.
        /// </summary>
        public void Setup(Transform startTransform, Transform endTransform, RopeConfig ropeConfig = null)
        {
            startAnchor = startTransform;
            endAnchor = endTransform;
            if (ropeConfig != null) config = ropeConfig;
            CacheRigidbodies();
            InitializeRope();
        }

        private void UpdatePinnedStates()
        {
            if (particles.Count == 0) return;
            particles[0].IsPinned = pinStart;
            particles[particles.Count - 1].IsPinned = pinEnd;
        }

        private void FixedUpdate()
        {
            if (particles.Count < 2) return;

            float dt = Time.fixedDeltaTime;

            // 1. Update anchor positions
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

            // 2. Accumulate forces & Verlet Step
            Vector3 gravityForce = config.gravity;
            for (int i = 0; i < particles.Count; i++)
            {
                RopeParticle p = particles[i];
                if (!p.IsPinned)
                {
                    p.Acceleration += gravityForce;
                    p.UpdateVerlet(dt, config.damping);
                }
            }

            // 3. Solve distance constraints and collisions iteratively
            int iterations = config.constraintIterations;
            for (int iter = 0; iter < iterations; iter++)
            {
                SolveDistanceConstraints();

                if (config.enableCollision)
                {
                    SolveCollisions();
                }
            }

            // 4. Update Tension Calculation and apply physics feedback to attached Rigidbodies
            CalculateTension();
            ApplyTensionForces();
        }

        /// <summary>
        /// Relaxation algorithm preserving segment lengths with elasticity.
        /// </summary>
        private void SolveDistanceConstraints()
        {
            float targetRest = segmentRestLength;
            float elasticityMultiplier = 1f - config.elasticity;

            for (int i = 0; i < particles.Count - 1; i++)
            {
                RopeParticle p1 = particles[i];
                RopeParticle p2 = particles[i + 1];

                Vector3 delta = p2.Position - p1.Position;
                float currentDist = delta.magnitude;

                if (currentDist <= 0.0001f) continue;

                // Difference between current distance and rest length
                float diff = (currentDist - targetRest) / currentDist;
                Vector3 correction = delta * (0.5f * diff * elasticityMultiplier);

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
        /// Resolves 3D collisions against colliders in the scene, pushing particles out of penetrating geometry.
        /// </summary>
        private void SolveCollisions()
        {
            float radius = config.particleRadius;
            float skin = config.skinWidth;
            LayerMask mask = config.collisionMask;

            for (int i = 0; i < particles.Count; i++)
            {
                RopeParticle p = particles[i];
                if (p.IsPinned) continue;

                int hits = Physics.OverlapSphereNonAlloc(p.Position, radius + skin, hitColliders, mask, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hits; h++)
                {
                    Collider col = hitColliders[h];
                    if (col == null) continue;

                    // Exclude anchor colliders if attached directly
                    if (startAnchor != null && col.transform.IsChildOf(startAnchor)) continue;
                    if (endAnchor != null && col.transform.IsChildOf(endAnchor)) continue;

                    Vector3 closest = col.ClosestPoint(p.Position);
                    Vector3 pushDir = p.Position - closest;
                    float pushDist = pushDir.magnitude;

                    float requiredDist = radius + skin;
                    if (pushDist < requiredDist)
                    {
                        Vector3 normal;
                        if (pushDist > 0.0001f)
                        {
                            normal = pushDir / pushDist;
                        }
                        else
                        {
                            // If strictly inside, push upward or away from collider bounds center
                            normal = (p.Position - col.bounds.center).normalized;
                            if (normal.sqrMagnitude < 0.001f) normal = Vector3.up;
                        }

                        p.Position = closest + normal * requiredDist;

                        // Friction / damp velocity on contact
                        Vector3 velocity = p.Position - p.PreviousPosition;
                        velocity -= Vector3.Dot(velocity, normal) * normal; // remove normal velocity component
                        p.PreviousPosition = p.Position - velocity * 0.85f;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates current total length and tension ratio.
        /// </summary>
        private void CalculateTension()
        {
            float lengthSum = 0f;
            for (int i = 0; i < particles.Count - 1; i++)
            {
                lengthSum += Vector3.Distance(particles[i].Position, particles[i + 1].Position);
            }

            currentTotalLength = lengthSum;
            currentTensionRatio = initialTotalRestLength > 0.001f ? currentTotalLength / initialTotalRestLength : 1f;

            OnTensionChanged?.Invoke(currentTensionRatio);
        }

        /// <summary>
        /// Pulls dynamic Rigidbodies if the rope is stretched taut under load.
        /// </summary>
        private void ApplyTensionForces()
        {
            if (isCut || currentTensionRatio <= 1.0f) return;

            float stretchExcess = currentTotalLength - initialTotalRestLength;
            if (stretchExcess <= 0f) return;

            float springStiffness = 150f;
            float forceMagnitude = stretchExcess * springStiffness;

            // Apply force to End Rigidbody towards Start Anchor
            if (endRigidbody != null && !endRigidbody.isKinematic && particles.Count >= 2)
            {
                Vector3 pullDirection = (particles[particles.Count - 2].Position - particles[particles.Count - 1].Position).normalized;
                endRigidbody.AddForceAtPosition(pullDirection * forceMagnitude, EndPosition);
            }

            // Apply opposite reaction force to Start Rigidbody if dynamic
            if (startRigidbody != null && !startRigidbody.isKinematic && particles.Count >= 2)
            {
                Vector3 pullDirection = (particles[1].Position - particles[0].Position).normalized;
                startRigidbody.AddForceAtPosition(pullDirection * forceMagnitude, StartPosition);
            }
        }

        /// <summary>
        /// Cuts the rope at a specific world coordinate. 
        /// Finds the closest segment and splits the rope realistically.
        /// </summary>
        public bool CutAtPoint(Vector3 worldPoint, float cutTolerance = 0.5f)
        {
            if (isCut || particles.Count < 2) return false;

            int bestSegment = -1;
            float minDistance = float.MaxValue;
            Vector3 bestPointOnSegment = Vector3.zero;

            for (int i = 0; i < particles.Count - 1; i++)
            {
                Vector3 pA = particles[i].Position;
                Vector3 pB = particles[i + 1].Position;

                Vector3 closest = ClosestPointOnSegment(pA, pB, worldPoint);
                float dist = Vector3.Distance(closest, worldPoint);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestSegment = i;
                    bestPointOnSegment = closest;
                }
            }

            if (bestSegment != -1 && minDistance <= cutTolerance)
            {
                CutSegment(bestSegment, bestPointOnSegment);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Cuts the rope at a specific segment index.
        /// Divides the rope into two dynamic falling pieces.
        /// </summary>
        public void CutSegment(int segmentIndex, Vector3 cutWorldPoint)
        {
            if (isCut || segmentIndex < 0 || segmentIndex >= particles.Count - 1) return;

            isCut = true;

            // Trigger cut event
            OnCut?.Invoke(this, segmentIndex, cutWorldPoint);

            // Split into two halves:
            // Half A remains on this VerletRope (from 0 to segmentIndex)
            // Half B is spawned as a detached piece that swings/falls freely
            List<RopeParticle> halfBParticles = new List<RopeParticle>();

            // Insert cut world point as new end for half A
            RopeParticle cutNodeA = new RopeParticle(cutWorldPoint, config.particleRadius);
            RopeParticle cutNodeB = new RopeParticle(cutWorldPoint, config.particleRadius);

            for (int i = segmentIndex + 1; i < particles.Count; i++)
            {
                halfBParticles.Add(particles[i]);
            }

            // Truncate current rope to Half A
            int removeCount = particles.Count - (segmentIndex + 1);
            particles.RemoveRange(segmentIndex + 1, removeCount);
            particles.Add(cutNodeA);
            pinEnd = false; // End is now severed and falls freely!

            // Spawn Half B as detached rope object
            if (halfBParticles.Count > 0)
            {
                SpawnSeveredHalf(cutNodeB, halfBParticles);
            }

            // Recalculate rest length for remaining half
            initialTotalRestLength = segmentRestLength * Mathf.Max(1, particles.Count - 1);
        }

        private void SpawnSeveredHalf(RopeParticle cutNodeB, List<RopeParticle> remainingParticles)
        {
            GameObject severedObj = new GameObject($"{gameObject.name}_SeveredPiece");
            severedObj.transform.position = cutNodeB.Position;

            VerletRope severedRope = severedObj.AddComponent<VerletRope>();
            severedRope.config = config;
            severedRope.pinStart = false; // severed end is free
            severedRope.pinEnd = pinEnd;
            severedRope.endAnchor = endAnchor;
            severedRope.endOffset = endOffset;
            severedRope.isCut = true;

            severedRope.particles.Clear();
            severedRope.particles.Add(cutNodeB);
            severedRope.particles.AddRange(remainingParticles);
            severedRope.segmentRestLength = segmentRestLength;
            severedRope.initialTotalRestLength = segmentRestLength * (severedRope.particles.Count - 1);

            // Copy material and renderer settings
            RopeRenderer originalRend = GetComponent<RopeRenderer>();
            RopeRenderer newRend = severedObj.GetComponent<RopeRenderer>();
            if (originalRend != null && newRend != null)
            {
                newRend.CopySettingsFrom(originalRend);
                newRend.Initialize(severedRope);
            }

            // Auto cleanup severed rope after a short duration if both ends are unpinned
            Destroy(severedObj, 4.0f);
        }

        private Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float sqrLen = ab.sqrMagnitude;
            if (sqrLen < 0.0001f) return a;

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / sqrLen);
            return a + t * ab;
        }

        private void OnDrawGizmos()
        {
            if (particles == null || particles.Count < 2)
            {
                if (startAnchor != null && endAnchor != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(StartPosition, EndPosition);
                }
                return;
            }

            // Draw particle chain and tension color in editor
            float tColor = Mathf.Clamp01((currentTensionRatio - 1f) / Mathf.Max(0.01f, (config != null ? config.maxTensionThreshold - 1f : 0.25f)));
            Gizmos.color = Color.Lerp(Color.green, Color.red, tColor);

            for (int i = 0; i < particles.Count - 1; i++)
            {
                Gizmos.DrawLine(particles[i].Position, particles[i + 1].Position);
                Gizmos.DrawWireSphere(particles[i].Position, config != null ? config.particleRadius : 0.04f);
            }
            Gizmos.DrawWireSphere(particles[particles.Count - 1].Position, config != null ? config.particleRadius : 0.04f);
        }
    }
}
