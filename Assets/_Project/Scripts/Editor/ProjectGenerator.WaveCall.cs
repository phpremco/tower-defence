using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 73: the rules of calling the next wave early (Data/Levels/WaveCall.asset) and their place on the campaign catalog.
    /// The full generator runs it after the campaign; the menu entry runs it on its own without regenerating anything else.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string WaveCallAssetPath = DataRoot + "/Levels/WaveCall.asset";

        // Modest on purpose: an eight-second countdown pays at most eight coins, so calling every wave of a twelve-wave level
        // early is worth a cheap upgrade, not a tower row. The balance model keeps the waiting player as its baseline.
        private const float WaveCallCoinsPerSecond = 1f;
        private const int WaveCallMaxBonus = 8;
        private const float WaveCallCooldownSeconds = 4f;
        private const bool WaveCallBeforeFirstWave = false;

        [MenuItem("Tools/Generate/Wave Call")]
        public static void GenerateWaveCall()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(DataRoot + "/Levels/CampaignCatalog.asset");
            WaveCallDefinition rules = CreateWaveCall(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Generated the wave call rules ({rules.CoinsPerSecond} coins/s, cap {rules.MaxBonusPerCall}, cooldown {rules.CooldownSeconds} s) and set them on {(catalog != null ? catalog.name : "no campaign catalog")}.");
        }

        private static WaveCallDefinition CreateWaveCall(CampaignCatalog catalog)
        {
            var rules = LoadOrCreate<WaveCallDefinition>(WaveCallAssetPath);
            var so = new SerializedObject(rules);
            so.FindProperty("_coinsPerSecond").floatValue = WaveCallCoinsPerSecond;
            so.FindProperty("_maxBonusPerCall").intValue = WaveCallMaxBonus;
            so.FindProperty("_cooldownSeconds").floatValue = WaveCallCooldownSeconds;
            so.FindProperty("_allowBeforeFirstWave").boolValue = WaveCallBeforeFirstWave;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rules);

            if (catalog != null)
            {
                var catalogSo = new SerializedObject(catalog);
                catalogSo.FindProperty("_waveCall").objectReferenceValue = rules;
                catalogSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
            }

            return rules;
        }
    }
}
