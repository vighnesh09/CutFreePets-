#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.UI;

namespace CutFreePets.Editor
{
    public static class LevelSelectSceneBuilder
    {
        [MenuItem("Cut Free Pets/Build Level Select Scene", false, 23)]
        public static void BuildLevelSelectScene()
        {
            BuildLevelSelectSceneInternal(true);
        }

        public static void BuildLevelSelectSceneInternal(bool showDialog)
        {
            string scenePath = "Assets/Scenes/LevelSelect.unity";
            UnityEngine.SceneManagement.Scene selectScene;

            if (File.Exists(scenePath))
            {
                selectScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                selectScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Clean existing scene
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(go);
            }

            // 1. Camera Setup
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.91f, 0.86f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camObj.AddComponent<AudioListener>();

            // 2. Canvas Setup (Screen Space Overlay, 1080x1920 reference)
            GameObject canvasObj = new GameObject("LevelSelect_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 3. EventSystem with New Input System module
            var existingEventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (inputModule != null) inputModule.AssignDefaultActions();
            }

            // 4. Background Image
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/LevelSelect_BG.png");
            if (bgSprite == null)
            {
                bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/Home_Screen_BG.png");
            }

            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;

            // 5. Header Bar (Top) - Hanging Wooden Plaque area
            GameObject headerObj = new GameObject("Header_Bar");
            headerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, 0f);
            headerRect.sizeDelta = new Vector2(0f, 420f);

            // Back Button (Top Left) - Smooth carved circular button
            Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Btn_Back.png");
            GameObject backBtnObj = new GameObject("Btn_Back");
            backBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform backRect = backBtnObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 1f);
            backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(40f, -40f);
            backRect.sizeDelta = new Vector2(130f, 130f);

            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.sprite = backSprite;
            backImg.preserveAspect = true;
            Button backBtn = backBtnObj.AddComponent<Button>();

            // Title Text ("SELECT LEVEL") - Centered right on the hanging wooden plank
            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, -270f);
            titleRect.sizeDelta = new Vector2(580f, 100f);

            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 58;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.36f, 0.19f, 0.06f); // Warm carved dark oak
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "SELECT LEVEL";

            // Total Stars Badge (Top Right)
            GameObject starBadgeObj = new GameObject("Badge_TotalStars");
            starBadgeObj.transform.SetParent(headerObj.transform, false);
            RectTransform sbRect = starBadgeObj.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 1f);
            sbRect.anchorMax = new Vector2(1f, 1f);
            sbRect.pivot = new Vector2(1f, 1f);
            sbRect.anchoredPosition = new Vector2(-40f, -50f);
            sbRect.sizeDelta = new Vector2(250f, 85f);

            Image sbBg = starBadgeObj.AddComponent<Image>();
            sbBg.color = new Color(0.22f, 0.13f, 0.06f, 0.65f); // Rich warm dark chocolate pill

            // Star icon in badge
            Sprite starFilledSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");
            GameObject sIconObj = new GameObject("Icon_Star");
            sIconObj.transform.SetParent(starBadgeObj.transform, false);
            RectTransform siRect = sIconObj.AddComponent<RectTransform>();
            siRect.anchorMin = new Vector2(0f, 0.5f);
            siRect.anchorMax = new Vector2(0f, 0.5f);
            siRect.pivot = new Vector2(0f, 0.5f);
            siRect.anchoredPosition = new Vector2(18f, 0f);
            siRect.sizeDelta = new Vector2(62f, 62f);
            Image siImg = sIconObj.AddComponent<Image>();
            siImg.sprite = starFilledSprite;
            siImg.preserveAspect = true;

            // Total Stars Text
            GameObject starTxtObj = new GameObject("Text_Count");
            starTxtObj.transform.SetParent(starBadgeObj.transform, false);
            RectTransform stRect = starTxtObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0f);
            stRect.anchorMax = new Vector2(1f, 1f);
            stRect.offsetMin = new Vector2(85f, 0f);
            stRect.offsetMax = new Vector2(-15f, 0f);

            Text stTxt = starTxtObj.AddComponent<Text>();
            stTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stTxt.fontSize = 34;
            stTxt.fontStyle = FontStyle.Bold;
            stTxt.color = new Color(1f, 0.94f, 0.48f); // Bright warm honey gold
            stTxt.alignment = TextAnchor.MiddleCenter;
            stTxt.text = "0 / 60";

            // 6. Scrollable Level Cards Grid Area (Perfect fit inside the central cardboard box)
            GameObject scrollObj = new GameObject("Scroll_View");
            scrollObj.transform.SetParent(canvasObj.transform, false);
            RectTransform scRect = scrollObj.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 0.5f);
            scRect.anchorMax = new Vector2(0.5f, 0.5f);
            scRect.pivot = new Vector2(0.5f, 0.5f);
            scRect.anchoredPosition = new Vector2(0f, -220f);
            scRect.sizeDelta = new Vector2(740f, 1160f);

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.scrollSensitivity = 30f;

            // Viewport
            GameObject viewObj = new GameObject("Viewport");
            viewObj.transform.SetParent(scrollObj.transform, false);
            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            vRect.pivot = new Vector2(0f, 1f);
            Image vImg = viewObj.AddComponent<Image>();
            vImg.color = Color.white;
            Mask mask = viewObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Grid Content
            GameObject contentObj = new GameObject("Grid_Content");
            contentObj.transform.SetParent(viewObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0f, 1600f);

            GridLayoutGroup grid = contentObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(210f, 210f);
            grid.spacing = new Vector2(25f, 32f);
            grid.padding = new RectOffset(15, 15, 20, 30);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3; // 3 columns cleanly centered inside the cardboard box

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vRect;
            scroll.content = cRect;

            // 7. Wire LevelSelectController
            LevelSelectController controller = canvasObj.AddComponent<LevelSelectController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("btnBack").objectReferenceValue = backBtn;
            so.FindProperty("txtTotalStars").objectReferenceValue = stTxt;
            so.FindProperty("headerPanel").objectReferenceValue = headerRect;
            so.FindProperty("gridContainer").objectReferenceValue = contentObj.transform;

            Sprite starEmptySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Star_Empty.png");
            Sprite cardWoodSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/LevelCard_Wood.png");
            Sprite lockIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Level_Lock_Icon.png");

            so.FindProperty("starFilledSprite").objectReferenceValue = starFilledSprite;
            so.FindProperty("starEmptySprite").objectReferenceValue = starEmptySprite;
            so.FindProperty("cardWoodSprite").objectReferenceValue = cardWoodSprite;
            so.FindProperty("lockIconSprite").objectReferenceValue = lockIconSprite;
            so.ApplyModifiedProperties();

            // Populate the grid preview in editor
            controller.PopulateGrid();
            controller.UpdateTotalStars();

            // Save Scene
            EditorSceneManager.SaveScene(selectScene, scenePath);

            // Update Build Settings to include LevelSelect at index 1
            HomeScreenSceneBuilder.UpdateBuildSettings("Assets/Scenes/HomeScreen.unity");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Level Select Screen Created!",
                    "LevelSelect.unity built successfully!\n\n" +
                    "• 20 Level Wooden Cards with star sockets\n" +
                    "• Golden Padlock overlay for locked levels\n" +
                    "• Realtime player stars progress from SaveManager\n" +
                    "• Smooth scrolling grid + back button to Home Screen",
                    "Awesome!"
                );
            }
        }
    }
}
#endif
