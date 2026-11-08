// Round 23d: the hero's post-to-post distances per level (C1_01..C1_06) and the switch time walking vs flying.
// Posts as HeroController builds them: one cell before each gate (road centre) and one before the base.
var sb = new System.Text.StringBuilder();
var hero = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroDefinition>("Assets/_Project/Data/Hero/Commander.asset");
for (int n = 1; n <= 6; n++)
{
    string path = "Assets/_Project/Data/Levels/Level_C1_0" + n + ".asset";
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>(path);
    if (level == null) { sb.AppendLine(path + " missing"); continue; }
    var road = level.Road;
    float cell = level.CellSize;
    var stands = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (var spot in level.Blockades)
    {
        UnityEngine.Vector3 along = road.DirectionAt(spot.Cell);
        stands.Add(road.CentreOf(spot.Cell) - along * cell);
    }
    var main = level.MainRoad;
    var baseCell = main[main.Length - 1];
    stands.Add(Game.Levels.LevelVisualBuilder.BasePosition(level) - road.DirectionAt(baseCell) * cell);
    var d = new System.Collections.Generic.List<float>();
    for (int i = 0; i < stands.Count; i++)
        for (int j = i + 1; j < stands.Count; j++)
        {
            UnityEngine.Vector3 delta = stands[j] - stands[i];
            delta.y = 0f;
            d.Add(delta.magnitude);
        }
    d.Sort();
    float median = d.Count == 0 ? 0f : (d.Count % 2 == 1 ? d[d.Count / 2] : 0.5f * (d[d.Count / 2 - 1] + d[d.Count / 2]));
    float walk = median / hero.WalkSpeed;
    float fly = hero.SwitchSeconds(median);
    sb.AppendLine($"C1_0{n} posts {stands.Count} pairs {d.Count} min {(d.Count > 0 ? d[0] : 0f):0.0} median {median:0.0} max {(d.Count > 0 ? d[d.Count - 1] : 0f):0.0} m | walk {walk:0.00} s | fly {fly:0.00} s | flies {hero.FliesBetween(median)}");
}
return sb.ToString();
