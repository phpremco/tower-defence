using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;
using static System.FormattableString;

namespace Game.Editor
{
    /// <summary>Round 22f: the finale stage, which writes the continent bosses' health and the boss health shares from the finale probe.</summary>
    public static partial class BalanceStages
    {
        public const string FinaleApplyPath = "Tools/PlayTest/finale_apply.txt";
        private const string EnemiesFolder = "Assets/_Project/Data/Enemies/";
        private const string DifficultiesFolder = "Assets/_Project/Data/Difficulty/";
        private const int HealthRounding = 50;
        private const float ShareRounding = 0.05f;

        /// <summary>
        /// Writes each probed continent boss's health (<see cref="FinaleProbeReport.DerivedHealth"/>) and, for the difficulties above
        /// Medium, the boss health share (<see cref="FinaleProbeReport.DerivedShare"/>) into the assets, rounded. Both are inputs of the
        /// calibration hash: every stage reports the calibration stale afterwards until measure, fit and apply ran again.
        /// </summary>
        public static string ApplyFinale()
        {
            if (!File.Exists(FinaleProbeReport.Path))
            {
                return "no finale probe report: run FinaleProbeTests first";
            }

            Loaded loaded = Load(true);
            List<FinaleProbeCell> cells = FinaleProbeReport.Read(File.ReadAllLines(FinaleProbeReport.Path));
            var text = new StringBuilder();
            text.Append(Invariant($"Finale apply {DateTime.Now:yyyy-MM-dd HH:mm} from {FinaleProbeReport.Path} ({cells.Count} cells); hold share {loaded.Model.Rules.FinaleHoldShare:0.00}.\n"));
            int changed = 0;
            foreach (ContinentFinale finale in FinalePlan.Finales)
            {
                float derived = FinaleProbeReport.DerivedHealth(cells, finale.Boss, loaded.Model.Rules);
                var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemiesFolder + finale.Boss + ".asset");
                if (derived <= 0f || boss == null)
                {
                    text.Append(Invariant($"  {finale.Boss,-16} not probed on Medium: unchanged\n"));
                    continue;
                }

                int health = Math.Max(HealthRounding, (int)Math.Round(derived / HealthRounding) * HealthRounding);
                text.Append(Invariant($"  {finale.Boss,-16} health {boss.MaxHealth:0} -> {health} (probe: holds x{FinaleProbeReport.Find(cells, finale.Boss, FinaleProbeReport.Medium).Holds:0.00} on Medium)\n"));
                if (Math.Abs(health - boss.MaxHealth) >= 1f)
                {
                    var serialized = new SerializedObject(boss);
                    serialized.FindProperty("_maxHealth").floatValue = health;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(boss);
                    changed++;
                }
            }

            for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
            {
                DifficultyProfile profile = loaded.Catalog.Difficulties[d];
                float derived = FinaleProbeReport.DerivedShare(cells, d);
                if (derived <= 0f)
                {
                    continue;
                }

                float share = (float)Math.Round(derived / ShareRounding) * ShareRounding;
                bool applied = d > FinaleProbeReport.Medium;
                text.Append(Invariant($"  {profile.name,-7} boss health share {profile.BossHealthShare:0.00} -> {share:0.00} (derived {derived:0.000}){(applied ? string.Empty : ", reported only (Medium is the reference, Easy stays forgiving)")}\n"));
                if (applied && Math.Abs(share - profile.BossHealthShare) >= ShareRounding * 0.5f)
                {
                    var serialized = new SerializedObject(profile);
                    serialized.FindProperty("_bossHealthShare").floatValue = share;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(profile);
                    changed++;
                }
            }

            if (changed > 0)
            {
                AssetDatabase.SaveAssets();
                text.Append("  the calibration's inputs changed: run measure, fit and apply before the next solve.\n");
            }

            File.WriteAllText(FinaleApplyPath, text.ToString());
            return Invariant($"finale apply: {changed} assets changed; see {FinaleApplyPath}");
        }

        [MenuItem("Tools/Generate/Balance/10 Finale from the probe (boss health, shares)")]
        private static void MenuApplyFinale() => Debug.Log("[Balance] " + ApplyFinale());
    }
}
