using Game.Core;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using Game.Platform;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Dr. Voss explains the upgrade screen the first time it is opened in a save (Remco, round 66): research points, the
    /// tiers, the continents that open them and Spencer's gear, in the level's own dialogue box. While he speaks the screen
    /// underneath takes no input, so Confirm reads on and never buys; Cancel skips him. Seen once it ends or is skipped
    /// (the save's story flags), so it never plays again.
    /// </summary>
    public sealed class UpgradeScreenGuide : MonoBehaviour
    {
        private const int CanvasOrder = 20;

        // The box sits low on the screen, clear of the tiles and the chosen subject's numbers it talks about.
        private const float BoxBottom = 36f;

        private DialoguePlayer _player;
        private DialogueInput _input;
        private CanvasGroup _screen;
        private int _endedFrame = -1;

        /// <summary>True while Voss speaks, and for the frame he stops in: the screen leaves that frame's Cancel to him.</summary>
        public bool OwnsInput => (_player != null && _player.IsBusy) || _endedFrame == Time.frameCount;

        /// <summary>The explanation still to be shown to this save, or null: none in the story, or seen already.</summary>
        public static DialogueDefinition Pending(CampaignCatalog campaign, ProgressService progress)
        {
            DialogueDefinition tip = campaign != null && campaign.Story != null ? campaign.Story.UpgradeScreenTip : null;
            return tip != null && !progress.HasSeenStory(tip.Id) ? tip : null;
        }

        /// <summary>Starts the explanation over the screen; <paramref name="screen"/> is the screen's own root, frozen while it plays.</summary>
        public void Play(DialogueDefinition dialogue, ProgressService progress, RectTransform screen, HeroDefinition hero)
        {
            _screen = screen.gameObject.GetComponent<CanvasGroup>();
            if (_screen == null)
            {
                _screen = screen.gameObject.AddComponent<CanvasGroup>();
            }

            _player = new DialoguePlayer(progress.MarkStorySeen);
            _player.BusyChanged += OnBusyChanged;
            RectTransform safe = UiKit.CreateRoot(transform, false);
            safe.parent.GetComponent<Canvas>().sortingOrder = CanvasOrder;
            var view = gameObject.AddComponent<DialogueView>();
            view.Initialize(safe, _player, new PortraitProvider(hero));
            view.SetPlacement(BoxBottom, 1f);
            _player.Presenter = view;
            if (Services.TryGet(out IInputProvider input))
            {
                _input = new DialogueInput(_player, input, () => false);
            }

            _player.Enqueue(dialogue);
            _player.TryStart();
        }

        private void Update()
        {
            _player?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            _input?.Dispose();
            if (_player != null)
            {
                _player.BusyChanged -= OnBusyChanged;
            }
        }

        // The screen underneath is frozen while he speaks and handed back, focus and all, when he is done.
        private void OnBusyChanged()
        {
            bool busy = _player.IsBusy;
            _screen.interactable = !busy;
            _screen.blocksRaycasts = !busy;
            if (!busy)
            {
                _endedFrame = Time.frameCount;
            }
        }
    }
}
