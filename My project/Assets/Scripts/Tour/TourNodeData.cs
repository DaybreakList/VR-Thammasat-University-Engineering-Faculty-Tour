using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRTSE.Tour
{
    [Serializable]
    public sealed class TourNodeLink
    {
        [SerializeField] private string label;
        [SerializeField] private TourNodeData targetNode;
        [SerializeField] private Vector3 localDirection = Vector3.forward;

        public string Label => label;
        public TourNodeData TargetNode => targetNode;
        public Vector3 LocalDirection => localDirection;

        public TourNodeLink(string label, TourNodeData targetNode, Vector3 localDirection)
        {
            this.label = label;
            this.targetNode = targetNode;
            this.localDirection = localDirection;
        }
    }

    [CreateAssetMenu(fileName = "TourNode", menuName = "VR TSE/Tour Node")]
    public sealed class TourNodeData : ScriptableObject
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string displayName;
        [SerializeField] private Material panoramaMaterial;
        [SerializeField] private float initialYaw;
        [SerializeField] private Vector2 mapPosition;
        [SerializeField] private List<TourNodeLink> outgoingLinks = new();

        public string NodeId => nodeId;
        public string DisplayName => displayName;
        public Material PanoramaMaterial => panoramaMaterial;
        public float InitialYaw => initialYaw;
        public Vector2 MapPosition => mapPosition;
        public IReadOnlyList<TourNodeLink> OutgoingLinks => outgoingLinks;

#if UNITY_EDITOR
        public void ConfigurePrototypeNode(
            string id,
            string nodeDisplayName,
            Material material,
            float yaw,
            Vector2 markerPosition,
            IEnumerable<TourNodeLink> links)
        {
            nodeId = id;
            displayName = nodeDisplayName;
            panoramaMaterial = material;
            initialYaw = yaw;
            mapPosition = markerPosition;
            outgoingLinks = new List<TourNodeLink>(links);
        }
#endif
    }
}
