using System.Collections.Generic;
using Game.Towers;

namespace Game.Bosses
{
    /// <summary>What one boss did in its fight, for the probes, the tests and the views: counts only, no behaviour.</summary>
    public sealed class BossRecord
    {
        public int Burrows { get; internal set; }
        public int Dives { get; internal set; }
        public int Climbs { get; internal set; }
        public int Hunts { get; internal set; }
        public int EggsLaid { get; internal set; }
        public int EggsHatched { get; internal set; }
        public int EggsShot { get; internal set; }
        public int GatesFrozen { get; internal set; }
        public int GatesShattered { get; internal set; }
        public int GatesThawed { get; internal set; }
        public int ArmourCracks { get; internal set; }
        public bool ArmourBroken { get; internal set; }
        public int Rage { get; internal set; }

        /// <summary>Every tower it threw, in order.</summary>
        public List<Tower> Thrown { get; } = new List<Tower>();

        /// <summary>Every tower it smashed into a wreck, in order.</summary>
        public List<Tower> Smashed { get; } = new List<Tower>();

        /// <summary>The most towers its effects reached at one moment (smashed, frozen) against the towers standing then.</summary>
        public float MostFieldShare { get; internal set; }

        public void Clear()
        {
            Burrows = Dives = Climbs = Hunts = 0;
            EggsLaid = EggsHatched = EggsShot = 0;
            GatesFrozen = GatesShattered = GatesThawed = ArmourCracks = Rage = 0;
            ArmourBroken = false;
            MostFieldShare = 0f;
            Thrown.Clear();
            Smashed.Clear();
        }
    }
}
