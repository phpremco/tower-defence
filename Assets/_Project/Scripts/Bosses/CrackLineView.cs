using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Bosses
{
    /// <summary>
    /// A line a boss lays on the ground along its planned route, opening ahead of it so the player sees where it is going.
    /// A burrow crack is a real fissure (round 29 ground mark): heaved stone lips that rise as it tears open, a dark gap
    /// with a molten core, rock chunks pushed up along its edges, dust and sparks kicked up at its open end, a warm halo
    /// over it, and a slow cooling once the boss is out. Lines of light (heat, frost, a shard's route, the drain thread)
    /// keep the painted glow blade. Meshes are built once; opening only moves a threshold and the drawn triangle count.
    /// </summary>
    public sealed class CrackLineView : MonoBehaviour
    {
        private const float Lift = 0.05f;
        private const float GritRate = 36f;
        private const float DustRate = 26f;
        private const float SparkRate = 30f;
        // The blade texture fades out at both of its ends (v 0 and 1); the seam keeps clear of them.
        private const float AlongMin = 0.2f;
        private const float AlongSpan = 0.6f;
        private const int TrianglesPerSegment = 24;
        // A mark stays longer than a line of light: the glow cools first, then the scar goes.
        private const float MarkFadeFactor = 2.5f;
        private const float MarkHaloShare = 0.06f;
        private const float OpenRamp = 2.2f;
        private const float ChunkRiseSeconds = 0.35f;
        private const float ChunkSink = 0.3f;
        private const float ChunkMinSize = 0.2f;
        private const float ChunkMaxSize = 0.38f;
        private const float ChunkSide = 0.22f;
        // How deep a heaved chunk stays in the ground, so it juts out of the lip instead of lying on it.
        private const float ChunkBedded = 0.08f;
        // README round 30 crack slabs: about this many metres apart along the lips, alternating sides, a few per crack.
        private const float SlabSpacingMin = 1.8f;
        private const float SlabSpacingMax = 3.4f;
        private const int SlabsMin = 3;
        private const float SlabEndMargin = 1f;
        private const float SlabScaleMin = 0.8f;
        private const float SlabScaleMax = 1.3f;
        // The export is metre-sized; at the game camera on a 3 m fissure the slabs need this much to read as slabs.
        private const float SlabWorldScale = 1.6f;
        private const float SlabSinkMin = 0.03f;
        private const float SlabSinkMax = 0.08f;
        // The hinge sits on the lip, this share of the mark's width out from the line, so the raised edge leans over the gap.
        private const float SlabHingeShare = 0.32f;
        private const float SlabHingeJitter = 0.12f;
        private const float SlabReach = 0.55f;
        // More slabs on the far lip, whose raised glowing edges face the camera.
        private const float SlabFarLipShare = 0.65f;
        // A slab on the near lip turns along the crack until mostly its edge, not its flat top, faces the camera.
        private const float SlabNearAlong = 0.75f;
        private const float SlabYawJitter = 15f;
        private const float SlabRiseSeconds = 0.4f;
        private const float SlabRiseDepth = 0.25f;
        private const float SlabFlatTilt = 30f;
        private const float SlabOvershoot = 6f;
        private const float SlabSettleDepth = 0.18f;
        private const float SlabSettleTilt = 14f;
        private const float SlabSettleRate = 1.4f;
        private static readonly Color Dust =new Color(0.36f, 0.3f, 0.26f, 0.55f);
        private static readonly Color Sparks = new Color(1f, 0.55f, 0.18f, 1f);
        private static readonly Color Rock = new Color(0.3f, 0.26f, 0.24f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static Material _rock;

        private CrackStyle _style;
        private readonly List<float> _along = new List<float>();
        private readonly List<Material> _materials = new List<Material>(3);
        private readonly List<Mesh> _meshes = new List<Mesh>(2);
        private readonly List<int[]> _triangles = new List<int[]>(2);
        private readonly List<Chunk> _chunks = new List<Chunk>();
        private Vector3[] _points;
        private float _length;
        private float _open = -1f;
        private float _fade = -1f;
        private Transform _head;
        private ParticleSystem _grit;
        private ParticleSystem _dust;
        private ParticleSystem _sparks;
        private Material _mark;
        private Mesh _markMesh;
        private Color _markGlow;
        private readonly List<SlabPlace> _slabs = new List<SlabPlace>();
        private CrackSlabPool _slabPool;
        private System.Func<Vector3, bool> _blocked;
        private MaterialPropertyBlock _slabBlock;
        private float _slabGlow = -1f;
        private float _fadeSeconds = -1f;

        private sealed class SlabPlace
        {
            public CrackSlabPool.Slab Slab;
            public Vector3 Rest;
            public Quaternion Yaw;
            public float Along;
            public float Age = -1f;
        }

        private sealed class Chunk
        {
            public Transform Transform;
            public Vector3 Rest;
            public float Along;
            public float Age = -1f;
        }

        /// <summary>Lays a crack along world points (on the ground); nothing shows until it opens.</summary>
        public static CrackLineView Create(Transform parent, IReadOnlyList<Vector3> points) => Create(parent, points, CrackStyle.Burrow);

        /// <summary>Lays a line in a style of its own (a thin heat line, a creeping frost) along world points.</summary>
        public static CrackLineView Create(Transform parent, IReadOnlyList<Vector3> points, CrackStyle style) => Create(parent, points, style, null);

        /// <summary>A line whose slabs (when its style has them) keep off the spots <paramref name="blocked"/> names.</summary>
        public static CrackLineView Create(Transform parent, IReadOnlyList<Vector3> points, CrackStyle style, System.Func<Vector3, bool> blocked)
        {
            var go = new GameObject("CrackLine");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CrackLineView>();
            view._style = style;
            view._blocked = blocked;
            view.Build(points);
            return view;
        }

        public float Length => _length;

        /// <summary>True when the line is drawn as a ground mark (the art exists).</summary>
        public bool IsMark => _mark != null;

        /// <summary>Opens the crack up to a distance along it.</summary>
        public void OpenTo(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, _length);
            if (Mathf.Approximately(distance, _open))
            {
                return;
            }

            _open = distance;
            int segments = 0;
            while (segments < _points.Length - 1 && _along[segments + 1] <= distance + 0.001f)
            {
                segments++;
            }

            for (int i = 0; i < _meshes.Count; i++)
            {
                _meshes[i].SetTriangles(_triangles[i], 0, segments * TrianglesPerSegment, 0, false);
            }

            if (_mark != null)
            {
                // The ramp runs behind the open end, so the very end is the fissure just starting to part.
                _mark.SetFloat(GroundMark.OpenDistance, distance + (distance >= _length - 0.01f ? OpenRamp : 0f));
            }

            _head.position = PointAt(distance) + Vector3.up * Lift;
            bool opening = distance < _length;
            ParticleKit.SetRate(_grit, opening ? GritRate * _style.GritShare : 0f);
            if (_dust != null)
            {
                ParticleKit.SetRate(_dust, opening ? DustRate : 0f);
                ParticleKit.SetRate(_sparks, opening ? SparkRate : 0f);
            }
        }

        /// <summary>Lets the crack cool down and disappear.</summary>
        public void Fade() => Fade(-1f);

        /// <summary>
        /// Cools it away within a time of its own (another crack is about to open over it): never slower than it already
        /// goes, and how far it has cooled carries over.
        /// </summary>
        public void Fade(float seconds)
        {
            float current = FadeSeconds;
            if (seconds > 0f && seconds < current)
            {
                if (_fade > 0f)
                {
                    _fade *= seconds / current;
                }

                _fadeSeconds = seconds;
            }

            if (_fade < 0f)
            {
                _fade = 0f;
                ParticleKit.SetRate(_grit, 0f);
                if (_dust != null)
                {
                    ParticleKit.SetRate(_dust, 0f);
                    ParticleKit.SetRate(_sparks, 0f);
                }
            }
        }

        private void Update()
        {
            UpdateChunks();
            UpdateSlabs();
            if (_fade < 0f)
            {
                return;
            }

            _fade += Time.deltaTime;
            float left = 1f - _fade / FadeSeconds;
            if (left <= 0f)
            {
                // The slabs go back to the pool before the crack is destroyed (children cannot leave a dying parent).
                ReleaseSlabs();
                Destroy(gameObject);
                return;
            }

            int layer = 0;
            if (_mark != null)
            {
                // The molten core cools long before the scar goes.
                float glow = Mathf.Clamp01(left * 2f - 1f);
                _mark.SetFloat(GroundMark.GlowFade, glow * glow);
                _mark.SetFloat(GroundMark.Fade, Mathf.Clamp01(left * 2.5f));
                layer = 1;
            }
            else
            {
                _materials[layer++].SetColor(MarkerArt.Tint, _style.Seam * left);
            }

            _materials[layer].SetColor(MarkerArt.Tint, _style.Halo * left * (_mark != null ? MarkHaloShare * left : 1f));
            _head.localScale = Vector3.one * left;
        }

        private void UpdateChunks()
        {
            float dt = Time.deltaTime;
            float sink = _fade >= 0f ? Mathf.Clamp01(_fade / FadeSeconds * 1.6f - 0.6f) : 0f;
            for (int i = 0; i < _chunks.Count; i++)
            {
                Chunk chunk = _chunks[i];
                if (chunk.Age < 0f)
                {
                    if (_open < chunk.Along)
                    {
                        continue;
                    }

                    chunk.Age = 0f;
                    chunk.Transform.gameObject.SetActive(true);
                }

                chunk.Age += dt;
                float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(chunk.Age / ChunkRiseSeconds));
                chunk.Transform.position = chunk.Rest + Vector3.down * (ChunkSink * (1f - up + sink));
            }
        }

        private float FadeSeconds => _fadeSeconds > 0f ? _fadeSeconds : _style.FadeSeconds * (_mark != null ? MarkFadeFactor : 1f);

        // The slabs tip up off their hinges as the crack opens past them (with a little overshoot); as it cools they
        // settle back and sink while their glow goes with the strip's.
        private void UpdateSlabs()
        {
            if (_slabs.Count == 0)
            {
                return;
            }

            float settle = _fade >= 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_fade / FadeSeconds * SlabSettleRate)) : 0f;
            for (int i = 0; i < _slabs.Count; i++)
            {
                SlabPlace place = _slabs[i];
                if (place.Age < 0f)
                {
                    if (_open < place.Along)
                    {
                        continue;
                    }

                    place.Age = 0f;
                    place.Slab.Holder.gameObject.SetActive(true);
                }

                place.Age += Time.deltaTime;
                float t = Mathf.Clamp01(place.Age / SlabRiseSeconds);
                float up = Mathf.SmoothStep(0f, 1f, t);
                float tilt = SlabFlatTilt * (1f - up) - SlabOvershoot * Mathf.Sin(t * Mathf.PI) + SlabSettleTilt * settle;
                Vector3 at = place.Rest + Vector3.down * (SlabRiseDepth * (1f - up) + SlabSettleDepth * settle);
                place.Slab.Holder.SetPositionAndRotation(at, place.Yaw * Quaternion.Euler(tilt, 0f, 0f));
            }

            // The glow cools with the strip's molten core (GroundMark.GlowFade in Update).
            float glow = _fade >= 0f ? Mathf.Clamp01((1f - _fade / FadeSeconds) * 2f - 1f) : 1f;
            glow *= glow;
            if (Mathf.Approximately(glow, _slabGlow))
            {
                return;
            }

            _slabGlow = glow;
            for (int i = 0; i < _slabs.Count; i++)
            {
                Renderer renderer = _slabs[i].Slab.Renderer;
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(_slabBlock);
                    _slabBlock.SetColor(EmissionColorId, _slabPool.HotGlow * glow);
                    renderer.SetPropertyBlock(_slabBlock);
                }
            }
        }

        private void ReleaseSlabs()
        {
            if (_slabPool != null)
            {
                foreach (SlabPlace place in _slabs)
                {
                    if (place.Slab.Renderer != null)
                    {
                        place.Slab.Renderer.SetPropertyBlock(null);
                    }

                    _slabPool.Give(place.Slab);
                }
            }

            _slabs.Clear();
        }

        private void OnDestroy()
        {
            // Torn down with the level: the slabs go with it.
            _slabs.Clear();
            foreach (Mesh mesh in _meshes)
            {
                Destroy(mesh);
            }

            foreach (Material material in _materials)
            {
                Destroy(material);
            }

            if (_markMesh != null)
            {
                Destroy(_markMesh);
            }
        }

        private void Build(IReadOnlyList<Vector3> points)
        {
            _points = new Vector3[points.Count];
            _along.Clear();
            float total = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                _points[i] = points[i] + Vector3.up * Lift;
                if (i > 0)
                {
                    total += Vector3.Distance(_points[i - 1], _points[i]);
                }

                _along.Add(total);
            }

            _length = total;
            if (_style.HasMark)
            {
                BuildMark(points);
            }

            if (_mark == null)
            {
                Layer("Seam", _style.Width, _style.Seam);
            }

            Layer("Halo", _style.GlowWidth, _style.Halo * (_mark != null ? MarkHaloShare : 1f));
            _head = new GameObject("Head").transform;
            _head.SetParent(transform, false);
            GlowDecal.PlacePool(_head, _style.HeadGlow, _style.HeadRadius);
            _grit = ParticleKit.Create(_head, "Grit", Vector3.up * 0.05f, _style.Grit, 0.4f, 0.9f, 0.04f, 0.12f, 1.2f, 2.8f, 1.2f, 80, 0.3f);
            ParticleKit.SetRate(_grit, 0f);
            if (_mark != null && _style.Chunks > 0)
            {
                // The front of a tearing fissure: dust thrown up and sparks off the molten core.
                _dust = ParticleKit.Create(_head, "Dust", Vector3.up * 0.1f, Dust, 0.9f, 1.8f, 0.35f, 0.8f, 0.3f, 1.1f, -0.04f, 90, 0.6f);
                ParticleKit.SetRate(_dust, 0f);
                _sparks = ParticleKit.UseCell(ParticleKit.Create(_head, "Sparks", Vector3.up * 0.05f, Sparks, 0.35f, 0.8f, 0.05f, 0.12f, 1.5f, 3.5f, 1.4f, 80, 0.25f), MarkerArt.ParticleCell.Ember);
                ParticleKit.SetRate(_sparks, 0f);
            }

            OpenTo(0f);
        }

        // The painted fissure along the line, its lips, and rock chunks heaved up beside it.
        private void BuildMark(IReadOnlyList<Vector3> points)
        {
            BossFxArt art = BossFxArt.Active;
            _mark = GroundMark.MaterialFor(_style.Mark);
            if (_mark == null)
            {
                return;
            }

            _materials.Add(_mark);
            if (_style.MarkGlow.a > 0f)
            {
                _mark.SetColor(GroundMark.EmissionColor, _style.MarkGlow);
            }

            _mark.SetFloat(GroundMark.OpenRamp, OpenRamp);
            _mark.SetFloat(GroundMark.OpenDistance, 0f);
            float tile = art.TileLength(_style.Mark) * _style.MarkWidth / Mathf.Max(0.1f, art.Width(_style.Mark));
            MeshRenderer strip = GroundMark.Strip(transform, "Mark", points, _style.MarkWidth, tile, _mark, true);
            _markMesh = strip.GetComponent<MeshFilter>().sharedMesh;
            PlaceChunks();
            if (_style.Slabs)
            {
                PlaceSlabs();
            }
        }

        private void PlaceChunks()
        {
            MarkerArt markers = MarkerArt.Active;
            if (markers == null || _style.Chunks <= 0 || markers.DebrisCount <= markers.HotChunks)
            {
                return;
            }

            int count = Mathf.FloorToInt(_length / 4f * _style.Chunks);
            for (int i = 0; i < count; i++)
            {
                float along = (i + Random.Range(0.1f, 0.9f)) * _length / Mathf.Max(1, count);
                Vector3 at = PointAt(along);
                Vector3 side = SideAt(along);
                float sign = i % 2 == 0 ? 1f : -1f;
                Vector3 rest = at + side * (sign * _style.MarkWidth * Random.Range(ChunkSide, ChunkSide + 0.08f)) - Vector3.up * (Lift + ChunkBedded);
                Mesh mesh = markers.Debris(Random.Range(markers.HotChunks, markers.DebrisCount));
                var go = new GameObject("Chunk");
                go.transform.SetParent(transform, false);
                go.transform.position = rest;
                go.transform.rotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));
                float size = Random.Range(ChunkMinSize, ChunkMaxSize);
                go.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, mesh.bounds.size.magnitude * 0.6f));
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = RockMaterial(markers);
                renderer.shadowCastingMode = ShadowCastingMode.On;
                go.SetActive(false);
                _chunks.Add(new Chunk { Transform = go.transform, Rest = rest, Along = along });
            }
        }

        // Slabs along both lips at random spacing, variant, size and sink, never on a tower or gate; each raised edge leans
        // over the fissure, and on the lip nearest the camera they turn along the crack so the edge still shows.
        private void PlaceSlabs()
        {
            _slabPool = CrackSlabPool.For(transform.parent);
            if (_slabPool == null || _length < SlabEndMargin * 2f)
            {
                return;
            }

            _slabBlock = new MaterialPropertyBlock();
            Camera view = Camera.main;
            Vector3 toCamera = view != null ? -view.transform.forward : Vector3.back;
            toCamera.y = 0f;
            toCamera = toCamera.sqrMagnitude > 0.0001f ? toCamera.normalized : Vector3.back;
            float along = SlabEndMargin + Random.Range(0f, SlabSpacingMin);
            int tried = 0;
            while (along < _length - SlabEndMargin || (tried < SlabsMin && along < _length))
            {
                TryPlaceSlab(along, toCamera);
                tried++;
                along += Random.Range(SlabSpacingMin, SlabSpacingMax);
            }
        }

        private void TryPlaceSlab(float along, Vector3 toCamera)
        {
            Vector3 at = PointAt(along);
            Vector3 side = SideAt(along);
            Vector3 tangent = Vector3.Cross(side, Vector3.up);
            float farSign = Vector3.Dot(side, toCamera) >= 0f ? -1f : 1f;
            float sign = Random.value < SlabFarLipShare ? farSign : -farSign;
            float scale = Random.Range(SlabScaleMin, SlabScaleMax) * SlabWorldScale;
            float hingeOut = _style.MarkWidth * SlabHingeShare * (1f + Random.Range(-SlabHingeJitter, SlabHingeJitter));
            Vector3 hinge = at + side * (sign * hingeOut);
            Vector3 facing = -side * sign;
            if (Vector3.Dot(facing, toCamera) < 0f)
            {
                Vector3 edge = Vector3.Dot(tangent, toCamera) >= 0f ? tangent : -tangent;
                facing = Vector3.Lerp(facing, edge, SlabNearAlong).normalized;
            }

            facing = Quaternion.Euler(0f, Random.Range(-SlabYawJitter, SlabYawJitter), 0f) * facing;
            if (_blocked != null && (_blocked(hinge) || _blocked(hinge + facing * (SlabReach * scale))))
            {
                return;
            }

            CrackSlabPool.Slab slab = _slabPool.Take(Random.Range(0, _slabPool.Variants));
            slab.Holder.SetParent(transform, false);
            slab.Holder.localScale = Vector3.one * scale;
            Vector3 rest = hinge - Vector3.up * (Lift + Random.Range(SlabSinkMin, SlabSinkMax) * scale);
            _slabs.Add(new SlabPlace { Slab = slab, Rest = rest, Yaw = Quaternion.LookRotation(facing, Vector3.up), Along = along });
        }

        // The debris pieces in the colour of torn basalt: dark, no glow (the shared debris look is a hot hex chunk).
        private static Material RockMaterial(MarkerArt markers)
        {
            if (_rock == null)
            {
                _rock = new Material(markers.DebrisMaterial) { name = "CrackRock" };
                _rock.SetColor(BaseColorId, Rock);
                _rock.SetColor(EmissionColorId, Color.black);
                _rock.SetFloat(RimStrengthId, 0f);
            }

            return _rock;
        }

        // One additive strip along the crack: three vertices across (edge, core, edge), the core bright, the edges soft.
        private void Layer(string name, float width, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            int count = _points.Length;
            var vertices = new Vector3[count * 3];
            var uvs = new Vector2[count * 3];
            var triangles = new int[Mathf.Max(0, count - 1) * TrianglesPerSegment];
            for (int i = 0; i < count; i++)
            {
                Vector3 side = Side(_points, i) * (width * 0.5f);
                float v = AlongMin + AlongSpan * (_length > 0f ? _along[i] / _length : 0f);
                vertices[i * 3] = _points[i] - side;
                vertices[i * 3 + 1] = _points[i];
                vertices[i * 3 + 2] = _points[i] + side;
                uvs[i * 3] = new Vector2(1f, v);
                uvs[i * 3 + 1] = new Vector2(0f, v);
                uvs[i * 3 + 2] = new Vector2(1f, v);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 3;
                int b = a + 3;
                int t = i * TrianglesPerSegment;
                // Left half and right half, each two triangles, wound both ways so the strip shows from above whatever its turn.
                Quad(triangles, t, a, a + 1, b + 1, b);
                Quad(triangles, t + 6, a + 1, a + 2, b + 2, b + 1);
                Quad(triangles, t + 12, b, b + 1, a + 1, a);
                Quad(triangles, t + 18, b + 1, b + 2, a + 2, a + 1);
            }

            var mesh = new Mesh { name = "CrackLine" + name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            Material material = GlowDecal.BladeMaterial(colour);
            material.SetColor(MarkerArt.Tint, colour);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _meshes.Add(mesh);
            _materials.Add(material);
            _triangles.Add(triangles);
        }

        private static void Quad(int[] triangles, int at, int a, int b, int c, int d)
        {
            triangles[at] = a;
            triangles[at + 1] = b;
            triangles[at + 2] = c;
            triangles[at + 3] = a;
            triangles[at + 4] = c;
            triangles[at + 5] = d;
        }

        private Vector3 PointAt(float distance)
        {
            for (int i = 1; i < _points.Length; i++)
            {
                if (_along[i] >= distance)
                {
                    float span = _along[i] - _along[i - 1];
                    float t = span > 0.0001f ? (distance - _along[i - 1]) / span : 1f;
                    return Vector3.Lerp(_points[i - 1], _points[i], t);
                }
            }

            return _points[_points.Length - 1];
        }

        private Vector3 SideAt(float distance)
        {
            for (int i = 1; i < _points.Length; i++)
            {
                if (_along[i] >= distance)
                {
                    Vector3 along = _points[i] - _points[i - 1];
                    along.y = 0f;
                    return along.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, along.normalized) : Vector3.right;
                }
            }

            return Vector3.right;
        }

        private static Vector3 Side(Vector3[] points, int i)
        {
            Vector3 incoming = i > 0 ? points[i] - points[i - 1] : Vector3.zero;
            Vector3 outgoing = i < points.Length - 1 ? points[i + 1] - points[i] : Vector3.zero;
            Vector3 along = incoming + outgoing;
            along.y = 0f;
            return along.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, along.normalized) : Vector3.right;
        }
    }
}
