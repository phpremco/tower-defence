using Game.Core;
using Game.Towers;

namespace Game.Tests
{
    /// <summary>
    /// How the probe bot plays (round 20, monoculture check): which owned towers it may build, whether it places each type
    /// by its role, whether it plays creatively (drones, mines, a slow field, selling a ground gun for a sky gun) and whether
    /// it plays naively (seeded random build spots, upgrades without thought). The plain greedy bot is unchanged.
    /// </summary>
    internal sealed class ProbeStrategy
    {
        /// <summary>The bot of BalanceProbeTests: every owned tower, coverage placement, no gadgets.</summary>
        public static readonly ProbeStrategy Greedy = new ProbeStrategy("greedy");

        /// <summary>Only the guns that hit both ground and air (the naive air check).</summary>
        public static readonly ProbeStrategy GunOnly = new ProbeStrategy("gun only", t => t.Targets == (TargetLayers.Ground | TargetLayers.Air));

        private ProbeStrategy(string name, System.Predicate<TowerDefinition> allowed = null, bool roleSpots = false, bool creative = false, bool naive = false)
        {
            Name = name;
            Allowed = allowed;
            RoleSpots = roleSpots;
            Creative = creative;
            Naive = naive;
        }

        public string Name { get; }

        /// <summary>The owned towers the bot may build; null for all of them.</summary>
        public System.Predicate<TowerDefinition> Allowed { get; }

        public bool RoleSpots { get; }

        public bool Creative { get; }

        public bool Naive { get; }

        /// <summary>One tower type only, placed by its role; hero on, no drones, gadgets or selling (wrecks are scrapped).</summary>
        public static ProbeStrategy Mono(string towerId) => new ProbeStrategy("mono " + towerId, t => t.Id == towerId, roleSpots: true);

        /// <summary>Every owned tower placed by its role, plus drones, mines, a slow field and sell-and-rebuy.</summary>
        public static ProbeStrategy CreativeBot() => new ProbeStrategy("creative", roleSpots: true, creative: true);

        /// <summary>The creative bot's purse and choices, but seeded random spots and thoughtless upgrades.</summary>
        public static ProbeStrategy NaiveBot() => new ProbeStrategy("naive", creative: true, naive: true);
    }
}
