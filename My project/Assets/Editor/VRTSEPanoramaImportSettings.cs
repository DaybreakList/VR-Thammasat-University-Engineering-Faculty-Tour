using System;
using UnityEditor;
using UnityEngine;

namespace VRTSE.Editor
{
    // Apply only to tour panoramas, not controller/UI textures or vendor samples.
    [InitializeOnLoad]
    public sealed class VRTSEPanoramaImportSettings : AssetPostprocessor
    {
        private const string PanoramaFolder = "Assets/Art/Panoramas";
        private const string CompletedKey = "VRTSE.PanoramaASTC.v1";

        static VRTSEPanoramaImportSettings()
        {
            if (!EditorPrefs.GetBool(CompletedKey, false))
                EditorApplication.delayCall += ApplyWhenReady;
        }

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(PanoramaFolder + "/", StringComparison.Ordinal))
                Configure((TextureImporter)assetImporter);
        }

        private static void Configure(TextureImporter importer)
        {
            // Skybox images do not need a second, CPU-readable pixel copy.
            importer.isReadable = false;
            importer.mipmapEnabled = true;
            var android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 4096;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.compressionQuality = 80;
            android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android);
        }

        private static void ApplyWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ApplyWhenReady;
                return;
            }
            ApplyExistingPanoramas();
        }

        [MenuItem("Tools/VR TSE/Optimize Panorama Imports for Pico")]
        public static void ApplyExistingPanoramas()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[VR TSE Performance] Leave Play Mode and wait for imports before optimizing textures.");
                return;
            }

            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PanoramaFolder }))
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is not TextureImporter importer)
                    continue;
                var android = importer.GetPlatformTextureSettings("Android");
                if (!importer.isReadable && importer.mipmapEnabled && android.overridden &&
                    android.maxTextureSize == 4096 && android.format == TextureImporterFormat.ASTC_6x6 &&
                    android.compressionQuality == 80 && !android.crunchedCompression)
                    continue;
                Configure(importer);
                importer.SaveAndReimport();
                changed++;
            }
            EditorPrefs.SetBool(CompletedKey, true);
            Debug.Log($"[VR TSE Performance] Optimized {changed} panorama imports: Android ASTC 6x6, max 4096, no CPU pixel copy.");
        }
    }
}
