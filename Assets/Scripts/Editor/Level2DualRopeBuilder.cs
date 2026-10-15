using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CutFreePets.Gameplay;
using CutFreePets.Rope;
using CutFreePets.Cut;

namespace CutFreePets.Editor
{
    /// <summary>
    /// Builder tool to author and assemble Level 2 according to the dual-rope V-suspension prototype:
    /// - Two symmetrical ceiling hangers (TopLeft_Hanger & TopRight_Hanger)
    /// - Two VerletRopes forming a V-shaped suspension holding Pet_Cage in the center
    /// - Simultaneous cut drops cage straight down through 3 vertical stars onto Ground_Platform
    /// - Single rope cut causes a pendulum swing to the side (and restarts if out of screen)
    /// - Level 3 contains the hinged ramp slide puzzle
    /// </summary>
    [InitializeOnLoad]
    public static class Level2DualRopeBuilder
    {
        private const string SESSION_KEY = "Level2DualRope_Applied_v3";

        static Level2DualRopeBuilder()
        {
            EditorApplication.delayCall += ApplyLevel2Setup;
        }

        [MenuItem("Tools/Build Level 2 (Dual Rope Pendulum)")]
        public static void ForceApply()
        {
            SessionState.SetBool(SESSION_KEY, false);
            ApplyLevel2Setup();
        }

        private static void ApplyLevel2Setup()
        {
            if (SessionState.GetBool(SESSION_KEY, false)) return;
            SessionState.SetBool(SESSION_KEY, true);

            string scenePath = "Assets/Scenes/Level_2.unity";
            string templateScenePath = "Assets/Scenes/Level_1.unity";

            // If Level_2.unity doesn't exist yet, copy Level_1.unity as a solid foundation
            if (!System.IO.File.Exists(scenePath))
            {
                AssetDatabase.CopyAsset(templateScenePath, scenePath);
                AssetDatabase.Refresh();
            }

            var currentScene = EditorSceneManager.GetActiveScene();
            bool needToReopenOrig = false;
            string originalScenePath = currentScene.path;

            if (currentScene.path != scenePath)
            {
                needToReopenOrig = true;
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Find Environment_Root
            GameObject envRoot = GameObject.Find("Environment_Root") ?? GameObject.Find("Level_01_Root");
            if (envRoot != null) envRoot.name = "Environment_Root";

            // 1. Setup Ground_Platform at (0, -3.85, 0)
            GameObject platformObj = GameObject.Find("Ground_Platform");
            if (platformObj != null)
            {
                platformObj.transform.localPosition = new Vector3(0f, -3.85f, 0f);
            }

            // 2. Setup Pet_Cage a little higher in the center at (0, 2.35, 0)
            GameObject cageObj = GameObject.Find("Pet_Cage");
            if (cageObj != null)
            {
                cageObj.transform.localPosition = new Vector3(0f, 2.35f, 0f);
                cageObj.transform.localRotation = Quaternion.identity;

                // Ensure no stale DistanceJoint2D components are serialized on the cage
                var staleJoints = cageObj.GetComponents<DistanceJoint2D>();
                foreach (var j in staleJoints)
                {
                    Object.DestroyImmediate(j);
                }

                // Ensure smooth slide material and anti-tumble damping
                Rigidbody2D cageRb = cageObj.GetComponent<Rigidbody2D>();
                if (cageRb != null)
                {
                    cageRb.angularDamping = 2.5f;
                    cageRb.linearDamping = 0.22f;
                }
            }

            Transform cageHook = cageObj != null ? cageObj.transform.Find("TopRing_Hook") : null;
            if (cageHook == null && cageObj != null)
            {
                GameObject hookObj = new GameObject("TopRing_Hook");
                hookObj.transform.parent = cageObj.transform;
                hookObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                hookObj.transform.localRotation = Quaternion.identity;
                cageHook = hookObj.transform;
            }

            // 3. Remove old single Rope and old hangers
            GameObject oldRope = GameObject.Find("Rope");
            if (oldRope != null) Object.DestroyImmediate(oldRope);

            GameObject oldHanger = GameObject.Find("TopMiddle_Hanger");

            // 4. Create or setup TopLeft_Hanger and TopRight_Hanger completely OUT OF SCREEN
            // Camera orthographic size is 5 (top edge is y = 5.0). Placing hangers at y = 5.65f guarantees
            // that the top starting points of both ropes are cleanly outside the screen.
            GameObject hangerLeft = GameObject.Find("TopLeft_Hanger");
            if (hangerLeft == null)
            {
                if (oldHanger != null)
                {
                    hangerLeft = Object.Instantiate(oldHanger, envRoot != null ? envRoot.transform : null);
                    hangerLeft.name = "TopLeft_Hanger";
                }
                else
                {
                    hangerLeft = new GameObject("TopLeft_Hanger");
                    if (envRoot != null) hangerLeft.transform.parent = envRoot.transform;
                }
            }
            hangerLeft.transform.localPosition = new Vector3(-2.70f, 5.65f, 0f);
            hangerLeft.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            GameObject hangerRight = GameObject.Find("TopRight_Hanger");
            if (hangerRight == null)
            {
                if (oldHanger != null)
                {
                    hangerRight = Object.Instantiate(oldHanger, envRoot != null ? envRoot.transform : null);
                    hangerRight.name = "TopRight_Hanger";
                }
                else
                {
                    hangerRight = new GameObject("TopRight_Hanger");
                    if (envRoot != null) hangerRight.transform.parent = envRoot.transform;
                }
            }
            hangerRight.transform.localPosition = new Vector3(2.70f, 5.65f, 0f);
            hangerRight.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            if (oldHanger != null) Object.DestroyImmediate(oldHanger);

            // 5. Create Rope_Left and Rope_Right
            RopeConfig config = AssetDatabase.LoadAssetAtPath<RopeConfig>("Assets/Settings/DefaultRopeConfig.asset");

            GameObject ropeLeftObj = GameObject.Find("Rope_Left");
            if (ropeLeftObj != null) Object.DestroyImmediate(ropeLeftObj);
            ropeLeftObj = new GameObject("Rope_Left");
            if (envRoot != null) ropeLeftObj.transform.parent = envRoot.transform;
            ropeLeftObj.transform.position = hangerLeft.transform.position;

            VerletRope ropeLeft = ropeLeftObj.AddComponent<VerletRope>();
            ropeLeft.Setup(hangerLeft.transform, cageHook != null ? cageHook : cageObj.transform, config);

            GameObject ropeRightObj = GameObject.Find("Rope_Right");
            if (ropeRightObj != null) Object.DestroyImmediate(ropeRightObj);
            ropeRightObj = new GameObject("Rope_Right");
            if (envRoot != null) ropeRightObj.transform.parent = envRoot.transform;
            ropeRightObj.transform.position = hangerRight.transform.position;

            VerletRope ropeRight = ropeRightObj.AddComponent<VerletRope>();
            ropeRight.Setup(hangerRight.transform, cageHook != null ? cageHook : cageObj.transform, config);

            // Connect ropes to PetCage
            PetCage petCage = cageObj != null ? cageObj.GetComponent<PetCage>() : null;
            if (petCage != null)
            {
                petCage.AttachRope(ropeLeft);
                petCage.AttachRope(ropeRight);
            }

            // 6. Setup 3 Stars vertically aligned down the center between cage and platform
            GameObject star1 = GameObject.Find("Star_Collectible_01");
            if (star1 != null) star1.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            GameObject star2 = GameObject.Find("Star_Collectible_02");
            if (star2 != null) star2.transform.localPosition = new Vector3(0f, -0.55f, 0f);

            GameObject star3 = GameObject.Find("Star_Collectible_03");
            if (star3 != null) star3.transform.localPosition = new Vector3(0f, -2.05f, 0f);

            // 7. Configure LevelScoreTracker to level 2
            LevelScoreTracker tracker = Object.FindAnyObjectByType<LevelScoreTracker>();
            if (tracker != null)
            {
                tracker.SetCurrentLevel(2);
            }

            // 8. Update UI text to "Level 2" if present
            UnityEngine.UI.Text[] texts = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include);
            foreach (var t in texts)
            {
                if (t != null && t.text.Contains("Level 1"))
                {
                    t.text = t.text.Replace("Level 1", "Level 2");
                }
            }

            // 9. Save scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Level2DualRopeBuilder]</color> Level 2 (Dual Rope Pendulum) successfully built and saved!");

            // 10. Update Unity Build Settings to include Level 1, Level 2, Level 3
            SyncBuildSettings();

            if (needToReopenOrig && !string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        public static void SyncBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            string home = "Assets/Scenes/HomeScreen.unity";
            if (System.IO.File.Exists(home)) scenes.Add(new EditorBuildSettingsScene(home, true));

            string select = "Assets/Scenes/LevelSelect.unity";
            if (System.IO.File.Exists(select)) scenes.Add(new EditorBuildSettingsScene(select, true));

            string l1 = "Assets/Scenes/Level_1.unity";
            if (System.IO.File.Exists(l1)) scenes.Add(new EditorBuildSettingsScene(l1, true));

            string l2 = "Assets/Scenes/Level_2.unity";
            if (System.IO.File.Exists(l2)) scenes.Add(new EditorBuildSettingsScene(l2, true));

            string l3 = "Assets/Scenes/Level_3.unity";
            if (System.IO.File.Exists(l3)) scenes.Add(new EditorBuildSettingsScene(l3, true));

            string sample = "Assets/Scenes/SampleScene.unity";
            if (System.IO.File.Exists(sample)) scenes.Add(new EditorBuildSettingsScene(sample, true));

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"<color=lime>[Level2DualRopeBuilder]</color> Build Settings synchronized with {scenes.Count} scenes!");
        }
    }
}
