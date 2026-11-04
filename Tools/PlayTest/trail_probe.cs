var sb = new System.Text.StringBuilder();
Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsInactive.Include))
{
    if (!r.name.Contains("Trail")) continue;
    sb.Append(r.name + " active=" + r.gameObject.activeInHierarchy + " bounds c=" + r.bounds.center.ToString("F2") + " s=" + r.bounds.size.ToString("F2") + " mat=" + r.sharedMaterial.name + " layer=" + r.gameObject.layer + "; ");
}
var road = context.Level.MainRoad;
var cell = road[road.Length / 2];
sb.Append(" terrainAt=" + context.Level.Terrain.HeightAt(cell).ToString("F3"));
var ray = new UnityEngine.Ray(context.Level.Road.CentreOf(cell) + UnityEngine.Vector3.up * 5f, UnityEngine.Vector3.down);
if (UnityEngine.Physics.Raycast(ray, out var hit, 20f)) sb.Append(" hit=" + hit.point.y.ToString("F3") + " " + hit.collider.name);
var cue = Game.Core.MarkerArt.Active.Material(Game.Core.MarkerKind.RoadCue);
sb.Append(" cueTex=" + (cue.GetTexture("_MainTex") != null ? cue.GetTexture("_MainTex").name : "null") + " shader=" + cue.shader.name + " supported=" + cue.shader.isSupported);
return sb.ToString();
