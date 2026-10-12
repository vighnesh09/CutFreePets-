using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Tuned physics, collision, tension, and rendering configuration for Cut the Rope feel.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRopeConfig", menuName = "Cut Free Pets/Rope Config")]
    public class RopeConfig : ScriptableObject
    {
        [Header("Structure & Segments")]
        [Tooltip("Number of simulation particles along the rope. Higher = silkier smooth curve.")]
        [Range(8, 40)]
        public int segmentCount = 24;

        [Tooltip("Additional slack ratio at start (1.0 = taut, 1.08 = natural slight hanging curve).")]
        [Range(0.9f, 1.5f)]
        public float slackMultiplier = 1.06f;

        [Header("Physics Solver & Realism")]
        [Tooltip("Substeps per physics update for ultra-stable, butter-smooth rope simulation without jitter.")]
        [Range(1, 6)]
        public int physicsSubsteps = 3;

        [Tooltip("Constraint solver passes per substep.")]
        [Range(4, 30)]
        public int constraintIterations = 18;

        [Tooltip("Velocity air resistance damping. 0.985 gives organic Cut the Rope pendulum swing.")]
        [Range(0.9f, 0.999f)]
        public float damping = 0.986f;

        [Tooltip("Bending stiffness (0 = limp string, 0.2 = natural springy rope).")]
        [Range(0f, 0.3f)]
        public float bendingStiffness = 0.08f;

        [Tooltip("Compliance elasticity (stretch factor).")]
        [Range(0f, 0.15f)]
        public float elasticity = 0.015f;

        [Tooltip("Gravity vector for rope particles.")]
        public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

        [Header("Collision & Wrapping")]
        [Tooltip("Enables collision wrapping around obstacles and pegs.")]
        public bool enableCollision = true;

        public LayerMask collisionMask = ~0;

        [Range(0.01f, 0.2f)]
        public float particleRadius = 0.035f;

        [Range(0.001f, 0.05f)]
        public float skinWidth = 0.01f;

        [Header("Visuals & Tension")]
        [Tooltip("Base thickness of the rope when relaxed.")]
        public float baseWidth = 0.12f;

        [Tooltip("Thickness ratio when under maximum tension (gets thinner under load).")]
        [Range(0.3f, 1f)]
        public float minStretchWidthFactor = 0.58f;

        [Tooltip("Stretch ratio considered full tension.")]
        public float maxTensionThreshold = 1.25f;

        [Tooltip("Natural golden-brown braided hemp color.")]
        public Color normalColor = new Color(0.96f, 0.82f, 0.62f);

        [Tooltip("Warning red-orange tint when strained.")]
        public Color highTensionColor = new Color(1f, 0.38f, 0.26f);

        [Tooltip("High-tension strain tremble amplitude.")]
        [Range(0f, 0.05f)]
        public float tensionVibrationAmount = 0.014f;

        [Header("Spline Smoothing")]
        [Tooltip("Subdivisions per segment for silky Catmull-Rom spline curves.")]
        [Range(2, 10)]
        public int splineSubdivisions = 6;
    }
}
