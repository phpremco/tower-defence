using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Hero;
using Game.Towers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Shows a slowly turning 3D model inside the interface. A small rig (camera, lights, turntable) is built far
    /// away from the scene and renders into a texture that a RawImage displays. Models are built the same way as
    /// in the level and framed automatically, so a drone and a tower fill the window alike.
    /// </summary>
    public sealed class ModelPreview : MonoBehaviour
    {
        private const int TextureSize = 512;
        private const float RigDistance = 5000f;
        private const float RigSpacing = 100f;
        private static int _rigCount;
        private const float TurnDegreesPerSecond = 28f;
        private const float BurstDegreesPerSecond = 720f;
        private const float BurstDecay = 2.6f;
        private const float LookDownDegrees = 16f;
        private const float FieldOfView = 30f;
        private const float FrameFill = 0.8f;
        private const int FitSamples = 16;
        private const int FitRounds = 6;
        private const int BoxCorners = 8;
        private const float MinFitDistance = 0.5f;
        private const float RotorDegreesPerSecond = 1400f;
        private const float KeyIntensity = 1.6f;
        private const float FillIntensity = 0.5f;
        private const float RimIntensity = 1.1f;
        // The stage (Blender round 27): a pedestal the model stands on, a halo round its foot and the research-bay
        // backdrop, so the preview reads as a product shot rather than a model floating in the dark. The pedestal grows
        // with the model's footprint, measured across its narrow side so a long barrel does not put a tower on a huge plate.
        private const float FloorRadiusShare = 1.25f;
        private const float NarrowRadiusShare = 1.6f;
        private const float MinFloorRadius = 0.3f;
        private const float HaloLift = 0.003f;
        private const float BackdropDistance = 30f;
        private const float BackdropSize = 40f;
        private static readonly Color Background = new Color(0.05f, 0.08f, 0.13f, 1f);
        private static readonly Color KeyColor = new Color(1f, 0.96f, 0.9f);
        private static readonly Color FillColor = new Color(0.55f, 0.7f, 1f);
        private static readonly Color RimColor = new Color(0.4f, 0.8f, 1f);

        private readonly List<Transform> _spinners = new List<Transform>();
        private Transform _rig;
        private Transform _turntable;
        private Transform _cameraPivot;
        private Camera _camera;
        private RenderTexture _texture;
        private readonly List<Material> _owned = new List<Material>();
        private readonly List<Vector3> _vertices = new List<Vector3>(4096);
        private readonly List<Vector3> _fitPoints = new List<Vector3>(64);
        private readonly HashSet<Renderer> _farLods = new HashSet<Renderer>();
        private Mesh _baked;
        private int _reframeIn;
        private const int ReframeFrames = 2;
        private DisplayStageArt _stage;
        private Transform _floor;
        private Transform _floorTop;
        private Transform _ring;
        private float _burst;

        public static ModelPreview Create(RectTransform parent, Vector2 size)
        {
            var go = new GameObject("ModelPreview", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.raycastTarget = false;
            go.GetComponent<RectTransform>().sizeDelta = size;

            var preview = go.AddComponent<ModelPreview>();
            preview.Build(image);
            return preview;
        }

        /// <summary>An imported model (such as the base), tinted like the level does it.</summary>
        public void ShowModel(GameObject model, float scale, Color tint)
        {
            Clear();
            if (model != null)
            {
                ModelFit.Place(model, _turntable, Vector3.zero, 0f, scale, tint, 0.15f);
            }

            Frame();
        }

        /// <summary>A tower at its top level, built exactly like the one on the field.</summary>
        public void ShowTower(TowerDefinition tower)
        {
            Clear();
            if (TowerModelBuilder.HasModel(tower))
            {
                TowerModelBuilder.Build(_turntable, tower, tower.MaxLevelIndex);
            }

            Frame();
        }

        /// <summary>Empties the turntable, for a window that is hidden while others stay in use.</summary>
        public void ShowNothing() => Clear();

        /// <summary>A creature at rest, built like the one on the field.</summary>
        public void ShowEnemy(Game.Enemies.EnemyDefinition enemy)
        {
            Clear();
            Game.Enemies.EnemyDisplayBuilder.Build(_turntable, enemy);
            Frame();
            // A skinned creature stands in its bind pose until its animator has run once: frame it again in its idle.
            _reframeIn = ReframeFrames;
        }

        /// <summary>The commander standing at rest with the weapon in hand.</summary>
        public void ShowHero(HeroDefinition hero)
        {
            Clear();
            HeroView.BuildDisplayFigure(_turntable, hero);
            Frame();
        }

        /// <summary>A drone with its rotors spinning, as it flies in the level.</summary>
        public void ShowDrone(DroneDefinition drone)
        {
            Clear();
            DroneModelBuilder.Rig rig = DroneModelBuilder.BuildDisplay(_turntable, drone);
            _spinners.AddRange(rig.Rotors);
            Frame();
        }

        /// <summary>The base with its dish turning: the imported model when there is one, else the outpost in the level's glow colour.</summary>
        public void ShowBase(Color glow, Game.Levels.BaseArtDefinition art = null)
        {
            Clear();
            Game.Levels.LevelVisualBuilder.BuildBaseDisplay(_turntable, glow, art);
            Frame();
        }

        /// <summary>Spins the turntable fast for a moment, slowing back to its idle turn: the model reacts to the purchase.</summary>
        public void Burst() => _burst = 1f;

        private void Clear()
        {
            _spinners.Clear();
            // Removed at once rather than at the end of the frame, so framing the next model does not measure the old one.
            for (int i = _turntable.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_turntable.GetChild(i).gameObject);
            }

            _turntable.localRotation = Quaternion.identity;
            _turntable.localPosition = Vector3.zero;
        }

        // Centres whatever stands on the turntable and backs the camera off until it fills most of the window.
        private void Frame()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool any = false;
            // The padded bounds' top: a turning, breathing creature can reach above its measured pose (the Shattered's
            // spires were cut off at the top), so the picture keeps room up to it; the seat still uses the posed feet.
            float ceiling = float.MinValue;
            CollectFarLods();
            foreach (Renderer renderer in _turntable.GetComponentsInChildren<Renderer>())
            {
                // The far LOD is not what the window shows, and its rig poses its own (lower) legs.
                if (_farLods.Contains(renderer))
                {
                    continue;
                }

                // Sparks and trails are no part of the body; a skinned mesh's own bounds are padded for its whole range of
                // motion, which left a boss standing in the air above the pedestal: its posed vertices are measured instead.
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                Bounds part = renderer is SkinnedMeshRenderer skinned ? PosedBounds(skinned) : renderer.bounds;
                ceiling = Mathf.Max(ceiling, renderer.bounds.max.y);
                if (!any)
                {
                    bounds = part;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(part);
                }
            }

            if (!any)
            {
                return;
            }

            // The pedestal lifts the model by its height over the foot; half of that is added to the picture and the
            // camera aims that much lower, so the foot is never cut off for a flat model.
            float floorRadius = FloorRadius(bounds);
            float lift = _stage != null ? floorRadius * _stage.HeightShare * 0.5f : 0f;
            Vector3 centre = bounds.center - _turntable.position;
            _turntable.localPosition = new Vector3(-centre.x, -centre.y + lift, -centre.z);

            // The model turns, so sideways it may show its longest horizontal side; vertically only its height counts.
            float wide = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float tall = Mathf.Max(bounds.extents.y + lift, 0.1f);
            float radius = Mathf.Max(bounds.extents.magnitude, 0.2f) + floorRadius;
            PlaceStage(bounds, floorRadius);
            CollectFitPoints(wide, tall + Mathf.Max(0f, ceiling - bounds.max.y), lift * 2f - tall);
            Vector2 view = FitView(Mathf.Max(wide, tall) / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) + wide);
            float distance = view.x;
            _camera.transform.localPosition = new Vector3(0f, view.y, -distance);
            _camera.nearClipPlane = Mathf.Max(0.05f, distance - radius * 2f);
            _camera.farClipPlane = distance + BackdropDistance + radius;
        }

        // What must stay in the picture while the turntable turns, in the camera pivot's frame: the rings the model sweeps
        // at its head and at its feet, and the pedestal as it really stands (its foot flares wider than its top and, seen
        // from above, its near edge reaches lowest: the Shattered's pedestal was cut off at the bottom by a box fit).
        private void CollectFitPoints(float bodyRadius, float top, float feet)
        {
            _fitPoints.Clear();
            Quaternion toCamera = Quaternion.Inverse(Quaternion.Euler(LookDownDegrees, 0f, 0f));
            for (int i = 0; i < FitSamples; i++)
            {
                float angle = i * Mathf.PI * 2f / FitSamples;
                var round = new Vector3(Mathf.Cos(angle) * bodyRadius, 0f, Mathf.Sin(angle) * bodyRadius);
                _fitPoints.Add(toCamera * (round + Vector3.up * top));
                _fitPoints.Add(toCamera * (round + Vector3.up * feet));
            }

            if (_floor == null)
            {
                return;
            }

            foreach (Renderer renderer in _floor.GetComponentsInChildren<Renderer>())
            {
                Bounds part = renderer.bounds;
                for (int corner = 0; corner < BoxCorners; corner++)
                {
                    var world = new Vector3(
                        (corner & 1) == 0 ? part.min.x : part.max.x,
                        (corner & 2) == 0 ? part.min.y : part.max.y,
                        (corner & 4) == 0 ? part.min.z : part.max.z);
                    _fitPoints.Add(toCamera * (world - _rig.position));
                }
            }
        }

        // The camera's distance (x) and its shift along its own up axis (y) so the fit points fill FrameFill of the picture,
        // as much room above as below. A few rounds of moving and scaling settle it; the start is the old box fit.
        private Vector2 FitView(float start)
        {
            float reach = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) * FrameFill;
            float distance = Mathf.Max(start, MinFitDistance);
            float up = 0f;
            for (int round = 0; round < FitRounds; round++)
            {
                float top = float.MinValue;
                float bottom = float.MaxValue;
                float side = 0f;
                foreach (Vector3 point in _fitPoints)
                {
                    float depth = Mathf.Max(point.z + distance, MinFitDistance);
                    float slope = (point.y - up) / depth;
                    top = Mathf.Max(top, slope);
                    bottom = Mathf.Min(bottom, slope);
                    side = Mathf.Max(side, Mathf.Abs(point.x) / depth);
                }

                up += (top + bottom) * 0.5f * distance;
                distance = Mathf.Max(MinFitDistance, distance * Mathf.Max((top - bottom) * 0.5f, side) / reach);
            }

            return new Vector2(distance, up);
        }

        private void CollectFarLods()
        {
            _farLods.Clear();
            foreach (LODGroup group in _turntable.GetComponentsInChildren<LODGroup>())
            {
                LOD[] lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                {
                    foreach (Renderer renderer in lods[i].renderers)
                    {
                        if (renderer != null)
                        {
                            _farLods.Add(renderer);
                        }
                    }
                }
            }
        }

        // The world bounds of a skinned mesh in the pose it stands in now (baked), not its padded import bounds.
        private Bounds PosedBounds(SkinnedMeshRenderer skinned)
        {
            _baked ??= new Mesh();
            // Unscaled, so the transform's own matrix scales it exactly once.
            skinned.BakeMesh(_baked, false);
            _baked.GetVertices(_vertices);
            if (_vertices.Count == 0)
            {
                return skinned.bounds;
            }

            Matrix4x4 toWorld = skinned.transform.localToWorldMatrix;
            var bounds = new Bounds(toWorld.MultiplyPoint3x4(_vertices[0]), Vector3.zero);
            for (int i = 1; i < _vertices.Count; i++)
            {
                bounds.Encapsulate(toWorld.MultiplyPoint3x4(_vertices[i]));
            }

            return bounds;
        }

        // Grows with the footprint across the narrow side (Clear() has turned the model to rest), never past the old
        // share of the widest side: a tower's long barrel no longer decides how big its plate is.
        private static float FloorRadius(Bounds bounds)
        {
            float narrow = Mathf.Min(bounds.extents.x, bounds.extents.z);
            float wide = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return Mathf.Max(MinFloorRadius, Mathf.Min(narrow * NarrowRadiusShare, wide * FloorRadiusShare));
        }

        // The pedestal's top (its pivot) sits at the model's feet; the halo lies at its foot. The backdrop hangs far behind.
        private void PlaceStage(Bounds bounds, float radius)
        {
            if (_stage == null)
            {
                return;
            }

            // Frame() has just shifted the turntable to centre the model, so the measured bounds move with it.
            float y = bounds.min.y + _turntable.localPosition.y - _rig.position.y;
            _floor.localPosition = new Vector3(0f, y, 0f);
            _floor.localScale = Vector3.one * radius;
            _ring.localPosition = new Vector3(0f, y - _stage.HeightShare * radius + HaloLift, 0f);
            _ring.localScale = new Vector3(radius, 1f, radius);
        }

        // The Blender stage from the UI skin; without it the model turns on the plain background colour.
        private void BuildStage()
        {
            _stage = UiKit.Skin != null ? UiKit.Skin.DisplayStage : null;
            if (_stage == null || !_stage.IsComplete)
            {
                _stage = null;
                return;
            }

            bool low = ModelQuality.IsLow;
            Material pedestal = _stage.PedestalMaterial(low);
            _floor = Holder("Pedestal", _rig);
            Part(_floor, _stage.Pedestal, _stage.PedestalNode, pedestal);
            if (_stage.PedestalRing != null)
            {
                _floorTop = Holder("PedestalRing", _floor);
                Part(_floorTop, _stage.PedestalRing, _stage.PedestalRingNode, pedestal);
            }

            _ring = Holder("Halo", _rig);
            Part(_ring, _stage.Halo, _stage.HaloNode, Own(GlowDecal.Additive(_stage.HaloTexture(low), _stage.HaloColour)));

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(backdrop.GetComponent<Collider>());
            backdrop.name = "Backdrop";
            backdrop.transform.SetParent(_cameraPivot, false);
            backdrop.transform.localPosition = new Vector3(0f, 0f, BackdropDistance);
            // The painted wall is 2:1; the square window sees its middle (the lit bay and the floor line).
            backdrop.transform.localScale = new Vector3(BackdropSize, BackdropSize * 0.5f, 1f);
            Material backdropMaterial = Own(new Material(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial));
            backdropMaterial.SetTexture("_BaseMap", _stage.Backdrop(low));
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = backdropMaterial;
        }

        private static Transform Holder(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        // A mesh at its place inside its FBX (the node matrix carries the axis change), so the holder's origin is the pivot.
        private static void Part(Transform holder, Mesh mesh, Matrix4x4 node, Material material)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(holder, false);
            go.transform.localPosition = node.GetColumn(3);
            go.transform.localRotation = node.rotation;
            go.transform.localScale = node.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private Material Own(Material material)
        {
            _owned.Add(material);
            return material;
        }

        private void Build(RawImage image)
        {
            _texture = new RenderTexture(TextureSize, TextureSize, 16);
            image.texture = _texture;

            var rig = new GameObject("PreviewRig");
            // Every window gets its own spot far off the field, so one camera never sees another window's model.
            rig.transform.position = new Vector3(RigDistance, 0f, _rigCount++ * RigSpacing);
            _rig = rig.transform;

            var turntable = new GameObject("Turntable");
            turntable.transform.SetParent(_rig, false);
            _turntable = turntable.transform;

            // The camera hangs from a pivot tilted down a little, so framing only has to move it back along its own axis.
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(_rig, false);
            pivot.transform.localRotation = Quaternion.Euler(LookDownDegrees, 0f, 0f);
            _cameraPivot = pivot.transform;

            var cameraGo = new GameObject("PreviewCamera");
            cameraGo.transform.SetParent(_cameraPivot, false);
            cameraGo.transform.localPosition = new Vector3(0f, 0f, -5f);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Background;
            _camera.fieldOfView = FieldOfView;
            _camera.targetTexture = _texture;
            _camera.aspect = 1f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 40f;

            // Three lights like a product shot: a warm key from the front right, a cool fill from the left and a
            // rim from behind, so the dark undersides and the glowing parts both read.
            AddLight("Key", new Vector3(40f, -35f, 0f), KeyColor, KeyIntensity).shadows = LightShadows.Soft;
            AddLight("Fill", new Vector3(20f, 60f, 0f), FillColor, FillIntensity);
            AddLight("Rim", new Vector3(-20f, 160f, 0f), RimColor, RimIntensity);
            BuildStage();
        }

        private Light AddLight(string name, Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rig, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        private void Update()
        {
            if (_turntable == null)
            {
                return;
            }

            if (_reframeIn > 0 && --_reframeIn == 0)
            {
                Frame();
            }

            float dt = Time.unscaledDeltaTime;
            _burst = Mathf.Max(0f, _burst - BurstDecay * dt);
            float eased = _burst * _burst;
            _turntable.Rotate(0f, (TurnDegreesPerSecond + BurstDegreesPerSecond * eased) * dt, 0f);
            for (int i = 0; i < _spinners.Count; i++)
            {
                if (_spinners[i] != null)
                {
                    _spinners[i].Rotate(0f, RotorDegreesPerSecond * dt, 0f, Space.Self);
                }
            }

            // The pedestal's top ring turns against the model, the halo drifts slowly: the stage reads as machinery.
            if (_stage != null)
            {
                if (_floorTop != null)
                {
                    _floorTop.Rotate(0f, _stage.RingDegreesPerSecond * dt, 0f, Space.Self);
                }

                _ring.Rotate(0f, _stage.HaloDegreesPerSecond * dt, 0f, Space.Self);
            }
        }

        private void OnDestroy()
        {
            if (_baked != null)
            {
                Destroy(_baked);
            }

            if (_rig != null)
            {
                Destroy(_rig.gameObject);
            }

            if (_texture != null)
            {
                _texture.Release();
            }

            foreach (Material material in _owned)
            {
                Destroy(material);
            }
        }
    }
}
