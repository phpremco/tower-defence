using Game.Core;
using Game.PathObjects;
using UnityEngine;

namespace Game.UI
{
    /// <summary>The icon and colour that stand for a street weapon's effect, the same on the HUD card and in the loadout.</summary>
    public static class PathObjectBadge
    {
        private static readonly Color SlowColor = new Color(0.75f, 1f, 1f);
        private static readonly Color TrapColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color BlastColor = new Color(1f, 0.6f, 0.3f);
        private static readonly Color RollColor = new Color(1f, 0.45f, 0.25f);
        private static readonly Color PatrolColor = new Color(0.5f, 0.95f, 1f);

        public static void For(PathObjectEffect effect, out IconType icon, out Color colour)
        {
            switch (effect)
            {
                case PathObjectEffect.Slow:
                    icon = IconType.Slow;
                    colour = SlowColor;
                    break;
                case PathObjectEffect.Blast:
                    icon = IconType.Blast;
                    colour = BlastColor;
                    break;
                case PathObjectEffect.Roll:
                    icon = IconType.Roll;
                    colour = RollColor;
                    break;
                case PathObjectEffect.Patrol:
                    icon = IconType.Patrol;
                    colour = PatrolColor;
                    break;
                default:
                    icon = IconType.Trap;
                    colour = TrapColor;
                    break;
            }
        }

        /// <summary>A short word for the effect, for the loadout screen.</summary>
        public static string Name(PathObjectEffect effect)
        {
            switch (effect)
            {
                case PathObjectEffect.Slow: return Loc.Get("gadget.effect.slow");
                case PathObjectEffect.Blast: return Loc.Get("gadget.effect.blast");
                case PathObjectEffect.Roll: return Loc.Get("gadget.effect.roll");
                case PathObjectEffect.Patrol: return Loc.Get("gadget.effect.patrol");
                default: return Loc.Get("gadget.effect.trap");
            }
        }
    }
}
