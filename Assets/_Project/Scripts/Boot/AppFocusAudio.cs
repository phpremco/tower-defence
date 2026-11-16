using Game.Core;
using Game.Economy;
using UnityEngine;

namespace Game.Boot
{
    /// <summary>
    /// Silences the game while its window is in the background, when the player's "Mute in background" option is on
    /// (the default). The build runs in the background, so without this the music plays on behind other windows. Also
    /// writes any unsaved settings when the game quits. Lives on a persistent object made by the bootstrapper.
    /// </summary>
    public sealed class AppFocusAudio : MonoBehaviour
    {
        private bool _muted;

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!Services.TryGet(out SettingsService settings) || !Services.TryGet(out AudioMix mix))
            {
                return;
            }

            bool mute = !hasFocus && settings.Data.MuteInBackground;
            if (mute == _muted)
            {
                return;
            }

            _muted = mute;
            // The listener carries the master level (see Bootstrapper), so silence and its return go through it.
            AudioListener.volume = mute ? 0f : mix.Master;
        }

        private void OnApplicationQuit()
        {
            if (Services.TryGet(out SettingsService settings))
            {
                settings.Flush();
            }
        }
    }
}
