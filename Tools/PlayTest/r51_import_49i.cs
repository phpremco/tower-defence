// Round 51 / 49I: syncs and rebuilds only the bat and Skyray rows the Blender round 49I changed (Bat and its Moss, Dust, Tide,
// Frost and corrupted rows, Skyray and its corrupted row, Shalebat and its corrupted row) with BlenderArtBuilder.RebuildOnly.
int worn = Game.Editor.BlenderArtBuilder.RebuildOnly("Bat", "Bat_Moss", "Bat_Dust", "Bat_Tide", "Bat_Frost", "Bat_Corrupted", "Skyray", "Skyray_Corrupted", "Shalebat", "Shalebat_Corrupted");
UnityEditor.AssetDatabase.SaveAssets();
return "rebuilt; worn by " + worn + " enemies";
