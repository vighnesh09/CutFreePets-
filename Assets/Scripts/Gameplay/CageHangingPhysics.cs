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
        [Header("Idle Hanging Sway (Organic Pendulum)")]
        [SerializeField] private bool enableIdleSway = true;
        [SerializeField] private float idleSwayAngle = 3.2f; // +/- 3.2 degrees
        [SerializeField] private float idleSwaySpeed = 1.45f;
        [SerializeField] private float idleSwayForce = 2.2f; // Gentle harmonic breathing push
        [SerializeField] private float inAirTiltFactor = 4.2f; // Tilt based on horizontal velocity

        private Rigidbody2D rb2D;
        private float swayTimer = 0f;
        private float currentSwayZ = 0f;

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
        /// </summary>
        public void UpdateSway(bool isLanded, bool isFreeFalling, int activeRopeCount)
        {
            if (isLanded || rb2D == null) return;

            if (!isFreeFalling && enableIdleSway && activeRopeCount > 0)
            {
                swayTimer += Time.deltaTime * idleSwaySpeed;
                float speed = Mathf.Abs(rb2D.linearVelocity.x);

                if (speed < 1.0f)
                {
                    // Gentle living harmonic breathing tilt when hanging in equilibrium
                    float breatheAngle = Mathf.Sin(swayTimer) * idleSwayAngle;
                    float targetAngle = Mathf.Clamp(-rb2D.linearVelocity.x * 2.2f + breatheAngle, -idleSwayAngle * 1.5f, idleSwayAngle * 1.5f);
                    currentSwayZ = Mathf.Lerp(currentSwayZ, targetAngle, Time.deltaTime * 3.5f);
                    rb2D.rotation = currentSwayZ;
                }
                else
                {
                    // Dynamic pendulum swing tilt (e.g. when 1 rope of a 2-rope setup is severed)
                    float swingTilt = Mathf.Clamp(-rb2D.linearVelocity.x * 4.2f, -28f, 28f);
                    currentSwayZ = Mathf.Lerp(currentSwayZ, swingTilt, Time.deltaTime * 6f);
                    rb2D.rotation = currentSwayZ;
                }
            }
            else if (isFreeFalling)
            {
                // In free-fall: natural aerodynamic tilting based on horizontal drift and slight tumble
                float targetFallTilt = Mathf.Clamp(-rb2D.linearVelocity.x * inAirTiltFactor, -18f, 18f);
                currentSwayZ = Mathf.Lerp(currentSwayZ, targetFallTilt, Time.deltaTime * 5.0f);
                rb2D.rotation = currentSwayZ;
            }
        }

        /// <summary>
        /// FixedUpdate lateral micro-push to keep rope chain dynamic and alive.
        /// </summary>
        public void FixedUpdatePush(bool isLanded, bool isFreeFalling, int activeRopeCount)
        {
            if (isLanded || rb2D == null) return;

            if (!isFreeFalling && enableIdleSway && activeRopeCount > 0)
            {
                // Gentle periodic harmonic impulse produces living left-right movement
                float lateralPush = (Mathf.Sin(swayTimer) * 0.75f + Mathf.Sin(swayTimer * 0.45f) * 0.25f) * idleSwayForce;
                rb2D.AddForce(new Vector2(lateralPush, 0f), ForceMode2D.Force);
            }
        }
    }
}
