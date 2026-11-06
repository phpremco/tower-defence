using Game.Core;
using Game.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The comic cut-in of a boss intro (Docs/BOSS_INTROS.md): a tilted panel in the Blender comic frame (ink border,
    /// cyan inlay, corner tab) slides in at the right edge of the screen with the hero's visor seen from the front, lit
    /// at the rim in the boss's colour, and the boss itself faintly mirrored in the curved visor glass. The picture comes
    /// from a small camera that rides in front of the head bone; the reflection from a second, tinier camera that looks
    /// from the visor at the boss. Both render only while the panel shows. The visor covers his face, so his face is
    /// never shown. Look only.
    /// </summary>
    public sealed class BossCutIn : MonoBehaviour
    {
        private const string HeadBone = "Head";
        private const string FrameSprite = "ComicFrame";
        private const string ReflectionShader = "Game/UI/VisorReflection";
        private const float PanelSize = 300f;
        // The picture sits inside the frame's ink band; the frame's 9-slice border is drawn at half its texture size.
        private const float FrameWidth = 22f;
        private const float FrameSlice = 2f;
        private const float TiltDegrees = -4f;
        private const float EdgeGap = 36f;
        private const float RaiseFromMiddle = 70f;
        private const float SlideSeconds = 0.25f;
        private const int TextureSize = 384;
        private const int LowTextureSize = 256;
        private const int ReflectionSize = 192;
        private const int LowReflectionSize = 128;
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

        // The visor glass relative to the head bone (metres, the figure's frame) and its half size, measured on the
        // Mercer export: the reflection is laid over exactly that part of the picture.
        private const float VisorLift = 0.075f;
        private const float VisorForward = 0.1f;
        private const float VisorHalfWidth = 0.13f;
        private const float VisorHalfHeight = 0.04f;
        private const float ReflectionFieldOfView = 38f;
        private const float BossAimHeight = 1.8f;
        private const float ReflectionStrength = 0.9f;
        private const float ReflectionTintLift = 0.5f;
        private const float MirrorStandOff = 0.25f;

        private static readonly Color Backing = new Color(0.03f, 0.05f, 0.09f, 1f);
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");

        private RectTransform _panel;
        private Image _frame;
        private bool _drawnFrame;
        private Text _label;
        private RawImage _picture;
        private RawImage _reflection;
        private Material _reflectionMaterial;
        private Camera _camera;
        private Camera _mirror;
        private Light _rim;
        private RenderTexture _texture;
        private RenderTexture _mirrorTexture;
        private Transform _head;
        private HeroArtView _hero;
        private Transform _boss;
        private float _slide;
        private bool _showing;

        /// <summary>True while the panel is on screen (sliding included).</summary>
        public bool IsShowing => _slide > 0f;

        /// <summary>True when the boss shows in the visor glass (the reflection camera renders).</summary>
        public bool Reflecting => _mirror != null && _mirror.enabled && _reflection != null && _reflection.enabled;

        /// <summary>The panel, for the HUD keep-out.</summary>
        public RectTransform Panel => _panel;

        public void Initialize(RectTransform safe)
        {
            Image backing = UiKit.CreatePanel(safe, "BossCutIn", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero, Backing);
            _panel = backing.rectTransform;
            _panel.pivot = new Vector2(1f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelSize, PanelSize);
            _panel.localRotation = Quaternion.Euler(0f, 0f, TiltDegrees);
            int size = ModelQuality.IsLow ? LowTextureSize : TextureSize;
            _texture = new RenderTexture(size, size, 24) { name = "BossCutIn" };
            _picture = Raw("Visor", _texture);
            Inset(_picture.rectTransform, FrameWidth);
            BuildReflection();
            BuildFrame(backing);
            _label = UiKit.CreateFixedText(_panel, Label, LabelSize, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(FrameWidth + 8f, FrameWidth + 4f), new Vector2(PanelSize * 0.6f, 32f));
            _panel.gameObject.SetActive(false);
        }

        /// <summary>Slides the panel in with the hero's visor, lit from the boss's side in its tint, the boss in the glass.</summary>
        public void Show(HeroArtView hero, Transform boss, Color tint)
        {
            _hero = hero;
            _head = hero != null ? hero.Bone(HeadBone) : null;
            if (_head == null || boss == null)
            {
                return;
            }

            _boss = boss;
            EnsureRig();
            if (!_drawnFrame)
            {
                // Without the Blender frame the plain panel wears the boss's colour as before.
                _frame.color = tint;
            }

            _label.color = tint;
            _rim.color = tint;
            if (_reflectionMaterial != null)
            {
                _reflectionMaterial.SetColor(TintId, Color.Lerp(tint, Color.white, ReflectionTintLift));
                _reflectionMaterial.SetFloat(StrengthId, ReflectionStrength);
            }

            _camera.enabled = true;
            _mirror.enabled = _reflection != null;
            _rim.enabled = true;
            _showing = true;
            _panel.gameObject.SetActive(true);
            Follow();
        }

        public void Hide() => _showing = false;

        private RawImage Raw(string name, Texture texture)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(_panel, false);
            image.raycastTarget = false;
            image.texture = texture;
            return image;
        }

        private static void Inset(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        // The mirrored boss: an oval over the visor, added onto the picture by its own shader. Placed every frame.
        private void BuildReflection()
        {
            Shader shader = Shader.Find(ReflectionShader);
            if (shader == null)
            {
                return;
            }

            int size = ModelQuality.IsLow ? LowReflectionSize : ReflectionSize;
            _mirrorTexture = new RenderTexture(size, size, 16) { name = "BossCutInReflection" };
            _reflectionMaterial = new Material(shader) { name = "VisorReflection" };
            _reflection = Raw("VisorReflection", _mirrorTexture);
            _reflection.material = _reflectionMaterial;
            RectTransform rect = _reflection.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        }

        // The Blender comic frame over the picture's edge; without it the backing panel takes the boss's colour.
        private void BuildFrame(Image backing)
        {
            UiArtSet art = UiArtSet.Active;
            Sprite sprite = art != null ? art.Sprite(FrameSprite) : null;
            _drawnFrame = sprite != null;
            if (!_drawnFrame)
            {
                _frame = backing;
                return;
            }

            _frame = new GameObject("ComicFrame", typeof(RectTransform)).AddComponent<Image>();
            _frame.transform.SetParent(_panel, false);
            _frame.raycastTarget = false;
            _frame.sprite = sprite;
            _frame.type = Image.Type.Sliced;
            _frame.pixelsPerUnitMultiplier = FrameSlice;
            Inset(_frame.rectTransform, 0f);
        }

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
            var eye = new GameObject("BossCutInReflection");
            eye.transform.SetParent(rig.transform, false);
            _mirror = eye.AddComponent<Camera>();
            _mirror.fieldOfView = ReflectionFieldOfView;
            _mirror.nearClipPlane = CameraNear;
            _mirror.farClipPlane = CameraFar;
            _mirror.targetTexture = _mirrorTexture;
            _mirror.depth = -11f;
            _mirror.enabled = false;
        }

        private void LateUpdate()
        {
            float target = _showing ? 1f : 0f;
            if (Mathf.Approximately(_slide, target))
            {
                if (_showing)
                {
                    Follow();
                }

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
                    _mirror.enabled = false;
                    _rim.enabled = false;
                }

                return;
            }

            Follow();
        }

        // In front of the visor, looking back at it, the rim light on the boss's side of the head; the reflection camera
        // at the visor looking at the boss, its picture laid over where the visor shows in the cut-in.
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
            Vector3 bossAt = _boss != null ? _boss.position : head + forward;
            Vector3 toBoss = bossAt - head;
            toBoss.y = 0f;
            toBoss = toBoss.sqrMagnitude > 0.0001f ? toBoss.normalized : forward;
            _rim.transform.position = head + toBoss * RimDistance + Vector3.up * RimLift;
            FollowReflection(head, forward, bossAt);
        }

        private void FollowReflection(Vector3 head, Vector3 forward, Vector3 bossAt)
        {
            if (_reflection == null || _mirror == null)
            {
                return;
            }

            Vector3 visor = head + Vector3.up * VisorLift + forward * VisorForward;
            Vector3 aim = bossAt + Vector3.up * BossAimHeight;
            // A little out from the glass towards the boss, so the helmet itself never fills the reflection.
            _mirror.transform.position = visor + (aim - visor).normalized * MirrorStandOff;
            _mirror.transform.LookAt(aim);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 centre = _camera.WorldToViewportPoint(visor);
            Vector3 side = _camera.WorldToViewportPoint(visor + right * VisorHalfWidth);
            Vector3 top = _camera.WorldToViewportPoint(visor + Vector3.up * VisorHalfHeight);
            Rect window = _picture.rectTransform.rect;
            RectTransform rect = _reflection.rectTransform;
            rect.anchoredPosition = new Vector2((centre.x - 0.5f) * window.width, (centre.y - 0.5f) * window.height);
            rect.sizeDelta = new Vector2(Mathf.Abs(side.x - centre.x) * 2f * window.width, Mathf.Abs(top.y - centre.y) * 2f * window.height);
        }

        private void OnDestroy()
        {
            if (_camera != null)
            {
                Destroy(_camera.gameObject);
            }

            Release(_texture);
            Release(_mirrorTexture);
            if (_reflectionMaterial != null)
            {
                Destroy(_reflectionMaterial);
            }
        }

        private static void Release(RenderTexture texture)
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
