using Game.Core;
using Game.Hero;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The commander in a cutscene: the same display figure the menus show, posed by a small rig of its own (walk,
    /// run, look around, draw the blaster) instead of the level's <see cref="HeroView"/>, which needs a living
    /// commander behind it. Every pose comes from the values passed in, so a sought moment looks the same as a
    /// played one. The gait numbers follow the level's walk, so the commander moves the same in both.
    /// </summary>
    public sealed class CutsceneHeroRig
    {
        public const float GaitPerUnit = 2.6f;

        private const float LegSwingDegrees = 46f;
        private const float RunSwingExtra = 0.25f;
        private const float KneeFoldDegrees = 62f;
        private const float KneeFoldLead = 0.6f;
        private const float RestHipDegrees = -6f;
        private const float RestKneeDegrees = 30f;
        private const float RunKneeDegrees = 14f;
        private const float StanceStaggerDegrees = 17f;
        private const float FrontKneeExtraDegrees = 8f;
        private const float BackKneeExtraDegrees = -6f;
        private const float FootSplayDegrees = 14f;
        private const float HunchDegrees = 12f;
        private const float WalkLeanDegrees = 15f;
        private const float RunLeanDegrees = 8f;
        private const float RestTwistDegrees = 20f;
        private const float HipCounterTwistShare = 0.15f;
        private const float RelaxedArmPitch = 6f;
        private const float RelaxedArmYaw = -10f;
        private const float AimArmPitch = -82f;
        private const float AimArmYaw = 8f;
        private const float RelaxedForearmDegrees = 62f;
        private const float RunForearmDegrees = 85f;
        private const float DrawnForearmDegrees = 12f;
        private const float ForearmSwingShare = 0.25f;
        private const float FreeArmRestPitch = 14f;
        private const float FreeArmRestYaw = -12f;
        private const float FreeArmAimPitch = -55f;
        private const float FreeArmSwingShare = 0.8f;
        private const float HeadForwardDegrees = 6f;
        private const float WalkBobHeight = 0.065f;
        private const float IdleBobHeight = 0.008f;
        private const float IdleSpeed = 1.6f;
        private const float RunCrouch = 0.04f;
        private const float DrawHandOver = 0.45f;
        private const float RunAbove = 0.5f;
        private const string HeadBone = "Head";

        private readonly Transform _figure;
        private readonly Transform _torso;
        private readonly Transform _head;
        private readonly Transform _armRight;
        private readonly Transform _forearmRight;
        private readonly Transform _armLeft;
        private readonly Transform _forearmLeft;
        private readonly Transform _thighRight;
        private readonly Transform _shinRight;
        private readonly Transform _thighLeft;
        private readonly Transform _shinLeft;
        private readonly Vector3 _figureRest;
        private readonly Transform _blaster;
        private readonly GameObject _blasterEnergy;
        private readonly Transform _shell;
        private readonly HeroModelFactory.Parts _parts;
        private readonly HeroWeaponModelFactory.Model _blasterModel;
        private readonly HeroArtDefinition _artDefinition;
        private readonly ArtModel _art;
        private readonly Transform _pistol;
        private readonly Transform _hand;
        private readonly Transform _holster;
        private readonly Transform _neck;
        private bool _drawn;

        public CutsceneHeroRig(Transform parent, HeroDefinition hero)
        {
            Root = new GameObject("Mercer").transform;
            Root.SetParent(parent, false);
            if (hero.Art != null && hero.Art.Body != null)
            {
                // The imported model, posed by sampling its clips: the same moment always gives the same pose.
                _artDefinition = hero.Art;
                _art = ArtModel.CreateSampled(Root, hero.Art.Body, ownMaterial: false);
                _hand = _art.Find(hero.Art.HandSocket);
                _holster = _art.Find(hero.Art.HolsterSocket);
                _neck = _art.Find(HeadBone);
                if (hero.Art.Pistol != null && _holster != null)
                {
                    ArtModel pistol = ArtModel.CreateStill(_holster, hero.Art.Pistol);
                    HideParts(pistol, hero.Art.HiddenParts);
                    _pistol = pistol.Root;
                }

                if (hero.Art.SwordHilt != null && _art.Find(hero.Art.BackSocket) != null)
                {
                    HideParts(ArtModel.CreateStill(_art.Find(hero.Art.BackSocket), hero.Art.SwordHilt), hero.Art.HiddenParts);
                }

                return;
            }

            _figure = HeroView.BuildDisplayFigure(Root, hero).transform;
            _figureRest = _figure.localPosition;
            _torso = _figure.Find("Torso");
            _head = _torso.Find("Head");
            _shell = _torso.Find("Shell");
            _armRight = _torso.Find("ArmR");
            _forearmRight = _armRight.Find("ForearmR");
            _armLeft = _torso.Find("ArmL");
            _forearmLeft = _armLeft.Find("ForearmL");
            _thighRight = _figure.Find("ThighR");
            _shinRight = _thighRight.Find("ShinR");
            _thighLeft = _figure.Find("ThighL");
            _shinLeft = _thighLeft.Find("ShinL");

            _parts = HeroModelFactory.Create();
            HeroWeaponDefinition ranged = hero.RangedWeapon != null ? hero.RangedWeapon : hero.Weapon;
            _blasterModel = HeroWeaponModelFactory.Create(HeroWeaponStyle.Blaster);
            Material[] materials = HeroWeaponModelFactory.CreateMaterials(hero.Color, ranged != null ? ranged.Color : hero.Color);
            _blaster = new GameObject("Blaster").transform;
            Part("Grip", _blaster, _blasterModel.Grip, materials);
            _blasterEnergy = Part("Energy", _blaster, _blasterModel.Energy, materials);
            Holster();
        }

        public Transform Root { get; }

        /// <summary>The muzzle of the blaster in world space, for a shot or a glint.</summary>
        public Vector3 Muzzle => _art != null ? (_pistol != null ? _pistol.TransformPoint(_artDefinition.Muzzle) : Root.position) : _blaster.TransformPoint(_blasterModel.Tip);

        /// <summary>
        /// Poses the commander. <paramref name="stride"/> 0 stands, 1 walks; <paramref name="run"/> adds the run on
        /// top; <paramref name="gait"/> is the step phase (distance walked times <see cref="GaitPerUnit"/>);
        /// <paramref name="headYaw"/> turns the head; <paramref name="draw"/> brings the blaster from the hip (0) to
        /// aim (1); <paramref name="time"/> drives the breathing.
        /// </summary>
        public void Pose(Vector3 position, float yawDegrees, float stride, float run, float gait, float headYaw, float draw, float time)
        {
            Root.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            if (_art != null)
            {
                PoseArt(stride, run, gait, headYaw, draw, time);
                return;
            }

            stride = Mathf.Clamp01(stride);
            run = Mathf.Clamp01(run) * stride;
            draw = Mathf.Clamp01(draw);
            float standing = 1f - stride;

            float swing = Mathf.Sin(gait) * LegSwingDegrees * stride * (1f + RunSwingExtra * run);
            float foldRight = Mathf.Max(0f, -Mathf.Cos(gait - KneeFoldLead)) * KneeFoldDegrees * stride;
            float foldLeft = Mathf.Max(0f, Mathf.Cos(gait - KneeFoldLead)) * KneeFoldDegrees * stride;
            float stagger = StanceStaggerDegrees * standing;
            float knee = Mathf.Lerp(RestKneeDegrees, RunKneeDegrees, stride);
            _thighRight.localRotation = Quaternion.Euler(RestHipDegrees + stagger + swing, FootSplayDegrees, 0f);
            _thighLeft.localRotation = Quaternion.Euler(RestHipDegrees - stagger - swing, -FootSplayDegrees, 0f);
            _shinRight.localRotation = Quaternion.Euler(knee + BackKneeExtraDegrees * standing + foldRight, 0f, 0f);
            _shinLeft.localRotation = Quaternion.Euler(knee + FrontKneeExtraDegrees * standing + foldLeft, 0f, 0f);

            float bob = Mathf.Lerp(Mathf.Sin(time * IdleSpeed) * IdleBobHeight, Mathf.Abs(Mathf.Sin(gait)) * WalkBobHeight, stride);
            _figure.localPosition = _figureRest + Vector3.up * (bob - RunCrouch * run);

            float twist = RestTwistDegrees * (1f - Mathf.Max(draw, stride)) - swing * HipCounterTwistShare;
            _torso.localRotation = Quaternion.Euler(HunchDegrees + WalkLeanDegrees * stride + RunLeanDegrees * run, twist, 0f);
            _head.localRotation = Quaternion.Euler(HeadForwardDegrees, headYaw, 0f);

            float armPitch = Mathf.Lerp(RelaxedArmPitch - swing * FreeArmSwingShare, AimArmPitch, draw);
            _armRight.localRotation = Quaternion.Euler(armPitch, Mathf.Lerp(RelaxedArmYaw, AimArmYaw, draw), 0f);
            float relaxedForearm = Mathf.Lerp(RelaxedForearmDegrees, RunForearmDegrees, stride);
            _forearmRight.localRotation = Quaternion.Euler(Mathf.Lerp(relaxedForearm - swing * ForearmSwingShare, DrawnForearmDegrees, draw), 0f, 0f);
            _armLeft.localRotation = Quaternion.Euler(Mathf.Lerp(FreeArmRestPitch + swing * FreeArmSwingShare, FreeArmAimPitch, draw * 0.8f), FreeArmRestYaw, 0f);
            _forearmLeft.localRotation = Quaternion.Euler(relaxedForearm + swing * ForearmSwingShare, 0f, 0f);

            bool drawn = draw >= DrawHandOver;
            if (drawn != _drawn)
            {
                if (drawn)
                {
                    Draw();
                }
                else
                {
                    Holster();
                }
            }
        }

        // Standing: the idle at the shot time; moving: the walk or the run at the phase the distance walked gives, so the
        // feet keep to the ground; drawn: the pistol in the hand at the end of the kick, which is the aim.
        private void PoseArt(float stride, float run, float gait, float headYaw, float draw, float time)
        {
            stride = Mathf.Clamp01(stride);
            bool drawn = Mathf.Clamp01(draw) >= DrawHandOver;
            if (drawn)
            {
                _art.Sample(ModelClipRole.Shoot, _art.LengthOf(ModelClipRole.Shoot), loop: false);
            }
            else if (stride <= 0f)
            {
                _art.Sample(ModelClipRole.Idle, time, loop: true);
            }
            else
            {
                ModelClipRole cycle = run >= RunAbove ? ModelClipRole.Move : ModelClipRole.Walk;
                ModelClip clip = _artDefinition.Body.Clips[Mathf.Max(0, _artDefinition.Body.IndexOf(cycle))];
                float distance = gait / GaitPerUnit;
                float phase = clip.DistancePerLoop > 0f ? distance / clip.DistancePerLoop * clip.Length : time;
                _art.Sample(cycle, phase, loop: true);
            }

            if (_neck != null && Mathf.Abs(headYaw) > 0.01f)
            {
                _neck.rotation = Quaternion.AngleAxis(headYaw, Vector3.up) * _neck.rotation;
            }

            if (_pistol != null && drawn != _drawn)
            {
                _drawn = drawn;
                _pistol.SetParent(drawn ? _hand : _holster, false);
                _pistol.localPosition = Vector3.zero;
                _pistol.localRotation = Quaternion.identity;
            }
        }

        private static void HideParts(ArtModel model, string[] parts)
        {
            foreach (Renderer renderer in model.Renderers)
            {
                if (System.Array.IndexOf(parts, renderer.name) >= 0)
                {
                    renderer.enabled = false;
                }
            }
        }

        private void Holster()
        {
            _drawn = false;
            _blaster.SetParent(_shell, false);
            _blaster.localPosition = _parts.HipHolster;
            _blaster.localRotation = _blasterModel.HolsterRotation;
            _blasterEnergy.SetActive(false);
        }

        private void Draw()
        {
            _drawn = true;
            _blaster.SetParent(_forearmRight, false);
            _blaster.localPosition = _parts.Hand;
            _blaster.localRotation = _blasterModel.GripRotation;
            _blasterEnergy.SetActive(true);
        }

        private static GameObject Part(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }
    }
}
