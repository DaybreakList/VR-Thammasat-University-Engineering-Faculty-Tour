using UnityEngine;
using UnityEngine.UI;
using VRTSE.Navigation;
using VRTSE.Tour;

namespace VRTSE.UI
{
    public sealed class VRMenuController : MonoBehaviour
    {
        private const string TutorialSeenKey = "VRTSE.TutorialSeen.v1";

        [Header("Systems")]
        [SerializeField] private TourManager tourManager;
        [SerializeField] private NavigationManager navigationManager;

        [Header("Panels")]
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject destinationPanel;
        [SerializeField] private GameObject tutorialPanel;

        [Header("Quick Menu")]
        [SerializeField] private Button mapButton;
        [SerializeField] private Button destinationsButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button dismissTutorialButton;

        [Header("Destination Actions")]
        [SerializeField] private Button room301Button;
        [SerializeField] private Button room302Button;
        [SerializeField] private Button cancelNavigationButton;

        private void OnEnable()
        {
            AddListener(mapButton, ToggleMap);
            AddListener(destinationsButton, ToggleDestinations);
            AddListener(homeButton, ReturnToStart);
            AddListener(helpButton, ShowHelp);
            AddListener(dismissTutorialButton, DismissTutorial);
            AddListener(room301Button, CloseDestinationPanel);
            AddListener(room302Button, CloseDestinationPanel);
            AddListener(cancelNavigationButton, CloseDestinationPanel);
        }

        private void Start()
        {
            SetActive(mapPanel, false);
            SetActive(destinationPanel, false);
            SetActive(tutorialPanel, PlayerPrefs.GetInt(TutorialSeenKey, 0) == 0);
        }

        private void OnDisable()
        {
            RemoveListener(mapButton, ToggleMap);
            RemoveListener(destinationsButton, ToggleDestinations);
            RemoveListener(homeButton, ReturnToStart);
            RemoveListener(helpButton, ShowHelp);
            RemoveListener(dismissTutorialButton, DismissTutorial);
            RemoveListener(room301Button, CloseDestinationPanel);
            RemoveListener(room302Button, CloseDestinationPanel);
            RemoveListener(cancelNavigationButton, CloseDestinationPanel);
        }

        public void Configure(
            TourManager tour,
            NavigationManager navigation,
            GameObject map,
            GameObject destinations,
            GameObject tutorial,
            Button mapToggle,
            Button destinationToggle,
            Button home,
            Button help,
            Button dismissTutorial,
            Button room301,
            Button room302,
            Button cancelNavigation)
        {
            tourManager = tour;
            navigationManager = navigation;
            mapPanel = map;
            destinationPanel = destinations;
            tutorialPanel = tutorial;
            mapButton = mapToggle;
            destinationsButton = destinationToggle;
            homeButton = home;
            helpButton = help;
            dismissTutorialButton = dismissTutorial;
            room301Button = room301;
            room302Button = room302;
            cancelNavigationButton = cancelNavigation;
        }

        public void ToggleMap()
        {
            bool shouldOpen = mapPanel != null && !mapPanel.activeSelf;
            CloseOverlays();
            SetActive(mapPanel, shouldOpen);
        }

        public void ToggleDestinations()
        {
            bool shouldOpen = destinationPanel != null && !destinationPanel.activeSelf;
            CloseOverlays();
            SetActive(destinationPanel, shouldOpen);
        }

        public void ReturnToStart()
        {
            CloseOverlays();
            navigationManager?.CancelNavigation();
            tourManager?.ReturnToStart();
        }

        public void ShowHelp()
        {
            CloseOverlays();
            SetActive(tutorialPanel, true);
        }

        public void DismissTutorial()
        {
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            PlayerPrefs.Save();
            SetActive(tutorialPanel, false);
        }

        private void CloseDestinationPanel()
        {
            SetActive(destinationPanel, false);
        }

        private void CloseOverlays()
        {
            SetActive(mapPanel, false);
            SetActive(destinationPanel, false);
            SetActive(tutorialPanel, false);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }
    }
}
