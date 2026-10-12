using UnityEngine;

namespace CutFreePets.Rope
{
    /// <summary>
    /// Configurable parameters for rope physics, stretch, collision, and visual feedback.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRopeConfig", menuName = "Cut Free Pets/Rope Config")]
    public class RopeConfig : ScriptableObject
    {
        [Header("Structure")]
        [Tooltip("Number of simulation segments between start and end anchors.")]
        [Range(4, 50)]
        public int segmentCount = 18;

        [Tooltip("Additional slack ratio at start (1.0 = taut, 1.2 = 20% extra slack).")]
        [Range(0.8f, 1.5f)]
        public float slackMultiplier = 1.0f;

        [Header("Physics & Solvers")]
        [Tooltip("Higher iterations make the rope stiffer and less stretchy.")]
        [Range(2, 40)]
        public int constraintIterations = 16;

        [Tooltip("Damping factor for particle velocity. Lower = more air resistance.")]
        [Range(0.85f, 0.999f)]
        public float damping = 0.975f;

        [Tooltip("Gravity acceleration applied to rope particles.")]
        public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

        [Tooltip("Elasticity / compliance factor (0 = completely rigid, 0.1 = stretchy like rubber).")]
        [Range(0f, 0.2f)]
        public float elasticity = 0.02f;

        [Header("Collision")]
        [Tooltip("Enables 3D collision against world obstacles (pegs, walls, barriers).")]
        public bool enableCollision = true;

        [Tooltip("Collision layer mask for world obstacles.")]
        public LayerMask collisionMask = ~0;

        [Tooltip("Collision radius for each rope particle.")]
        [Range(0.01f, 0.25f)]
        public float particleRadius = 0.04f;

        [Tooltip("Extra skin buffer to prevent visual clipping through colliders.")]
        [Range(0.001f, 0.05f)]
        public float skinWidth = 0.01f;

        [Header("Visuals & Tension")]
        [Tooltip("Base thickness of the rope when relaxed.")]
        public float baseWidth = 0.08f;

        [Tooltip("Thickness ratio when under maximum tension (rope thins as it stretches).")]
        [Range(0.3f, 1f)]
        public float minStretchWidthFactor = 0.65f;

        [Tooltip("Tension ratio considered full stretch (e.g. 1.25 = 25% stretched beyond rest length).")]
        public float maxTensionThreshold = 1.25f;

        [Tooltip("Base color of the rope.")]
        public Color normalColor = new Color(0.85f, 0.72f, 0.53f); // Warm natural hemp rope

        [Tooltip("Color tint when reaching high tension.")]
        public Color highTensionColor = new Color(1f, 0.45f, 0.35f);

        [Tooltip("Subtle vibration amplitude when rope is under heavy tension.")]
        [Range(0f, 0.05f)]
        public float tensionVibrationAmount = 0.015f;

        [Header("Spline Smoothing")]
        [Tooltip("Subdivisions per segment for silky smooth Catmull-Rom spline curves.")]
        [Range(2, 10)]
        public int splineSubdivisions = 4;
    }
}
