using System;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using VRTSE.Tour;
using VRTSE.UI;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSERoom301Setup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string EntryPortalPrefabPath = "Assets/Prefabs/Hotspots/Hotspot_Room301Entry.prefab";
        private const string SetupCompletedKey = "VRTSE.Room301PlaceholderCreated.v1";
        private const string LogPrefix = "[VR TSE Room 301 Setup]";

        private static bool s_Queued;

        static VRTSERoom301Setup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueSetup();
        }

        [MenuItem("Tools/VR TSE/Create Room 301 Placeholder")]
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
                Material floorMaterial = CreateMaterial(
                    "Assets/Materials/Room/MAT_Room301_Floor.mat",
                    new Color(0.18f, 0.22f, 0.26f));
                Material wallMaterial = CreateMaterial(
                    "Assets/Materials/Room/MAT_Room301_Walls.mat",
                    new Color(0.76f, 0.79f, 0.76f));
                Material ceilingMaterial = CreateMaterial(
                    "Assets/Materials/Room/MAT_Room301_Ceiling.mat",
                    new Color(0.90f, 0.92f, 0.90f));
                Material entryMaterial = CreateEmissiveMaterial(
                    "Assets/Materials/UI/MAT_RoomPortal_Enter.mat",
                    new Color(0.05f, 0.85f, 0.38f));
                Material exitMaterial = CreateEmissiveMaterial(
                    "Assets/Materials/UI/MAT_RoomPortal_Exit.mat",
                    new Color(1f, 0.18f, 0.08f));

                GameObject entryPortalPrefab = CreateEntryPortalPrefab(entryMaterial);

                Scene scene = SceneManager.GetSceneByPath(ScenePath);
                bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
                if (openedForSetup)
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

                try
                {
                    ConfigureScene(
                        scene,
                        floorMaterial,
                        wallMaterial,
                        ceilingMaterial,
                        exitMaterial,
                        entryPortalPrefab);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                finally
                {
                    if (openedForSetup && scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }

                EditorPrefs.SetBool(SetupCompletedKey, true);
                Debug.Log($"{LogPrefix} Empty room shell, teleport floor, entry, and exit are ready in {ScenePath}");
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

        private static void ConfigureScene(
            Scene scene,
            Material floorMaterial,
            Material wallMaterial,
            Material ceilingMaterial,
            Material exitMaterial,
            GameObject entryPortalPrefab)
        {
            GameObject roomManagerObject = FindInScene(scene, "RoomModeManager");
            GameObject tourManagerObject = FindInScene(scene, "TourManager");
            GameObject screenFaderObject = FindInScene(scene, "ScreenFader");
            GameObject locationPanel = FindInScene(scene, "CurrentLocationPanel");
            GameObject xrOriginObject = FindInScene(scene, "XR Origin");
            GameObject panoramaEnvironment = FindInScene(scene, "PanoramaEnvironment");
            GameObject worldUI = FindInScene(scene, "WorldUI");
            GameObject roomRoot = FindInScene(scene, "Room3DRoot");
            GameObject roomModelRoot = FindInScene(scene, "Room301Model");
            GameObject roomSpawnPoint = FindInScene(scene, "RoomSpawnPoint");
            GameObject exitPoint = FindInScene(scene, "ExitPoint");
            GameObject teleportFloor = FindInScene(scene, "TeleportationFloor");
            GameObject hotspotContainer = FindInScene(scene, "HotspotContainer");

            if (roomManagerObject == null || tourManagerObject == null || screenFaderObject == null ||
                locationPanel == null || xrOriginObject == null || panoramaEnvironment == null ||
                worldUI == null || roomRoot == null || roomModelRoot == null ||
                roomSpawnPoint == null || exitPoint == null || teleportFloor == null ||
                hotspotContainer == null)
                throw new InvalidOperationException("The Room 301 prototype hierarchy is incomplete.");

            CreateRoomShell(roomModelRoot.transform, wallMaterial, ceilingMaterial);
            CreateRoomLights(roomModelRoot.transform);
            ConfigureTeleportFloor(teleportFloor.transform, floorMaterial);

            var roomModeManager = roomManagerObject.GetComponent<RoomModeManager>();
            if (roomModeManager == null)
                roomModeManager = roomManagerObject.AddComponent<RoomModeManager>();

            ConfigureSpawnAndExit(roomSpawnPoint.transform, exitPoint.transform, exitMaterial, roomModeManager);

            TourManager tourManager = tourManagerObject.GetComponent<TourManager>();
            ScreenFader screenFader = screenFaderObject.GetComponent<ScreenFader>();
            FloorUIController floorUI = locationPanel.GetComponent<FloorUIController>();
            XROrigin xrOrigin = xrOriginObject.GetComponent<XROrigin>();
            TourNodeData p04 = AssetDatabase.LoadAssetAtPath<TourNodeData>(
                "Assets/Data/TourNodes/P04_Room301.asset");

            if (tourManager == null || screenFader == null || floorUI == null || xrOrigin == null || p04 == null)
                throw new InvalidOperationException("Room 301 system references are incomplete.");

            roomModeManager.ConfigurePrototype(
                tourManager,
                screenFader,
                floorUI,
                xrOrigin,
                panoramaEnvironment,
                worldUI,
                roomRoot,
                roomSpawnPoint.transform,
                hotspotContainer.transform,
                p04,
                entryPortalPrefab);

            roomRoot.SetActive(false);
            EditorUtility.SetDirty(roomModeManager);
            EditorUtility.SetDirty(roomRoot);
        }

        private static void CreateRoomShell(Transform modelRoot, Material wallMaterial, Material ceilingMaterial)
        {
            Transform shell = FindOrCreateTransform(modelRoot, "GeneratedRoomShell");
            CreateBox(shell, "BackWall", new Vector3(0f, 1.5f, 4f), new Vector3(10f, 3f, 0.16f), wallMaterial);
            CreateBox(shell, "LeftWall", new Vector3(-5f, 1.5f, 0f), new Vector3(0.16f, 3f, 8f), wallMaterial);
            CreateBox(shell, "RightWall", new Vector3(5f, 1.5f, 0f), new Vector3(0.16f, 3f, 8f), wallMaterial);
            CreateBox(shell, "FrontWallLeft", new Vector3(-3f, 1.5f, -4f), new Vector3(4f, 3f, 0.16f), wallMaterial);
            CreateBox(shell, "FrontWallRight", new Vector3(3f, 1.5f, -4f), new Vector3(4f, 3f, 0.16f), wallMaterial);
            CreateBox(shell, "DoorHeader", new Vector3(0f, 2.6f, -4f), new Vector3(2f, 0.8f, 0.16f), wallMaterial);
            CreateBox(shell, "Ceiling", new Vector3(0f, 3.05f, 0f), new Vector3(10f, 0.1f, 8f), ceilingMaterial);
        }

        private static void CreateRoomLights(Transform modelRoot)
        {
            Transform lightsRoot = FindOrCreateTransform(modelRoot, "RoomLights");
            CreatePointLight(lightsRoot, "CeilingLight_A", new Vector3(-2.5f, 2.65f, 0f));
            CreatePointLight(lightsRoot, "CeilingLight_B", new Vector3(2.5f, 2.65f, 0f));
        }

        private static void ConfigureTeleportFloor(Transform teleportFloor, Material floorMaterial)
        {
            Transform surface = teleportFloor.Find("TeleportSurface");
            GameObject surfaceObject;
            if (surface == null)
            {
                surfaceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                surfaceObject.name = "TeleportSurface";
                surfaceObject.transform.SetParent(teleportFloor, false);
            }
            else
            {
                surfaceObject = surface.gameObject;
            }

            surfaceObject.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            surfaceObject.transform.localRotation = Quaternion.identity;
            surfaceObject.transform.localScale = new Vector3(10f, 0.1f, 8f);
            surfaceObject.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
            if (surfaceObject.GetComponent<TeleportationArea>() == null)
                surfaceObject.AddComponent<TeleportationArea>();
        }

        private static void ConfigureSpawnAndExit(
            Transform spawnPoint,
            Transform exitPoint,
            Material exitMaterial,
            RoomModeManager roomModeManager)
        {
            spawnPoint.localPosition = new Vector3(0f, 1.6f, -2.8f);
            spawnPoint.localRotation = Quaternion.identity;

            exitPoint.localPosition = new Vector3(0f, 1.35f, -3.35f);
            exitPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Transform existing = exitPoint.Find("ExitRoomPortal");
            GameObject portal;
            if (existing == null)
            {
                portal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                portal.name = "ExitRoomPortal";
                portal.transform.SetParent(exitPoint, false);
                portal.AddComponent<XRSimpleInteractable>();
                portal.AddComponent<RoomPortalInteractable>();
            }
            else
            {
                portal = existing.gameObject;
            }

            portal.transform.localPosition = Vector3.zero;
            portal.transform.localRotation = Quaternion.identity;
            portal.transform.localScale = Vector3.one * 0.55f;
            portal.GetComponent<MeshRenderer>().sharedMaterial = exitMaterial;
            portal.GetComponent<RoomPortalInteractable>().Configure(roomModeManager, true);
            CreatePortalLabel(portal.transform, "EXIT TO P04", Quaternion.identity);
        }

        private static GameObject CreateEntryPortalPrefab(Material entryMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(EntryPortalPrefabPath);
            if (existing != null)
                return existing;

            GameObject temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temporary.name = "Hotspot_Room301Entry";
            temporary.transform.localScale = Vector3.one * 0.55f;
            temporary.GetComponent<MeshRenderer>().sharedMaterial = entryMaterial;
            temporary.AddComponent<XRSimpleInteractable>();
            temporary.AddComponent<RoomPortalInteractable>();
            CreatePortalLabel(temporary.transform, "ENTER ROOM 301", Quaternion.identity);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, EntryPortalPrefabPath);
            UnityEngine.Object.DestroyImmediate(temporary);
            return prefab;
        }

        private static void CreatePortalLabel(Transform portal, string label, Quaternion localRotation)
        {
            Transform existing = portal.Find("LabelCanvas");
            GameObject canvasObject = existing != null
                ? existing.gameObject
                : new GameObject("LabelCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(portal, false);

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(420f, 80f);
            canvasRect.localPosition = new Vector3(0f, 0.78f, 0f);
            canvasRect.localRotation = localRotation;
            canvasRect.localScale = Vector3.one * 0.0025f;

            Canvas canvas = GetOrAdd<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            Transform textTransform = canvasObject.transform.Find("Label");
            GameObject textObject = textTransform != null
                ? textTransform.gameObject
                : new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            Stretch(textRect);

            Text text = GetOrAdd<Text>(textObject);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private static GameObject CreateBox(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            Transform existing = parent.Find(objectName);
            GameObject box;
            if (existing == null)
            {
                box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = objectName;
                box.transform.SetParent(parent, false);
            }
            else
            {
                box = existing.gameObject;
            }

            box.transform.localPosition = localPosition;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = localScale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static void CreatePointLight(Transform parent, string objectName, Vector3 localPosition)
        {
            Transform existing = parent.Find(objectName);
            GameObject lightObject = existing != null ? existing.gameObject : new GameObject(objectName);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            Light light = GetOrAdd<Light>(lightObject);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = 3f;
            light.range = 7f;
            // Point-light shadows render six cubemap faces per light.
            light.shadows = LightShadows.None;
        }

        private static Material CreateMaterial(string assetPath, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    throw new InvalidOperationException("The URP Lit shader was not found.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.color = color;
            material.SetFloat("_Smoothness", 0.18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateEmissiveMaterial(string assetPath, Color color)
        {
            Material material = CreateMaterial(assetPath, color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform FindOrCreateTransform(Transform parent, string objectName)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
                return existing;

            var child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            return child.transform;
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
    }
}
