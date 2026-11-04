using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The prologue's Earth of 2053, seen from orbit: a grey-brown planet with murky seas, open-pit scars and a few
    /// dying amber city lights on its night side, wrapped in brown smog and a dirty haze along the rim, turning
    /// slowly under a pale sun. ORA tells the story over it in her hologram boxes. The surface comes from the cast's
    /// Blender maps (README round 26) when they are there, else from the <c>EarthSurface</c> texture or <see cref="EarthPainter"/>.
    /// </summary>
    public sealed class EarthSet : ICutsceneSet
    {
        private const int MapWidth = 1024;
        private const int LowMapWidth = 512;
        private const float Radius = 10f;
        private const float AxisTilt = 23f;
        private const float StartTurn = 150f;
        private const float DegreesPerSecond = 1.2f;
        private const float SmogAlpha = 0.6f;
        private static readonly Vector3 SunDirection = new Vector3(0.7f, -0.2f, 0.65f);

        private VisualTheme _theme;
        private PlanetBody _earth;
        private SpaceBackdrop _backdrop;
        private Transform _view;

        public VisualTheme Theme => _theme;

        public void Build(Transform root, CutsceneContext context)
        {
            CutsceneCast cast = context.Cast;
            _theme = cast != null && cast.EarthTheme != null ? cast.EarthTheme : context.FieldLevel.Theme;
            SpaceStyle space = CrysPlanet.SpaceOf(context.Catalog);
            _view = context.Camera.transform;

            Quaternion tilt = Quaternion.AngleAxis(AxisTilt, Vector3.forward) * Quaternion.AngleAxis(StartTurn, Vector3.up);
            PlanetStyle style = cast != null ? cast.EarthPlanet : PlanetStyle.Earth();
            EarthMaps painted = cast != null ? cast.EarthMapsFor(context.ParticleScale < 1f) : null;
            if (painted != null)
            {
                // The Blender Earth: its own maps, and the smog deck tinted by its painted colour.
                var maps = new GlobePainter.Maps { Albedo = painted.Albedo, Normal = painted.Normal, Gloss = painted.Gloss, Emission = painted.Emission };
                _earth = new PlanetBody(root, "Earth", Vector3.zero, Radius, PlanetMaterials.Surface(maps, painted.Smog, style), painted.Smog, SmogAlpha, style, tilt, DegreesPerSecond);
                _earth.TintCloudsFromMap();
            }
            else
            {
                EarthPainter.Painting painting = EarthPainter.Paint(context.ParticleScale < 1f ? LowMapWidth : MapWidth);
                GlobePainter.Maps maps = painting.Maps;
                if (cast != null && cast.EarthSurface != null)
                {
                    maps = new GlobePainter.Maps { Albedo = cast.EarthSurface, Normal = maps.Normal, Gloss = maps.Gloss, Emission = maps.Emission };
                }

                _earth = new PlanetBody(root, "Earth", Vector3.zero, Radius, PlanetMaterials.Surface(maps, painting.Smog, style), painting.Smog, SmogAlpha, style, tilt, DegreesPerSecond);
            }

            _earth.SetSun(SpaceKit.Sun(root, SunDirection, _theme));
            _backdrop = new SpaceBackdrop(root, space, SunDirection, context.ParticleScale < 1f);
        }

        public void Enter(CutsceneAction action) => SpaceKit.ApplyDarkness(_theme);

        public void Tick(float time, float progress)
        {
            _earth.Pose(time);
            _backdrop.Pose(_view);
        }

        public void Exit()
        {
        }

        public bool TryGetAnchor(CameraAnchor anchor, out Vector3 origin, out Quaternion facing)
        {
            origin = Vector3.zero;
            facing = Quaternion.identity;
            return false;
        }
    }
}
