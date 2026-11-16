using Game.Core;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// A banner at the top of the screen that says why the game paused when the gamepad in use drops out, and that the
    /// pad is back once it reconnects (<see cref="IInputProvider.DeviceRegained"/>). It lives for the whole session on
    /// a persistent object, so it covers menus, cutscenes and levels alike; the pause itself stays with the level.
    /// </summary>
    public sealed class ControllerNotice : MonoBehaviour
    {
        private const int SortingOrder = 32000;
        private const int FontSize = 34;
        private const float BannerTop = -24f;
        private const float BannerWidth = 1400f;
        private const float BannerHeight = 64f;
        private const float RegainedSeconds = 2.5f;

        private IInputProvider _input;
        private Text _label;
        private float _hideAt = float.PositiveInfinity;

        /// <summary>Hooks the banner to the input provider; call once after the provider is registered.</summary>
        public void Bind(IInputProvider input)
        {
            Unbind();
            _input = input;
            _input.DeviceLost += OnLost;
            _input.DeviceRegained += OnRegained;
        }

        private void Awake()
        {
            RectTransform root = UiKit.CreateRoot(transform, withStarfield: false);
            Canvas canvas = root.GetComponentInParent<Canvas>();
            canvas.sortingOrder = SortingOrder;
            _label = UiKit.CreateFixedText(root, string.Empty, FontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, BannerTop), new Vector2(BannerWidth, BannerHeight));
            _label.raycastTarget = false;
            _label.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        // Unscaled time: the level is paused (time scale 0) while the banner is up.
        private void Update()
        {
            if (Time.unscaledTime >= _hideAt)
            {
                _hideAt = float.PositiveInfinity;
                _label.gameObject.SetActive(false);
            }
        }

        private void OnLost()
        {
            Show(Loc.Get("system.controller_lost"), float.PositiveInfinity);
        }

        private void OnRegained()
        {
            Show(Loc.Get("system.controller_back"), Time.unscaledTime + RegainedSeconds);
        }

        private void Show(string text, float hideAt)
        {
            _label.text = text;
            _label.gameObject.SetActive(true);
            _hideAt = hideAt;
        }

        private void Unbind()
        {
            if (_input == null)
            {
                return;
            }

            _input.DeviceLost -= OnLost;
            _input.DeviceRegained -= OnRegained;
            _input = null;
        }
    }
}
