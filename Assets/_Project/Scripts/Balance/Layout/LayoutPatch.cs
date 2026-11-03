namespace Game.Balance
{
    /// <summary>One cell of generated terrain: its kind as <see cref="LayoutTerrain"/> numbers it.</summary>
    public readonly struct LayoutPatch
    {
        public LayoutPatch(Cell cell, int kind)
        {
            Cell = cell;
            Kind = kind;
        }

        public Cell Cell { get; }

        public int Kind { get; }
    }
}
