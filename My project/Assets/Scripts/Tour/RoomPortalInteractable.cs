using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRTSE.Tour
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class RoomPortalInteractable : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable interactable;
        [SerializeField] private RoomModeManager roomModeManager;
        [SerializeField] private bool exitsRoom;

        private void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
        }

        private void OnEnable()
        {
            interactable.selectEntered.AddListener(HandleSelected);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(HandleSelected);
        }

        public void Configure(RoomModeManager manager, bool isExit)
        {
            roomModeManager = manager;
            exitsRoom = isExit;
        }

        private void HandleSelected(SelectEnterEventArgs _)
        {
            if (roomModeManager == null)
                return;

            if (exitsRoom)
                roomModeManager.ExitRoom301();
            else
                roomModeManager.EnterRoom301();
        }
    }
}
