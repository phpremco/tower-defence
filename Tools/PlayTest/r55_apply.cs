// Round 55: the RuinGate's scale in the effect art and the Shattered's gate set-back in its boss data, written through
// SerializedObject (no generator run). Arguments in Tools/PlayTest/r55_apply.txt: "<gate scale> <set-back m>".
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/r55_apply.txt").Trim().Split(' ');
float scale = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
float back = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.BossFxArt>("Assets/_Project/Data/Vfx/BossFxArt.asset");
var artObject = new UnityEditor.SerializedObject(art);
artObject.FindProperty("_ruinGateScale").floatValue = scale;
artObject.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(art);
var boss = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.BossDefinition>("Assets/_Project/Data/Enemies/Bosses/ShatteredBoss.asset");
var bossObject = new UnityEditor.SerializedObject(boss);
bossObject.FindProperty("_split._gateSetBack").floatValue = back;
bossObject.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(boss);
UnityEditor.AssetDatabase.SaveAssets();
return "gate scale " + art.RuinGateScale + ", Shattered set-back " + boss.Split.GateSetBack;
