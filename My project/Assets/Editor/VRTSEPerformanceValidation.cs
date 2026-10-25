using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRTSE.Tour;

namespace VRTSE.Editor
{
    [InitializeOnLoad]
    public static class VRTSEPerformanceValidation
    {
        private const string Key = "VRTSE.PerformancePoolValidated.v1";

        static VRTSEPerformanceValidation()
        {
            if (!EditorPrefs.GetBool(Key, false))
                EditorApplication.delayCall += RunWhenReady;
        }

        private static void RunWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += RunWhenReady;
                return;
            }
            ValidatePools();
        }

        [MenuItem("Tools/VR TSE/Validate Performance Pools")]
        public static void ValidatePools()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            // Inactive temporary hierarchy: no XR/input lifecycle or rendering changes.
            var host = new GameObject("VRTSE_PerformanceValidation") { hideFlags = HideFlags.HideAndDontSave };
            host.SetActive(false);
            try
            {
                TourNodeData[] nodes = AssetDatabase.FindAssets("t:TourNodeData", new[] { "Assets/Data/TourNodes" })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<TourNodeData>(AssetDatabase.GUIDToAssetPath(guid)))
                    .OrderBy(node => node.NodeId).ToArray();
                if (nodes.Length != 10)
                    throw new InvalidOperationException("The current pool regression check expects the ten-node loop.");

                var container = new GameObject("Hotspots");
                container.transform.SetParent(host.transform, false);
                var roomRoot = new GameObject("Room");
                roomRoot.transform.SetParent(host.transform, false);
                TourManager tour = host.AddComponent<TourManager>();
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hotspots/Hotspot_PanoramaPortal.prefab");
                if (prefab == null)
                    throw new InvalidOperationException("Hotspot prefab is missing.");
                tour.ConfigurePrototype(nodes[0], container.transform, prefab, null);
                MethodInfo rebuild = typeof(TourManager).GetMethod("RebuildHotspots", BindingFlags.Instance | BindingFlags.NonPublic);
                for (int index = 0; index < 500; index++)
                {
                    TourNodeData node = nodes[index % nodes.Length];
                    rebuild.Invoke(tour, new object[] { node });
                    if (tour.ActiveHotspots.Count != node.OutgoingLinks.Count || container.transform.childCount != 2)
                        throw new InvalidOperationException("The hotspot pool grew or lost an active hotspot.");
                    for (int slot = 0; slot < tour.ActiveHotspots.Count; slot++)
                        if (tour.ActiveHotspots[slot].TargetNode != node.OutgoingLinks[slot].TargetNode)
                            throw new InvalidOperationException("A pooled hotspot retained its old target.");
                }

                RoomModeManager room = host.AddComponent<RoomModeManager>();
                GameObject portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hotspots/Hotspot_Room301Entry.prefab");
                if (portalPrefab == null)
                    throw new InvalidOperationException("Room entry prefab is missing.");
                TourNodeData roomNode = nodes.Single(node => node.NodeId == "P04");
                room.ConfigurePrototype(tour, null, null, null, null, null, roomRoot, null,
                    container.transform, roomNode, portalPrefab);
                MethodInfo change = typeof(RoomModeManager).GetMethod("HandleNodeChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                change.Invoke(room, new object[] { roomNode });
                Transform portal = container.transform.Find("Enter_Room301");
                if (portal == null)
                    throw new InvalidOperationException("Room entrance was not created.");
                for (int index = 0; index < 50; index++)
                {
                    change.Invoke(room, new object[] { nodes[0] });
                    if (portal.gameObject.activeSelf)
                        throw new InvalidOperationException("Room entrance remains active away from P04.");
                    change.Invoke(room, new object[] { roomNode });
                    if (!portal.gameObject.activeSelf || container.transform.childCount != 3)
                        throw new InvalidOperationException("Room entrance was duplicated or not re-enabled.");
                }
                VRTSEFloorMapSetup.ValidateLoopRoutes();
                EditorPrefs.SetBool(Key, true);
                Debug.Log("[VR TSE Performance] PASS: 500 hotspot rebuilds kept 2 objects; 50 room-portal revisits kept 1 portal; 100 runtime BFS routes validated.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
