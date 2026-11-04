// Night 2: the field layout of the cutscene (road length, gate distance, where landing, base, drill, dig site, entrance and gate lie).
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no player";
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sets = (System.Collections.IDictionary)typeof(Game.UI.CutscenePlayer).GetField("_sets", flags).GetValue(player);
if (!sets.Contains(Game.Levels.CutsceneSet.Field)) return "no field yet";
var field = sets[Game.Levels.CutsceneSet.Field];
var layout = (Game.UI.FieldLayout)field.GetType().GetField("_layout", flags).GetValue(field);
var sb = new System.Text.StringBuilder();
sb.Append("road " + layout.RoadLength.ToString("0.0") + " gateDist " + layout.GateDistance.ToString("0.0") + " | ");
sb.Append("entrance " + layout.Entrance.ToString("0.0") + " gate " + layout.Gate.ToString("0.0") + " base " + layout.Base.ToString("0.0") + " landing " + layout.Landing.ToString("0.0") + " | ");
sb.Append("drill " + layout.Drill.ToString("0.0") + " dig " + layout.DigSite.ToString("0.0") + " digAlong " + Game.UI.FieldLayout.DistanceAlong(layout.Road, layout.DigSite).ToString("0.0") + " digToRoad " + UnityEngine.Vector3.Distance(layout.DigSite, Game.UI.FieldLayout.PointAlong(layout.Road, Game.UI.FieldLayout.DistanceAlong(layout.Road, layout.DigSite))).ToString("0.0") + " | ");
sb.Append("base-landing " + UnityEngine.Vector3.Distance(layout.Base, layout.Landing).ToString("0.0") + " drill-base " + UnityEngine.Vector3.Distance(layout.Drill, layout.Base).ToString("0.0") + " dig-gate " + UnityEngine.Vector3.Distance(layout.DigSite, layout.Gate).ToString("0.0") + " | blockades " + layout.Level.Blockades.Length + " cell " + layout.Level.CellSize + " roadW " + layout.Level.Road.Width);
var cast = Game.Core.Services.Get<Game.Levels.CampaignCatalog>().Story.OpeningCutscene.Cast;
foreach (var e in cast.Swarm) sb.Append(" | " + e.name + " speed " + e.Speed + " walk " + e.WalkSeconds);
return sb.ToString();
