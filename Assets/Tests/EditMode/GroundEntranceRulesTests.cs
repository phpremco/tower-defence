using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 53: the sealed ruin gate's threshold. A nest mound stood in the arch's mouth on Arena_Ruins, two cells in front
    /// of the ground entrance, and the giant walked out over it; nests now keep their distance, and the validator says so.
    /// </summary>
    public sealed class GroundEntranceRulesTests
    {
        private const string ArenaRuinsPath = "Assets/_Project/Data/Levels/Arena/Arena_Ruins.asset";

        [Test]
        public void Threshold_CoversHalfTheArchAndAStep_DiagonalsIncluded()
        {
            var entrance = new Vector2Int(10, 10);
            int edge = GroundEntranceRules.ThresholdCells;
            Assert.IsTrue(GroundEntranceRules.CrowdsThreshold(entrance, entrance));
            Assert.IsTrue(GroundEntranceRules.CrowdsThreshold(entrance, entrance + new Vector2Int(0, -2)), "round 53's nest, two cells in front");
            Assert.IsTrue(GroundEntranceRules.CrowdsThreshold(entrance, entrance + new Vector2Int(edge, -edge)));
            Assert.IsFalse(GroundEntranceRules.CrowdsThreshold(entrance, entrance + new Vector2Int(edge + 1, 0)));
            Assert.IsFalse(GroundEntranceRules.CrowdsThreshold(entrance, entrance + new Vector2Int(0, -(edge + 1))));
        }

        [Test]
        public void ArenaRuins_KeepsItsGateThresholdClearOfNests()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(ArenaRuinsPath);
            Assert.IsNotNull(level, "no Arena_Ruins");
            Assert.AreEqual(EntranceKind.Ground, level.BossEntrance.Kind);
            foreach (ElementNest nest in level.Nests)
            {
                Assert.IsFalse(GroundEntranceRules.CrowdsThreshold(level.BossEntrance.Cell, nest.Cell), $"the nest at {nest.Cell} stands in the ruin gate's threshold");
            }
        }

        [Test]
        public void Validator_FlagsANestInTheThreshold()
        {
            var original = AssetDatabase.LoadAssetAtPath<LevelDefinition>(ArenaRuinsPath);
            LevelDefinition level = Object.Instantiate(original);
            try
            {
                var so = new SerializedObject(level);
                SerializedProperty nests = so.FindProperty("_nests");
                nests.arraySize++;
                nests.GetArrayElementAtIndex(nests.arraySize - 1).FindPropertyRelative("Cell").vector2IntValue = level.BossEntrance.Cell + new Vector2Int(0, -2);
                so.ApplyModifiedPropertiesWithoutUndo();

                var errors = new List<string>();
                LevelValidator.Validate(level, errors);

                Assert.IsTrue(errors.Exists(e => e.Contains("threshold of the ground boss entrance")), string.Join("\n", errors));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }
    }
}
