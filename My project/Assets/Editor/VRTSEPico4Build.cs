using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace VRTSE.Editor
{
    /// <summary>
    /// Configures the Android/OpenXR baseline for PICO 4 and creates a sideloadable APK.
    /// The request marker lets the already-open Editor continue once Play Mode and imports stop.
    /// </summary>
    [InitializeOnLoad]
    public static class VRTSEPico4Build
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string PicoSupportFeatureId = "com.unity.openxr.feature.pico";
        private const string PicoExtensionsFeatureId = "com.unity.openxr.pico.features";
        private const string Pico4ControllerFeatureId = "com.unity.openxr.feature.input.PICO4touch";
        private const string LogPrefix = "[VR TSE PICO 4 Build]";
        private const string ApkName = "VR_TSE_Virtual_Tour_Pico4.apk";
        private const string RequestMarkerName = "PICO4_BUILD_REQUESTED.txt";

        private static bool s_Building;
        private static double s_NextAttemptTime;
        private static bool s_WaitMessageLogged;

        static VRTSEPico4Build()
        {
            EditorApplication.update += TryRunRequestedBuild;
        }

        [MenuItem("Tools/VR TSE/Configure Pico 4 Android")]
        public static void ConfigureFromMenu()
        {
            ConfigurePico4Android();
        }

        [MenuItem("Tools/VR TSE/Build Pico 4 APK")]
        public static void BuildFromMenu()
        {
            RunBuild();
        }

        private static void TryRunRequestedBuild()
        {
            if (EditorApplication.timeSinceStartup < s_NextAttemptTime)
                return;

            s_NextAttemptTime = EditorApplication.timeSinceStartup + 2.0;
            string markerPath = GetRequestMarkerPath();
            if (!File.Exists(markerPath) || s_Building)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                if (!s_WaitMessageLogged)
                {
                    s_WaitMessageLogged = true;
                    Debug.Log($"{LogPrefix} Waiting for Play Mode/import/compilation to finish.");
                }
                return;
            }

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            if (FeatureHelpers.GetFeatureWithIdForBuildTarget(
                    BuildTargetGroup.Android,
                    PicoSupportFeatureId) == null)
            {
                if (!s_WaitMessageLogged)
                {
                    s_WaitMessageLogged = true;
                    Debug.Log($"{LogPrefix} Waiting for the PICO OpenXR package to import.");
                }
                return;
            }

            s_WaitMessageLogged = false;
            File.Delete(markerPath);
            RunBuild();
        }

        private static void RunBuild()
        {
            if (s_Building)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                Debug.LogWarning($"{LogPrefix} Stop Play Mode and wait for Unity to finish importing before building.");
                return;
            }

            s_Building = true;
            try
            {
                ConfigurePico4Android();

                string absoluteScenePath = Path.Combine(
                    Application.dataPath,
                    "Scenes/Prototype/Floor3Prototype.unity");
                if (!File.Exists(absoluteScenePath))
                    throw new FileNotFoundException("Floor3Prototype scene was not found.", absoluteScenePath);

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenePath, true),
                };

                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Android,
                        BuildTarget.Android))
                {
                    throw new InvalidOperationException("Unity could not switch to the Android build target.");
                }

                string buildRoot = Environment.GetEnvironmentVariable("VRTSE_BUILD_ROOT");
                if (string.IsNullOrWhiteSpace(buildRoot))
                {
                    buildRoot = Path.GetFullPath(
                        Path.Combine(Application.dataPath, "..", "..", "Builds"));
                }

                string outputDirectory = Path.Combine(buildRoot, "Android");
                Directory.CreateDirectory(outputDirectory);

                string apkPath = Path.Combine(outputDirectory, ApkName);
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                };

                Debug.Log($"{LogPrefix} Building {ScenePath} to {apkPath}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                WriteBuildReport(outputDirectory, summary);

                if (summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Android build finished with result {summary.result}; " +
                        $"errors={summary.totalErrors}, warnings={summary.totalWarnings}.");
                }

                Debug.Log(
                    $"{LogPrefix} Success: {summary.outputPath} " +
                    $"({summary.totalSize.ToString("N0", CultureInfo.InvariantCulture)} bytes, " +
                    $"{summary.totalTime.TotalSeconds:F1} seconds)");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Failed: {exception}");
            }
            finally
            {
                s_Building = false;
            }
        }

        private static void ConfigurePico4Android()
        {
            ConfigureExternalTools();

            PlayerSettings.companyName = "VR TSE";
            PlayerSettings.productName = "VR TSE Virtual Tour";
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.Android,
                "com.vrtse.virtualtour");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.Android,
                new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;

            XRGeneralSettings generalSettings = GetOrCreateGeneralSettings(BuildTargetGroup.Android);
            XRManagerSettings managerSettings = generalSettings.Manager;
            if (managerSettings == null)
                throw new InvalidOperationException("XR Manager Settings were not created for Android.");

            if (!XRPackageMetadataStore.AssignLoader(
                    managerSettings,
                    OpenXRLoaderTypeName,
                    BuildTargetGroup.Android) &&
                !HasOpenXRLoader(managerSettings))
            {
                throw new InvalidOperationException("Unity could not assign the OpenXR loader to Android.");
            }

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            OpenXRSettings openXrSettings =
                OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXrSettings == null)
                throw new InvalidOperationException("Android OpenXR settings are unavailable.");

            foreach (OpenXRInteractionFeature interaction in
                     openXrSettings.GetFeatures<OpenXRInteractionFeature>())
            {
                bool shouldEnable = GetFeatureId(interaction) == Pico4ControllerFeatureId;
                if (interaction.enabled != shouldEnable)
                {
                    interaction.enabled = shouldEnable;
                    EditorUtility.SetDirty(interaction);
                }
            }

            EnableFeature(PicoSupportFeatureId);
            EnableFeature(PicoExtensionsFeatureId);
            EnableFeature(Pico4ControllerFeatureId);

            openXrSettings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            openXrSettings.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            openXrSettings.optimizeBufferDiscards = true;
            EditorUtility.SetDirty(openXrSettings);

            AssetDatabase.SaveAssets();
            ApplyAutomaticValidationFixes(BuildTargetGroup.Android);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"{LogPrefix} Configured Android API 29+, ARM64/IL2CPP, " +
                "Vulkan + OpenGLES3, OpenXR, PICO Support and PICO 4 controllers.");
        }

        private static void ConfigureExternalTools()
        {
            string androidPlayerRoot = Path.Combine(
                EditorApplication.applicationContentsPath,
                "PlaybackEngines",
                "AndroidPlayer");
            string jdkRoot = Environment.GetEnvironmentVariable("VRTSE_JDK_ROOT");
            if (string.IsNullOrWhiteSpace(jdkRoot))
            {
                string workspaceRoot = Path.GetFullPath(
                    Path.Combine(Application.dataPath, "..", ".."));
                jdkRoot = Path.Combine(workspaceRoot, "Tools", "OpenJDK");
            }
            string sdkRoot = Path.Combine(androidPlayerRoot, "SDK");
            string ndkRoot = Path.Combine(androidPlayerRoot, "NDK");

            RequireFile(Path.Combine(jdkRoot, "bin", "java.exe"), "OpenJDK");
            RequireFile(Path.Combine(sdkRoot, "platform-tools", "adb.exe"), "Android SDK");
            RequireFile(Path.Combine(ndkRoot, "source.properties"), "Android NDK");

            AndroidExternalToolsSettings.jdkRootPath = jdkRoot;
            AndroidExternalToolsSettings.sdkRootPath = sdkRoot;
            AndroidExternalToolsSettings.ndkRootPath = ndkRoot;
        }

        private static void RequireFile(string path, string componentName)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{componentName} is missing.", path);
        }

        private static XRGeneralSettings GetOrCreateGeneralSettings(BuildTargetGroup buildTargetGroup)
        {
            XRGeneralSettings generalSettings =
                XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (generalSettings != null && generalSettings.Manager != null)
                return generalSettings;

            MethodInfo getOrCreateMethod = typeof(XRGeneralSettingsPerBuildTarget).GetMethod(
                "GetOrCreate",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (getOrCreateMethod == null)
                throw new MissingMethodException(
                    typeof(XRGeneralSettingsPerBuildTarget).FullName,
                    "GetOrCreate");

            var perBuildTarget =
                getOrCreateMethod.Invoke(null, null) as XRGeneralSettingsPerBuildTarget;
            if (perBuildTarget == null)
                throw new InvalidOperationException("Unity could not create XR settings for build targets.");

            if (!perBuildTarget.HasManagerSettingsForBuildTarget(buildTargetGroup))
                perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);

            generalSettings = perBuildTarget.SettingsForBuildTarget(buildTargetGroup);
            if (generalSettings == null)
                throw new InvalidOperationException("Android XR General Settings are unavailable.");

            return generalSettings;
        }

        private static bool HasOpenXRLoader(XRManagerSettings managerSettings)
        {
            foreach (XRLoader loader in managerSettings.activeLoaders)
            {
                if (loader != null && loader.GetType().FullName == OpenXRLoaderTypeName)
                    return true;
            }

            return false;
        }

        private static void EnableFeature(string featureId)
        {
            OpenXRFeature feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(
                BuildTargetGroup.Android,
                featureId);
            if (feature == null)
                throw new InvalidOperationException($"OpenXR feature was not found: {featureId}");

            if (!feature.enabled)
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
        }

        private static string GetFeatureId(OpenXRFeature feature)
        {
            Type featureType = feature.GetType();
            object[] attributes = featureType.GetCustomAttributes(
                typeof(OpenXRFeatureAttribute),
                false);
            if (attributes.Length == 0)
                return string.Empty;

            return ((OpenXRFeatureAttribute)attributes[0]).FeatureId;
        }

        private static void ApplyAutomaticValidationFixes(BuildTargetGroup buildTargetGroup)
        {
            var issues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, buildTargetGroup);

            foreach (OpenXRFeature.ValidationRule issue in issues)
            {
                if (issue.fixItAutomatic && issue.fixIt != null)
                    issue.fixIt.Invoke();
            }

            issues.Clear();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, buildTargetGroup);

            var errors = new List<string>();
            foreach (OpenXRFeature.ValidationRule issue in issues)
            {
                string severity = issue.error ? "Error" : "Warning";
                Debug.LogWarning($"{LogPrefix} Remaining OpenXR {severity}: {issue.message}");
                if (issue.error)
                    errors.Add(issue.message);
            }

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "OpenXR validation still has errors: " + string.Join(" | ", errors));
            }
        }

        private static string GetRequestMarkerPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", RequestMarkerName));
        }

        private static void WriteBuildReport(string outputDirectory, BuildSummary summary)
        {
            string reportPath = Path.Combine(outputDirectory, "BUILD_REPORT.txt");
            string contents =
                $"VR TSE PICO 4 Android Build{Environment.NewLine}" +
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}{Environment.NewLine}" +
                $"Scene: {ScenePath}{Environment.NewLine}" +
                $"Package: com.vrtse.virtualtour{Environment.NewLine}" +
                $"Architecture: ARM64{Environment.NewLine}" +
                $"ScriptingBackend: IL2CPP{Environment.NewLine}" +
                $"Result: {summary.result}{Environment.NewLine}" +
                $"Output: {summary.outputPath}{Environment.NewLine}" +
                $"SizeBytes: {summary.totalSize}{Environment.NewLine}" +
                $"Duration: {summary.totalTime}{Environment.NewLine}" +
                $"Warnings: {summary.totalWarnings}{Environment.NewLine}" +
                $"Errors: {summary.totalErrors}{Environment.NewLine}";
            File.WriteAllText(reportPath, contents);
        }
    }
}
