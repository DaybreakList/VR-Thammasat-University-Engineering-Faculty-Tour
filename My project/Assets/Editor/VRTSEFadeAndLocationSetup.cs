using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRTSE.Tour;
using VRTSE.UI;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSEFadeAndLocationSetup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string SetupCompletedKey = "VRTSE.FadeAndLocationCreated.v1";
        private const string LogPrefix = "[VR TSE Presentation Setup]";

        private static bool s_Queued;

        static VRTSEFadeAndLocationSetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueSetup();
        }

        [MenuItem("Tools/VR TSE/Configure Fade and Current Location")]
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
                Debug.Log($"{LogPrefix} Fade and current-location UI are ready in {ScenePath}");
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

        private static void ConfigureScene(Scene scene)
        {
            GameObject tourManagerObject = FindInScene(scene, "TourManager");
            GameObject faderObject = FindInScene(scene, "ScreenFader");
            GameObject locationPanel = FindInScene(scene, "CurrentLocationPanel");
            GameObject mainCameraObject = FindInScene(scene, "Main Camera");

            if (tourManagerObject == null || faderObject == null ||
                locationPanel == null || mainCameraObject == null)
                throw new InvalidOperationException("Required prototype scene objects were not found.");

            Camera mainCamera = mainCameraObject.GetComponent<Camera>();
            if (mainCamera == null)
                throw new InvalidOperationException("Main Camera component was not found.");

            ScreenFader fader = faderObject.GetComponent<ScreenFader>();
            if (fader == null)
                fader = faderObject.AddComponent<ScreenFader>();
            CanvasGroup fadeGroup = CreateFadeCanvas(mainCamera.transform, mainCamera);
            fader.ConfigurePrototype(fadeGroup);

            FloorUIController floorUI = locationPanel.GetComponent<FloorUIController>();
            if (floorUI == null)
                floorUI = locationPanel.AddComponent<FloorUIController>();
            Text locationText = CreateLocationPanel(locationPanel.transform, mainCamera);
            floorUI.ConfigurePrototype(locationText, mainCamera.transform);

            TourManager tourManager = tourManagerObject.GetComponent<TourManager>();
            if (tourManager == null)
                throw new InvalidOperationException("TourManager component was not found.");
            tourManager.ConfigurePresentation(fader, floorUI);

            EditorUtility.SetDirty(fader);
            EditorUtility.SetDirty(floorUI);
            EditorUtility.SetDirty(tourManager);
        }

        private static CanvasGroup CreateFadeCanvas(Transform cameraTransform, Camera mainCamera)
        {
            Transform existing = cameraTransform.Find("VRFadeCanvas");
            GameObject canvasObject = existing != null ? existing.gameObject : new GameObject("VRFadeCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(cameraTransform, false);

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2000f, 2000f);
            canvasRect.localPosition = new Vector3(0f, 0f, 0.2f);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.001f;

            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = short.MaxValue;

            CanvasGroup group = GetOrAdd<CanvasGroup>(canvasObject);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject imageObject = FindOrCreateChild(canvasObject.transform, "FadeImage", typeof(RectTransform));
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            Stretch(imageRect);
            Image image = GetOrAdd<Image>(imageObject);
            image.color = Color.black;
            image.raycastTarget = false;
            return group;
        }

        private static Text CreateLocationPanel(Transform panelRoot, Camera mainCamera)
        {
            GameObject canvasObject = FindOrCreateChild(panelRoot, "CurrentLocationCanvas", typeof(RectTransform));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(680f, 118f);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.0012f;

            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 50;

            GameObject backgroundObject = FindOrCreateChild(canvasObject.transform, "Background", typeof(RectTransform));
            Stretch(backgroundObject.GetComponent<RectTransform>());
            Image background = GetOrAdd<Image>(backgroundObject);
            background.color = new Color(0.015f, 0.035f, 0.065f, 0.82f);
            background.raycastTarget = false;

            GameObject textObject = FindOrCreateChild(backgroundObject.transform, "LocationText", typeof(RectTransform));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            Stretch(textRect);
            textRect.offsetMin = new Vector2(24f, 10f);
            textRect.offsetMax = new Vector2(-24f, -10f);

            Text text = GetOrAdd<Text>(textObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = "CURRENT LOCATION\nP01  —  Floor Hub / Elevator Area";
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
    }
}
