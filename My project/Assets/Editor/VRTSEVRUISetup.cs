using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRTSE.Navigation;
using VRTSE.Tour;
using VRTSE.UI;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSEVRUISetup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string LogPrefix = "[VR TSE VR UI Setup]";
        private const string SetupCompletedKey = "VRTSE.VRUIUpgradeCompleted.v1";

        static VRTSEVRUISetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                EditorApplication.delayCall += RunAutomaticSetup;
        }

        private static void RunAutomaticSetup()
        {
            if (EditorPrefs.GetBool(SetupCompletedKey, false))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += RunAutomaticSetup;
                return;
            }

            RunSetup();
        }

        [MenuItem("Tools/VR TSE/Upgrade VR User Interface")]
        public static void RunSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"{LogPrefix} Wait for Unity to finish compiling or leave Play Mode.");
                return;
            }

            try
            {
                Scene scene = SceneManager.GetSceneByPath(ScenePath);
                bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
                if (openedForSetup)
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

                try
                {
                    ConfigureScene(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    AssetDatabase.SaveAssets();
                }
                finally
                {
                    if (openedForSetup && scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }

                Debug.Log($"{LogPrefix} Readable panels, quick controls, and first-use help are ready.");
                EditorPrefs.SetBool(SetupCompletedKey, true);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Setup failed: {exception}");
                throw;
            }
        }

        private static void ConfigureScene(Scene scene)
        {
            GameObject worldUI = Require(scene, "WorldUI");
            GameObject locationPanel = Require(scene, "CurrentLocationPanel");
            GameObject mapPanel = Require(scene, "FloorMapPanel");
            GameObject destinationPanel = Require(scene, "RoomMenuPanel");
            GameObject navigationPanel = Require(scene, "NavigationPanel");
            GameObject cameraObject = Require(scene, "Main Camera");
            GameObject tourObject = Require(scene, "TourManager");
            GameObject navigationObject = Require(scene, "NavigationManager");

            Camera worldCamera = cameraObject.GetComponent<Camera>();
            FloorUIController floorUI = locationPanel.GetComponent<FloorUIController>();
            TourManager tourManager = tourObject.GetComponent<TourManager>();
            NavigationManager navigationManager = navigationObject.GetComponent<NavigationManager>();
            if (worldCamera == null || floorUI == null || tourManager == null || navigationManager == null)
                throw new InvalidOperationException("A required camera or controller component is missing.");

            ImproveExistingPanels(locationPanel, mapPanel, destinationPanel, navigationPanel);

            Button mapButton;
            Button destinationsButton;
            Button homeButton;
            Button helpButton;
            GameObject quickMenu = CreateQuickMenu(
                worldUI.transform,
                worldCamera,
                out mapButton,
                out destinationsButton,
                out homeButton,
                out helpButton);

            Button dismissTutorialButton;
            GameObject tutorial = CreateTutorial(
                worldUI.transform,
                worldCamera,
                out dismissTutorialButton);

            Button room301Button = Require(scene, "Room301Button").GetComponent<Button>();
            Button room302Button = Require(scene, "Room302Button").GetComponent<Button>();
            Button cancelButton = Require(scene, "CancelButton").GetComponent<Button>();

            VRMenuController menuController = GetOrAdd<VRMenuController>(worldUI);
            menuController.Configure(
                tourManager,
                navigationManager,
                mapPanel,
                destinationPanel,
                tutorial,
                mapButton,
                destinationsButton,
                homeButton,
                helpButton,
                dismissTutorialButton,
                room301Button,
                room302Button,
                cancelButton);

            floorUI.ConfigureQuickMenu(quickMenu.transform, tutorial.transform);
            EditorUtility.SetDirty(menuController);
            EditorUtility.SetDirty(floorUI);
        }

        private static void ImproveExistingPanels(
            GameObject locationPanel,
            GameObject mapPanel,
            GameObject destinationPanel,
            GameObject navigationPanel)
        {
            RectTransform locationCanvas = FindRect(locationPanel.transform, "CurrentLocationCanvas");
            locationCanvas.sizeDelta = new Vector2(760f, 132f);
            locationCanvas.localScale = Vector3.one * 0.0013f;
            StyleText(locationCanvas, "LocationText", 32, FontStyle.Bold);

            RectTransform mapCanvas = FindRect(mapPanel.transform, "FloorMapCanvas");
            mapCanvas.sizeDelta = new Vector2(440f, 330f);
            mapCanvas.localScale = Vector3.one * 0.0013f;
            StyleText(mapCanvas, "Title", 27, FontStyle.Bold);

            RectTransform destinationCanvas = FindRect(destinationPanel.transform, "DestinationMenuCanvas");
            destinationCanvas.sizeDelta = new Vector2(520f, 390f);
            destinationCanvas.localScale = Vector3.one * 0.00125f;
            Text destinationTitle = StyleText(destinationCanvas, "Title", 30, FontStyle.Bold);
            SetRect(destinationTitle.rectTransform, new Vector2(460f, 52f), new Vector2(0f, 155f));
            StyleExistingButton(destinationCanvas, "Room301Button", "ROOM 301", new Vector2(420f, 72f), new Vector2(0f, 66f));
            StyleExistingButton(destinationCanvas, "Room302Button", "ROOM 302", new Vector2(420f, 72f), new Vector2(0f, -24f));
            StyleExistingButton(destinationCanvas, "CancelButton", "CANCEL ROUTE", new Vector2(340f, 62f), new Vector2(0f, -125f));

            RectTransform navigationCanvas = FindRect(navigationPanel.transform, "NavigationMessageCanvas");
            navigationCanvas.sizeDelta = new Vector2(760f, 108f);
            navigationCanvas.localScale = Vector3.one * 0.00125f;
            StyleText(navigationCanvas, "NavigationMessageText", 27, FontStyle.Bold);
        }

        private static GameObject CreateQuickMenu(
            Transform parent,
            Camera worldCamera,
            out Button mapButton,
            out Button destinationsButton,
            out Button homeButton,
            out Button helpButton)
        {
            GameObject panel = FindOrCreateChild(parent, "QuickMenuPanel", typeof(RectTransform));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(860f, 112f);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.0012f;

            ConfigureCanvas(panel, worldCamera, 70);
            Image background = CreateImage(panel.transform, "Background", new Color(0.012f, 0.03f, 0.055f, 0.94f), false);
            Stretch(background.rectTransform);

            mapButton = CreateButton(panel.transform, "MapToggleButton", "MAP", new Vector2(-315f, 0f), new Vector2(180f, 68f), new Color(0.04f, 0.42f, 0.72f, 1f));
            destinationsButton = CreateButton(panel.transform, "DestinationsToggleButton", "DESTINATIONS", new Vector2(-105f, 0f), new Vector2(220f, 68f), new Color(0.04f, 0.42f, 0.72f, 1f));
            homeButton = CreateButton(panel.transform, "HomeButton", "HOME", new Vector2(120f, 0f), new Vector2(180f, 68f), new Color(0.08f, 0.55f, 0.42f, 1f));
            helpButton = CreateButton(panel.transform, "HelpButton", "HELP", new Vector2(320f, 0f), new Vector2(160f, 68f), new Color(0.32f, 0.30f, 0.62f, 1f));
            return panel;
        }

        private static GameObject CreateTutorial(
            Transform parent,
            Camera worldCamera,
            out Button dismissButton)
        {
            GameObject panel = FindOrCreateChild(parent, "FirstUseTutorialPanel", typeof(RectTransform));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(820f, 540f);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.0012f;

            ConfigureCanvas(panel, worldCamera, 100);
            Image background = CreateImage(panel.transform, "Background", new Color(0.01f, 0.025f, 0.05f, 0.98f), false);
            Stretch(background.rectTransform);

            Text title = CreateText(panel.transform, "Title", "WELCOME TO VR TSE TOUR", 38, FontStyle.Bold);
            title.color = new Color(0.35f, 0.82f, 1f, 1f);
            SetRect(title.rectTransform, new Vector2(740f, 70f), new Vector2(0f, 210f));

            const string instructions =
                "1  Point the controller ray at a button or hotspot.\n" +
                "2  Press the controller trigger to select.\n" +
                "3  Select a hotspot to move between 360 locations.\n" +
                "4  Open MAP to check your current position.\n" +
                "5  Open DESTINATIONS to start route guidance.\n" +
                "6  Inside Room 301, point at the floor to teleport.";
            Text body = CreateText(panel.transform, "Instructions", instructions, 29, FontStyle.Normal);
            body.alignment = TextAnchor.MiddleLeft;
            body.lineSpacing = 1.25f;
            SetRect(body.rectTransform, new Vector2(700f, 310f), new Vector2(0f, 12f));

            Text hint = CreateText(panel.transform, "RepeatHint", "You can open this guide again with HELP.", 23, FontStyle.Italic);
            hint.color = new Color(0.72f, 0.82f, 0.9f, 1f);
            SetRect(hint.rectTransform, new Vector2(650f, 40f), new Vector2(0f, -178f));

            dismissButton = CreateButton(panel.transform, "DismissTutorialButton", "START TOUR", new Vector2(0f, -228f), new Vector2(300f, 66f), new Color(0.08f, 0.58f, 0.42f, 1f));
            return panel;
        }

        private static void ConfigureCanvas(GameObject target, Camera worldCamera, int sortingOrder)
        {
            Canvas canvas = GetOrAdd<Canvas>(target);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = worldCamera;
            canvas.sortingOrder = sortingOrder;
            GetOrAdd<TrackedDeviceGraphicRaycaster>(target);
        }

        private static void StyleExistingButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 size,
            Vector2 position)
        {
            Transform child = FindDescendant(parent, objectName);
            if (child == null)
                throw new InvalidOperationException($"UI object was not found: {objectName}");

            SetRect((RectTransform)child, size, position);
            Image image = child.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.04f, 0.42f, 0.72f, 1f);
            StyleText(child, "Label", 28, FontStyle.Bold);
        }

        private static Button CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            Image image = CreateImage(parent, objectName, color, true);
            SetRect(image.rectTransform, size, position);
            Button button = GetOrAdd<Button>(image.gameObject);
            button.targetGraphic = image;
            button.interactable = true;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.82f, 0.34f, 1f);
            colors.pressedColor = new Color(0.66f, 0.9f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Text text = CreateText(image.transform, "Label", label, 26, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private static Image CreateImage(Transform parent, string objectName, Color color, bool raycastTarget)
        {
            GameObject imageObject = FindOrCreateChild(parent, objectName, typeof(RectTransform));
            Image image = GetOrAdd<Image>(imageObject);
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Text CreateText(Transform parent, string objectName, string value, int size, FontStyle style)
        {
            GameObject textObject = FindOrCreateChild(parent, objectName, typeof(RectTransform));
            Text text = GetOrAdd<Text>(textObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            Outline outline = GetOrAdd<Outline>(textObject);
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Text StyleText(Transform parent, string objectName, int size, FontStyle style)
        {
            Transform child = FindDescendant(parent, objectName);
            if (child == null)
                throw new InvalidOperationException($"Text object was not found: {objectName}");

            Text text = child.GetComponent<Text>();
            if (text == null)
                throw new InvalidOperationException($"Text component was not found: {objectName}");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            Outline outline = GetOrAdd<Outline>(child.gameObject);
            outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static RectTransform FindRect(Transform parent, string objectName)
        {
            Transform child = FindDescendant(parent, objectName);
            if (child == null || child is not RectTransform rect)
                throw new InvalidOperationException($"RectTransform was not found: {objectName}");
            return rect;
        }

        private static Transform FindDescendant(Transform parent, string objectName)
        {
            if (parent.name == objectName)
                return parent;

            return parent.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => candidate.name == objectName);
        }

        private static GameObject Require(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(candidate => candidate.name == objectName);
                if (found != null)
                    return found.gameObject;
            }

            throw new InvalidOperationException($"Scene object was not found: {objectName}");
        }

        private static GameObject FindOrCreateChild(Transform parent, string childName, params Type[] components)
        {
            Transform child = parent.Find(childName);
            if (child != null)
                return child.gameObject;

            var childObject = new GameObject(childName, components);
            childObject.transform.SetParent(parent, false);
            return childObject;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }
    }
}
