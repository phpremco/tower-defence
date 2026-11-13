using Game.Combat;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Round 61 without the full generator (Remco's rule): writes what the changed generator now would. The Gun Turret's air
/// share (ProjectGenerator.GunAirShare) and the hero's shockwave look (ProjectGenerator.ApplyShockwaveVfx: the
/// Vfx_HeroShockwave asset from the spec, hung on the VfxLibrary). The waves follow with the solve stage (ApplyWaves.cs).
/// Safe to run again. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound61.cs --entry ApplyRound61.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class ApplyRound61
{
    public static string Run()
    {
        var gun = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/PulseRifle.asset");
        var so = new SerializedObject(gun);
        float before = so.FindProperty("_airDamageFactor").floatValue;
        so.FindProperty("_airDamageFactor").floatValue = Game.Editor.ProjectGenerator.GunAirShare;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gun);

        ShockwaveVfxDefinition shockwave = Game.Editor.ProjectGenerator.ApplyShockwaveVfx();
        AssetDatabase.SaveAssets();
        var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>("Assets/_Project/Data/Vfx/VfxLibrary.asset");
        return $"Gun Turret air share {before} -> {gun.AirDamageFactor}; shockwave {AssetDatabase.GetAssetPath(shockwave)}: ring {Name(shockwave.RingSheet)}, refraction {Name(shockwave.Distortion)}, "
            + $"flash {Name(shockwave.Flash)}, sparks {Name(shockwave.SparkSheet)}, debris {Name(shockwave.DebrisSheet)}, dust puffs {shockwave.DustPuffs}; library points at it: {library.Shockwave == shockwave}";
    }

    private static string Name(VfxSheet sheet) => sheet != null ? sheet.name : "none";
}
