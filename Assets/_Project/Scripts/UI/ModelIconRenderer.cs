using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Turns a procedural model into a small picture for the HUD cards and the dialogue portraits. Requests queue up;
    /// one is rendered per frame by a camera far away from the scene, read back into a texture and handed over as a
    /// sprite. Pictures are cached by key, so a level only renders its cards once.
    /// </summary>
    public sealed class ModelIconRenderer : MonoBehaviour
    {
        private const float RigDistance = 6000f;
        private const float CameraDistance = 8f;
        private const float LightIntensity = 2.6f;
        private const float LightRange = 9f;
        private const float FillIntensity = 1.2f;
        private const int DepthBits = 16;

        // Behind the model as the camera sees it, a little above and to the side, so the rim catches one edge more.
        private static readonly Vector3 RimOffset = new Vector3(0.9f, 1.1f, 2.4f);

        private static readonly Dictionary<object, Sprite> Cache = new Dictionary<object, Sprite>();
        private static ModelIconRenderer _instance;

        private readonly Queue<Job> _queue = new Queue<Job>();
        private readonly Dictionary<int, RenderTexture> _textures = new Dictionary<int, RenderTexture>();
        private Transform _rig;
        private Transform _stage;
        private Transform _cameraTransform;
        private Camera _camera;
        private Light _rim;
        private bool _running;

        private readonly struct Job
        {
            public readonly object Key;
            public readonly Func<Transform, GameObject> Build;
            public readonly Action<Sprite> Done;
            public readonly IconFraming Framing;

            public Job(object key, Func<Transform, GameObject> build, Action<Sprite> done, IconFraming framing)
            {
                Key = key;
                Build = build;
                Done = done;
                Framing = framing;
            }
        }

        /// <summary>
        /// Asks for the picture of a model. <paramref name="build"/> creates the model (standing on y = 0, facing +z)
        /// under the given parent; <paramref name="done"/> gets the sprite, at once when it is cached.
        /// </summary>
        public static void Request(object key, Func<Transform, GameObject> build, Action<Sprite> done) => Request(key, build, done, IconFraming.Default);

        /// <summary>As <see cref="Request(object, Func{Transform, GameObject}, Action{Sprite})"/>, with an explicit framing.</summary>
        public static void Request(object key, Func<Transform, GameObject> build, Action<Sprite> done, IconFraming framing)
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

            _instance._queue.Enqueue(new Job(key, build, done, framing));
            if (!_instance._running)
            {
                _instance.StartCoroutine(_instance.Render());
            }
        }

        private void Build()
        {
            _rig = new GameObject("IconRig").transform;
            _rig.position = new Vector3(0f, RigDistance, 0f);
            _rig.SetParent(transform, false);

            _stage = new GameObject("Stage").transform;
            _stage.SetParent(_rig, false);

            var cameraGo = new GameObject("IconCamera");
            _cameraTransform = cameraGo.transform;
            _cameraTransform.SetParent(_rig, false);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 20f;
            _camera.enabled = false;

            // Point lights only, so the rig never changes the lighting of the level it floats above.
            AddLight(_rig, new Vector3(-2.5f, 3.5f, -2.5f), LightIntensity);
            AddLight(_rig, new Vector3(3f, 1.5f, 1f), FillIntensity);
            _rim = AddLight(_rig, Vector3.zero, 0f);
            _rim.enabled = false;
        }

        private static Light AddLight(Transform parent, Vector3 localPosition, float intensity)
        {
            var go = new GameObject("IconLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = LightRange;
            light.shadows = LightShadows.None;
            return light;
        }

        private void Frame(IconFraming framing)
        {
            _stage.localPosition = new Vector3(0f, framing.Lift, 0f);
            _stage.localRotation = Quaternion.Euler(0f, framing.Yaw, 0f);
            Quaternion view = Quaternion.Euler(framing.Pitch, 0f, 0f);
            _cameraTransform.localRotation = view;
            _cameraTransform.localPosition = view * new Vector3(0f, 0f, -CameraDistance);
            _camera.orthographicSize = framing.OrthoSize;
            _camera.targetTexture = TextureFor(framing.Size);

            _rim.enabled = framing.RimIntensity > 0f;
            _rim.color = framing.Rim;
            _rim.intensity = framing.RimIntensity;
            _rim.transform.localPosition = new Vector3(0f, framing.Lift, 0f) + view * RimOffset;
        }

        // One render texture per picture size, kept for the session: cards and portraits alternate without reallocating.
        private RenderTexture TextureFor(int size)
        {
            if (!_textures.TryGetValue(size, out RenderTexture texture))
            {
                texture = new RenderTexture(size, size, DepthBits, RenderTextureFormat.ARGB32);
                _textures[size] = texture;
            }

            return texture;
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

                Frame(request.Framing);
                GameObject model = request.Build(_stage);
                _camera.enabled = true;
                yield return new WaitForEndOfFrame();

                int size = request.Framing.Size;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _camera.targetTexture;
                texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;
                _camera.enabled = false;
                Destroy(model);

                var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                Cache[request.Key] = sprite;
                request.Done(sprite);
            }

            _running = false;
        }

        private void OnDestroy()
        {
            foreach (RenderTexture texture in _textures.Values)
            {
                texture.Release();
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
