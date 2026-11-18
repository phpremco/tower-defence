namespace Game.Balance
{
    /// <summary>
    /// Every knob of the balance model in one place. The generator passes one instance to the solver and the ledger,
    /// the tests and the BalanceLab tool use the same one, so a retune is a change of one number here and a
    /// regeneration. Docs/BALANCE.md explains each knob and the order to turn them in.
    /// </summary>
    public sealed class BalanceRules
    {
        // ---- Threat and kill money -------------------------------------------------------------------------------

        /// <summary>One threat point is this much effective health (health plus armour).</summary>
        public float HealthPerThreat = 20f;
        public float AirThreatFactor = 1.2f;

        /// <summary>
        /// A point of armour is priced at this many points of health (round 24: armour is a pool a heavy gun strips point for
        /// point and light fire chips at a third of its damage, so it costs the player more than health does, and its kill
        /// money should pay for the heavier gun it asks for). 0.8 until round 24, when armour soaked a share of every hit.
        /// </summary>
        public float ArmorThreatShare = 1.25f;
        public float BaseDamageThreat = 0.04f;
        public float AttackDamageThreat = 0.03f;

        /// <summary>
        /// Kill money is <c>MoneyPerThreat * threat ^ RewardExponent</c>: tough creatures pay a little less per point. This
        /// is the economy's main lever: the purse buys towers that last, so the more a kill pays, the faster the defence
        /// outgrows the crowd and the tougher the solver must make late creatures. See Docs/BALANCE.md.
        /// </summary>
        public float MoneyPerThreat = 1.3f;
        public float RewardExponent = 0.9f;

        /// <summary>A boss pays this many times the ordinary rule: the rebuild after its powers has to be affordable.</summary>
        public float BossRewardFactor = 3f;

        // ---- How well the player's defence works -----------------------------------------------------------------

        /// <summary>
        /// Share of the window a tower really spends hurting something (target switching, misses, imperfect placement).
        /// Calibrated against the PlayMode bot: pressure 1 is where it stops holding. Retune after a probe run. 0.35 until
        /// 2026-11-03; the S4 probe (bot at the trunk gate, frame-rate-proof guns, the kill money of <see cref="KillPurse"/>)
        /// lost with the ledger at 0.7 to 0.85. After the model and bot fixes of that day it held Crysium Vein untouched at
        /// 0.27 and lost it at wave 10 (the prowler and roller rush) at 0.3 and 0.35: the bot holds or breaks there.
        /// </summary>
        public float Utilisation = 0.27f;

        /// <summary>Most creatures one splash hit can catch, however dense the stream, for a splash gun without its own cap (TowerStats.SplashTargets).</summary>
        public float MaxSplashTargets = 2.4f;

        /// <summary>
        /// An upgrade step must buy at least this many times the single-target DPS per coin of the level below (Remco: high risk,
        /// high reward; the critic of round 21: 3% was a wash). BalanceSheet prices the steps from it, TowerRolesTests asserts it.
        /// </summary>
        public float UpgradeMargin = 1.15f;

        /// <summary>Gun towers the player can place usefully per ground front (build spots in reach of the road).</summary>
        public int TowersPerFront = 16;

        /// <summary>
        /// The hero holds and hits what stops at a gate; counted at this share of the wave window. 0.5 until 2026-11-03: at
        /// the busiest gate the hero falls and is out for two minutes (four or five waves), so it fights less than half the time.
        /// </summary>
        public float HeroShare = 0.35f;

        /// <summary>
        /// Creatures within reach of the hero's shockwave when he fires it (round 24): the bot fires at this crowd or at a boss, and
        /// the ledger counts the pulse on that many creatures in a wave's window; at a siege it hits the attackers at the gate.
        /// </summary>
        public int ShockwaveCrowd = 4;

        /// <summary>
        /// Share of the ground health of earlier waves that reaches the defence together with a wave (<see cref="KillPurse.Carried"/>)
        /// that counts against it. Those creatures were also counted in their own wave, so half goes to each.
        /// </summary>
        public float CarriedShare = 0.5f;

        /// <summary>A gate holds walkers this long at most per wave, however strong it is.</summary>
        public float MaxGateHoldSeconds = 8f;

        /// <summary>Share of a gate's hold time the towers can use (only those in reach of the gate fire at the queue).</summary>
        public float GateWindowShare = 0.5f;

        /// <summary>Walkers that hammer a gate at the same time; the rest of the queue waits behind them.</summary>
        public int AttackersPerGate = 5;

        /// <summary>
        /// A lane whose shared stretch (the trunk to the base) is shorter than this share of its walk is a separate front;
        /// a nest or pool whose creatures join with less road than this ahead skips part of the defence.
        /// </summary>
        public float TrunkReference = 0.35f;

        /// <summary>Towers on a separate front are idle while the other front is busy; they count at this share.</summary>
        public float FrontEfficiency = 0.7f;

        /// <summary>Harvest drones bring in the mines' stock gradually, from this wave on, emptied by this share of the waves.</summary>
        public int MinesOpenFromWave = 3;
        public float MineDepletionShare = 0.6f;

        // ---- The target curve the wave solver aims at (Medium; other difficulties follow their profiles) -------------

        /// <summary>
        /// Pressure the solver aims plain Medium waves at, mid-level (bot calibration, hero included). One means the bot just
        /// holds. 0.6 until 2026-11-04; raised to 0.8 with the human target curve (<see cref="HumanSurplus"/>), so the solver's
        /// shape already sits near the level the calibration trims the waves to (Medium trims 0.97 to 1.15 on Crystal Plains).
        /// </summary>
        public float TargetPressure = 0.8f;

        /// <summary>The opening waves ramp up from this share of the target, over this many waves.</summary>
        public float OpeningShare = 0.45f;
        public int OpeningWaves = 4;

        /// <summary>The target rises within a level, so the late waves do not get easier as the purse snowballs.</summary>
        public float EarlyTargetShare = 0.85f;
        public float LateTargetShare = 1.2f;

        /// <summary>Target factors per wave kind, so the rhythm survives the solver.</summary>
        public float RushFactor = 1.1f;
        public float HeavyFactor = 1.15f;
        public float AirFactor = 0.9f;
        public float BossFactor = 1.4f;

        /// <summary>The wave after a boss is a breather.</summary>
        public float AfterBossFactor = 0.8f;

        /// <summary>Seconds before the first wave and between waves, as the generator writes them: the model's purse runs on the same clock.</summary>
        public float FirstWaveDelay = 10f;
        public float WaveDelay = 8f;

        /// <summary>The solver may change the budget from one wave to the next by at most this factor either way.</summary>
        public float MaxBudgetStep = 1.45f;

        /// <summary>Smallest budget of any wave, in threat points, so the first wave is never empty.</summary>
        public float MinBudget = 4f;

        /// <summary>
        /// Pressure of each difficulty as a share of Medium (Easy to Psycho). Used only for the waves a difficulty adds
        /// (extra air raids, extra bosses); everything else follows the difficulty profile, and the profiles are tuned so
        /// their waves land near these shares. { 0.7, 1, 1.5, 2.1 } until 2026-11-04; the human curve (Easy 1.4x, Medium 1.15x,
        /// Hard 0.95x, Psycho 0.8x) puts the bot-lens shares near 0.8 / 1 / 1.35 / 1.7.
        /// </summary>
        public float[] DifficultyPressure = { 0.8f, 1f, 1.35f, 1.7f };

        // ---- Checks the solver does not optimise -------------------------------------------------------------------

        /// <summary>
        /// A plain wave may reach the difficulty's share of the highest Medium target (late heavy waves) times this margin;
        /// bosses get the allowance on top.
        /// </summary>
        public float BandMargin = 1.4f;
        public float BossAllowance = 1.6f;

        /// <summary>
        /// Most of a boss fight's firepower a continent boss's mechanic may take away (Docs/BOSSES.md: no wipe-outs, an effect
        /// reaches about a quarter of the field): the ledger never counts more, and <see cref="BalanceChecks.CheckBossMechanic"/>
        /// fails a boss whose mechanic would.
        /// </summary>
        public float MaxBossMechanicShare = 0.35f;

        /// <summary>Most pressure a plain wave may reach on a difficulty (Easy to Psycho).</summary>
        public float MaxPressure(int difficulty) =>
            DifficultyPressure[System.Math.Min(difficulty, DifficultyPressure.Length - 1)] * TargetPressure * LateTargetShare * HeavyFactor * BandMargin;

        /// <summary>No wave may be more than this many times the heaviest of the few plain waves before it.</summary>
        public float MaxCliff = 2.6f;

        /// <summary>Late Easy waves must still ask something on average.</summary>
        public float MinLateEasyPressure = 0.15f;

        // ---- The human lens: a competent player instead of the bot ----------------------------------------------------

        /// <summary>
        /// <see cref="Utilisation"/> of a competent human. The bot rings the first trunk gate; a player puts towers in the
        /// bends and at the gates where one gun covers two stretches of road, and buys upgrades where they pay, so each gun
        /// spends about a quarter more of the window on something. Set 2026-11-04 on Remco's C1_01 Easy report ("two or three
        /// times the firepower the waves needed, the hero never had to act"): at 0.34 the model gives a fresh player 2.4
        /// times the firepower on the plain waves, and 3.6 times with the permanent upgrades in his save. The solver and the
        /// bands stay on the bot's <see cref="Utilisation"/>; this lens only judges the human checks (<see cref="BalanceChecks.CheckHuman"/>).
        /// </summary>
        public float HumanUtilisation = 0.34f;

        /// <summary>
        /// Most firepower over need, towers alone, a competent player may have on the plain Easy waves of a level. 2.1 until
        /// 2026-11-04; with the human curve Easy sits at 1.4x at a continent's first level.
        /// </summary>
        // 1.6 until 2026-11-09; 1.9 since Easy may be comfortable (Remco): C1_02 Easy sits at scale 0.757 because at 0.87 the searched
        // plans lost 3/3 in play (round 27) while the paper read 1.6x; the paper is lenient about that level's gate fights.
        // 2.4 since round 34b: the Easy scales are set by play (the pay-off rule: a reasonable plan wins, three stars allowed), and the
        // trim puts the paper at 1.4x over the scale (Shard Ridge 0.6 -> 2.3x); the band only catches a runaway scale now.
        public float MaxEasySurplus = 2.4f;

        /// <summary>
        /// The plain Easy waves of a level may ask at most this share of what the plain Medium waves ask of the same player,
        /// so Easy stays clearly the easier choice (about 0.8 on most levels).
        /// </summary>
        // 0.93 until 2026-11-09; 0.96 since Remco let Easy be comfortable (a plain row may win): on the tutorial level, whose Medium
        // is itself forgiving, the gap is thinnest (C1_01 0.95).
        public float MaxEasyShareOfMedium = 0.96f;

        /// <summary>The rules as the human lens sees a level: a competent player's utilisation, and the towers alone (no hero).</summary>
        public BalanceRules HumanTowersOnly()
        {
            BalanceRules human = Copy();
            human.Utilisation = HumanUtilisation;
            human.HeroShare = 0f;
            return human;
        }

        /// <summary>The human lens with the hero fighting beside the towers: what "beatable" is judged by.</summary>
        public BalanceRules HumanWithHero()
        {
            BalanceRules human = Copy();
            human.Utilisation = HumanUtilisation;
            return human;
        }

        // ---- The human target curve: what the waves are calibrated to (2026-11-04, Remco: "alles wat zwaarder maken") ----

        /// <summary>
        /// Firepower over need, towers alone, a competent player has on the plain waves of a continent's first level, Easy to
        /// Psycho. Easy 1.4 (the hero matters on boss and late waves), Medium 1.15 (placement and hero), Hard 0.95 (the hero
        /// and good play, or some permanent upgrades), Psycho 0.8 (the progression model already counts the continent's
        /// end-game upgrades for Psycho, <see cref="Progression"/>). Each level's waves are trimmed to this curve
        /// (<see cref="CampaignModel.BuildWaves"/>) after the solver has shaped them.
        /// </summary>
        public float[] HumanSurplus = { 1.4f, 1.15f, 0.95f, 0.8f };

        /// <summary>
        /// The campaign gets harder level by level (Remco: "hoe verder je in het spel komt hoe lastiger"): the pressure of the
        /// human target rises by this share per level of a continent, net of the upgrades the player is expected to own there,
        /// up to <see cref="HumanRampMax"/>.
        /// </summary>
        public float HumanRampPerLevel = 0.015f;
        public float HumanRampMax = 0.2f;

        /// <summary>The surplus the human target asks for at a level of a continent (index from 0) on a difficulty.</summary>
        public float HumanSurplusAt(int difficulty, int continentIndex) =>
            HumanSurplus[System.Math.Min(difficulty, HumanSurplus.Length - 1)] / (1f + System.Math.Min(HumanRampMax, HumanRampPerLevel * System.Math.Max(0, continentIndex)));

        /// <summary>
        /// What the probe says the model misreads per difficulty, Easy to Psycho (round 83): a factor on the health of every
        /// creature of that difficulty, on every level alike, on top of what the solver gives. The solver shapes a level
        /// (which wave is how tough against the others); how tough the level is as played is measured, and the part of that
        /// correction every level shares lives here, so a new level starts from it. A cell's own scalar (the script's
        /// target scale, found by the closed loop of Docs/WAVE_GRAMMAR.md, "How to balance a new level") comes on top and
        /// should stay near one (<see cref="CellScalarLeast"/> to <see cref="CellScalarMost"/>).
        /// </summary>
        // Round 83b: the geometric means of the first continent's 24 scalars after three passes of the closed loop
        // (0.98 / 1.31 / 1 / 0.87 was the first guess, the level of round 82).
        // Round 84: 0.981 / 1.365 / 1.051 / 0.936 times the means of the pass after the composition levers became rules.
        public float[] DifficultyScale = { 0.982f, 1.369f, 0.993f, 0.918f };

        public float DifficultyScaleFor(int difficulty) => DifficultyScale.Length == 0 ? 1f : DifficultyScale[System.Math.Min(difficulty, DifficultyScale.Length - 1)];

        /// <summary>
        /// The range a cell's own scalar may lie in. Outside it the model misreads the level's map or script by more than the
        /// closed loop should paper over: look at the level, not at the scalar.
        /// </summary>
        public float CellScalarLeast = 0.7f;
        public float CellScalarMost = 1.4f;

        /// <summary>Most the stored human surplus of a level may stray from its target curve, as a share of the target.</summary>
        public float HumanTargetTolerance = 0.06f;

        /// <summary>From one level of a continent to the next the human surplus may rise by at most this share (the ramp is monotone).</summary>
        public float RampTolerance = 0.03f;

        /// <summary>
        /// The in-play ramp (round 22e): per level and difficulty the margin the best replayed strategy and the creative reference keep
        /// (<see cref="AcceptanceRules.PlayMargin"/>, 0 to 1), which may rise from one level of a continent to the next by at most this much.
        /// </summary>
        public float InPlayRampTolerance = 0.15f;

        /// <summary>
        /// Most a single wave (bosses included) may ask of a competent player with the hero (<see cref="HumanWithHero"/>), Easy
        /// to Psycho. Pressure one is "just holds"; above it a few creatures reach the gate and the base, whose health, the
        /// hero's abilities, base turrets, drones and path objects (none in the model) carry the rest. Easy bosses fall to the
        /// defence outright; on Psycho a boss asks for everything the player has. The calibration softens a wave above it on
        /// its own (<see cref="CampaignModel.BuildWaves"/>), so a boss keeps high health but stays killable.
        /// </summary>
        public float[] HeroCap = { 1f, 1.15f, 1.25f, 1.35f };

        public float HeroCapFor(int difficulty) => HeroCap[System.Math.Min(difficulty, HeroCap.Length - 1)];

        /// <summary>
        /// A boss wave must be held outright: a boss that reaches the base ends the level in seconds (round 9 finale probe), so the
        /// plan model and the calibration judge a boss wave at this cap, whatever the difficulty's hero cap (round 22: every
        /// "ledger holds, game lost at the boss wave" disagreement of the first fit was this).
        /// </summary>
        public float BossWaveCap = 1f;

        /// <summary>The cap a wave is judged at: the hero cap, or the boss cap on a boss wave.</summary>
        public float CapFor(float cap, bool bossWave) => bossWave ? System.Math.Min(cap, BossWaveCap) : cap;

        /// <summary>
        /// The siege model (round 22c, <see cref="Leak"/>): creatures the guns do not kill on the walk hammer the gates and the base
        /// at their structure damage times this factor, while the guards there kill them. The structure damage the recorded runs
        /// really took over the paper hammer; fitted on recorded runs and kept on the BalanceCalibration asset, never typed.
        /// </summary>
        public float SiegeFactor = 1f;

        /// <summary>
        /// Share of a wave a repair drone really spends restoring something (the rest is flying between its bay and the structures);
        /// measured from the probe's drone records and kept on the BalanceCalibration asset.
        /// </summary>
        public float RepairDroneBusyShare = 0.5f;

        /// <summary>
        /// Share of their damage per second the guards at a gate's queue and the base's doorstep really land on a siege (round 22d):
        /// leakers spread out, slip past and break structures before the guns are done, so full rate overstated thin plans.
        /// Fitted on recorded bot and plan runs (the share with the best verdict agreement), kept on the BalanceCalibration asset.
        /// </summary>
        public float QueueGuardShare = 1f;

        /// <summary>
        /// The route-sampled kill model's fire factor (round 27, <see cref="RouteKill"/>): the damage the recorded guns really dealt
        /// over the route model's account of them at the per-type efficiencies. One number for the whole walk, because the per-type
        /// efficiencies stay on the pressure lens's scale (the purchases and the solver read them); fitted on the records and kept
        /// on the BalanceCalibration asset, never typed.
        /// </summary>
        public float RouteFire = 1f;

        /// <summary>
        /// The ledger buys sky guns only while the flyers' pressure is above this (round 22d): a sky gun does nothing for the ground and
        /// its price leaves every later purse, so one more bought on a hair starved the waves after and the solver could not land
        /// (Quartz Flats Easy wave 8). One means "until they are held".
        /// </summary>
        public float SkyHoldPressure = 1f;

        /// <summary>
        /// "Air is air" (Remco, round 61): a player who owns a gun made for the sky stands one for the first wave that brings
        /// flyers, whatever the guns that merely reach the air could do. Without it the ledger held small raids with a row of
        /// all-round guns, never bought the sky gun below <see cref="SkyHoldPressure"/>, and the solver sized every first raid
        /// against that row: Shard Ridge's first Easy raid came out at 30 health for four flyers.
        /// </summary>
        public bool SkyGunForFlyers = true;

        /// <summary>
        /// The least a raid's health multiplier may be per difficulty, Easy first (round 61; zero: no floor). The solver sizes a
        /// wave by paper pressure, and the paper rates a sky gun against a packed raid at about a third of what it does in play
        /// (flyers hang over a gate while they lash it), so on the low pressure Easy asks a raid came out as four flyers of 8
        /// health that any gun shot down. With the floor a flyer in a raid is about as tough as its Codex entry says, which is
        /// what makes the sky gun worth its price. Held inside the trim to the human curve (the other waves make room for it, so
        /// the level as a whole stays on the curve); the hero cap still softens a raid nobody could hold.
        /// </summary>
        public float[] RaidHealthFloor = { 0.6f, 0f, 0f, 0f };

        public float RaidHealthFloorFor(int difficulty) => RaidHealthFloor.Length == 0 ? 0f : RaidHealthFloor[System.Math.Min(difficulty, RaidHealthFloor.Length - 1)];

        /// <summary>
        /// The least a level's boss is scaled to, as a multiple of its book health before the difficulty's own health factor,
        /// Easy to Psycho (round 64, Remco: "on Hard it died before the first gate with one Cannon and one Gun Turret"). A boss
        /// wave had one health multiplier for the boss and its crowd, and the solver and the cap sized it by the crowd: on a
        /// crowded finale the boss came out at a third of its book health (Shard Ridge Hard: 349 hit points at wave 16, less
        /// than on Medium; Fault Line Hard: 217). The boss now has a scale of its own on top of the wave's multiplier
        /// (<see cref="WaveSpec.BossHealthScale"/>), never under this floor. Continent bosses are not touched; they have their
        /// own health share and the finale probe.
        /// </summary>
        public float[] LevelBossHealthFloor = { 0.5f, 0.75f, 0.9f, 0.9f };

        public float LevelBossHealthFloorFor(int difficulty) => LevelBossHealthFloor.Length == 0 ? 0f : LevelBossHealthFloor[System.Math.Min(difficulty, LevelBossHealthFloor.Length - 1)];

        /// <summary>
        /// What a level's walking boss asks on its own of a competent player with the hero, Easy to Psycho: the ledger's
        /// pressure of the boss alone against the defence that player stands for the boss's whole wave, where one is
        /// "everything that defence can deal in the boss's walk and its stand at the gate". The boss is sized to ask exactly
        /// this (<see cref="WaveSolver.Solve"/>), never under <see cref="LevelBossHealthFloor"/>, so it grows with the
        /// firepower the player has by then; the crowd of its wave fills what the boss leaves. A flying boss follows its
        /// wave instead (its pressure is the sky's, and the sky guns bought for it stay out of the purse).
        /// </summary>
        /// <remarks>
        /// Rounds 64 to 82 read the boss against a defence bought for the boss alone (the best single-target guns), where
        /// 0.2 / 0.33 / 0.4 / 0.45 gave the bosses of round 82. Read against the defence that really stands for the wave
        /// (round 83) the same bosses ask about 0.35 / 0.55 / 0.65 / 0.7; the values are a step above those (Remco: the
        /// Medium bosses feel weak).
        /// </remarks>
        public float[] LevelBossAlone = { 0.4f, 0.6f, 0.7f, 0.75f };

        public float LevelBossAloneFor(int difficulty) => LevelBossAlone.Length == 0 ? 0f : LevelBossAlone[System.Math.Min(difficulty, LevelBossAlone.Length - 1)];

        /// <summary>
        /// The most a boss wave may ask of a competent player with the hero, Easy to Psycho (round 64). A boss wave must
        /// really be held (<see cref="BossWaveCap"/>): on Easy and Medium a wave that is just held. On Hard it may go a
        /// little over, as the replays carried it. On Psycho the player has nothing to spare (grown to 1.35 and again to
        /// 1.15, Shard Ridge's last boss beat the searched plans). Over it the crowd of the wave gives way first, then the
        /// boss comes down to its floor.
        /// </summary>
        public float[] LevelBossGrowthCap = { 1f, 1f, 1.15f, 1f };

        public float LevelBossGrowthCapFor(int difficulty) => LevelBossGrowthCap.Length == 0 ? BossWaveCap : LevelBossGrowthCap[System.Math.Min(difficulty, LevelBossGrowthCap.Length - 1)];

        /// <summary>
        /// The share of a wave's budget its crowd keeps when a boss leads the wave (the composer's constant until round 65),
        /// on every level and difficulty. Rounds 65 to 83 let a cell thin it further (the lever "bosscrowd", Shard Ridge
        /// Hard and Psycho at 0.35, so the crowd left the boss room under the cap). Since round 83 the solver gives the
        /// crowd's health way to the boss by rule, so the lever went in round 84.
        /// </summary>
        public float BossWaveShare = 0.6f;

        /// <summary>
        /// The growth layer of the combat rules (Docs/COMBAT_RULES.md, section 6), Easy to Psycho: the factor the plated
        /// troops of a wave gain in number with each wave past the grace window of their trait (<see cref="GrowthWindow"/>),
        /// through the waves a defence without the counter must fall in. One is no growth. It applies where armour is the
        /// only counter trait a difficulty of a level sends (<see cref="WaveComposer.GrowthOn"/>: no flyer in any of its
        /// waves). Where flyers come too, the raids end a defence that lacks a counter (the grace report: every such row
        /// falls to a raid), and growth on top was measured (round 68: no plan won Shard Ridge Hard or Quartz Flats Easy)
        /// and read on paper (round 84: six and seven waves of Shard Ridge Hard and Psycho two to four times over the hero
        /// cap) as more than those levels carry. Rounds 68 to 83 wrote it per cell (the lever "armourgrowth", on the two
        /// levels without flyers).
        /// </summary>
        public float[] ArmourGrowth = { 1.4f, 1.4f, 1.5f, 1.5f };

        public float ArmourGrowthFor(int difficulty) => At(ArmourGrowth, difficulty);

        /// <summary>The same for flyers (the raids, the sprinkle, a flying troop). Built in round 68 and never set: one on every difficulty.</summary>
        public float[] AirGrowth = { 1f, 1f, 1f, 1f };

        public float AirGrowthFor(int difficulty) => At(AirGrowth, difficulty);

        /// <summary>
        /// The weight of plated troops in every fill of a wave, Easy to Psycho (1 = as the level's roster writes them): on
        /// the harder difficulties more of a wave's budget goes to armour, so the anti-armour gun is asked for in numbers.
        /// The builder's per-cell lever "plated" until round 84 (eleven cells, nine of them Hard or Psycho at 1.5).
        /// </summary>
        public float[] PlatedBias = { 1f, 1f, 1.5f, 1.5f };

        public float PlatedBiasFor(int difficulty) => At(PlatedBias, difficulty);

        /// <summary>
        /// The weight of swarm troops (no armour, at most twice the health of the lightest troop of the fill) in every fill,
        /// Easy to Psycho: on the harder difficulties more small bodies than a blast catches. The builder's per-cell lever
        /// "swarm" until round 84 (eight cells, seven of them Hard or Psycho).
        /// </summary>
        public float[] SwarmBias = { 1f, 1f, 1.5f, 1.5f };

        public float SwarmBiasFor(int difficulty) => At(SwarmBias, difficulty);

        private static float At(float[] values, int difficulty) => values == null || values.Length == 0 ? 1f : values[System.Math.Min(difficulty, values.Length - 1)];

        /// <summary>
        /// The Easy raid rule (the curator, round 22f): on Easy the first air raid comes no earlier than the wave by which the
        /// expected player's kills have paid for the cheapest owned sky gun, plus this many waves; a player who owns no sky gun
        /// gets no raid. <see cref="CampaignModel.EasyRaidFloor"/> derives the floor from the ledger's purse walk at every solve.
        /// </summary>
        public int EasyRaidMarginWaves = 1;

        /// <summary>
        /// The first-flyer rule per difficulty, Easy to Psycho (Remco, round 77): where true, no flyer comes before the bounties
        /// pay for one gun made for the sky. The wave that sky gun must stand in is the first flyer wave plus the grace waves
        /// (<see cref="CounterPurse"/>, the affordability check of round 76), and the bounties of every wave before it must
        /// reach the cheapest owned sky gun's price; the solver moves the raids and holds the flying troops until they do
        /// (<see cref="CampaignModel.BuildWaves"/>). So a player who spent the opening on ground guns can still earn the
        /// counter in time. Hard and Psycho players are expected to know the level and keep the coins back.
        /// </summary>
        public bool[] FlyersAfterBounties = { true, true, false, false };

        public bool FlyersAfterBountiesFor(int difficulty) => FlyersAfterBounties.Length > 0 && FlyersAfterBounties[System.Math.Min(difficulty, FlyersAfterBounties.Length - 1)];

        /// <summary>
        /// The finale probe (round 22f) finds the boss health multiple the creative reference bot just beats on Medium; the data
        /// carries this share of it, so a competent human has margin at the finale (FinaleProbeReport.DerivedHealth).
        /// </summary>
        public float FinaleHoldShare = 0.85f;

        /// <summary>
        /// Round 30 (Remco): Psycho's boss health share is never below Hard's, whatever the finale probe derives; the finale stage
        /// writes the larger of the two and ContinentFinaleTests asks that the Psycho finale player still beats every boss as-is.
        /// </summary>
        public bool PsychoBossShareAtLeastHard = true;

        /// <summary>
        /// Waves before a level's end at which a repair drone bought then still earns its price back (Remco 2026-11-07: a real
        /// investment; the last waves never pay). BalanceSheet.RepairDronePrice derives the drone's price from it. 3 until
        /// 2026-11-09; Remco then set the price at about 100 as a compromise between the rule's 60 (busy share 0.34 measured
        /// on late buys) and the old 170, which is five waves of the measured saving.
        /// </summary>
        public float RepairDroneBreakEvenWaves = 5f;

        /// <summary>
        /// The repair drone's price as the designer chose it (Remco 2026-11-09: about 100). Zero lets the investment rule
        /// (<see cref="RepairDroneBreakEvenWaves"/>) set the price; with a number here the droneprice stage writes this one and
        /// reports the rule's figure as advice, so a re-measured busy share does not move a chosen number (round 30 moved it
        /// to 120 that way).
        /// </summary>
        public int RepairDronePrice = 100;

        /// <summary>
        /// "Beatable" with the hero and the upgrades the campaign expects (<see cref="BalanceChecks.CheckBeatable"/>): the plain
        /// waves must leave at least this surplus (Easy to Psycho), and no wave may ask more than its <see cref="HeroCap"/> plus
        /// <see cref="BeatableTolerance"/>. Psycho below one: the end-game player also brings what the model leaves out.
        /// </summary>
        // Psycho 0.82 until 2026-11-09: C1_03 Psycho reads 0.80 on paper while its plans win 3/3 in play (the paper is pessimistic
        // there, lenient elsewhere, round 27); play is the judge, so the paper floor sits a little under it (0.75).
        public float[] MinBeatableSurplus = { 1.15f, 1.05f, 0.95f, 0.75f };
        public float BeatableTolerance = 0.04f;

        public float MinBeatableSurplusFor(int difficulty) => MinBeatableSurplus[System.Math.Min(difficulty, MinBeatableSurplus.Length - 1)];

        // ---- Meta economy: research and power between levels -------------------------------------------------------

        /// <summary>
        /// Research paid per difficulty, as a multiple of the level's base. With 1 / 2 / 3 / 5, every level on Easy pays a
        /// fifth of all research upgrades and every level on Psycho pays all of them (GAME_DESIGN, Economy).
        /// </summary>
        public float[] ResearchMultipliers = { 1f, 2f, 3f, 5f };

        /// <summary>Power per difficulty; none on Easy, Psycho alone pays every drone upgrade.</summary>
        public float[] PowerMultipliers = { 0f, 1f, 1.5f, 2.2f };

        /// <summary>Share of a continent's research budget one clear of every level of it on Easy pays.</summary>
        public float EasyResearchShare = 0.2f;

        /// <summary>
        /// Levels per continent (STORY: six continents of twenty). Payouts are weighted by a level's index in its continent,
        /// against the full twenty, so a level pays the same however many of its continent's levels exist yet.
        /// </summary>
        public int LevelsPerContinent = 20;

        /// <summary>Research weight rises from 1 at a continent's first level by this much up to its nineteenth.</summary>
        public float ResearchCurveRise = 0.5f;

        /// <summary>Research weight of a continent's last level, the continent boss.</summary>
        public float ContinentFinaleWeight = 2f;

        /// <summary>No level pays less base research than this, so a small continent budget never pays a point or two.</summary>
        public int MinResearchPerLevel = 4;

        /// <summary>
        /// The highest permanent-upgrade tier open once the continent at this index is reached (Remco, round 66: ten tiers,
        /// the first continent opens four, each later one or two more up to ten on the fifth). The generator writes it to the
        /// upgrade catalogue, which the game reads; the progression model shops under the same caps.
        /// </summary>
        public int[] UpgradeTiersPerContinent = { 4, 6, 8, 9, 10 };

        public int UpgradeTierCap(int continent) =>
            UpgradeTiersPerContinent[System.Math.Max(0, System.Math.Min(continent, UpgradeTiersPerContinent.Length - 1))];

        /// <summary>
        /// The price of each tier as a multiple of the first tier's. The first three are the old three tiers (1, 2, 3 times
        /// the first), so what a player bought before round 66 cost and gives the same; from the fourth on a tier costs a
        /// little more than the straight line, ending at twenty times the first.
        /// </summary>
        public float[] UpgradeTierCostCurve = { 1f, 2f, 3f, 4.4f, 6f, 8f, 10.4f, 13.2f, 16.4f, 20f };

        /// <summary>
        /// A continent's research budget (what Psycho alone, or Medium and Hard together, pay) as a share of what the tiers
        /// it opens cost. All four difficulties pay 2.2 budgets, so at 0.55 they pay everything the continent opens with a
        /// fifth to spare, and no single difficulty buys it all. Crystal Plains' budget (748, FinalePlan) is this share of
        /// its four tiers; it is pinned there because the waves are calibrated on it.
        /// </summary>
        public float ResearchBudgetShare = 0.55f;

        /// <summary>Power weight of a level: one, plus this much per boss wave on Medium (bosses pay power, GAME_DESIGN).</summary>
        public float PowerPerBossWeight = 0.75f;

        /// <summary>Boss waves the power curve expects per level on Medium, so a continent's power sums to its budget.</summary>
        public float ExpectedBossesPerLevel = 2f;

        /// <summary>A copy to vary one knob on (the arrays are shared: treat them as read-only).</summary>
        /// <summary>
        /// The wave grammar's numbers (round 79, Docs/WAVE_GRAMMAR.md): clusters and breathers, where surprises fall, how a
        /// boss wave is laid out. One object for every level; a copy of the rules shares it.
        /// </summary>
        public WaveGrammarRules Grammar = new WaveGrammarRules();

        public BalanceRules Copy() => (BalanceRules)MemberwiseClone();

        /// <summary>The default instance the generator and the tools use.</summary>
        public static BalanceRules Default => new BalanceRules();
    }
}
