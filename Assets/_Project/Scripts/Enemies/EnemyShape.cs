namespace Game.Enemies
{
    /// <summary>Which procedural creature an enemy wears. None keeps whatever imported model or placeholder it has.</summary>
    public enum EnemyShape
    {
        None = 0,

        /// <summary>The hive queen: a swollen abdomen full of eggs on long legs.</summary>
        Queen = 7,

        /// <summary>The armadillo-like walker that curls up and rolls.</summary>
        Roller = 8,

        /// <summary>The ant: three segments, big jaws, comes in crowds.</summary>
        Skitter = 20,

        /// <summary>The scorpion: pincers, a tail over its back and glowing plates.</summary>
        Stinger = 21,

        /// <summary>The element bat: a small body under wide membrane wings.</summary>
        Bat = 22,

        /// <summary>The upright shield bearer with a crystal sword.</summary>
        Bulwark = 23,

        /// <summary>The pack hunter: a lean four-legged runner with a spined back.</summary>
        Prowler = 24,

        /// <summary>The energy ball: a core with rings turning around it.</summary>
        Wisp = 25,

        /// <summary>The flying ray: a flat body between two broad fins and a spiked tail.</summary>
        Skyray = 26,

        /// <summary>The slug: a soft body on a slime foot with eyes on stalks.</summary>
        Oozer = 27,

        /// <summary>The ice beetle: a hunched six-legged crawler with a back of ice prisms that shatter off.</summary>
        Shardback = 28,

        /// <summary>The marsh leech: a long low segmented body with a sucker mouth, leaving a mud trail.</summary>
        Mireleech = 29,

        BroodMother = 30,
        AcidEmpress = 31,
        StormMatriarch = 32,
        CrystalWarlord = 33,
        PackAlpha = 34,
        CoreWisp = 35,
        SkySovereign = 36,
        SlimeMonarch = 37,
        RollerKing = 38,

        /// <summary>The stone crawler: a hunched boulder of a body on six stubby legs, basalt plates split by glowing veins.</summary>
        Cragback = 39,
    }
}
