namespace Game.Balance
{
    /// <summary>
    /// Every knob of the balance model in one place. The generator passes one instance to the wave formula and the ledger,
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
        // Round 88b: 1.5 (3 until then). Armour equal to the body doubled every boss bounty in round 87 (Brood Mother 198 -> 337);
        // a mid-level boss on Hard added 34 to 82 % to the purse in one go and the last boss paid a pile nobody can spend
        // (Tools/PlayTest/r88b_economy.txt). Remco: "money piles up, the later the easier".
        public float BossRewardFactor = 1.5f;

        // ---- How well the player's defence works -----------------------------------------------------------------

        /// <summary>
        /// Share of the window a tower really spends hurting something (target switching, misses, imperfect placement).
        /// Calibrated against the PlayMode bot: pressure 1 is where it stops holding. Retune after a probe run. 0.35 until
        /// 2026-11-03; the S4 probe (bot at the trunk gate, frame-rate-proof guns, the kill money of <see cref="KillPurse"/>)
        /// lost with the ledger at 0.7 to 0.85. After the model and bot fixes of that day it held Crysium Vein untouched at
        /// 0.27 and lost it at wave 10 (the prowler and roller rush) at 0.3 and 0.35: the bot holds or breaks there.
        /// </summary>
        public float Utilisation = 0.27f;

        /// <summary>What a gun lands on a boss in its reach: <see cref="BossUtilisation"/>, or the stream's utilisation where that is not set.</summary>
        public float BossFire => BossUtilisation > 0f ? BossUtilisation : Utilisation;

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

        // ---- The health formula (round 87): how tough the creatures of a wave are ----------------------------------

        /// <summary>
        /// The rise of creature health over a level, geometric in the wave's phase (first wave 0, last wave 1): the first
        /// wave walks in at <see cref="RampStart"/> times the level's middle, the last at <see cref="RampEnd"/>
        /// (<see cref="WaveFormula.Ramp"/>). Fitted in round 87 on the plain ground waves round 86 stored for the first six
        /// levels (Tools/PlayTest/r87_fit.py: 0.747 and 1.339, one slope for every difficulty; a slope per difficulty took
        /// the residual from x1.37 to x1.34 only). The two multiply to one, so <see cref="DifficultyScale"/> is the
        /// multiplier of a level's middle wave.
        /// </summary>
        // Round 88b: 0.6 to 1.68 (0.75 to 1.34 until then). A gun taken to level 3 gains x3 to x4.7 while a creature gained
        // x1.79 over a level, so on paper the pressure per coin stood still from the middle to the last third on Hard
        // (x1.00; Tools/PlayTest/r88b_economy.txt) and Remco felt levels get easier towards the end. At x2.8 it rises on
        // every difficulty (Hard x1.17, the others x1.57 to x1.59).
        public float RampStart = 0.6f;
        public float RampEnd = 1.68f;

        /// <summary>Health factors per wave kind (a rush, a heavy wave, an air raid), so the rhythm of a level is felt.</summary>
        public float RushFactor = 1.1f;
        public float HeavyFactor = 1.15f;
        public float AirFactor = 0.9f;

        /// <summary>The wave after a boss is a breather.</summary>
        public float AfterBossFactor = 0.8f;

        /// <summary>The crowd that walks in with a level's boss is this much softer than its place in the level says: the boss is the fight.</summary>
        public float BossCrowdFactor = 0.55f;

        /// <summary>
        /// Mid-level peaks (round 87, Remco: "a bit of panic" before the boss, not eleven safe waves and one pass or
        /// fail): the phases of a level at which a ground wave walks in <see cref="PeakFactor"/> times tougher. A level of
        /// <see cref="LongLevelWaves"/> waves or more gets one more at <see cref="LongLevelPeakPhase"/>. Which wave a peak
        /// lands on is <see cref="WaveFormula.Peaks"/>.
        /// </summary>
        public float[] PeakPhases = { 0.4f, 0.7f };
        public int LongLevelWaves = 16;
        public float LongLevelPeakPhase = 0.55f;
        public float PeakFactor = 1.35f;

        /// <summary>
        /// Whether a swarm (rhythm R) that comes between two air raids may carry a peak (round 88b: no). Fault Line on
        /// Psycho has seven raids in sixteen waves and its only late ground wave, the swarm of wave 13 between the raids
        /// of 12 and 14, took the ramp, the rush factor and a peak at once (x3.3 on the fastest creatures, a full base
        /// lost in that one wave in 32 of 57 lost bot runs). Between two raids the purse belongs to the sky guns, so the
        /// swarm there is an ordinary wave; the peak is not placed anew, so no other wave moves
        /// (<see cref="WaveSolver.PeaksOf"/>).
        /// </summary>
        public bool PeakOnARushBetweenRaids;

        /// <summary>
        /// The ceiling on what a wave's place, kind and peak together may do to its creatures (round 88c): ramp x kind x
        /// peak never exceeds this multiple of the level's middle. The ramp of round 88b (x1.68 at the end) times a late
        /// peak times a heavy wave reached x2.6 (Shard Ridge wave 13 on Hard and Psycho, the wave between its raids, where
        /// 56 of 60 lost Psycho runs ended and the rule "no ground wave far tougher than its level" broke). A peak early or
        /// in the middle of a level is untouched (the latest that stays under it: a heavy peak at 0.64 of the level); a
        /// late peak is cut to the ceiling, so the last third of a level is carried by the ramp and the boss, not by one
        /// wave. The boss-crowd and after-boss factors are applied after it.
        /// </summary>
        public float WaveCeiling = 1.8f;

        /// <summary>Seconds before the first wave and between waves, as the generator writes them: the model's purse runs on the same clock.</summary>
        public float FirstWaveDelay = 10f;
        public float WaveDelay = 8f;

        // ---- Checks on the paper ---------------------------------------------------------------------------------------

        /// <summary>
        /// Most of a boss fight's firepower a continent boss's mechanic may take away (Docs/BOSSES.md: no wipe-outs, an effect
        /// reaches about a quarter of the field): the ledger never counts more, and <see cref="BalanceChecks.CheckBossMechanic"/>
        /// fails a boss whose mechanic would.
        /// </summary>
        public float MaxBossMechanicShare = 0.35f;

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
        /// Share of its data rate a gun lands on a boss while the boss is in its reach; zero: <see cref="Utilisation"/>,
        /// as for the stream (the bots' own lens and the calibration fit, which were fitted that way). See
        /// <see cref="HumanBossUtilisation"/>.
        /// </summary>
        public float BossUtilisation;

        /// <summary>
        /// <see cref="BossUtilisation"/> of the human lenses (round 86): one. The utilisation of a stream is what a gun
        /// loses to the gaps in it; a boss is one slow body that every gun in reach fires at without a gap, and the
        /// ledger already counts only the seconds the boss is in a gun's reach. Charged at the stream's 0.34 the boss of
        /// Landing Site Easy asked 1.45 on its own of the seven guns a reference player really stood at x1.76 of the data
        /// (and the whole wave 2.56), in a run that lost no base health; at one it asks 0.42 (Tools/PlayTest/boss_paper_*.txt).
        /// The paper stays on the careful side: the crowd of that wave still reads 1.11 on its own.
        /// </summary>
        public float HumanBossUtilisation = 1f;

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
            human.BossUtilisation = HumanBossUtilisation;
            human.HeroShare = 0f;
            return human;
        }

        /// <summary>The human lens with the hero fighting beside the towers: what "beatable" is judged by.</summary>
        public BalanceRules HumanWithHero()
        {
            BalanceRules human = Copy();
            human.Utilisation = HumanUtilisation;
            human.BossUtilisation = HumanBossUtilisation;
            return human;
        }

        // ---- The human target curve: what the waves are calibrated to (2026-11-04, Remco: "alles wat zwaarder maken") ----

        /// <summary>
        /// Firepower over need, towers alone, a competent player has on the plain waves of a continent's first level, Easy to
        /// Psycho. Easy 1.4 (the hero matters on boss and late waves), Medium 1.15 (placement and hero), Hard 0.95 (the hero
        /// and good play, or some permanent upgrades), Psycho 0.8 (the progression model already counts the continent's
        /// end-game upgrades for Psycho, <see cref="Progression"/>). Each level's waves are trimmed to this curve
        /// by this curve only on paper now (the finale's stand-in waves, <see cref="CampaignModel.ModelTarget"/>); since
        /// round 87 a wave's health is <see cref="WaveFormula"/>'s.
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
        /// The health multiplier of a level's middle wave per difficulty, Easy to Psycho, on every level alike (round 87;
        /// until then a correction on a solved wave). The level's own scalar (Tools/PlayTest/wave_tuning.txt, found by the
        /// closed loop of Docs/WAVE_GRAMMAR.md, "How to balance a new level") comes on top and should stay near one
        /// (<see cref="LevelScalarLeast"/> to <see cref="LevelScalarMost"/>).
        /// </summary>
        // Round 87: the fit of the formula on round 86's stored waves (Tools/PlayTest/r87_fit.py: 1.499 / 1.853 / 1.229 / 2.381).
        // Round 88b: 1.175 / 1.44 / 1.14 / 1.53 after the steeper ramp, the tower threat, the escorts, the blast cap and the
        // harder Hard and Psycho targets (two one-seed passes; the scalars had run onto their lower bound).
        public float[] DifficultyScale = { 1.23f, 1.634f, 1.074f, 1.38f };

        public float DifficultyScaleFor(int difficulty) => DifficultyScale.Length == 0 ? 1f : DifficultyScale[System.Math.Min(difficulty, DifficultyScale.Length - 1)];

        /// <summary>
        /// The range a level's own scalar may lie in. Outside it the formula misreads the level's map or script by more than
        /// the closed loop should paper over: look at the level, not at the scalar.
        /// </summary>
        public float LevelScalarLeast = 0.7f;
        public float LevelScalarMost = 1.4f;

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
        /// defence outright; on Psycho a boss asks for everything the player has. The plan search and the finale model judge
        /// a plan against it; no stored wave is sized by it since round 87 (<see cref="WaveFormula"/>).
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
        /// The least a raid's health multiplier may be per difficulty, Easy first (round 61; zero: no floor): a flyer in a
        /// raid is about as tough as its Codex entry says, which is what makes the sky gun worth its price. Rounds 61 to 86
        /// needed it against a solve on paper pressure (a first Easy raid came out as four flyers of 8 health); under the
        /// formula of round 87 it only holds a raid up where a level's scalar is low (<see cref="WaveSolver.Solve"/>).
        /// </summary>
        public float[] RaidHealthFloor = { 0.6f, 0f, 0f, 0f };

        public float RaidHealthFloorFor(int difficulty) => RaidHealthFloor.Length == 0 ? 0f : RaidHealthFloor[System.Math.Min(difficulty, RaidHealthFloor.Length - 1)];

        /// <summary>
        /// Every boss walks in armour (Remco, round 85: "they have no armour; that should be standard"): the armour pool of
        /// a boss, a level's or a continent's, is at least this share of its body. A boss whose own data gives it more
        /// keeps that. So light fire only chips every boss and the anti-armour guns are what a boss asks for, by the same
        /// table as every other plated creature (Docs/COMBAT_RULES.md, section 1). The generator and the finale stage
        /// write it (<see cref="BossArmourFor"/>). Round 87: an armour pool as large as the body (0.25 until then), so
        /// that stripping a boss's plates is a job the player sees a Cannon do.
        /// </summary>
        // Round 88b: 0.5 (1 in round 87). With armour equal to the body the last boss of the two levels without flyers ended
        // 66 to 88 % of all lost runs and only a Cannon-first build got through (laser and cannon archetypes three to six
        // ladder steps apart); a third of the bar as armour still asks for shells (x2.5 per coin) without being a wall.
        public float BossArmourShare = 0.5f;

        /// <summary>The armour pool a boss of this body carries: its own, or the rule's share of its body when that is more.</summary>
        public float BossArmourFor(float body, float armour) => System.Math.Max(armour, (float)System.Math.Round(body * BossArmourShare));

        /// <summary>
        /// A level's boss, one formula (round 87, <see cref="WaveFormula.BossBar"/>): its bar as spawned (body plus
        /// armour) is <see cref="BossBase"/> on Medium at a continent's first level for a boss in the last wave, times
        /// <see cref="BossDifficulty"/> (Easy to Psycho, never falling), times one plus <see cref="BossLevelGrowth"/> per
        /// level of the continent, times <see cref="BossEarlyShare"/> for a boss in the first wave rising to one in the
        /// last, times <see cref="FlyingBossShare"/> for a boss that flies (few guns reach it). It does not move with the
        /// level's scalar or <see cref="DifficultyScale"/>: a boss grows with the level and the difficulty and with
        /// nothing else, and the closed loop cannot trade crowd for boss. Rounds 85 and 86 sized it from the creatures
        /// around it, which made Hard bosses smaller than Medium ones on five of six levels.
        /// </summary>
        // Round 88b: 1750 (2050 until then): the boss now brings a brood worth 0.3 to 0.9 of its bar and fires at the guns.
        public float BossBase = 1550f;
        public float[] BossDifficulty = { 0.7f, 1f, 1.09f, 1.8f };
        // Round 88b: 0.16 with BossBase 1550 (0.12 with 2050): the first two levels send no flyers and nothing there ended a run
        // but the last boss (67 and 84 % of the lost runs), so the first bosses come down and the sixth level's stays.
        public float BossLevelGrowth = 0.16f;
        public float BossEarlyShare = 0.3f;
        public float FlyingBossShare = 0.6f;

        public float BossDifficultyFor(int difficulty) => At(BossDifficulty, difficulty);

        /// <summary>
        /// The floor under that formula (Remco, round 85: "on Easy the boss felt like nonsense, the lava rocks seem
        /// stronger"): the bar of a level's boss (body plus armour, the health bar the player sees) is never under this
        /// many times the bar of the toughest ordinary walker met in the level before it, Easy to Psycho
        /// (<see cref="BossYardstick"/>). The constants above are chosen so that the formula sizes the bosses and the
        /// floor only catches a level whose walkers run away. Zero: no floor.
        /// </summary>
        public float[] BossOverRegular = { 3f, 3f, 3f, 3f };

        public float BossOverRegularFor(int difficulty) => BossOverRegular == null || BossOverRegular.Length == 0 ? 0f : BossOverRegular[System.Math.Min(difficulty, BossOverRegular.Length - 1)];

        /// <summary>
        /// The share of a wave's budget its crowd keeps when a boss leads the wave (the composer's constant until round 65),
        /// on every level and difficulty. Rounds 65 to 83 let a cell thin it further (the lever "bosscrowd", Shard Ridge
        /// Hard and Psycho at 0.35, so the crowd left the boss room under the cap); the lever went in round 84. How tough
        /// that crowd is, is <see cref="BossCrowdFactor"/>.
        /// </summary>
        public float BossWaveShare = 0.6f;

        /// <summary>
        /// The escort rule of a level boss (round 88b, Remco: "the boss wave feels pathetic ... once the boss passed the
        /// first defence it felt like 5 minutes until it reached the end"). Every level boss sends a brood out of its body
        /// along its walk. Over an unhindered walk from spawn to the base the brood is worth the difficulty's escort worth
        /// (DifficultyProfile, <see cref="DifficultyStats.EscortWorth"/>) times the boss's bar as spawned, in creatures as
        /// tough as the ordinary creatures of the boss's own wave. It grows along the road: at the start of the walk it
        /// comes at <see cref="EscortStartWeight"/> times its mean rate, at the end at <see cref="EscortEndWeight"/> (a
        /// straight line between; the two average to one, so the worth holds whatever they are). A boss killed at the
        /// first defence has sent little; the long stretch behind it gets the most.
        /// </summary>
        public float EscortStartWeight = 0.4f;
        public float EscortEndWeight = 1.6f;

        /// <summary>
        /// Share of the brood's health that walks in as the toughest plated walker the level sent before the boss, so the
        /// escort is not only food for a blast. Only when that share of the whole walk pays for at least one of them, and
        /// never for a boss that flies.
        /// </summary>
        public float EscortHeavyShare = 0.25f;

        /// <summary>The most a boss ever sends, as a multiple of the whole walk's worth: a boss held at a gate or slowed for long keeps sending, but not without end.</summary>
        public float EscortMostOfWorth = 1.5f;

        /// <summary>Share of its bounty an escort creature pays. Zero: a brood that grows with the walk must not pay for letting the boss walk.</summary>
        public float EscortBountyShare;

        /// <summary>
        /// Where a creature of the brood goes when it comes out of the boss (round 88c, Remco: "those Cragbacks were on top
        /// of the boss at the same speed"). A Cragback walks at 0.6, a Brood Mother at 0.55: sent out at the boss's feet it
        /// stays there and hides it for the whole walk. So every creature of a brood breaks out of the boss's front and
        /// runs to a place of its own ahead of it: the first <see cref="EscortLeadCells"/> clear of the two bodies, each
        /// next one <see cref="EscortSpacingCells"/> further, <see cref="EscortSlots"/> places before the first is used
        /// again. It runs there at <see cref="EscortDashFactor"/> times its own pace and walks on at its own pace from
        /// there, so the boss stays in view and the brood reaches the guns first, as an escort should.
        /// </summary>
        public float EscortLeadCells = 1.2f;
        public float EscortSpacingCells = 0.7f;
        public int EscortSlots = 4;
        public float EscortDashFactor = 2.6f;

        /// <summary>Where the brood of a level boss joins its road on average, as a share of the road: the weights put its middle past halfway.</summary>
        public float EscortStartShare => EscortStartWeight + EscortEndWeight <= 0f ? 0.5f : (EscortStartWeight * 0.5f + (EscortEndWeight - EscortStartWeight) / 3f) / ((EscortStartWeight + EscortEndWeight) * 0.5f);

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
        /// The first air raid of a level is announced (round 88c, Remco on Shard Ridge Hard: "suddenly tons of air units,
        /// none all game ... when all money is already spent on guns"): <see cref="FirstRaidWarningWaves"/> waves before
        /// the level's first raid wave the player is warned. And a level with an air raid starts with extra money worth
        /// this many of the sky gun it sells (round 88d, Levels.StartMoneyRule; round 88c paid the same coins as a purse
        /// with the warning, which Remco found illogical in the middle of a level). Easy and Medium get none: there the
        /// first-flyer rule above already holds the flyers back until the bounties have paid for the gun. On Hard and
        /// Psycho nothing held them back ("expected to know the level"), which a first play cannot: the extra start
        /// money is that knowledge in coins, and spending it on something else is the player's own choice.
        /// </summary>
        public float[] RaidStartGuns = { 0f, 0f, 1f, 1f };

        /// <summary>How many waves before its first air raid a level warns the player.</summary>
        public int FirstRaidWarningWaves = 2;

        public float RaidStartGunsFor(int difficulty) => RaidStartGuns.Length > 0 ? RaidStartGuns[System.Math.Max(0, System.Math.Min(difficulty, RaidStartGuns.Length - 1))] : 0f;

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
