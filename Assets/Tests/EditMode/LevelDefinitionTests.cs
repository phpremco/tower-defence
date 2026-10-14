using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class LevelDefinitionTests
    {
        private LevelDefinition _level;
        private VisualTheme _theme;

        [SetUp]
        public void SetUp()
        {
            _level = ScriptableObject.CreateInstance<LevelDefinition>();
            _theme = ScriptableObject.CreateInstance<VisualTheme>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_level);
            Object.DestroyImmediate(_theme);
        }

        [Test]
        public void IsValid_FailsWithoutTheme()
        {
            Assert.IsFalse(_level.IsValid(out string error));
            StringAssert.Contains("theme", error);
        }

        [Test]
        public void IsValid_PassesWithTheme()
        {
            SetTheme(_theme);

            Assert.IsTrue(_level.IsValid(out _));
        }

        [Test]
        public void IsValid_FailsWithEmptyGrid()
        {
            SetTheme(_theme);
            var so = new SerializedObject(_level);
            so.FindProperty("_gridSize").vector2IntValue = Vector2Int.zero;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsFalse(_level.IsValid(out string error));
            StringAssert.Contains("Grid", error);
        }

        private void SetTheme(VisualTheme theme)
        {
            var so = new SerializedObject(_level);
            so.FindProperty("_theme").objectReferenceValue = theme;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
