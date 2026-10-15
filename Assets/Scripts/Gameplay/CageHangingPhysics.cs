using UnityEngine;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Component: CageHangingPhysics
    /// Responsibility:
    /// - Manages natural harmonic pendulum sway while suspended on ropes (Cut the Rope feel)
    /// - Manages in-air aerodynamic angular tilt during free fall
    /// - Applies subtle lateral wave physics force to connected ropes
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class CageHangingPhysics : MonoBehaviour
    {
        [Header("Idle Hanging Sway (Subtle Living Movement)")]
        [SerializeField] private bool enableIdleSway = true;
        [SerializeField] private float idleSwaySpeed = 1.0f;
        [SerializeField] private float idleSwayForce = 0.22f; // Very gentle subtle micro-drift
        [SerializeField] private float maxIdleSwaySpeed = 0.16f; // Caps swing so it only sways a little
        [SerializeField] private float inAirTiltFactor = 4.2f; // Tilt based on horizontal velocity

        private Rigidbody2D rb2D;

        public bool EnableIdleSway
        {
            get => enableIdleSway;
            set => enableIdleSway = value;
        }

        private void Awake()
        {
            rb2D = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// Updates organic pendulum rotation while suspended or in free-fall.
        /// Direct transform rotation overrides are disabled to prevent joint jitter/vibration.
        /// </summary>
        public void UpdateSway(bool isLanded, bool isFreeFalling, int activeRopeCount)
        {
            // Kept for interface compatibility; physics forces in FixedUpdatePush handle all rotation naturally.
        }

        /// <summary>
        /// FixedUpdate lateral micro-push to keep rope chain dynamic and alive without physics chatter.
        /// </summary>
        public void FixedUpdatePush(bool isLanded, bool isFreeFalling, int activeRopeCount)
        {
            if (isLanded || rb2D == null) return;

            if (!isFreeFalling && enableIdleSway && activeRopeCount > 0)
            {
                // Only apply subtle micro-push if within gentle sway limits
                if (Mathf.Abs(rb2D.linearVelocity.x) < maxIdleSwaySpeed)
                {
                    float t = Time.time * idleSwaySpeed;
                    float lateralPush = (Mathf.Sin(t) * 0.75f + Mathf.Sin(t * 0.45f) * 0.25f) * idleSwayForce;
                    rb2D.AddForce(new Vector2(lateralPush, 0f), ForceMode2D.Force);
                }
            }
            else if (isFreeFalling)
            {
                // Natural aerodynamic stabilization torque during free-fall
                float targetFallTilt = Mathf.Clamp(-rb2D.linearVelocity.x * inAirTiltFactor, -18f, 18f);
                float angleDelta = Mathf.DeltaAngle(rb2D.rotation, targetFallTilt);
                rb2D.AddTorque(angleDelta * 0.25f, ForceMode2D.Force);
            }
        }
    }
}
