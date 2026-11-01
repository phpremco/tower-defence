// Reads the rendered colour of the game view at the middle of the main road, at the ground cell beside it, at the
// hero and at the base, so value separation (ground, road, units) can be measured instead of guessed.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var level = context.Level;
var road = level.MainRoad;
var cam = UnityEngine.Camera.main;
var shot = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
var sb = new System.Text.StringBuilder();
sb.Append(shot.width + "x" + shot.height + " ");
System.Action<string, UnityEngine.Vector3> sample = (name, world) =>
{
    UnityEngine.Vector3 s = cam.WorldToScreenPoint(world);
    if (s.x < 0 || s.y < 0 || s.x >= shot.width || s.y >= shot.height) { sb.Append(name + "=offscreen "); return; }
    UnityEngine.Color c = shot.GetPixel((int)s.x, (int)s.y);
    float lum = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    sb.Append(name + "=(" + c.r.ToString("0.00") + "," + c.g.ToString("0.00") + "," + c.b.ToString("0.00") + ") L" + lum.ToString("0.00") + " ");
};
int samples = 0;
for (int i = 2; i < road.Length - 2 && samples < 4; i += (road.Length / 5))
{
    var cell = road[i];
    sample("road" + i, level.Road.CentreOf(cell));
    // The ground cell two steps to the side of the road that is not road itself.
    foreach (var d in new[] { new UnityEngine.Vector2Int(0, 2), new UnityEngine.Vector2Int(0, -2), new UnityEngine.Vector2Int(2, 0), new UnityEngine.Vector2Int(-2, 0) })
    {
        var g = cell + d;
        if (level.Grid.Contains(g) && !level.Road.IsRoad(g) && !level.Terrain.IsLiquid(g))
        {
            sample("ground" + i, level.Grid.CellToWorld(g));
            break;
        }
    }
    samples++;
}
if (context.Hero != null) sample("hero", context.Hero.transform.position + UnityEngine.Vector3.up * 0.5f);
UnityEngine.Object.Destroy(shot);
return sb.ToString();
