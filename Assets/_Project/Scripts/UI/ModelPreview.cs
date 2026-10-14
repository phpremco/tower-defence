using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Shows a slowly turning 3D model inside the interface. A small rig (camera, light, model) is built far away from
    /// the scene and renders into a texture that a RawImage displays.
    /// </summary>
    public sealed class ModelPreview : MonoBehaviour
    {
        private const int TextureSize = 512;
        private const float RigDistance = 5000f;
        private const float TurnDegreesPerSecond = 28f;
        private const float CameraHeight = 1.3f;
        private const float CameraBack = 4.4f;
        private static readonly Color Background = new Color(0.05f, 0.08f, 0.13f, 1f);

        private Transform _rig;
        private Transform _turntable;
        private RenderTexture _texture;

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

        public void Show(GameObject model, float scale, Color tint)
        {
            for (int i = _turntable.childCount - 1; i >= 0; i--)
            {
                Destroy(_turntable.GetChild(i).gameObject);
            }

            _turntable.localRotation = Quaternion.identity;
            if (model != null)
            {
                ModelFit.Place(model, _turntable, Vector3.zero, 0f, scale, tint, 0.15f);
            }
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

            var cameraGo = new GameObject("PreviewCamera");
            cameraGo.transform.SetParent(_rig, false);
            cameraGo.transform.localPosition = new Vector3(0f, CameraHeight, -CameraBack);
            cameraGo.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.fieldOfView = 32f;
            camera.targetTexture = _texture;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;

            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(_rig, false);
            lightGo.transform.localRotation = Quaternion.Euler(40f, -35f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.85f;
        }

        private void Update()
        {
            if (_turntable != null)
            {
                _turntable.Rotate(0f, TurnDegreesPerSecond * Time.unscaledDeltaTime, 0f);
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

