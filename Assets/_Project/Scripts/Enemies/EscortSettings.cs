namespace Game.Enemies
{
    /// <summary>
    /// The escort rule of a level boss as play applies it (round 88b): what the brood it sends is worth over a whole walk,
    /// how it grows along the road, and what it pays. The difficulty gives the worth; the balance rules give the rest.
    /// </summary>
    public readonly struct EscortSettings
    {
        /// <summary>Health of the whole escort over an unhindered walk from spawn to the base, as a share of the boss's bar as spawned.</summary>
        public readonly float Worth;

        /// <summary>How fast the brood comes at the start of the walk against its mean rate.</summary>
        public readonly float StartWeight;

        /// <summary>How fast it comes at the end of the walk against its mean rate; above the start so the road behind the first defence gets worse.</summary>
        public readonly float EndWeight;

        /// <summary>Share of the escort's health that walks in as plated creatures, when the escort can pay for at least one.</summary>
        public readonly float HeavyShare;

        /// <summary>The most a boss ever sends, as a multiple of the full walk's worth: a boss held or slowed for long does not flood the field.</summary>
        public readonly float MostOfWorth;

        /// <summary>Share of its bounty an escort creature pays; zero keeps a boss's walk from being a source of money.</summary>
        public readonly float BountyShare;

        public EscortSettings(float worth, float startWeight, float endWeight, float heavyShare, float mostOfWorth, float bountyShare)
        {
            Worth = worth;
            StartWeight = startWeight;
            EndWeight = endWeight;
            HeavyShare = heavyShare;
            MostOfWorth = mostOfWorth;
            BountyShare = bountyShare;
        }
    }
}
