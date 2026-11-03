// Prints the layout of the first campaign level that the opening cutscene uses: size, camera, base, gates, nests, mines, road samples.
var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
var level = catalog.Nodes[catalog.FirstNode(0)].Level;
var sb = new System.Text.StringBuilder();
sb.Append(level.name + " world " + level.WorldSize + " cell " + level.CellSize + " cam pitch " + level.Camera.PitchDegrees + " yaw " + level.Camera.YawDegrees + " fit " + Game.Levels.FixedCameraRig.FitSize(level, 16f/9f));
sb.Append(" | base " + Game.Levels.LevelVisualBuilder.BasePosition(level) + " dir " + level.Road.DirectionAt(level.MainRoad[level.MainRoad.Length-1]));
foreach (var b in level.Blockades) sb.Append(" | gate " + b.Cell + " " + level.Road.CentreOf(b.Cell) + " dir " + level.Road.DirectionAt(b.Cell) + " h " + level.Terrain.HeightAt(b.Cell));
foreach (var n in level.Nests) sb.Append(" | nest " + n.Element + " " + level.Grid.CellToWorld(n.Cell));
foreach (var m in level.MineCells) sb.Append(" | mine " + level.Grid.CellToWorld(m));
var route = level.Road.Route(0);
sb.Append(" | route0 n=" + route.Length + " first " + route[0] + " last " + route[route.Length-1]);
for (int i = 0; i < route.Length; i += System.Math.Max(1, route.Length/8)) sb.Append(" r" + i + route[i]);
sb.Append(" | paths " + level.Paths.Length + " hero " + (level.Hero != null ? level.Hero.name : "none") + " theme " + level.Theme.name);
return sb.ToString();
