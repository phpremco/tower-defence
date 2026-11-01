using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Animates one weapon in the commander's hand: holstered when the fighting is over, drawn when it starts,
    /// and an attack animation per hit. Each weapon style has its own rig (a sword swings, a gun aims and kicks)
    /// and the rig tells the view how to hold the arm and the body each frame through a <see cref="Pose"/>. Pure
    /// view code; it reacts to what the view passes in and never touches game logic.
    /// </summary>
    public abstract class HeroWeaponRig
    {
        /// <summary>How the arm and the body should be held this frame.</summary>
        public struct Pose
        {
            public float ArmPitch;
            public float ArmYaw;

            /// <summary>How far the whole figure steps towards the enemy.</summary>
            public float Lunge;

            /// <summary>Forward lean of the torso, in degrees.</summary>
            public float Lean;

            /// <summary>Turn of the torso around its own axis, in degrees.</summary>
            public float Twist;

            /// <summary>How much the arm should point straight at the target (one for a gun at aim), overriding the arm angles.</summary>
            public float Aim;

            /// <summary>The kick of a shot: how far the muzzle rises, in degrees.</summary>
            public float Recoil;

            /// <summary>True while an attack animation plays.</summary>
            public bool Busy;
        }

        private const float CombatHoldSeconds = 4f;
        private const float RelaxedPitch = 6f;
        private const float RelaxedYaw = -10f;
        private const float ArmSwingShare = 0.8f;
        private const float PoseFollowSpeed = 7f;
        private const float IgniteSeconds = 0.18f;
        private const float DropSpin = 420f;
        private const float DropSeconds = 0.5f;

        private Transform _arm;
        private Transform _shell;
        private Transform _figure;
        private Vector3 _hand;
        private Vector3 _holster;
        private float _combatUntil = float.NegativeInfinity;
        private float _ignite;
        private float _armPitch = RelaxedPitch;
        private float _armYaw = RelaxedYaw;
        private bool _dropped;
        private float _dropTime;
        private Vector3 _dropFrom;
        private Vector3 _dropTo;
        private Quaternion _dropRotation;

        protected HeroWeaponDefinition Definition { get; private set; }

        protected HeroWeaponModelFactory.Model Model { get; private set; }

        protected Transform Weapon { get; private set; }

        protected Transform Energy { get; private set; }

        protected Transform Tip { get; private set; }

        /// <summary>True while the weapon is in the hand.</summary>
        public bool Drawn { get; private set; }

        /// <summary>How far the energy part has powered up, from zero to one.</summary>
        protected float Ignite => _ignite;

        /// <summary>The arm pose the rig holds when nothing is happening while drawn.</summary>
        protected abstract float ReadyPitch { get; }

        protected abstract float ReadyYaw { get; }

        public static HeroWeaponRig For(HeroWeaponDefinition definition)
        {
            switch (definition.Style)
            {
                case HeroWeaponStyle.Blaster: return new BlasterRig();
                default: return new SwordRig();
            }
        }

        /// <summary>Builds the weapon meshes and hangs them on the figure, holstered.</summary>
        public void Build(HeroWeaponDefinition definition, HeroModelFactory.Parts parts, Transform figure, Transform shell, Transform arm, Color trim)
        {
            Definition = definition;
            Model = HeroWeaponModelFactory.Create(definition.Style);
            _figure = figure;
            _shell = shell;
            _arm = arm;
            _hand = parts.Hand;
            _holster = Model.OnBack ? parts.BackHolster : parts.HipHolster;

            Material[] materials = HeroWeaponModelFactory.CreateMaterials(trim, definition.Color);
            Weapon = new GameObject("Weapon").transform;
            Part("Grip", Weapon, Model.Grip, materials);
            Energy = Part("Energy", Weapon, Model.Energy, materials);
            Tip = new GameObject("Tip").transform;
            Tip.SetParent(Weapon, false);
            Tip.localPosition = Model.Tip;
            OnBuilt(definition);
            Holster();
        }

        protected virtual void OnBuilt(HeroWeaponDefinition definition)
        {
        }

        private static Transform Part(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }

        /// <summary>The hero attacks something at this position: draw if needed and play the attack.</summary>
        public void Attack(Vector3 target)
        {
            if (_dropped)
            {
                return;
            }

            _combatUntil = Time.time + CombatHoldSeconds;
            if (!Drawn)
            {
                Draw();
            }

            OnAttack(target);
        }

        /// <summary>The weapon rests on the pack or the hip, powered down.</summary>
        public void Holster()
        {
            Drawn = false;
            _dropped = false;
            Weapon.SetParent(_shell, false);
            Weapon.localPosition = _holster;
            Weapon.localRotation = Model.HolsterRotation;
            Weapon.localScale = Vector3.one;
            Energy.gameObject.SetActive(false);
            OnHolstered();
        }

        /// <summary>The weapon comes to the hand; the energy part powers up over a few frames.</summary>
        public void Draw()
        {
            Drawn = true;
            _dropped = false;
            _ignite = 0f;
            Weapon.SetParent(_arm, false);
            Weapon.localPosition = _hand;
            Weapon.localRotation = Model.GripRotation;
            Weapon.localScale = Vector3.one;
            Energy.gameObject.SetActive(true);
            OnDrawn();
        }

        /// <summary>The weapon falls out of the hand and lies where it lands, until the hero recovers it.</summary>
        public void Drop(float groundY)
        {
            if (!Drawn)
            {
                Draw();
            }

            _dropped = true;
            _dropTime = 0f;
            Energy.gameObject.SetActive(false);
            _dropFrom = Weapon.position;
            Vector3 away = _figure.right * 0.5f + _figure.forward * 0.3f;
            _dropTo = new Vector3(_dropFrom.x + away.x, groundY + 0.05f, _dropFrom.z + away.z);
            _dropRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            Weapon.SetParent(null, true);
            OnHolstered();
        }

        /// <summary>Per frame: holsters once the fighting has been over for a while, powers up, animates.</summary>
        public Pose Tick(bool walking, float legSwing)
        {
            if (_dropped)
            {
                Fall();
                return RestPose(walking, legSwing);
            }

            Pose pose = Animate();
            if (Drawn && !pose.Busy && Time.time >= _combatUntil)
            {
                Holster();
            }

            if (Drawn && _ignite < 1f)
            {
                _ignite = Mathf.Min(1f, _ignite + Time.deltaTime / IgniteSeconds);
                OnIgnite(_ignite);
            }

            if (!pose.Busy)
            {
                return RestPose(walking, legSwing);
            }

            _armPitch = pose.ArmPitch;
            _armYaw = pose.ArmYaw;
            return pose;
        }

        // The arm hangs and swings with the stride, or holds the weapon ready while drawn.
        private Pose RestPose(bool walking, float legSwing)
        {
            float wantedPitch = Drawn ? ReadyPitch : RelaxedPitch - legSwing * ArmSwingShare;
            float wantedYaw = Drawn ? ReadyYaw : RelaxedYaw;
            float k = 1f - Mathf.Exp(-PoseFollowSpeed * Time.deltaTime);
            _armPitch = Mathf.Lerp(_armPitch, wantedPitch, k);
            _armYaw = Mathf.Lerp(_armYaw, wantedYaw, k);
            Pose rest = IdlePose(walking);
            rest.ArmPitch = _armPitch;
            rest.ArmYaw = _armYaw;
            rest.Busy = false;
            return rest;
        }

        // The dropped weapon tumbles to the ground beside the hero and stays there.
        private void Fall()
        {
            if (_dropTime >= DropSeconds)
            {
                return;
            }

            _dropTime += Time.deltaTime;
            float t = Mathf.Clamp01(_dropTime / DropSeconds);
            Vector3 position = Vector3.Lerp(_dropFrom, _dropTo, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.25f;
            Weapon.position = position;
            Weapon.rotation = t < 1f ? Quaternion.Slerp(Weapon.rotation, _dropRotation, t) * Quaternion.AngleAxis(DropSpin * Time.deltaTime * (1f - t), Vector3.forward) : _dropRotation;
        }

        /// <summary>The idle stance (lean, twist) while drawn or relaxed; the arm is filled in by the base.</summary>
        protected virtual Pose IdlePose(bool walking) => new Pose();

        /// <summary>The attack animation. Return Busy = false once it has finished.</summary>
        protected abstract Pose Animate();

        protected abstract void OnAttack(Vector3 target);

        protected virtual void OnDrawn()
        {
        }

        protected virtual void OnHolstered()
        {
        }

        protected virtual void OnIgnite(float fraction)
        {
        }

        protected static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
