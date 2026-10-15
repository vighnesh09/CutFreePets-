#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CutFreePets.Rope;
using CutFreePets.Gameplay;
using CutFreePets.Cut;

namespace CutFreePets.Editor
{
    public static class RopeSceneSetupMenu
    {
        [MenuItem("Cut Free Pets/Legacy Builders/Setup 1-Rope Cage Level", false, 100)]
        public static void SetupHangingBoxDemo()
        {
            SetupHangingBoxDemoInternal(true);
        }

        public static void SetupHangingBoxDemoInternal(bool showDialog)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Clean 1-Rope 1-Box Demo");
            int group = Undo.GetCurrentGroup();

            // 1. Clean up ALL previous old demo roots and loose objects in scene
            string[] oldNames = new string[] { 
                "CutTheRope_Demo", "HangingBox_Level", "CutTheRope_Level", "RopeDemo_Setup", "RopeDemo",
                "Peg_TopAnchor", "Obstacle_Peg", "Pet_Target", "Peg_Top", "Weight_Bottom",
                "TopMiddle_Hanger", "Hanging_Box", "Pet_Cage", "Rope", "SwipeCutBlade", "Directional Light",
                "Star_Collectible_01", "Star_Collectible_02", "Star_Collectible_03", "Star_Collectible", "LevelScoreTracker",
                "VictoryCanvas", "EventSystem"
            };
            foreach (string name in oldNames)
            {
                foreach (var obj in Object.FindObjectsByType<GameObject>())
                {
                    if (obj != null && obj.name == name)
                    {
                        Undo.DestroyObjectImmediate(obj);
                    }
                }
            }

            // Clean up ANY existing SpriteRenderers using the peg disc sprite in the scene
            Sprite pegDiscSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Rope/Peg_Disc_Albedo.png");
            if (pegDiscSprite == null)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Rope/Peg_Disc_Albedo.png"))
                {
                    if (asset is Sprite s)
                    {
                        pegDiscSprite = s;
                        break;
                    }
                }
            }
            if (pegDiscSprite != null)
            {
                foreach (var sr in Object.FindObjectsByType<SpriteRenderer>())
                {
                    if (sr != null && sr.sprite == pegDiscSprite)
                    {
                        Undo.DestroyObjectImmediate(sr.gameObject);
                    }
                }
            }

            // Clean up any other objects having RopeHanger or VerletRope
            foreach (var h in Object.FindObjectsByType<RopeHanger>())
            {
                if (h != null) Undo.DestroyObjectImmediate(h.gameObject);
            }
            foreach (var r in Object.FindObjectsByType<VerletRope>())
            {
                if (r != null) Undo.DestroyObjectImmediate(r.gameObject);
            }

            // 2. Set Mobile Vertical Screen Project Orientation (Portrait)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // 3. Config & Braided Material
            RopeConfig config = GetOrCreateRopeConfig();
            Material ropeMat = GetOrCreateBraidedMaterial();

            // 4. Camera setup optimized for 9:16 Vertical Mobile Screen
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                camObj.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
            }
            cam.orthographic = true;
            cam.orthographicSize = 5.0f; // Perfect vertical mobile view framing (9:16)
            cam.transform.position = new Vector3(0f, 0.6f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.18f, 0.16f, 0.23f); // Clean cozy dark purple
            cam.clearFlags = CameraClearFlags.SolidColor;

            // 5. Single Root Container
            GameObject root = new GameObject("HangingBox_Level");
            Undo.RegisterCreatedObjectUndo(root, "Create Root");

            // 6. ONE Top Rope Anchor (Hanger peg outside/above visible screen at Y = 5.8)
            // Creates the authentic "rope enters from ceiling" Cut the Rope aesthetic
            GameObject hangerObj = new GameObject("TopMiddle_Hanger");
            hangerObj.transform.parent = root.transform;
            hangerObj.transform.position = new Vector3(0f, 5.8f, 0f);
            RopeHanger hanger = hangerObj.AddComponent<RopeHanger>();
            hanger.SetupVisuals();

            // 7. ONE Hanging Pet Cage (Lifted higher up at Y = 2.4 so rope hangs down naturally)
            GameObject cageObj = new GameObject("Pet_Cage");
            cageObj.transform.parent = root.transform;
            cageObj.transform.position = new Vector3(0f, 2.4f, 0f);

            BoxCollider2D cageCol = cageObj.AddComponent<BoxCollider2D>();
            cageCol.size = new Vector2(3.38f, 3.1f);
            cageCol.offset = new Vector2(0f, -0.25f);

            CatSpriteImporterHelper.ReimportCatSprites();

            PetCage petCage = cageObj.AddComponent<PetCage>();
            petCage.SetupVisuals();
            petCage.EnsureDebrisSpritesLoaded();
            EditorUtility.SetDirty(petCage);

            // Mount the animated Cat inside the cage at the pet slot
            GameObject catObj = new GameObject("Cat_Character");
            catObj.transform.parent = petCage.PetSlot;
            catObj.transform.localPosition = Vector3.zero;
            catObj.transform.localRotation = Quaternion.identity;
            catObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            CatAnimationPlayer catPlayer = catObj.AddComponent<CatAnimationPlayer>();
            catPlayer.LoadFramesFromProject();

            // Dedicated top ring hook point where rope binds
            GameObject ringHookObj = new GameObject("TopRing_Hook");
            ringHookObj.transform.parent = cageObj.transform;
            ringHookObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            ringHookObj.transform.localRotation = Quaternion.identity;

            // 8. Polished Golden-Wood Landing Platform at bottom of screen (Y = -3.85)
            // Slim, sleek, and perfectly sized
            GameObject floorObj = new GameObject("Ground_Platform");
            floorObj.transform.parent = root.transform;
            floorObj.transform.position = new Vector3(0f, -3.85f, 0f);

            Sprite woodPlatformSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Platform_Wood_Golden.png");

            SpriteRenderer floorRend = floorObj.AddComponent<SpriteRenderer>();
            floorRend.sprite = woodPlatformSprite;
            floorRend.color = Color.white;
            floorRend.sortingOrder = 4;
            floorObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            BoxCollider2D floorCol = floorObj.AddComponent<BoxCollider2D>();
            // Collider sized to top surface of the wooden dock
            floorCol.size = new Vector2(3.9f, 0.5f);
            floorCol.offset = new Vector2(0f, -0.05f);

            // 9. THREE COLLECTIBLE STARS aligned in a single vertical column (X = 0)
            // Sized cleanly at scale 0.35, with generous 1.25m vertical spacing
            // Star 1 (Y = 0.1f), Star 2 (Y = -1.15f), Star 3 (Y = -2.4f)
            Vector3[] starPositions = new Vector3[] {
                new Vector3(0f, 0.10f, 0f),
                new Vector3(0f, -1.15f, 0f),
                new Vector3(0f, -2.40f, 0f)
            };

            Sprite starSpriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");
            Shader starShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            Material starSharedMat = starShader != null ? new Material(starShader) : null;

            for (int s = 0; s < 3; s++)
            {
                GameObject starObj = new GameObject($"Star_Collectible_0{s + 1}");
                starObj.transform.parent = root.transform;
                starObj.transform.position = starPositions[s];
                starObj.transform.localScale = new Vector3(0.24f, 0.24f, 1f);

                SpriteRenderer sr = starObj.AddComponent<SpriteRenderer>();
                sr.sprite = starSpriteAsset;
                if (starSharedMat != null) sr.material = starSharedMat;
                sr.sortingOrder = 15;

                CircleCollider2D col = starObj.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;

                starObj.AddComponent<StarCollectible>();
            }

            // 10. LevelScoreTracker to record stars and progress
            GameObject trackerObj = new GameObject("LevelScoreTracker");
            trackerObj.transform.parent = root.transform;
            trackerObj.AddComponent<LevelScoreTracker>();

            // 11. ONE Braided Rope Connecting Top Hanger -> Cage Top Ring Hook
            GameObject ropeObj = new GameObject("Rope");
            ropeObj.transform.parent = root.transform;
            ropeObj.transform.position = hangerObj.transform.position;

            VerletRope rope = ropeObj.AddComponent<VerletRope>();
            rope.Setup(hangerObj.transform, ringHookObj.transform, config);

            // Link rope to PetCage so it drops when cut
            petCage.AttachRope(rope);

            // 12. Swipe Cut Blade (Pure touch/mouse swipe, zero scissors)
            GameObject bladeObj = new GameObject("SwipeCutBlade");
            bladeObj.transform.parent = root.transform;
            bladeObj.AddComponent<SwipeCutBlade>();

            // 13. Level Clear Victory Popup UI Canvas (Cut the Rope style: wooden board, 3 star sockets, Next/Replay/Menu buttons)
            VictoryPopupBuilder.CreateOrUpdateVictoryPopup(root.transform);

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = ropeObj;

            EditorUtility.SetDirty(root);
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Cut The Rope Layout Complete!",
                    "Vertical mobile (Portrait 9:16) level setup created!\n\n" +
                    "• Top Peg Hanger: Positioned high at Y = 4.5\n" +
                    "• Hanging Pet Cage: Positioned at Y = 1.8\n" +
                    "• 3 Glossy Stars: Floating in fall path\n" +
                    "• Ground Platform: Bottom of screen at Y = -3.8\n" +
                    "• Star Score & Progress Tracking Active!",
                    "Great!"
                );
            }
        }

        private static RopeConfig GetOrCreateRopeConfig()
        {
            string folderPath = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            string assetPath = $"{folderPath}/DefaultRopeConfig.asset";
            RopeConfig config = AssetDatabase.LoadAssetAtPath<RopeConfig>(assetPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<RopeConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
            }

            config.segmentCount = 24;
            config.physicsSubsteps = 3;
            config.constraintIterations = 18;
            config.damping = 0.992f;
            config.bendingStiffness = 0.04f;
            config.elasticity = 0.012f;
            config.slackMultiplier = 1.06f;
            config.baseWidth = 0.18f;
            config.minStretchWidthFactor = 0.72f;
            config.normalColor = new Color(0.95f, 0.82f, 0.62f);
            config.highTensionColor = new Color(1f, 0.38f, 0.26f);
            config.splineSubdivisions = 6;
            config.enableCollision = true;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            return config;
        }

        private static Material GetOrCreateBraidedMaterial()
        {
            string matPath = "Assets/Settings/Rope_Material.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Rope/Rope_Braided_Albedo.png");

            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                             ?? Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }
            }

            if (mat != null && texture != null)
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                mat.color = new Color(0.95f, 0.8f, 0.6f);
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
#endif
