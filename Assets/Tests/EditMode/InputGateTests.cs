using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class InputGateTests
    {
        [Test]
        public void InputBlocked_WhilePausedOrHeld()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            var context = new LevelContext(level, null, null, null, null, null, null, null, null, null, 1, true);

            Assert.IsFalse(context.InputBlocked);
            context.Held = true;
            Assert.IsTrue(context.InputBlocked, "an overlay such as the briefing, codex or a dialogue holds the clock");
            context.Held = false;
            context.Paused = true;
            Assert.IsTrue(context.InputBlocked);

            Object.DestroyImmediate(level);
        }
    }
}
