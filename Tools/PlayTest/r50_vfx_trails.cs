// Round 50: which projectile styles have a painted trail or beam in the VFX library, and which fall back to the plain line.
var library = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Combat.VfxLibrary>("Assets/_Project/Data/Vfx/VfxLibrary.asset");
var sb = new System.Text.StringBuilder();
foreach (Game.Combat.ProjectileStyle style in System.Enum.GetValues(typeof(Game.Combat.ProjectileStyle)))
{
    var w = library.WeaponFor(style);
    sb.Append(style + ": trail=" + (w != null && w.Trail != null) + " beam=" + (w != null && w.Beam != null) + "; ");
}
return sb.ToString();
