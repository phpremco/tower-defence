using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The interface art painted in Blender (ArtSource README round 25): the icon set, the nine-slice kit (panels, cards,
    /// buttons, bars, slider handle, tabs, focus frame), the dialogue and radar pieces, and the full pictures (menu sky,
    /// preview backdrop, map halo, radar scope, dialogue tiles). Written by Tools/Generate/Blender UI; screens look
    /// sprites up by their name in the export (an icon by its <see cref="IconType"/> name).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/UI Art Set", fileName = "UiArtSet")]
    public sealed class UiArtSet : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public string Name;
            public Sprite Sprite;
        }

        [SerializeField] private Entry[] _sprites = new Entry[0];
        [Header("Pictures")]
        [SerializeField] private Texture2D _starfield;
        [SerializeField] private Texture2D _previewBackdrop;
        [SerializeField] private Texture2D _mapHalo;
        [SerializeField] private Texture2D _radar;
        [Tooltip("Radar scope radius in the picture's pixels (README: 471 of 1024).")]
        [SerializeField] private float _radarScopeShare = 471f / 1024f;
        [Header("Dialogue tiles")]
        [SerializeField] private Texture2D _dialogueNoise;
        [SerializeField] private Texture2D _dialogueScanlines;
        [SerializeField] private Texture2D _dialogueRadial;
        [SerializeField] private Texture2D _dialogueShade;

        private Dictionary<string, Sprite> _byName;

        public Texture2D Starfield => _starfield;
        public Texture2D PreviewBackdrop => _previewBackdrop;
        public Texture2D MapHalo => _mapHalo;
        public Texture2D Radar => _radar;
        public float RadarScopeShare => _radarScopeShare;
        public Texture2D DialogueNoise => _dialogueNoise;
        public Texture2D DialogueScanlines => _dialogueScanlines;
        public Texture2D DialogueRadial => _dialogueRadial;
        public Texture2D DialogueShade => _dialogueShade;

        /// <summary>The sprite of that name, or null when the set has none.</summary>
        public Sprite Sprite(string name)
        {
            if (_byName == null)
            {
                _byName = new Dictionary<string, Sprite>(_sprites.Length);
                foreach (Entry entry in _sprites)
                {
                    if (entry.Sprite != null && !string.IsNullOrEmpty(entry.Name))
                    {
                        _byName[entry.Name] = entry.Sprite;
                    }
                }
            }

            return _byName.TryGetValue(name, out Sprite sprite) ? sprite : null;
        }

        /// <summary>The registered set (through the UI skin), or null in tests and tools that run without it.</summary>
        public static UiArtSet Active => UiKit.Skin != null ? UiKit.Skin.Art : null;

        private void OnEnable() => _byName = null;

        private void OnValidate()
        {
            _byName = null;
            if (_sprites.Length == 0 || _starfield == null)
            {
                Debug.LogWarning($"UiArtSet '{name}' is empty; run Tools/Generate/Blender UI.", this);
            }
        }
    }
}
