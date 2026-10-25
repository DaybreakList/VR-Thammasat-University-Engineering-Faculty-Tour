using System;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRTSE.Tour;

namespace VRTSE.Editor
{
    /// <summary>
    /// Creates the P01/P02 vertical slice with clearly marked development panoramas.
    /// Real 2:1 panorama images can replace these textures without changing runtime code.
    /// </summary>
    [InitializeOnLoad]
    public static class VRTSETwoNodeTourSetup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string PanoramaFolder = "Assets/Art/Panoramas";
        private const string PanoramaMaterialFolder = "Assets/Materials/Panoramas";
        private const string HotspotPrefabFolder = "Assets/Prefabs/Hotspots";
        private const string NodeFolder = "Assets/Data/TourNodes";
        private const string SetupCompletedKey = "VRTSE.TwoNodeTourCreated.v1";
        private const string LogPrefix = "[VR TSE Two-Node Setup]";

        private const string P01TexturePath = PanoramaFolder + "/DEV_P01_FloorHub.png";
        private const string P02TexturePath = PanoramaFolder + "/DEV_P02_CentralLobby.png";
        private const string P01MaterialPath = PanoramaMaterialFolder + "/MAT_P01_FloorHub.mat";
        private const string P02MaterialPath = PanoramaMaterialFolder + "/MAT_P02_CentralLobby.mat";
        private const string HotspotMaterialPath = "Assets/Materials/UI/MAT_Hotspot_Normal.mat";
        private const string HotspotPrefabPath = HotspotPrefabFolder + "/Hotspot_PanoramaPortal.prefab";
        private const string P01NodePath = NodeFolder + "/P01_FloorHub.asset";
        private const string P02NodePath = NodeFolder + "/P02_CentralLobby.asset";

        private static bool s_Queued;

        static VRTSETwoNodeTourSetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueSetup();
        }

        [MenuItem("Tools/VR TSE/Create P01-P02 Tour Slice")]
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
                EnsureFolder(PanoramaMaterialFolder);
                EnsureFolder(HotspotPrefabFolder);
                EnsureFolder(NodeFolder);

                CreateDevelopmentPanorama(P01TexturePath, new Color(0.08f, 0.20f, 0.35f), new Color(0.40f, 0.75f, 1f));
                CreateDevelopmentPanorama(P02TexturePath, new Color(0.08f, 0.32f, 0.22f), new Color(0.45f, 1f, 0.65f));

                Material p01Material = CreatePanoramaMaterial(P01MaterialPath, P01TexturePath);
                Material p02Material = CreatePanoramaMaterial(P02MaterialPath, P02TexturePath);
                GameObject hotspotPrefab = CreateHotspotPrefab();

                TourNodeData p01 = LoadOrCreateNode(P01NodePath);
                TourNodeData p02 = LoadOrCreateNode(P02NodePath);

                p01.ConfigurePrototypeNode(
                    "P01",
                    "Floor Hub / Elevator Area",
                    p01Material,
                    0f,
                    new Vector2(0.18f, 0.50f),
                    new[] { new TourNodeLink("Go to Central Lobby", p02, Vector3.forward) });

                p02.ConfigurePrototypeNode(
                    "P02",
                    "Central Lobby",
                    p02Material,
                    180f,
                    new Vector2(0.42f, 0.50f),
                    new[] { new TourNodeLink("Return to Floor Hub", p01, Vector3.back) });

                EditorUtility.SetDirty(p01);
                EditorUtility.SetDirty(p02);
                AssetDatabase.SaveAssets();

                ConfigurePrototypeScene(p01, hotspotPrefab);
                EditorPrefs.SetBool(SetupCompletedKey, true);
                Debug.Log($"{LogPrefix} P01 ↔ P02 is ready in {ScenePath}");
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

        private static void CreateDevelopmentPanorama(string assetPath, Color floorColor, Color skyColor)
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
                    float longitudeBand = 0.92f + 0.08f * Mathf.Sin(x / (float)width * Mathf.PI * 8f);
                    Color pixelColor = rowColor * longitudeBand;

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
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
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

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_Mapping", 1f);
            material.SetFloat("_ImageType", 0f);
            material.SetFloat("_Exposure", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateHotspotPrefab()
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HotspotPrefabPath);
            if (existingPrefab != null)
                return existingPrefab;

            Material hotspotMaterial = AssetDatabase.LoadAssetAtPath<Material>(HotspotMaterialPath);
            if (hotspotMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                hotspotMaterial = new Material(shader);
                Color blue = new Color(0.05f, 0.55f, 1f);
                hotspotMaterial.color = blue;
                hotspotMaterial.EnableKeyword("_EMISSION");
                hotspotMaterial.SetColor("_EmissionColor", blue * 2f);
                AssetDatabase.CreateAsset(hotspotMaterial, HotspotMaterialPath);
            }

            GameObject temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temporary.name = "Hotspot_PanoramaPortal";
            temporary.transform.localScale = Vector3.one * 0.45f;
            temporary.GetComponent<MeshRenderer>().sharedMaterial = hotspotMaterial;
            temporary.AddComponent<XRSimpleInteractable>();
            temporary.AddComponent<HotspotView>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, HotspotPrefabPath);
            UnityEngine.Object.DestroyImmediate(temporary);
            return prefab;
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

        private static void ConfigurePrototypeScene(TourNodeData p01, GameObject hotspotPrefab)
        {
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
                if (manager == null)
                    manager = managerObject.AddComponent<TourManager>();

                XROrigin xrOrigin = xrOriginObject.GetComponent<XROrigin>();
                if (xrOrigin == null)
                    throw new InvalidOperationException("XR Origin component was not found in the prototype scene.");

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
    }
}
