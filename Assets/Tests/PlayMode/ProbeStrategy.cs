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

        /// <summary>The affordable buyer who first buys the counter tower when the field or the HUD's next-wave preview shows armour or flyers (the sensible rung). No model.</summary>
        Sensible = 5,
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

        /// <summary>Every owned gun except the ones made for the sky (round 61: what the flyers do to a player who skips air defence).</summary>
        public static readonly ProbeStrategy NoSkyGun = new ProbeStrategy("no sky gun", t => t.Targets != TargetLayers.Air);

        /// <summary>The ledger-less buyer of whatever is dearest and affordable.</summary>
        public static readonly ProbeStrategy AffordableBot = new ProbeStrategy("affordable", style: PlayStyle.Affordable);

        private ProbeStrategy(string name, System.Predicate<TowerDefinition> allowed = null, bool roleSpots = false, bool creative = false, bool naive = false, PlayStyle style = PlayStyle.Ledger, PlanSpec plan = null, StrategySpec spec = null)
        {
            Spec = spec ?? StrategySpec.Creative(string.Empty, 0);
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

        /// <summary>The strategy the creative bot executes (round 22e): the reference point for the plain creative bot, a searched one for the replay.</summary>
        public StrategySpec Spec { get; }

        /// <summary>As <see cref="NoSkyGun"/>, but when flyers are over the base it builds the guns it has beside it (the player who patches).</summary>
        public static readonly ProbeStrategy NoSkyGunGuardsBase = new ProbeStrategy("no sky, guards", t => t.Targets != TargetLayers.Air) { GuardsBase = true };

        /// <summary>Whether the bot puts air-capable guns beside the base while flyers besiege it.</summary>
        public bool GuardsBase { get; private set; }

        /// <summary>Sky guns the ledger bot stands before the first raid whatever its own judgement of the raid says (0: only when it judges them needed).</summary>
        public int MinSkyGuns { get; private set; }

        /// <summary>True for the obvious family: it reads no balance model.</summary>
        public bool Obvious => Style == PlayStyle.MonoRow || Style == PlayStyle.NearGate || Style == PlayStyle.Affordable || Style == PlayStyle.Sensible;

        /// <summary>The ledger bot with this many sky guns standing before the first raid (round 61: the player who does buy air defence).</summary>
        public static ProbeStrategy WithSkyGuns(int count) => new ProbeStrategy("sky guns " + count) { MinSkyGuns = count };

        /// <summary>One tower type only, placed by its role; hero on, no drones, gadgets or selling (wrecks are scrapped).</summary>
        public static ProbeStrategy Mono(string towerId) => new ProbeStrategy("mono " + towerId, t => t.Id == towerId, roleSpots: true, style: PlayStyle.MonoRow);

        /// <summary>Every owned tower placed by its role, plus drones, mines, a slow field and sell-and-rebuy (the ledger-driven reference player).</summary>
        public static ProbeStrategy CreativeBot() => new ProbeStrategy("creative", roleSpots: true, creative: true);

        /// <summary>The creative bot executing a searched strategy (the tester's stage B for strategies): its spots, mix, triggers and timing.</summary>
        public static ProbeStrategy Adaptive(StrategySpec spec) => new ProbeStrategy("strategy " + spec.Name, roleSpots: true, creative: true, spec: spec);

        /// <summary>The careless player: the owned types in turn on the first free spots nearest the gate, random upgrades, no gadgets or drones.</summary>
        public static ProbeStrategy NaiveBot() => new ProbeStrategy("naive", naive: true, style: PlayStyle.NearGate);

        /// <summary>
        /// The sensible player of the headroom ladder (Docs/BALANCE_HEADROOM.md): the affordable buyer on role spots, who stands a
        /// counter tower (by the role table of the combat rules) before anything else once the field or the HUD's next-wave
        /// preview shows armour or flyers (the preview since bot version 3; the rules are at BalanceProbeTests.SensibleAct).
        /// </summary>
        public static ProbeStrategy SensibleBot() => new ProbeStrategy("sensible", roleSpots: true, style: PlayStyle.Sensible);

        /// <summary>Replays a plan; placed exactly where the plan says.</summary>
        public static ProbeStrategy Scripted(PlanSpec plan) => new ProbeStrategy("plan " + plan.Name, style: PlayStyle.Scripted, plan: plan);

        /// <summary>
        /// A scripted plan played by someone who sees the field (round 74): the plan's steps as written, but a marked rift
        /// behind the defence gate gets its gun first, one wave ahead, as the sensible player does it. A plan recorded before a
        /// level's rift was authored is otherwise blind to it and loses to one creature. Same name, so the same seeded draws.
        /// </summary>
        public static ProbeStrategy ScriptedGuardingRifts(PlanSpec plan) => new ProbeStrategy("plan " + plan.Name, style: PlayStyle.Scripted, plan: plan) { GuardsRearRift = true };

        /// <summary>
        /// A searched strategy played adaptively by someone who knows the level (the skilled rung since round 76): as
        /// <see cref="Adaptive"/>, and a rift behind the defence gate gets its gun a wave ahead; since bot version 3 flyers biting
        /// the base out of reach of every sky gun get one beside it (BalanceProbeTests.CoverBaseSky). Same name, so the same seeded draws.
        /// </summary>
        public static ProbeStrategy AdaptiveGuardingRifts(StrategySpec spec) => new ProbeStrategy("strategy " + spec.Name, roleSpots: true, creative: true, spec: spec) { GuardsRearRift = true };

        /// <summary>Whether a scripted or adaptive player reacts to a rift behind the defence gate before going on with its plan.</summary>
        public bool GuardsRearRift { get; private set; }
    }
}
