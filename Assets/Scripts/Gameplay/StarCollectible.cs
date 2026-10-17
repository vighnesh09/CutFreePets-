using System;
using System.Collections;
using UnityEngine;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Collectible Star for Cut the Rope gameplay.
    /// Floats with a gentle hover bob & shimmer, triggers upon contact with the falling PetCage,
    /// emits a satisfying particle burst/pop, and registers toward the level's 3-star tally.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class StarCollectible : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer starRenderer;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 2.2f;
        [SerializeField] private float rotateSpeed = 15f;

        [Header("State")]
        [SerializeField] private bool isCollected = false;

        public static event Action<StarCollectible> OnStarCollected;
        public bool IsCollected => isCollected;

        private Vector3 startPos;
        private CircleCollider2D col2D;
        private float seed;

        private void Awake()
        {
            col2D = GetComponent<CircleCollider2D>();
            if (col2D == null) col2D = gameObject.AddComponent<CircleCollider2D>();
            col2D.isTrigger = true;
            col2D.radius = 0.45f;

            if (starRenderer == null) starRenderer = GetComponent<SpriteRenderer>();
            if (starRenderer == null) starRenderer = gameObject.AddComponent<SpriteRenderer>();

            starRenderer.sortingOrder = 15; // In front of background and cage

            if (starSprite == null)
            {
                starSprite = Resources.Load<Sprite>("Star_Collectible");
                if (starSprite == null)
                {
                    Sprite[] allSprites = Resources.LoadAll<Sprite>("Star_Collectible");
                    if (allSprites != null && allSprites.Length > 0)
                        starSprite = allSprites[0];
                }
            }

#if UNITY_EDITOR
            if (starSprite == null)
            {
                starSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");
            }
#endif
            if (starSprite != null) starRenderer.sprite = starSprite;

            // Ensure proper unlit sprite shader under URP
            if (starRenderer.sharedMaterial == null || starRenderer.sharedMaterial.shader.name.Contains("Error") || starRenderer.sharedMaterial.shader.name.Contains("Hidden"))
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                if (unlit != null)
                {
                    starRenderer.material = new Material(unlit);
                }
            }

            startPos = transform.position;
            seed = UnityEngine.Random.Range(0f, 10f);
        }

        private void Update()
        {
            if (isCollected) return;

            // Gentle Cut the Rope bobbing & subtle shimmer
            float offset = Mathf.Sin((Time.time * bobSpeed) + seed) * bobAmplitude;
            transform.position = startPos + new Vector3(0f, offset, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.time * 1.5f) + seed) * (rotateSpeed * 0.5f));

            // Continuous overlap check (catches fast pendulum swings and edge overlaps)
            CheckContactFailsafe();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryHandleCollision(collision);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            TryHandleCollision(collision);
        }

        private void TryHandleCollision(Collider2D collision)
        {
            if (isCollected) return;

            // Trigger when touched by PetCage or any part of the falling cage
            PetCage cage = collision.GetComponentInParent<PetCage>();
            if (cage != null)
            {
                Collect();
            }
        }

        private void CheckContactFailsafe()
        {
            if (isCollected) return;

            Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.55f);
            if (hit != null)
            {
                PetCage cage = hit.GetComponentInParent<PetCage>();
                if (cage != null)
                {
                    Collect();
                }
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            if (col2D != null) col2D.enabled = false;
            OnStarCollected?.Invoke(this);
            CutFreePets.Core.HapticManager.PlayStarPickupHaptic();

            StartCoroutine(CollectAnimation());
        }

        private IEnumerator CollectAnimation()
        {
            // Pop & burst animation
            float elapsed = 0f;
            float duration = 0.35f;
            Vector3 initScale = transform.localScale;

            // Spawn 4 golden spark burst bits
            SpawnStarSparks();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Scale up then shrink away
                float scaleMod = (t < 0.35f) ? Mathf.Lerp(1f, 1.45f, t / 0.35f) : Mathf.Lerp(1.45f, 0f, (t - 0.35f) / 0.65f);
                transform.localScale = initScale * scaleMod;

                if (starRenderer != null)
                {
                    Color c = starRenderer.color;
                    c.a = Mathf.Clamp01(1f - t);
                    starRenderer.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void SpawnStarSparks()
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            Material sparkMat = unlit != null ? new Material(unlit) : null;
            Transform parentContainer = transform.parent;

            for (int i = 0; i < 6; i++)
            {
                GameObject spark = new GameObject("StarSpark");
                spark.transform.position = transform.position;
                if (parentContainer != null) spark.transform.SetParent(parentContainer, true);

                SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
                sr.sprite = starSprite;
                if (sparkMat != null) sr.material = sparkMat;
                sr.color = new Color(1f, 0.95f, 0.4f, 0.9f);
                sr.sortingOrder = 10;
                spark.transform.localScale = Vector3.one * 0.18f;

                float angle = i * (360f / 6f) + UnityEngine.Random.Range(-15f, 15f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

                Rigidbody2D rb = spark.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0.8f;
                rb.linearVelocity = dir * UnityEngine.Random.Range(2.5f, 4.2f);

                Destroy(spark, 0.5f);
            }
        }
    }
}
