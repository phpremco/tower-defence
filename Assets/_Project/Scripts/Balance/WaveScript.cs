namespace Game.Balance
{
    /// <summary>Where a boss walks in from, per difficulty.</summary>
    public enum BossRoute
    {
        /// <summary>Through the main gate with the other creatures: the default for the whole first world.</summary>
        MainGate = 0,

        /// <summary>Through the gate of the second road, a second front for the boss alone.</summary>
        SecondRoad = 1,

        /// <summary>Up through the level's own boss entrance (a pool or the open ground).</summary>
        Entrance = 2,
    }

    /// <summary>One creature of a level's roster: when it joins, how much of the budget it takes and how it arrives.</summary>
    public sealed class TroopSpec
    {
        public string Enemy = string.Empty;
        public int FirstWave = 1;
        public float Weight = 1f;
        public float Interval = 1f;

        /// <summary>Spawn source as the game stores it: 0 the gate, otherwise a pool kind or 4 for the element nests.</summary>
        public int Source;
        public float Delay;

        /// <summary>Index of the easiest difficulty this troop joins on; easy may be spared a species.</summary>
        public int MinDifficulty;

        /// <summary>Creatures that never come alone come in groups of about this many; zero mixes singles and threes.</summary>
        public int Pack;

        /// <summary>
        /// Creatures that come one at a time, a whole interval apart: too spread for a shell to catch two, so rate of fire is
        /// the answer (round 21a, Docs/BALANCE.md "Roles by enemy"). Ignored for a pack species.
        /// </summary>
        public bool Loose;
    }

    /// <summary>A boss, the wave it walks in on, from which difficulty, and its way in per difficulty.</summary>
    public sealed class BossSpec
    {
        public int Wave;
        public string Enemy = string.Empty;

        /// <summary>Easiest difficulty with this boss: harder difficulties add boss waves (GAME_DESIGN).</summary>
        public int MinDifficulty;

        /// <summary>Route per difficulty index (Easy to Psycho); a shorter array repeats its last entry.</summary>
        public BossRoute[] Routes = { BossRoute.MainGate };

        public BossRoute RouteFor(int difficulty) => Routes.Length == 0 ? BossRoute.MainGate : Routes[difficulty < Routes.Length ? difficulty : Routes.Length - 1];
    }

    /// <summary>
    /// A level's wave script: how many waves, which creatures join and from which wave, the rhythm of wave kinds, which
    /// waves turn into air raids per difficulty, and on which waves a boss walks in. The script sets the size of the crowd;
    /// how tough the creatures are comes from the solver, so the script carries the level's character, not its balance.
    /// </summary>
    public sealed class WaveScriptSpec
    {
        public string Key = string.Empty;
        public int Waves;

        /// <summary>
        /// Threat budget of the first and the last wave (one point is twenty health); the curve between is geometric.
        /// This is the size of the crowd, and with it the kill money. How tough each creature is comes from the solver.
        /// </summary>
        public float Budget = 8f;
        public float FinalBudget = 100f;

        public float BudgetOf(int number) =>
            Waves <= 1 ? Budget : Budget * (float)System.Math.Pow(FinalBudget / Budget, (number - 1) / (double)(Waves - 1));

        public TroopSpec[] Troops = new TroopSpec[0];

        /// <summary>The creature of the air raids and of the escorts behind the last boss.</summary>
        public string Flyer = string.Empty;

        /// <summary>Seconds between two flyers of an air raid: a small value packs the raid so splash flak is the answer.</summary>
        public float FlyerInterval = 1.6f;

        /// <summary>Wave numbers (from 1) that become air raids; a difficulty takes the first few of them.</summary>
        public int[] AirWaves = new int[0];
        public int[] AirWavesPerDifficulty = { 0, 0, 0, 0 };

        /// <summary>
        /// How much bigger an air raid is per difficulty (Easy to Psycho): the flyer crowd and the pressure the solver aims
        /// the raid at grow together, so there are more flyers of about the same toughness and splash matters more. A
        /// shorter array repeats its last entry.
        /// </summary>
        public float[] AirRaidScale = { 1f };

        public float AirRaidScaleFor(int difficulty) =>
            AirRaidScale.Length == 0 ? 1f : AirRaidScale[difficulty < AirRaidScale.Length ? difficulty : AirRaidScale.Length - 1];

        /// <summary>From this difficulty index on, plain waves after the first air raid also carry a few flyers.</summary>
        public int AirSprinkleFrom = 4;

        /// <summary>
        /// Per difficulty, the earliest wave the first air raid may come on (0 = as written): the difficulty's raids shift together
        /// until the first reaches it, and a floor past the last wave removes them. The solver owns it (the Easy raid rule, round
        /// 22f, <see cref="CampaignModel.EasyRaidFloor"/>: no raid before the expected player can afford a sky gun, plus a wave).
        /// </summary>
        public int[] FirstAirWaveFloor = { 0 };

        public int FirstAirWaveFloorFor(int difficulty) => FirstAirWaveFloor == null || FirstAirWaveFloor.Length == 0 ? 0 : FirstAirWaveFloor[difficulty < FirstAirWaveFloor.Length ? difficulty : FirstAirWaveFloor.Length - 1];

        /// <summary>
        /// The builder bot's lever (round 22, WaveTuning): a scale on the human target pressure per difficulty (Easy to Psycho; a
        /// shorter array repeats its last entry). 1 is the curve; the builder moves a failing cell by small steps, at most twice.
        /// </summary>
        public float[] TargetScale = { 1f };

        public float TargetScaleFor(int difficulty) => TargetScale == null || TargetScale.Length == 0 ? 1f : TargetScale[difficulty < TargetScale.Length ? difficulty : TargetScale.Length - 1];

        /// <summary>
        /// The builder's script-level levers per difficulty (round 22e, the curator's decision 4; WaveTuning): the weight of plated
        /// troops in every fill (1 = as written), the tempo of every group's interval (below 1 faster), and a wave that opens with a
        /// small raid of fast, spread runners (0 = none). For cells the target scale cannot move, such as C1_01 Easy and Hard.
        /// </summary>
        public float[] PlatedBias = { 1f };

        /// <summary>Round 27: the weight of swarm troops (no armour, at most twice the health of the lightest troop) in every fill (1 = as written): the runners-and-swarm lever against a splash row.</summary>
        public float[] SwarmBias = { 1f };
        public float[] Tempo = { 1f };
        public int[] RaidWave = { 0 };

        public float PlatedBiasFor(int difficulty) => At(PlatedBias, difficulty, 1f);

        public float SwarmBiasFor(int difficulty) => At(SwarmBias, difficulty, 1f);

        public float TempoFor(int difficulty) => At(Tempo, difficulty, 1f);

        public int RaidWaveFor(int difficulty) => RaidWave == null || RaidWave.Length == 0 ? 0 : RaidWave[difficulty < RaidWave.Length ? difficulty : RaidWave.Length - 1];

        private static float At(float[] values, int difficulty, float fallback) => values == null || values.Length == 0 ? fallback : values[difficulty < values.Length ? difficulty : values.Length - 1];

        /// <summary>One letter per wave: P plain, R rush, H heavy, M ambush. A shorter string repeats.</summary>
        public string Rhythm = "P";
        public BossSpec[] Bosses = new BossSpec[0];

        /// <summary>Small fixed groups that preview a coming creature late in the level.</summary>
        public TeaserSpec[] Teasers = new TeaserSpec[0];
    }
}
