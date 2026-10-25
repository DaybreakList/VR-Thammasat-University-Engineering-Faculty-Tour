using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRTSE.UI;
using VRTSE.Tour;
using VRTSE.Navigation;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSEFloorMapSetup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string SetupCompletedKey = "VRTSE.RectangularLoopMapCreated.v4";
        private const string LogPrefix = "[VR TSE Floor Map Setup]";

        private static TourNodeData[] LoadConnectedNodes()
        {
            var home = AssetDatabase.LoadAssetAtPath<TourNodeData>("Assets/Data/TourNodes/P01_FloorHub.asset");
            if (home == null)
                throw new InvalidOperationException("P01 home node was not found.");
            var visited = new HashSet<TourNodeData> { home };
            var pending = new Queue<TourNodeData>();
            pending.Enqueue(home);
            while (pending.Count > 0)
            {
                foreach (TourNodeLink link in pending.Dequeue().OutgoingLinks)
                {
                    if (link?.TargetNode != null && visited.Add(link.TargetNode))
                        pending.Enqueue(link.TargetNode);
                }
            }
            return visited.OrderBy(node => node.NodeId).ToArray();
        }

        private static bool s_Queued;

        static VRTSEFloorMapSetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueSetup();
        }

        [MenuItem("Tools/VR TSE/Create Floor 3 Map and Marker")]
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
                ValidateLoopRoutes();
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
                Debug.Log($"{LogPrefix} Floor 3 schematic map and live marker are ready in {ScenePath}");
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
            GameObject mapPanel = FindInScene(scene, "FloorMapPanel");
            GameObject locationPanel = FindInScene(scene, "CurrentLocationPanel");
            GameObject mainCameraObject = FindInScene(scene, "Main Camera");

            if (mapPanel == null || locationPanel == null || mainCameraObject == null)
                throw new InvalidOperationException("FloorMapPanel, CurrentLocationPanel, or Main Camera was not found.");

            Camera mainCamera = mainCameraObject.GetComponent<Camera>();
            FloorUIController floorUI = locationPanel.GetComponent<FloorUIController>();
            if (mainCamera == null || floorUI == null)
                throw new InvalidOperationException("Main Camera or FloorUIController component was not found.");

            RectTransform mapArea = CreateMapCanvas(mapPanel.transform, mainCamera);
            RectTransform marker = CreateMapContent(mapArea);
            floorUI.ConfigureMap(mapPanel.transform, mapArea, marker);

            EditorUtility.SetDirty(floorUI);
            EditorUtility.SetDirty(mapPanel);
        }

        private static RectTransform CreateMapCanvas(Transform panelRoot, Camera mainCamera)
        {
            GameObject canvasObject = FindOrCreateChild(panelRoot, "FloorMapCanvas", typeof(RectTransform));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(440f, 330f);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.0013f;

            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;
            canvas.sortingOrder = 45;

            Image background = CreateImage(
                canvasObject.transform,
                "Background",
                new Color(0.015f, 0.035f, 0.065f, 0.88f));
            Stretch(background.rectTransform);

            Text title = CreateText(canvasObject.transform, "Title", "FLOOR 3 — WALKWAY MAP", 25, FontStyle.Bold);
            SetRect(title.rectTransform, new Vector2(410f, 42f), new Vector2(0f, 137f));

            Text legend = CreateText(canvasObject.transform, "MapLegend",
                "YELLOW: YOU   |   GREEN: ROUTE\nHOME returns to P01", 19, FontStyle.Normal);
            SetRect(legend.rectTransform, new Vector2(410f, 50f), new Vector2(0f, -132f));

            GameObject areaObject = FindOrCreateChild(canvasObject.transform, "MapArea", typeof(RectTransform));
            RectTransform areaRect = areaObject.GetComponent<RectTransform>();
            SetRect(areaRect, new Vector2(390f, 200f), new Vector2(0f, 5f));
            Image areaBackground = GetOrAdd<Image>(areaObject);
            areaBackground.color = new Color(0.04f, 0.09f, 0.14f, 0.95f);
            areaBackground.raycastTarget = false;
            return areaRect;
        }

        private static RectTransform CreateMapContent(RectTransform mapArea)
        {
            TourNodeData[] nodes = LoadConnectedNodes();
            // Hide old generated elements, then re-enable only the current graph.
            // This removes stale branches without deleting other scene content.
            foreach (Transform child in mapArea)
            {
                if (child.name.StartsWith("Line_") || child.name.StartsWith("Node_") ||
                    child.name.StartsWith("Label_"))
                    child.gameObject.SetActive(false);
            }
            var edges = new HashSet<string>();
            foreach (TourNodeData node in nodes)
            {
                foreach (TourNodeLink link in node.OutgoingLinks)
                {
                    TourNodeData target = link?.TargetNode;
                    if (target == null)
                        continue;
                    string edge = string.CompareOrdinal(node.NodeId, target.NodeId) < 0
                        ? $"Line_{node.NodeId}_{target.NodeId}"
                        : $"Line_{target.NodeId}_{node.NodeId}";
                    if (edges.Add(edge))
                        CreateConnection(mapArea, edge, node.MapPosition, target.MapPosition);
                }
            }
            foreach (TourNodeData node in nodes)
                CreateNode(mapArea, node);

            Image markerImage = CreateImage(mapArea, "CurrentPositionMarker", new Color(1f, 0.78f, 0.08f, 1f));
            RectTransform marker = markerImage.rectTransform;
            SetRect(marker, new Vector2(18f, 18f), ToMapPoint(mapArea, nodes[0].MapPosition));
            marker.localRotation = Quaternion.identity;
            marker.SetAsLastSibling();
            return marker;
        }

        private static void CreateConnection(
            RectTransform mapArea,
            string objectName,
            Vector2 startNormalized,
            Vector2 endNormalized)
        {
            Vector2 start = ToMapPoint(mapArea, startNormalized);
            Vector2 end = ToMapPoint(mapArea, endNormalized);
            // Replace legacy diagonal connectors with thick, right-angle corridors.
            Transform legacy = mapArea.Find(objectName);
            if (legacy != null)
                legacy.gameObject.SetActive(false);

            Vector2 corner = new Vector2(start.x, end.y);
            CreateCorridorSegment(mapArea, objectName + "_Horizontal", start, corner);
            CreateCorridorSegment(mapArea, objectName + "_Vertical", corner, end);
        }

        private static void CreateCorridorSegment(RectTransform area, string name, Vector2 start, Vector2 end)
        {
            Image corridor = CreateImage(area, name, new Color(0.32f, 0.58f, 0.75f, 0.8f));
            Vector2 delta = end - start;
            corridor.gameObject.SetActive(delta.sqrMagnitude > 0.01f);
            SetRect(corridor.rectTransform,
                new Vector2(Mathf.Abs(delta.x) + 14f, Mathf.Abs(delta.y) + 14f), (start + end) * 0.5f);
            corridor.rectTransform.localRotation = Quaternion.identity;
        }

        private static void CreateNode(RectTransform mapArea, TourNodeData node)
        {
            string id = node.NodeId;
            Vector2 position = ToMapPoint(mapArea, node.MapPosition);

            Image dot = CreateImage(mapArea, $"Node_{id}", new Color(0.15f, 0.62f, 1f, 1f));
            dot.gameObject.SetActive(true);
            SetRect(dot.rectTransform, new Vector2(28f, 28f), position);
            dot.rectTransform.localRotation = Quaternion.identity;

            string name = id == "P01" ? "P01 HOME"
                : node.DisplayName.Contains("Room 301") ? id + " / 301"
                : node.DisplayName.Contains("Room 302") ? id + " / 302" : id;
            Text label = CreateText(mapArea, $"Label_{id}", name, 18, FontStyle.Bold);
            label.gameObject.SetActive(true);
            float labelOffset = node.MapPosition.y < 0.4f ? -26f : 26f;
            SetRect(label.rectTransform, new Vector2(94f, 24f), position + new Vector2(0f, labelOffset));
        }

        [MenuItem("Tools/VR TSE/Validate Rectangular Loop Routes")]
        public static void ValidateLoopRoutes()
        {
            TourNodeData[] nodes = LoadConnectedNodes();
            if (nodes.Length < 10)
                throw new InvalidOperationException("Expected at least 10 connected panorama nodes.");
            foreach (TourNodeData start in nodes)
            {
                if (start.OutgoingLinks.Count != 2)
                    throw new InvalidOperationException($"{start.NodeId} must have two loop neighbours.");
                foreach (TourNodeLink link in start.OutgoingLinks)
                {
                    if (!link.TargetNode.OutgoingLinks.Any(reverse => reverse.TargetNode == start))
                        throw new InvalidOperationException($"Missing reverse link at {start.NodeId}.");
                    Vector2 delta = link.TargetNode.MapPosition - start.MapPosition;
                    if (Mathf.Abs(delta.x) > 0.001f && Mathf.Abs(delta.y) > 0.001f)
                        throw new InvalidOperationException($"Diagonal walkway at {start.NodeId}.");
                }
                foreach (TourNodeData target in nodes)
                {
                    TourNodeData[] path = NavigationManager.FindShortestRoute(start, target);
                    if (path == null || path[0] != start || path[path.Length - 1] != target ||
                        path.Length - 1 > nodes.Length / 2)
                        throw new InvalidOperationException($"Invalid shortest route: {start.NodeId} -> {target.NodeId}");
                    for (int index = 0; index + 1 < path.Length; index++)
                        if (!path[index].OutgoingLinks.Any(link => link.TargetNode == path[index + 1]))
                            throw new InvalidOperationException("A route crosses an unconnected edge.");
                }
            }
            Debug.Log($"{LogPrefix} Validated {nodes.Length * nodes.Length} routes on the bidirectional rectangular loop.");
        }

        private static Vector2 ToMapPoint(RectTransform mapArea, Vector2 normalizedPosition)
        {
            Rect rect = mapArea.rect;
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPosition.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPosition.y));
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject imageObject = FindOrCreateChild(parent, objectName, typeof(RectTransform));
            Image image = GetOrAdd<Image>(imageObject);
            image.color = color;
            image.raycastTarget = false;
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
