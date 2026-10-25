using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRTSE.Tour;
using VRTSE.UI;

namespace VRTSE.Navigation
{
    public sealed class NavigationManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private TourManager tourManager;
        [SerializeField] private FloorUIController floorUI;
        [SerializeField] private Transform hotspotContainer;

        [Header("Floor 3 Nodes")]
        [SerializeField] private TourNodeData p01;
        [SerializeField] private TourNodeData p02;
        [SerializeField] private TourNodeData p03;
        [SerializeField] private TourNodeData p04;
        [SerializeField] private TourNodeData p05;

        [Header("Destination Menu")]
        [SerializeField] private Button room301Button;
        [SerializeField] private Button room302Button;
        [SerializeField] private Button cancelButton;

        private TourNodeData[] activeRoute;
        private TourNodeData destinationNode;
        private string destinationName;

        private void OnEnable()
        {
            if (tourManager != null)
                tourManager.NodeChanged += HandleNodeChanged;
            if (room301Button != null)
                room301Button.onClick.AddListener(StartRoom301Navigation);
            if (room302Button != null)
                room302Button.onClick.AddListener(StartRoom302Navigation);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelNavigation);
        }

        private void Start()
        {
            floorUI?.ShowNavigationMessage("OPEN DESTINATIONS TO START A ROUTE");
        }

        private void OnDisable()
        {
            if (tourManager != null)
                tourManager.NodeChanged -= HandleNodeChanged;
            if (room301Button != null)
                room301Button.onClick.RemoveListener(StartRoom301Navigation);
            if (room302Button != null)
                room302Button.onClick.RemoveListener(StartRoom302Navigation);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(CancelNavigation);
        }

        public void ConfigurePrototype(
            TourManager manager,
            FloorUIController uiController,
            Transform hotspots,
            TourNodeData floorHub,
            TourNodeData centralLobby,
            TourNodeData northCorridor,
            TourNodeData room301,
            TourNodeData room302,
            Button room301Selection,
            Button room302Selection,
            Button cancelSelection)
        {
            tourManager = manager;
            floorUI = uiController;
            hotspotContainer = hotspots;
            p01 = floorHub;
            p02 = centralLobby;
            p03 = northCorridor;
            p04 = room301;
            p05 = room302;
            room301Button = room301Selection;
            room302Button = room302Selection;
            cancelButton = cancelSelection;
        }

        public void StartRoom301Navigation()
        {
            BeginNavigation("ROOM 301", p04);
        }

        public void StartRoom302Navigation()
        {
            BeginNavigation("ROOM 302", p05);
        }

        public void CancelNavigation()
        {
            activeRoute = null;
            destinationNode = null;
            destinationName = string.Empty;
            HighlightTarget(null);
            floorUI?.ShowMapRoute(null);
            floorUI?.ShowNavigationMessage("NAVIGATION CANCELLED\nSelect another destination");
        }

        private void BeginNavigation(string selectedDestination, TourNodeData target)
        {
            if (tourManager == null || tourManager.CurrentNode == null || target == null)
                return;

            destinationName = selectedDestination;
            destinationNode = target;
            UpdateGuidanceForNode(tourManager.CurrentNode);
        }

        private void HandleNodeChanged(TourNodeData node)
        {
            if (destinationNode == null)
                return;

            UpdateGuidanceForNode(node);
        }

        private void UpdateGuidanceForNode(TourNodeData node)
        {
            activeRoute = FindShortestRoute(node, destinationNode);
            floorUI?.ShowMapRoute(activeRoute);
            if (activeRoute == null)
            {
                floorUI?.ShowNavigationMessage(
                    $"NO CONNECTED ROUTE TO {destinationName}\nChoose another destination or use HOME");
                HighlightTarget(null);
                return;
            }

            if (activeRoute.Length == 1)
            {
                floorUI?.ShowNavigationMessage($"DESTINATION REACHED\n{destinationName}");
                HighlightTarget(null);
                activeRoute = null;
                destinationNode = null;
                floorUI?.ShowMapRoute(null);
                return;
            }

            TourNodeData nextNode = activeRoute[1];
            floorUI?.ShowNavigationMessage(
                $"ROUTE TO {destinationName}\nNEXT: {nextNode.NodeId} {nextNode.DisplayName}");
            HighlightTarget(nextNode);
        }

        private void HighlightTarget(TourNodeData target)
        {
            if (tourManager == null)
                return;

            for (int index = 0; index < tourManager.ActiveHotspots.Count; index++)
            {
                HotspotView hotspot = tourManager.ActiveHotspots[index];
                hotspot.SetHighlighted(target != null && hotspot.TargetNode == target);
            }
        }

        // Each panorama link is one walking step. BFS finds the fewest valid steps
        // from the current location, including reverse travel and detours via P06.
        public static TourNodeData[] FindShortestRoute(TourNodeData start, TourNodeData destination)
        {
            if (start == null || destination == null)
                return null;

            var previous = new Dictionary<TourNodeData, TourNodeData> { [start] = null };
            var pending = new Queue<TourNodeData>();
            pending.Enqueue(start);
            while (pending.Count > 0)
            {
                TourNodeData current = pending.Dequeue();
                if (current == destination)
                {
                    var path = new List<TourNodeData>();
                    for (TourNodeData node = destination; node != null; node = previous[node])
                        path.Add(node);
                    path.Reverse();
                    return path.ToArray();
                }

                foreach (TourNodeLink link in current.OutgoingLinks)
                {
                    TourNodeData next = link?.TargetNode;
                    if (next == null || previous.ContainsKey(next))
                        continue;
                    previous[next] = current;
                    pending.Enqueue(next);
                }
            }

            return null;
        }
    }
}
