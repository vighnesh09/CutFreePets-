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

        [MenuItem("Cut Free Pets/Open Scene/3. Level 1 (Completed Gameplay)", false, 12)]
        public static void OpenLevel1Scene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level_1.unity", OpenSceneMode.Single);
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
                "Design levels in seconds! Create new level scenes, connect ropes between hangers and cages, or drop prefabs with a single click.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);
            GUILayout.Label("1. Create New Level Scene", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            levelNumberToCreate = EditorGUILayout.IntField("Level Number", levelNumberToCreate);
            if (GUILayout.Button("Create Level", GUILayout.Width(110)))
            {
                CreateNewLevelScene(levelNumberToCreate);
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

            EditorGUILayout.Space(15);
            GUILayout.Label("4. Clean & Optimize Level 1 Scene", EditorStyles.boldLabel);
            if (GUILayout.Button("✨ Rebuild Level 1 from Modular Prefabs", GUILayout.Height(30)))
            {
                RebuildLevel1Clean();
            }

            EditorGUILayout.Space(15);
            GUILayout.Label("5. Build Settings Sync", EditorStyles.boldLabel);
            if (GUILayout.Button("🔄 Sync All Levels into Build Settings", GUILayout.Height(30)))
            {
                SyncBuildSettings();
            }
        }

        [MenuItem("Cut Free Pets/Rebuild Level 1 from Modular Prefabs", false, 3)]
        public static void RebuildLevel1Clean()
        {
            string targetPath = "Assets/Scenes/Level_1.unity";

            // Open or create Level_1.unity
            var scene = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);

            // Clean old objects
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                Undo.DestroyObjectImmediate(go);
            }

            // 1. Mobile Portrait Camera (9:16)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            cam.transform.position = new Vector3(0f, 0.6f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.20f);
            camObj.AddComponent<AudioListener>();

            // 2. Global 2D Light
            GameObject lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            if (light2D != null)
            {
                light2D.lightType = Light2D.LightType.Global;
                light2D.intensity = 1.0f;
            }

            // 3. Level 01 Elements Root
            GameObject levelRoot = new GameObject("Level_01_Root");

            // 4. Ground Platform Prefab Instance
            GameObject platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Ground_Platform.prefab");
            GameObject platformObj = platformPrefab != null 
                ? (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab)
                : CreateDefaultGroundPlatform();
            platformObj.name = "Ground_Platform";
            platformObj.transform.parent = levelRoot.transform;
            platformObj.transform.position = new Vector3(0f, -3.85f, 0f);

            // 5. Rope Hanger Peg Prefab Instance
            GameObject hangerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rope/Rope_Hanger.prefab");
            GameObject hangerObj = hangerPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(hangerPrefab)
                : CreateDefaultHanger();
            hangerObj.name = "TopMiddle_Hanger";
            hangerObj.transform.parent = levelRoot.transform;
            hangerObj.transform.position = new Vector3(0f, 4.5f, 0f);
            RopeHanger hangerComp = hangerObj.GetComponent<RopeHanger>() ?? hangerObj.AddComponent<RopeHanger>();

            // 6. Pet Cage Prefab Instance
            GameObject cagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Pet_Cage.prefab");
            GameObject cageObj = cagePrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(cagePrefab)
                : CreateDefaultPetCage();
            cageObj.name = "Pet_Cage";
            cageObj.transform.parent = levelRoot.transform;
            cageObj.transform.position = new Vector3(0f, 2.4f, 0f);
            PetCage petCageComp = cageObj.GetComponent<PetCage>() ?? cageObj.AddComponent<PetCage>();

            // 7. Rope connection
            ConnectRope(hangerComp, petCageComp);

            // 8. 3 Collectible Star Prefab Instances
            Vector3[] starPositions = new Vector3[] {
                new Vector3(0f, 0.10f, 0f),
                new Vector3(0f, -1.15f, 0f),
                new Vector3(0f, -2.40f, 0f)
            };

            for (int s = 0; s < 3; s++)
            {
                GameObject starObj = CreateStarInstance();
                starObj.name = $"Star_Collectible_0{s + 1}";
                starObj.transform.parent = levelRoot.transform;
                starObj.transform.position = starPositions[s];
            }

            // 9. Swipe Blade
            GameObject bladeObj = new GameObject("SwipeCutBlade");
            bladeObj.transform.parent = levelRoot.transform;
            bladeObj.AddComponent<SwipeCutBlade>();

            // 10. LevelScoreTracker
            GameObject trackerObj = new GameObject("LevelScoreTracker");
            trackerObj.transform.parent = levelRoot.transform;
            LevelScoreTracker tracker = trackerObj.AddComponent<LevelScoreTracker>();
            tracker.SetCurrentLevel(1);

            // 11. Victory Popup Canvas (Prefab Instance)
            VictoryPopupBuilder.CreateOrUpdateVictoryPopup(levelRoot.transform);

            EditorSceneManager.SaveScene(scene, targetPath);
            SyncBuildSettings();

            EditorUtility.DisplayDialog(
                "Level 1 Clean & Optimized",
                "Level_1.unity has been cleanly rebuilt using linked Prefab instances!\n\n" +
                "• All elements linked to Assets/Prefabs/\n" +
                "• Completely modular and lightweight\n" +
                "• Changing any prefab updates Level 1 instantly!",
                "Great!"
            );
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

        public static void CreateNewLevelScene(int levelIndex)
        {
            string targetPath = $"Assets/Scenes/Level_{levelIndex}.unity";

            if (File.Exists(targetPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Level Scene Exists",
                    $"Level scene '{targetPath}' already exists. Open it instead?",
                    "Open Scene",
                    "Cancel"
                );
                if (overwrite)
                {
                    EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
                }
                return;
            }

            // Create a clean new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Mobile Portrait Camera (9:16)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            cam.transform.position = new Vector3(0f, 0.6f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.16f, 0.20f);
            camObj.AddComponent<AudioListener>();

            // 2. Light for 2D URP
            GameObject lightObj = new GameObject("Global 2D Light");
            var light2D = lightObj.AddComponent<Light2D>();
            if (light2D != null)
            {
                light2D.lightType = Light2D.LightType.Global;
                light2D.intensity = 1.0f;
            }

            // 3. Level Elements Root
            GameObject levelRoot = new GameObject($"Level_{levelIndex:D2}_Root");

            // 4. Ground Platform (Prefab or Component)
            GameObject platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Ground_Platform.prefab");
            GameObject platformObj = platformPrefab != null 
                ? (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab)
                : CreateDefaultGroundPlatform();
            platformObj.name = "Ground_Platform";
            platformObj.transform.parent = levelRoot.transform;
            platformObj.transform.position = new Vector3(0f, -3.85f, 0f);

            // 5. Rope Hanger Peg
            GameObject hangerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rope/Rope_Hanger.prefab");
            GameObject hangerObj = hangerPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(hangerPrefab)
                : CreateDefaultHanger();
            hangerObj.name = "TopMiddle_Hanger";
            hangerObj.transform.parent = levelRoot.transform;
            hangerObj.transform.position = new Vector3(0f, 4.5f, 0f);
            RopeHanger hangerComp = hangerObj.GetComponent<RopeHanger>() ?? hangerObj.AddComponent<RopeHanger>();

            // 6. Pet Cage with Cat
            GameObject cagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Pet_Cage.prefab");
            GameObject cageObj = cagePrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(cagePrefab)
                : CreateDefaultPetCage();
            cageObj.name = "Pet_Cage";
            cageObj.transform.parent = levelRoot.transform;
            cageObj.transform.position = new Vector3(0f, 2.4f, 0f);
            PetCage petCageComp = cageObj.GetComponent<PetCage>() ?? cageObj.AddComponent<PetCage>();

            // 7. Connect Default Rope
            ConnectRope(hangerComp, petCageComp);

            // 8. 3 Collectible Stars with perfect spacing
            Vector3[] starPositions = new Vector3[] {
                new Vector3(0f, 0.10f, 0f),
                new Vector3(0f, -1.15f, 0f),
                new Vector3(0f, -2.40f, 0f)
            };

            for (int s = 0; s < 3; s++)
            {
                GameObject starObj = CreateStarInstance();
                starObj.name = $"Star_Collectible_0{s + 1}";
                starObj.transform.parent = levelRoot.transform;
                starObj.transform.position = starPositions[s];
            }

            // 9. Swipe Cut Blade
            GameObject bladeObj = new GameObject("SwipeCutBlade");
            bladeObj.transform.parent = levelRoot.transform;
            bladeObj.AddComponent<SwipeCutBlade>();

            // 10. LevelScoreTracker configured for this level index
            GameObject trackerObj = new GameObject("LevelScoreTracker");
            trackerObj.transform.parent = levelRoot.transform;
            LevelScoreTracker tracker = trackerObj.AddComponent<LevelScoreTracker>();
            tracker.SetCurrentLevel(levelIndex);

            // 11. Victory Popup Canvas
            VictoryPopupBuilder.CreateOrUpdateVictoryPopup(levelRoot.transform);

            // Save and sync scene
            EditorSceneManager.SaveScene(newScene, targetPath);
            SyncBuildSettings();

            EditorUtility.DisplayDialog(
                "Level Created!",
                $"Successfully created and configured '{targetPath}'!\n\n" +
                "• All prefabs and mechanics connected\n" +
                "• Added automatically to Build Settings\n" +
                "• Ready for instant play or repositioning!",
                "Awesome"
            );
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
            floorObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            BoxCollider2D floorCol = floorObj.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(3.9f, 0.5f);
            floorCol.offset = new Vector2(0f, -0.05f);
            return floorObj;
        }

        private static GameObject CreateDefaultPetCage()
        {
            GameObject cageObj = new GameObject("Pet_Cage");
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
            catObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

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

        [MenuItem("Cut Free Pets/Sync All Level Scenes in Build Settings", false, 30)]
        public static void SyncBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            // Always Home first (0), LevelSelect second (1)
            string home = "Assets/Scenes/HomeScreen.unity";
            if (File.Exists(home)) scenes.Add(new EditorBuildSettingsScene(home, true));

            string levelSelect = "Assets/Scenes/LevelSelect.unity";
            if (File.Exists(levelSelect)) scenes.Add(new EditorBuildSettingsScene(levelSelect, true));

            // Level 1 / SampleScene
            string sampleScene = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(sampleScene)) scenes.Add(new EditorBuildSettingsScene(sampleScene, true));

            // Discover all Level_*.unity scenes and add them in order
            for (int i = 1; i <= 30; i++)
            {
                string path = $"Assets/Scenes/Level_{i}.unity";
                if (File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"<color=lime>[LevelDesigner]</color> Synchronized {scenes.Count} scenes into Unity Build Settings!");
        }
    }
}
#endif
