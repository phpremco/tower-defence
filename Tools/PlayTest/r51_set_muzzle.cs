// Round 51: the hero pistol's muzzle and barrel axis in the grip space the import leaves (BlenderStoryBuilder.ImportedGrip):
// the report's muzzle (0, 0.062, 0.192) is turned half a turn about Y, because the imported pistol's barrel runs along -Z.
// Writes only `_muzzle` and `_barrelAxis` of the hero art, the same values the story builder now writes.
var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Hero.HeroArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets("t:HeroArtDefinition")[0]));
var so = new UnityEditor.SerializedObject(art);
var muzzle = so.FindProperty("_muzzle");
var before = muzzle.vector3Value;
muzzle.vector3Value = new UnityEngine.Vector3(0f, 0.062f, -0.192f);
so.FindProperty("_barrelAxis").vector3Value = UnityEngine.Vector3.back;
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(art);
UnityEditor.AssetDatabase.SaveAssets();
return art.name + " muzzle " + before.ToString("0.000") + " -> " + art.Muzzle.ToString("0.000") + " barrel " + art.BarrelAxis.ToString("0.0");
