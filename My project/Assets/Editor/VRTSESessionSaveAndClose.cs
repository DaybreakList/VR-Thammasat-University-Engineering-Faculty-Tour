using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSESessionSaveAndClose
    {
        [Serializable]
        private sealed class Request
        {
            public int processId;
            public string expiresUtc;
        }

        private static readonly string RequestPath = Path.GetFullPath("Temp/VRTSE_SAVE_AND_CLOSE_REQUEST.json");
        private static double nextCheck;

        static VRTSESessionSaveAndClose()
        {
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (EditorApplication.timeSinceStartup < nextCheck)
                return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(RequestPath))
                return;
            Request request = JsonUtility.FromJson<Request>(File.ReadAllText(RequestPath));
            if (request == null || request.processId != Process.GetCurrentProcess().Id ||
                DateTime.UtcNow > DateTime.Parse(request.expiresUtc, null, System.Globalization.DateTimeStyles.RoundtripKind))
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty && string.IsNullOrEmpty(scene.path))
                {
                    UnityEngine.Debug.LogError("[VR TSE Session] Save the untitled scene before closing.");
                    return;
                }
            }
            if (!EditorSceneManager.SaveOpenScenes())
                return;
            AssetDatabase.SaveAssets();
            string statusPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "SESSION_SAVE_STATUS.txt"));
            File.WriteAllText(statusPath, "SAVED\nUTC: " + DateTime.UtcNow.ToString("O") +
                "\nEditor process: " + request.processId + "\nOpen scenes and Assets saved before requesting Editor exit.\n");
            File.Delete(RequestPath);
            UnityEngine.Debug.Log("[VR TSE Session] Open scenes and Assets saved. Closing Editor.");
            EditorApplication.Exit(0);
        }
    }
}
