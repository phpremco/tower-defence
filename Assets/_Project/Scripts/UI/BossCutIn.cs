using Game.Core;
using Game.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The comic cut-in of a boss intro (Docs/BOSS_INTROS.md): a tilted panel slides in at the right edge of the screen
    /// with the hero's visor seen from the front, lit at the rim in the boss's colour (warm for fire, cold for ice). The
    /// picture comes from a small camera that rides in front of the head bone, rendering into a texture only while the
    /// panel shows; the visor covers his face, so his face is never shown. Look only.
    /// </summary>
    public sealed class BossCutIn : MonoBehaviour
    {
        private const string HeadBone = "Head";
        private const float PanelSize = 300f;
        private const float FrameWidth = 10f;
        private const float TiltDegrees = -4f;
        private const float EdgeGap = 36f;
        private const float RaiseFromMiddle = 70f;
        private const float SlideSeconds = 0.25f;
        private const int TextureSize = 384;
        private const int LowTextureSize = 256;
        private const float CameraDistance = 0.85f;
        private const float CameraLift = 0.12f;
        private const float CameraFieldOfView = 26f;
        private const float CameraNear = 0.05f;
        private const float CameraFar = 80f;
        private const float RimRange = 2.4f;
        private const float RimIntensity = 4f;
        private const float RimDistance = 0.7f;
        private const float RimLift = 0.25f;
        private const int LabelSize = 22;
        private const string Label = "MERCER";

        private RectTransform _panel;
        private Image _frame;
        private Text _label;
        private Camera _camera;
        private Light _rim;
        private RenderTexture _texture;
        private Transform _head;
        private HeroArtView _hero;
        private Vector3 _boss;
        private float _slide;
        private bool _showing;

        /// <summary>True while the panel is on screen (sliding included).</summary>
        public bool IsShowing => _slide > 0f;

        /// <summary>The panel, for the HUD keep-out.</summary>
        public RectTransform Panel => _panel;

        public void Initialize(RectTransform safe)
        {
            _frame = UiKit.CreatePanel(safe, "BossCutIn", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
            _panel = _frame.rectTransform;
            _panel.pivot = new Vector2(1f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelSize, PanelSize);
            _panel.localRotation = Quaternion.Euler(0f, 0f, TiltDegrees);
            var picture = new GameObject("Visor", typeof(RectTransform)).AddComponent<RawImage>();
            picture.transform.SetParent(_panel, false);
            picture.raycastTarget = false;
            RectTransform rect = picture.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(FrameWidth, FrameWidth);
            rect.offsetMax = new Vector2(-FrameWidth, -FrameWidth);
            int size = ModelQuality.IsLow ? LowTextureSize : TextureSize;
            _texture = new RenderTexture(size, size, 24) { name = "BossCutIn" };
            picture.texture = _texture;
            _label = UiKit.CreateFixedText(_panel, Label, LabelSize, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(FrameWidth + 8f, FrameWidth + 4f), new Vector2(PanelSize * 0.6f, 32f));
            _panel.gameObject.SetActive(false);
        }

        /// <summary>Slides the panel in with the hero's visor, lit from the boss's side in its tint.</summary>
        public void Show(HeroArtView hero, Vector3 bossPosition, Color tint)
        {
            _hero = hero;
            _head = hero != null ? hero.Bone(HeadBone) : null;
            if (_head == null)
            {
                return;
            }

            _boss = bossPosition;
            EnsureRig();
            _frame.color = tint;
            _label.color = tint;
            _rim.color = tint;
            _camera.enabled = true;
            _rim.enabled = true;
            _showing = true;
            _panel.gameObject.SetActive(true);
            Follow();
        }

        public void Hide() => _showing = false;

        private void EnsureRig()
        {
            if (_camera != null)
            {
                return;
            }

            var rig = new GameObject("BossCutInCamera");
            _camera = rig.AddComponent<Camera>();
            _camera.fieldOfView = CameraFieldOfView;
            _camera.nearClipPlane = CameraNear;
            _camera.farClipPlane = CameraFar;
            _camera.targetTexture = _texture;
            _camera.depth = -10f;
            _camera.enabled = false;
            var light = new GameObject("BossCutInRim");
            light.transform.SetParent(rig.transform, false);
            _rim = light.AddComponent<Light>();
            _rim.type = LightType.Point;
            _rim.range = RimRange;
            _rim.intensity = RimIntensity;
            _rim.shadows = LightShadows.None;
            _rim.enabled = false;
        }

        private void LateUpdate()
        {
            float target = _showing ? 1f : 0f;
            if (Mathf.Approximately(_slide, target))
            {
                return;
            }

            _slide = Mathf.MoveTowards(_slide, target, Time.unscaledDeltaTime / SlideSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, _slide);
            _panel.anchoredPosition = new Vector2(-EdgeGap + (1f - eased) * (PanelSize + EdgeGap * 2f), RaiseFromMiddle);
            if (_slide <= 0f)
            {
                _panel.gameObject.SetActive(false);
                if (_camera != null)
                {
                    _camera.enabled = false;
                    _rim.enabled = false;
                }

                return;
            }

            Follow();
        }

        // In front of the visor, looking back at it, the rim light on the boss's side of the head.
        private void Follow()
        {
            if (_head == null || _camera == null)
            {
                return;
            }

            Vector3 forward = _hero.FigureForward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Vector3 head = _head.position;
            _camera.transform.position = head + forward * CameraDistance + Vector3.up * CameraLift;
            _camera.transform.LookAt(head + Vector3.up * CameraLift);
            Vector3 toBoss = _boss - head;
            toBoss.y = 0f;
            toBoss = toBoss.sqrMagnitude > 0.0001f ? toBoss.normalized : forward;
            _rim.transform.position = head + toBoss * RimDistance + Vector3.up * RimLift;
        }

        private void OnDestroy()
        {
            if (_camera != null)
            {
                Destroy(_camera.gameObject);
            }

            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
