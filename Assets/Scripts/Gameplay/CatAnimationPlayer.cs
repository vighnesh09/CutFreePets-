using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CutFreePets.Gameplay
{
    public enum CatMood
    {
        Sad,    // When trapped inside hanging cage
        Happy   // When rescued / landed safely
    }

    /// <summary>
    /// Dual-state animated sprite player for the pet inside the cage.
    /// Supports both Sad (trapped crying) and Happy (rescued smiling) animations
    /// with crystal-clear transparency, proper cage layering, and responsive mood transitions.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CatAnimationPlayer : MonoBehaviour
    {
        [Header("Animation State")]
        [SerializeField] private CatMood currentMood = CatMood.Sad;
        [SerializeField] private float playbackFps = 12f;
        [SerializeField] private bool autoSwitchOnLanding = true;

        [Header("Sprite Animation Clips")]
        [SerializeField] private Sprite[] sadFrames;
        [SerializeField] private Sprite[] happyFrames;

        [Header("Visual Sizing")]
        [SerializeField] private Vector2 spriteDisplaySize = new Vector2(0.9f, 0.9f);
        [SerializeField] private Vector3 slotLocalOffset = new Vector3(0f, -0.05f, 0f);

        private SpriteRenderer spriteRenderer;
        private PetCage parentCage;
        private float frameTimer = 0f;
        private int currentFrameIndex = 0;

        public CatMood CurrentMood
        {
            get => currentMood;
            set => SetMood(value);
        }

        public Sprite[] SadFrames => sadFrames;
        public Sprite[] HappyFrames => happyFrames;

        private void Awake()
        {
            EnsureComponents();
        }

        private void OnValidate()
        {
            EnsureComponents();
            UpdateSprite();
        }

        private void OnEnable()
        {
            EnsureComponents();
            BindToCage();
        }

        private void OnDisable()
        {
            UnbindFromCage();
        }

        private void EnsureComponents()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            // Layering: Behind cage front wire bars (sortingOrder 8), in front of background
            spriteRenderer.sortingOrder = 7;
            spriteRenderer.color = Color.white;

            transform.localScale = new Vector3(spriteDisplaySize.x, spriteDisplaySize.y, 1f);
            transform.localPosition = slotLocalOffset;

#if UNITY_EDITOR
            if ((sadFrames == null || sadFrames.Length == 0) || (happyFrames == null || happyFrames.Length == 0))
            {
                LoadFramesFromProject();
            }
#endif
        }

        private void BindToCage()
        {
            if (parentCage == null) parentCage = GetComponentInParent<PetCage>();
            if (parentCage != null && autoSwitchOnLanding)
            {
                parentCage.OnLandedOnGoal -= HandleCageLanded;
                parentCage.OnLandedOnGoal += HandleCageLanded;
            }
        }

        private void UnbindFromCage()
        {
            if (parentCage != null)
            {
                parentCage.OnLandedOnGoal -= HandleCageLanded;
            }
        }

        private void HandleCageLanded(Collision2D col)
        {
            SetMood(CatMood.Happy);
        }

        public void SetMood(CatMood mood)
        {
            if (currentMood == mood) return;
            currentMood = mood;
            currentFrameIndex = 0;
            frameTimer = 0f;
            UpdateSprite();
        }

        private void Update()
        {
            if (spriteRenderer == null) EnsureComponents();

            Sprite[] activeFrames = (currentMood == CatMood.Sad) ? sadFrames : happyFrames;
            if (activeFrames == null || activeFrames.Length == 0) return;

            float dt = Application.isPlaying ? Time.deltaTime : 0.033f;
            frameTimer += dt;

            float frameDuration = 1f / Mathf.Max(1f, playbackFps);
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                currentFrameIndex = (currentFrameIndex + 1) % activeFrames.Length;
                UpdateSprite();
            }
        }

        private void UpdateSprite()
        {
            Sprite[] activeFrames = (currentMood == CatMood.Sad) ? sadFrames : happyFrames;
            if (activeFrames != null && activeFrames.Length > 0)
            {
                currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, activeFrames.Length - 1);
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = activeFrames[currentFrameIndex];
                    spriteRenderer.sortingOrder = 7;
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Set Mood: Happy")]
        public void TestSetHappy() => SetMood(CatMood.Happy);

        [ContextMenu("Set Mood: Sad")]
        public void TestSetSad() => SetMood(CatMood.Sad);

        [ContextMenu("Reload Animation Frames")]
        public void LoadFramesFromProject()
        {
            sadFrames = LoadSpritesFromDirectory("Assets/Art/cat/Frames_Sad");
            happyFrames = LoadSpritesFromDirectory("Assets/Art/cat/Frames_Happy");

            EnsureComponents();
            UpdateSprite();
            EditorUtility.SetDirty(this);
        }

        private Sprite[] LoadSpritesFromDirectory(string dirPath)
        {
            if (!AssetDatabase.IsValidFolder(dirPath)) return new Sprite[0];

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { dirPath });
            List<Sprite> list = new List<Sprite>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null) list.Add(s);
            }

            // Sort naturally by filename
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            return list.ToArray();
        }
#endif
    }
}
