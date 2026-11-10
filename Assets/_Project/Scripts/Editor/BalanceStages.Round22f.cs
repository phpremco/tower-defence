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
        private const float MaxHealthStep = 3f;
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

                // Round 29: the probe scales a boss's escorts with its health multiple, so a huge hold says as much about the escorts
                // as about the body; the health moves at most a step per apply and the next probe judges again.
                float stepped = Math.Min(derived, boss.MaxHealth * MaxHealthStep);
                int health = Math.Max(HealthRounding, (int)Math.Round(stepped / HealthRounding) * HealthRounding);
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

            // Round 30 (Remco): Psycho never carries less boss than Hard. Both take one share: Hard's derivation, pulled down to the
            // share at which the Psycho finale player (its money and lighter escorts given) still beats every boss.
            bool shared = loaded.Model.Rules.PsychoBossShareAtLeastHard;
            float sharedShare = shared ? FinaleProbeReport.HardPsychoShare(cells, loaded.Model.Rules) : -1f;
            float ceiling = shared ? FinaleProbeReport.PsychoCeiling(cells, loaded.Model.Rules) : -1f;
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
                string note = applied ? string.Empty : ", reported only (Medium is the reference, Easy stays forgiving)";
                if (applied && sharedShare > 0f)
                {
                    share = (float)Math.Round(sharedShare / ShareRounding) * ShareRounding;
                    note = Invariant($", Hard and Psycho share one (Hard derives {FinaleProbeReport.DerivedShare(cells, FinaleProbeReport.Hard):0.00}, the Psycho ceiling is {ceiling:0.00})");
                }

                text.Append(Invariant($"  {profile.name,-7} boss health share {profile.BossHealthShare:0.00} -> {share:0.00} (derived {derived:0.000}){note}\n"));
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

        /// <summary>Round 29: the continent bosses' data again (health, armour, powers, Codex text) without the full generator.</summary>
        public static string ApplyContinentBosses()
        {
            int count = ProjectGenerator.ApplyContinentBosses().Count;
            return Invariant($"continent bosses written: {count}");
        }

        /// <summary>Round 29: one campaign level's map again from its spec (Tools/PlayTest/relayout_arg.txt names the key).</summary>
        public static string RelayoutLevel(string key) => ProjectGenerator.RelayoutCampaignLevel(key);

        [MenuItem("Tools/Generate/Balance/11 Continent bosses (data and powers)")]
        private static void MenuApplyContinentBosses() => Debug.Log("[Balance] " + ApplyContinentBosses());

        [MenuItem("Tools/Generate/Balance/10 Finale from the probe (boss health, shares)")]
        private static void MenuApplyFinale() => Debug.Log("[Balance] " + ApplyFinale());
    }
}
