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
        private const float TurnDegreesPerSecond = 28f;
        private const float BurstDegreesPerSecond = 720f;
        private const float BurstDecay = 2.6f;
        private const float LookDownDegrees = 16f;
        private const float FieldOfView = 30f;
        private const float FrameFill = 0.8f;
        private const float RotorDegreesPerSecond = 1400f;
        private const float KeyIntensity = 1.6f;
        private const float FillIntensity = 0.5f;
        private const float RimIntensity = 1.1f;
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
            else if (tower.Model != null)
            {
                ModelFit.Place(tower.Model, _turntable, Vector3.zero, 0f, tower.ModelScale, tower.KeepModelColours ? Color.white : Color.Lerp(Color.white, tower.Color, 0.55f), 0.15f);
            }

            Frame();
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
            DroneModelBuilder.Rig rig = DroneModelBuilder.Build(_turntable, drone);
            _spinners.AddRange(rig.Rotors);
            Frame();
        }

        /// <summary>The outpost with its ring and dish turning, in the glow colour of the level.</summary>
        public void ShowBase(Color glow)
        {
            Clear();
            Game.Levels.LevelVisualBuilder.BuildBaseDisplay(_turntable, glow);
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
            foreach (Renderer renderer in _turntable.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (!any)
                {
                    bounds = renderer.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!any)
            {
                return;
            }

            Vector3 centre = bounds.center - _turntable.position;
            _turntable.localPosition = new Vector3(-centre.x, -centre.y, -centre.z);

            // The model turns, so sideways it may show its longest horizontal side; vertically only its height counts.
            float halfTan = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
            float wide = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float tall = Mathf.Max(bounds.extents.y, 0.1f);
            float radius = Mathf.Max(bounds.extents.magnitude, 0.2f);
            float distance = (Mathf.Max(wide, tall) / halfTan + wide) / FrameFill;
            _camera.transform.localPosition = new Vector3(0f, 0f, -distance);
            _camera.nearClipPlane = Mathf.Max(0.05f, distance - radius * 2f);
            _camera.farClipPlane = distance + radius * 2f;
        }

        private void Build(RawImage image)
        {
            _texture = new RenderTexture(TextureSize, TextureSize, 16);
            image.texture = _texture;

            var rig = new GameObject("PreviewRig");
            rig.transform.position = new Vector3(RigDistance, 0f, 0f);
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
            AddLight("Key", new Vector3(40f, -35f, 0f), KeyColor, KeyIntensity);
            AddLight("Fill", new Vector3(20f, 60f, 0f), FillColor, FillIntensity);
            AddLight("Rim", new Vector3(-20f, 160f, 0f), RimColor, RimIntensity);
        }

        private void AddLight(string name, Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rig, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (_turntable == null)
            {
                return;
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
        }

        private void OnDestroy()
        {
            if (_rig != null)
            {
                Destroy(_rig.gameObject);
            }

            if (_texture != null)
            {
                _texture.Release();
            }
        }
    }
}
