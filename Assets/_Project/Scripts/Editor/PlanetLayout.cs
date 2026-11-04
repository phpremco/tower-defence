using System.Collections.Generic;
using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// The level sites of S-69-CRYS as the planet was painted around them (<c>ArtSource/Exports/Planet_S69/layout.json</c>,
    /// version 22b: an organic route walked over the land, at least 6.1 degrees apart). Copied here so a fresh clone
    /// without ArtSource builds the same map; a continent without a painted route falls back to <see cref="NodePlacer"/>.
    /// Sites run in level order, from the landing site onward.
    /// </summary>
    public static class PlanetLayout
    {
        public const string Version = "22b";

        private static readonly GlobePoint[] CrystalPlains =
        {
            new GlobePoint(-56.07f, -11.07f), new GlobePoint(-49.75f, -10.37f), new GlobePoint(-44.82f, -4.04f), new GlobePoint(-37.79f, -4.04f),
            new GlobePoint(-28.65f, 0.88f), new GlobePoint(-20.21f, 0.88f), new GlobePoint(-16f, 6.5f), new GlobePoint(-12.48f, 12.13f),
            new GlobePoint(-18.81f, 12.13f), new GlobePoint(-24.43f, 8.61f), new GlobePoint(-32.87f, 6.5f), new GlobePoint(-41.31f, 5.1f),
            new GlobePoint(-49.04f, 2.29f), new GlobePoint(-57.48f, -1.93f), new GlobePoint(-61.7f, 2.99f), new GlobePoint(-55.37f, 4.39f),
            new GlobePoint(-48.34f, 8.61f), new GlobePoint(-43.42f, 13.54f), new GlobePoint(-37.79f, 10.72f), new GlobePoint(-29.36f, 12.83f),
        };

        private static readonly Dictionary<string, GlobePoint[]> ByContinent = new Dictionary<string, GlobePoint[]> { { "C1", CrystalPlains } };

        /// <summary>The painted sites of a continent by key (C1 ...), or null when its route is not painted yet.</summary>
        public static GlobePoint[] Sites(string continentKey) => ByContinent.TryGetValue(continentKey, out GlobePoint[] sites) ? (GlobePoint[])sites.Clone() : null;
    }
}
