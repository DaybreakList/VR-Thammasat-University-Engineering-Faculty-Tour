using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSERoom301FurnitureCleanup
    {
        private const string ScenePath = "Assets/Scenes/Prototype/Floor3Prototype.unity";
        private const string CleanupCompletedKey = "VRTSE.Room301FurnitureRemoved.v1";
        private const string LogPrefix = "[VR TSE Room 301 Cleanup]";

        private static bool s_Queued;

        static VRTSERoom301FurnitureCleanup()
        {
            if (!EditorPrefs.GetBool(CleanupCompletedKey, false))
                QueueCleanup();
        }

        [MenuItem("Tools/VR TSE/Remove Room 301 Placeholder Furniture")]
        public static void RunCleanup()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                QueueCleanup();
                return;
            }

            try
            {
                Scene scene = SceneManager.GetSceneByPath(ScenePath);
                bool openedForCleanup = !scene.IsValid() || !scene.isLoaded;
                if (openedForCleanup)
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

                try
                {
                    GameObject roomModel = FindInScene(scene, "Room301Model");
                    if (roomModel == null)
                        throw new InvalidOperationException("Room301Model was not found.");

                    Transform generatedFurniture = roomModel.transform.Find("KenneyFurniture");
                    if (generatedFurniture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(generatedFurniture.gameObject);
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        Debug.Log($"{LogPrefix} Removed the generated KenneyFurniture scene objects. Source FBX files were preserved.");
                    }
                    else
                    {
                        Debug.Log($"{LogPrefix} No generated furniture remained in the scene.");
                    }
                }
                finally
                {
                    if (openedForCleanup && scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }

                EditorPrefs.SetBool(CleanupCompletedKey, true);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} Cleanup failed: {exception}");
            }
        }

        private static void QueueCleanup()
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
            RunCleanup();
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
    }
}
