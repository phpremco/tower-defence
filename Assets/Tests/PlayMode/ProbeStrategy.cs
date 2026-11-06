using Game.Balance;
using Game.Core;
using Game.Towers;

namespace Game.Tests
{
    /// <summary>How a probe bot decides what to buy.</summary>
    internal enum PlayStyle
    {
        /// <summary>The ledger-driven bot of BalanceProbeTests (greedy, gun only, creative): judges its towers with the balance model.</summary>
        Ledger = 0,

        /// <summary>A row of one tower type on its role spots; builds while it can, then alternates upgrades and towers. No model.</summary>
        MonoRow = 1,

        /// <summary>A careless mix: the owned types in turn on the first free spots nearest the gate, a random upgrade now and then. No model.</summary>
        NearGate = 2,

        /// <summary>Buys the dearest step it can afford, whatever it is, on coverage spots. No model.</summary>
        Affordable = 3,

        /// <summary>Replays a written plan step by step (PlanReplayTests); decides nothing itself.</summary>
        Scripted = 4,
    }

    /// <summary>
    /// How the probe bot plays (round 20, extended in round 22): which owned towers it may build, whether it places each type
    /// by its role, whether it plays creatively (drones, mines, a slow field, selling a ground gun for a sky gun), and its
    /// <see cref="PlayStyle"/>. The obvious family (mono rows, the near-gate mix, the affordable buyer) reads no balance model, so
    /// it never moves between rounds; the ledger bots are reference players; a scripted plan is the tester's stage B.
    /// </summary>
    internal sealed class ProbeStrategy
    {
        /// <summary>The bot of BalanceProbeTests: every owned tower, coverage placement, no gadgets.</summary>
        public static readonly ProbeStrategy Greedy = new ProbeStrategy("greedy");

        /// <summary>Only the guns that hit both ground and air (the naive air check).</summary>
        public static readonly ProbeStrategy GunOnly = new ProbeStrategy("gun only", t => t.Targets == (TargetLayers.Ground | TargetLayers.Air));

        /// <summary>The ledger-less buyer of whatever is dearest and affordable.</summary>
        public static readonly ProbeStrategy AffordableBot = new ProbeStrategy("affordable", style: PlayStyle.Affordable);

        private ProbeStrategy(string name, System.Predicate<TowerDefinition> allowed = null, bool roleSpots = false, bool creative = false, bool naive = false, PlayStyle style = PlayStyle.Ledger, PlanSpec plan = null)
        {
            Name = name;
            Allowed = allowed;
            RoleSpots = roleSpots;
            Creative = creative;
            Naive = naive;
            Style = style;
            Plan = plan;
        }

        public string Name { get; }

        /// <summary>The owned towers the bot may build; null for all of them.</summary>
        public System.Predicate<TowerDefinition> Allowed { get; }

        public bool RoleSpots { get; }

        public bool Creative { get; }

        /// <summary>Spots in the careless order (nearest the gate, overlap ignored) and gadgets anywhere.</summary>
        public bool Naive { get; }

        public PlayStyle Style { get; }

        public PlanSpec Plan { get; }

        /// <summary>True for the obvious family: it reads no balance model.</summary>
        public bool Obvious => Style == PlayStyle.MonoRow || Style == PlayStyle.NearGate || Style == PlayStyle.Affordable;

        /// <summary>One tower type only, placed by its role; hero on, no drones, gadgets or selling (wrecks are scrapped).</summary>
        public static ProbeStrategy Mono(string towerId) => new ProbeStrategy("mono " + towerId, t => t.Id == towerId, roleSpots: true, style: PlayStyle.MonoRow);

        /// <summary>Every owned tower placed by its role, plus drones, mines, a slow field and sell-and-rebuy (the ledger-driven reference player).</summary>
        public static ProbeStrategy CreativeBot() => new ProbeStrategy("creative", roleSpots: true, creative: true);

        /// <summary>The careless player: the owned types in turn on the first free spots nearest the gate, random upgrades, no gadgets or drones.</summary>
        public static ProbeStrategy NaiveBot() => new ProbeStrategy("naive", naive: true, style: PlayStyle.NearGate);

        /// <summary>Replays a plan; placed exactly where the plan says.</summary>
        public static ProbeStrategy Scripted(PlanSpec plan) => new ProbeStrategy("plan " + plan.Name, style: PlayStyle.Scripted, plan: plan);
    }
}
