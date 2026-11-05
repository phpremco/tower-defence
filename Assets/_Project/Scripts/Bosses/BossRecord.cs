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
        public int Volleys { get; internal set; }
        public int PadsOvergrown { get; internal set; }
        public int PadsCleaned { get; internal set; }
        public int SporesShot { get; internal set; }
        public bool Uprooted { get; internal set; }
        public int Splits { get; internal set; }
        public int ShardsKilled { get; internal set; }
        public bool Rejoined { get; internal set; }

        /// <summary>The rejoined giant's health against its full health, at the moment it formed.</summary>
        public float RejoinedShare { get; internal set; }
        public bool RoseFromRift { get; internal set; }
        public int BaseCrushes { get; internal set; }

        /// <summary>Every tower type it learned, in order (one per phase change).</summary>
        public List<TowerDefinition> Learned { get; } = new List<TowerDefinition>();

        /// <summary>Every gate it drained, in order (one per phase).</summary>
        public List<Tower> Drained { get; } = new List<Tower>();

        /// <summary>Its shards, in order of the split.</summary>
        public List<Game.Enemies.Enemy> Shards { get; } = new List<Game.Enemies.Enemy>();

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
            Volleys = PadsOvergrown = PadsCleaned = SporesShot = Splits = ShardsKilled = BaseCrushes = 0;
            Uprooted = Rejoined = RoseFromRift = false;
            RejoinedShare = 0f;
            Learned.Clear();
            Drained.Clear();
            Shards.Clear();
            MostFieldShare = 0f;
            Thrown.Clear();
            Smashed.Clear();
        }
    }
}
