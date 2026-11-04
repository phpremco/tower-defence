// Sets tunables of the Crystal Plains spawn origin definition from Tools/PlayTest/origin_tune.txt ("field value" lines,
// e.g. "_closeDelay 3"), saved; the builder leaves these numbers alone on a regeneration.
var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.SpawnOriginDefinition>("Assets/_Project/Data/Art/Origins/SpawnOrigins_CrystalPlains.asset");
var so = new UnityEditor.SerializedObject(definition);
var text = "";
foreach (var line in System.IO.File.ReadAllLines("Tools/PlayTest/origin_tune.txt"))
{
    var parts = line.Trim().Split(' ');
    if (parts.Length < 2) continue;
    so.FindProperty(parts[0]).floatValue = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
    text += parts[0] + "=" + parts[1] + " ";
}
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.AssetDatabase.SaveAssets();
return text;
