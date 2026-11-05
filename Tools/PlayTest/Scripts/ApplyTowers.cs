using System;
using System.Reflection;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Runs only the generator's "Roster" step (Laser, Cryo, Tesla, Railgun, Support and the hybrids, with the balance sheet's
/// prices) on the existing Gun, Cannon and Flak assets and saves, for a tower change while the full generator must not run
/// (it syncs the Blender exports from ArtSource). Same code as that step of Tools/Generate/Scenes and Starter Data. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyTowers.cs --entry ApplyTowers.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplyTowers
{
    public static string Run()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        TowerDefinition Load(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{id}.asset");
        Type generator = typeof(Game.Editor.ProjectGenerator);
        generator.GetMethod("CreateRoster", flags).Invoke(null, new object[] { Load("PulseRifle"), Load("Cannon"), Load("Rocket") });
        AssetDatabase.SaveAssets();
        TowerDefinition laser = Load("Laser");
        return $"Laser: speed {laser.ProjectileSpeed}, lock {laser.BeamLockSeconds}, sweep {laser.BeamSweepSeconds}, L1 {laser.GetLevel(0).Damage}/{laser.GetLevel(0).FireInterval}";
    }
}
