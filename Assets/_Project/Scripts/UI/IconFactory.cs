using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public enum IconType
    {
        Upgrade,
        Repair,
        Sell,
        Speed,
        SpeedDouble,
        SpeedTriple,
        Pause,
        SkipWave,
        Star,
        Check,
        Hero,
        Shockwave,
        Combine,
        Ground,
        Air,
        Slow,
        Arrow,
        Disc,
        Ring,
        Beacon,
        Blast,
        Roll,
        Net,
        Trap,
        Patrol,

        /// <summary>An open book: the codex.</summary>
        Book,

        /// <summary>A flat-topped hexagon plate: a region on the planet map.</summary>
        Hex,

        /// <summary>A padlock: a region that is not open yet.</summary>
        Lock,

        // README round 25: the extras of the Blender set, by the same names as their sprites.
        StarEmpty,
        BossMarker,
        EnemyOrigin,
        Drone,
        Base,
        Gear,
        Close,
        Research,
        Power,
        Enemy,
        Credits,
        Info,
    }

    /// <summary>
    /// The HUD icons painted in Blender (ArtSource README round 25, <c>UI_Icons</c>): each <see cref="IconType"/> is the
    /// sprite of the same name in the <see cref="UiArtSet"/>. White glyphs are tinted by the image colour; BossMarker
    /// and EnemyOrigin come pre-coloured. Null without the set (tests that build screens on their own).
    /// </summary>
    public static class IconFactory
    {
        private static readonly Dictionary<IconType, Sprite> Cache = new Dictionary<IconType, Sprite>();
        private static UiArtSet _cachedFor;

        public static Sprite Get(IconType type)
        {
            UiArtSet art = UiArtSet.Active;
            if (art != _cachedFor)
            {
                Cache.Clear();
                _cachedFor = art;
            }

            if (Cache.TryGetValue(type, out Sprite sprite))
            {
                return sprite;
            }

            sprite = art != null ? art.Sprite(type.ToString()) : null;
            Cache[type] = sprite;
            return sprite;
        }
    }
}
