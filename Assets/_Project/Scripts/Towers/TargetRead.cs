using Game.Core;

namespace Game.Towers
{
    /// <summary>What a tower's card says about ground and sky (<see cref="TargetRead"/>).</summary>
    public enum TargetReadKind
    {
        /// <summary>Nothing to say: a support tower, or one that does no damage of its own.</summary>
        None,
        GroundOnly,
        AirOnly,
        GroundAndAir,
        /// <summary>Aims at both, but only a small share of its damage lands on flyers.</summary>
        AirWeak,
        /// <summary>Aims at both, but only a small share of its damage lands on walkers.</summary>
        GroundWeak,
    }

    /// <summary>
    /// How well a tower does against ground and sky, read from its data (target layers and the damage share per layer), in the
    /// few words the tower bar and the tower menu show. Round 62: the Gun Turret aims up but lands a fifth of its damage there,
    /// and nothing on screen said so; the player saw two equal badges and built a row of them against a raid.
    /// </summary>
    public static class TargetRead
    {
        /// <summary>A layer that takes this share of the damage or less reads as "weak" (the Gun Turret's 0.2 up, and the 0.5 it had).</summary>
        public const float WeakShare = 0.5f;

        public static TargetReadKind Of(TowerDefinition tower)
        {
            if (tower == null || tower.IsSupport || tower.MaxLevelIndex < 0 || tower.GetLevel(0).Damage <= 0f)
            {
                return TargetReadKind.None;
            }

            bool ground = (tower.Targets & TargetLayers.Ground) != 0;
            bool air = (tower.Targets & TargetLayers.Air) != 0;
            if (ground && air)
            {
                bool airWeak = tower.AirDamageFactor <= WeakShare;
                bool groundWeak = tower.GroundDamageFactor <= WeakShare;
                // Weak on both sides is simply a weak gun, not a lopsided one.
                return airWeak == groundWeak ? TargetReadKind.GroundAndAir : airWeak ? TargetReadKind.AirWeak : TargetReadKind.GroundWeak;
            }

            return air ? TargetReadKind.AirOnly : ground ? TargetReadKind.GroundOnly : TargetReadKind.None;
        }

        /// <summary>The words for a card or an info line; empty when there is nothing to say.</summary>
        public static string Label(TowerDefinition tower) => Label(Of(tower));

        public static string Label(TargetReadKind kind)
        {
            switch (kind)
            {
                case TargetReadKind.GroundOnly: return "Ground only";
                case TargetReadKind.AirOnly: return "Air only";
                case TargetReadKind.GroundAndAir: return "Ground and air";
                case TargetReadKind.AirWeak: return "Air: weak";
                case TargetReadKind.GroundWeak: return "Ground: weak";
                default: return string.Empty;
            }
        }
    }
}
