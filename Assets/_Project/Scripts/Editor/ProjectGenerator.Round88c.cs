using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// Round 88c in data, for a project the full generator is never run on: the purse that comes with the warning of a
    /// level's first air raid (<see cref="BalanceRules.FirstRaidPurseGuns"/> per difficulty, on the difficulty profiles),
    /// a call sound for the five level bosses whose brood had none (existing approved recordings), and the combat rules
    /// asset with the mob fight's corrected reason. Safe to run again; the generator's own steps write the same.
    /// </summary>
    public static partial class ProjectGenerator
    {
        // The level bosses whose Spawn power came with round 88b and had no call of its own.
        private static readonly string[] BroodCallVoices = { "PackAlpha", "AcidEmpress", "CrystalWarlord", "SkySovereign", "RollerKing" };

        /// <summary>The first-raid purse of a difficulty of the balance sheet, by its place in the sheet (Easy first).</summary>
        public static float FirstRaidPurseGunsOf(string difficultyId)
        {
            BalanceSheet.Difficulty[] sheets = BalanceSheet.Difficulties;
            for (int i = 0; i < sheets.Length; i++)
            {
                if (sheets[i].Id == difficultyId)
                {
                    return BalanceRules.Default.FirstRaidPurseGunsFor(i);
                }
            }

            return 0f;
        }

        public static string ApplyRound88c()
        {
            var report = new StringBuilder();
            foreach (BalanceSheet.Difficulty sheet in BalanceSheet.Difficulties)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DataRoot}/Difficulty/{sheet.Id}.asset");
                if (profile == null)
                {
                    continue;
                }

                var so = new SerializedObject(profile);
                so.FindProperty("_firstRaidPurseGuns").floatValue = FirstRaidPurseGunsOf(sheet.Id);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                report.Append(System.FormattableString.Invariant($"{sheet.Id}: first-raid purse {FirstRaidPurseGunsOf(sheet.Id):0.##} sky guns\n"));
            }

            int voices = 0;
            foreach (VoiceSpec spec in Voices)
            {
                if (System.Array.IndexOf(BroodCallVoices, spec.Enemy) >= 0)
                {
                    CreateVoice(spec);
                    voices++;
                }
            }

            report.Append($"brood call on {voices} boss voices\n");
            CreateCombatRules();
            report.Append("combat rules asset rewritten\n");
            AssetDatabase.SaveAssets();
            return report.ToString();
        }
    }
}
