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
        [SerializeField] private float idleSwayAngle = 2.4f; // +/- 2.4 degrees
        [SerializeField] private float idleSwaySpeed = 1.35f;
        [SerializeField] private float inAirTiltFactor = 4.0f; // Tilt based on horizontal velocity

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
            if (isLanded) return;

            if (!isFreeFalling && enableIdleSway && activeRopeCount > 0)
            {
                // Organic breathing pendulum sway while hanging
                swayTimer += Time.deltaTime * idleSwaySpeed;
                float targetAngle = (Mathf.Sin(swayTimer) * 0.75f + Mathf.Sin(swayTimer * 0.5f) * 0.25f) * idleSwayAngle;
                currentSwayZ = Mathf.Lerp(currentSwayZ, targetAngle, Time.deltaTime * 3.5f);
                transform.rotation = Quaternion.Euler(0f, 0f, currentSwayZ);
            }
            else if (isFreeFalling && rb2D != null)
            {
                // In free-fall: aerodynamic tilting based on horizontal drift
                float targetFallTilt = Mathf.Clamp(-rb2D.linearVelocity.x * inAirTiltFactor, -10f, 10f);
                currentSwayZ = Mathf.Lerp(currentSwayZ, targetFallTilt, Time.deltaTime * 4.5f);
                transform.rotation = Quaternion.Euler(0f, 0f, currentSwayZ);
            }
        }

        /// <summary>
        /// FixedUpdate lateral micro-push to keep rope chain dynamic.
        /// </summary>
        public void FixedUpdatePush(bool isLanded, bool isFreeFalling, int activeRopeCount)
        {
            if (isLanded) return;

            if (!isFreeFalling && enableIdleSway && rb2D != null && activeRopeCount > 0)
            {
                float lateralPush = Mathf.Cos(swayTimer) * 0.18f;
                rb2D.AddForce(new Vector2(lateralPush, 0f), ForceMode2D.Force);
            }
        }
    }
}
