using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using VRTSE.UI;

namespace VRTSE.Tour
{
    public sealed class TourManager : MonoBehaviour
    {
        [Header("Prototype Nodes")]
        [SerializeField] private TourNodeData startNode;

        [Header("Scene References")]
        [SerializeField] private Transform hotspotContainer;
        [SerializeField] private GameObject hotspotPrefab;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private ScreenFader screenFader;
        [SerializeField] private FloorUIController floorUI;

        [Header("Hotspot Placement")]
        [SerializeField, Min(0.5f)] private float hotspotDistance = 3f;
        [SerializeField, Min(0f)] private float hotspotHeight = 1.5f;

        [Header("Performance")]
        [Tooltip("Enable only when panorama changes must relight reflective 3D objects.")]
        [SerializeField] private bool updateEnvironmentLighting;

        public TourNodeData CurrentNode { get; private set; }
        public event Action<TourNodeData> NodeChanged;
        public IReadOnlyList<HotspotView> ActiveHotspots => activeHotspots;

        private bool isTransitioning;
        private readonly List<HotspotView> hotspotPool = new();
        private readonly List<HotspotView> activeHotspots = new();
        private static readonly int RotationProperty = Shader.PropertyToID("_Rotation");

        private void Start()
        {
            DisablePositionalLocomotion();
            ApplyNode(startNode);
        }

        public void ConfigurePrototype(
            TourNodeData initialNode,
            Transform container,
            GameObject prefab,
            XROrigin origin)
        {
            startNode = initialNode;
            hotspotContainer = container;
            hotspotPrefab = prefab;
            xrOrigin = origin;
        }

        public void ConfigurePresentation(ScreenFader fader, FloorUIController uiController)
        {
            screenFader = fader;
            floorUI = uiController;
        }

        public void ShowNode(TourNodeData node)
        {
            if (isTransitioning || node == CurrentNode)
                return;

            if (!IsNodeValid(node))
                return;

            if (screenFader == null)
            {
                ApplyNode(node);
                return;
            }

            isTransitioning = true;
            bool started = screenFader.TryFadeThrough(
                () => ApplyNode(node),
                () => isTransitioning = false);

            if (!started)
                isTransitioning = false;
        }

        public void ReturnToStart()
        {
            if (CurrentNode == startNode)
            {
                floorUI?.ShowLocation(startNode);
                return;
            }

            ShowNode(startNode);
        }

        private bool IsNodeValid(TourNodeData node)
        {
            if (node == null)
            {
                Debug.LogError("[VR TSE Tour] Cannot show a null tour node.", this);
                return false;
            }

            if (node.PanoramaMaterial == null)
            {
                Debug.LogError($"[VR TSE Tour] {node.NodeId} has no panorama material.", node);
                return false;
            }

            return true;
        }

        private void ApplyNode(TourNodeData node)
        {
            if (!IsNodeValid(node))
                return;

            CurrentNode = node;
            RenderSettings.skybox = node.PanoramaMaterial;
            if (node.PanoramaMaterial.HasProperty(RotationProperty))
                node.PanoramaMaterial.SetFloat(RotationProperty, node.InitialYaw);

            if (updateEnvironmentLighting)
                DynamicGI.UpdateEnvironment();
            RebuildHotspots(node);
            floorUI?.ShowLocation(node);
            NodeChanged?.Invoke(node);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[VR TSE Tour] Current node: {node.NodeId} — {node.DisplayName}", node);
#endif
        }

        private void RebuildHotspots(TourNodeData node)
        {
            if (hotspotContainer == null || hotspotPrefab == null)
            {
                Debug.LogError("[VR TSE Tour] Hotspot references are not configured.", this);
                return;
            }

            foreach (HotspotView hotspot in activeHotspots)
                hotspot.gameObject.SetActive(false);
            activeHotspots.Clear();

            foreach (TourNodeLink link in node.OutgoingLinks)
            {
                if (link?.TargetNode == null)
                    continue;

                Vector3 direction = link.LocalDirection.sqrMagnitude > 0.001f
                    ? link.LocalDirection.normalized
                    : Vector3.forward;

                int poolIndex = activeHotspots.Count;
                HotspotView hotspotView;
                if (poolIndex < hotspotPool.Count)
                {
                    hotspotView = hotspotPool[poolIndex];
                }
                else
                {
                    GameObject instance = Instantiate(hotspotPrefab, hotspotContainer);
                    hotspotView = instance.GetComponent<HotspotView>();
                    if (hotspotView == null)
                    {
                        Debug.LogError("[VR TSE Tour] Hotspot prefab is missing HotspotView.", instance);
                        Destroy(instance);
                        continue;
                    }
                    hotspotPool.Add(hotspotView);
                }

                hotspotView.transform.localPosition = direction * hotspotDistance + Vector3.up * hotspotHeight;
                hotspotView.transform.localRotation = Quaternion.identity;
                hotspotView.Initialize(this, link.TargetNode);
                hotspotView.gameObject.SetActive(true);
                activeHotspots.Add(hotspotView);
            }
        }

        private void DisablePositionalLocomotion()
        {
            if (xrOrigin == null)
                return;

            foreach (LocomotionProvider provider in xrOrigin.GetComponentsInChildren<LocomotionProvider>(true))
                provider.enabled = false;
        }
    }
}
