using System;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRTSE.Tour;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSESixNodeTourSetup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string PanoramaFolder = "Assets/Art/Panoramas";
        private const string MaterialFolder = "Assets/Materials/Panoramas";
        private const string NodeFolder = "Assets/Data/TourNodes";
        private const string HotspotPrefabPath = "Assets/Prefabs/Hotspots/Hotspot_PanoramaPortal.prefab";
        private const string SetupCompletedKey = "VRTSE.SixNodeTourCreated.v1";
        private const string LogPrefix = "[VR TSE Six-Node Setup]";

        private static bool s_Queued;

        static VRTSESixNodeTourSetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueSetup();
        }

        [MenuItem("Tools/VR TSE/Expand Tour to Six Nodes")]
        public static void RunSetup()
        {
            if (File.Exists("Assets/Data/TourNodes/P10_WestCorridor.asset"))
            {
                EditorPrefs.SetBool(SetupCompletedKey, true);
                Debug.Log($"{LogPrefix} Rectangular loop exists; preserving its connections.");
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                QueueSetup();
                return;
            }

            try
            {
                EnsureFolder(PanoramaFolder);
                EnsureFolder(MaterialFolder);
                EnsureFolder(NodeFolder);

                NodeDefinition[] definitions =
                {
                    new("P01", "FloorHub", "Floor Hub / Elevator Area",
                        new Color(0.08f, 0.20f, 0.35f), new Color(0.40f, 0.75f, 1f), 0f, new Vector2(0.18f, 0.50f)),
                    new("P02", "CentralLobby", "Central Lobby",
                        new Color(0.08f, 0.32f, 0.22f), new Color(0.45f, 1f, 0.65f), 180f, new Vector2(0.38f, 0.50f)),
                    new("P03", "NorthCorridor", "North Corridor",
                        new Color(0.22f, 0.10f, 0.36f), new Color(0.72f, 0.46f, 1f), 0f, new Vector2(0.58f, 0.50f)),
                    new("P04", "Room301", "In front of Room 301",
                        new Color(0.38f, 0.17f, 0.04f), new Color(1f, 0.62f, 0.20f), 20f, new Vector2(0.78f, 0.34f)),
                    new("P05", "Room302", "In front of Room 302",
                        new Color(0.36f, 0.07f, 0.20f), new Color(1f, 0.40f, 0.70f), -20f, new Vector2(0.78f, 0.66f)),
                    new("P06", "Information", "Information Point",
                        new Color(0.04f, 0.28f, 0.32f), new Color(0.35f, 0.95f, 1f), 90f, new Vector2(0.18f, 0.76f)),
                };

                TourNodeData[] nodes = new TourNodeData[definitions.Length];
                Material[] materials = new Material[definitions.Length];

                for (int index = 0; index < definitions.Length; index++)
                {
                    NodeDefinition definition = definitions[index];
                    string texturePath = $"{PanoramaFolder}/DEV_{definition.Id}_{definition.FileLabel}.png";
                    string materialPath = $"{MaterialFolder}/MAT_{definition.Id}_{definition.FileLabel}.mat";
                    string nodePath = $"{NodeFolder}/{definition.Id}_{definition.FileLabel}.asset";

                    CreateDevelopmentPanorama(texturePath, definition.FloorColor, definition.SkyColor, index);
                    materials[index] = CreatePanoramaMaterial(materialPath, texturePath);
                    nodes[index] = LoadOrCreateNode(nodePath);
                }

                TourNodeData p01 = nodes[0];
                TourNodeData p02 = nodes[1];
                TourNodeData p03 = nodes[2];
                TourNodeData p04 = nodes[3];
                TourNodeData p05 = nodes[4];
                TourNodeData p06 = nodes[5];

                ConfigureNode(p01, definitions[0], materials[0],
                    new TourNodeLink("Go to Central Lobby", p02, Vector3.forward),
                    new TourNodeLink("Go to Information", p06, Vector3.left));

                ConfigureNode(p02, definitions[1], materials[1],
                    new TourNodeLink("Return to Floor Hub", p01, Vector3.back),
                    new TourNodeLink("Go to North Corridor", p03, Vector3.forward));

                ConfigureNode(p03, definitions[2], materials[2],
                    new TourNodeLink("Return to Central Lobby", p02, Vector3.back),
                    new TourNodeLink("Go to Room 301", p04, new Vector3(-0.65f, 0f, 0.76f)),
                    new TourNodeLink("Go to Room 302", p05, new Vector3(0.65f, 0f, 0.76f)));

                ConfigureNode(p04, definitions[3], materials[3],
                    new TourNodeLink("Return to North Corridor", p03, Vector3.back));

                ConfigureNode(p05, definitions[4], materials[4],
                    new TourNodeLink("Return to North Corridor", p03, Vector3.back));

                ConfigureNode(p06, definitions[5], materials[5],
                    new TourNodeLink("Return to Floor Hub", p01, Vector3.right));

                foreach (TourNodeData node in nodes)
                    EditorUtility.SetDirty(node);
                AssetDatabase.SaveAssets();

                ConfigurePrototypeScene(p01);
                EditorPrefs.SetBool(SetupCompletedKey, true);
                Debug.Log($"{LogPrefix} P01-P06 graph is ready in {ScenePath}");
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

        private static void ConfigureNode(
            TourNodeData node,
            NodeDefinition definition,
            Material material,
            params TourNodeLink[] links)
        {
            node.ConfigurePrototypeNode(
                definition.Id,
                definition.DisplayName,
                material,
                definition.Yaw,
                definition.MapPosition,
                links);
        }

        private static void CreateDevelopmentPanorama(
            string assetPath,
            Color floorColor,
            Color skyColor,
            int nodeIndex)
        {
            if (File.Exists(Path.GetFullPath(assetPath)))
                return;

            const int width = 1024;
            const int height = 512;
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                float vertical = y / (float)(height - 1);
                Color rowColor = vertical < 0.48f
                    ? Color.Lerp(floorColor * 0.55f, floorColor, vertical / 0.48f)
                    : Color.Lerp(skyColor, skyColor * 0.45f, (vertical - 0.48f) / 0.52f);

                for (int x = 0; x < width; x++)
                {
                    float band = 0.91f + 0.09f * Mathf.Sin(x / (float)width * Mathf.PI * (8f + nodeIndex));
                    Color pixelColor = rowColor * band;
                    bool horizon = Mathf.Abs(y - height / 2) <= 2;
                    bool forwardMarker = Mathf.Abs(x - width / 2) <= 10 && y > height / 3 && y < height * 2 / 3;

                    if (horizon)
                        pixelColor = Color.white * 0.65f;
                    if (forwardMarker)
                        pixelColor = new Color(1f, 0.72f, 0.10f);

                    pixels[y * width + x] = pixelColor;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static Material CreatePanoramaMaterial(string materialPath, string texturePath)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Skybox/Panoramic");
                if (shader == null)
                    throw new InvalidOperationException("Unity could not find the Skybox/Panoramic shader.");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetFloat("_Mapping", 1f);
            material.SetFloat("_ImageType", 0f);
            material.SetFloat("_Exposure", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static TourNodeData LoadOrCreateNode(string assetPath)
        {
            TourNodeData node = AssetDatabase.LoadAssetAtPath<TourNodeData>(assetPath);
            if (node != null)
                return node;

            node = ScriptableObject.CreateInstance<TourNodeData>();
            AssetDatabase.CreateAsset(node, assetPath);
            return node;
        }

        private static void ConfigurePrototypeScene(TourNodeData p01)
        {
            GameObject hotspotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HotspotPrefabPath);
            if (hotspotPrefab == null)
                throw new InvalidOperationException("The panorama hotspot prefab was not found.");

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject managerObject = FindInScene(scene, "TourManager");
                GameObject hotspotContainer = FindInScene(scene, "HotspotContainer");
                GameObject xrOriginObject = FindInScene(scene, "XR Origin");

                if (managerObject == null || hotspotContainer == null || xrOriginObject == null)
                    throw new InvalidOperationException("The prototype scene hierarchy is incomplete.");

                TourManager manager = managerObject.GetComponent<TourManager>();
                XROrigin xrOrigin = xrOriginObject.GetComponent<XROrigin>();
                if (manager == null || xrOrigin == null)
                    throw new InvalidOperationException("TourManager or XR Origin component was not found.");

                manager.ConfigurePrototype(p01, hotspotContainer.transform, hotspotPrefab, xrOrigin);
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedForSetup && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
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

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private readonly struct NodeDefinition
        {
            public readonly string Id;
            public readonly string FileLabel;
            public readonly string DisplayName;
            public readonly Color FloorColor;
            public readonly Color SkyColor;
            public readonly float Yaw;
            public readonly Vector2 MapPosition;

            public NodeDefinition(
                string id,
                string fileLabel,
                string displayName,
                Color floorColor,
                Color skyColor,
                float yaw,
                Vector2 mapPosition)
            {
                Id = id;
                FileLabel = fileLabel;
                DisplayName = displayName;
                FloorColor = floorColor;
                SkyColor = skyColor;
                Yaw = yaw;
                MapPosition = mapPosition;
            }
        }
    }
}
