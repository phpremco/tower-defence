using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    /// <summary>
    /// The look of a shot, chosen by its <see cref="ProjectileStyle"/> and coloured like the tower that fired it. The
    /// weapon's <see cref="WeaponVfxDefinition"/> brings the painted parts: a glowing bolt, slug, tracer or plasma
    /// sprite laid along the flight, a ribbon trail, a rocket's flame and smoke ribbon, a beam or crackling arc from
    /// the muzzle to the target. Without a recipe the plain procedural parts stand in (a streak, a tracer shell, a
    /// missile with a flame, a line beam, a frost shard, a jagged arc, a lobbed mortar shell). One pooled object
    /// carries every part and turns on what the style needs; a shot only sets properties. Reacts to Projectile
    /// events only.
    /// </summary>
    [RequireComponent(typeof(Projectile))]
    public sealed class ProjectileView : MonoBehaviour
    {
        private const float CoreEmission = 4f;
        private const float BeamEmission = 5f;
        private const float BeamWidth = 0.1f;
        private const float BeamGlowWidth = 0.36f;
        private const float BeamGlowAlpha = 0.3f;
        private static readonly Vector3 SlugScale = new Vector3(0.1f, 0.3f, 0.1f);
        private static readonly Vector3 BoltScale = new Vector3(0.07f, 0.16f, 0.07f);
        private static readonly Vector3 FlakScale = new Vector3(0.06f, 0.14f, 0.06f);
        private static readonly Color FlakHot = new Color(1f, 0.97f, 0.88f);
        private static readonly Color FlakEdge = new Color(1f, 0.68f, 0.28f);
        private static readonly Color ShellTracer = new Color(1f, 0.75f, 0.4f);
        private static readonly Color ExhaustGlow = new Color(1f, 0.55f, 0.2f);
        private const float BoltTrailWidth = 0.07f;
        private const float BeamLingerSeconds = 0.3f;
        private const float ArcLingerSeconds = 0.1f;
        private const int ArcPoints = 9;
        private const float ArcJitter = 0.22f;
        private const float ArcFlickerSeconds = 0.03f;
        private const float MortarHeight = 1.6f;
        private const float RocketWobble = 0.08f;
        private const float RocketWobbleSpeed = 20f;
        private const float Whiteness = 0.55f;
        private const int ShellSmoke = 4;
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int FrameId = Shader.PropertyToID("_Frame");

        private Projectile _projectile;
        private Transform _visual;
        private Transform _core;
        private Transform _slug;
        private Transform _missile;
        private TrailRenderer _trail;
        private TrailRenderer _smokeTrail;
        private LineRenderer _beam;
        private LineRenderer _beamGlow;
        private BeamFade _beamFade;
        private BeamFade _beamGlowFade;
        private ParticleSystem _flame;
        private ParticleSystem _frost;
        private ParticleSystem _smoke;
        private MeshRenderer _bolt;
        private MeshRenderer _exhaust;
        private MaterialPropertyBlock _block;
        private Material _plainTrail;
        private Gradient _ribbonFade;
        private Material _plainBeam;
        private VfxLibrary _library;
        private WeaponVfxDefinition _weapon;
        private Transform _camera;
        private bool _low;
        private bool _arcCard;
        private float _flicker;
        private float _wobblePhase;
        private float _frameClock;

        private void Awake()
        {
            _projectile = GetComponent<Projectile>();
            _projectile.Launched += OnLaunched;
            _projectile.Impacted += OnImpacted;
            Build();
        }

        /// <summary>The recipes to dress shots from, the texture size of the profile and the camera the cards face.</summary>
        public void Configure(VfxLibrary library, bool low, Camera camera)
        {
            _library = library;
            _low = low;
            Camera facing = camera != null ? camera : Camera.main;
            _camera = facing != null ? facing.transform : null;
        }

        private void OnDestroy()
        {
            if (_projectile != null)
            {
                _projectile.Launched -= OnLaunched;
                _projectile.Impacted -= OnImpacted;
            }
        }

        private void Build()
        {
            _block = new MaterialPropertyBlock();
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            _core = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.white, _visual, Vector3.zero, Vector3.one * 0.2f, CoreEmission).transform;
            // A rounded bar that flies lengthwise: the capsule's long axis is turned onto the heading.
            _slug = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Capsule, Color.white, _visual, Vector3.zero, SlugScale, CoreEmission).transform;
            _slug.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _missile = BuildMissile(_visual);
            ShadowsOff(_visual);

            _bolt = VfxQuad.Create(transform, "Bolt", VfxQuad.Centred);
            _exhaust = VfxQuad.Create(transform, "Exhaust", VfxQuad.FromStart);

            _ribbonFade = FadingGradient(1f);
            _plainTrail = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.minVertexDistance = 0.03f;
            _trail.shadowCastingMode = ShadowCastingMode.Off;
            _trail.sharedMaterial = _plainTrail;
            _trail.textureMode = LineTextureMode.Stretch;

            var smokeTrail = new GameObject("SmokeTrail");
            smokeTrail.transform.SetParent(transform, false);
            _smokeTrail = smokeTrail.AddComponent<TrailRenderer>();
            _smokeTrail.minVertexDistance = 0.06f;
            _smokeTrail.shadowCastingMode = ShadowCastingMode.Off;
            _smokeTrail.textureMode = LineTextureMode.Tile;
            _smokeTrail.colorGradient = FadingGradient(0.8f);
            _smokeTrail.emitting = false;
            _smokeTrail.enabled = false;

            _beam = Line("Beam", BeamWidth);
            _beamGlow = Line("BeamGlow", BeamGlowWidth);
            _beamFade = _beam.GetComponent<BeamFade>();
            _beamGlowFade = _beamGlow.GetComponent<BeamFade>();

            _flame = ParticleKit.Create(_visual, "Flame", Vector3.zero, Color.white, 0.12f, 0.25f, 0.08f, 0.16f, 0.2f, 0.6f, 0f, 60, 0.03f);
            _frost = ParticleKit.Create(_visual, "Frost", Vector3.zero, Color.white, 0.3f, 0.6f, 0.04f, 0.09f, 0.3f, 0.9f, 0.3f, 60, 0.08f);
            _smoke = ParticleKit.Create(_visual, "Smoke", Vector3.zero, new Color(0.3f, 0.3f, 0.3f, 0.3f), 0.5f, 0.9f, 0.12f, 0.22f, 0.1f, 0.3f, -0.08f, 40, 0.05f);
        }

        // A white ribbon that thins to nothing at its tail; the weapon colour comes from the material tint.
        private static Gradient FadingGradient(float alpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        // A small missile: a body, a glowing nose and three fins, pointing along +z.
        private static Transform BuildMissile(Transform parent)
        {
            var missile = new GameObject("Missile").transform;
            missile.SetParent(parent, false);
            GameObject body = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Capsule, new Color(0.85f, 0.88f, 0.95f), missile, Vector3.zero, new Vector3(0.09f, 0.16f, 0.09f), 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.white, missile, new Vector3(0f, 0f, 0.17f), Vector3.one * 0.07f, CoreEmission);
            for (int i = 0; i < 3; i++)
            {
                GameObject fin = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, new Color(0.15f, 0.17f, 0.22f), missile, Quaternion.Euler(0f, 0f, i * 120f) * new Vector3(0f, 0.06f, -0.12f), new Vector3(0.01f, 0.06f, 0.08f), 0f);
                fin.transform.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
            }

            return missile;
        }

        // Beams hang beside the projectile, not under it, so they can linger after it returns to the pool.
        private LineRenderer Line(string name, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform.parent, false);
            var line = go.AddComponent<LineRenderer>();
            go.AddComponent<BeamFade>();
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.positionCount = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            line.enabled = false;
            return line;
        }

        private static void ShadowsOff(Transform root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private void OnLaunched(Projectile projectile)
        {
            Color color = projectile.Color;
            Color bright = Color.Lerp(color, Color.white, Whiteness);
            ProjectileStyle style = projectile.Style;
            _weapon = _library != null ? _library.WeaponFor(style) : null;
            _wobblePhase = Random.Range(0f, 10f);
            _frameClock = Random.Range(0f, 1f);
            _visual.localPosition = Vector3.zero;
            bool flak = style == ProjectileStyle.Flak;

            bool core = style == ProjectileStyle.Shell || style == ProjectileStyle.Frost || style == ProjectileStyle.Mortar;
            bool painted = _weapon != null && _weapon.Projectile != null;
            _core.gameObject.SetActive(core && !(painted && style == ProjectileStyle.Frost));
            _core.localScale = Vector3.one * (style == ProjectileStyle.Mortar ? 0.3f : style == ProjectileStyle.Shell ? 0.16f : 0.2f);
            _core.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.MaterialFor(style == ProjectileStyle.Shell ? new Color(0.2f, 0.2f, 0.22f) : bright, style == ProjectileStyle.Shell ? 0f : CoreEmission);

            // Bolts, rail slugs and flak tracers are glowing rounded bars: the rail's longer and brighter, the flak's white-hot.
            bool slug = style == ProjectileStyle.Rail || style == ProjectileStyle.Bolt || flak;
            _slug.gameObject.SetActive(slug && !painted);
            _slug.localScale = style == ProjectileStyle.Rail ? SlugScale : flak ? FlakScale : BoltScale;
            _slug.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.MaterialFor(flak ? FlakHot : bright, style == ProjectileStyle.Rail || flak ? CoreEmission * 1.5f : CoreEmission);
            ShowCard(_bolt, painted ? _weapon.Projectile : null, flak ? FlakHot : color);

            _missile.gameObject.SetActive(style == ProjectileStyle.Rocket);
            bool exhaust = style == ProjectileStyle.Rocket && _weapon != null && _weapon.Exhaust != null;
            ShowCard(_exhaust, exhaust ? _weapon.Exhaust : null, ExhaustGlow);

            DressTrail(style, flak ? FlakEdge : style == ProjectileStyle.Shell ? ShellTracer : bright, color);
            DressBeam(style, color, bright);

            SetColor(_flame, Color.Lerp(new Color(1f, 0.6f, 0.2f), Color.white, 0.3f) * 2f);
            SetColor(_frost, bright * 1.5f);
            ParticleKit.SetRate(_flame, style == ProjectileStyle.Rocket && !exhaust ? 120f : 0f);
            ParticleKit.SetRate(_frost, style == ProjectileStyle.Frost ? 60f : 0f);
            bool smokeRibbon = style == ProjectileStyle.Rocket && _weapon != null && _weapon.SmokeTrail != null;
            ParticleKit.SetRate(_smoke, style == ProjectileStyle.Rocket && !smokeRibbon ? 30f : 0f);
            _smokeTrail.Clear();
            _smokeTrail.enabled = smokeRibbon;
            _smokeTrail.emitting = smokeRibbon;
            if (smokeRibbon)
            {
                _smokeTrail.sharedMaterial = _weapon.SmokeTrail.MaterialFor(_low);
                _smokeTrail.widthMultiplier = _weapon.SmokeTrailWidth;
                _smokeTrail.time = _weapon.SmokeTrailSeconds;
            }

            if (style == ProjectileStyle.Shell || style == ProjectileStyle.Mortar)
            {
                _smoke.Emit(ShellSmoke);
            }

            LateUpdate();
        }

        private void ShowCard(MeshRenderer card, VfxSheet sheet, Color tint)
        {
            bool show = sheet != null && _camera != null;
            card.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            card.sharedMaterial = sheet.MaterialFor(_low);
            _block.Clear();
            _block.SetColor(TintId, tint);
            _block.SetFloat(FrameId, 0f);
            card.SetPropertyBlock(_block);
        }

        private void DressTrail(ProjectileStyle style, Color plainColour, Color tint)
        {
            bool trail = style != ProjectileStyle.Laser && style != ProjectileStyle.Lightning;
            _trail.emitting = trail;
            _trail.enabled = trail;
            _trail.Clear();
            if (!trail)
            {
                return;
            }

            if (_weapon != null && _weapon.Trail != null)
            {
                _trail.sharedMaterial = _weapon.Trail.MaterialFor(_low);
                _trail.time = _weapon.TrailSeconds;
                _trail.widthMultiplier = _weapon.TrailWidth;
                _trail.colorGradient = _ribbonFade;
                _block.Clear();
                _block.SetColor(TintId, style == ProjectileStyle.Shell ? ShellTracer : style == ProjectileStyle.Flak ? FlakEdge : tint);
                _trail.SetPropertyBlock(_block);
                return;
            }

            _trail.sharedMaterial = _plainTrail;
            _trail.SetPropertyBlock(null);
            _trail.time = style == ProjectileStyle.Rail ? 0.35f : style == ProjectileStyle.Shell || style == ProjectileStyle.Flak ? 0.1f : 0.18f;
            _trail.widthMultiplier = style == ProjectileStyle.Rail ? 0.22f : style == ProjectileStyle.Rocket ? 0.12f : style == ProjectileStyle.Bolt || style == ProjectileStyle.Flak ? BoltTrailWidth : 0.14f;
            SetGradient(_trail, plainColour, style == ProjectileStyle.Rail ? 1f : 0.8f);
        }

        private void DressBeam(ProjectileStyle style, Color color, Color bright)
        {
            bool beam = style == ProjectileStyle.Laser || style == ProjectileStyle.Lightning;
            VfxSheet sheet = beam && _weapon != null ? _weapon.Beam : null;
            _arcCard = sheet != null && style == ProjectileStyle.Lightning;
            if (!beam)
            {
                _beamFade.Hide();
                _beamGlowFade.Hide();
                return;
            }

            if (sheet != null)
            {
                // One painted line carries core and glow; a painted arc spans the whole way and swaps its picture.
                _beamFade.Show(_weapon.BeamWidth);
                _beamGlowFade.Hide();
                _beam.positionCount = 2;
                // Stretched: the painted core fades out at both ends of its strip, so tiling it would dash the beam.
                _beam.textureMode = LineTextureMode.Stretch;
                _beam.sharedMaterial = sheet.MaterialFor(_low);
                _beam.startColor = Color.white;
                _beam.endColor = Color.white;
                _block.Clear();
                _block.SetColor(TintId, color);
                _block.SetFloat(FrameId, Random.Range(0, sheet.Frames));
                _beam.SetPropertyBlock(_block);
                _flicker = 0f;
                return;
            }

            _beamFade.Show(BeamWidth);
            _beamGlowFade.Show(BeamGlowWidth);
            _beam.positionCount = style == ProjectileStyle.Lightning ? ArcPoints : 2;
            _beamGlow.positionCount = _beam.positionCount;
            _beam.textureMode = LineTextureMode.Stretch;
            _beam.SetPropertyBlock(null);
            // An opaque glowing core, so the bloom catches the beam and the halo never hides it.
            _beam.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(bright, Color.white, 0.6f), BeamEmission);
            _beamGlow.startColor = new Color(color.r, color.g, color.b, BeamGlowAlpha);
            _beamGlow.endColor = new Color(color.r, color.g, color.b, BeamGlowAlpha * 0.5f);
            _flicker = 0f;
        }

        // The beam stays where it struck and thins out, so a hit reads as a flash rather than a blink.
        private void OnImpacted(Vector3 position, float radius, Color color, ProjectileStyle style)
        {
            if (_projectile.Style == ProjectileStyle.Laser || _projectile.Style == ProjectileStyle.Lightning)
            {
                float seconds = _projectile.Style == ProjectileStyle.Laser ? BeamLingerSeconds : ArcLingerSeconds;
                _beamFade.Begin(seconds);
                _beamGlowFade.Begin(seconds);
            }
        }

        private static void SetColor(ParticleSystem system, Color color)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
        }

        private static void SetGradient(TrailRenderer trail, Color color, float alpha)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.5f), 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
        }

        private void LateUpdate()
        {
            Vector3 heading = _projectile.Destination - transform.position;
            if (heading.sqrMagnitude > 0.0001f)
            {
                _visual.rotation = Quaternion.LookRotation(heading);
            }

            switch (_projectile.Style)
            {
                case ProjectileStyle.Laser:
                    Beam(false);
                    break;
                case ProjectileStyle.Lightning:
                    Beam(true);
                    break;
                case ProjectileStyle.Mortar:
                    _visual.localPosition = Vector3.up * (Mathf.Sin(_projectile.Progress * Mathf.PI) * MortarHeight);
                    break;
                case ProjectileStyle.Rocket:
                    float wobble = Mathf.Sin(Time.time * RocketWobbleSpeed + _wobblePhase) * RocketWobble * (1f - _projectile.Progress);
                    _visual.localPosition = _visual.right * wobble;
                    break;
            }

            PlaceCards(heading);
        }

        // The bolt lies along the flight with its head forward; the flame reaches back from the rocket's tail.
        private void PlaceCards(Vector3 heading)
        {
            if (_camera == null || _weapon == null)
            {
                return;
            }

            _frameClock += Time.deltaTime;
            Vector3 forward = _camera.forward;
            if (_bolt.gameObject.activeSelf)
            {
                VfxSheet sheet = _weapon.Projectile;
                Transform bolt = _bolt.transform;
                bolt.SetPositionAndRotation(_visual.position, VfxQuad.Along(heading, forward));
                bolt.localScale = new Vector3(_weapon.ProjectileLength, _weapon.ProjectileWidth, 1f);
                Animate(_bolt, sheet);
            }

            if (_exhaust.gameObject.activeSelf)
            {
                Transform exhaust = _exhaust.transform;
                Vector3 back = heading.sqrMagnitude > 0.0001f ? -heading.normalized : -_visual.forward;
                exhaust.SetPositionAndRotation(_visual.position + back * 0.12f, VfxQuad.Along(back, forward));
                exhaust.localScale = new Vector3(_weapon.ExhaustLength, _weapon.ExhaustLength * _weapon.Exhaust.CellAspect, 1f);
                Animate(_exhaust, _weapon.Exhaust);
            }
        }

        // A looping sheet (plasma, flame) steps through its cells at its own rate.
        private void Animate(MeshRenderer card, VfxSheet sheet)
        {
            if (sheet.Frames <= 1 || sheet.Fps <= 0f)
            {
                return;
            }

            card.GetPropertyBlock(_block);
            _block.SetFloat(FrameId, Mathf.Floor(_frameClock * sheet.Fps) % sheet.Frames);
            card.SetPropertyBlock(_block);
        }

        // The beam runs from the muzzle to wherever the shot has got to, so it reaches out and then snaps away.
        // Lightning jitters its middle points every few frames, or a painted arc swaps to another picture.
        private void Beam(bool jagged)
        {
            Vector3 from = _projectile.Origin;
            Vector3 to = transform.position;
            if (!jagged || _arcCard)
            {
                _beam.SetPosition(0, from);
                _beam.SetPosition(1, to);
                _beamGlow.SetPosition(0, from);
                _beamGlow.SetPosition(1, to);
                if (_arcCard)
                {
                    Flicker();
                }

                return;
            }

            _flicker -= Time.deltaTime;
            if (_flicker > 0f)
            {
                return;
            }

            _flicker = ArcFlickerSeconds;
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.up);
            for (int i = 0; i < ArcPoints; i++)
            {
                float t = i / (float)(ArcPoints - 1);
                float amplitude = Mathf.Sin(t * Mathf.PI) * ArcJitter;
                Vector3 point = Vector3.Lerp(from, to, t) + side * Random.Range(-amplitude, amplitude) + Vector3.up * Random.Range(-amplitude, amplitude) * 0.5f;
                _beam.SetPosition(i, point);
                _beamGlow.SetPosition(i, point);
            }
        }

        private void Flicker()
        {
            _flicker -= Time.deltaTime;
            if (_flicker > 0f)
            {
                return;
            }

            _flicker = ArcFlickerSeconds;
            _beam.GetPropertyBlock(_block);
            _block.SetFloat(FrameId, Random.Range(0, _weapon.Beam.Frames));
            _beam.SetPropertyBlock(_block);
        }
    }
}
