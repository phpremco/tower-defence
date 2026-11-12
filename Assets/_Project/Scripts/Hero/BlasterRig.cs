using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Hero
{
    /// <summary>
    /// A ranged weapon: drawn from the hip and held at aim with the arm out, it kicks back on every shot while a
    /// bolt of light flicks to the target and the muzzle flashes. Between shots the aim drifts a little.
    /// </summary>
    public sealed class BlasterRig : HeroWeaponRig
    {
        private const float AimPitch = -82f;
        private const float AimYaw = 8f;
        private const float AimTwist = -22f;
        private const float DriftSpeed = 1.8f;
        private const float DriftPitch = 2f;
        private const float DriftYaw = 3f;
        private const float KickSeconds = 0.14f;
        private const float KickPitch = 14f;
        private const float KickLean = -4f;
        private const float AimLean = 3f;
        private const float BoltSeconds = 0.09f;
        private const float BoltWidth = 0.08f;
        private const float FlashSize = 0.16f;
        private const float FlashEmission = 5f;

        private LineRenderer _bolt;
        private Transform _flash;
        private float _kick = float.PositiveInfinity;
        private Vector3 _target;
        private Color _color;

        protected override float ReadyPitch => AimPitch + Mathf.Sin(Time.time * DriftSpeed) * DriftPitch;

        protected override float ReadyYaw => AimYaw + Mathf.Sin(Time.time * DriftSpeed * 1.3f) * DriftYaw;

        protected override void OnBuilt(HeroWeaponDefinition definition)
        {
            _color = definition.Color;
            _flash = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(_color, Color.white, 0.5f), Tip, Vector3.zero, Vector3.one * FlashSize, FlashEmission).transform;
            _flash.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            _flash.gameObject.SetActive(false);

            var boltGo = new GameObject("Bolt");
            boltGo.transform.SetParent(Weapon, false);
            _bolt = boltGo.AddComponent<LineRenderer>();
            _bolt.positionCount = 2;
            _bolt.useWorldSpace = true;
            _bolt.widthMultiplier = BoltWidth;
            LineKit.UseBeam(_bolt);
            _bolt.startColor = Color.Lerp(_color, Color.white, 0.6f);
            _bolt.endColor = _color;
            _bolt.enabled = false;
        }

        protected override void OnAttack(Vector3 target)
        {
            _kick = 0f;
            _target = target + Vector3.up * 0.4f;
            _flash.gameObject.SetActive(true);
            _flash.localScale = Vector3.one * FlashSize;
            _bolt.enabled = true;
        }

        protected override void OnHolstered()
        {
            if (_bolt != null)
            {
                _bolt.enabled = false;
                _flash.gameObject.SetActive(false);
            }
        }

        // Aiming turns the torso a little and leans into the gun, like a shooter squaring up; the arm itself is
        // pointed at the target by the view.
        protected override Pose IdlePose(bool walking) => new Pose { Twist = Drawn && !walking ? AimTwist : 0f, Lean = Drawn ? AimLean : 0f, Aim = Drawn ? 1f : 0f };

        protected override Pose Animate()
        {
            if (_kick >= KickSeconds)
            {
                return new Pose { Busy = false };
            }

            _kick += Time.deltaTime;
            float t = Mathf.Clamp01(_kick / KickSeconds);
            float kick = Mathf.Sin(t * Mathf.PI);
            bool lit = _kick < BoltSeconds;
            _flash.gameObject.SetActive(lit);
            _flash.localScale = Vector3.one * (FlashSize * (1.3f - t));
            _bolt.enabled = lit;
            if (lit)
            {
                _bolt.SetPosition(0, Tip.position);
                _bolt.SetPosition(1, _target);
            }

            return new Pose
            {
                ArmPitch = AimPitch + KickPitch * kick,
                ArmYaw = AimYaw,
                Lean = AimLean + KickLean * kick,
                Twist = AimTwist,
                Aim = 1f,
                Recoil = KickPitch * kick,
                Busy = true,
            };
        }
    }
}
