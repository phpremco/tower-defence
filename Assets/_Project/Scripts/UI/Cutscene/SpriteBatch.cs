using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// Many small camera-facing sprites in one mesh and one draw (<c>Game/CutsceneSprites</c>): the owner fills it each
    /// pose with <see cref="Begin"/>, <see cref="Add"/> and <see cref="End"/>. Every array is made once at its capacity, so
    /// a pose allocates nothing; unused slots are drawn at size zero. Additive for light (dust, stars, sparks), alpha for
    /// smoke and steam.
    /// </summary>
    public sealed class SpriteBatch
    {
        public const string ShaderName = "Game/CutsceneSprites";

        private const float BoundsSize = 4000f;
        private static readonly int ClockId = Shader.PropertyToID("_Clock");
        private static readonly Vector2[] Corners = { new Vector2(-1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f), new Vector2(1f, -1f) };

        private readonly Mesh _mesh;
        private readonly Vector3[] _centres;
        private readonly Vector4[] _shapes;
        private readonly Color[] _colours;
        private int _count;

        public SpriteBatch(Transform parent, string name, int capacity, Texture2D texture, bool additive, Color tint, float twinkle = 0f)
        {
            Capacity = Mathf.Max(1, capacity);
            int vertices = Capacity * 4;
            _centres = new Vector3[vertices];
            _shapes = new Vector4[vertices];
            _colours = new Color[vertices];
            var corners = new Vector2[vertices];
            var triangles = new int[Capacity * 6];
            for (int i = 0; i < Capacity; i++)
            {
                for (int c = 0; c < 4; c++)
                {
                    corners[i * 4 + c] = Corners[c];
                }

                int v = i * 4;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            _mesh = new Mesh { name = name, indexFormat = vertices > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            _mesh.MarkDynamic();
            _mesh.vertices = _centres;
            _mesh.uv = corners;
            _mesh.SetUVs(1, _shapes);
            _mesh.colors = _colours;
            _mesh.triangles = triangles;
            // The corners are spread in the vertex shader, so the mesh's own bounds mean nothing: never cull it.
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * BoundsSize);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Material = CreateMaterial(texture, additive, tint, twinkle);
            renderer.sharedMaterial = Material;
            Renderer = renderer;
        }

        public int Capacity { get; }

        public Material Material { get; }

        public MeshRenderer Renderer { get; }

        /// <summary>Starts a new pose: every sprite added before is dropped.</summary>
        public void Begin() => _count = 0;

        /// <summary>One sprite this pose: its centre (world), its half size in metres, colour, turn (radians) and twinkle phase.</summary>
        public void Add(Vector3 centre, float size, Color colour, float turn = 0f, float phase = 0f)
        {
            if (_count >= Capacity || size <= 0f)
            {
                return;
            }

            int v = _count * 4;
            var shape = new Vector4(size, turn, phase, 0f);
            for (int c = 0; c < 4; c++)
            {
                _centres[v + c] = centre;
                _shapes[v + c] = shape;
                _colours[v + c] = colour;
            }

            _count++;
        }

        /// <summary>Uploads the pose; the cutscene clock drives the twinkle.</summary>
        public void End(float clock)
        {
            for (int i = _count; i < Capacity; i++)
            {
                int v = i * 4;
                if (_shapes[v].x == 0f)
                {
                    break;
                }

                for (int c = 0; c < 4; c++)
                {
                    _shapes[v + c] = Vector4.zero;
                }
            }

            _mesh.SetVertices(_centres);
            _mesh.SetUVs(1, _shapes);
            _mesh.SetColors(_colours);
            Material.SetFloat(ClockId, clock);
        }

        private static Material CreateMaterial(Texture2D texture, bool additive, Color tint, float twinkle)
        {
            Shader shader = Shader.Find(ShaderName);
            var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Unlit")) { name = ShaderName };
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Tint", tint);
            material.SetFloat("_Twinkle", twinkle);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.renderQueue = (int)RenderQueue.Transparent + (additive ? 1 : 0);
            return material;
        }
    }
}
