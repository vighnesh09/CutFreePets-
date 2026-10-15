#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.UI;
using CutFreePets.Gameplay;

namespace CutFreePets.Editor
{
    public static class HomeScreenSceneBuilder
    {
        [MenuItem("Cut Free Pets/Build Complete Game (Home, Level Select & Level 1)", false, 0)]
        public static void BuildCompleteGame()
        {
            // 1. Open SampleScene and build Level 1 with Rope, Cage, Cat, Stars, and Victory Popup UI
            string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(sampleScenePath))
            {
                var sampleScene = EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Single);
                RopeSceneSetupMenu.SetupHangingBoxDemoInternal(false);
                EditorSceneManager.SaveScene(sampleScene);
            }
            else
            {
                RopeSceneSetupMenu.SetupHangingBoxDemoInternal(false);
            }

            // 2. Open / Build LevelSelect scene
            LevelSelectSceneBuilder.BuildLevelSelectSceneInternal(false);

            // 3. Open / Build HomeScreen scene
            string homeScenePath = "Assets/Scenes/HomeScreen.unity";
            BuildHomeScreenSceneInternal(false);

            // 4. Ensure HomeScreen is the open active scene ready to play
            EditorSceneManager.OpenScene(homeScenePath, OpenSceneMode.Single);

            EditorUtility.DisplayDialog(
                "Cut Free Pets - Complete Game Built!",
                "All scenes and systems have been built and linked:\n\n" +
                "1. Home Screen (HomeScreen.unity):\n" +
                "   • Animated Cat sitting on Braided Pastel Mat\n" +
                "   • Big Play button ➔ opens Adventure Map Screen\n" +
                "   • Settings & Quit popups\n\n" +
                "2. Adventure Map (LevelSelect.unity):\n" +
                "   • Candy Crush-style winding forest trail (30 Levels)\n" +
                "   • Tactile wooden medallions with 3-star sockets\n" +
                "   • Pulsating golden aura on current playable level\n" +
                "   • Illuminated stepping stones along the trail\n" +
                "   • Floating focus button + smooth vertical scrolling\n\n" +
                "3. Level 1 (SampleScene.unity):\n" +
                "   • Rope, Hanging Cage & Rescued Cat\n" +
                "   • 3 Collectible Stars with physics detection\n" +
                "   • Shattering cage on landing\n" +
                "   • Level Complete Victory Popup (Stars, Next, Replay, Menu)\n\n" +
                "Press the PLAY button at the top of Unity to play now!",
                "Let's Play!"
            );
        }

        [MenuItem("Cut Free Pets/Build Home Screen Scene", false, 21)]
        public static void BuildHomeScreenScene()
        {
            BuildHomeScreenSceneInternal(true);
        }

        public static void BuildHomeScreenSceneInternal(bool showDialog)
        {
            // 1. Create or open HomeScreen.unity in Assets/Scenes
            string scenePath = "Assets/Scenes/HomeScreen.unity";
            UnityEngine.SceneManagement.Scene homeScene;

            if (File.Exists(scenePath))
            {
                homeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                homeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Clean scene
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                Object.DestroyImmediate(go);
            }

            // 2. Camera Setup for 9:16 Portrait
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.95f, 0.90f, 0.85f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camObj.AddComponent<AudioListener>();

            // 3. Screen Space Canvas
            GameObject canvasObj = new GameObject("Home_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Mobile Portrait 9:16 reference
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 4. EventSystem with InputSystemUIInputModule
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule != null) inputModule.AssignDefaultActions();

            // 5. Fullscreen Cozy Living Room Background (Clean floor with centered resting mat)
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/Home_Screen_BG_CleanFloor.png");
            if (bgSprite == null) bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/Home_Screen_BG.png");
            GameObject bgObj = new GameObject("Home_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = false;
            bgImg.raycastTarget = false;

            // 6. Header: Game Title Logo ("CUT FREE PETS")
            Sprite titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/UI_Game_Title_Logo.png");
            GameObject titleObj = new GameObject("Title_Logo");
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -80f);
            titleRect.sizeDelta = new Vector2(720f, 420f);
            Image titleImg = titleObj.AddComponent<Image>();
            titleImg.sprite = titleSprite;
            titleImg.preserveAspect = true;
            titleImg.raycastTarget = false;

            // 7. Total Stars Header Pill (Top Right)
            GameObject starsPillObj = new GameObject("Stars_Pill");
            starsPillObj.transform.SetParent(canvasObj.transform, false);
            RectTransform pillRect = starsPillObj.AddComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(1f, 1f);
            pillRect.anchorMax = new Vector2(1f, 1f);
            pillRect.pivot = new Vector2(1f, 1f);
            pillRect.anchoredPosition = new Vector2(-40f, -50f);
            pillRect.sizeDelta = new Vector2(200f, 70f);

            // Pill dark gold background
            Image pillImg = starsPillObj.AddComponent<Image>();
            pillImg.color = new Color(0.12f, 0.10f, 0.18f, 0.75f);

            // Mini star icon inside pill
            Sprite goldStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");
            GameObject miniStarObj = new GameObject("MiniStar");
            miniStarObj.transform.SetParent(starsPillObj.transform, false);
            RectTransform msRect = miniStarObj.AddComponent<RectTransform>();
            msRect.anchorMin = new Vector2(0f, 0.5f);
            msRect.anchorMax = new Vector2(0f, 0.5f);
            msRect.pivot = new Vector2(0.5f, 0.5f);
            msRect.anchoredPosition = new Vector2(40f, 0f);
            msRect.sizeDelta = new Vector2(48f, 48f);
            Image msImg = miniStarObj.AddComponent<Image>();
            msImg.sprite = goldStarSprite;
            msImg.preserveAspect = true;

            // Text counter inside pill
            GameObject starTextObj = new GameObject("Text_TotalStars");
            starTextObj.transform.SetParent(starsPillObj.transform, false);
            RectTransform txtRect = starTextObj.AddComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0f, 0f);
            txtRect.anchorMax = new Vector2(1f, 1f);
            txtRect.offsetMin = new Vector2(75f, 0f);
            txtRect.offsetMax = new Vector2(-15f, 0f);
            Text txtStars = starTextObj.AddComponent<Text>();
            txtStars.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtStars.fontSize = 34;
            txtStars.fontStyle = FontStyle.Bold;
            txtStars.color = new Color(1f, 0.88f, 0.35f);
            txtStars.alignment = TextAnchor.MiddleCenter;
            txtStars.text = "0";

            // 8. Cozy Braided Pastel Mat / Resting Platform (Positioned naturally on the room floor)
            Sprite pedestalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/Cat_Resting_Platform.png");
            GameObject pedestalObj = new GameObject("Cat_Resting_Platform");
            pedestalObj.transform.SetParent(canvasObj.transform, false);
            RectTransform pedRect = pedestalObj.AddComponent<RectTransform>();
            pedRect.anchorMin = new Vector2(0.5f, 0.5f);
            pedRect.anchorMax = new Vector2(0.5f, 0.5f);
            pedRect.pivot = new Vector2(0.5f, 0.5f);
            pedRect.anchoredPosition = new Vector2(0f, -370f);
            pedRect.sizeDelta = new Vector2(820f, 260f);
            Image pedImg = pedestalObj.AddComponent<Image>();
            pedImg.sprite = pedestalSprite;
            pedImg.preserveAspect = true;
            pedImg.raycastTarget = false;

            // 8b. Animated In-Game Cat Mascot (Standing firmly seated on the braided floor mat)
            GameObject catObj = new GameObject("Animated_Cat");
            catObj.transform.SetParent(canvasObj.transform, false);
            RectTransform catRect = catObj.AddComponent<RectTransform>();
            catRect.anchorMin = new Vector2(0.5f, 0.5f);
            catRect.anchorMax = new Vector2(0.5f, 0.5f);
            catRect.pivot = new Vector2(0.5f, 0.5f);
            catRect.anchoredPosition = new Vector2(0f, -100f);
            catRect.sizeDelta = new Vector2(540f, 540f);
            Image catImg = catObj.AddComponent<Image>();
            catImg.preserveAspect = true;
            catImg.raycastTarget = false;

            // Load all 60 frames matching the in-game rescued cat
            List<Sprite> happyFramesList = new List<Sprite>();
            for (int f = 0; f < 60; f++)
            {
                string framePath = $"Assets/Art/UI/Home/Cat_Showcase/showcase_{f:D2}.png";
                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                if (spr == null)
                {
                    // Fallback to original frames if showcase is missing
                    framePath = $"Assets/Art/cat/Frames_Happy/happy_{f:D2}.png";
                    spr = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                }
                if (spr != null) happyFramesList.Add(spr);
            }
            if (happyFramesList.Count > 0)
            {
                catImg.sprite = happyFramesList[0];
                catImg.color = Color.white;
            }

            // 9. Giant Glossy PLAY Button (Center-Bottom)
            Sprite playSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/UI_Btn_Play.png");
            GameObject playBtnObj = new GameObject("Btn_Play");
            playBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform playRect = playBtnObj.AddComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0f);
            playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.anchoredPosition = new Vector2(0f, 290f);
            playRect.sizeDelta = new Vector2(240f, 240f);
            Image playImg = playBtnObj.AddComponent<Image>();
            playImg.sprite = playSprite;
            playImg.preserveAspect = true;
            Button playBtn = playBtnObj.AddComponent<Button>();

            // 10. Bottom Row: Settings & Quit Buttons (Positioned cleanly under Play)
            Sprite settingsSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/UI_Btn_Settings.png");
            Sprite quitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Home/UI_Btn_Quit.png");

            // Settings Button (Bottom Left)
            GameObject settingsBtnObj = new GameObject("Btn_Settings");
            settingsBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform setRect = settingsBtnObj.AddComponent<RectTransform>();
            setRect.anchorMin = new Vector2(0.5f, 0f);
            setRect.anchorMax = new Vector2(0.5f, 0f);
            setRect.pivot = new Vector2(0.5f, 0.5f);
            setRect.anchoredPosition = new Vector2(-190f, 120f);
            setRect.sizeDelta = new Vector2(150f, 150f);
            Image setImg = settingsBtnObj.AddComponent<Image>();
            setImg.sprite = settingsSprite;
            setImg.preserveAspect = true;
            Button setBtn = settingsBtnObj.AddComponent<Button>();

            // Quit Button (Bottom Right)
            GameObject quitBtnObj = new GameObject("Btn_Quit");
            quitBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform quitRect = quitBtnObj.AddComponent<RectTransform>();
            quitRect.anchorMin = new Vector2(0.5f, 0f);
            quitRect.anchorMax = new Vector2(0.5f, 0f);
            quitRect.pivot = new Vector2(0.5f, 0.5f);
            quitRect.anchoredPosition = new Vector2(190f, 120f);
            quitRect.sizeDelta = new Vector2(150f, 150f);
            Image quitImg = quitBtnObj.AddComponent<Image>();
            quitImg.sprite = quitSprite;
            quitImg.preserveAspect = true;
            Button quitBtn = quitBtnObj.AddComponent<Button>();

            // 11. Settings Modal Dialog Panel
            GameObject settingsPanel = CreateSettingsModal(canvasObj.transform);
            Button closeSettingsBtn = settingsPanel.transform.Find("Board/Btn_Close")?.GetComponent<Button>();
            Button resetProgBtn = settingsPanel.transform.Find("Board/Btn_ResetProgress")?.GetComponent<Button>();

            // 12. Quit Confirmation Modal
            GameObject quitPanel = CreateQuitConfirmModal(canvasObj.transform);
            Button confirmQuitBtn = quitPanel.transform.Find("Board/Btn_Yes")?.GetComponent<Button>();
            Button cancelQuitBtn = quitPanel.transform.Find("Board/Btn_No")?.GetComponent<Button>();

            // 13. Add HomeScreenController Component & Wire
            HomeScreenController controller = canvasObj.AddComponent<HomeScreenController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("btnPlay").objectReferenceValue = playBtn;
            so.FindProperty("btnSettings").objectReferenceValue = setBtn;
            so.FindProperty("btnQuit").objectReferenceValue = quitBtn;
            so.FindProperty("titleLogoTransform").objectReferenceValue = titleRect;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("btnCloseSettings").objectReferenceValue = closeSettingsBtn;
            so.FindProperty("btnResetProgress").objectReferenceValue = resetProgBtn;
            so.FindProperty("quitConfirmPanel").objectReferenceValue = quitPanel;
            so.FindProperty("btnConfirmQuit").objectReferenceValue = confirmQuitBtn;
            so.FindProperty("btnCancelQuit").objectReferenceValue = cancelQuitBtn;
            so.FindProperty("catDisplayImage").objectReferenceValue = catImg;
            so.FindProperty("txtTotalStars").objectReferenceValue = txtStars;

            SerializedProperty catFramesProp = so.FindProperty("happyCatFrames");
            catFramesProp.arraySize = happyFramesList.Count;
            for (int i = 0; i < happyFramesList.Count; i++)
            {
                catFramesProp.GetArrayElementAtIndex(i).objectReferenceValue = happyFramesList[i];
            }
            so.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(homeScene, scenePath);

            // Update Build Settings so HomeScreen is scene 0 and SampleScene is scene 1
            UpdateBuildSettings(scenePath);

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Home Screen Created!",
                    "HomeScreen scene built successfully!\n\n" +
                    "• Index 0: Assets/Scenes/HomeScreen.unity (First to load)\n" +
                    "• Index 1: Assets/Scenes/SampleScene.unity (Level 1)\n" +
                    "• Play, Settings, Quit, and Animated Cat active!",
                    "Great!"
                );
            }
        }

        private static GameObject CreateSettingsModal(Transform parent)
        {
            GameObject modalObj = new GameObject("Settings_Modal");
            modalObj.transform.SetParent(parent, false);
            RectTransform mRect = modalObj.AddComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero;
            mRect.anchorMax = Vector2.one;
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;

            // Dim
            Image mDim = modalObj.AddComponent<Image>();
            mDim.color = new Color(0f, 0f, 0f, 0.72f);

            // Wooden Board
            Sprite boardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Win_Board.png");
            GameObject board = new GameObject("Board");
            board.transform.SetParent(modalObj.transform, false);
            RectTransform bRect = board.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.5f);
            bRect.anchorMax = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(680f, 480f);
            Image bImg = board.AddComponent<Image>();
            bImg.sprite = boardSprite;
            bImg.preserveAspect = true;

            // Settings Title Text
            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(board.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 85f);
            tRect.sizeDelta = new Vector2(400f, 60f);
            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 42;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.color = new Color(0.95f, 0.85f, 0.55f);
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.text = "SETTINGS";

            // Reset Progress Button
            GameObject resetBtnObj = new GameObject("Btn_ResetProgress");
            resetBtnObj.transform.SetParent(board.transform, false);
            RectTransform rRect = resetBtnObj.AddComponent<RectTransform>();
            rRect.anchoredPosition = new Vector2(0f, -10f);
            rRect.sizeDelta = new Vector2(340f, 75f);
            Image rImg = resetBtnObj.AddComponent<Image>();
            rImg.color = new Color(0.85f, 0.35f, 0.25f, 0.95f);
            resetBtnObj.AddComponent<Button>();

            GameObject rTxtObj = new GameObject("Text");
            rTxtObj.transform.SetParent(resetBtnObj.transform, false);
            RectTransform rtRect = rTxtObj.AddComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            Text rtTxt = rTxtObj.AddComponent<Text>();
            rtTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rtTxt.fontSize = 28;
            rtTxt.fontStyle = FontStyle.Bold;
            rtTxt.color = Color.white;
            rtTxt.alignment = TextAnchor.MiddleCenter;
            rtTxt.text = "RESET PROGRESS";

            // Close Button
            Sprite replaySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Btn_Replay.png");
            GameObject closeBtnObj = new GameObject("Btn_Close");
            closeBtnObj.transform.SetParent(board.transform, false);
            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(0f, -115f);
            cRect.sizeDelta = new Vector2(100f, 100f);
            Image cImg = closeBtnObj.AddComponent<Image>();
            cImg.sprite = replaySprite;
            cImg.preserveAspect = true;
            closeBtnObj.AddComponent<Button>();

            modalObj.SetActive(false);
            return modalObj;
        }

        private static GameObject CreateQuitConfirmModal(Transform parent)
        {
            GameObject modalObj = new GameObject("Quit_Modal");
            modalObj.transform.SetParent(parent, false);
            RectTransform mRect = modalObj.AddComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero;
            mRect.anchorMax = Vector2.one;
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;

            Image mDim = modalObj.AddComponent<Image>();
            mDim.color = new Color(0f, 0f, 0f, 0.72f);

            Sprite boardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Win_Board.png");
            GameObject board = new GameObject("Board");
            board.transform.SetParent(modalObj.transform, false);
            RectTransform bRect = board.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.5f);
            bRect.anchorMax = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(680f, 480f);
            Image bImg = board.AddComponent<Image>();
            bImg.sprite = boardSprite;
            bImg.preserveAspect = true;

            GameObject titleObj = new GameObject("Text_Title");
            titleObj.transform.SetParent(board.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 65f);
            tRect.sizeDelta = new Vector2(500f, 60f);
            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 38;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.color = new Color(0.95f, 0.85f, 0.55f);
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.text = "QUIT GAME?";

            // Yes Button
            GameObject yesBtnObj = new GameObject("Btn_Yes");
            yesBtnObj.transform.SetParent(board.transform, false);
            RectTransform yRect = yesBtnObj.AddComponent<RectTransform>();
            yRect.anchoredPosition = new Vector2(-120f, -70f);
            yRect.sizeDelta = new Vector2(170f, 75f);
            Image yImg = yesBtnObj.AddComponent<Image>();
            yImg.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            yesBtnObj.AddComponent<Button>();

            GameObject yTxtObj = new GameObject("Text");
            yTxtObj.transform.SetParent(yesBtnObj.transform, false);
            RectTransform ytRect = yTxtObj.AddComponent<RectTransform>();
            ytRect.anchorMin = Vector2.zero;
            ytRect.anchorMax = Vector2.one;
            Text ytTxt = yTxtObj.AddComponent<Text>();
            ytTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ytTxt.fontSize = 30;
            ytTxt.fontStyle = FontStyle.Bold;
            ytTxt.color = Color.white;
            ytTxt.alignment = TextAnchor.MiddleCenter;
            ytTxt.text = "YES";

            // No Button
            GameObject noBtnObj = new GameObject("Btn_No");
            noBtnObj.transform.SetParent(board.transform, false);
            RectTransform nRect = noBtnObj.AddComponent<RectTransform>();
            nRect.anchoredPosition = new Vector2(120f, -70f);
            nRect.sizeDelta = new Vector2(170f, 75f);
            Image nImg = noBtnObj.AddComponent<Image>();
            nImg.color = new Color(0.25f, 0.75f, 0.35f, 0.95f);
            noBtnObj.AddComponent<Button>();

            GameObject nTxtObj = new GameObject("Text");
            nTxtObj.transform.SetParent(noBtnObj.transform, false);
            RectTransform ntRect = nTxtObj.AddComponent<RectTransform>();
            ntRect.anchorMin = Vector2.zero;
            ntRect.anchorMax = Vector2.one;
            Text ntTxt = nTxtObj.AddComponent<Text>();
            ntTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ntTxt.fontSize = 30;
            ntTxt.fontStyle = FontStyle.Bold;
            ntTxt.color = Color.white;
            ntTxt.alignment = TextAnchor.MiddleCenter;
            ntTxt.text = "NO";

            modalObj.SetActive(false);
            return modalObj;
        }

        public static void UpdateBuildSettings(string homeScenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(homeScenePath, true)); // Index 0: HomeScreen

            string levelSelectPath = "Assets/Scenes/LevelSelect.unity";
            if (File.Exists(levelSelectPath))
            {
                scenes.Add(new EditorBuildSettingsScene(levelSelectPath, true)); // Index 1: LevelSelect
            }

            string sampleScenePath = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(sampleScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(sampleScenePath, true)); // Index 2: SampleScene (Level 1)
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("<color=cyan>[HomeScreenSceneBuilder]</color> Build Settings updated with HomeScreen (0), LevelSelect (1), and SampleScene (2).");
        }
    }
}
#endif
