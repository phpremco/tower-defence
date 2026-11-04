using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The dark under the field, built from the Blender cave kit (README round 26): rock walls down both sides with a
    /// leaning vault above, ceiling slabs, stalactites, rubble floors with glowing fissures and veins, boulders, and
    /// crystal clusters that tremble with the drill above, the only light down here. In the second shot the Burrowers
    /// wake: a pair of eyes opens in the crevice of a wall close by, then another, then hundreds in a wave rolling away
    /// into the dark. Every eye is seated in rock: it is placed where a ray from the corridor meets a wall, boulder or
    /// ceiling and sunk into it, never in mid-air. Posed from time alone.
    /// </summary>
    public sealed class UndergroundSet : ICutsceneSet
    {
        private const float Length = 38f;
        private const float StartZ = -4f;
        private const float FloorHeight = -1.6f;
        private const float WallX = 6.4f;
        private const float WallStep = 4.3f;
        private const float VaultX = 3.4f;
        private const float VaultHeight = 4.4f;
        private const float VaultTilt = 60f;
        private const float CeilingHeight = 5.8f;
        private const float CeilingStep = 3.6f;
        private const float FloorStep = 4.6f;
        private const float FloorSpan = 4.4f;
        private const int Clusters = 6;
        private const float CrystalLight = 8f;
        private const float CrystalRange = 9f;
        private const float FissureLight = 1.6f;
        private const float FissureRange = 6f;
        private const float CreviceLight = 3.5f;
        private const float WallRimLight = 1.6f;
        private const float WallRimRange = 5.5f;
        private static readonly Color RimViolet = new Color(0.65f, 0.4f, 1f);
        private const float CreviceRange = 4.5f;
        private const float TrembleDegrees = 3f;
        private const float TrembleShift = 0.03f;
        private const float DustRate = 14f;
        private const int DustParticles = 300;

        // The lead pairs: a crevice wall close beside the camera's path, then one further across; the wave starts from them.
        private static readonly Vector3 LeadWall = new Vector3(2.1f, FloorHeight, 11.2f);
        private static readonly Vector3 LeadFacing = new Vector3(-0.38f, 0f, -0.92f);
        private static readonly Vector3 SecondWall = new Vector3(-2.7f, FloorHeight, 15f);
        private static readonly Vector3 SecondFacing = new Vector3(0.45f, 0f, -0.89f);

        // Where the camera travels through the cave: every eye turns a little towards it, the lead pairs more.
        private static readonly Vector3 Watch = new Vector3(0f, 0.4f, 4f);
        private const float LeadToWatch = 0.6f;
        private const float WaveToWatch = 0.4f;
        private const float CreviceHeight = 2.0f;
        private const float SecondCreviceHeight = 2.9f;
        private const float LeadEyeSize = 2.3f;
        private const float SecondEyeSize = 1.9f;
        private const float FirstEyeShare = 0.12f;
        private const float SecondEyeShare = 0.28f;
        private const float WaveShare = 0.42f;
        private const float WaveSpeed = 9f;
        private const float LeadOpenSeconds = 1.1f;
        private const float WaveOpenSeconds = 0.35f;

        // README round 26: the unit eye has a 1 m slit; scale 0.15 per unit of eye size. A pair's pivots sit one slit apart.
        private const float ScalePerSize = 0.15f;
        private const float PairGap = 1f;
        // In a crevice the slot is narrow: the pair sits closer so both eyes fit between its buttresses.
        private const float CreviceGap = 0.8f;
        private const float MinPairSpacing = 0.55f;
        private const float RayReach = 14f;
        private const float SurfaceProbe = 0.8f;
                private const int SeatAttempts = 6;
        private static readonly Color DustColour = new Color(0.4f, 0.38f, 0.36f, 0.35f);
        private static readonly Color EmberColour = new Color(1f, 0.5f, 0.25f);

        // Every imported piece arrives turned half a turn about Y from the README frame (the cavern side faces -Z).
        private static readonly Matrix4x4 ImportTurn = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));

        private readonly List<Transform> _crystals = new List<Transform>();
        private readonly List<Quaternion> _crystalRest = new List<Quaternion>();
        private readonly List<Vector3> _crystalPositions = new List<Vector3>();
        private readonly List<MeshCollider> _rock = new List<MeshCollider>();
        private CutsceneContext _context;
        private VisualTheme _theme;
        private UndergroundArt _art;
        private bool _low;
        private EyeField _eyes;
        private ParticleSystem _dust;
        private CutsceneAction _action;
        private float _eyeShotSeconds;

        public VisualTheme Theme => _theme;

        /// <summary>The eye pairs, for probes.</summary>
        public EyeField Eyes => _eyes;

        public void Build(Transform root, CutsceneContext context)
        {
            _context = context;
            VisualTheme own = context.Cast != null ? context.Cast.UndergroundTheme : null;
            _theme = own != null ? own : context.FieldLevel.Theme;
            _art = context.Cast != null ? context.Cast.Underground : null;
            _low = context.ParticleScale < 1f;
            _eyeShotSeconds = context.Cutscene.CameraLengthOf(CutsceneAction.EyesOpen);
            if (_art == null || !_art.IsComplete)
            {
                Debug.LogWarning("The underground cutscene has no cave art; the cave stays dark.");
                return;
            }

            BuildCave(root);
            BuildCrystals(root);
            BuildEyes(root, context.Scaled(context.Cast.EyePairs));
            ForgetRock();
            _dust = ParticleKit.UseCell(ParticleKit.Create(root, "Dust", new Vector3(0f, 3.5f, Length * 0.4f), DustColour, 3f, 5f, 0.05f, 0.12f, 0.05f, 0.2f, 0.08f, context.Scaled(DustParticles), 6f), MarkerArt.ParticleCell.Mote);
            AddLight(root, new Vector3(0f, 0.4f, Length - 6f), EmberColour, 3f, 9f);
        }

        public void Enter(CutsceneAction action)
        {
            _action = action;
            // The cave brings no sun: its theme sets the dark fog and the faint ambient, the crystals do the rest.
            ThemeLighting.ApplyAtmosphere(_theme);
        }

        public void Exit()
        {
            if (_dust != null)
            {
                ParticleKit.SetRate(_dust, 0f);
            }
        }

        public void ClearView(IReadOnlyList<Vector3> eyes, IReadOnlyList<Vector3> targets)
        {
        }

        public bool TryGetAnchor(CameraAnchor anchor, out Vector3 origin, out Quaternion facing)
        {
            origin = Vector3.zero;
            facing = Quaternion.identity;
            return false;
        }

        public void Tick(float time, float progress)
        {
            bool trembling = _action == CutsceneAction.CrystalsTremble;
            float tremble = trembling ? CutsceneMath.Span(progress, 0.1f, 0.4f) : 0.35f;
            for (int i = 0; i < _crystals.Count; i++)
            {
                float a = Mathf.Sin(time * 31f + i * 1.3f) * TrembleDegrees * tremble;
                float b = Mathf.Sin(time * 23f + i * 2.9f) * TrembleDegrees * tremble;
                _crystals[i].rotation = _crystalRest[i] * Quaternion.Euler(a, 0f, b);
                _crystals[i].position = _crystalPositions[i] + Vector3.up * (Mathf.Sin(time * 41f + i) * TrembleShift * tremble);
            }

            if (_dust != null)
            {
                ParticleKit.SetRate(_dust, DustRate * (0.3f + tremble));
            }

            // The eyes only open in their own shot; before that every lid stays shut. They follow the camera.
            _eyes?.Pose(_action == CutsceneAction.EyesOpen ? time : -1f, _context.Camera.transform.position);
        }

        // Walls down both sides, a leaning vault above them, slabs and stalactites hanging, floors laid end to end.
        private void BuildCave(Transform root)
        {
            int k = 0;
            for (float z = StartZ; z < Length; z += WallStep, k++)
            {
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;
                    CavePiece wall = (k + s) % 3 == 0 ? CavePiece.WallB : CavePiece.WallA;
                    float jitter = (CutsceneMath.Hash(k * 2 + s, 151) - 0.5f) * 0.4f;
                    float yaw = -90f * side + (CutsceneMath.Hash(k * 2 + s, 153) - 0.5f) * 20f;
                    Piece(root, wall, new Vector3(side * WallX + jitter, FloorHeight - 0.2f, z + jitter), Quaternion.Euler(0f, yaw, 0f), 1.05f + CutsceneMath.Hash(k * 2 + s, 155) * 0.2f, true);
                    Piece(root, CavePiece.WallA, new Vector3(side * VaultX, VaultHeight, z + 1f + jitter), Quaternion.Euler(0f, -90f * side, 0f) * Quaternion.Euler(VaultTilt, 0f, 0f), 1.1f, true);
                    Piece(root, CavePiece.Boulder, new Vector3(side * (WallX - 1.3f), FloorHeight - 0.1f, z + 1.8f + jitter), Quaternion.Euler(0f, CutsceneMath.Hash(k * 2 + s, 157) * 360f, 0f), 0.8f + CutsceneMath.Hash(k * 2 + s, 159) * 0.5f, true);
                    // A low rim light along each wall, cyan and violet in turn, so the rock round the eyes has a form.
                    AddLight(root, new Vector3(side * (WallX - 2.6f), FloorHeight + 1.6f, z + 0.6f), k % 2 == 0 ? _theme.AccentColor : RimViolet, WallRimLight, WallRimRange);
                }
            }

            k = 0;
            for (float z = StartZ; z < Length; z += CeilingStep, k++)
            {
                Piece(root, CavePiece.Ceiling, new Vector3((CutsceneMath.Hash(k, 161) - 0.5f) * 1.6f, CeilingHeight, z + 1.8f), Quaternion.Euler(0f, CutsceneMath.Hash(k, 163) * 360f, 0f), 1.3f, true);
                if (k % 2 == 0)
                {
                    Piece(root, CavePiece.Stalactites, new Vector3((CutsceneMath.Hash(k, 165) - 0.5f) * 4.4f, 4.2f + CutsceneMath.Hash(k, 167) * 0.4f, z + CutsceneMath.Hash(k, 169) * 3f), Quaternion.Euler(0f, CutsceneMath.Hash(k, 171) * 360f, 0f), 0.9f + CutsceneMath.Hash(k, 173) * 0.5f, false);
                }
            }

            k = 0;
            for (float z = StartZ; z < Length; z += FloorStep, k++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    CavePiece floor = (k + x) % 3 == 1 ? CavePiece.FloorB : CavePiece.FloorA;
                    Piece(root, floor, new Vector3(x * FloorSpan, FloorHeight, z + 1.5f), Quaternion.Euler(0f, CutsceneMath.Hash(k * 3 + x + 1, 175) * 360f, 0f), 1.15f, true);
                    if (floor == CavePiece.FloorB)
                    {
                        // The fissure in the rubble glows cyan from below: the walls beside it catch its light.
                        AddLight(root, new Vector3(x * FloorSpan, FloorHeight, z + 1.5f) + Vector3.up * 0.6f, _theme.AccentColor, FissureLight, FissureRange);
                    }
                }

                if (k % 2 == 1)
                {
                    Piece(root, CavePiece.Vein, new Vector3((CutsceneMath.Hash(k, 177) - 0.5f) * 5f, FloorHeight + 0.05f, z + CutsceneMath.Hash(k, 179) * 3f), Quaternion.Euler(0f, CutsceneMath.Hash(k, 181) * 360f, 0f), 1.2f, false);
                }
            }

            // The two crevice walls of the lead eyes stand out into the corridor, turned towards the camera's path.
            Piece(root, CavePiece.WallB, LeadWall, Quaternion.LookRotation(LeadFacing), 1f, true);
            Piece(root, CavePiece.WallB, SecondWall, Quaternion.LookRotation(SecondFacing), 1.15f, true);
            // The veins inside the crevices light the rock round the eyes, so they read as sitting in it.
            AddLight(root, LeadWall + Vector3.up * CreviceHeight + LeadFacing.normalized * 1.2f, _theme.AccentColor, CreviceLight, CreviceRange);
            AddLight(root, SecondWall + Vector3.up * SecondCreviceHeight + SecondFacing.normalized * 1.2f, _theme.AccentColor, CreviceLight, CreviceRange);
        }

        private void BuildCrystals(Transform root)
        {
            for (int i = 0; i < Clusters; i++)
            {
                float z = 6f + i * (Length - 12f) / Clusters + CutsceneMath.Hash(i, 121) * 2f;
                float side = i % 2 == 0 ? -1f : 1f;
                var position = new Vector3(side * (3.9f + CutsceneMath.Hash(i, 123) * 0.8f), FloorHeight + 0.05f, z);
                Transform crystal = Piece(root, CavePiece.CrystalCluster, position, Quaternion.Euler(0f, CutsceneMath.Hash(i, 125) * 360f, 0f), 1.2f + CutsceneMath.Hash(i, 127) * 0.8f, false);
                _crystals.Add(crystal);
                _crystalRest.Add(crystal.rotation);
                _crystalPositions.Add(position);
                AddLight(root, position + Vector3.up * 1.2f, _theme.AccentColor, CrystalLight, CrystalRange);
                GlowDecal.PlacePool(crystal, _theme.AccentColor * 0.25f, 1.6f);
            }
        }

        // The lead pairs sit in the crevices of their walls; the rest are cast onto the rock round the corridor and open in
        // a wave from the first, the further the later.
        private void BuildEyes(Transform root, int pairs)
        {
            // The rock colliders were made this frame; the queries need them where their pieces stand.
            Physics.SyncTransforms();
            float length = _eyeShotSeconds > 0f ? _eyeShotSeconds : 9f;
            var seated = new List<EyeField.Pair>(pairs);
            Vector3 first = Vector3.zero;
            if (TrySeatInCrevice(LeadWall, LeadFacing, CreviceHeight, LeadEyeSize, length * FirstEyeShare, out EyeField.Pair lead))
            {
                seated.Add(lead);
                first = (lead.Left + lead.Right) * 0.5f;
            }

            if (TrySeatInCrevice(SecondWall, SecondFacing, SecondCreviceHeight, SecondEyeSize, length * SecondEyeShare, out EyeField.Pair second))
            {
                seated.Add(second);
            }

            for (int i = 0; seated.Count < pairs && i < pairs * SeatAttempts; i++)
            {
                float angle = Mathf.Lerp(-0.15f, 1.15f, CutsceneMath.Hash(i, 131)) * Mathf.PI;
                float z = 4f + CutsceneMath.Hash(i, 135) * (Length - 6f);
                var origin = new Vector3(0f, 1.2f, z);
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                float size = 1.5f + CutsceneMath.Hash(i, 81) * 1.4f;
                if (!TrySeat(origin, direction, size, 0f, WaveOpenSeconds, i, WaveToWatch, PairGap, out EyeField.Pair pair) || Crowded(seated, pair))
                {
                    continue;
                }

                Vector3 centre = (pair.Left + pair.Right) * 0.5f;
                pair.OpensAt = length * WaveShare + Vector3.Distance(centre, first) / WaveSpeed + CutsceneMath.Hash(i, 137) * 0.4f;
                seated.Add(pair);
            }

            _eyes = new EyeField(root, _art, seated, _low);
        }

        private bool TrySeatInCrevice(Vector3 wall, Vector3 facing, float height, float size, float opensAt, out EyeField.Pair pair)
        {
            Vector3 forward = facing.normalized;
            Vector3 origin = wall + Vector3.up * height + forward * 2.5f;
            if (!TrySeat(origin, -forward, size, opensAt, LeadOpenSeconds, 0, LeadToWatch, CreviceGap, out pair))
            {
                return false;
            }

            // In the slot both eyes sit in one plane at the depth of the nearer one, so neither hides behind a buttress.
            Vector3 centre = (pair.Left + pair.Right) * 0.5f;
            Vector3 half = (pair.Right - pair.Left) * 0.5f;
            float nearer = Mathf.Max(Vector3.Dot(pair.Left - centre, pair.Facing), Vector3.Dot(pair.Right - centre, pair.Facing));
            Vector3 plane = centre + pair.Facing * nearer;
            Vector3 across = Vector3.ProjectOnPlane(half, pair.Facing);
            pair.Left = plane - across;
            pair.Right = plane + across;
            return true;
        }

        // A pair on the rock the ray meets: each eye cast onto the surface on its own and sunk into it by the seat depth.
        private bool TrySeat(Vector3 origin, Vector3 direction, float size, float opensAt, float openSeconds, int variant, float toWatch, float pairGap, out EyeField.Pair pair)
        {
            pair = default;
            if (!CastRock(new Ray(origin, direction), RayReach, out RaycastHit hit) || Vector3.Dot(hit.normal, -direction) < 0.3f || hit.normal.y > 0.7f)
            {
                return false;
            }

            Vector3 facing = Vector3.Slerp(hit.normal, (Watch - hit.point).normalized, toWatch).normalized;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, facing);
            up = up.sqrMagnitude > 0.01f ? up.normalized : Vector3.ProjectOnPlane(Vector3.forward, facing).normalized;
            Vector3 right = Vector3.Cross(up, facing).normalized;
            float scale = ScalePerSize * size;
            float gap = pairGap * scale * 0.5f;
            if (!SeatEye(hit.point - right * gap, facing, scale, out Vector3 left) || !SeatEye(hit.point + right * gap, facing, scale, out Vector3 rightEye))
            {
                return false;
            }

            pair = new EyeField.Pair
            {
                Left = left,
                Right = rightEye,
                Facing = facing,
                Up = up,
                Scale = scale,
                OpensAt = opensAt,
                OpenSeconds = openSeconds,
                Variant = variant % Mathf.Max(1, _art.Eyes.Length),
            };
            return true;
        }

        private bool SeatEye(Vector3 near, Vector3 facing, float scale, out Vector3 pivot)
        {
            pivot = near;
            if (!CastRock(new Ray(near + facing * SurfaceProbe, -facing), SurfaceProbe * 2f, out RaycastHit hit))
            {
                return false;
            }

            pivot = hit.point - facing * (_art.SeatDepth * scale);
            return true;
        }

        private bool CastRock(Ray ray, float reach, out RaycastHit nearest)
        {
            nearest = default;
            bool found = false;
            foreach (MeshCollider rock in _rock)
            {
                if (rock.Raycast(ray, out RaycastHit hit, reach) && (!found || hit.distance < nearest.distance))
                {
                    nearest = hit;
                    found = true;
                }
            }

            return found;
        }

        private static bool Crowded(List<EyeField.Pair> seated, EyeField.Pair pair)
        {
            Vector3 centre = (pair.Left + pair.Right) * 0.5f;
            foreach (EyeField.Pair other in seated)
            {
                if (Vector3.Distance((other.Left + other.Right) * 0.5f, centre) < MinPairSpacing + (other.Scale + pair.Scale) * PairGap)
                {
                    return true;
                }
            }

            return false;
        }

        // The colliders only serve the seating of the eyes; nothing in a cutscene collides.
        private void ForgetRock()
        {
            foreach (MeshCollider rock in _rock)
            {
                Object.Destroy(rock);
            }

            _rock.Clear();
        }

        private Transform Piece(Transform root, CavePiece kind, Vector3 position, Quaternion rotation, float scale, bool rock)
        {
            EnvironmentMesh piece = _art.Piece(kind);
            var go = new GameObject($"{kind}");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * scale;

            Matrix4x4 node = ImportTurn * piece.Node;
            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(go.transform, false);
            mesh.transform.localPosition = node.GetPosition();
            mesh.transform.localRotation = node.rotation;
            mesh.transform.localScale = node.lossyScale;
            mesh.AddComponent<MeshFilter>().sharedMesh = piece.Mesh;
            var renderer = mesh.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = piece.MaterialFor(_low);
            renderer.shadowCastingMode = piece.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (rock)
            {
                MeshCollider collider = mesh.AddComponent<MeshCollider>();
                collider.sharedMesh = piece.Mesh;
                _rock.Add(collider);
            }

            return go.transform;
        }

        private static void AddLight(Transform root, Vector3 position, Color colour, float intensity, float range)
        {
            var glow = new GameObject("Light");
            glow.transform.SetParent(root, false);
            glow.transform.position = position;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }
    }
}
