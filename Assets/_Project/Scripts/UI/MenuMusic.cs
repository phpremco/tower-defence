using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// The light rock loop under the menus. Created by the first menu screen, it survives the switches between
    /// main menu, map and base, follows the music volume in the mix, and fades itself out when a level or the Boot
    /// scene loads so the level's own score takes over without a gap or an overlap.
    /// </summary>
    public sealed class MenuMusic : MonoBehaviour
    {
        private const float Volume = 0.45f;
        private const float FadeInSeconds = 2.5f;
        private const float FadeOutSeconds = 0.9f;

        private AudioMix _mix;
        private AudioSource _source;
        private float _gain = 1f;
        private float _level;
        private bool _leaving;

        /// <summary>Starts the menu music unless it is already playing.</summary>
        public static void Ensure(AudioMix mix)
        {
            if (FindAnyObjectByType<MenuMusic>() != null)
            {
                return;
            }

            var go = new GameObject("MenuMusic");
            DontDestroyOnLoad(go);
            go.AddComponent<MenuMusic>().Initialize(mix);
        }

        private void Initialize(AudioMix mix)
        {
            _mix = mix;
            _source = gameObject.AddComponent<AudioSource>();
            // The recorded menu loop when the library has one, at its measured gain; the generated theme otherwise.
            Services.TryGet(out SfxLibrary library);
            bool recorded = library != null && !library.MenuMusic.IsEmpty;
            _source.clip = recorded ? library.MenuMusic[0] : ProceduralMusic.Menu();
            _gain = 1f;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            _source.Play();
            _mix.Changed += Apply;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_mix != null)
            {
                _mix.Changed -= Apply;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneNames.Level || scene.name == SceneNames.Boot)
            {
                _leaving = true;
            }
        }

        private void Update()
        {
            float target = _leaving ? 0f : 1f;
            float seconds = _leaving ? FadeOutSeconds : FadeInSeconds;
            _level = Mathf.MoveTowards(_level, target, Time.unscaledDeltaTime / seconds);
            Apply();
            if (_leaving && _level <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void Apply()
        {
            _source.volume = Volume * _gain * _level * _mix.MusicGain;
        }
    }
}
