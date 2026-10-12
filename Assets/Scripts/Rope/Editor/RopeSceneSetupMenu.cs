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
        [MenuItem("Tools/Cut Free Pets/Setup Clean 1-Rope 1-Box Demo", false, 1)]
        [MenuItem("Cut Free Pets/Setup Clean 1-Rope 1-Box Demo", false, 1)]
        public static void SetupHangingBoxDemo()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Clean 1-Rope 1-Box Demo");
            int group = Undo.GetCurrentGroup();

            // 1. Clean up ALL previous old demo roots and loose objects in scene
            string[] oldNames = new string[] { 
                "HangingBox_Level", "CutTheRope_Level", "RopeDemo_Setup", "RopeDemo",
                "Peg_TopAnchor", "Obstacle_Peg", "Pet_Target", "Peg_Top", "Weight_Bottom"
            };
            foreach (string name in oldNames)
            {
                GameObject old = GameObject.Find(name);
                if (old != null) Undo.DestroyObjectImmediate(old);
            }

            // Clean up any other objects having RopeHanger or VerletRope
            foreach (var h in Object.FindObjectsByType<RopeHanger>())
            {
                Undo.DestroyObjectImmediate(h.gameObject);
            }
            foreach (var r in Object.FindObjectsByType<VerletRope>())
            {
                Undo.DestroyObjectImmediate(r.gameObject);
            }

            // 2. Config & Braided Material
            RopeConfig config = GetOrCreateRopeConfig();
            Material ropeMat = GetOrCreateBraidedMaterial();

            // 3. Camera setup in Portrait 2D
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
            cam.orthographicSize = 4.2f;
            cam.transform.position = new Vector3(0f, 1.2f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.backgroundColor = new Color(0.18f, 0.16f, 0.23f); // Clean cozy dark purple
            cam.clearFlags = CameraClearFlags.SolidColor;

            // 4. Single Root Container
            GameObject root = new GameObject("HangingBox_Level");
            Undo.RegisterCreatedObjectUndo(root, "Create Root");

            // 5. ONE Top Middle Hanger Peg
            GameObject hangerObj = new GameObject("TopMiddle_Hanger");
            hangerObj.transform.parent = root.transform;
            hangerObj.transform.position = new Vector3(0f, 3.6f, 0f);
            RopeHanger hanger = hangerObj.AddComponent<RopeHanger>();
            hanger.SetupVisuals();

            // 6. ONE Lower Hanging Box (Square)
            GameObject boxObj = new GameObject("Hanging_Box");
            boxObj.transform.parent = root.transform;
            boxObj.transform.position = new Vector3(0f, 0.5f, 0f);

            BoxCollider2D boxCol = boxObj.AddComponent<BoxCollider2D>();
            boxCol.size = new Vector2(0.85f, 0.85f);

            HangingBox hangingBox = boxObj.AddComponent<HangingBox>();
            boxObj.AddComponent<DraggableTarget>(); // Allows dragging box around

            // 7. Ground Floor Platform below for landing
            GameObject floorObj = new GameObject("Ground_Platform");
            floorObj.transform.parent = root.transform;
            floorObj.transform.position = new Vector3(0f, -2.5f, 0f);

            BoxCollider2D floorCol = floorObj.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(5.5f, 0.5f);

            SpriteRenderer floorRend = floorObj.AddComponent<SpriteRenderer>();
            floorRend.color = new Color(0.28f, 0.35f, 0.45f);
            floorRend.sortingOrder = 3;

            Texture2D floorTex = new Texture2D(16, 16);
            Color[] cols = new Color[256];
            for (int i = 0; i < 256; i++) cols[i] = Color.white;
            floorTex.SetPixels(cols);
            floorTex.Apply();
            floorRend.sprite = Sprite.Create(floorTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            floorObj.transform.localScale = new Vector3(5.5f, 0.5f, 1f);

            // 8. ONE Braided Rope Connecting Hanger -> Box
            GameObject ropeObj = new GameObject("Rope");
            ropeObj.transform.parent = root.transform;
            ropeObj.transform.position = hangerObj.transform.position;

            VerletRope rope = ropeObj.AddComponent<VerletRope>();
            rope.Setup(hangerObj.transform, boxObj.transform, config);

            // Link rope to HangingBox so it drops when cut
            hangingBox.AttachRope(rope);

            // 9. Swipe Cut Blade (Pure touch/mouse swipe, zero scissors)
            GameObject bladeObj = new GameObject("SwipeCutBlade");
            bladeObj.transform.parent = root.transform;
            bladeObj.AddComponent<SwipeCutBlade>();

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = ropeObj;

            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            EditorUtility.DisplayDialog(
                "Clean Setup Complete!",
                "Clean 1-Rope, 1-Box setup created!\n\n" +
                "• Top Middle: 1 RopeHanger peg\n" +
                "• Middle: 1 Braided Rope\n" +
                "• Bottom: 1 HangingBox (square)\n" +
                "• Direct touch/mouse swipe slicing active!",
                "Great!"
            );
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
                config.segmentCount = 20;
                config.baseWidth = 0.12f;
                config.elasticity = 0.015f;
                config.enableCollision = true;
                config.slackMultiplier = 1.05f;
                config.normalColor = new Color(0.95f, 0.8f, 0.6f);
                config.highTensionColor = new Color(1f, 0.38f, 0.28f);
                config.minStretchWidthFactor = 0.55f;
                config.splineSubdivisions = 5;

                AssetDatabase.CreateAsset(config, assetPath);
                AssetDatabase.SaveAssets();
            }

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
