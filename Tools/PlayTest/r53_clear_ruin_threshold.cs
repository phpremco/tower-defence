// Round 53: applies the generator's new ruin-threshold rule (ProjectGenerator.ClearRuinThreshold) to the existing
// Arena_Ruins copy without regenerating it: the nests inside its ground entrance's threshold go; nothing else changes.
var level = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>("Assets/_Project/Data/Levels/Arena/Arena_Ruins.asset");
var so = new UnityEditor.SerializedObject(level);
var nests = so.FindProperty("_nests");
var gate = level.BossEntrance.Cell;
var method = typeof(Game.Editor.ProjectGenerator).GetMethod("ClearRuinThreshold", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
int before = nests.arraySize;
method.Invoke(null, new object[] { nests, gate });
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(level);
UnityEditor.AssetDatabase.SaveAssets();
var errors = new System.Collections.Generic.List<string>();
Game.Levels.LevelValidator.Validate(level, errors);
return "nests " + before + " -> " + level.Nests.Length + " (gate " + gate + "); validator errors " + errors.Count + " " + string.Join(" | ", errors);
