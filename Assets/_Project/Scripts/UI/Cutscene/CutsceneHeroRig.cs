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
        private const string SpineBone = "Spine2";
        private const string UpperArmBone = "UpperArm_R";
        private const string ForearmBone = "Forearm_R";

        // The disbelief: the torso leans back, the right hand comes up to the visor, the head tips up to take it in.
        private const float ReactLeanDegrees = 10f;
        private const float ReactArmRaiseDegrees = 118f;
        private const float ReactArmInDegrees = 28f;
        private const float ReactForearmDegrees = 105f;
        private const float ReactHeadTiltDegrees = 9f;

        // Where in each blink gap the blink falls is hashed from the gap's number with this salt.
        private const int BlinkSalt = 211;

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
        private readonly Transform _spine;
        private readonly Transform _upperArm;
        private readonly Transform _forearm;
        private readonly Transform[] _overridden = new Transform[0];
        private readonly Quaternion[] _overriddenRest = new Quaternion[0];
        // Round 40: the face shapes driven after every sampled pose, and every node of the model for a cross-fade between two clips.
        private readonly FaceRig _face;
        private readonly Transform[] _nodes = new Transform[0];
        private readonly Vector3[] _fadePositions = new Vector3[0];
        private readonly Quaternion[] _fadeRotations = new Quaternion[0];
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
                _spine = _art.Find(SpineBone);
                _upperArm = _art.Find(UpperArmBone);
                _forearm = _art.Find(ForearmBone);
                // The clips leave out bones they never move, so a turned bone keeps its turn into the next pose: the bones the
                // reaction turns go back to their rest before every pose.
                _overridden = new[] { _spine, _upperArm, _forearm, _neck };
                _overriddenRest = new Quaternion[_overridden.Length];
                for (int i = 0; i < _overridden.Length; i++)
                {
                    _overriddenRest[i] = _overridden[i] != null ? _overridden[i].localRotation : Quaternion.identity;
                }

                _face = new FaceRig(_art.Renderers, hero.Art.FaceShapes());
                _nodes = _art.Root.GetComponentsInChildren<Transform>(true);
                _fadePositions = new Vector3[_nodes.Length];
                _fadeRotations = new Quaternion[_nodes.Length];

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

        /// <summary>
        /// The commander's disbelief: stepping back <paramref name="stepBack"/> metres from <paramref name="position"/>
        /// (the walk played backwards while <paramref name="stepping"/>), the right hand raised towards the visor by
        /// <paramref name="hand"/> 0..1, the torso leaning back with it and turned by <paramref name="twist"/>, the head turned by
        /// <paramref name="headYaw"/> (negative: to his left). A negative step moves him forwards, away from what is behind him.
        /// </summary>
        public void React(Vector3 position, float yawDegrees, float stepBack, bool stepping, float hand, float headYaw, float twist, float time)
        {
            Quaternion facing = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 at = position - facing * Vector3.forward * stepBack;
            if (_art == null)
            {
                Pose(at, yawDegrees, stepping ? 0.6f : 0f, 0f, -stepBack * GaitPerUnit, headYaw, 0f, time);
                return;
            }

            Root.SetPositionAndRotation(at, facing);
            RestoreOverrides();
            if (stepping)
            {
                ModelClip clip = _artDefinition.Body.Clips[Mathf.Max(0, _artDefinition.Body.IndexOf(ModelClipRole.Walk))];
                float phase = clip.DistancePerLoop > 0f ? -stepBack / clip.DistancePerLoop * clip.Length : time;
                _art.Sample(ModelClipRole.Walk, phase, loop: true);
            }
            else
            {
                _art.Sample(ModelClipRole.Idle, time, loop: true);
            }

            Vector3 right = facing * Vector3.right;
            float raise = CutsceneMath.Smooth(Mathf.Clamp01(hand));
            Turn(_spine, right, -ReactLeanDegrees * raise);
            Turn(_spine, Vector3.up, twist);
            Turn(_upperArm, right, -ReactArmRaiseDegrees * raise);
            Turn(_upperArm, Vector3.up, -ReactArmInDegrees * raise);
            Turn(_forearm, right, -ReactForearmDegrees * raise);
            Turn(_neck, right, -ReactHeadTiltDegrees * raise);
            Turn(_neck, Vector3.up, headYaw);
            if (_pistol != null && _drawn)
            {
                _drawn = false;
                _pistol.SetParent(_holster, false);
                _pistol.localPosition = Vector3.zero;
                _pistol.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// The commander's disbelief with the Blender clip (README round 35): while <paramref name="stepping"/> he backs
        /// <paramref name="stepBack"/> metres away as in <see cref="React"/>, then plays Disbelief from
        /// <paramref name="seconds"/> into it (the hand to the chin guard, a slow head shake), held on its last frame.
        /// Without the clip it falls back to <see cref="React"/> with the hand up.
        /// </summary>
        public void Disbelief(Vector3 position, float yawDegrees, float stepBack, bool stepping, float seconds, float time)
        {
            if (_art == null || _artDefinition.Body.IndexOf(ModelClipRole.Disbelief) < 0 || stepping)
            {
                React(position, yawDegrees, stepBack, stepping, stepping ? 0f : 1f, 0f, 0f, time);
                return;
            }

            Quaternion facing = Quaternion.Euler(0f, yawDegrees, 0f);
            Root.SetPositionAndRotation(position - facing * Vector3.forward * stepBack, facing);
            RestoreOverrides();
            _art.Sample(ModelClipRole.Disbelief, seconds, loop: false);
            if (_pistol != null && _drawn)
            {
                _drawn = false;
                _pistol.SetParent(_holster, false);
                _pistol.localPosition = Vector3.zero;
                _pistol.localRotation = Quaternion.identity;
            }
        }

        /// <summary>How long the imported figure's clip of <paramref name="role"/> runs; zero when it has none (or no imported figure).</summary>
        public float ClipLength(ModelClipRole role) => _art != null && _artDefinition.Body.IndexOf(role) >= 0 ? _art.LengthOf(role) : 0f;

        /// <summary>
        /// Plays a one-off clip of the imported figure <paramref name="seconds"/> into it (held on its last frame) at a place and
        /// heading; the caller moves the body. Check <see cref="ClipLength"/> first: without the clip nothing is posed.
        /// </summary>
        public void PoseClip(Vector3 position, float yawDegrees, ModelClipRole role, float seconds) => PoseClip(position, yawDegrees, role, seconds, false, false);

        /// <summary>
        /// Plays a clip of the imported figure <paramref name="seconds"/> into it (wrapped when <paramref name="loop"/>, else held on
        /// its last frame), with the pistol in the hand or the holster. Without the clip nothing is posed.
        /// </summary>
        public void PoseClip(Vector3 position, float yawDegrees, ModelClipRole role, float seconds, bool loop, bool pistolInHand)
        {
            Root.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            if (_art == null)
            {
                return;
            }

            RestoreOverrides();
            _art.Sample(role, seconds, loop);
            HoldPistol(pistolInHand);
        }

        /// <summary>
        /// A cross-fade between two clips (round 40: the nervous aim into the slow lower): <paramref name="share"/> 0 is the first
        /// clip at its moment, 1 the second; every node of the model is blended between the two sampled poses.
        /// </summary>
        public void PoseBlend(Vector3 position, float yawDegrees, ModelClipRole from, float fromSeconds, bool fromLoop, ModelClipRole to, float toSeconds, bool toLoop, float share, bool pistolInHand)
        {
            Root.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            if (_art == null)
            {
                return;
            }

            RestoreOverrides();
            _art.Sample(from, fromSeconds, fromLoop);
            for (int i = 0; i < _nodes.Length; i++)
            {
                _fadePositions[i] = _nodes[i].localPosition;
                _fadeRotations[i] = _nodes[i].localRotation;
            }

            _art.Sample(to, toSeconds, toLoop);
            float weight = Mathf.Clamp01(share);
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].localPosition = Vector3.Lerp(_fadePositions[i], _nodes[i].localPosition, weight);
                _nodes[i].localRotation = Quaternion.Slerp(_fadeRotations[i], _nodes[i].localRotation, weight);
            }

            HoldPistol(pistolInHand);
        }

        /// <summary>
        /// A clip that moves the upper body only (the stow) over a cycle that carries the legs: the cycle first, the clip on top.
        /// </summary>
        public void PoseLayered(Vector3 position, float yawDegrees, ModelClipRole cycle, float cycleSeconds, ModelClipRole top, float topSeconds, bool pistolInHand)
        {
            Root.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            if (_art == null)
            {
                return;
            }

            RestoreOverrides();
            _art.Sample(cycle, cycleSeconds, loop: true);
            _art.Sample(top, topSeconds, loop: false);
            HoldPistol(pistolInHand);
        }

        /// <summary>The walk or the run at the gait's phase (as <see cref="Pose"/> moving), with the pistol held in the hand or holstered.</summary>
        public void Travel(Vector3 position, float yawDegrees, float stride, float run, float gait, float time, bool pistolInHand)
        {
            Root.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));
            if (_art == null)
            {
                Pose(position, yawDegrees, stride, run, gait, 0f, 0f, time);
                return;
            }

            RestoreOverrides();
            SampleCycle(stride, run, gait, time);
            HoldPistol(pistolInHand);
        }

        /// <summary>The moment in a clip where its cue falls (the draw's grab, the stow's release), or zero.</summary>
        public float EventOf(ModelClipRole role)
        {
            int index = _art != null ? _artDefinition.Body.IndexOf(role) : -1;
            return index >= 0 ? _artDefinition.Body.Clips[index].EventSeconds : 0f;
        }

        /// <summary>The acting data of one of the commander's clips (its root offset at the first frame, its face), or null.</summary>
        public HeroActingClip Acting(ModelClipRole role) => _art != null ? _artDefinition.ActingFor(role) : null;

        /// <summary>Adds a clip's face tracks at a moment of it (scaled by <paramref name="share"/> for a cross-fade); applied by <see cref="ApplyFace"/>.</summary>
        public void Face(ModelClipRole role, float seconds, float share = 1f)
        {
            HeroActingClip acting = Acting(role);
            if (acting != null)
            {
                _face.Add(acting.Face, seconds, share);
            }
        }

        /// <summary>Adds face tracks the cutscene owns (a beat no clip carries) at a moment of them.</summary>
        public void Face(FaceCurve[] curves, float seconds, float share = 1f) => _face?.Add(curves, seconds, share);

        /// <summary>
        /// Writes the face after the body was sampled (a clip writes its own zero shape curves over it): the tracks added for
        /// this pose, plus a blink now and then from the shot clock alone (the art's blink rhythm), so a seek shows the same face.
        /// </summary>
        public void ApplyFace(float time)
        {
            if (_face == null || !_face.HasFace)
            {
                return;
            }

            float gap = (_artDefinition.BlinkEvery.x + _artDefinition.BlinkEvery.y) * 0.5f;
            int beat = Mathf.FloorToInt(time / gap);
            float start = beat * gap + CutsceneMath.Hash(beat, BlinkSalt) * (gap - _artDefinition.BlinkSeconds);
            float into = (time - start) / _artDefinition.BlinkSeconds;
            if (into >= 0f && into <= 1f)
            {
                _face.AtLeast(_artDefinition.BlinkShape, Mathf.Sin(into * Mathf.PI));
            }

            _face.Apply();
        }

        // The pistol rides the hand while drawn and the holster otherwise, at its grip (identity in the socket).
        private void HoldPistol(bool inHand)
        {
            if (_pistol == null || _drawn == inHand)
            {
                return;
            }

            _drawn = inHand;
            _pistol.SetParent(inHand ? _hand : _holster, false);
            _pistol.localPosition = Vector3.zero;
            _pistol.localRotation = Quaternion.identity;
        }

        private void RestoreOverrides()
        {
            for (int i = 0; i < _overridden.Length; i++)
            {
                if (_overridden[i] != null)
                {
                    _overridden[i].localRotation = _overriddenRest[i];
                }
            }
        }

        // A turn about a world axis through the bone, on top of the sampled pose.
        private static void Turn(Transform bone, Vector3 axis, float degrees)
        {
            if (bone != null && Mathf.Abs(degrees) > 0.01f)
            {
                bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
            }
        }

        // Standing: the idle at the shot time; moving: the walk or the run at the phase the distance walked gives, so the
        // feet keep to the ground; drawn: the pistol in the hand at the end of the kick, which is the aim.
        private void PoseArt(float stride, float run, float gait, float headYaw, float draw, float time)
        {
            RestoreOverrides();
            stride = Mathf.Clamp01(stride);
            bool drawn = Mathf.Clamp01(draw) >= DrawHandOver;
            if (drawn)
            {
                _art.Sample(ModelClipRole.Shoot, _art.LengthOf(ModelClipRole.Shoot), loop: false);
            }
            else
            {
                SampleCycle(stride, run, gait, time);
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

        // Standing: the idle at the shot time; moving: the walk or the run at the phase the distance walked gives.
        private void SampleCycle(float stride, float run, float gait, float time)
        {
            if (stride <= 0f)
            {
                _art.Sample(ModelClipRole.Idle, time, loop: true);
                return;
            }

            ModelClipRole cycle = run >= RunAbove ? ModelClipRole.Move : ModelClipRole.Walk;
            ModelClip clip = _artDefinition.Body.Clips[Mathf.Max(0, _artDefinition.Body.IndexOf(cycle))];
            float distance = gait / GaitPerUnit;
            float phase = clip.DistancePerLoop > 0f ? distance / clip.DistancePerLoop * clip.Length : time;
            _art.Sample(cycle, phase, loop: true);
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
