// Round 50: the hero weapons' draw time from the hero art's draw clips (the pistol draw ends in the aim, the blade draw in
// the guard), so the first shot or blow of an engagement waits for the draw. Only `_drawSeconds` is written.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("t:HeroArtDefinition")[0]));
var sb = new System.Text.StringBuilder();
foreach (var pair in new[] { ("Weapon_Blaster", Game.Core.ModelClipRole.DrawPistol), ("Weapon_EnergySword", Game.Core.ModelClipRole.DrawBlade) })
{
    var weapon = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroWeaponDefinition>("Assets/_Project/Data/Hero/" + pair.Item1 + ".asset");
    float seconds = art.Body.Clips[art.Body.IndexOf(pair.Item2)].Length;
    var so = new UnityEditor.SerializedObject(weapon);
    so.FindProperty("_drawSeconds").floatValue = (float)System.Math.Round(seconds, 3);
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(weapon);
    sb.Append(pair.Item1).Append(" draw ").Append(seconds.ToString("0.000")).Append(" s, stays drawn ").Append(weapon.StayDrawnSeconds).Append(" s; ");
}

UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString();
