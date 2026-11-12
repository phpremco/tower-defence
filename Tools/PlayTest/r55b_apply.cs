// Round 55b: rebuilds the arena copies that borrow Fault Line's map (Arena_Ruins: the ruin gate's entrance and its footprint)
// through the generator's own targeted path (ProjectGenerator.RebuildArenaCopiesOf, internal: called by reflection), not the
// full generator, and saves them. Prints the new entrance, its footprint and the validator's verdict on the level.
var generator = System.Type.GetType("Game.Editor.ProjectGenerator, Game.Editor");
var rebuild = generator.GetMethod("RebuildArenaCopiesOf", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
int rebuilt = (int)rebuild.Invoke(null, new object[] { "Level_C1_04" });
UnityEditor.AssetDatabase.SaveAssets();
var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>("Assets/_Project/Data/Levels/Arena/Arena_Ruins.asset");
var e = level.BossEntrance;
var errors = new System.Collections.Generic.List<string>();
Game.Levels.LevelValidator.Validate(level, errors);
return "rebuilt " + rebuilt + "; entrance " + e.Kind + " " + e.Cell + ", footprint " + (e.Footprint == null ? 0 : e.Footprint.Length) + " cells: " + (e.Footprint == null ? "" : string.Join(" ", e.Footprint)) + " | validator: " + (errors.Count == 0 ? "ok" : string.Join(" / ", errors));
