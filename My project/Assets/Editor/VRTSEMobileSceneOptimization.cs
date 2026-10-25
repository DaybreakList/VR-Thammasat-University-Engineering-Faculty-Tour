using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRTSE.Editor
{
    // Operates on Unity's build-time scene copy. Desktop simulator testing stays available.
    public sealed class VRTSEMobileSceneOptimization : IProcessSceneWithReport
    {
        public int callbackOrder => 100;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.Android ||
                scene.path != "Assets/Scenes/Prototype/Floor3Prototype.unity")
                return;

            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                        continue;
                    string type = behaviour.GetType().Name;
                    if (type != "XRDeviceSimulator" && type != "XRInteractionSimulator")
                        continue;
                    Object.DestroyImmediate(behaviour.gameObject);
                    removed++;
                }
            }
            Debug.Log($"[VR TSE Performance] Excluded {removed} desktop simulator objects from the Android scene copy.");
        }
    }
}
