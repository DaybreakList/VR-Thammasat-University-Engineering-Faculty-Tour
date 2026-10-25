using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRTSE.Editor
{
    /// <summary>
    /// Imports the required simulator sample and creates the first non-destructive
    /// Floor 3 prototype scene skeleton.
    /// </summary>
    [InitializeOnLoad]
    public static class VRTSEPrototypeSceneSetup
    {
        private const string PackageName = "com.unity.xr.interaction.toolkit";
        private const string PackageVersion = "3.5.1";
        private const string DeviceSimulatorSampleName = "XR Device Simulator";
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string XROriginPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string DeviceSimulatorPrefabPath =
            "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Device Simulator/XR Device Simulator.prefab";
        private const string SetupCompletedKey = "VRTSE.PrototypeSceneCreated.v1";
        private const string LogPrefix = "[VR TSE Prototype Setup]";

        private static bool s_Queued;

        static VRTSEPrototypeSceneSetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueSetup();
        }

        [MenuItem("Tools/VR TSE/Create Floor 3 Prototype Scene")]
        public static void RunSetup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueSetup();
                return;
            }

            try
            {
                if (!EnsureDeviceSimulatorSample())
                {
                    QueueSetup();
                    return;
                }

                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                    CreatePrototypeScene();

                AddSceneToBuildSettings();
                EditorPrefs.SetBool(SetupCompletedKey, true);
                Debug.Log($"{LogPrefix} Created and registered {ScenePath}");
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
            EditorApplication.delayCall += () =>
            {
                s_Queued = false;
                RunSetup();
            };
        }

        private static bool EnsureDeviceSimulatorSample()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DeviceSimulatorPrefabPath) != null)
                return true;

            Sample simulatorSample = Sample.FindByPackage(PackageName, PackageVersion)
                .FirstOrDefault(sample => sample.displayName == DeviceSimulatorSampleName);

            if (string.IsNullOrEmpty(simulatorSample.displayName))
                throw new InvalidOperationException(
                    $"Could not find the {DeviceSimulatorSampleName} sample in {PackageName} {PackageVersion}.");

            bool imported = simulatorSample.Import(Sample.ImportOptions.OverridePreviousImports);
            if (!imported)
                throw new InvalidOperationException($"Unity could not import {DeviceSimulatorSampleName}.");

            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(DeviceSimulatorPrefabPath) != null;
        }

        private static void CreatePrototypeScene()
        {
            Scene originalActiveScene = SceneManager.GetActiveScene();
            Scene prototypeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            prototypeScene.name = "Floor3Prototype";
            SceneManager.SetActiveScene(prototypeScene);

            try
            {
                CreateSystemsHierarchy();
                CreateXRHierarchy(prototypeScene);
                CreateTourHierarchy();
                CreateRoomHierarchy();
                CreateUIHierarchy();
                CreateLighting();

                if (!EditorSceneManager.SaveScene(prototypeScene, ScenePath))
                    throw new InvalidOperationException($"Unity could not save {ScenePath}.");
            }
            finally
            {
                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                    SceneManager.SetActiveScene(originalActiveScene);

                if (prototypeScene.IsValid() && prototypeScene.isLoaded)
                    EditorSceneManager.CloseScene(prototypeScene, true);
            }
        }

        private static void CreateSystemsHierarchy()
        {
            GameObject systems = new GameObject("Systems");
            CreateChild(systems, "TourManager");
            CreateChild(systems, "NavigationManager");
            CreateChild(systems, "RoomModeManager");
            CreateChild(systems, "ScreenFader");
        }

        private static void CreateXRHierarchy(Scene destinationScene)
        {
            GameObject interactionManager = new GameObject("XR Interaction Manager");
            interactionManager.AddComponent<XRInteractionManager>();

            GameObject xrOriginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(XROriginPrefabPath);
            if (xrOriginPrefab == null)
                throw new InvalidOperationException($"Missing XR Origin prefab: {XROriginPrefabPath}");

            GameObject xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(xrOriginPrefab, destinationScene);
            xrOrigin.name = "XR Origin";
            xrOrigin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<XRUIInputModule>();

            GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeviceSimulatorPrefabPath);
            if (simulatorPrefab == null)
                throw new InvalidOperationException($"Missing XR Device Simulator prefab: {DeviceSimulatorPrefabPath}");

            GameObject simulator = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab, destinationScene);
            simulator.name = "XR Device Simulator";
        }

        private static void CreateTourHierarchy()
        {
            GameObject panoramaEnvironment = new GameObject("PanoramaEnvironment");
            CreateChild(panoramaEnvironment, "HotspotContainer");
        }

        private static void CreateRoomHierarchy()
        {
            GameObject roomRoot = new GameObject("Room3DRoot");
            CreateChild(roomRoot, "Room301Model");
            CreateChild(roomRoot, "RoomSpawnPoint").transform.localPosition = new Vector3(0f, 0f, -1f);
            CreateChild(roomRoot, "ExitPoint").transform.localPosition = new Vector3(0f, 0f, 1f);
            CreateChild(roomRoot, "TeleportationFloor");
            roomRoot.SetActive(false);
        }

        private static void CreateUIHierarchy()
        {
            GameObject worldUI = new GameObject("WorldUI");
            CreateChild(worldUI, "CurrentLocationPanel");
            CreateChild(worldUI, "FloorMapPanel");
            CreateChild(worldUI, "RoomMenuPanel");
            CreateChild(worldUI, "NavigationPanel");
        }

        private static void CreateLighting()
        {
            GameObject lighting = new GameObject("Lighting");
            GameObject directionalLight = CreateChild(lighting, "Directional Light");
            Light light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            directionalLight.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            if (existingScenes.Any(scene => scene.path == ScenePath))
                return;

            EditorBuildSettings.scenes = existingScenes
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
                .ToArray();
        }
    }
}
