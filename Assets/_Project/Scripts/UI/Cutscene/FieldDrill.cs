using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The drill on its deposit: the rig unfolds out of the ground, then the drill string spins and sinks through the
    /// platform deep into the ground and stays down, turning. When the bit bites, dust bursts out at its foot and the
    /// ground cracks open round it with a warm glow from below (the Blender bore-hole cracks and crack strips spreading
    /// out); a deep rumble loop follows the drilling. Posed from progress and time alone; the contact burst is part of
    /// the shot's particle clock, so a seek shows it too.
    /// </summary>
    public sealed class FieldDrill
    {
        private const float MaxDepth = 2.4f;
        // Round 39: the deck's underside is found among the rig's vertices this close (metres) to its root; the footing sits
        // this far in from the deck's edge, tucks this far up under it and reaches this far into the ground.
        private const float DeckBand = 0.15f;
        private const float FootingInset = 0.03f;
        private const float FootingTuck = 0.04f;
        private const float FootingSink = 0.2f;
        private const int FootingSegments = 6;
        private const float FootingSmoothness = 0.35f;
        private const float FootingMetallic = 0.5f;
        private static readonly Color FootingColour = new Color(0.13f, 0.14f, 0.17f);
        // The glow decals lie on the slope's plane fitted over this radius, lifted this far off it.
        private const float SlopeFitRadius = 1.6f;
        private const float DecalLift = 0.06f;
        private const float StripSlopeFitRadius = 0.5f;
        private const float SpinDegreesPerSecond = 540f;
        private const float SinkSpinDegreesPerSecond = 300f;
        private const float BitTop = 0.2f;
        private const float DustRate = 22f;
        private const float RumbleShare = 0.6f;
        private const int Strips = 6;
        private const float StripLength = 2.4f;
        private const float StripWidth = 0.42f;
        private const float BoreRadius = 2.6f;
        private const float CrackGlow = 1.8f;
        private const float DepositGlow = 0.5f;
        private const int ContactDust = 28;
        private const string StringBone = "String";
        private static readonly Color DustColour = new Color(1.5f, 1.42f, 1.42f, 0.85f);

        // The drill's sink starts a fifth into the shot and the bit reaches the ground at this share of the sink (its tip
        // goes from 0.44 m above the deck to the ground at about a quarter of the Sink clip).
        public const float ContactShareOfSink = 0.27f;

        private readonly Transform _rig;
        private readonly Transform _bit;
        private readonly float _bitLength;
        private readonly ParticleSystem _dust;
        private readonly ParticleSystem _contact;
        private readonly Transform[] _strips;
        private readonly Transform _bore;
        private readonly Material _crackMaterial;
        private readonly Material _boreMaterial;
        private readonly Color _crackColour;
        private readonly GameObject _deposit;
        private readonly AudioSource _rumble;
        private readonly CutsceneAudio _audio;
        private readonly ArtModel _art;
        private readonly Transform _string;
        private readonly Transform _groundPlane;
        private readonly GameObject _footing;

        public FieldDrill(Transform parent, FieldLayout layout, CutsceneContext context, float contactSeconds)
        {
            VisualTheme theme = layout.Level.Theme;
            _audio = context.Audio;
            bool low = context.ParticleScale < 1f;
            Root = new GameObject("Drill").transform;
            Root.SetParent(parent, false);
            Root.SetPositionAndRotation(layout.Drill, layout.DrillFacing);
            // At the colony rig it is the rig the level shows afterwards, so it takes the rig's size too.
            ColonyRigDefinition colonyRig = theme.Environment != null ? theme.Environment.ColonyRig : null;
            if (layout.HasColonyRig && colonyRig != null)
            {
                Root.localScale = Vector3.one * colonyRig.DrillScale;
            }
            _crackColour = Color.Lerp(theme.AccentColor, new Color(1f, 0.55f, 0.2f), 0.5f);

            GameObject custom = context.Cast != null ? context.Cast.Drill : null;
            DrillModelFactory.Parts parts = DrillModelFactory.Create();
            Material[] materials = ModelPalette.Create(theme.BaseColor);
            _rig = new GameObject("Rig").transform;
            _rig.SetParent(Root, false);
            ModelArtDefinition art = context.Cast != null ? context.Cast.DrillArt : null;
            if (art != null)
            {
                // The imported derrick has its own drill string: its Sink clip drives it down; afterwards it stays down and turns.
                _art = ArtModel.CreateSampled(_rig, art, ownMaterial: false);
                _string = _art.Find(StringBone);
            }
            else if (custom != null)
            {
                Object.Instantiate(custom, _rig);
            }
            else
            {
                _rig.gameObject.AddComponent<MeshFilter>().sharedMesh = parts.Rig;
                _rig.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            }

            _bit = new GameObject("Bit").transform;
            _bit.SetParent(Root, false);
            _bit.gameObject.AddComponent<MeshFilter>().sharedMesh = parts.Bit;
            _bit.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            _bitLength = parts.BitLength;

            // Round 39: the rig stands where the border climbs; its deck floated over the downhill side with a hard edge
            // ("something weird at the bottom"), and its flat glow decals cut through the slope as bright lines. A footing
            // now runs from under the deck down into the ground all round, and the decals lie on the slope's own plane.
            _groundPlane = new GameObject("GroundPlane").transform;
            _groundPlane.SetParent(Root, false);
            _groundPlane.rotation = Quaternion.FromToRotation(Vector3.up, layout.SurfaceNormal(Root.position, SlopeFitRadius)) * Root.rotation;
            _footing = BuildFooting(parent, layout);

            _deposit = GlowDecal.PlacePool(_groundPlane, _crackColour * DepositGlow, 2.4f, DecalLift);
            _boreMaterial = Additive(CutsceneSprite.DrillCracks, _crackColour * CrackGlow);
            _crackMaterial = Additive(CutsceneSprite.CrackDrillA, _crackColour * CrackGlow);
            Material stripB = Additive(CutsceneSprite.CrackDrillB, _crackColour * CrackGlow);
            _bore = Flat("Bore", _groundPlane, _boreMaterial, Vector3.up * (DecalLift + 0.015f), 0f);
            // One cell of the 2 x 2 bore-hole sheet (README: tiling 0.5, an offset per cell).
            _boreMaterial.mainTextureScale = Vector2.one * 0.5f;
            _boreMaterial.mainTextureOffset = new Vector2(0.5f, 0f);
            _boreMaterial.SetTextureScale("_EmissionMap", Vector2.one * 0.5f);
            _boreMaterial.SetTextureOffset("_EmissionMap", new Vector2(0.5f, 0f));
            _strips = new Transform[Strips];
            for (int i = 0; i < Strips; i++)
            {
                float yaw = i * (360f / Strips) + CutsceneMath.Hash(i, 3) * 25f;
                var pivot = new GameObject($"Crack{i}").transform;
                pivot.SetParent(_groundPlane, false);
                pivot.localPosition = Vector3.up * (DecalLift + 0.01f);
                pivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
                Transform strip = Flat("Strip", pivot, i % 2 == 0 ? _crackMaterial : stripB, new Vector3(0f, 0f, StripLength * 0.5f + 0.7f), 0f);
                strip.localScale = new Vector3(StripWidth, StripLength, 1f);
                FollowGround(pivot, strip, layout);
                _strips[i] = pivot;
            }

            ImpactVfxDefinition burst = context.FieldLevel.SpawnOrigins != null ? context.FieldLevel.SpawnOrigins.BurstVfx : null;
            VfxSheet dust = burst != null ? (burst.DustSheet != null ? burst.DustSheet : burst.SmokeSheet) : null;
            _dust = CutsceneBursts.Stream(Root, "Dust", Vector3.up * 0.3f, dust, low, context.Scaled(160), new Vector2(0.8f, 1.6f), new Vector2(0.6f, 1.8f), -0.04f, DustColour, 1.3f);
            _contact = CutsceneBursts.Burst(Root, "ContactDust", Vector3.up * 0.2f, dust, low, context.Scaled(ContactDust), contactSeconds, new Vector2(1.4f, 2.6f), new Vector2(1.5f, 3.5f), -0.05f, DustColour, 1f, VfxParticles.Frames.OverLife);
            _rumble = context.Audio.CreateLoop("DrillRumble", CutsceneSound.DrillRun);
        }

        public Transform Root { get; }

        /// <summary>Where the haulers pick up: the ore heap beside the drill's platform, on the ground.</summary>
        public Vector3 Pickup => Root.position + Root.rotation * new Vector3(-1.9f, 0f, 0.6f);

        /// <summary>The drilling shot begins: the contact burst starts its clock; in any other shot it rests.</summary>
        public void Enter(bool drilling)
        {
            _contact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (drilling)
            {
                _contact.Play(true);
            }
        }

        /// <summary>
        /// <paramref name="build"/>: the rig out of the ground (0..1). <paramref name="depth"/>: how far the string has
        /// sunk (0..1). <paramref name="crack"/>: how far the ground has cracked open (0..1). <paramref name="sunkAt"/>: the
        /// moment (on the same clock as <paramref name="time"/>) the string reached the bottom, so its turn runs on from there.
        /// </summary>
        public void Pose(float build, float depth, float crack, float time, bool loud, float sunkAt)
        {
            Root.gameObject.SetActive(build > 0f);
            if (_footing != null)
            {
                _footing.SetActive(build > 0f);
            }

            if (build <= 0f)
            {
                _audio.SetLoopVolume(_rumble, 0f);
                return;
            }

            float grow = CutsceneMath.OutBack(build);
            _rig.localScale = new Vector3(1f, Mathf.Max(0.01f, grow), 1f);
            _bit.gameObject.SetActive(build >= 1f && _art == null);
            if (_art != null)
            {
                PoseArt(depth, time, sunkAt);
            }

            _bit.localPosition = Vector3.up * (_bitLength + BitTop - depth * MaxDepth);
            _bit.localRotation = Quaternion.Euler(0f, time * SpinDegreesPerSecond * Mathf.Min(1f, depth * 4f), 0f);

            float bore = CutsceneMath.Smooth(CutsceneMath.Span(crack, 0f, 0.6f));
            _bore.gameObject.SetActive(bore > 0f);
            _bore.localScale = new Vector3(Mathf.Max(0.001f, BoreRadius * 2f * bore), Mathf.Max(0.001f, BoreRadius * 2f * bore), 1f);
            for (int i = 0; i < _strips.Length; i++)
            {
                float open = CutsceneMath.Span(crack, 0.1f + i * 0.07f, 0.6f + i * 0.07f);
                _strips[i].gameObject.SetActive(open > 0f);
                _strips[i].localScale = new Vector3(Mathf.Max(0.001f, open), 1f, Mathf.Max(0.001f, open));
            }

            float flicker = 0.8f + 0.2f * Mathf.Sin(time * 7f);
            GlowDecal.SetColour(_crackMaterial, _crackColour * (CrackGlow * flicker));
            GlowDecal.SetColour(_boreMaterial, _crackColour * (CrackGlow * flicker));

            _deposit.SetActive(crack > 0f);
            bool drilling = depth > ContactShareOfSink;
            ParticleKit.SetRate(_dust, drilling ? DustRate * (loud ? 1f : 0.35f) : 0f);
            _audio.SetLoopVolume(_rumble, drilling ? RumbleShare * (loud ? 1f : 0.3f) : 0f);
        }

        // While it sinks the Sink clip drives the string (with an extra turn on top); once down, the clip's last frame
        // holds it down and the string turns on its own bone. The Spin clip would lift it back to the top.
        private void PoseArt(float depth, float time, float sunkAt)
        {
            float sinkLength = _art.LengthOf(ModelClipRole.Deploy);
            _art.Sample(ModelClipRole.Deploy, Mathf.Clamp01(depth) * sinkLength, loop: false);
            if (_string == null)
            {
                return;
            }

            // Round 48: once down it turns faster from where the sinking turn left it (the angle jumped by ~120 deg before).
            float spin = depth >= 1f
                ? sunkAt * SinkSpinDegreesPerSecond + (time - sunkAt) * SpinDegreesPerSecond
                : time * SinkSpinDegreesPerSecond * Mathf.Min(1f, depth * 4f);
            _string.localRotation *= Quaternion.AngleAxis(spin, Vector3.up);
        }

        public void Silence() => _audio.SetLoopVolume(_rumble, 0f);

        /// <summary>The footing under the deck (round 39), or null without a rig mesh; for probes and tests.</summary>
        public GameObject Footing => _footing;

        // A plain steel skirt round the deck's underside, from just under the deck down to a little below the drawn ground at
        // every point of its edge, so the deck reads as standing on the slope instead of hovering over it.
        private GameObject BuildFooting(Transform parent, FieldLayout layout)
        {
            List<Vector3> points = RigPoints();
            Quaternion yaw = Root.rotation;
            Quaternion unyaw = Quaternion.Inverse(yaw);
            Vector3 origin = Root.position;
            float under = float.PositiveInfinity;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (Vector3 point in points)
            {
                float height = point.y - origin.y;
                if (height < -DeckBand || height > DeckBand)
                {
                    continue;
                }

                Vector3 local = unyaw * (point - origin);
                min = Vector2.Min(min, new Vector2(local.x, local.z));
                max = Vector2.Max(max, new Vector2(local.x, local.z));
                under = Mathf.Min(under, point.y);
            }

            if (float.IsInfinity(under))
            {
                return null;
            }

            min += Vector2.one * FootingInset;
            max -= Vector2.one * FootingInset;
            var corners = new[] { new Vector2(min.x, min.y), new Vector2(max.x, min.y), new Vector2(max.x, max.y), new Vector2(min.x, max.y) };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Vector3 centre = origin + yaw * new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f);
            for (int side = 0; side < corners.Length; side++)
            {
                Vector2 a = corners[side];
                Vector2 b = corners[(side + 1) % corners.Length];
                for (int i = 0; i < FootingSegments; i++)
                {
                    Vector3 from = origin + yaw * ToXz(Vector2.Lerp(a, b, i / (float)FootingSegments));
                    Vector3 to = origin + yaw * ToXz(Vector2.Lerp(a, b, (i + 1) / (float)FootingSegments));
                    AddPanel(vertices, triangles, from, to, under + FootingTuck, layout, centre);
                }
            }

            var mesh = new Mesh { name = "DrillFooting" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("DrillFooting");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = SurfaceMaterials.Create(FootingColour, 0f, FootingSmoothness, FootingMetallic);
            return go;
        }

        // One panel of the footing between two points of the deck's edge, facing out from the deck's middle.
        private static void AddPanel(List<Vector3> vertices, List<int> triangles, Vector3 from, Vector3 to, float top, FieldLayout layout, Vector3 centre)
        {
            float fromBottom = Mathf.Min(top, layout.SurfaceAt(from.x, from.z) - FootingSink);
            float toBottom = Mathf.Min(top, layout.SurfaceAt(to.x, to.z) - FootingSink);
            int start = vertices.Count;
            vertices.Add(new Vector3(from.x, top, from.z));
            vertices.Add(new Vector3(to.x, top, to.z));
            vertices.Add(new Vector3(to.x, toBottom, to.z));
            vertices.Add(new Vector3(from.x, fromBottom, from.z));
            Vector3 outward = (from + to) * 0.5f - centre;
            outward.y = 0f;
            Vector3 face = Vector3.Cross(vertices[start + 1] - vertices[start], vertices[start + 2] - vertices[start]);
            bool flip = Vector3.Dot(face, outward) < 0f;
            triangles.AddRange(flip ? new[] { start, start + 2, start + 1, start, start + 3, start + 2 } : new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        private static Vector3 ToXz(Vector2 point) => new Vector3(point.x, 0f, point.y);

        // A crack runs a few metres out over ground that curves away from the slope's plane: its pivot tips so the strip
        // runs from the bore to the drawn ground under its far end, rolled to the ground's slope halfway along.
        private void FollowGround(Transform pivot, Transform strip, FieldLayout layout)
        {
            Vector3 a = strip.TransformPoint(new Vector3(0f, 0.5f, 0f));
            Vector3 b = strip.TransformPoint(new Vector3(0f, -0.5f, 0f));
            Vector3 far = (a - Root.position).sqrMagnitude > (b - Root.position).sqrMagnitude ? a : b;
            Vector3 start = pivot.position;
            Vector3 end = new Vector3(far.x, layout.SurfaceAt(far.x, far.z), far.z);
            Vector3 middle = (start + end) * 0.5f;
            Vector3 normal = layout.SurfaceNormal(middle, StripSlopeFitRadius);
            Vector3 lift = normal * DecalLift;
            pivot.rotation = Quaternion.LookRotation(end + lift - start, normal);
        }

        // The rig's vertices in the world as built (full size), from its imported mesh or the procedural one.
        private List<Vector3> RigPoints()
        {
            var points = new List<Vector3>();
            var baked = new Mesh();
            foreach (Renderer renderer in _rig.GetComponentsInChildren<Renderer>(true))
            {
                Vector3[] vertices;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    skinned.BakeMesh(baked, true);
                    vertices = baked.vertices;
                }
                else if (renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    vertices = filter.sharedMesh.vertices;
                }
                else
                {
                    continue;
                }

                Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
                foreach (Vector3 vertex in vertices)
                {
                    points.Add(toWorld.MultiplyPoint3x4(vertex));
                }
            }

            Object.DestroyImmediate(baked);
            return points;
        }

        private static Material Additive(CutsceneSprite sprite, Color colour)
        {
            Texture2D texture = CutsceneFxSet.Get(sprite);
            return texture != null ? GlowDecal.Additive(texture, colour) : new Material(GlowDecal.RadialMaterial(colour));
        }

        private static Transform Flat(string name, Transform parent, Material material, Vector3 localPosition, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }
    }
}
