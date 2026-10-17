#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using CutFreePets.Rope;
using CutFreePets.Gameplay;
using CutFreePets.Cut;
using CutFreePets.UI;

namespace CutFreePets.Editor
{
    /// <summary>
    /// Master Level Designer Suite:
    /// 1. Create Ready-to-Play Level with 1 Click (Level_X.unity)
    /// 2. Fast Rope Connector (Select Hanger + Select Cage -> Instant rope)
    /// 3. One-click Add Collectible Stars, Ground Platform, or Hanger Pegs
    /// 4. Auto-registers all levels into Unity Build Settings
    /// </summary>
    public class LevelDesignerWindow : EditorWindow
    {
        private int levelNumberToCreate = 2;
        private RopeHanger selectedHanger;
        private PetCage selectedCage;

        [MenuItem("Cut Free Pets/Play Complete Game (Home Screen)", false, 0)]
        public static void PlayCompleteGameMenu()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HomeScreen.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Cut Free Pets/Open Scene/1. Home Screen (Menu)", false, 10)]
        public static void OpenHomeScreenScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HomeScreen.unity", OpenSceneMode.Single);
        }

        [MenuItem("Cut Free Pets/Open Scene/2. Level Selection (Adventure Map)", false, 11)]
        public static void OpenLevelSelectScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/LevelSelect.unity", OpenSceneMode.Single);
        }

        [MenuItem("Cut Free Pets/Open Scene/3. Game Stage (Single-Scene Architecture)", false, 12)]
        public static void OpenGameScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        }

        [MenuItem("Cut Free Pets/Level Designer Suite", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelDesignerWindow>("Level Designer");
            window.minSize = new Vector2(360f, 480f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("🎮 Cut Free Pets - Level Designer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Design levels in seconds! Create new level prefabs, connect ropes between hangers and cages, or drop gameplay elements with a single click.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);
            GUILayout.Label("1. Create New Level Prefab (Architecture 2)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            levelNumberToCreate = EditorGUILayout.IntField("Level Number", levelNumberToCreate);
            if (GUILayout.Button("Create Prefab", GUILayout.Width(110)))
            {
                CreateNewLevelPrefab(levelNumberToCreate);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);
            GUILayout.Label("2. Instant Rope Connector", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select a Hanger Peg and a Pet Cage in the Scene, then click 'Connect with Rope'.",
                MessageType.None
            );

            selectedHanger = (RopeHanger)EditorGUILayout.ObjectField("Hanger Peg", selectedHanger, typeof(RopeHanger), true);
            selectedCage = (PetCage)EditorGUILayout.ObjectField("Pet Cage", selectedCage, typeof(PetCage), true);

            if (GUILayout.Button("Auto-Detect from Selection", GUILayout.Height(26)))
            {
                AutoDetectSelection();
            }

            EditorGUI.BeginDisabledGroup(selectedHanger == null || selectedCage == null);
            if (GUILayout.Button("🔗 Connect with Rope", GUILayout.Height(32)))
            {
                ConnectRope(selectedHanger, selectedCage);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(15);
            GUILayout.Label("3. Quick Scene Utilities", EditorStyles.boldLabel);

            if (GUILayout.Button("⭐ Add Collectible Star to Scene", GUILayout.Height(28)))
            {
                AddStarToScene();
            }

            if (GUILayout.Button("⚓ Add Rope Hanger Peg to Scene", GUILayout.Height(28)))
            {
                AddHangerToScene();
            }

            if (GUILayout.Button("🪵 Add Ground Platform to Scene", GUILayout.Height(28)))
            {
                AddPlatformToScene();
            }

            if (GUILayout.Button("🛝 Add Sloped Slide Ramp to Scene", GUILayout.Height(28)))
            {
                AddSlopedSlideToScene();
            }

            if (GUILayout.Button("🪵 Add Horizontal Support Plank to Scene", GUILayout.Height(28)))
            {
                AddHorizontalSupportPlankToScene();
            }

            EditorGUILayout.Space(15);
            GUILayout.Label("4. Master Game Stage", EditorStyles.boldLabel);
            if (GUILayout.Button("🎮 Open Game Scene (Play Mode & Preview)", GUILayout.Height(30)))
            {
                OpenGameScene();
            }

            EditorGUILayout.Space(15);
            GUILayout.Label("5. Build Settings Sync", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 Sync Architecture 2 Scenes into Build Settings", GUILayout.Height(30)))
            {
                SyncBuildSettings();
            }
        }

        [MenuItem("Cut Free Pets/Select Level 1 Prefab", false, 3)]
        public static void SelectLevel1Prefab()
        {
            GameObject level1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Levels/Level_001.prefab");
            if (level1Prefab != null)
            {
                Selection.activeObject = level1Prefab;
                EditorGUIUtility.PingObject(level1Prefab);
            }
            else
            {
                Debug.LogWarning("[LevelDesigner] Level_001.prefab not found in Assets/Resources/Levels/");
            }
        }

        private void AutoDetectSelection()
        {
            foreach (var go in Selection.gameObjects)
            {
                var h = go.GetComponentInParent<RopeHanger>();
                if (h != null) selectedHanger = h;

                var c = go.GetComponentInParent<PetCage>();
                if (c != null) selectedCage = c;
            }
        }

        [MenuItem("Cut Free Pets/Quick Connect Selected (Hanger + Cage) %&r", false, 2)]
        public static void QuickConnectFromMenu()
        {
            RopeHanger hanger = null;
            PetCage cage = null;

            foreach (var go in Selection.gameObjects)
            {
                if (hanger == null) hanger = go.GetComponentInParent<RopeHanger>();
                if (cage == null) cage = go.GetComponentInParent<PetCage>();
            }

            if (hanger == null || cage == null)
            {
                EditorUtility.DisplayDialog(
                    "Select Objects First",
                    "Please select both a Rope Hanger and a Pet Cage in the Scene or Hierarchy before connecting.",
                    "OK"
                );
                return;
            }

            ConnectRope(hanger, cage);
        }

        public static void ConnectRope(RopeHanger hanger, PetCage cage)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Connect Rope");
            int group = Undo.GetCurrentGroup();

            Transform cageHook = cage.transform.Find("TopRing_Hook");
            if (cageHook == null)
            {
                GameObject hookObj = new GameObject("TopRing_Hook");
                hookObj.transform.parent = cage.transform;
                hookObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                hookObj.transform.localRotation = Quaternion.identity;
                cageHook = hookObj.transform;
                Undo.RegisterCreatedObjectUndo(hookObj, "Create TopRing_Hook");
            }

            GameObject ropeObj = new GameObject($"Rope_{hanger.name}_{cage.name}");
            ropeObj.transform.position = hanger.AttachWorldPosition;
            Undo.RegisterCreatedObjectUndo(ropeObj, "Create Rope");

            RopeConfig config = AssetDatabase.LoadAssetAtPath<RopeConfig>("Assets/Settings/DefaultRopeConfig.asset");
            if (config == null) config = ScriptableObject.CreateInstance<RopeConfig>();

            VerletRope rope = ropeObj.AddComponent<VerletRope>();
            rope.Setup(hanger.transform, cageHook, config);

            cage.AttachRope(rope);

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = ropeObj;
            Debug.Log($"<color=lime>[LevelDesigner]</color> Successfully connected rope between '{hanger.name}' and '{cage.name}'!");
        }

        public static void CreateNewLevelPrefab(int levelIndex)
        {
            string targetPath = $"Assets/Resources/Levels/Level_{levelIndex:D3}.prefab";
            string targetPrefabsPath = $"Assets/Prefabs/Levels/Level_{levelIndex:D3}.prefab";

            if (File.Exists(targetPath))
            {
                bool openIt = EditorUtility.DisplayDialog(
                    "Level Prefab Exists",
                    $"Level prefab '{targetPath}' already exists. Select it?",
                    "Select Prefab",
                    "Cancel"
                );
                if (openIt)
                {
                    GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
                    Selection.activeObject = existing;
                    EditorGUIUtility.PingObject(existing);
                }
                return;
            }

            string templatePath = "Assets/Resources/Levels/Level_001.prefab";
            if (File.Exists(templatePath))
            {
                AssetDatabase.CopyAsset(templatePath, targetPath);
                AssetDatabase.CopyAsset(templatePath, targetPrefabsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
                Debug.Log($"<color=lime>[LevelDesigner]</color> Created new modular Level prefab: {targetPath} (Level {levelIndex})!");

                EditorUtility.DisplayDialog(
                    "Level Prefab Created!",
                    $"Successfully created '{targetPath}'!\n\n" +
                    "• Double-click it to open and edit in Unity's Prefab Mode\n" +
                    $"• In GameScene, press Play and enter {levelIndex} in [LevelLoader] to playtest instantly\n" +
                    "• No Build Settings registration required!",
                    "Awesome"
                );
            }
            else
            {
                Debug.LogError($"[LevelDesigner] Base template '{templatePath}' not found.");
            }
        }

        public static GameObject CreateStarInstance()
        {
            GameObject starPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Star_Collectible.prefab");
            if (starPrefab != null)
            {
                return (GameObject)PrefabUtility.InstantiatePrefab(starPrefab);
            }

            // Create instance cleanly with correct sprite and scale
            GameObject starObj = new GameObject("Star_Collectible");
            Sprite starSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");
            Shader starShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            
            SpriteRenderer sr = starObj.AddComponent<SpriteRenderer>();
            sr.sprite = starSprite;
            if (starShader != null) sr.material = new Material(starShader);
            sr.sortingOrder = 15;
            starObj.transform.localScale = new Vector3(0.24f, 0.24f, 1f);

            CircleCollider2D col = starObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            starObj.AddComponent<StarCollectible>();
            return starObj;
        }

        private static GameObject CreateDefaultHanger()
        {
            GameObject hangerObj = new GameObject("Rope_Hanger");
            hangerObj.AddComponent<RopeHanger>().SetupVisuals();
            return hangerObj;
        }

        private static GameObject CreateDefaultGroundPlatform()
        {
            GameObject floorObj = new GameObject("Ground_Platform");
            Sprite woodPlatformSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Platform_Wood_Golden.png");
            SpriteRenderer floorRend = floorObj.AddComponent<SpriteRenderer>();
            floorRend.sprite = woodPlatformSprite;
            floorRend.color = Color.white;
            floorRend.sortingOrder = 4;
            floorObj.transform.localScale = new Vector3(0.60f, 0.68f, 1f); // Compact platform leaving wide margins for level design

            BoxCollider2D floorCol = floorObj.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(3.9f, 0.5f);
            floorCol.offset = new Vector2(0f, -0.05f);
            return floorObj;
        }

        private static GameObject CreateDefaultPetCage()
        {
            GameObject cageObj = new GameObject("Pet_Cage");
            cageObj.transform.localScale = new Vector3(0.70f, 0.70f, 1f); // Compact cage leaving ample screen breathing room
            BoxCollider2D cageCol = cageObj.AddComponent<BoxCollider2D>();
            cageCol.size = new Vector2(3.38f, 3.1f);
            cageCol.offset = new Vector2(0f, -0.25f);

            PetCage petCage = cageObj.AddComponent<PetCage>();
            petCage.SetupVisuals();
            petCage.EnsureDebrisSpritesLoaded();

            GameObject catObj = new GameObject("Cat_Character");
            catObj.transform.parent = petCage.PetSlot;
            catObj.transform.localPosition = Vector3.zero;
            catObj.transform.localRotation = Quaternion.identity;
            catObj.transform.localScale = new Vector3(0.90f, 0.90f, 1f); // Balanced, cute cat inside cage

            CatAnimationPlayer catPlayer = catObj.AddComponent<CatAnimationPlayer>();
            catPlayer.LoadFramesFromProject();

            return cageObj;
        }

        public static void AddStarToScene()
        {
            GameObject star = CreateStarInstance();
            Undo.RegisterCreatedObjectUndo(star, "Add Star Collectible");
            star.transform.position = SceneView.lastActiveSceneView != null 
                ? SceneView.lastActiveSceneView.camera.transform.position + new Vector3(0f, 0f, 10f)
                : Vector3.zero;
            Selection.activeGameObject = star;
        }

        public static void AddHangerToScene()
        {
            GameObject hangerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rope/Rope_Hanger.prefab");
            GameObject hanger = hangerPrefab != null 
                ? (GameObject)PrefabUtility.InstantiatePrefab(hangerPrefab)
                : CreateDefaultHanger();
            Undo.RegisterCreatedObjectUndo(hanger, "Add Rope Hanger");
            hanger.transform.position = new Vector3(0f, 3.5f, 0f);
            Selection.activeGameObject = hanger;
        }

        public static void AddPlatformToScene()
        {
            GameObject platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Ground_Platform.prefab");
            GameObject platform = platformPrefab != null 
                ? (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab)
                : CreateDefaultGroundPlatform();
            Undo.RegisterCreatedObjectUndo(platform, "Add Ground Platform");
            platform.transform.position = new Vector3(0f, -3.85f, 0f);
            Selection.activeGameObject = platform;
        }

        public static void AddSlopedSlideToScene()
        {
            GameObject slidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Obstacle_Sloped_Slide.prefab");
            GameObject slide = slidePrefab != null 
                ? (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab)
                : CreateDefaultSlopedSlide();
            Undo.RegisterCreatedObjectUndo(slide, "Add Sloped Slide Ramp");
            slide.transform.position = SceneView.lastActiveSceneView != null 
                ? SceneView.lastActiveSceneView.camera.transform.position + new Vector3(0f, 0f, 10f)
                : new Vector3(-0.95f, -2.75f, 0f);
            Selection.activeGameObject = slide;
        }

        private static GameObject CreateDefaultSlopedSlide()
        {
            GameObject obj = new GameObject("Obstacle_Sloped_Slide");
            obj.transform.localScale = new Vector3(0.68f, 0.68f, 1f);
            ObstacleSlopedSlide comp = obj.AddComponent<ObstacleSlopedSlide>();
            comp.Direction = SlopeDirection.LeftToRight;
            comp.ApplyConfiguration();
            return obj;
        }

        public static void AddHorizontalSupportPlankToScene()
        {
            GameObject plank = new GameObject("Obstacle_Horizontal_Plank");
            plank.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            SpriteRenderer sr = plank.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Obstacle_Slide_Ramp.png");
            sr.sortingOrder = 5;

            BoxCollider2D boxCol = plank.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(4.75f, 0.72f);
            boxCol.offset = new Vector2(0f, -0.02f);
            PhysicsMaterial2D slideMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/Smooth_Slide_Material.physicsMaterial2D");
            if (slideMat != null) boxCol.sharedMaterial = slideMat;

            Undo.RegisterCreatedObjectUndo(plank, "Add Horizontal Support Plank");
            plank.transform.position = SceneView.lastActiveSceneView != null 
                ? SceneView.lastActiveSceneView.camera.transform.position + new Vector3(0f, 0f, 10f)
                : new Vector3(0f, -2.0f, 0f);
            Selection.activeGameObject = plank;
        }

        [MenuItem("Cut Free Pets/Sync Architecture 2 Scenes in Build Settings", false, 30)]
        public static void SyncBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            // Always Home first (0), LevelSelect second (1), GameScene third (2)
            string home = "Assets/Scenes/HomeScreen.unity";
            if (File.Exists(home)) scenes.Add(new EditorBuildSettingsScene(home, true));

            string levelSelect = "Assets/Scenes/LevelSelect.unity";
            if (File.Exists(levelSelect)) scenes.Add(new EditorBuildSettingsScene(levelSelect, true));

            string gameScene = "Assets/Scenes/GameScene.unity";
            if (File.Exists(gameScene)) scenes.Add(new EditorBuildSettingsScene(gameScene, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"<color=lime>[LevelDesigner]</color> Synchronized {scenes.Count} core scenes into Unity Build Settings (Architecture 2)!");
        }
    }
}
#endif
