using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CutFreePets.Gameplay;

namespace CutFreePets.Editor
{
    [InitializeOnLoad]
    public static class Level3ObstacleBuilder
    {
        private const string SESSION_KEY = "Level3Obstacle_Applied_v1";

        static Level3ObstacleBuilder()
        {
            EditorApplication.delayCall += ApplyObstacleSetup;
        }

        [MenuItem("Tools/Rebuild Level 3 Obstacle Ramp")]
        public static void ForceApply()
        {
            SessionState.SetBool(SESSION_KEY, false);
            ApplyObstacleSetup();
        }

        private static void ApplyObstacleSetup()
        {
            if (BuildPipeline.isBuildingPlayer) return;
            if (SessionState.GetBool(SESSION_KEY, false)) return;
            SessionState.SetBool(SESSION_KEY, true);

            string scenePath = "Assets/Scenes/Level_3.unity";
            var currentScene = EditorSceneManager.GetActiveScene();
            bool needToReopenOrig = false;
            string originalScenePath = currentScene.path;

            if (currentScene.path != scenePath)
            {
                needToReopenOrig = true;
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Remove any legacy wedge ramp
            GameObject oldWedge = GameObject.Find("Obstacle_Sloped_Slide");
            if (oldWedge != null)
            {
                Object.DestroyImmediate(oldWedge);
            }

            // Find or create Obstacle_Hinged_Ramp
            GameObject rampObj = GameObject.Find("Obstacle_Hinged_Ramp") 
                               ?? GameObject.Find("Obstacle_Slide_Ramp") 
                               ?? GameObject.Find("Sloped_Ramp");

            if (rampObj == null)
            {
                rampObj = new GameObject("Obstacle_Hinged_Ramp");
                GameObject envRoot = GameObject.Find("Environment_Root");
                if (envRoot != null) rampObj.transform.parent = envRoot.transform;
            }
            rampObj.name = "Obstacle_Hinged_Ramp";

            // Position hinged ramp so its left hinge is anchored at (-2.55, -2.10)
            // When tilted at the standard -25 deg, its tip smoothly feeds the cage onto Ground_Platform
            rampObj.transform.localPosition = new Vector3(-2.55f, -2.10f, 0f);
            rampObj.transform.localEulerAngles = Vector3.zero;
            rampObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            // Configure HingedTiltRamp component with standard properties
            System.Type rampType = System.Type.GetType("CutFreePets.Gameplay.HingedTiltRamp, Assembly-CSharp");
            if (rampType != null)
            {
                Component hingedComp = rampObj.GetComponent(rampType);
                if (hingedComp == null) hingedComp = rampObj.AddComponent(rampType);

                rampType.GetProperty("MaxTiltAngle")?.SetValue(hingedComp, -25f);
                rampType.GetProperty("TiltSpeed")?.SetValue(hingedComp, 48f);

                rampType.GetMethod("EnsureComponents")?.Invoke(hingedComp, null);
                rampType.GetMethod("ResetRamp")?.Invoke(hingedComp, null);
            }

            // Ensure Pet_Cage position, upright stability & smooth sliding collider
            GameObject cageObj = GameObject.Find("Pet_Cage");
            if (cageObj != null)
            {
                cageObj.transform.localPosition = new Vector3(-1.15f, 2.35f, 0f);

                Rigidbody2D cageRb = cageObj.GetComponent<Rigidbody2D>();
                if (cageRb != null)
                {
                    cageRb.angularDamping = 2.0f;
                    cageRb.linearDamping = 0.2f;
                }

                BoxCollider2D cageCol = cageObj.GetComponent<BoxCollider2D>();
                if (cageCol != null)
                {
                    cageCol.edgeRadius = 0.08f;
                    PhysicsMaterial2D slideMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/Smooth_Slide_Material.physicsMaterial2D");
                    if (slideMat != null) cageCol.sharedMaterial = slideMat;
                }
            }

            // Position ceiling hanger
            GameObject hangerObj = GameObject.Find("TopMiddle_Hanger");
            if (hangerObj != null)
            {
                hangerObj.transform.localPosition = new Vector3(-1.15f, 5.80f, 0f);
            }

            // Ensure Ground_Platform is at (1.35, -3.85)
            GameObject platformObj = GameObject.Find("Ground_Platform");
            if (platformObj != null)
            {
                platformObj.transform.localPosition = new Vector3(1.35f, -3.85f, 0f);
            }

            // Align 3 stars along the drop, slide, and landing path
            GameObject star1 = GameObject.Find("Star_Collectible_01");
            if (star1 != null) star1.transform.localPosition = new Vector3(-1.15f, 0.15f, 0f);

            GameObject star2 = GameObject.Find("Star_Collectible_02");
            if (star2 != null) star2.transform.localPosition = new Vector3(-0.10f, -2.10f, 0f);

            GameObject star3 = GameObject.Find("Star_Collectible_03");
            if (star3 != null) star3.transform.localPosition = new Vector3(1.35f, -2.85f, 0f);

            // Connect to prefab
            string prefabPath = "Assets/Prefabs/Gameplay/Obstacle_Hinged_Ramp.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(rampObj, prefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[Level3ObstacleBuilder]</color> Obstacle_Hinged_Ramp successfully created, configured, and saved in Level 3!");

            if (needToReopenOrig && !string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }
    }
}
