namespace Game.Balance
{
    /// <summary>How much of the map one element's terrain covers: a share of all cells, as ponds or patches off the road.</summary>
    public readonly struct TerrainShare
    {
        public TerrainShare(string element, float share)
        {
            Element = element;
            Share = share;
        }

        /// <summary>An element name (<see cref="LayoutTerrain"/>), or "Raised" for higher ground.</summary>
        public string Element { get; }

        public float Share { get; }
    }
}
