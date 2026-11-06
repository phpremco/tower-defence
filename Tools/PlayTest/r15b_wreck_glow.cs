// Round 15b (tuning): sets the fallen base wreck's glow to the factor in r15b_glow.txt times its painted glow (the
// material's source emission), so glow values can be compared on one wreck without reloading.
float factor = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r15b_glow.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var view = UnityEngine.Object.FindAnyObjectByType<Game.Levels.BaseArtView>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var art = (Game.Levels.BaseArtDefinition)typeof(Game.Levels.BaseArtView).GetField("_art", flags).GetValue(view);
var wreck = (Game.Core.ArtModel)typeof(Game.Levels.BaseArtView).GetField("_wreck", flags).GetValue(view);
if (wreck == null) { return "no wreck"; }
var source = art.Wreck.MaterialFor(Game.Core.ModelQuality.IsLow);
wreck.Material.SetColor("_EmissionColor", source.GetColor("_EmissionColor") * factor);
return "wreck glow x" + factor + " = " + wreck.Material.GetColor("_EmissionColor");
