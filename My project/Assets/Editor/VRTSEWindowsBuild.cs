using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSEWindowsBuild
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string LogPrefix = "[VR TSE Windows Build]";
        private const string ExecutableName = "VR_TSE_Virtual_Tour.exe";

        private static bool s_Building;
        private static double s_NextCheck;

        [Serializable]
        private sealed class BuildRequest
        {
            public int processId;
            public string outputRoot;
        }

        static VRTSEWindowsBuild()
        {
            EditorApplication.update += CheckRequestedBuild;
        }

        private static void CheckRequestedBuild()
        {
            if (s_Building || EditorApplication.timeSinceStartup < s_NextCheck)
                return;
            s_NextCheck = EditorApplication.timeSinceStartup + 2;
            string requestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "WINDOWS_BUILD_REQUESTED.json"));
            if (!File.Exists(requestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;
            BuildRequest request = JsonUtility.FromJson<BuildRequest>(File.ReadAllText(requestPath));
            if (request == null || request.processId != System.Diagnostics.Process.GetCurrentProcess().Id)
                return;
            if (!EditorSceneManager.SaveOpenScenes())
                return;
            AssetDatabase.SaveAssets();
            File.Delete(requestPath);
            RunBuild(request.outputRoot);
        }

        [MenuItem("Tools/VR TSE/Build Windows x86-64")]
        public static void BuildFromMenu()
        {
            RunBuild();
        }

        private static void RunBuild(string requestedRoot = null)
        {
            if (s_Building)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                Debug.LogWarning($"{LogPrefix} Stop Play Mode and wait for Unity to finish compiling before building.");
                return;
            }

            s_Building = true;
            try
            {
                string absoluteScenePath = Path.Combine(Application.dataPath, "Scenes/Prototype/Floor3Prototype.unity");
                if (!File.Exists(absoluteScenePath))
                    throw new FileNotFoundException("Floor3Prototype scene was not found.", absoluteScenePath);

                // Preserve SampleScene in the project, but make the prototype the only startup scene in this build.
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(ScenePath, true),
                };

                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                        BuildTargetGroup.Standalone,
                        BuildTarget.StandaloneWindows64))
                {
                    throw new InvalidOperationException("Unity could not switch to the Windows x86-64 build target.");
                }

                string buildRoot = requestedRoot;
                if (string.IsNullOrWhiteSpace(buildRoot))
                    buildRoot = Environment.GetEnvironmentVariable("VRTSE_BUILD_ROOT");
                if (string.IsNullOrWhiteSpace(buildRoot))
                    buildRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds"));
                string outputDirectory = Path.GetFullPath(Path.Combine(buildRoot, "Windows"));
                Directory.CreateDirectory(outputDirectory);

                string executablePath = Path.Combine(outputDirectory, ExecutableName);
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = executablePath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None,
                };

                Debug.Log($"{LogPrefix} Building {ScenePath} to {executablePath}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                WriteBuildReport(outputDirectory, summary);

                if (summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Windows build finished with result {summary.result}; " +
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
                if (Application.isBatchMode)
                    throw;
            }
            finally
            {
                s_Building = false;
            }
        }

        private static void WriteBuildReport(string outputDirectory, BuildSummary summary)
        {
            string reportPath = Path.Combine(outputDirectory, "BUILD_REPORT.txt");
            string contents =
                $"VR TSE Windows x86-64 Build{Environment.NewLine}" +
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}{Environment.NewLine}" +
                $"Scene: {ScenePath}{Environment.NewLine}" +
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
