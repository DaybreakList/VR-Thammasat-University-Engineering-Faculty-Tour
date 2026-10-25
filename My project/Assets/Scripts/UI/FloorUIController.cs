using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTSE.Tour;

namespace VRTSE.UI
{
    public sealed class FloorUIController : MonoBehaviour
    {
        [SerializeField] private Text currentLocationText;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 viewOffset = new Vector3(0f, 0.36f, 1.2f);

        [Header("Floor Map")]
        [SerializeField] private Transform mapPanelRoot;
        [SerializeField] private RectTransform mapArea;
        [SerializeField] private RectTransform currentPositionMarker;
        [SerializeField] private Vector3 mapViewOffset = new Vector3(-0.5f, -0.1f, 1.25f);

        [Header("Navigation UI")]
        [SerializeField] private Transform destinationMenuRoot;
        [SerializeField] private Transform navigationPanelRoot;
        [SerializeField] private Text navigationMessageText;
        [SerializeField] private Vector3 destinationMenuOffset = new Vector3(0.5f, -0.08f, 1.25f);
        [SerializeField] private Vector3 navigationMessageOffset = new Vector3(0f, -0.34f, 1.15f);

        [Header("Quick Menu UI")]
        [SerializeField] private Transform quickMenuRoot;
        [SerializeField] private Transform tutorialPanelRoot;
        [SerializeField] private Vector3 quickMenuOffset = new Vector3(0f, -0.5f, 1.2f);
        [SerializeField] private Vector3 tutorialViewOffset = new Vector3(0f, 0f, 1.05f);

        private readonly List<MapSegment> mapSegments = new();
        private readonly HashSet<string> routeEdges = new();
        private bool mapSegmentsCached;

        private readonly struct MapSegment
        {
            public readonly Image Image;
            public readonly string Edge;
            public MapSegment(Image image, string edge) { Image = image; Edge = edge; }
        }

        public void ConfigurePrototype(Text label, Transform cameraTransform)
        {
            currentLocationText = label;
            followTarget = cameraTransform;
        }

        public void ConfigureMap(
            Transform panelRoot,
            RectTransform markerArea,
            RectTransform marker)
        {
            mapPanelRoot = panelRoot;
            mapArea = markerArea;
            currentPositionMarker = marker;
            mapSegmentsCached = false;
        }

        public void ConfigureNavigationUI(
            Transform menuRoot,
            Transform messageRoot,
            Text messageText)
        {
            destinationMenuRoot = menuRoot;
            navigationPanelRoot = messageRoot;
            navigationMessageText = messageText;
        }

        public void ConfigureQuickMenu(Transform menuRoot, Transform tutorialRoot)
        {
            quickMenuRoot = menuRoot;
            tutorialPanelRoot = tutorialRoot;
        }

        public void ShowNavigationMessage(string message)
        {
            if (navigationMessageText != null)
                navigationMessageText.text = message;
        }

        public void ShowMapRoute(IReadOnlyList<TourNodeData> route)
        {
            if (mapArea == null)
                return;

            if (!mapSegmentsCached)
            {
                mapSegments.Clear();
                foreach (Image image in mapArea.GetComponentsInChildren<Image>(true))
                {
                    if (!image.name.StartsWith("Line_"))
                        continue;
                    string[] parts = image.name.Split('_');
                    if (parts.Length >= 3)
                        mapSegments.Add(new MapSegment(image, EdgeKey(parts[1], parts[2])));
                }
                mapSegmentsCached = true;
            }

            routeEdges.Clear();
            if (route != null)
                for (int index = 0; index + 1 < route.Count; index++)
                    routeEdges.Add(EdgeKey(route[index].NodeId, route[index + 1].NodeId));

            foreach (MapSegment segment in mapSegments)
            {
                if (segment.Image == null)
                    continue;
                Color color = routeEdges.Contains(segment.Edge)
                    ? new Color(0.25f, 1f, 0.65f, 1f)
                    : new Color(0.32f, 0.58f, 0.75f, 0.8f);
                if (segment.Image.color != color)
                    segment.Image.color = color;
            }
        }

        private static string EdgeKey(string first, string second)
        {
            return string.CompareOrdinal(first, second) < 0
                ? first + "_" + second : second + "_" + first;
        }

        public void ShowRoomLocation(string roomName)
        {
            if (currentLocationText != null)
                currentLocationText.text = $"CURRENT LOCATION\n{roomName} — 3D MODE";
        }

        public void ShowLocation(TourNodeData node)
        {
            if (node == null)
                return;

            if (currentLocationText != null)
                currentLocationText.text = $"CURRENT LOCATION\n{node.NodeId}  —  {node.DisplayName}";

            UpdateMapMarker(node.MapPosition);
        }

        private void LateUpdate()
        {
            if (followTarget == null)
                return;

            FollowPanel(transform, viewOffset);
            FollowPanel(mapPanelRoot, mapViewOffset);
            FollowPanel(destinationMenuRoot, destinationMenuOffset);
            FollowPanel(navigationPanelRoot, navigationMessageOffset);
            FollowPanel(quickMenuRoot, quickMenuOffset);
            FollowPanel(tutorialPanelRoot, tutorialViewOffset);
        }

        private void FollowPanel(Transform panel, Vector3 offset)
        {
            if (panel == null || !panel.gameObject.activeInHierarchy)
                return;
            Vector3 position = followTarget.TransformPoint(offset);
            Quaternion rotation = followTarget.rotation;
            if (panel.position != position || panel.rotation != rotation)
                panel.SetPositionAndRotation(position, rotation);
        }

        private void UpdateMapMarker(Vector2 normalizedPosition)
        {
            if (mapArea == null || currentPositionMarker == null)
                return;

            Rect mapRect = mapArea.rect;
            currentPositionMarker.anchoredPosition = new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, Mathf.Clamp01(normalizedPosition.x)),
                Mathf.Lerp(mapRect.yMin, mapRect.yMax, Mathf.Clamp01(normalizedPosition.y)));
            currentPositionMarker.SetAsLastSibling();
        }
    }
}
