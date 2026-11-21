using Game.Enemies;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Round 88b, the escort rule in data: every level boss carries one Spawn power, so the rule (BalanceRules.EscortStartWeight,
    /// DifficultyProfile.EscortWorth) has a creature to send. The brood mother, the storm matriarch, the slime monarch and the
    /// hive queen had theirs; the others get one here, with a creature of their own kind. How many come is the rule's, not
    /// the power's: its count is only what a boss outside the rule (a test, a menu guardian) would send.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private readonly struct EscortSpec
        {
            public readonly string Boss;
            public readonly string Ability;
            public readonly string Minion;
            public readonly float Interval;
            public readonly float FirstDelay;
            public readonly Color Color;

            public EscortSpec(string boss, string ability, string minion, float interval, float firstDelay, Color color)
            {
                Boss = boss;
                Ability = ability;
                Minion = minion;
                Interval = interval;
                FirstDelay = firstDelay;
                Color = color;
            }
        }

        // The count a call has outside the rule; under it the brood's budget decides.
        private const int EscortPlainCount = 2;

        // A fast boss calls a little more often, so its short walk still has several calls.
        private static readonly EscortSpec[] EscortSpecs =
        {
            new EscortSpec("PackAlpha", "PackCall", "Prowler", 8f, 5f, new Color(1f, 0.5f, 0.15f)),
            new EscortSpec("CoreWisp", "WispWake", "Wisp", 10f, 6f, new Color(0.85f, 0.9f, 1f)),
            new EscortSpec("AcidEmpress", "StingerGuard", "Stinger", 12f, 7f, new Color(1f, 0.35f, 0.9f)),
            new EscortSpec("CrystalWarlord", "ShardGuard", "ShardRunner", 12f, 7f, new Color(0.5f, 1f, 1f)),
            new EscortSpec("SkySovereign", "RayWing", "Skyray", 10f, 6f, new Color(0.5f, 1f, 1f)),
            new EscortSpec("RollerKing", "RollerPack", "Roller", 10f, 6f, new Color(1f, 0.7f, 0.3f)),
        };

        /// <summary>
        /// Writes the escort data without the full generator: the Spawn power of every level boss that had none, and the
        /// escort worth of the four difficulties. Safe to run again; the generator calls the same.
        /// </summary>
        public static string ApplyEscorts()
        {
            int powers = ApplyEscortAbilities();
            int difficulties = 0;
            foreach (BalanceSheet.Difficulty sheet in BalanceSheet.Difficulties)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DataRoot}/Difficulty/{sheet.Id}.asset");
                if (profile == null)
                {
                    continue;
                }

                var so = new SerializedObject(profile);
                so.FindProperty("_escortWorth").floatValue = sheet.EscortWorth;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
                difficulties++;
            }

            AssetDatabase.SaveAssets();
            return $"escort powers on {powers} level bosses, escort worth on {difficulties} difficulties";
        }

        private static int ApplyEscortAbilities()
        {
            int done = 0;
            foreach (EscortSpec spec in EscortSpecs)
            {
                var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/{spec.Boss}.asset");
                var minion = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/{spec.Minion}.asset");
                if (boss == null || minion == null)
                {
                    Debug.LogWarning($"Escort of {spec.Boss}: the boss or its creature {spec.Minion} has no asset; skipped.");
                    continue;
                }

                BossAbilityDefinition ability = CreateAbility(spec.Ability, BossAbilityKind.Spawn, spec.Interval, spec.FirstDelay, 0f, 1f, 1f, 1f, minion, EscortPlainCount, spec.Color);
                var name = new SerializedObject(ability);
                name.FindProperty("_displayName").stringValue = LocalisationTables.KeyOrText(ability, "_displayName", Spaced(spec.Ability));
                name.ApplyModifiedPropertiesWithoutUndo();
                AddAbility(boss, ability);
                done++;
            }

            return done;
        }

        private static void AddAbility(EnemyDefinition boss, BossAbilityDefinition ability)
        {
            var so = new SerializedObject(boss);
            SerializedProperty list = so.FindProperty("_abilities");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == ability)
                {
                    return;
                }
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = ability;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }
    }
}
