// Round 50: the hero art's draw and shoot clips (length, grab moment) and the weapons' intervals, to set the draw times.
var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:HeroArtDefinition"))
{
    var hero = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    sb.Append(hero.name).Append(": ");
    var body = (Game.Core.ModelArtDefinition)typeof(Game.Hero.HeroArtDefinition).GetProperty("Body").GetValue(hero);
    foreach (var clip in body.Clips)
    {
        if (clip.Clip == null) continue;
        string n = clip.Clip.name;
        if (n.Contains("Draw") || n.Contains("Stow") || n.Contains("Shoot") || n.Contains("Slash") || n.Contains("Fire")) sb.Append(n).Append("/").Append(clip.Role).Append(" len ").Append(clip.Length.ToString("0.000")).Append(" ev ").Append(clip.EventSeconds.ToString("0.000")).Append(" cue ").Append(clip.CueSeconds.ToString("0.000")).Append(clip.UpperBody ? " upper" : "").Append("; ");
    }
}
foreach (var w in new[] { "Weapon_Blaster", "Weapon_EnergySword" })
{
    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroWeaponDefinition>("Assets/_Project/Data/Hero/" + w + ".asset");
    sb.Append(w).Append(" interval ").Append(def.AttackInterval).Append(" kind ").Append(def.Kind).Append("; ");
}
return sb.ToString();
