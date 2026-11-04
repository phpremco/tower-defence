// Import stage 3b: syncs the story, hero and cutscene exports (before the builder exists) so their FBX trees can be read.
var assets = new[] { "Mercer", "Voss", "Ora", "NTI_Emblem", "Pioneer1", "Pioneer1_Wreck", "RadarConsole", "Cutscene_Capsule", "Cutscene_Drill", "Cutscene_Mothership", "Cutscene_Beacon", "Drone_Builder", "Drone_Hauler" };
var extras = new System.Collections.Generic.Dictionary<string, string[]>
{
    { "Mercer", new[] { "Weapons/Mercer_Pistol.fbx", "Weapons/Mercer_SwordHilt.fbx", "Weapons/Mercer_SwordBlade.fbx", "Weapons/MercerWeapons_Albedo.png", "Weapons/MercerWeapons_Normal.png", "Weapons/MercerWeapons_Emission.png", "Weapons/MercerWeapons_Mask.png", "Weapons/MercerWeapons_report.json" } },
};
int n = Game.Editor.BlenderSync.Sync(assets, extras);
return "copied " + n;
