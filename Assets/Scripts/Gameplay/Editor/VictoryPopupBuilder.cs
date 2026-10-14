#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CutFreePets.Gameplay;
using CutFreePets.UI;

namespace CutFreePets.Editor
{
    public static class VictoryPopupBuilder
    {
        [MenuItem("Cut Free Pets/Build Victory Popup UI", false, 22)]
        public static void MenuBuildVictoryPopup()
        {
            CreateOrUpdateVictoryPopup(null);
            EditorUtility.DisplayDialog("Victory Popup UI", "Victory Popup UI successfully created/updated in the scene!", "OK");
        }

        public static GameObject CreateOrUpdateVictoryPopup(Transform parent = null)
        {
            // Check if VictoryCanvas already exists in scene
            GameObject existingCanvas = GameObject.Find("VictoryCanvas");
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(existingCanvas);
            }

            // 1. Create Screen Space Overlay Canvas
            GameObject canvasObj = new GameObject("VictoryCanvas");
            if (parent != null) canvasObj.transform.parent = parent;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Above all world sprites

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Mobile Portrait 9:16 reference
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists in scene with the correct input module
            var existingEventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // Use InputSystemUIInputModule for New Input System compatibility
                var inputModule = eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    inputModule.AssignDefaultActions();
                }
            }
            else
            {
                // Ensure existing EventSystem has the New Input System module
                var legacyModule = existingEventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (legacyModule != null)
                {
                    Object.DestroyImmediate(legacyModule);
                    var newModule = existingEventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    if (newModule != null) newModule.AssignDefaultActions();
                }
            }

            // 2. Victory Popup Root Panel
            GameObject popupRoot = new GameObject("VictoryPopup_Root");
            popupRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform rootRect = popupRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // 3. Background Overlay (Set to clear so game remains crystal clear with no blue/dark screen)
            GameObject dimObj = new GameObject("Background_Dim");
            dimObj.transform.SetParent(popupRoot.transform, false);
            RectTransform dimRect = dimObj.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            Image dimImg = dimObj.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0f);
            dimImg.raycastTarget = false;

            // 4. Wooden Victory Board (Symmetrically trimmed to exact 629x442, scaled nicely for 1080x1920)
            Sprite winBoardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Win_Board.png");
            GameObject boardObj = new GameObject("Win_Board");
            boardObj.transform.SetParent(popupRoot.transform, false);
            RectTransform boardRect = boardObj.AddComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardRect.pivot = new Vector2(0.5f, 0.5f);
            boardRect.anchoredPosition = new Vector2(0f, 40f);
            boardRect.sizeDelta = new Vector2(740f, 520f); // Exactly matches the 629:442 aspect ratio

            Image boardImg = boardObj.AddComponent<Image>();
            if (winBoardSprite != null)
            {
                boardImg.sprite = winBoardSprite;
                boardImg.type = Image.Type.Simple;
                boardImg.preserveAspect = true;
            }

            // 5. Stars Container - Centered and evenly spaced across the plate
            // Center star at X = 0, Left star at X = -160, Right star at X = 160
            // Vertically placed at Y = 28 (upper inner area of the plate)
            Sprite emptyStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Star_Empty.png");
            Sprite goldStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/Star_Collectible.png");

            Vector2[] starPos = new Vector2[] {
                new Vector2(-160f, 20f),
                new Vector2(0f, 38f),
                new Vector2(160f, 20f)
            };
            float[] starRot = new float[] { 6f, 0f, -6f }; // Gentle natural arch tilt
            float[] starSizes = new float[] { 130f, 150f, 130f }; // Center star prominent

            Image[] emptyStarSockets = new Image[3];
            Image[] goldenStars = new Image[3];

            for (int i = 0; i < 3; i++)
            {
                // Socket
                GameObject socketObj = new GameObject($"StarSocket_{i + 1}");
                socketObj.transform.SetParent(boardObj.transform, false);
                RectTransform sRect = socketObj.AddComponent<RectTransform>();
                sRect.anchoredPosition = starPos[i];
                sRect.localRotation = Quaternion.Euler(0f, 0f, starRot[i]);
                sRect.sizeDelta = new Vector2(starSizes[i], starSizes[i]);

                Image sImg = socketObj.AddComponent<Image>();
                sImg.sprite = emptyStarSprite;
                sImg.preserveAspect = true;
                emptyStarSockets[i] = sImg;

                // Golden Star Overlay inside socket
                GameObject goldObj = new GameObject($"GoldenStar_{i + 1}");
                goldObj.transform.SetParent(socketObj.transform, false);
                RectTransform gRect = goldObj.AddComponent<RectTransform>();
                gRect.anchorMin = Vector2.zero;
                gRect.anchorMax = Vector2.one;
                gRect.offsetMin = Vector2.zero;
                gRect.offsetMax = Vector2.zero;
                gRect.localRotation = Quaternion.identity;

                Image gImg = goldObj.AddComponent<Image>();
                gImg.sprite = goldStarSprite;
                gImg.preserveAspect = true;
                goldenStars[i] = gImg;
            }

            // 6. Action Buttons Container - Centered and evenly spaced across the bottom area of the plate
            Sprite btnReplaySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Btn_Replay.png");
            Sprite btnNextSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Btn_Next.png");
            Sprite btnMenuSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/UI_Btn_Menu.png");

            // Replay Button (Left, balanced at X = -160, Y = -120)
            GameObject replayBtnObj = new GameObject("Btn_Replay");
            replayBtnObj.transform.SetParent(boardObj.transform, false);
            RectTransform repRect = replayBtnObj.AddComponent<RectTransform>();
            repRect.anchoredPosition = new Vector2(-160f, -120f);
            repRect.sizeDelta = new Vector2(105f, 105f);
            Image repImg = replayBtnObj.AddComponent<Image>();
            repImg.sprite = btnReplaySprite;
            repImg.preserveAspect = true;
            Button repBtn = replayBtnObj.AddComponent<Button>();

            // Next Level Button (Center, prominent at X = 0, Y = -118)
            GameObject nextBtnObj = new GameObject("Btn_Next");
            nextBtnObj.transform.SetParent(boardObj.transform, false);
            RectTransform nextRect = nextBtnObj.AddComponent<RectTransform>();
            nextRect.anchoredPosition = new Vector2(0f, -118f);
            nextRect.sizeDelta = new Vector2(150f, 118f);
            Image nextImg = nextBtnObj.AddComponent<Image>();
            nextImg.sprite = btnNextSprite;
            nextImg.preserveAspect = true;
            Button nextBtn = nextBtnObj.AddComponent<Button>();

            // Menu Button (Right, balanced at X = 160, Y = -120)
            GameObject menuBtnObj = new GameObject("Btn_Menu");
            menuBtnObj.transform.SetParent(boardObj.transform, false);
            RectTransform menuRect = menuBtnObj.AddComponent<RectTransform>();
            menuRect.anchoredPosition = new Vector2(160f, -120f);
            menuRect.sizeDelta = new Vector2(105f, 105f);
            Image menuImg = menuBtnObj.AddComponent<Image>();
            menuImg.sprite = btnMenuSprite;
            menuImg.preserveAspect = true;
            Button menuBtn = menuBtnObj.AddComponent<Button>();

            // 7. Add & Wire VictoryPopupUI Component
            VictoryPopupUI popupUI = canvasObj.AddComponent<VictoryPopupUI>();

            // Wire serialized fields via SerializedObject for robust editor serialization
            SerializedObject so = new SerializedObject(popupUI);
            so.FindProperty("popupRoot").objectReferenceValue = popupRoot;
            so.FindProperty("backgroundDim").objectReferenceValue = dimImg;
            so.FindProperty("boardTransform").objectReferenceValue = boardRect;

            SerializedProperty socketsProp = so.FindProperty("emptyStarSockets");
            socketsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                socketsProp.GetArrayElementAtIndex(i).objectReferenceValue = emptyStarSockets[i];
            }

            SerializedProperty goldProp = so.FindProperty("goldenStars");
            goldProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                goldProp.GetArrayElementAtIndex(i).objectReferenceValue = goldenStars[i];
            }

            so.FindProperty("btnNext").objectReferenceValue = nextBtn;
            so.FindProperty("btnReplay").objectReferenceValue = repBtn;
            so.FindProperty("btnMenu").objectReferenceValue = menuBtn;
            so.ApplyModifiedProperties();

            // Hide popup root initially so it only shows upon winning
            popupRoot.SetActive(false);

            EditorUtility.SetDirty(canvasObj);
            return canvasObj;
        }
    }
}
#endif
