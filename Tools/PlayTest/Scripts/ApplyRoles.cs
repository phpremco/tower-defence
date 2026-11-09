using System;
using System.Reflection;
using Game.Enemies;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Round 21a (roles by enemy), round 24 (the armour pool): runs only the generator's "Towers", "Roster", "Bestiary" and "Hero" steps
/// (tower prices, damage, layer shares, splash caps and armour factors; creature armour and kill money; the hero's health and weapons) and saves, for a balance change while the full generator must not run (it
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
        // Round 24: the hero's health, regeneration and the weapons' armour factors live in the Hero step.
        generator.GetMethod("GenerateHero", flags).Invoke(null, new[] { context });
        AssetDatabase.SaveAssets();

        TowerDefinition Tower(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{id}.asset");
        var crag = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Cragback.asset");
        TowerDefinition gun = Tower("PulseRifle");
        return $"Gun air share {gun.AirDamageFactor}, prices {gun.GetLevel(0).Cost}/{gun.GetLevel(1).Cost}/{gun.GetLevel(2).Cost}; gun armour factor {gun.ArmourFactor}; Cragback armour {crag.Armor} reward {crag.Reward}";
    }
}
