// Builds a display model of every tower asset (not blockades) along the main route from the spawn, level 0 on one
// side of the road and level 2 on the other, and aims the camera at the middle with the orthographic size from
// zoom_size.txt. Editor only.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
float half = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var old = UnityEngine.GameObject.Find("TowerShowcase");
if (old != null) UnityEngine.Object.Destroy(old);
var root = new UnityEngine.GameObject("TowerShowcase").transform;
UnityEngine.Vector3[] route = context.Level.Road.Route(0);
// Points every 1.3 units along the polyline, starting a little past the spawn.
var points = new System.Collections.Generic.List<(UnityEngine.Vector3 At, UnityEngine.Vector3 Along)>();
float next = 2.5f;
float walked = 0f;
for (int i = 0; i < route.Length - 1; i++)
{
    UnityEngine.Vector3 a = route[i];
    UnityEngine.Vector3 b = route[i + 1];
    float length = UnityEngine.Vector3.Distance(a, b);
    while (next <= walked + length)
    {
        points.Add((UnityEngine.Vector3.Lerp(a, b, (next - walked) / length), (b - a).normalized));
        next += 1.3f;
    }
    walked += length;
}
string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TowerDefinition", new[] { "Assets/_Project/Data/Towers" });
int index = 0;
foreach (string guid in guids)
{
    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (definition == null || definition.BlocksGroundEnemies || !Game.Towers.TowerModelBuilder.HasModel(definition)) continue;
    if (index >= points.Count) break;
    UnityEngine.Vector3 side = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, points[index].Along).normalized;
    foreach (int level in new[] { 0, 2 })
    {
        var model = Game.Towers.TowerModelBuilder.Build(root, definition, level);
        model.transform.position = points[index].At + side * (level == 0 ? -0.6f : 0.8f);
        model.transform.rotation = UnityEngine.Quaternion.LookRotation(-side);
    }
    index++;
}
// zoom_arg.txt may hold the index of the tower to centre on; anything else centres the row.
int focusIndex = index / 2;
int.TryParse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim(), out focusIndex);
UnityEngine.Vector3 focus = points[UnityEngine.Mathf.Clamp(focusIndex, 0, points.Count - 1)].At + UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, points[0].Along).normalized * 0.1f;
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
pan.SetView(focus, 4f);
pan.enabled = false;
UnityEngine.Camera.main.orthographicSize = half;
return "showcase of " + index + " towers at " + focus.ToString("0.0");
