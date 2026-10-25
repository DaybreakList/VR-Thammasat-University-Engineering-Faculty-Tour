using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRTSE.Tour
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class HotspotView : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable interactable;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material highlightedMaterial;

        private TourManager tourManager;
        private TourNodeData targetNode;

        public TourNodeData TargetNode => targetNode;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
            if (visualRenderer == null)
                visualRenderer = GetComponentInChildren<Renderer>();
            if (normalMaterial == null && visualRenderer != null)
                normalMaterial = visualRenderer.sharedMaterial;
        }

        private void OnEnable()
        {
            interactable.selectEntered.AddListener(OnSelected);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnSelected);
        }

        public void Initialize(TourManager manager, TourNodeData destination)
        {
            tourManager = manager;
            targetNode = destination;
            name = $"Hotspot_To_{destination.NodeId}";
            SetHighlighted(false);
        }

        public void ConfigureVisuals(Renderer renderer, Material normal, Material highlighted)
        {
            visualRenderer = renderer;
            normalMaterial = normal;
            highlightedMaterial = highlighted;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (visualRenderer == null)
                return;

            Material material = highlighted && highlightedMaterial != null
                ? highlightedMaterial
                : normalMaterial;
            if (material != null)
                visualRenderer.sharedMaterial = material;
        }

        private void OnSelected(SelectEnterEventArgs _)
        {
            if (tourManager != null && targetNode != null)
                tourManager.ShowNode(targetNode);
        }
    }
}
