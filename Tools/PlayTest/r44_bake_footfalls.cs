// Round 44: bakes Spencer's footfalls into his clips (Tools/Generate/Hero Footfalls) and lists them per clip.
var hero = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroArtDefinition>("Assets/_Project/Data/Art/Story/MercerArt.asset");
string report = Game.Editor.HeroFootfallBaker.Bake(hero.Body);
UnityEditor.AssetDatabase.SaveAssets();
return report;
