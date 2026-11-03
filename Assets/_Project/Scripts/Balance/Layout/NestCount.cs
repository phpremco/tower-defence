namespace Game.Balance
{
    /// <summary>How many nests of one element a level has.</summary>
    public readonly struct NestCount
    {
        public NestCount(string element, int count)
        {
            Element = element;
            Count = count;
        }

        public string Element { get; }

        public int Count { get; }
    }
}
