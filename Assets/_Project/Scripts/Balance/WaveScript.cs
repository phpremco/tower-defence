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

        /// <summary>Wave numbers (from 1) that become air raids; a difficulty takes the first few of them.</summary>
        public int[] AirWaves = new int[0];
        public int[] AirWavesPerDifficulty = { 0, 0, 0, 0 };

        /// <summary>From this difficulty index on, plain waves after the first air raid also carry a few flyers.</summary>
        public int AirSprinkleFrom = 4;

        /// <summary>One letter per wave: P plain, R rush, H heavy, M ambush. A shorter string repeats.</summary>
        public string Rhythm = "P";
        public BossSpec[] Bosses = new BossSpec[0];
    }
}
