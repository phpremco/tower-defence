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
        public float ArmorThreatShare = 0.8f;

        /// <summary>
        /// The hit size plating is priced against in threat: a plated creature counts as its health over the share of such a
        /// hit that gets through (a middling gun, between the Pulse Rifle and the Cannon), so its kill money pays for the
        /// heavier gun it asks for.
        /// </summary>
        public float PlatingReferenceHit = 15f;
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
        public float AmbushFactor = 1f;
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
        public float MaxEasySurplus = 1.6f;

        /// <summary>
        /// The plain Easy waves of a level may ask at most this share of what the plain Medium waves ask of the same player,
        /// so Easy stays clearly the easier choice (about 0.8 on most levels).
        /// </summary>
        public float MaxEasyShareOfMedium = 0.93f;

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

        /// <summary>When false, the waves keep the solver's shape and are not trimmed to the human target (tests of the solver alone).</summary>
        public bool CalibrateToHuman = true;

        /// <summary>The surplus the human target asks for at a level of a continent (index from 0) on a difficulty.</summary>
        public float HumanSurplusAt(int difficulty, int continentIndex) =>
            HumanSurplus[System.Math.Min(difficulty, HumanSurplus.Length - 1)] / (1f + System.Math.Min(HumanRampMax, HumanRampPerLevel * System.Math.Max(0, continentIndex)));

        /// <summary>Most the stored human surplus of a level may stray from its target curve, as a share of the target.</summary>
        public float HumanTargetTolerance = 0.06f;

        /// <summary>From one level of a continent to the next the human surplus may rise by at most this share (the ramp is monotone).</summary>
        public float RampTolerance = 0.03f;

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
        /// The leak model (round 22b): what a wave's creatures the defence does not stop do to the base. The share of a wave over
        /// pressure one gets through, stands at the doorstep and hammers the base with its structure damage (<see cref="Leak"/>) for
        /// this many seconds each (the measured life of a leaker at the doorstep: the hero, the base turret and the guns in reach kill
        /// it eventually; none of that is in the model). Fitted on recorded runs and kept on the BalanceCalibration asset, never typed.
        /// </summary>
        public float LeakFactor = 10f;

        /// <summary>
        /// Share of a wave a repair drone really spends restoring something (the rest is flying between its bay and the structures);
        /// measured from the probe's drone records and kept on the BalanceCalibration asset.
        /// </summary>
        public float RepairDroneBusyShare = 0.5f;

        /// <summary>
        /// "Beatable" with the hero and the upgrades the campaign expects (<see cref="BalanceChecks.CheckBeatable"/>): the plain
        /// waves must leave at least this surplus (Easy to Psycho), and no wave may ask more than its <see cref="HeroCap"/> plus
        /// <see cref="BeatableTolerance"/>. Psycho below one: the end-game player also brings what the model leaves out.
        /// </summary>
        public float[] MinBeatableSurplus = { 1.15f, 1.05f, 0.95f, 0.82f };
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

        /// <summary>Power weight of a level: one, plus this much per boss wave on Medium (bosses pay power, GAME_DESIGN).</summary>
        public float PowerPerBossWeight = 0.75f;

        /// <summary>Boss waves the power curve expects per level on Medium, so a continent's power sums to its budget.</summary>
        public float ExpectedBossesPerLevel = 2f;

        /// <summary>A copy to vary one knob on (the arrays are shared: treat them as read-only).</summary>
        public BalanceRules Copy() => (BalanceRules)MemberwiseClone();

        /// <summary>The default instance the generator and the tools use.</summary>
        public static BalanceRules Default => new BalanceRules();
    }
}
