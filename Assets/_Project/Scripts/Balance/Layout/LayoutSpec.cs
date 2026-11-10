namespace Game.Balance
{
    /// <summary>
    /// What a generated map must look like: the road template and seed, the grid, how many gates, plazas and nests, and
    /// how much of the ground each element covers. <see cref="RoadGenerator"/> turns it into a <see cref="RoadLayout"/>
    /// and <see cref="LayoutChecks"/> decides whether that layout plays fairly.
    /// </summary>
    public sealed class LayoutSpec
    {
        public string Key = string.Empty;
        public RoadTemplate Template = RoadTemplate.Serpentine;
        public int Seed;
        public int Width = 36;
        public int Height = 22;
        public int RoadWidth = 2;

        /// <summary>Horizontal legs of a serpentine (odd, so the last runs east to the base) or columns of a hook.</summary>
        public int Legs = 3;

        /// <summary>Blockades along the road, three to five, spread over the whole walk.</summary>
        public int Gates = 4;

        /// <summary>
        /// Share of the walk before the first free gate (the defence gate of the plan model). 0.12 puts it just past the first
        /// legs. Round 29 tried 0.3 on Quartz Flats; in play the unheld approach made the level far harder, so every level keeps
        /// the default (round 30) and an open opening is made with <see cref="OpeningClearShare"/> instead.
        /// </summary>
        public double FirstGateShare = 0.12;

        /// <summary>
        /// Share of the main road's walk, from the spawn, that terrain patches keep clear of (within <see cref="RoadGenerator.OpeningClearance"/>
        /// cells): every cell beside the opening legs stays buildable, so the spots there fire before the first gate holds
        /// anything. 0 places terrain everywhere (the default); Quartz Flats 0.4 (round 30, Remco: fix a hard level by its map).
        /// </summary>
        public double OpeningClearShare;

        /// <summary>Plazas where roads meet: zero or one, only on templates with a second road.</summary>
        public int Plazas;

        public TerrainShare[] Terrain = new TerrainShare[0];
        public NestCount[] Nests = new NestCount[0];

        /// <summary>Most ground fronts the level may ask the player to hold (the continent's tempo; Introduction is 2).</summary>
        public int MaxFronts = 2;

        public bool HasSecondRoad => Template == RoadTemplate.Fork || Template == RoadTemplate.LateJoin;
    }
}
