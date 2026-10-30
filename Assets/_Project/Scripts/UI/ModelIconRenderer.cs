using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// Turns a procedural model into a small picture for the HUD cards. Requests queue up; one is rendered per
    /// frame by a camera far away from the scene, read back into a texture and handed over as a sprite. Pictures
    /// are cached by key, so a level only renders its cards once.
    /// </summary>
    public sealed class ModelIconRenderer : MonoBehaviour
    {
        private const int Size = 160;
        private const float RigDistance = 6000f;
        private const float OrthoSize = 0.78f;
        private const float ViewPitch = 32f;
        private const float ViewYaw = -38f;
        private const float ModelLift = -0.45f;
        private const float LightIntensity = 2.6f;
        private const float LightRange = 9f;
        private const float FillIntensity = 1.2f;

        private static readonly Dictionary<object, Sprite> Cache = new Dictionary<object, Sprite>();
        private static ModelIconRenderer _instance;

        private readonly Queue<Job> _queue = new Queue<Job>();
        private Transform _stage;
        private Camera _camera;
        private RenderTexture _texture;
        private bool _running;

        private readonly struct Job
        {
            public readonly object Key;
            public readonly Func<Transform, GameObject> Build;
            public readonly Action<Sprite> Done;

            public Job(object key, Func<Transform, GameObject> build, Action<Sprite> done)
            {
                Key = key;
                Build = build;
                Done = done;
            }
        }

        /// <summary>
        /// Asks for the picture of a model. <paramref name="build"/> creates the model (standing on y = 0, facing +z)
        /// under the given parent; <paramref name="done"/> gets the sprite, at once when it is cached.
        /// </summary>
        public static void Request(object key, Func<Transform, GameObject> build, Action<Sprite> done)
        {
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                done(cached);
                return;
            }

            if (_instance == null)
            {
                _instance = new GameObject("ModelIconRenderer").AddComponent<ModelIconRenderer>();
                _instance.Build();
            }

            _instance._queue.Enqueue(new Job(key, build, done));
            if (!_instance._running)
            {
                _instance.StartCoroutine(_instance.Render());
            }
        }

        private void Build()
        {
            _texture = new RenderTexture(Size, Size, 16, RenderTextureFormat.ARGB32);
            var rig = new GameObject("IconRig");
            rig.transform.position = new Vector3(0f, RigDistance, 0f);
            rig.transform.SetParent(transform, false);

            _stage = new GameObject("Stage").transform;
            _stage.SetParent(rig.transform, false);
            _stage.localPosition = new Vector3(0f, ModelLift, 0f);
            _stage.localRotation = Quaternion.Euler(0f, ViewYaw, 0f);

            var cameraGo = new GameObject("IconCamera");
            cameraGo.transform.SetParent(rig.transform, false);
            cameraGo.transform.localRotation = Quaternion.Euler(ViewPitch, 0f, 0f);
            cameraGo.transform.localPosition = cameraGo.transform.localRotation * new Vector3(0f, 0f, -8f);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = OrthoSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 20f;
            _camera.targetTexture = _texture;
            _camera.enabled = false;

            // Point lights only, so the rig never changes the lighting of the level it floats above.
            AddLight(rig.transform, new Vector3(-2.5f, 3.5f, -2.5f), LightIntensity);
            AddLight(rig.transform, new Vector3(3f, 1.5f, 1f), FillIntensity);
        }

        private static void AddLight(Transform parent, Vector3 localPosition, float intensity)
        {
            var go = new GameObject("IconLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = LightRange;
            light.shadows = LightShadows.None;
        }

        // One model per frame: build it, let the camera draw it at the end of the frame, read the pixels back.
        private IEnumerator Render()
        {
            _running = true;
            while (_queue.Count > 0)
            {
                Job request = _queue.Dequeue();
                if (Cache.TryGetValue(request.Key, out Sprite cached) && cached != null)
                {
                    request.Done(cached);
                    continue;
                }

                GameObject model = request.Build(_stage);
                _camera.enabled = true;
                yield return new WaitForEndOfFrame();

                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _texture;
                texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                _camera.enabled = false;
                Destroy(model);

                var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
                Cache[request.Key] = sprite;
                request.Done(sprite);
            }

            _running = false;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
