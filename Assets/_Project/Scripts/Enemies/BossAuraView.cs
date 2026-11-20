using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Enemies
{
    /// <summary>
    /// What makes a level boss unmistakable on any ground (round 88a, Remco lost the Brood Mother against the road of
    /// Landing Site under the fire of his towers): a mark on the ground under it, drawn below the creature and above the
    /// road, so no hit effect covers it and no palette swallows it. A dark disc sets the body off from light road tiles and
    /// from glowing ground; a white-hot ring with a red glow round it reads against dark and warm ground alike and
    /// breathes slowly; a ripple runs out from it every few seconds (movement catches the eye); and chevrons ahead of the
    /// ring point the way it walks. Under a flyer the mark lies on the ground where it will pass. Pure view: it reads the
    /// boss and never drives it.
    /// </summary>
    public sealed class BossAuraView : MonoBehaviour
    {
        private const float GroundHeight = 0.06f;
        private const float LayerStep = 0.004f;
        private const float RingThickness = 0.045f;
        private const float GlowThickness = 0.16f;
        private const int RingSegments = 64;
        private const float DiscShare = 1.12f;
        private const float DiscAlpha = 0.62f;
        private const float RingEmission = 4.5f;
        private const float GlowEmission = 2.2f;
        private const float GlowAlpha = 0.35f;
        private const float BreathDepth = 0.25f;
        private const float BreathSpeed = 2.4f;
        private const float RippleSeconds = 1.8f;
        private const float RippleGrowth = 1.9f;
        private const float RippleAlpha = 0.8f;
        private const int Chevrons = 2;
        private const float ChevronSize = 0.42f;
        private const float ChevronGap = 0.34f;
        private const float ChevronLead = 0.28f;
        private const float ChevronEmission = 4f;
        private const float ChevronWave = 0.35f;
        private const float TurnSpeed = 6f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // White-hot core and a red glow: a warm, hostile mark that is neither a tower's blue nor the hero's yellow ring.
        private static readonly Color Core = new Color(1f, 0.86f, 0.7f);
        private static readonly Color Glow = new Color(1f, 0.16f, 0.08f);

        private Enemy _enemy;
        private float _radius;
        private Transform _root;
        private Transform _ripple;
        private Material _ringMaterial;
        private Material _glowMaterial;
        private Material _rippleMaterial;
        private Material _chevronMaterial;
        private Quaternion _facing = Quaternion.identity;
        private bool _shown;
        private readonly System.Collections.Generic.List<Mesh> _meshes = new System.Collections.Generic.List<Mesh>(4);

        /// <summary>Set by the creature's view while the body is hidden (deep under a liquid): the mark goes with it.</summary>
        public bool Hidden { get; set; }

        /// <summary>True while the mark is drawn (for probes and tests).</summary>
        public bool Showing => _shown;

        /// <param name="radius">Half the body across on the ground, in world units; the ring lies just outside it.</param>
        public void Initialize(Enemy enemy, float radius)
        {
            _enemy = enemy;
            _radius = radius;
            _root = new GameObject("BossAura").transform;
            // Beside the creature, not under it: the creature's own transform tilts, bobs and scales.
            _root.SetParent(transform.parent, false);
            BuildDisc();
            _ringMaterial = Emissive(Core, 1f, RingEmission);
            AddRing("Ring", _radius, RingThickness, _ringMaterial, 3);
            _glowMaterial = Emissive(Glow, GlowAlpha, GlowEmission);
            AddRing("Glow", _radius, GlowThickness, _glowMaterial, 2);
            _rippleMaterial = Emissive(Glow, RippleAlpha, GlowEmission);
            _ripple = AddRing("Ripple", _radius, RingThickness, _rippleMaterial, 1);
            _chevronMaterial = Emissive(Core, 1f, ChevronEmission);
            Mesh chevron = ChevronMesh();
            _meshes.Add(chevron);
            for (int i = 0; i < Chevrons; i++)
            {
                var go = new GameObject("Chevron");
                go.transform.SetParent(_root, false);
                go.transform.localPosition = Vector3.forward * (_radius + ChevronLead + i * ChevronGap) + Vector3.up * (LayerStep * 3);
                go.transform.localScale = Vector3.one * ChevronSize;
                go.AddComponent<MeshFilter>().sharedMesh = chevron;
                Flat(go.AddComponent<MeshRenderer>(), _chevronMaterial);
            }

            _root.gameObject.SetActive(false);
        }

        // A pooled boss that leaves the field is switched off: its mark, which lives beside it, goes with it.
        private void OnDisable()
        {
            if (_root != null)
            {
                _shown = false;
                _root.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            foreach (Mesh mesh in _meshes)
            {
                Destroy(mesh);
            }

            Destroy(_ringMaterial);
            Destroy(_glowMaterial);
            Destroy(_rippleMaterial);
            Destroy(_chevronMaterial);
        }

        private void LateUpdate()
        {
            bool show = _enemy != null && _enemy.IsAlive && !Hidden && !_enemy.IsConcealed;
            if (show != _shown)
            {
                _shown = show;
                _root.gameObject.SetActive(show);
                if (show)
                {
                    _facing = Heading();
                }
            }

            if (!show)
            {
                return;
            }

            Vector3 position = _enemy.Position;
            _facing = Quaternion.Slerp(_facing, Heading(), TurnSpeed * Time.deltaTime);
            _root.SetPositionAndRotation(new Vector3(position.x, GroundHeight, position.z), _facing);

            float time = Time.time;
            float breath = 1f - BreathDepth * (0.5f + 0.5f * Mathf.Sin(time * BreathSpeed));
            _ringMaterial.SetColor(EmissionColorId, Core * (RingEmission * breath));
            _glowMaterial.SetColor(EmissionColorId, Glow * (GlowEmission * breath));

            float phase = Mathf.Repeat(time / RippleSeconds, 1f);
            _ripple.localScale = Vector3.one * Mathf.Lerp(1f, RippleGrowth, phase);
            float fade = (1f - phase) * (1f - phase);
            _rippleMaterial.SetColor(BaseColorId, new Color(Glow.r, Glow.g, Glow.b, RippleAlpha * fade));
            _rippleMaterial.SetColor(EmissionColorId, Glow * (GlowEmission * fade));

            // The chevrons light up one after the other, away from the boss: the eye follows them down the road.
            float wave = 0.5f + 0.5f * Mathf.Sin(time * BreathSpeed * 2f);
            _chevronMaterial.SetColor(EmissionColorId, Core * (ChevronEmission * (1f - ChevronWave * wave)));
        }

        private Quaternion Heading()
        {
            Vector3 heading = _enemy.Heading;
            heading.y = 0f;
            return heading.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(heading, Vector3.up) : _facing;
        }

        // A soft dark blob (the flyer's shadow art) or, without the art, a plain dark disc.
        private void BuildDisc()
        {
            float size = _radius * DiscShare * 2f;
            MarkerArt art = MarkerArt.Active;
            MeshRenderer blob = art != null ? art.Place(MarkerKind.BlobShadow, _root, "Disc") : null;
            if (blob != null)
            {
                blob.transform.localScale = Vector3.one * size;
                MarkerArt.SetTint(blob, new MaterialPropertyBlock(), new Color(1f, 1f, 1f, DiscAlpha));
                return;
            }

            GameObject disc = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, Color.black, _root, Vector3.down * 0.01f, new Vector3(size, 0.005f, size));
            Flat(disc.GetComponent<MeshRenderer>(), PlaceholderVisuals.TransparentMaterialFor(Color.black, DiscAlpha, 0f));
        }

        // layer: the parts lie a hair above each other in a fixed order, so the core is never drawn under its glow.
        private Transform AddRing(string ringName, float radius, float thickness, Material material, int layer)
        {
            var builder = new PropMeshBuilder(1);
            builder.AddTorus(Vector3.zero, Vector3.up, radius, thickness, RingSegments, 4, 0);
            var go = new GameObject(ringName);
            go.transform.SetParent(_root, false);
            // A flat band, not a tube: nothing of it stands up into the creature's legs.
            go.transform.localScale = new Vector3(1f, 0.1f, 1f);
            go.transform.localPosition = Vector3.up * (LayerStep * layer);
            Mesh mesh = builder.ToMesh(ringName);
            _meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            Flat(go.AddComponent<MeshRenderer>(), material);
            return go.transform;
        }

        private static void Flat(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Its own copy, so the breathing of one boss never moves the mark of another.
        private static Material Emissive(Color colour, float alpha, float emission) => new Material(PlaceholderVisuals.TransparentMaterialFor(colour, alpha, emission));

        // A chevron lying on the ground, its point along +Z, one unit wide; drawn from both sides.
        private static Mesh ChevronMesh()
        {
            var mesh = new Mesh { name = "BossChevron" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.15f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0f, 0f, 0.12f), new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, -0.15f),
            };
            mesh.triangles = new[]
            {
                0, 1, 3, 1, 2, 3, 0, 3, 5, 3, 4, 5,
                0, 3, 1, 1, 3, 2, 0, 5, 3, 3, 5, 4,
            };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
