# Prototype performance pass — 2026-10-25

Target: Pico 4 first, with Windows simulator support retained. This is a source
and configuration optimization pass, not a measured headset FPS certification.

## Changes

| Area | Before | After |
| --- | --- | --- |
| Panorama hotspots | Destroy and Instantiate on every node change | Reuse a pool; current loop needs two hotspot objects |
| Room entrance | Recreated on each return to P04 | One cached portal, enabled only at P04 |
| Skybox lighting | DynamicGI.UpdateEnvironment on every move | Disabled by default; optional Inspector switch when needed |
| Highlighted hotspot lookup | Scan hotspot hierarchy and allocate an array | Iterate TourManager's active hotspot list |
| Map route colours | Rescan images, rebuild forward/reverse strings for every image and edge | Cache segments; build canonical edge set once per route update |
| Head-follow UI | Write transforms for all panels every frame | Skip hidden panels and unchanged poses |
| Room point-light shadows | Soft shadows, six faces per light | Disabled for point lights inside Room301; directional shadows retained |
| Mobile URP | HDR, 50 m shadow range, 4 additional lights/object | LDR, 20 m range, 2 additional lights/object |
| PC URP | 50 m shadows, 4 cascades, full-resolution SSAO | 25 m shadows, 2 cascades, downsampled SSAO |
| Android panorama import | Automatic format, no Android override | ASTC 6x6, max 4096, mipmaps, Read/Write off |
| Android build scene | Includes desktop XR Device Simulator prefab | Build processor excludes simulator objects from Android scene copy |
| Release node logging | Every node change | Compiled only in Editor or Development builds |

Panorama import policy applies only to Assets/Art/Panoramas. It is applied to
future images automatically, and existing imports can be updated through
Tools > VR TSE > Optimize Panorama Imports for Pico. All six current texture
metafiles were checked and contain the Android ASTC override. Original image
files are retained. The 4096 maximum does not upscale the 1024-wide placeholders.

For scale only: a 4096x2048 ASTC 6x6 image needs about 3.56 MiB for its top mip,
roughly 4.75 MiB including mip levels, versus about 42.7 MiB for uncompressed
RGBA32 including mip levels. These are theoretical storage figures for a future
4K image, not measured memory savings for this project or the APK.

The room portal was moved to the east of P04 to keep it clear of the north/west
walkway hotspots in the rectangular loop.

## Existing efficient settings retained

- Ten furniture FBX files already use mesh compression level 2, Read/Write off,
  no animation import, no generated colliders, and welded vertices.
- Mobile URP already uses the SRP Batcher and no opaque/depth texture copies.
  Render scale remains 0.8 and MSAA remains 1; these were not lowered further.
- Android quality selects Mobile. Pico build settings already use ARM64,
  IL2CPP, single-pass-instanced OpenXR and no depth submission.
- Pico build script already uses release BuildOptions.None and includes only
  Floor3Prototype. Engine stripping was already enabled.
- Source Samples total about 32.9 MiB of the roughly 39 MiB Assets tree. Sample
  scenes are not enabled in Build Settings. Unreferenced source files are not
  equivalent to APK contents; Resources assets/dependencies need BuildReport
  inspection before making package-size claims.
- Packages, samples, existing builds and user assets have not been deleted.

## Verification

- Runtime and Editor assemblies pass compilation using Unity's generated
  compiler settings. Deprecated build-setting overloads were also updated.
- Ten panorama node/material references and all six underlying texture GUIDs
  resolve after renumbering. All 100 pairs are connected within five moves.
- Unity has reimported the six panorama texture settings.
- An Editor regression check is provided at Tools > VR TSE > Validate
  Performance Pools. It exercises the actual pooling methods in an inactive
  temporary hierarchy: 500 rebuilds, 50 room-portal revisits, and 100 runtime
  BFS routes. It never enters XR Play Mode or changes scene rendering settings.
- This regression check ran in Unity and PASSED. The live Editor.log records:
  `[VR TSE Performance] PASS: 500 hotspot rebuilds kept 2 objects; 50 room-portal revisits kept 1 portal; 100 runtime BFS routes validated.`
  Unity's imported Editor assembly was updated at 00:33 local time on 2026-10-25.
- Interactive Play Mode, Windows/Android player builds, GPU time, thermal
  behaviour, and headset memory have not been certified by these checks.
  Existing EXE/APK files predate this pass and require a new build after testing.

## Acceptance on Pico 4

Run a Development build for profiling, then a release build for final checking.
Test a clockwise and counterclockwise lap, room selection from both sides,
cancel/HOME, map/help toggles, repeated room entry/exit, and teleportation.
Check that UI and panoramas remain readable after ASTC compression, that room
lighting remains acceptable without point shadows, and that there is no portal
overlap at P04.

In Unity Profiler compare CPU/GPU frame time, GC allocations, texture memory,
draw calls and active object counts after warm-up. Hotspot/portal object counts
should stay bounded rather than grow each lap. Test at least 10 minutes on the
headset for sustained behaviour; a desktop simulator does not establish Pico FPS.

## Tuning and reversal

TourManager.updateEnvironmentLighting restores per-node environment relighting.
Mobile_RPAsset fields changed: m_SupportsHDR 1->0, m_ShadowDistance 50->20,
m_AdditionalLightsPerObjectLimit 4->2. PC_RPAsset shadow distance changed 50->25
and cascade count 4->2; PC_Renderer SSAO Downsample changed 0->1.
Point shadows are controlled by RoomModeManager.Awake and VRTSERoom301Setup.
Android texture settings are controlled by VRTSEPanoramaImportSettings.
VRTSEMobileSceneOptimization only affects the Android build-time scene copy.
