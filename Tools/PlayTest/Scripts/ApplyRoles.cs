using System;
using System.Reflection;
using Game.Enemies;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Round 21a (roles by enemy): runs only the generator's "Towers", "Roster" and "Bestiary" steps (tower prices, damage, layer
/// shares and splash caps; creature plating and kill money) and saves, for a balance change while the full generator must not run (it
/// syncs the Blender exports from ArtSource). Same code as those steps of Tools/Generate/Scenes and Starter Data. Run
/// ApplyWaves.cs after it so the waves are solved against the new numbers:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRoles.cs --entry ApplyRoles.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplyRoles
{
    public static string Run()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type generator = typeof(Game.Editor.ProjectGenerator);
        Type contextType = generator.GetNestedType("GeneratorContext", BindingFlags.NonPublic);
        object context = Activator.CreateInstance(contextType, true);
        generator.GetMethod("GenerateCoreTowers", flags).Invoke(null, new[] { context });
        object Get(string property) => contextType.GetProperty(property).GetValue(context);
        generator.GetMethod("CreateRoster", flags).Invoke(null, new[] { Get("Pulse"), Get("Cannon"), Get("Rocket") });
        generator.GetMethod("CreateBestiary", flags).Invoke(null, null);
        AssetDatabase.SaveAssets();

        TowerDefinition Tower(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{id}.asset");
        var crag = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Cragback.asset");
        TowerDefinition gun = Tower("PulseRifle");
        return $"Gun air share {gun.AirDamageFactor}, prices {gun.GetLevel(0).Cost}/{gun.GetLevel(1).Cost}/{gun.GetLevel(2).Cost}; Cragback plating {crag.Plating} floor {crag.PlatingFloor} reward {crag.Reward}";
    }
}
