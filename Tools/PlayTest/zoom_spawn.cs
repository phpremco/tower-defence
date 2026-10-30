// Zooms the camera onto the start of the road path whose index is in Tools/PlayTest/zoom_arg.txt (a number),
// at the zoom factor in zoom_size.txt; "0" size restores the full view.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

int path = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim());
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
UnityEngine.Vector3 focus = context.Level.Road.Route(path)[0];
rig.SetView(zoom > 0f ? focus : UnityEngine.Vector3.zero, zoom > 0f ? zoom : 1f);
return "zoomed on spawn of path " + path + " at " + focus;
