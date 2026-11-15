// Round 65: the length of every flight route as its waypoints give it and as a flyer now flies it (FlightCurve), per
// flyer the campaign levels send, with the sharpest turn before and after and the radius used. Writes
// Tools/PlayTest/r65_air_routes.txt and, as CSV for a top-down trace, Tools/PlayTest/r65_air_routes.csv.
var inv = System.Globalization.CultureInfo.InvariantCulture;
var sb = new System.Text.StringBuilder();
var csv = new System.Text.StringBuilder("level,path,kind,index,x,z\n");
var flyers = new System.Collections.Generic.List<Game.Enemies.EnemyDefinition>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
{
    var e = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (e.Flies) flyers.Add(e);
}
sb.AppendLine("level | route | waypoints | length (cells) | sharpest turn | flyer: radius (cells) -> length, change, sharpest turn between points");
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    if (path.Contains("/Arena/")) continue;
    var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>(path);
    var sent = new System.Collections.Generic.HashSet<Game.Enemies.EnemyDefinition>();
    foreach (var set in level.DifficultyWaves) foreach (var wave in set.Waves) foreach (var g in wave.Groups) if (g.Enemy != null && g.Enemy.Flies) sent.Add(g.Enemy);
    for (int p = 0; p < level.Paths.Length; p++)
    {
        if (!level.Paths[p].Flying) continue;
        UnityEngine.Vector3[] route = level.Road.Route(p);
        float before = Game.Levels.FlightCurve.Length(route) / level.CellSize;
        sb.Append(level.name + " | " + level.Paths[p].Name + " | " + route.Length + " | " + before.ToString("0.00", inv) + " | " + Game.Levels.FlightCurve.SharpestTurn(route).ToString("0", inv) + " deg |");
        for (int i = 0; i < route.Length; i++) csv.Append(level.name + "," + p + ",waypoints," + i + "," + route[i].x.ToString("0.000", inv) + "," + route[i].z.ToString("0.000", inv) + "\n");
        bool traced = false;
        foreach (var flyer in flyers)
        {
            if (!sent.Contains(flyer)) continue;
            float radius = Game.Levels.EnemySpawner.TurnRadius(flyer, level.CellSize);
            UnityEngine.Vector3[] round = Game.Levels.FlightCurve.Round(route, radius);
            float after = Game.Levels.FlightCurve.Length(round) / level.CellSize;
            sb.Append(" " + flyer.name + ": " + (radius / level.CellSize).ToString("0.00", inv) + " -> " + after.ToString("0.00", inv) + " (" + ((after / before - 1f) * 100f).ToString("0.0", inv) + "%), " + Game.Levels.FlightCurve.SharpestTurn(round).ToString("0", inv) + " deg;");
            if (!traced)
            {
                traced = true;
                for (int i = 0; i < round.Length; i++) csv.Append(level.name + "," + p + "," + flyer.name + "," + i + "," + round[i].x.ToString("0.000", inv) + "," + round[i].z.ToString("0.000", inv) + "\n");
            }
        }
        sb.AppendLine();
    }
    // Flyers that leave a nest fly straight to the road and then along it: the road's right-angle corners.
    for (int n = 0; n < level.Nests.Length; n++)
    {
        foreach (var flyer in flyers)
        {
            if (!sent.Contains(flyer) || flyer.Element != level.Nests[n].Element) continue;
            UnityEngine.Vector3[] route = Game.Levels.OffRoadRoute.Build(level, level.Grid.CellToWorld(level.Nests[n].Cell), out float progress);
            float radius = Game.Levels.EnemySpawner.TurnRadius(flyer, level.CellSize);
            UnityEngine.Vector3[] round = Game.Levels.FlightCurve.Round(route, radius);
            float before = Game.Levels.FlightCurve.Length(route) / level.CellSize;
            float after = Game.Levels.FlightCurve.Length(round) / level.CellSize;
            sb.AppendLine(level.name + " | nest " + n + " at " + level.Nests[n].Cell + " (" + flyer.name + ", along the road) | " + route.Length + " | " + before.ToString("0.00", inv) + " | " + Game.Levels.FlightCurve.SharpestTurn(route).ToString("0", inv) + " deg | radius " + (radius / level.CellSize).ToString("0.00", inv) + " -> " + after.ToString("0.00", inv) + " (" + ((after / before - 1f) * 100f).ToString("0.0", inv) + "%), " + Game.Levels.FlightCurve.SharpestTurn(round).ToString("0", inv) + " deg");
            for (int i = 0; i < route.Length; i++) csv.Append(level.name + ",nest" + n + ",waypoints," + i + "," + route[i].x.ToString("0.000", inv) + "," + route[i].z.ToString("0.000", inv) + "\n");
            for (int i = 0; i < round.Length; i++) csv.Append(level.name + ",nest" + n + "," + flyer.name + "," + i + "," + round[i].x.ToString("0.000", inv) + "," + round[i].z.ToString("0.000", inv) + "\n");
        }
    }
}
System.IO.File.WriteAllText("Tools/PlayTest/r65_air_routes.txt", sb.ToString());
System.IO.File.WriteAllText("Tools/PlayTest/r65_air_routes.csv", csv.ToString());
return sb.ToString();
