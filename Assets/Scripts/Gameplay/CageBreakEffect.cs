using System;
using System.Collections;
using UnityEngine;

namespace CutFreePets.Gameplay
{
    /// <summary>
    /// Component: CageBreakEffect
    /// Responsibility:
    /// - Spawns shattering debris pieces (handle, pillars, door bars, base tray)
    /// - Spawns 360-degree golden impact sparkles
    /// - Creates a soft floor shadow under the rescued pet
    /// - Executes screen camera micro-shake
    /// - Grounds the pet firmly onto the landing platform with cartoon squash-and-stretch
    /// </summary>
    [DisallowMultipleComponent]
    public class CageBreakEffect : MonoBehaviour
    {
        [Header("Explosion Settings")]
        [SerializeField] private bool enableBreakShatter = true;
        [SerializeField] private float debrisExplodeForce = 4.2f;

        [Header("Debris Sprites")]
        [SerializeField] private Sprite topHandleSprite;
        [SerializeField] private Sprite doorBarsSprite;
        [SerializeField] private Sprite leftPillarSprite;
        [SerializeField] private Sprite rightPillarSprite;
        [SerializeField] private Sprite baseTraySprite;
        [SerializeField] private Sprite groundShadowSprite;
        [SerializeField] private Sprite sparkSprite;

        public bool EnableBreakShatter
        {
            get => enableBreakShatter;
            set => enableBreakShatter = value;
        }

        private void Awake()
        {
            EnsureDebrisSpritesLoaded();
        }

        public void EnsureDebrisSpritesLoaded()
        {
#if UNITY_EDITOR
            if (topHandleSprite == null)
                topHandleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Top_Handle.png");
            if (doorBarsSprite == null)
                doorBarsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Door_Bars.png");
            if (leftPillarSprite == null)
                leftPillarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Left_Pillar.png");
            if (rightPillarSprite == null)
                rightPillarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Right_Pillar.png");
            if (baseTraySprite == null)
                baseTraySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Debris/Debris_Base_Tray.png");
            if (groundShadowSprite == null)
                groundShadowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Cat_Ground_Shadow.png");
            if (sparkSprite == null)
                sparkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Debris/Snip_Spark.png");
#endif
            if (topHandleSprite == null) topHandleSprite = Resources.Load<Sprite>("Debris/Debris_Top_Handle");
            if (doorBarsSprite == null) doorBarsSprite = Resources.Load<Sprite>("Debris/Debris_Door_Bars");
            if (leftPillarSprite == null) leftPillarSprite = Resources.Load<Sprite>("Debris/Debris_Left_Pillar");
            if (rightPillarSprite == null) rightPillarSprite = Resources.Load<Sprite>("Debris/Debris_Right_Pillar");
            if (baseTraySprite == null) baseTraySprite = Resources.Load<Sprite>("Debris/Debris_Base_Tray");
            if (groundShadowSprite == null) groundShadowSprite = Resources.Load<Sprite>("Cat_Ground_Shadow");
            if (sparkSprite == null) sparkSprite = Resources.Load<Sprite>("Debris/Snip_Spark");

            if (topHandleSprite == null) topHandleSprite = CreateFallbackDebrisSprite(new Color(0.95f, 0.78f, 0.25f), 64, 40);
            if (doorBarsSprite == null) doorBarsSprite = CreateFallbackDebrisSprite(new Color(0.90f, 0.72f, 0.20f), 24, 80);
            if (leftPillarSprite == null) leftPillarSprite = CreateFallbackDebrisSprite(new Color(0.85f, 0.65f, 0.18f), 40, 100);
            if (rightPillarSprite == null) rightPillarSprite = CreateFallbackDebrisSprite(new Color(0.85f, 0.65f, 0.18f), 40, 100);
            if (baseTraySprite == null) baseTraySprite = CreateFallbackDebrisSprite(new Color(0.78f, 0.55f, 0.15f), 120, 32);
        }

        private Sprite CreateFallbackDebrisSprite(Color col, int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++) cols[i] = col;
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Executes the full shattering & rescue sequence when cage lands.
        /// </summary>
        public void PlayBreakSequence(Vector3 cagePos, Quaternion cageRot, float platformTopY, Transform petSlot)
        {
            EnsureDebrisSpritesLoaded();

            // 1. Spawning anatomical shattered debris pieces
            if (enableBreakShatter)
            {
                // Top Handle: pops up and tumbles
                SpawnDebrisPiece(topHandleSprite, cagePos + new Vector3(0f, 1.45f, 0f), cageRot, 0.70f,
                    new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f), debrisExplodeForce), UnityEngine.Random.Range(180f, 260f));

                // Left Pillar: bursts outward left and up
                SpawnDebrisPiece(leftPillarSprite, cagePos + new Vector3(-0.95f, 0.10f, 0f), cageRot, 0.60f,
                    new Vector2(-2.8f, 2.0f), -320f);

                // Right Pillar: bursts outward right and up
                SpawnDebrisPiece(rightPillarSprite, cagePos + new Vector3(0.95f, 0.10f, 0f), cageRot, 0.60f,
                    new Vector2(2.8f, 2.0f), 320f);

                // Door Bars: pops forward/down
                SpawnDebrisPiece(doorBarsSprite, cagePos + new Vector3(0f, 0.05f, 0f), cageRot, 0.60f,
                    new Vector2(UnityEngine.Random.Range(-0.6f, 0.6f), 1.8f), 180f);

                // Base Tray: drops to platform surface
                SpawnDebrisPiece(baseTraySprite, cagePos + new Vector3(0f, -1.35f, 0f), cageRot, 0.65f,
                    new Vector2(UnityEngine.Random.Range(-0.3f, 0.3f), 0.8f), 60f);

                // 2. 8 Golden Star Sparkles in a 360 degree radial burst
                if (sparkSprite != null)
                {
                    for (int s = 0; s < 8; s++)
                    {
                        float angle = s * (360f / 8f) + UnityEngine.Random.Range(-12f, 12f);
                        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                        SpawnSparkPiece(sparkSprite, cagePos + new Vector3(dir.x * 0.25f, dir.y * 0.25f, 0f), dir * 4.4f);
                    }
                }
            }

            // 3. Ground contact shadow on landing platform
            if (groundShadowSprite != null)
            {
                GameObject shadowObj = new GameObject("Cat_Ground_Shadow");
                shadowObj.transform.position = new Vector3(cagePos.x, platformTopY + 0.02f, 0f);
                shadowObj.transform.localScale = new Vector3(1.7f, 0.42f, 1f);

                SpriteRenderer srShadow = shadowObj.AddComponent<SpriteRenderer>();
                srShadow.sprite = groundShadowSprite;
                srShadow.color = new Color(0f, 0f, 0f, 0.45f);
                srShadow.sortingOrder = 4;
            }

            // 4. Firmly ground pet onto platform with no floating
            if (petSlot != null)
            {
                petSlot.SetParent(null, true);
                float targetCatY = platformTopY + 0.86f;
                Vector3 targetGroundedPos = new Vector3(cagePos.x, targetCatY, 0f);
                petSlot.position = targetGroundedPos;

                CatAnimationPlayer cat = petSlot.GetComponentInChildren<CatAnimationPlayer>();
                if (cat != null)
                {
                    cat.SetMood(CatMood.Happy);
                    StartCoroutine(GroundedLandingBounce(cat.transform, targetGroundedPos));
                }
            }

            // 5. Camera micro impact shake
            StartCoroutine(CameraImpactShake(0.06f, 0.16f));
        }

        private void SpawnDebrisPiece(Sprite spr, Vector3 startPos, Quaternion startRot, float scale, Vector2 velocity, float angularVel)
        {
            if (spr == null) return;

            GameObject debObj = new GameObject($"Debris_{spr.name}");
            debObj.transform.position = startPos;
            debObj.transform.rotation = startRot;
            debObj.transform.localScale = Vector3.one * scale;

            SpriteRenderer sr = debObj.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = 9;

            Rigidbody2D debRb = debObj.AddComponent<Rigidbody2D>();
            debRb.gravityScale = 2.2f;
            debRb.linearVelocity = velocity;
            debRb.angularVelocity = angularVel;

            StartCoroutine(FadeAndDestroyDebris(debObj, sr, 0.85f, 0.45f));
        }

        private void SpawnSparkPiece(Sprite spr, Vector3 startPos, Vector2 velocity)
        {
            if (spr == null) return;

            GameObject sparkObj = new GameObject("Debris_Spark");
            sparkObj.transform.position = startPos;
            sparkObj.transform.localScale = Vector3.one * 0.55f;

            SpriteRenderer ssr = sparkObj.AddComponent<SpriteRenderer>();
            ssr.sprite = spr;
            ssr.sortingOrder = 11;
            ssr.color = new Color(1f, 0.92f, 0.35f, 1f);

            Rigidbody2D srb = sparkObj.AddComponent<Rigidbody2D>();
            srb.gravityScale = 0.8f;
            srb.linearVelocity = velocity;
            srb.angularVelocity = UnityEngine.Random.Range(-400f, 400f);

            StartCoroutine(FadeAndDestroyDebris(sparkObj, ssr, 0.45f, 0.35f));
        }

        private IEnumerator GroundedLandingBounce(Transform catTransform, Vector3 groundPos)
        {
            Vector3 baseScale = catTransform != null ? catTransform.localScale : new Vector3(1.22f, 1.22f, 1f);
            float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (catTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                catTransform.position = groundPos;

                float squashWave = Mathf.Sin(t * Mathf.PI * 2f) * Mathf.Exp(-t * 3.5f);
                float scaleX = baseScale.x * (1f + squashWave * 0.14f);
                float scaleY = baseScale.y * (1f - squashWave * 0.14f);
                catTransform.localScale = new Vector3(scaleX, scaleY, baseScale.z);

                yield return null;
            }

            if (catTransform != null)
            {
                catTransform.position = groundPos;
                catTransform.localScale = baseScale;
            }
        }

        private IEnumerator CameraImpactShake(float intensity, float duration)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3 origPos = cam.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentIntensity = Mathf.Lerp(intensity, 0f, elapsed / duration);
                cam.transform.position = origPos + (Vector3)(UnityEngine.Random.insideUnitCircle * currentIntensity);
                yield return null;
            }

            if (cam != null) cam.transform.position = origPos;
        }

        private IEnumerator FadeAndDestroyDebris(GameObject debObj, SpriteRenderer sr, float delay, float fadeDuration)
        {
            yield return new WaitForSeconds(delay);
            float elapsed = 0f;
            Color c = sr != null ? sr.color : Color.white;
            Vector3 initScale = debObj != null ? debObj.transform.localScale : Vector3.one;

            while (elapsed < fadeDuration)
            {
                if (sr == null || debObj == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float alpha = Mathf.Lerp(c.a, 0f, t);
                sr.color = new Color(c.r, c.g, c.b, alpha);
                debObj.transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                yield return null;
            }
            if (debObj != null) Destroy(debObj);
        }
    }
}
