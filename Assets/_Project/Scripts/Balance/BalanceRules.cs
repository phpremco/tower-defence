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
        /// Calibrated against the PlayMode greedy bot: pressure 1 is where it stops holding. Retune after a probe run.
        /// </summary>
        public float Utilisation = 0.35f;

        /// <summary>Most creatures one splash hit can catch, however dense the stream.</summary>
        public float MaxSplashTargets = 2.4f;

        /// <summary>Gun towers the player can place usefully per ground front (build spots in reach of the road).</summary>
        public int TowersPerFront = 16;

        /// <summary>The hero holds and hits what stops at a gate; counted at this share of the wave window.</summary>
        public float HeroShare = 0.5f;

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

        // ---- Meta economy: research and power between levels -------------------------------------------------------

        /// <summary>
        /// Research paid per difficulty, as a multiple of the level's base. With 1 / 2 / 3 / 5, every level on Easy pays a
        /// fifth of all research upgrades and every level on Psycho pays all of them (GAME_DESIGN, Economy).
        /// </summary>
        public float[] ResearchMultipliers = { 1f, 2f, 3f, 5f };

        /// <summary>Power per difficulty; none on Easy, Psycho alone pays every drone upgrade.</summary>
        public float[] PowerMultipliers = { 0f, 1f, 1.5f, 2.2f };

        /// <summary>Share of all research upgrades one clear of every level on Easy pays.</summary>
        public float EasyResearchShare = 0.2f;

        /// <summary>Base power of a level: a fixed part plus a part per boss wave (bosses pay power, GAME_DESIGN).</summary>
        public int PowerPerLevel = 4;
        public int PowerPerBoss = 3;

        /// <summary>Later levels pay more research than earlier ones: base research grows by this share per level.</summary>
        public float ResearchGrowthPerLevel = 0.18f;

        /// <summary>The default instance the generator and the tools use.</summary>
        public static BalanceRules Default => new BalanceRules();
    }
}
