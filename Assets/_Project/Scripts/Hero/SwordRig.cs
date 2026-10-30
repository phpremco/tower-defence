using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Hero
{
    /// <summary>
    /// The energy sword: drawn with a twirl, held in a guard stance that sways while the hero waits, and swung in
    /// a cycle of three cuts (a cross slash, a backhand and an overhead chop with a long lunge). The blade grows
    /// when it ignites and leaves a trail during each cut.
    /// </summary>
    public sealed class SwordRig : HeroWeaponRig
    {
        private readonly struct Cut
        {
            public readonly float Seconds;
            public readonly float WindupPitch;
            public readonly float WindupYaw;
            public readonly float StrikePitch;
            public readonly float StrikeYaw;
            public readonly float Lunge;
            public readonly float Lean;

            public Cut(float seconds, float windupPitch, float windupYaw, float strikePitch, float strikeYaw, float lunge, float lean)
            {
                Seconds = seconds;
                WindupPitch = windupPitch;
                WindupYaw = windupYaw;
                StrikePitch = strikePitch;
                StrikeYaw = strikeYaw;
                Lunge = lunge;
                Lean = lean;
            }
        }

        private const float GuardPitch = -28f;
        private const float GuardYaw = -6f;
        private const float GuardTwist = -14f;
        private const float SwaySpeed = 2.4f;
        private const float SwayPitch = 3f;
        private const float SwayYaw = 4f;
        private const float WindupShare = 0.3f;
        private const float StrikeShare = 0.28f;
        private const float TwirlSeconds = 0.28f;
        private const float TrailSeconds = 0.22f;
        private const float TrailWidth = 0.35f;

        private static readonly Cut[] Cuts =
        {
            new Cut(0.42f, -60f, -65f, 25f, 70f, 0.32f, 16f),
            new Cut(0.4f, -45f, 80f, 15f, -75f, 0.26f, 12f),
            new Cut(0.52f, -150f, 0f, 35f, 10f, 0.48f, 26f),
        };

        private TrailRenderer _trail;
        private int _cutIndex;
        private float _swingTime = float.PositiveInfinity;
        private float _twirl = float.PositiveInfinity;

        protected override float ReadyPitch => GuardPitch + Mathf.Sin(Time.time * SwaySpeed) * SwayPitch;

        protected override float ReadyYaw => GuardYaw + Mathf.Sin(Time.time * SwaySpeed * 0.7f) * SwayYaw;

        protected override void OnBuilt(HeroWeaponDefinition definition)
        {
            _trail = Tip.gameObject.AddComponent<TrailRenderer>();
            _trail.time = TrailSeconds;
            _trail.widthMultiplier = TrailWidth;
            _trail.minVertexDistance = 0.02f;
            _trail.emitting = false;
            _trail.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            Color color = definition.Color;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            _trail.colorGradient = gradient;
        }

        protected override void OnAttack(Vector3 target)
        {
            _swingTime = 0f;
            _cutIndex = (_cutIndex + 1) % Cuts.Length;
        }

        protected override void OnDrawn()
        {
            _twirl = 0f;
            Energy.localScale = new Vector3(1f, 1f, 0.01f);
        }

        protected override void OnHolstered()
        {
            if (_trail != null)
            {
                _trail.emitting = false;
                _trail.Clear();
            }
        }

        protected override void OnIgnite(float fraction) => Energy.localScale = new Vector3(1f, 1f, Mathf.Max(0.01f, fraction));

        // In the guard stance the torso turns side-on; relaxed it faces the road straight.
        protected override Pose IdlePose(bool walking) => new Pose { Twist = Drawn && !walking ? GuardTwist : 0f };

        protected override Pose Animate()
        {
            Twirl();
            Cut cut = Cuts[_cutIndex];
            if (_swingTime >= cut.Seconds)
            {
                _trail.emitting = false;
                return new Pose { Busy = false };
            }

            _swingTime += Time.deltaTime;
            float t = Mathf.Clamp01(_swingTime / cut.Seconds);
            float pitch;
            float yaw;
            float strike;
            if (t < WindupShare)
            {
                float k = Smooth(t / WindupShare);
                pitch = Mathf.Lerp(GuardPitch, cut.WindupPitch, k);
                yaw = Mathf.Lerp(GuardYaw, cut.WindupYaw, k);
                strike = 0f;
            }
            else if (t < WindupShare + StrikeShare)
            {
                float k = Smooth((t - WindupShare) / StrikeShare);
                pitch = Mathf.Lerp(cut.WindupPitch, cut.StrikePitch, k);
                yaw = Mathf.Lerp(cut.WindupYaw, cut.StrikeYaw, k);
                strike = k;
            }
            else
            {
                float k = Smooth((t - WindupShare - StrikeShare) / (1f - WindupShare - StrikeShare));
                pitch = Mathf.Lerp(cut.StrikePitch, GuardPitch, k);
                yaw = Mathf.Lerp(cut.StrikeYaw, GuardYaw, k);
                strike = 1f - k;
            }

            _trail.emitting = t >= WindupShare * 0.7f && t < WindupShare + StrikeShare + 0.12f;
            return new Pose
            {
                ArmPitch = pitch,
                ArmYaw = yaw,
                Lunge = cut.Lunge * strike,
                Lean = cut.Lean * strike,
                Twist = yaw * 0.25f,
                Busy = true,
            };
        }

        // A flourish on the draw: the hilt spins once around the grip as the blade lights.
        private void Twirl()
        {
            if (_twirl >= TwirlSeconds)
            {
                return;
            }

            _twirl += Time.deltaTime;
            float t = Mathf.Clamp01(_twirl / TwirlSeconds);
            Weapon.localRotation = Model.GripRotation * Quaternion.AngleAxis(360f * Smooth(t), Vector3.right);
        }
    }
}
