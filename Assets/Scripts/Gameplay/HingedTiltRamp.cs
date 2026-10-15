using System.Collections;
using UnityEngine;

namespace CutFreePets.Gameplay
{
    public enum HingedTiltDirection
    {
        RightSideDown, // Hinge on Left, right side tilts down forming a slope to the right
        LeftSideDown   // Hinge on Right, left side tilts down forming a slope to the left
    }

    /// <summary>
    /// Component: HingedTiltRamp
    /// A realistic wooden ramp with an ornate golden mechanical hinge on one end.
    /// Rests horizontally until the pet cage lands on it, then tilts downward under the cage's
    /// weight, creating a smooth slide ramp that directs the cage toward the goal platform.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class HingedTiltRamp : MonoBehaviour
    {
        [Header("Tilt Configuration (Professional Standard Settings)")]
        [Tooltip("Direction to tilt upon weight impact.")]
        [SerializeField] private HingedTiltDirection direction = HingedTiltDirection.RightSideDown;
        [Tooltip("Resting angle before impact.")]
        [SerializeField] private float initialAngle = 0f;
        [Tooltip("Standardized maximum tilt angle across all puzzle levels (Default: -25 deg for smooth natural slide).")]
        [SerializeField] private float maxTiltAngle = -25f;
        [Tooltip("Rotational tilt speed in degrees per second for natural mechanical weight.")]
        [SerializeField] private float tiltSpeed = 48f;
        [Tooltip("Slight mechanical hesitation delay as weight settles before tilting.")]
        [SerializeField] private float triggerDelay = 0.05f;

        [Header("Physics Settings")]
        [SerializeField] private PhysicsMaterial2D slideMaterial;

        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private Rigidbody2D rb2D;

        private bool hasTriggered = false;
        private float currentAngle = 0f;
        private Coroutine tiltCoroutine;

        public bool HasTriggered => hasTriggered;
        public float CurrentAngle => currentAngle;
        public float MaxTiltAngle { get => maxTiltAngle; set => maxTiltAngle = value; }
        public float TiltSpeed { get => tiltSpeed; set => tiltSpeed = value; }

        private void Awake()
        {
            EnsureComponents();
            ResetRamp();
        }

        private void OnValidate()
        {
            EnsureComponents();
            if (!Application.isPlaying)
            {
                ResetRamp();
            }
        }

        public void EnsureComponents()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            if (spriteRenderer.sprite == null)
            {
#if UNITY_EDITOR
                spriteRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Obstacle_Hinged_Ramp.png");
#endif
            }
            spriteRenderer.sortingOrder = 5;

            // Flipping for Right-to-Left orientation if selected
            bool isFlipped = (direction == HingedTiltDirection.LeftSideDown);
            spriteRenderer.flipX = isFlipped;

            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();

            // Setup smooth slide material
            if (slideMaterial == null)
            {
#if UNITY_EDITOR
                slideMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/Smooth_Slide_Material.physicsMaterial2D");
#endif
            }
            if (slideMaterial != null) boxCollider.sharedMaterial = slideMaterial;

            // Collider size matching the slim wooden plank
            boxCollider.size = new Vector2(4.90f, 0.66f);
            float offsetX = isFlipped ? -2.45f : 2.45f;
            boxCollider.offset = new Vector2(offsetX, 0f);
            boxCollider.edgeRadius = 0.04f;

            // Kinematic Rigidbody2D for high-precision, glitch-free rotational physics
            if (rb2D == null) rb2D = GetComponent<Rigidbody2D>();
            if (rb2D == null) rb2D = gameObject.AddComponent<Rigidbody2D>();
            rb2D.bodyType = RigidbodyType2D.Kinematic;
            rb2D.useFullKinematicContacts = true;
        }

        public void ResetRamp()
        {
            hasTriggered = false;
            currentAngle = initialAngle;
            transform.localEulerAngles = new Vector3(0f, 0f, initialAngle);
            if (rb2D != null) rb2D.rotation = initialAngle;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckAndTrigger(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            CheckAndTrigger(collision);
        }

        private void CheckAndTrigger(Collision2D collision)
        {
            if (hasTriggered) return;

            // Trigger when Pet_Cage lands on the plank
            if (collision.gameObject.GetComponent<PetCage>() != null || 
                collision.gameObject.name.Contains("Pet_Cage") ||
                collision.gameObject.name.Contains("Cage"))
            {
                TriggerTilt();
            }
        }

        public void TriggerTilt()
        {
            if (hasTriggered) return;
            hasTriggered = true;

            if (tiltCoroutine != null) StopCoroutine(tiltCoroutine);
            tiltCoroutine = StartCoroutine(AnimateTilt());
        }

        private IEnumerator AnimateTilt()
        {
            if (triggerDelay > 0f)
            {
                yield return new WaitForSeconds(triggerDelay);
            }

            float targetAngle = (direction == HingedTiltDirection.RightSideDown) ? maxTiltAngle : -maxTiltAngle;
            float duration = Mathf.Abs(targetAngle - currentAngle) / Mathf.Max(10f, tiltSpeed);
            float elapsed = 0f;
            float startAngle = currentAngle;

            while (elapsed < duration)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Organic mechanical S-curve (SmoothStep) easing for realistic weight and soft stop
                float easeT = Mathf.SmoothStep(0f, 1f, t);
                currentAngle = Mathf.Lerp(startAngle, targetAngle, easeT);

                if (rb2D != null && Application.isPlaying)
                {
                    rb2D.MoveRotation(currentAngle);
                }
                transform.localEulerAngles = new Vector3(0f, 0f, currentAngle);
            }

            currentAngle = targetAngle;
            if (rb2D != null && Application.isPlaying)
            {
                rb2D.MoveRotation(currentAngle);
            }
            transform.localEulerAngles = new Vector3(0f, 0f, currentAngle);
        }
    }
}
