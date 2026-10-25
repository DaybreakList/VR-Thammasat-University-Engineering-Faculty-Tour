# Project compatibility patch

Embedded from the official PICO Unity OpenXR SDK release_1.4.0,
resolved Git hash 3aa3e62bff41 (see the original package metadata and license).

2026-10-24: Updated the VirtualDisplayDemo_OpenXR and EnterpriseAPI_OpenXR
samples to retrieve composition-layer IDs through
CompositionLayerManager.TryGetLayerId instead of obsolete GetInstanceID.
Both samples wait for a registered layer and a valid Android surface before
calling the corresponding enterprise API.

This embedded package takes precedence over the Git dependency in manifest.json.
Keep this folder in source control to preserve the fix on other computers.
When upgrading the SDK, review whether the upstream version includes this fix.
