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

        /// <summary>
        /// For a troop out of the element nests: the level's nest every group of it comes out of, counted from 1 in the level's
        /// nests (round 74); 0 lets each creature draw a nest of its element. Author it where the troop comes in ones and twos
        /// and the level has more than one nest for it (the level validator insists), so the rift it uses is the same on every run.
        /// </summary>
        public int Nest;

        /// <summary>Index of the easiest difficulty this troop joins on; easy may be spared a species.</summary>
        public int MinDifficulty;

        /// <summary>Index of the hardest difficulty this troop joins on (round 68: a troop that comes later on the easier difficulties only).</summary>
        public int MaxDifficulty = int.MaxValue;

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
        /// 22f, <see cref="CampaignModel.EasyRaidFloor"/>: no raid before the expected player can afford a sky gun, plus a wave;
        /// the first-flyer rule, round 77, <see cref="BalanceRules.FlyersAfterBounties"/>).
        /// </summary>
        public int[] FirstAirWaveFloor = { 0 };

        public int FirstAirWaveFloorFor(int difficulty) => FirstAirWaveFloor == null || FirstAirWaveFloor.Length == 0 ? 0 : FirstAirWaveFloor[difficulty < FirstAirWaveFloor.Length ? difficulty : FirstAirWaveFloor.Length - 1];

        /// <summary>
        /// Per difficulty, the earliest wave a flying troop may join (0 = as written). The solver owns it: the first-flyer rule
        /// (round 77, <see cref="BalanceRules.FlyersAfterBounties"/>) holds the troops back with the raids, because it is about
        /// the first flyer a player meets, whatever sends it. Apart from <see cref="FirstAirWaveFloor"/>: the Easy raid rule
        /// moves raids only and leaves a nest's few flyers where the script wrote them.
        /// </summary>
        public int[] FlyingTroopFloor = { 0 };

        public int FlyingTroopFloorFor(int difficulty) => FlyingTroopFloor == null || FlyingTroopFloor.Length == 0 ? 0 : FlyingTroopFloor[difficulty < FlyingTroopFloor.Length ? difficulty : FlyingTroopFloor.Length - 1];

        /// <summary>
        /// The builder bot's lever (round 22, WaveTuning): a scale on the human target pressure per difficulty (Easy to Psycho; a
        /// shorter array repeats its last entry). 1 is the curve; the builder moves a failing cell by small steps, at most twice.
        /// </summary>
        public float[] TargetScale = { 1f };

        public float TargetScaleFor(int difficulty) => TargetScale == null || TargetScale.Length == 0 ? 1f : TargetScale[difficulty < TargetScale.Length ? difficulty : TargetScale.Length - 1];

        /// <summary>Waves of grace per difficulty after a counter trait first appears (the combat rules' window; the generator writes both from one table).</summary>
        public int[] GraceWaves = { 0 };

        /// <summary>Waves after the grace window within which a defence without the counter must have lost; the growth runs through them.</summary>
        public int[] FallWithinWaves = { 0 };

        public int FallWithinWavesFor(int difficulty) => FallWithinWaves == null || FallWithinWaves.Length == 0 ? 0 : FallWithinWaves[difficulty < FallWithinWaves.Length ? difficulty : FallWithinWaves.Length - 1];

        public int GraceWavesFor(int difficulty) => GraceWaves == null || GraceWaves.Length == 0 ? 0 : GraceWaves[difficulty < GraceWaves.Length ? difficulty : GraceWaves.Length - 1];

        /// <summary>The composer's memory of the wave each counter trait first appears in, per difficulty (<see cref="WaveComposer.Growth"/>).</summary>
        internal readonly System.Collections.Generic.Dictionary<long, int> FirstTraitWaves = new System.Collections.Generic.Dictionary<long, int>();

        /// <summary>The grammar's memory of the waves that carry a surprise, per difficulty (<see cref="WaveGrammar.SurpriseOn"/>).</summary>
        internal readonly System.Collections.Generic.Dictionary<long, SurpriseKind[]> SurprisePlans = new System.Collections.Generic.Dictionary<long, SurpriseKind[]>();

        private static float At(float[] values, int difficulty, float fallback) => values == null || values.Length == 0 ? fallback : values[difficulty < values.Length ? difficulty : values.Length - 1];

        /// <summary>
        /// One letter per wave: P plain (a stream in clusters, later a combo swarm), R rush (a swarm), H heavy (a tank with its
        /// escort). A shorter string repeats. Air raids, surprises and the layout of a boss wave are not written here: the wave
        /// grammar places them (<see cref="WaveGrammar"/>, Docs/WAVE_GRAMMAR.md).
        /// </summary>
        public string Rhythm = "P";
        public BossSpec[] Bosses = new BossSpec[0];

        /// <summary>Small fixed groups that preview a coming creature late in the level.</summary>
        public TeaserSpec[] Teasers = new TeaserSpec[0];

        /// <summary>
        /// The level's own surprises, per wave (round 81): each replaces the grammar's default for its wave on the
        /// difficulties it names. Empty leaves every wave to the grammar's rules.
        /// </summary>
        public SurpriseSpec[] Surprises = new SurpriseSpec[0];

        /// <summary>
        /// The main roads the level writes per wave (round 82): each replaces the grammar's default choice for its wave on
        /// the difficulties it names. Empty leaves every wave to the grammar's rule (<see cref="WaveGrammar.RoadsOn"/>).
        /// </summary>
        public RoadSpec[] RoadPlan = new RoadSpec[0];

        /// <summary>The roads the level writes for this wave on this difficulty (the first that applies), or null.</summary>
        public RoadSpec RoadsFor(int number, int difficulty)
        {
            foreach (RoadSpec roads in RoadPlan)
            {
                if (roads.AppliesTo(number, difficulty))
                {
                    return roads;
                }
            }

            return null;
        }

        /// <summary>The surprise the level writes for this wave on this difficulty (the first that applies), or null.</summary>
        public SurpriseSpec SurpriseFor(int number, int difficulty)
        {
            foreach (SurpriseSpec surprise in Surprises)
            {
                if (surprise.AppliesTo(number, difficulty))
                {
                    return surprise;
                }
            }

            return null;
        }
    }
}
