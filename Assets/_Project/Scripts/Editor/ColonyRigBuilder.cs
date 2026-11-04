using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Puts the colony's drilling rig into the levels: a <see cref="ColonyRigDefinition"/> wearing the opening
    /// cutscene's derrick and haulers (their art comes from the cutscene cast, built by the story art step), hung on
    /// the Crystal Plains environment set so every level of that continent shows it. Run again, it only re-points
    /// the same asset; tweaks to the rig's sizes and flight in the asset are kept.
    /// </summary>
    public static class ColonyRigBuilder
    {
        private const string CastPath = "Assets/_Project/Data/Story/Cutscene/CutsceneCast.asset";
        private const string SetPath = "Assets/_Project/Data/Art/Environment/CrystalPlains.asset";
        private const string RigPath = "Assets/_Project/Data/Art/Environment/ColonyRig_CrystalPlains.asset";

        [MenuItem("Tools/Generate/Colony Rig (drill beside every Crystal Plains level)")]
        public static void BuildMenu()
        {
            bool built = Apply();
            AssetDatabase.SaveAssets();
            Debug.Log(built ? "Colony rig: on the Crystal Plains set." : "Colony rig: cutscene cast or environment set missing, nothing changed.");
        }

        /// <summary>True when the rig was built and hung on the set.</summary>
        public static bool Apply()
        {
            var cast = AssetDatabase.LoadAssetAtPath<CutsceneCast>(CastPath);
            var set = AssetDatabase.LoadAssetAtPath<EnvironmentSet>(SetPath);
            if (cast == null || set == null || cast.DrillArt == null)
            {
                return false;
            }

            ColonyRigDefinition rig = BlenderArtBuilder.LoadOrCreate<ColonyRigDefinition>(RigPath);
            var rigObject = new SerializedObject(rig);
            rigObject.FindProperty("_drill").objectReferenceValue = cast.DrillArt;
            rigObject.FindProperty("_hauler").objectReferenceValue = cast.HaulerArt;
            rigObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rig);

            var setObject = new SerializedObject(set);
            setObject.FindProperty("_colonyRig").objectReferenceValue = rig;
            setObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return true;
        }
    }
}
