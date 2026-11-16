using Game.Combat;
using UnityEditor;

/// <summary>
/// Round 61 without the full generator (Remco's rule): writes what the changed generator now would: the hero's shockwave
/// look (ProjectGenerator.ApplyShockwaveVfx: the Vfx_HeroShockwave asset from the spec, hung on the VfxLibrary).
/// Round 68: the other half of this script, the Gun Turret's air share, is gone. Since round 67 no weapon stores a factor
/// (the removed field TowerDefinition._airDamageFactor); the share is the Bullet row of the combat rules
/// (ProjectGenerator.Damage.cs, written by Tools/PlayTest/Scripts/ApplyRound67.cs). Safe to run again. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound61.cs --entry ApplyRound61.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class ApplyRound61
{
    public static string Run()
    {
        ShockwaveVfxDefinition shockwave = Game.Editor.ProjectGenerator.ApplyShockwaveVfx();
        AssetDatabase.SaveAssets();
        var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>("Assets/_Project/Data/Vfx/VfxLibrary.asset");
        return $"shockwave {AssetDatabase.GetAssetPath(shockwave)}: ring {Name(shockwave.RingSheet)}, refraction {Name(shockwave.Distortion)}, "
            + $"flash {Name(shockwave.Flash)}, sparks {Name(shockwave.SparkSheet)}, debris {Name(shockwave.DebrisSheet)}, dust puffs {shockwave.DustPuffs}; library points at it: {library.Shockwave == shockwave}. "
            + "The Gun Turret's air share is the combat rules' Bullet row since round 67 (ApplyRound67.cs).";
    }

    private static string Name(VfxSheet sheet) => sheet != null ? sheet.name : "none";
}
