namespace Game.Levels
{
    /// <summary>
    /// A signature moment of a continent boss fight that the hero may call out (Docs/BOSS_INTROS.md, mid-fight barks).
    /// The boss framework counts them; the intro data pairs each with a line.
    /// </summary>
    public enum BossMoment
    {
        /// <summary>It flung a tower up out of the ground.</summary>
        Throw = 0,

        /// <summary>It went under the ground.</summary>
        Burrow = 1,

        /// <summary>It smashed the tower it hunted.</summary>
        Smash = 2,

        /// <summary>It laid eggs.</summary>
        Eggs = 3,

        /// <summary>It froze a gate into a block of ice.</summary>
        GateFreeze = 4,

        /// <summary>Its armour came off for good.</summary>
        ArmourBreak = 5,

        /// <summary>It overgrew a pad of towers.</summary>
        Overgrow = 6,

        /// <summary>It tore itself out of the ground and walks.</summary>
        Uproot = 7,

        /// <summary>It burst into parts.</summary>
        Split = 8,

        /// <summary>Its parts came back together.</summary>
        Rejoin = 9,

        /// <summary>It adapted to a weapon that hurt it.</summary>
        Adapt = 10,

        /// <summary>It climbed onto the base and struck it.</summary>
        OnBase = 11,
    }
}
