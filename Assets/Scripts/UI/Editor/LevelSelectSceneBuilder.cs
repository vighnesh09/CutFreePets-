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
            cam.backgroundColor = new Color(0.24f, 0.44f, 0.22f); // Deep forest green backdrop
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

            // 4. Scrollable Adventure Map View (Full screen vertical scroll behind fixed header)
            GameObject scrollObj = new GameObject("AdventureMap_ScrollView");
            scrollObj.transform.SetParent(canvasObj.transform, false);
            RectTransform scRect = scrollObj.AddComponent<RectTransform>();
            scRect.anchorMin = Vector2.zero;
            scRect.anchorMax = Vector2.one;
            scRect.sizeDelta = Vector2.zero;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 35f;

            // Viewport
            GameObject viewObj = new GameObject("Viewport");
            viewObj.transform.SetParent(scrollObj.transform, false);
            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            vRect.pivot = new Vector2(0.5f, 0.5f);
            Image vImg = viewObj.AddComponent<Image>();
            vImg.color = Color.white;
            Mask mask = viewObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Map Content Container (Houses background, path, milestones, and nodes)
            GameObject contentObj = new GameObject("Map_Content");
            contentObj.transform.SetParent(viewObj.transform, false);
            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0f);
            cRect.anchorMax = new Vector2(0.5f, 0f);
            cRect.pivot = new Vector2(0.5f, 0f);
            cRect.sizeDelta = new Vector2(1080f, 6800f);

            // Rich vertical forest background image
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/AdventureMap_BG.png");
            GameObject bgObj = new GameObject("Adventure_Background");
            bgObj.transform.SetParent(contentObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;

            // Path Stepping Stones Container
            GameObject pathContainerObj = new GameObject("Path_SteppingStones");
            pathContainerObj.transform.SetParent(contentObj.transform, false);
            RectTransform pathRect = pathContainerObj.AddComponent<RectTransform>();
            pathRect.anchorMin = new Vector2(0.5f, 0f);
            pathRect.anchorMax = new Vector2(0.5f, 0f);
            pathRect.pivot = new Vector2(0.5f, 0f);
            pathRect.anchoredPosition = Vector2.zero;
            pathRect.sizeDelta = cRect.sizeDelta;

            // Nodes Container
            GameObject nodesContainerObj = new GameObject("Level_Nodes");
            nodesContainerObj.transform.SetParent(contentObj.transform, false);
            RectTransform nodesRect = nodesContainerObj.AddComponent<RectTransform>();
            nodesRect.anchorMin = new Vector2(0.5f, 0f);
            nodesRect.anchorMax = new Vector2(0.5f, 0f);
            nodesRect.pivot = new Vector2(0.5f, 0f);
            nodesRect.anchoredPosition = Vector2.zero;
            nodesRect.sizeDelta = cRect.sizeDelta;

            scroll.viewport = vRect;
            scroll.content = cRect;

            // 5. Fixed Compact Header Bar (Remains anchored at top while map scrolls)
            GameObject headerObj = new GameObject("Header_Bar");
            headerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, 0f);
            headerRect.sizeDelta = new Vector2(0f, 170f);

            // Header Background banner (Translucent cozy dark wood overlay)
            Image headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.16f, 0.28f, 0.14f, 0.88f); // Soft forest canopy tint

            // Back Button (Top Left)
            Sprite backSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Btn_Back.png");
            GameObject backBtnObj = new GameObject("Btn_Back");
            backBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform backRect = backBtnObj.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0.5f);
            backRect.anchorMax = new Vector2(0f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.anchoredPosition = new Vector2(35f, 0f);
            backRect.sizeDelta = new Vector2(110f, 110f);

            Image backImg = backBtnObj.AddComponent<Image>();
            backImg.sprite = backSprite;
            backImg.preserveAspect = true;
            Button backBtn = backBtnObj.AddComponent<Button>();

            // Title: "ADVENTURE MAP"
            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(480f, 80f);

            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 44;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(1f, 0.94f, 0.82f); // Warm creamy white
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "ADVENTURE MAP";

            // Total Stars Badge (Top Right)
            GameObject starBadgeObj = new GameObject("Badge_TotalStars");
            starBadgeObj.transform.SetParent(headerObj.transform, false);
            RectTransform sbRect = starBadgeObj.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 0.5f);
            sbRect.anchorMax = new Vector2(1f, 0.5f);
            sbRect.pivot = new Vector2(1f, 0.5f);
            sbRect.anchoredPosition = new Vector2(-35f, 0f);
            sbRect.sizeDelta = new Vector2(230f, 75f);

            Image sbBg = starBadgeObj.AddComponent<Image>();
            sbBg.color = new Color(0.12f, 0.20f, 0.10f, 0.90f);

            Sprite starFilledSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");
            GameObject sIconObj = new GameObject("Icon_Star");
            sIconObj.transform.SetParent(starBadgeObj.transform, false);
            RectTransform siRect = sIconObj.AddComponent<RectTransform>();
            siRect.anchorMin = new Vector2(0f, 0.5f);
            siRect.anchorMax = new Vector2(0f, 0.5f);
            siRect.pivot = new Vector2(0f, 0.5f);
            siRect.anchoredPosition = new Vector2(14f, 0f);
            siRect.sizeDelta = new Vector2(50f, 50f);
            Image siImg = sIconObj.AddComponent<Image>();
            siImg.sprite = starFilledSprite;
            siImg.preserveAspect = true;

            GameObject starTxtObj = new GameObject("Text_Count");
            starTxtObj.transform.SetParent(starBadgeObj.transform, false);
            RectTransform stRect = starTxtObj.AddComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = new Vector2(70f, 0f);
            stRect.offsetMax = new Vector2(-12f, 0f);

            Text stTxt = starTxtObj.AddComponent<Text>();
            stTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stTxt.fontSize = 30;
            stTxt.fontStyle = FontStyle.Bold;
            stTxt.color = new Color(1f, 0.92f, 0.45f);
            stTxt.alignment = TextAnchor.MiddleCenter;
            stTxt.text = "0 / 90";

            // 6. Floating Compass / Focus Current Level Button (Bottom Right)
            GameObject focusBtnObj = new GameObject("Btn_FocusCurrent");
            focusBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform fRect = focusBtnObj.AddComponent<RectTransform>();
            fRect.anchorMin = new Vector2(1f, 0f);
            fRect.anchorMax = new Vector2(1f, 0f);
            fRect.pivot = new Vector2(1f, 0f);
            fRect.anchoredPosition = new Vector2(-40f, 50f);
            fRect.sizeDelta = new Vector2(130f, 130f);

            Image fImg = focusBtnObj.AddComponent<Image>();
            fImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Node_Wood_Unlocked.png");
            fImg.preserveAspect = true;
            Button focusBtn = focusBtnObj.AddComponent<Button>();

            // Small indicator inside focus button
            GameObject fTxtObj = new GameObject("Text_Icon");
            fTxtObj.transform.SetParent(focusBtnObj.transform, false);
            RectTransform ftRect = fTxtObj.AddComponent<RectTransform>();
            ftRect.anchorMin = Vector2.zero;
            ftRect.anchorMax = Vector2.one;
            Text ftTxt = fTxtObj.AddComponent<Text>();
            ftTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ftTxt.fontSize = 44;
            ftTxt.fontStyle = FontStyle.Bold;
            ftTxt.color = new Color(0.32f, 0.18f, 0.05f);
            ftTxt.alignment = TextAnchor.MiddleCenter;
            ftTxt.text = "🎯";

            // 7. Toast Message Panel (for locked levels feedback)
            GameObject toastObj = new GameObject("Toast_LockedMessage");
            toastObj.transform.SetParent(canvasObj.transform, false);
            RectTransform toastRect = toastObj.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0.5f);
            toastRect.anchorMax = new Vector2(0.5f, 0.5f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.anchoredPosition = new Vector2(0f, -80f);
            toastRect.sizeDelta = new Vector2(760f, 130f);

            Image toastImg = toastObj.AddComponent<Image>();
            toastImg.color = new Color(0.18f, 0.12f, 0.06f, 0.94f); // Rich warm wooden plaque tone

            GameObject tTxtObj = new GameObject("Text_Message");
            tTxtObj.transform.SetParent(toastObj.transform, false);
            RectTransform ttRect = tTxtObj.AddComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.offsetMin = new Vector2(25f, 10f);
            ttRect.offsetMax = new Vector2(-25f, -10f);

            Text tTxt = tTxtObj.AddComponent<Text>();
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 28;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.color = new Color(1f, 0.92f, 0.70f);
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.text = "Complete the previous level to unlock this adventure!";

            // 8. Wire AdventureMapController
            AdventureMapController controller = canvasObj.AddComponent<AdventureMapController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("btnBack").objectReferenceValue = backBtn;
            so.FindProperty("txtTitle").objectReferenceValue = titleTxt;
            so.FindProperty("txtTotalStars").objectReferenceValue = stTxt;
            so.FindProperty("btnFocusCurrent").objectReferenceValue = focusBtn;
            so.FindProperty("scrollRect").objectReferenceValue = scroll;
            so.FindProperty("mapContent").objectReferenceValue = cRect;
            so.FindProperty("pathContainer").objectReferenceValue = pathContainerObj.transform;
            so.FindProperty("nodesContainer").objectReferenceValue = nodesContainerObj.transform;
            so.FindProperty("toastPanel").objectReferenceValue = toastObj;
            so.FindProperty("txtToastMessage").objectReferenceValue = tTxt;

            so.FindProperty("nodeUnlockedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Node_Wood_Unlocked.png");
            so.FindProperty("nodeLockedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Node_Wood_Locked.png");
            so.FindProperty("starFilledSprite").objectReferenceValue = starFilledSprite;
            so.FindProperty("starEmptySprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Star_Empty.png");
            so.FindProperty("lockIconSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Level_Lock_Icon.png");
            so.FindProperty("steppingStoneSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Path_SteppingStone.png");
            so.FindProperty("currentGlowSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Node_Current_Glow.png");
            so.ApplyModifiedProperties();

            // Populate Map Preview in editor
            controller.BuildAdventureMap();
            controller.UpdateTotalStarsDisplay();

            // Save Scene
            EditorSceneManager.SaveScene(selectScene, scenePath);

            // Update Build Settings
            HomeScreenSceneBuilder.UpdateBuildSettings("Assets/Scenes/HomeScreen.unity");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Adventure Map Created!",
                    "Winding Candy Crush-style Forest Adventure Map built successfully!\n\n" +
                    "• Organic winding path across 30 levels\n" +
                    "• Golden radiant pulse on current playable level\n" +
                    "• Stepping stones connecting consecutive nodes\n" +
                    "• Fixed compact header with 'ADVENTURE MAP' title\n" +
                    "• Floating focus compass button + smooth vertical scrolling",
                    "Awesome!"
                );
            }
        }
    }
}
#endif
