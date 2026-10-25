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
    public static class VRTSENavigationSetup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string HotspotPrefabPath = "Assets/Prefabs/Hotspots/Hotspot_PanoramaPortal.prefab";
        private const string NormalMaterialPath = "Assets/Materials/UI/MAT_Hotspot_Normal.mat";
        private const string HighlightMaterialPath = "Assets/Materials/UI/MAT_Hotspot_Highlight.mat";
        private const string SetupCompletedKey = "VRTSE.FixedRouteNavigationCreated.v1";
        private const string LogPrefix = "[VR TSE Navigation Setup]";

        private static bool s_Queued;

        static VRTSENavigationSetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueSetup();
        }

        [MenuItem("Tools/VR TSE/Create Fixed Route Navigation")]
        public static void RunSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                QueueSetup();
                return;
            }

            try
            {
                ConfigureHotspotPrefab();

                Scene scene = SceneManager.GetSceneByPath(ScenePath);
                bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
                if (openedForSetup)
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

                try
                {
                    ConfigureScene(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                finally
                {
                    if (openedForSetup && scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }

                EditorPrefs.SetBool(SetupCompletedKey, true);
                Debug.Log($"{LogPrefix} Room 301/302 fixed-route navigation is ready in {ScenePath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Setup failed: {exception}");
            }
        }

        private static void QueueSetup()
        {
            if (s_Queued)
                return;

            s_Queued = true;
            EditorApplication.update += RunWhenEditorIsReady;
        }

        private static void RunWhenEditorIsReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.update -= RunWhenEditorIsReady;
            s_Queued = false;
            RunSetup();
        }

        private static void ConfigureHotspotPrefab()
        {
            Material normal = AssetDatabase.LoadAssetAtPath<Material>(NormalMaterialPath);
            if (normal == null)
                throw new InvalidOperationException("The normal hotspot material was not found.");

            Material highlight = AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath);
            if (highlight == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    throw new InvalidOperationException("The URP Lit shader was not found.");

                highlight = new Material(shader);
                AssetDatabase.CreateAsset(highlight, HighlightMaterialPath);
            }

            Color yellow = new Color(1f, 0.65f, 0.03f, 1f);
            highlight.color = yellow;
            highlight.EnableKeyword("_EMISSION");
            highlight.SetColor("_EmissionColor", yellow * 3f);
            EditorUtility.SetDirty(highlight);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HotspotPrefabPath);
            try
            {
                HotspotView hotspot = prefabRoot.GetComponent<HotspotView>();
                Renderer renderer = prefabRoot.GetComponentInChildren<Renderer>();
                if (hotspot == null || renderer == null)
                    throw new InvalidOperationException("The hotspot prefab is missing HotspotView or Renderer.");

                hotspot.ConfigureVisuals(renderer, normal, highlight);
                renderer.sharedMaterial = normal;
                EditorUtility.SetDirty(hotspot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, HotspotPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureScene(Scene scene)
        {
            GameObject navigationObject = FindInScene(scene, "NavigationManager");
            GameObject tourObject = FindInScene(scene, "TourManager");
            GameObject hotspotContainer = FindInScene(scene, "HotspotContainer");
            GameObject locationPanel = FindInScene(scene, "CurrentLocationPanel");
            GameObject roomMenuPanel = FindInScene(scene, "RoomMenuPanel");
            GameObject navigationPanel = FindInScene(scene, "NavigationPanel");
            GameObject mainCameraObject = FindInScene(scene, "Main Camera");

            if (navigationObject == null || tourObject == null || hotspotContainer == null ||
                locationPanel == null || roomMenuPanel == null || navigationPanel == null ||
                mainCameraObject == null)
                throw new InvalidOperationException("The prototype navigation hierarchy is incomplete.");

            TourManager tourManager = tourObject.GetComponent<TourManager>();
            FloorUIController floorUI = locationPanel.GetComponent<FloorUIController>();
            Camera mainCamera = mainCameraObject.GetComponent<Camera>();
            if (tourManager == null || floorUI == null || mainCamera == null)
                throw new InvalidOperationException("TourManager, FloorUIController, or Main Camera was not found.");

            Button room301Button;
            Button room302Button;
            Button cancelButton;
            CreateDestinationMenu(
                roomMenuPanel.transform,
                mainCamera,
                out room301Button,
                out room302Button,
                out cancelButton);
            Text navigationText = CreateNavigationMessage(navigationPanel.transform, mainCamera);

            var navigationManager = navigationObject.GetComponent<NavigationManager>();
            if (navigationManager == null)
                navigationManager = navigationObject.AddComponent<NavigationManager>();

            TourNodeData p01 = LoadNode("Assets/Data/TourNodes/P01_FloorHub.asset");
            TourNodeData p02 = LoadNode("Assets/Data/TourNodes/P02_CentralLobby.asset");
            TourNodeData p03 = LoadNode("Assets/Data/TourNodes/P03_NorthCorridor.asset");
            TourNodeData p04 = LoadNode("Assets/Data/TourNodes/P04_Room301.asset");
            TourNodeData p05 = LoadNode("Assets/Data/TourNodes/P07_Room302.asset");

            floorUI.ConfigureNavigationUI(
                roomMenuPanel.transform,
                navigationPanel.transform,
                navigationText);
            navigationManager.ConfigurePrototype(
                tourManager,
                floorUI,
                hotspotContainer.transform,
                p01,
                p02,
                p03,
                p04,
                p05,
                room301Button,
                room302Button,
                cancelButton);

            EditorUtility.SetDirty(floorUI);
            EditorUtility.SetDirty(navigationManager);
        }

        private static void CreateDestinationMenu(
            Transform panelRoot,
            Camera mainCamera,
            out Button room301Button,
            out Button room302Button,
            out Button cancelButton)
        {
            GameObject canvasObject = FindOrCreateChild(panelRoot, "DestinationMenuCanvas", typeof(RectTransform));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 270f);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.0012f;

            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 55;
            GetOrAdd<TrackedDeviceGraphicRaycaster>(canvasObject);

            Image background = CreateImage(
                canvasObject.transform,
                "Background",
                new Color(0.015f, 0.035f, 0.065f, 0.9f),
                false);
            Stretch(background.rectTransform);

            Text title = CreateText(canvasObject.transform, "Title", "SELECT DESTINATION", 24, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(360f, 42f), new Vector2(0f, 105f));

            room301Button = CreateButton(canvasObject.transform, "Room301Button", "ROOM 301", new Vector2(0f, 42f));
            room302Button = CreateButton(canvasObject.transform, "Room302Button", "ROOM 302", new Vector2(0f, -30f));
            cancelButton = CreateButton(canvasObject.transform, "CancelButton", "CANCEL ROUTE", new Vector2(0f, -99f));
            SetRect(cancelButton.GetComponent<RectTransform>(), new Vector2(260f, 48f), new Vector2(0f, -99f));
        }

        private static Text CreateNavigationMessage(Transform panelRoot, Camera mainCamera)
        {
            GameObject canvasObject = FindOrCreateChild(panelRoot, "NavigationMessageCanvas", typeof(RectTransform));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(650f, 92f);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.0012f;

            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 52;

            Image background = CreateImage(
                canvasObject.transform,
                "Background",
                new Color(0.02f, 0.06f, 0.10f, 0.9f),
                false);
            Stretch(background.rectTransform);

            Text message = CreateText(
                canvasObject.transform,
                "NavigationMessageText",
                "SELECT A DESTINATION\nRoom 301 or Room 302",
                23,
                FontStyle.Bold);
            Stretch(message.rectTransform);
            message.rectTransform.offsetMin = new Vector2(20f, 8f);
            message.rectTransform.offsetMax = new Vector2(-20f, -8f);
            return message;
        }

        private static Button CreateButton(
            Transform parent,
            string objectName,
            string label,
            Vector2 position)
        {
            Image image = CreateImage(parent, objectName, new Color(0.04f, 0.40f, 0.70f, 1f), true);
            SetRect(image.rectTransform, new Vector2(320f, 58f), position);

            Button button = GetOrAdd<Button>(image.gameObject);
            button.targetGraphic = image;
            button.interactable = true;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.40f, 1f);
            colors.pressedColor = new Color(0.75f, 0.92f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text buttonText = CreateText(image.transform, "Label", label, 23, FontStyle.Bold);
            Stretch(buttonText.rectTransform);
            return button;
        }

        private static TourNodeData LoadNode(string assetPath)
        {
            TourNodeData node = AssetDatabase.LoadAssetAtPath<TourNodeData>(assetPath);
            if (node == null)
                throw new InvalidOperationException($"Tour node was not found: {assetPath}");
            return node;
        }

        private static Image CreateImage(Transform parent, string objectName, Color color, bool raycastTarget)
        {
            GameObject imageObject = FindOrCreateChild(parent, objectName, typeof(RectTransform));
            Image image = GetOrAdd<Image>(imageObject);
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            string value,
            int fontSize,
            FontStyle style)
        {
            GameObject textObject = FindOrCreateChild(parent, objectName, typeof(RectTransform));
            Text text = GetOrAdd<Text>(textObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == objectName);
                if (match != null)
                    return match.gameObject;
            }

            return null;
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

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 size, Vector2 position)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = position;
            rectTransform.localScale = Vector3.one;
        }
    }
}
