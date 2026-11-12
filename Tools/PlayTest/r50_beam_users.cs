// Round 50: lists every enemy with a laser or volley power, its art row and the art's beam socket.
var sb = new System.Text.StringBuilder();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
{
    var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    foreach (var ability in enemy.Abilities)
    {
        if (ability == null) continue;
        if (ability.Kind != Game.Enemies.BossAbilityKind.Laser && ability.Kind != Game.Enemies.BossAbilityKind.Volley) continue;
        sb.Append(enemy.name + ":" + ability.Kind + " art=" + (enemy.Art != null ? enemy.Art.name + " socket='" + enemy.Art.BeamSocket + "'" : "none") + "; ");
    }
}
return sb.ToString();
