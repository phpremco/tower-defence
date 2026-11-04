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

        /// <summary>Most creatures one splash hit can catch, however dense the stream.</summary>
        public float MaxSplashTargets = 2.4f;

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

        /// <summary>Pressure the solver aims plain Medium waves at, mid-level. One means the towers alone just hold.</summary>
        public float TargetPressure = 0.6f;

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

        /// <summary>The solver may change the budget from one wave to the next by at most this factor either way.</summary>
        public float MaxBudgetStep = 1.45f;

        /// <summary>Smallest budget of any wave, in threat points, so the first wave is never empty.</summary>
        public float MinBudget = 4f;

        /// <summary>
        /// Pressure of each difficulty as a share of Medium (Easy to Psycho). Used only for the waves a difficulty adds
        /// (extra air raids, extra bosses); everything else follows the difficulty profile, and the profiles are tuned so
        /// their waves land near these shares.
        /// </summary>
        public float[] DifficultyPressure = { 0.7f, 1f, 1.5f, 2.1f };

        // ---- Checks the solver does not optimise -------------------------------------------------------------------

        /// <summary>
        /// A plain wave may reach the difficulty's share of the highest Medium target (late heavy waves) times this margin;
        /// bosses get the allowance on top.
        /// </summary>
        public float BandMargin = 1.4f;
        public float BossAllowance = 1.6f;

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

        /// <summary>Most firepower over need, towers alone, a competent player may have on the plain Easy waves of a level.</summary>
        public float MaxEasySurplus = 2.1f;

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
