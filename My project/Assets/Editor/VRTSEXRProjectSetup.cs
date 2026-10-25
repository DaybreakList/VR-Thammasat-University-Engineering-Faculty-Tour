using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace VRTSE.Editor
{
    /// <summary>
    /// Keeps the prototype's Windows OpenXR baseline reproducible.
    /// The setup is idempotent and can also be rerun from the Tools menu.
    /// </summary>
    [InitializeOnLoad]
    public static class VRTSEXRProjectSetup
    {
        private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string LogPrefix = "[VR TSE XR Setup]";
        private const string SetupCompletedKey = "VRTSE.XR.WindowsOpenXRConfigured.v1";

        private static bool s_Queued;

        static VRTSEXRProjectSetup()
        {
            if (!EditorPrefs.GetBool(SetupCompletedKey, false))
                QueueConfiguration();
        }

        [MenuItem("Tools/VR TSE/Configure Windows OpenXR")]
        public static void ConfigureWindowsOpenXR()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueConfiguration();
                return;
            }

            const BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone;

            try
            {
                XRGeneralSettings generalSettings = GetOrCreateGeneralSettings(buildTargetGroup);
                XRManagerSettings managerSettings = generalSettings.Manager;

                if (managerSettings == null)
                    throw new InvalidOperationException("XR Manager Settings were not created for Standalone.");

                if (!XRPackageMetadataStore.AssignLoader(
                        managerSettings,
                        OpenXRLoaderTypeName,
                        buildTargetGroup))
                {
                    throw new InvalidOperationException("Unity could not assign the OpenXR loader to Standalone.");
                }

                FeatureHelpers.RefreshFeatures(buildTargetGroup);
                EnableInteractionProfile(buildTargetGroup, OculusTouchControllerProfile.featureId);
                EnableInteractionProfile(buildTargetGroup, MetaQuestTouchPlusControllerProfile.featureId);
                EnableInteractionProfile(buildTargetGroup, KHRSimpleControllerProfile.featureId);

                AssetDatabase.SaveAssets();
                ApplyAutomaticValidationFixes(buildTargetGroup);
                AssetDatabase.SaveAssets();

                EditorPrefs.SetBool(SetupCompletedKey, true);
                Debug.Log($"{LogPrefix} Windows OpenXR is configured and validation was checked.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Configuration failed: {exception}");
            }
        }

        private static void QueueConfiguration()
        {
            if (s_Queued)
                return;

            s_Queued = true;
            EditorApplication.delayCall += () =>
            {
                s_Queued = false;
                ConfigureWindowsOpenXR();
            };
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
                throw new InvalidOperationException("Standalone XR General Settings are unavailable.");

            return generalSettings;
        }

        private static void EnableInteractionProfile(
            BuildTargetGroup buildTargetGroup,
            string featureId)
        {
            OpenXRFeature feature =
                FeatureHelpers.GetFeatureWithIdForBuildTarget(buildTargetGroup, featureId);

            if (feature == null)
                throw new InvalidOperationException($"OpenXR feature was not found: {featureId}");

            if (feature.enabled)
                return;

            feature.enabled = true;
            EditorUtility.SetDirty(feature);
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

            foreach (OpenXRFeature.ValidationRule issue in issues)
            {
                string severity = issue.error ? "Error" : "Warning";
                Debug.LogWarning($"{LogPrefix} Remaining OpenXR {severity}: {issue.message}");
            }
        }
    }
}
