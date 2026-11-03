namespace Game.Balance
{
    /// <summary>A generated nest: where element creatures rise, and which element.</summary>
    public readonly struct LayoutNest
    {
        public LayoutNest(Cell cell, string element)
        {
            Cell = cell;
            Element = element;
        }

        public Cell Cell { get; }

        public string Element { get; }
    }
}
