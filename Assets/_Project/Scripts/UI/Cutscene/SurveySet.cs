using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The survey of S-69-CRYS: the blue-green planet of the map, turning under a hologram scanner. A bright ring
    /// sweeps over it from pole to pole again and again, a cage of faint meridians turns around it, and crystal
    /// glints flicker on the side facing us. Everything is posed from time alone.
    /// </summary>
    public sealed class SurveySet : ICutsceneSet
    {
        private const int StarCount = 1600;
        private const float Radius = 10f;
        private const float DegreesPerSecond = 1.6f;
        private const float StartTurn = 18f;
        private const float CloudAlpha = 0.9f;
        private const float RimPower = 3.2f;
        private const float AtmosphereStrength = 1.3f;
        private const float ScanSeconds = 4.2f;
        private const float ScanLift = 1.025f;
        private const float ScanWidth = 0.35f;
        private const int ScanPoints = 96;
        private const int Meridians = 8;
        private const int MeridianPoints = 48;
        private const float MeridianWidth = 0.09f;
        private const float MeridianDegreesPerSecond = 9f;
        private const float ScanIntensity = 3.5f;
        private const float MeridianIntensity = 1.4f;
        private const int Glints = 26;
        private const float GlintSize = 0.55f;
        private const float GlintLift = 1.006f;
        private static readonly Vector3 SunDirection = new Vector3(0.4f, -0.3f, 0.87f);
        private static readonly Color AtmosphereColour = new Color(0.4f, 0.65f, 1f);
        private static readonly Color ScanColour = new Color(0.35f, 1f, 0.9f);
        private static readonly Color MeridianColour = new Color(0.3f, 0.85f, 1f, 0.6f);
        private static readonly Color GlintColour = new Color(0.7f, 0.95f, 1f) * 4f;

        private readonly List<LineRenderer> _meridians = new List<LineRenderer>();
        private readonly List<Transform> _glints = new List<Transform>();
        private readonly List<Vector3> _glintDirections = new List<Vector3>();
        private readonly Vector3[] _scanPoints = new Vector3[ScanPoints];
        private readonly Vector3[] _meridianPoints = new Vector3[MeridianPoints];
        private VisualTheme _theme;
        private CutsceneContext _context;
        private PlanetBody _planet;
        private LineRenderer _scan;
        private LineRenderer _echo;

        public VisualTheme Theme => _theme;

        public void Build(Transform root, CutsceneContext context)
        {
            _context = context;
            _theme = context.FieldLevel.Theme;
            SpaceKit.BuildStars(root, context.Scaled(StarCount));
            _planet = CrysPlanet.Create(root, context.Catalog, Vector3.back, Vector3.zero, Radius, CloudAlpha, AtmosphereColour * AtmosphereStrength, RimPower, StartTurn, DegreesPerSecond);
            SpaceKit.Sun(root, SunDirection, _theme);

            _scan = SpaceKit.Line(root, "Scan", ScanPoints, true, ScanIntensity);
            _echo = SpaceKit.Line(root, "ScanEcho", ScanPoints, true, ScanIntensity);
            for (int i = 0; i < Meridians; i++)
            {
                LineRenderer meridian = SpaceKit.Line(root, $"Meridian{i}", MeridianPoints, false, MeridianIntensity);
                meridian.widthMultiplier = MeridianWidth;
                meridian.startColor = MeridianColour;
                meridian.endColor = MeridianColour;
                _meridians.Add(meridian);
            }

            for (int i = 0; i < Glints; i++)
            {
                // Scattered over the land that faces the camera while the shot plays.
                float longitude = Mathf.Lerp(-55f, 55f, CutsceneMath.Hash(i, 51));
                float latitude = Mathf.Lerp(-50f, 55f, CutsceneMath.Hash(i, 53));
                _glintDirections.Add(PlanetCoordinates.ToDirection(longitude, latitude));
                _glints.Add(SpaceKit.Glow(root, $"Glint{i}", GlintColour));
            }
        }

        public void Enter(CutsceneAction action) => SpaceKit.ApplyDarkness(_theme);

        public void Tick(float time, float progress)
        {
            _planet.Pose(time);
            PoseScan(time);
            PoseMeridians(time);
            PoseGlints(time);
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

        // The ring runs from the north pole to the south and starts over; a dimmer echo follows half a sweep behind.
        private void PoseScan(float time)
        {
            float phase = Mathf.Repeat(time / ScanSeconds, 1f);
            Ring(_scan, phase, ScanWidth * Mathf.Sin(phase * Mathf.PI), ScanColour);
            float echo = Mathf.Repeat(phase + 0.5f, 1f);
            Ring(_echo, echo, ScanWidth * 0.5f * Mathf.Sin(echo * Mathf.PI), ScanColour * 0.4f);
        }

        private void Ring(LineRenderer line, float phase, float width, Color colour)
        {
            float polar = phase * Mathf.PI;
            float radius = Radius * ScanLift * Mathf.Sin(polar);
            float height = Radius * ScanLift * Mathf.Cos(polar);
            Vector3 centre = _planet.Centre;
            for (int i = 0; i < ScanPoints; i++)
            {
                float angle = i / (float)ScanPoints * Mathf.PI * 2f;
                _scanPoints[i] = centre + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            }

            line.SetPositions(_scanPoints);
            line.widthMultiplier = width;
            line.startColor = colour;
            line.endColor = colour;
        }

        private void PoseMeridians(float time)
        {
            Vector3 centre = _planet.Centre;
            for (int m = 0; m < _meridians.Count; m++)
            {
                float yaw = (m / (float)_meridians.Count * 180f + time * MeridianDegreesPerSecond) * Mathf.Deg2Rad;
                var across = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                for (int i = 0; i < MeridianPoints; i++)
                {
                    float polar = i / (float)(MeridianPoints - 1) * Mathf.PI * 2f;
                    _meridianPoints[i] = centre + (across * Mathf.Sin(polar) + Vector3.up * Mathf.Cos(polar)) * (Radius * ScanLift);
                }

                _meridians[m].SetPositions(_meridianPoints);
            }
        }

        // A glint flares where the scan ring has just passed, and only on the side that faces the camera.
        private void PoseGlints(float time)
        {
            Transform view = _context.Camera.transform;
            Quaternion turn = _planet.Rotation;
            Vector3 centre = _planet.Centre;
            float scanHeight = Mathf.Cos(Mathf.Repeat(time / ScanSeconds, 1f) * Mathf.PI);
            for (int i = 0; i < _glints.Count; i++)
            {
                Vector3 normal = turn * _glintDirections[i];
                Vector3 position = centre + normal * (Radius * GlintLift);
                float facing = Vector3.Dot(normal, (view.position - position).normalized);
                float nearScan = Mathf.Clamp01(1f - (normal.y - scanHeight) * 4f) * Mathf.Clamp01((normal.y - scanHeight) * 4f + 1f);
                float twinkle = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * (1.3f + CutsceneMath.Hash(i, 55)) + i * 2.1f)), 6f);
                float size = GlintSize * Mathf.Clamp01(facing * 3f) * Mathf.Max(twinkle, nearScan);
                Transform glint = _glints[i];
                glint.gameObject.SetActive(size > 0.01f);
                glint.SetPositionAndRotation(position, view.rotation);
                glint.localScale = Vector3.one * size;
            }
        }
    }
}
