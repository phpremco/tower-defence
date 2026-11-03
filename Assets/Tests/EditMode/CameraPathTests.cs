using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class CameraPathTests
    {
        [Test]
        public void Endpoints_EqualTheKeys()
        {
            CameraKey[] keys =
            {
                Key(0f, new Vector3(0f, 1f, 0f), Vector3.forward, 40f),
                Key(0.4f, new Vector3(3f, 2f, 1f), Vector3.right, 30f),
                Key(1f, new Vector3(6f, 1f, -2f), Vector3.up, 20f),
            };

            CameraSample start = CameraPath.Sample(keys, 0f);
            CameraSample middle = CameraPath.Sample(keys, 0.4f);
            CameraSample end = CameraPath.Sample(keys, 1f);

            Assert.That(Vector3.Distance(start.Position, keys[0].Position), Is.LessThan(0.0001f));
            Assert.That(Vector3.Distance(middle.Position, keys[1].Position), Is.LessThan(0.0001f), "the curve passes through every key");
            Assert.That(Vector3.Distance(end.Position, keys[2].Position), Is.LessThan(0.0001f));
            Assert.AreEqual(20f, end.FieldOfView, 0.0001f);
            Assert.That(Vector3.Distance(end.LookAt, Vector3.up), Is.LessThan(0.0001f));
        }

        [Test]
        public void SingleKey_HoldsItsPose()
        {
            CameraKey[] keys = { Key(0.3f, new Vector3(1f, 2f, 3f), Vector3.zero, 35f) };

            foreach (float progress in new[] { 0f, 0.3f, 0.8f, 1f })
            {
                CameraSample sample = CameraPath.Sample(keys, progress);
                Assert.That(Vector3.Distance(sample.Position, keys[0].Position), Is.LessThan(0.0001f));
                Assert.AreEqual(35f, sample.FieldOfView, 0.0001f);
            }
        }

        [Test]
        public void Easing_StaysMonotonicAndInRange()
        {
            foreach (CameraEase ease in new[] { CameraEase.Linear, CameraEase.In, CameraEase.Out, CameraEase.InOut })
            {
                Assert.AreEqual(0f, Easing.Apply(ease, 0f), 0.0001f, ease.ToString());
                Assert.AreEqual(1f, Easing.Apply(ease, 1f), 0.0001f, ease.ToString());
                float previous = 0f;
                for (int i = 1; i <= 100; i++)
                {
                    float value = Easing.Apply(ease, i / 100f);
                    Assert.GreaterOrEqual(value, previous - 0.00001f, $"{ease} runs backwards at {i / 100f}");
                    previous = value;
                }
            }
        }

        [Test]
        public void FieldOfView_EasesBetweenKeysWithoutOvershoot()
        {
            CameraKey[] keys = { Key(0f, Vector3.zero, Vector3.forward, 50f), Key(1f, Vector3.one, Vector3.forward, 14f) };
            float previous = 50f;
            for (int i = 0; i <= 50; i++)
            {
                float fov = CameraPath.Sample(keys, i / 50f).FieldOfView;
                Assert.LessOrEqual(fov, previous + 0.0001f);
                Assert.That(fov, Is.InRange(14f, 50f));
                previous = fov;
            }
        }

        private static CameraKey Key(float at, Vector3 position, Vector3 lookAt, float fieldOfView) =>
            new CameraKey { At = at, Position = position, LookAt = lookAt, FieldOfView = fieldOfView, Ease = CameraEase.InOut };
    }
}
