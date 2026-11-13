using System;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Landing capsules leaving the mothership's bay one after another and falling to the planet: each drops out under
    /// the open bay in a puff of cold gas, lying along the ship, and coasts straight down, clear of the hull, while it
    /// grows to its flight size and pitches into its burn attitude; only then does its deorbit booster
    /// (<see cref="CapsuleStage"/>) light, aimed down and away towards the landing site so the plume points up and out,
    /// away from the hull and the bay where the next capsule appears. It burns with a hot plume and a trail of exhaust
    /// smoke, then blows off in a flash, a spray of sparks and its tumbling clamps. The capsule goes on alone into the
    /// air, where the heat shield glows, a sheath of plasma wraps it and a white-hot head with a layered burning trail
    /// lights up the air round it, so each capsule ends as a falling star. Round 44 (Remco: "the launching should only launch
    /// once"): the drop happens once; a capsule that has gone into the air stays gone. Round 59 (Remco chose the salvo): the
    /// cast's cargo capsules leave first, smaller and marked, each on its own way to another region of the planet (the first
    /// to the landing site with the base modules), and Spencer's capsule leaves last; each sinks behind the planet's rim while
    /// its glows fade out, and a spent booster drifts on tumbling instead of vanishing. Every pose follows from time alone;
    /// trails and smoke are the path itself sampled back in time, never a history of frames.
    /// </summary>
    public sealed class CapsuleDrop
    {
        private const float FirstRelease = 1.2f;
        private const float FallSeconds = 6.5f;
        // Round 36: a little larger than true scale against the ship, so the booster, its burn and the separation read from the orbit lens.
        private const float CapsuleScale = 0.85f;
        private const float AtmosphereDepth = 0.35f;
        private const int TrailPoints = 22;
        private const float TrailStep = 0.07f;
        private const float TrailWidth = 0.7f;
        private const float HaloWidth = 2.4f;
        private const float HeadSize = 3f;
        private const float ShieldEmission = 5f;
        private const float TrailIntensity = 3f;
        private const float HaloIntensity = 1.2f;
        private const float ColdHead = 0.25f;
        private const float BayWarmUp = 0.4f;
        private const float BayFade = 1.6f;
        // Round 59: the head, the trails, the air glow and the sheath fade out over the last of the fall, so a capsule sinking
        // behind the rim leaves no glow to switch off.
        private const float EndFadeSeconds = 1.1f;
        // How hard a cargo capsule's burn shakes the lens next to the pathfinder's (the shot follows his).
        private const float CargoRumble = 0.3f;

        // Round 37: the release. The capsule appears lying along the ship just under the open bay (the stack is far longer
        // than the bay is deep, so it cannot hang in it), at a size that fits between the open doors, and coasts straight
        // down out of the bay's axis while it grows and pitches; its centre is this deep under the bay socket, then falls
        // at this speed and acceleration (metres, m/s, m/s^2). The booster lights once the plume at full length is clear of
        // the hull (Tools/PlayTest/r37_orbit_probe.cs checks it against the ship's mesh).
        private const float StartScale = 0.55f;
        private const float StartDepth = 0.6f;
        private const float PushSpeed = 1.4f;
        private const float PushAcceleration = 2.6f;
        // Round 59: the capsule slides out of the hull instead of appearing under it: it starts this much higher, inside the bay
        // where the hull hides it from the lens, and joins the round-37 coast this long after its release (eased, so from then
        // on its path, its ignition and its burn are where they were).
        private const float EmergeRise = 0.9f;
        private const float EmergeSeconds = 0.5f;
        // The cold head glow and a marker light come up once the capsule is out of the bay, and the trail grows in after the
        // ignition, so no glow switches on at full size.
        private const float GlowInDelay = 0.3f;
        private const float GlowInSeconds = 0.5f;
        private const float TrailInSeconds = 0.3f;
        // How far the burn attitude leans from straight down towards the landing site (horizontal share of the axis).
        private const float BurnLean = 0.6f;
        // The stack's middle above the capsule's root, in model units (shield at -0.9, bell exit at 2.58): it pitches about it.
        private const float StackCentre = 0.84f;
        // How the fall leaves the coast: share of its pace at the start (the rest builds up as it falls).
        private const float FallStartPace = 0.6f;
        // After the separation the capsule turns its shield into the path over this long.
        private const float AlignSeconds = 0.9f;
        // The fall bends through a point this far above and this far back (towards the lens) from the landing site, in planet radii.
        private const float SwingLift = 0.25f;
        private const float SwingBack = 0.2f;
        // Round 59: a capsule bound for the far side would cut through the planet on the straight curve; its path is kept above
        // a floor this share of the radius over the ground, sinking to the ground as the fall ends, and bends onto it over this
        // many metres, so it skims over the face to the rim instead.
        private const float GroundClearance = 0.03f;
        private const float GroundBlend = 1.5f;
        // Over the last of the fall the floor sinks this far under the ground, so a capsule skimming towards the rim goes down
        // behind it instead of hanging just over it, where the lens would still see it as its fall ends.
        private const float EndSink = 2f;
        private const float SinkFrom = 0.9f;

        // The booster's burn (seconds after release): the coast first, then the burn until the separation.
        private const float IgniteAge = 1.7f;
        private const float SeparateAge = 3.3f;

        // Exhaust smoke left behind the bell while it burns, and the cold gas of the push out of the bay.
        private const float SmokeGap = 0.07f;
        private const float LowSmokeGap = 0.14f;
        private const float SmokeLife = 1.8f;
        private const float SmokeSpeed = 2.4f;
        // Exhaust leaves a moving booster: the puffs keep this share of its speed, so they trail it instead of blowing back to the ship.
        private const float SmokeCarry = 0.6f;
        private const float SmokeGrowth = 1.3f;
        private const float SmokeAlpha = 0.42f;
        private const int SteamPuffs = 10;
        private const int LowSteamPuffs = 5;
        private const float SteamLife = 1.3f;
        private const float SteamSpeed = 1.4f;
        private const float SteamAlpha = 0.5f;
        // The cold gas blows out of the bay's mouth: down, spread mostly along the bay's length, little towards its doors.
        private const float SteamAlongBay = 0.7f;
        private const float SteamAcrossBay = 0.25f;

        // The separation: a flash and sparks thrown out round the joint.
        private const int Sparks = 16;
        private const int LowSparks = 8;
        private const float SparkLife = 0.9f;
        private const float FlashSize = 5f;
        private const float FlashDecay = 7f;

        // Re-entry: the plasma sheath and the glow it throws on the air round the capsule.
        private const float SheathAhead = 0.45f;
        private const float SheathRadius = 0.42f;
        private const float SheathLength = 6f;
        private const float AirGlowSize = 14f;
        private const float VelocityStep = 0.05f;

        // Round 59: a cargo capsule's marker light, on its flank towards the lens (model units above the root and out from the
        // axis), breathing slowly instead of blinking so it never pops; it dims to this share between breaths.
        private const float MarkerHeight = 0.25f;
        private const float MarkerOut = 0.55f;
        private const float MarkerSize = 1.1f;
        private const float MarkerRest = 0.3f;
        private const float MarkerPeriod = 1.3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color HotColour = new Color(1f, 0.5f, 0.15f);
        private static readonly Color CoreColour = new Color(1f, 0.85f, 0.6f) * 3f;
        private static readonly Color SheathCore = new Color(1f, 0.9f, 0.75f) * 5f;
        private static readonly Color SheathEdge = new Color(1f, 0.32f, 0.2f) * 2.4f;
        private static readonly Color AirGlow = new Color(1f, 0.42f, 0.22f) * 0.55f;
        private static readonly Color SmokeColour = new Color(0.72f, 0.76f, 0.84f, 1f);
        private static readonly Color SteamColour = new Color(0.9f, 0.95f, 1f, 1f);
        private static readonly Color SparkColour = new Color(1f, 0.75f, 0.4f) * 4f;
        private static readonly Color FlashColour = new Color(1f, 0.95f, 0.85f) * 5f;

        private readonly List<Capsule> _capsules = new List<Capsule>();
        private readonly Vector3[] _trail = new Vector3[TrailPoints];
        private readonly Func<float, Vector3> _bayAt;
        private readonly Vector3 _planetCentre;
        private readonly float _planetRadius;
        private readonly Transform _view;
        private readonly float _gap;
        private readonly float _smokeGap;
        private readonly int _steamPuffs;
        private readonly int _sparks;
        private readonly SpriteBatch _smoke;
        private readonly SpriteBatch _glints;
        // The ship's axes (it does not turn during the shot): out of the bay, along the bay, across it.
        private readonly Vector3 _down;
        private readonly Vector3 _along;
        private readonly Vector3 _across;

        /// <param name="bayAt">Where the bay opening is at a moment of the shot (the ship moves).</param>
        /// <param name="ship">How the ship lies: the bay opens along its down, its length runs along its forward.</param>
        public CapsuleDrop(Transform parent, CutsceneContext context, Color glow, Func<float, Vector3> bayAt, Quaternion ship, Vector3 planetCentre, float planetRadius)
        {
            _bayAt = bayAt;
            _down = ship * Vector3.down;
            _along = ship * Vector3.forward;
            _across = ship * Vector3.right;
            _planetCentre = planetCentre;
            _planetRadius = planetRadius;
            _view = context.Camera.transform;
            CutsceneCast cast = context.Cast;
            // Round 59: every capsule of the salvo flies on every profile, because its launch sounds are cued per capsule.
            int count = cast != null ? cast.CapsuleCount : 1;
            _gap = cast != null ? cast.ReleaseGap : 0f;
            bool low = context.ParticleScale < 1f;
            _smokeGap = low ? LowSmokeGap : SmokeGap;
            _steamPuffs = low ? LowSteamPuffs : SteamPuffs;
            _sparks = low ? LowSparks : Sparks;

            GameObject custom = cast != null ? cast.Capsule : null;
            ModelArtDefinition art = cast != null ? cast.CapsuleArt : null;
            ModelArtDefinition booster = cast != null ? cast.BoosterArt : null;
            CapsuleModelFactory.Parts parts = CapsuleModelFactory.Create();
            Material[] materials = ModelPalette.Create(glow);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0f), new GradientColorKey(HotColour, 0.25f), new GradientColorKey(new Color(0.8f, 0.2f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.3f), new GradientAlphaKey(0f, 1f) });
            var halo = new Gradient();
            halo.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.6f, 0.35f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.25f), 0.4f), new GradientColorKey(new Color(0.5f, 0.15f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0.35f, 0.35f), new GradientAlphaKey(0f, 1f) });
            for (int i = 0; i < count; i++)
            {
                bool pathfinder = i == count - 1;
                CapsuleFlight flight = FlightOf(cast, i, pathfinder);
                Capsule capsule = BuildCapsule(parent, i, flight, art, custom, parts, materials, gradient, halo, booster, !low);
                capsule.Pathfinder = pathfinder;
                capsule.Size = flight.Scale;
                capsule.Target = TargetOf(flight.Aim);
                _capsules.Add(capsule);
            }

            int puffs = Mathf.CeilToInt((SeparateAge - IgniteAge + SmokeLife) / _smokeGap) + 1;
            Texture2D dot = SpaceKit.Sprite(CutsceneSprite.GlowSoft, SpaceKit.SoftDotTexture);
            _smoke = new SpriteBatch(parent, "CapsuleSmoke", count * (puffs + _steamPuffs), SmokeTexture.Puff, false, Color.white);
            _glints = new SpriteBatch(parent, "CapsuleSparks", count * (_sparks + 1), dot, true, Color.white);
        }

        public void Pose(float time)
        {
            _smoke.Begin();
            _glints.Begin();
            for (int i = 0; i < _capsules.Count; i++)
            {
                PoseCapsule(i, time);
            }

            _smoke.End(time);
            _glints.End(time);
        }

        /// <summary>
        /// How hard the camera should rumble at a moment, 0..1: a kick as each booster lights, a low rumble while it burns
        /// and a jolt at the separation, strongest for the pathfinder's capsule (the one the shot follows).
        /// </summary>
        public float Rumble(float time)
        {
            float rumble = 0f;
            for (int i = 0; i < _capsules.Count; i++)
            {
                float age = Age(i, time);
                if (age < IgniteAge || age > FallSeconds)
                {
                    continue;
                }

                float weight = _capsules[i].Pathfinder ? 1f : CargoRumble;
                float lit = age - IgniteAge;
                float burn = age < SeparateAge ? 0.25f + 0.6f * Mathf.Exp(-lit * 3f) : 0f;
                float jolt = age >= SeparateAge ? 0.7f * Mathf.Exp(-(age - SeparateAge) * 5f) : 0f;
                rumble = Mathf.Max(rumble, (burn + jolt) * weight);
            }

            return Mathf.Clamp01(rumble);
        }

        /// <summary>
        /// How brightly the open bay is lit at a moment, 0..1: it brightens just before each capsule leaves and fades as
        /// the capsule falls clear, so a light in the bay shows the racked capsules and the doors at each drop.
        /// </summary>
        public float BayGlow(float time)
        {
            float glow = 0f;
            for (int i = 0; i < _capsules.Count; i++)
            {
                float release = Release(i);
                if (time < release - BayWarmUp)
                {
                    continue;
                }

                float age = time - release;
                float g = age < 0f ? 1f + age / BayWarmUp : Mathf.Exp(-age * BayFade);
                glow = Mathf.Max(glow, g);
            }

            return glow;
        }

        /// <summary>When capsule <paramref name="index"/> of a salvo leaves the bay, in seconds of the launch shot, the capsules <paramref name="gap"/> apart.</summary>
        public static float ReleaseAt(int index, float gap) => FirstRelease + index * gap;

        /// <summary>When the pathfinder's capsule, the last of <paramref name="capsules"/>, leaves the bay (the generator's cues match).</summary>
        public static float PathfinderRelease(int capsules, float gap) => ReleaseAt(Mathf.Max(0, capsules - 1), gap);

        /// <summary>How long a capsule takes from the bay to the air, after which it is gone for good.</summary>
        public static float Lifetime => FallSeconds;

        /// <summary>Seconds after its release that a capsule's booster lights.</summary>
        public static float IgnitionAge => IgniteAge;

        /// <summary>Seconds after its release that a capsule's booster blows off.</summary>
        public static float SeparationAge => SeparateAge;

        /// <summary>
        /// Round 59: how strongly a capsule's glows still show <paramref name="age"/> seconds after its release, 1 through the
        /// fall and eased to 0 at its end, so the last of a fall fades instead of switching off.
        /// </summary>
        public static float EndFade(float age) => CutsceneMath.Smooth(Mathf.Clamp01((FallSeconds - age) / EndFadeSeconds));

        /// <summary>
        /// Round 59: where on a planet a capsule aimed at <paramref name="aim"/> (planet radii from the top: across, away from the
        /// lens) comes down, just under the surface so the shield meets the ground.
        /// </summary>
        public static Vector3 LandingPoint(Vector2 aim, Vector3 planetCentre, float planetRadius)
        {
            var direction = new Vector3(aim.x, 1f, aim.y);
            return planetCentre + direction.normalized * (planetRadius * 0.995f);
        }

        /// <summary>How many capsules this launch releases.</summary>
        public int Count => _capsules.Count;

        /// <summary>When capsule <paramref name="index"/> leaves the bay in this launch.</summary>
        public float Release(int index) => ReleaseAt(index, _gap);

        /// <summary>True for the pathfinder's capsule (the last one).</summary>
        public bool IsPathfinder(int index) => _capsules[index].Pathfinder;

        /// <summary>True while a capsule is out of the bay and on its way down at this moment of the shot.</summary>
        public bool InFlight(int index, float time)
        {
            float age = Age(index, time);
            return age >= 0f && age < FallSeconds;
        }

        /// <summary>Where capsule <paramref name="index"/> comes down.</summary>
        public Vector3 TargetOf(int index) => _capsules[index].Target;

        // The flight of capsule i: the cargo flights in order, the pathfinder's last; a cast without them drops one capsule on
        // the pathfinder's lane.
        private static CapsuleFlight FlightOf(CutsceneCast cast, int index, bool pathfinder)
        {
            if (cast == null)
            {
                return new CapsuleFlight();
            }

            if (pathfinder)
            {
                return cast.PathfinderFlight;
            }

            CapsuleFlight[] cargo = cast.CargoFlights;
            return index < cargo.Length && cargo[index] != null ? cargo[index] : cast.PathfinderFlight;
        }

        private Vector3 TargetOf(Vector2 aim) => LandingPoint(aim, _planetCentre, _planetRadius);

        private float Age(int index, float time)
        {
            float release = Release(index);
            return time < release ? -1f : time - release;
        }

        private void PoseCapsule(int i, float time)
        {
            Capsule capsule = _capsules[i];
            float age = Age(i, time);
            bool released = age >= 0f;
            bool falling = released && age < FallSeconds;
            capsule.Root.gameObject.SetActive(falling);
            bool burning = falling && age >= IgniteAge;
            capsule.Trail.enabled = burning;
            capsule.Halo.enabled = burning;
            capsule.Head.gameObject.SetActive(falling);
            capsule.AirGlow.gameObject.SetActive(falling);
            if (capsule.Marker != null)
            {
                capsule.Marker.gameObject.SetActive(falling);
            }

            if (!released)
            {
                capsule.Stage?.Hide();
                capsule.Sheath.Pose(Vector3.zero, Vector3.zero, 0f, 0f, 0f, 0f, _view);
                return;
            }

            float launched = time - age;
            if (falling)
            {
                PoseFall(capsule, i, launched, age, time);
            }
            else
            {
                capsule.Sheath.Pose(Vector3.zero, Vector3.zero, 0f, 0f, 0f, 0f, _view);
            }

            // The spent booster drifts on, tumbling, after its capsule has gone: it shrinks with distance instead of vanishing.
            PoseStage(capsule, i, launched, age, time, ScaleAt(i, age));
            AddSteam(i, launched, age);
            AddSmoke(i, launched, age);
            AddSeparation(i, launched, age);
        }

        private void PoseFall(Capsule capsule, int i, float launched, float age, float time)
        {
            Pose frame = Frame(i, launched, age);
            float scale = ScaleAt(i, age);
            float size = capsule.Size;
            float fade = EndFade(age);
            float glowIn = GlowIn(age);
            float trailIn = CutsceneMath.Fade(age - IgniteAge, TrailInSeconds);
            Vector3 position = frame.position;
            Vector3 forward = Velocity(i, launched, age).normalized;
            float heat = Heat(position);
            capsule.Root.SetPositionAndRotation(position, frame.rotation);
            capsule.Root.localScale = Vector3.one * (scale / CapsuleScale);
            capsule.Shield.SetColor("_EmissionColor", HotColour * (ShieldEmission * heat * heat * fade));
            capsule.Head.SetPositionAndRotation(position, _view.rotation);
            capsule.Head.localScale = Vector3.one * ((HeadSize * heat + ColdHead) * (scale / CapsuleScale) * fade * glowIn);
            // The trail is the burning fall: nothing of it reaches back past the burn towards the ship.
            for (int p = 0; p < TrailPoints; p++)
            {
                _trail[p] = PathPoint(i, launched, Mathf.Max(IgniteAge, age - p * TrailStep));
            }

            capsule.Trail.SetPositions(_trail);
            capsule.Trail.widthMultiplier = TrailWidth * size * Mathf.Max(0.1f, heat) * fade * trailIn;
            capsule.Halo.SetPositions(_trail);
            capsule.Halo.widthMultiplier = HaloWidth * size * heat * fade * trailIn;
            capsule.AirGlow.SetPositionAndRotation(position - forward * 1.2f, _view.rotation);
            capsule.AirGlow.localScale = Vector3.one * (AirGlowSize * size * heat * heat * fade);
            float plasma = heat * Mathf.Sqrt(heat) * fade;
            capsule.Sheath.Pose(position + forward * (SheathAhead * size), -forward, SheathRadius * size * (0.7f + 0.5f * heat), SheathLength * size * (0.4f + heat), plasma, time, _view);
            PoseMarker(capsule, i, frame, scale, fade * glowIn, time);
        }

        // A cargo capsule's marker light: on its flank towards the lens, breathing slowly, fading with the end of the fall.
        private void PoseMarker(Capsule capsule, int i, Pose frame, float scale, float fade, float time)
        {
            if (capsule.Marker == null)
            {
                return;
            }

            Vector3 up = frame.rotation * Vector3.up;
            Vector3 centre = frame.position + up * (MarkerHeight * scale);
            Vector3 toView = Vector3.ProjectOnPlane(_view.position - centre, up).normalized;
            float breath = 0.5f + 0.5f * Mathf.Sin((time / MarkerPeriod + CutsceneMath.Hash(i, 57)) * Mathf.PI * 2f);
            float glow = Mathf.Lerp(MarkerRest, 1f, breath * breath) * fade;
            capsule.Marker.SetPositionAndRotation(centre + toView * (MarkerOut * scale), _view.rotation);
            capsule.Marker.localScale = Vector3.one * (MarkerSize * scale * glow);
        }

        private void PoseStage(Capsule capsule, int i, float launched, float age, float time, float scale)
        {
            if (capsule.Stage == null)
            {
                return;
            }

            Pose mount = Mount(i, launched, age);
            Pose separation = Mount(i, launched, SeparateAge);
            capsule.Stage.Pose(mount, separation, Velocity(i, launched, SeparateAge), age - IgniteAge, age - SeparateAge, time, _view, scale);
        }

        // The cold gas of the push: a burst out of the bay's mouth as the capsule drops clear of it, hiding the moment it
        // leaves; it blows down and spreads along the bay, never back into the hull or the doors.
        private void AddSteam(int i, float launched, float age)
        {
            if (age > SteamLife)
            {
                return;
            }

            Vector3 bay = _bayAt(launched + age);
            float u = age / SteamLife;
            for (int p = 0; p < _steamPuffs; p++)
            {
                float angle = (p + CutsceneMath.Hash(i * 31 + p, 81)) / _steamPuffs * Mathf.PI * 2f;
                Vector3 outward = _down + _along * (Mathf.Cos(angle) * SteamAlongBay) + _across * (Mathf.Sin(angle) * SteamAcrossBay);
                Vector3 centre = bay + outward * (SteamSpeed * age * (0.7f + 0.6f * CutsceneMath.Hash(p, 83)));
                Color colour = SteamColour;
                colour.a = SteamAlpha * (1f - u) * (1f - u);
                _smoke.Add(centre, 0.15f + 0.6f * age, colour, angle);
            }
        }

        // Puffs of exhaust left behind the bell: thrown back along the plume but carried on with much of the booster's
        // speed, so they drift behind it and spread as they thin.
        private void AddSmoke(int i, float launched, float age)
        {
            float last = Mathf.Min(age, SeparateAge);
            float mouthScale = CapsuleScale * _capsules[i].Size;
            for (float emitted = IgniteAge; emitted <= last; emitted += _smokeGap)
            {
                float puff = age - emitted;
                if (puff > SmokeLife)
                {
                    continue;
                }

                Pose mount = Mount(i, launched, emitted);
                Vector3 back = mount.rotation * Vector3.up;
                Vector3 carried = Velocity(i, launched, emitted) * SmokeCarry;
                int seed = i * 977 + Mathf.RoundToInt(emitted * 100f);
                var spread = new Vector3(CutsceneMath.Hash(seed, 85) - 0.5f, CutsceneMath.Hash(seed, 87) - 0.5f, CutsceneMath.Hash(seed, 89) - 0.5f);
                Vector3 centre = CapsuleStage.MouthOf(mount, mouthScale) + (back * SmokeSpeed + carried) * puff + spread * (0.8f * puff);
                float u = puff / SmokeLife;
                Color colour = SmokeColour;
                colour.a = SmokeAlpha * Mathf.Pow(1f - u, 1.5f) * Mathf.Clamp01(puff * 8f);
                _smoke.Add(centre, 0.12f + SmokeGrowth * puff, colour, CutsceneMath.Hash(seed, 91) * Mathf.PI * 2f);
            }
        }

        // The separation: a white flash at the joint and sparks thrown out round it, carried on with the capsule's speed.
        private void AddSeparation(int i, float launched, float age)
        {
            float since = age - SeparateAge;
            if (since < 0f || since > SparkLife)
            {
                return;
            }

            float size = _capsules[i].Size;
            Pose joint = Mount(i, launched, SeparateAge);
            Vector3 velocity = Velocity(i, launched, SeparateAge);
            _glints.Add(joint.position + velocity * since, FlashSize * size * Mathf.Exp(-since * FlashDecay), FlashColour);
            float fade = 1f - since / SparkLife;
            for (int s = 0; s < _sparks; s++)
            {
                int seed = i * 131 + s;
                var direction = new Vector3(CutsceneMath.Hash(seed, 93) - 0.5f, CutsceneMath.Hash(seed, 95) - 0.5f, CutsceneMath.Hash(seed, 97) - 0.5f).normalized;
                float speed = 2f + 3.5f * CutsceneMath.Hash(seed, 99);
                Vector3 centre = joint.position + velocity * (since * 0.8f) + direction * (speed * since);
                _glints.Add(centre, (0.09f * fade + 0.02f) * size, SparkColour * fade);
            }
        }

        // The capsule's mount for its booster at an age: the apex of the capsule model, up pointing away from the shield.
        private Pose Mount(int index, float launched, float age)
        {
            Pose frame = Frame(index, launched, age);
            Vector3 up = frame.rotation * Vector3.up;
            return new Pose(frame.position + up * (ScaleAt(index, age) * (CapsuleStage.MountHeight - 0.9f)), frame.rotation);
        }

        // Where the capsule's root is and how it lies: the coast under the bay (pitching about the stack's middle), then
        // the fall, the shield turned into the path after the separation.
        private Pose Frame(int index, float launched, float age)
        {
            Vector3 up = UpAt(index, launched, age);
            Vector3 root = age < IgniteAge ? CoastCentre(launched, age) - up * (StackCentre * ScaleAt(index, age)) : PathPoint(index, launched, age);
            return new Pose(root, Facing(up));
        }

        // The way from the shield to the booster: along the ship (booster aft) as it leaves, leaning into the burn
        // attitude through the coast, held through the burn, then turned to trail the path.
        private Vector3 UpAt(int index, float launched, float age)
        {
            Vector3 burn = BurnUp(index, launched);
            if (age < IgniteAge)
            {
                return Vector3.Slerp(-_along, burn, CutsceneMath.Smooth(age / IgniteAge));
            }

            if (age < SeparateAge)
            {
                return burn;
            }

            Vector3 trailing = -Velocity(index, launched, age).normalized;
            return Vector3.Slerp(burn, trailing, CutsceneMath.Smooth(Mathf.Clamp01((age - SeparateAge) / AlignSeconds)));
        }

        // The burn attitude: the shield down and leaning towards the capsule's landing site, so the plume points up and
        // away from the site, off the hull and off the bay the next capsule leaves.
        private Vector3 BurnUp(int index, float launched)
        {
            Vector3 toSite = _capsules[index].Target - _bayAt(launched);
            Vector3 level = Vector3.ProjectOnPlane(toSite, _down).normalized;
            return -(_down + level * BurnLean).normalized;
        }

        // The up axis never runs along the ship's width in this shot, so it is a steady roll reference for every attitude.
        private Quaternion Facing(Vector3 up) => Quaternion.LookRotation(-up, _across) * Quaternion.Euler(-90f, 0f, 0f);

        // The capsule's size at an age: it grows out of the bay to its flight size, a cargo capsule to its share of the pathfinder's.
        private float ScaleAt(int index, float age) => Mathf.Lerp(StartScale, CapsuleScale, CutsceneMath.Smooth(Mathf.Clamp01(age / IgniteAge))) * _capsules[index].Size;

        // The stack's middle through the coast: moving with the ship, straight out of the bay and gathering speed.
        private Vector3 CoastCentre(float launched, float age) => _bayAt(launched + age) + _down * CoastDepth(age);

        private static float CoastDepth(float age) => StartDepth + PushSpeed * age + 0.5f * PushAcceleration * age * age - EmergeRise * (1f - CutsceneMath.Fade(age, EmergeSeconds));

        /// <summary>Round 59: how far the glows of a capsule <paramref name="age"/> seconds out of the bay have come up, 0..1.</summary>
        public static float GlowIn(float age) => CutsceneMath.Fade(age - GlowInDelay, GlowInSeconds);

        private Vector3 Velocity(int index, float launched, float age) => (PathPoint(index, launched, age + VelocityStep) - PathPoint(index, launched, age)) / VelocityStep;

        // The root's path: the coast (stack upright at its end) and then a falling curve that keeps speeding up into the
        // air, leaving the coast at its speed and in its direction.
        private Vector3 PathPoint(int index, float launched, float age)
        {
            if (age < IgniteAge)
            {
                return CoastCentre(launched, age) - UpAt(index, launched, age) * (StackCentre * ScaleAt(index, age));
            }

            float fall = FallSeconds - IgniteAge;
            float s = Mathf.Clamp01((age - IgniteAge) / fall);
            float eased = FallStartPace * s + (1f - FallStartPace) * s * s * (1.6f - 0.6f * s);
            Vector3 bay = _bayAt(launched + IgniteAge);
            Vector3 start = CoastCentre(launched, IgniteAge) - BurnUp(index, launched) * (StackCentre * CapsuleScale * _capsules[index].Size) - bay;
            float speed = PushSpeed + PushAcceleration * IgniteAge;
            Vector3 lead = start + _down * (speed * fall / (3f * FallStartPace));
            Vector3 target = _capsules[index].Target;
            Vector3 swing = target + new Vector3(0f, _planetRadius * SwingLift, -_planetRadius * SwingBack) - bay;
            target -= bay;
            float a = 1f - eased;
            Vector3 relative = a * a * a * start + 3f * a * a * eased * lead + 3f * a * eased * eased * swing + eased * eased * eased * target;
            return AboveGround(_bayAt(launched + age) + relative, s);
        }

        // Keeps a point of the fall above the floor over the planet for its share of the fall.
        private Vector3 AboveGround(Vector3 point, float share)
        {
            Vector3 fromCentre = point - _planetCentre;
            float distance = fromCentre.magnitude;
            float floor = _planetRadius * (1f + GroundClearance * (1f - share)) - EndSink * CutsceneMath.Smooth(CutsceneMath.Span(share, SinkFrom, 1f));
            return _planetCentre + fromCentre * (CutsceneMath.SmoothMax(distance, floor, GroundBlend) / distance);
        }

        private float Heat(Vector3 position)
        {
            float altitude = Vector3.Distance(position, _planetCentre) - _planetRadius;
            return CutsceneMath.Smooth(1f - Mathf.Clamp01(altitude / (_planetRadius * AtmosphereDepth)));
        }

        private Capsule BuildCapsule(Transform parent, int index, CapsuleFlight flight, ModelArtDefinition art, GameObject custom, CapsuleModelFactory.Parts parts, Material[] materials, Gradient gradient, Gradient halo, ModelArtDefinition booster, bool shimmer)
        {
            var root = new GameObject($"Capsule{index}").transform;
            root.SetParent(parent, false);
            var model = new GameObject("Model").transform;
            model.SetParent(root, false);
            model.localScale = Vector3.one * CapsuleScale;
            model.localPosition = Vector3.down * (CapsuleScale * 0.9f);
            bool tinted = flight.HullTint != Color.white;
            Material shield;
            if (art != null)
            {
                // The imported capsule with its legs folded (the first frame of its landing); its heat shield wears its own glow.
                // Round 59: a cargo capsule wears its own copy of the hull material in its tint.
                ArtModel capsule = ArtModel.CreateSampled(model, art, ownMaterial: tinted);
                if (tinted)
                {
                    Tint(capsule.Material, flight.HullTint);
                }

                capsule.Sample(ModelClipRole.Land, 0f, loop: false);
                shield = CapsuleArt.OwnShield(capsule);
            }
            else
            {
                if (custom != null)
                {
                    UnityEngine.Object.Instantiate(custom, model);
                }
                else
                {
                    AddMesh("Hull", model, parts.Hull, tinted ? Tinted(materials, flight.HullTint) : materials);
                }

                shield = SurfaceMaterials.Create(new Color(0.12f, 0.1f, 0.09f), 1f, 0.3f);
                AddMesh("Shield", model, parts.Shield, new[] { shield });
            }

            LineRenderer trail = SpaceKit.Line(parent, $"Trail{index}", TrailPoints, false, TrailIntensity, CutsceneSprite.PlasmaTrail);
            trail.colorGradient = gradient;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            // A wide, dim second trail round the hot one: the air the capsule has set glowing.
            LineRenderer haloTrail = SpaceKit.Line(parent, $"TrailHalo{index}", TrailPoints, false, HaloIntensity, CutsceneSprite.PlasmaTrail);
            haloTrail.colorGradient = halo;
            haloTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.15f));
            Transform head = SpaceKit.Glow(parent, $"Head{index}", CoreColour);
            Transform air = SpaceKit.Glow(parent, $"AirGlow{index}", AirGlow);
            Transform marker = flight.HasMarkings ? SpaceKit.Glow(parent, $"Marker{index}", flight.Markings, CutsceneSprite.RunningLight) : null;
            var sheath = new ExhaustPlume(parent, $"Sheath{index}", SheathCore, SheathEdge, 1f, false);
            CapsuleStage stage = booster != null ? new CapsuleStage(parent, index, booster, CapsuleScale * flight.Scale, shimmer) : null;
            return new Capsule { Root = root, Shield = shield, Trail = trail, Halo = haloTrail, Head = head, AirGlow = air, Marker = marker, Sheath = sheath, Stage = stage };
        }

        private static void Tint(Material material, Color tint)
        {
            int id = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
            if (material.HasProperty(id))
            {
                material.SetColor(id, material.GetColor(id) * tint);
            }
        }

        private static Material[] Tinted(Material[] materials, Color tint)
        {
            var copies = new Material[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                copies[i] = new Material(materials[i]);
                Tint(copies[i], tint);
            }

            return copies;
        }

        private static void AddMesh(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        private sealed class Capsule
        {
            public Transform Root;
            public Material Shield;
            public LineRenderer Trail;
            public LineRenderer Halo;
            public Transform Head;
            public Transform AirGlow;
            public Transform Marker;
            public ExhaustPlume Sheath;
            public CapsuleStage Stage;
            public bool Pathfinder;
            public float Size = 1f;
            public Vector3 Target;
        }
    }
}
