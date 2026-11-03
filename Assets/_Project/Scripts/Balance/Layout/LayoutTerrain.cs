namespace Game.Balance
{
    /// <summary>
    /// Terrain kinds as the game stores them (<c>Game.Levels.TerrainKind</c>, same numbers) and the terrain an element's
    /// nest lives in (<c>LevelValidator.NestTerrain</c>), kept here so the layout generator needs no engine types.
    /// A test holds both tables equal.
    /// </summary>
    public static class LayoutTerrain
    {
        public const int Ground = 0;
        public const int Raised = 1;
        public const int Water = 2;
        public const int Lava = 3;
        public const int Quicksand = 4;
        public const int Ice = 5;
        public const int Bog = 6;
        public const int Rift = 7;

        /// <summary>The highest kind number, for tables indexed by kind.</summary>
        public const int Last = Rift;

        /// <summary>The name a terrain share uses for higher ground, which belongs to no element.</summary>
        public const string RaisedName = "Raised";

        /// <summary>The terrain kind an element's ground is made of: a liquid, raised ground, or plain ground (elements without a liquid).</summary>
        public static int KindOf(string element)
        {
            switch (element)
            {
                case "Water":
                    return Water;
                case "Fire":
                    return Lava;
                case "Sand":
                    return Quicksand;
                case "Ice":
                    return Ice;
                case "Bog":
                    return Bog;
                case "Stone":
                    return Rift;
                case RaisedName:
                    return Raised;
                default:
                    return Ground;
            }
        }

        /// <summary>True for the kinds nothing can be built on and creatures can rise from.</summary>
        public static bool IsLiquid(int kind) => kind >= Water;
    }
}
