# Rectangular panorama walkway prototype

Current layout (each point is a 360 panorama location):

```text
P09 INFO ---- P08 ---- P07 ROOM 302 ---- P06
   |                                    |
  P10                                  P05
   |                                    |
P01 HOME ---- P02 ---- P03 ------------ P04 ROOM 301
```

All ten segments can be traversed in both directions. The perimeter follows P01-P02-P03-P04-P05-P06-P07-P08-P09-P10-P01.
Room 301 remains attached to P04 and its existing 3D interior is preserved.
HOME is a separate action that cancels guidance and returns to P01.
Selecting a room calculates a route from the current node without moving home.

The graph lives in Assets/Data/TourNodes. The new nodes are P05 East Corridor,
P06 North-East Corner, P08 North Walkway and P10 West Corridor. Every node has
its own panorama material. New materials currently reuse a tinted development
texture as a placeholder. Replace their MainTex with real 2:1 panorama images
and adjust initialYaw when real photographs are available.

To add more photograph locations, create a TourNodeData asset and material,
place its normalized MapPosition on the appropriate side, and split the old
neighbour link into two reciprocal connections through the new point.
Set LocalDirection consistently with the walkway. Navigation traverses the
links; the map now reads the same graph rather than a hard-coded six-point list.

In Unity, leave Play Mode and choose Assets > Refresh. The map setup runs once
after import. Alternatively choose Tools > VR TSE > Create Floor 3 Map and Marker.
Tools > VR TSE > Validate Rectangular Loop Routes validates the loaded assets
using the runtime NavigationManager.FindShortestRoute implementation.

Validation before import: all ten node GUIDs and reciprocal axis-aligned links
were checked; all 100 node pairs are connected in at most five moves. The
updated Editor assembly passed isolated compilation with Unity's compiler.
Unity scene import and Play Mode acceptance still require verification.

Play Mode checks: complete a full clockwise and counterclockwise lap; select
Room 302 from P01 (P01-P10-P09-P08-P07); select Room 302 from P04
(P04-P05-P06-P07); change direction while navigating; cancel; use HOME;
enter/exit Room 301 at P04; verify map position and route colours at every node.
Rebuild Windows/Android only after these checks. Existing builds are older.
