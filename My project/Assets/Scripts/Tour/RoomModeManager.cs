using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using VRTSE.UI;

namespace VRTSE.Tour
{
    public sealed class RoomModeManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private TourManager tourManager;
        [SerializeField] private ScreenFader screenFader;
        [SerializeField] private FloorUIController floorUI;
        [SerializeField] private XROrigin xrOrigin;

        [Header("Mode Roots")]
        [SerializeField] private GameObject panoramaEnvironment;
        [SerializeField] private GameObject worldUIRoot;
        [SerializeField] private GameObject room3DRoot;
        [SerializeField] private Transform roomSpawnPoint;
        [SerializeField] private Transform hotspotContainer;

        [Header("Room 301")]
        [SerializeField] private TourNodeData room301PanoramaNode;
        [SerializeField] private GameObject roomEntryPortalPrefab;

        private LocomotionProvider[] locomotionProviders;
        private Vector3 panoramaCameraPosition;
        private Vector3 panoramaCameraForward;
        private bool isInRoom;
        private bool isTransitioning;
        private GameObject cachedEntryPortal;

        private void Awake()
        {
            if (room3DRoot != null)
            {
                foreach (Light light in room3DRoot.GetComponentsInChildren<Light>(true))
                    if (light.type == LightType.Point)
                        light.shadows = LightShadows.None;
            }

            if (xrOrigin != null)
            {
                locomotionProviders = xrOrigin.GetComponentsInChildren<LocomotionProvider>(true);
                if (xrOrigin.Camera != null)
                {
                    panoramaCameraPosition = xrOrigin.Camera.transform.position;
                    panoramaCameraForward = xrOrigin.Camera.transform.forward;
                }
            }

            if (room3DRoot != null)
                room3DRoot.SetActive(false);
            if (panoramaEnvironment != null)
                panoramaEnvironment.SetActive(true);
            if (worldUIRoot != null)
                worldUIRoot.SetActive(true);
            SetRoomLocomotion(false);
        }

        private void OnEnable()
        {
            if (tourManager != null)
                tourManager.NodeChanged += HandleNodeChanged;
        }

        private void OnDisable()
        {
            if (tourManager != null)
                tourManager.NodeChanged -= HandleNodeChanged;
        }

        public void ConfigurePrototype(
            TourManager manager,
            ScreenFader fader,
            FloorUIController uiController,
            XROrigin origin,
            GameObject panoramaRoot,
            GameObject uiRoot,
            GameObject roomRoot,
            Transform spawnPoint,
            Transform hotspots,
            TourNodeData p04,
            GameObject entryPortalPrefab)
        {
            tourManager = manager;
            screenFader = fader;
            floorUI = uiController;
            xrOrigin = origin;
            panoramaEnvironment = panoramaRoot;
            worldUIRoot = uiRoot;
            room3DRoot = roomRoot;
            roomSpawnPoint = spawnPoint;
            hotspotContainer = hotspots;
            room301PanoramaNode = p04;
            roomEntryPortalPrefab = entryPortalPrefab;
        }

        public void EnterRoom301()
        {
            if (isInRoom || isTransitioning || screenFader == null)
                return;

            isTransitioning = true;
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                panoramaCameraPosition = xrOrigin.Camera.transform.position;
                panoramaCameraForward = xrOrigin.Camera.transform.forward;
            }

            bool started = screenFader.TryFadeThrough(ActivateRoomMode, () => isTransitioning = false);
            if (!started)
                isTransitioning = false;
        }

        public void ExitRoom301()
        {
            if (!isInRoom || isTransitioning || screenFader == null)
                return;

            isTransitioning = true;
            bool started = screenFader.TryFadeThrough(ActivatePanoramaMode, () => isTransitioning = false);
            if (!started)
                isTransitioning = false;
        }

        private void ActivateRoomMode()
        {
            if (panoramaEnvironment != null)
                panoramaEnvironment.SetActive(false);
            if (worldUIRoot != null)
                worldUIRoot.SetActive(false);
            if (room3DRoot != null)
                room3DRoot.SetActive(true);

            if (xrOrigin != null && roomSpawnPoint != null)
            {
                xrOrigin.MatchOriginUpCameraForward(Vector3.up, roomSpawnPoint.forward);
                xrOrigin.MoveCameraToWorldLocation(roomSpawnPoint.position);
            }

            SetRoomLocomotion(true);
            isInRoom = true;
            floorUI?.ShowRoomLocation("ROOM 301");
        }

        private void ActivatePanoramaMode()
        {
            SetRoomLocomotion(false);

            if (xrOrigin != null)
            {
                Vector3 forward = panoramaCameraForward.sqrMagnitude > 0.001f
                    ? panoramaCameraForward
                    : Vector3.forward;
                xrOrigin.MatchOriginUpCameraForward(Vector3.up, forward);
                xrOrigin.MoveCameraToWorldLocation(panoramaCameraPosition);
            }

            if (room3DRoot != null)
                room3DRoot.SetActive(false);
            if (panoramaEnvironment != null)
                panoramaEnvironment.SetActive(true);
            if (worldUIRoot != null)
                worldUIRoot.SetActive(true);

            isInRoom = false;
            floorUI?.ShowLocation(room301PanoramaNode);
        }

        private void HandleNodeChanged(TourNodeData node)
        {
            if (isInRoom)
                return;

            if (node != room301PanoramaNode)
            {
                if (cachedEntryPortal != null)
                    cachedEntryPortal.SetActive(false);
                return;
            }
            CreateEntryPortal();
        }

        private void CreateEntryPortal()
        {
            if (hotspotContainer == null || roomEntryPortalPrefab == null)
                return;

            if (cachedEntryPortal != null)
            {
                cachedEntryPortal.SetActive(true);
                return;
            }

            GameObject portal = Instantiate(roomEntryPortalPrefab, hotspotContainer);
            cachedEntryPortal = portal;
            portal.name = "Enter_Room301";
            // Keep the room entrance clear of the north/west walkway hotspots at P04.
            portal.transform.localPosition = Vector3.right * 3f + Vector3.up * 1.35f;
            portal.transform.localRotation = Quaternion.identity;

            RoomPortalInteractable interactable = portal.GetComponent<RoomPortalInteractable>();
            if (interactable != null)
                interactable.Configure(this, false);
        }

        private void SetRoomLocomotion(bool enabled)
        {
            if (locomotionProviders == null)
                return;

            foreach (LocomotionProvider provider in locomotionProviders)
            {
                bool supportedRoomProvider =
                    provider is TeleportationProvider || provider is SnapTurnProvider;
                provider.enabled = enabled && supportedRoomProvider;
            }
        }
    }
}
