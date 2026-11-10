namespace Game.Levels
{
    /// <summary>
    /// The order the shoulder buttons (LB / RB, Q / E) walk the build bar in: the towers, then the street weapons, then
    /// round again. Positions count the towers first (0 to towers - 1), then the street weapons.
    /// </summary>
    public static class SelectionCycle
    {
        /// <summary>The bar position one step on from the current one; -1 when the bar is empty.</summary>
        public static int Next(int current, int towers, int streetWeapons, int direction)
        {
            int total = towers + streetWeapons;
            if (total <= 0)
            {
                return -1;
            }

            int from = current >= 0 && current < total ? current : 0;
            int step = direction >= 0 ? 1 : -1;
            return (from + step + total) % total;
        }
    }
}
