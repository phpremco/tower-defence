using Game.Combat;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>Everything a drone needs from the level to do its job, handed over once when it is launched.</summary>
    public readonly struct DroneWorld
    {
        public DroneWorld(Vector3 home, StructureRegistry structures, TargetRegistry targets, ProjectilePool projectiles, Vector3[] resourceNodes, float[] mineStocks)
        {
            MineStocks = mineStocks;
            Home = home;
            Structures = structures;
            Targets = targets;
            Projectiles = projectiles;
            ResourceNodes = resourceNodes;
        }

        /// <summary>Where the base is: drones start here and the harvest drone delivers here.</summary>
        public Vector3 Home { get; }

        public StructureRegistry Structures { get; }
        public TargetRegistry Targets { get; }
        public ProjectilePool Projectiles { get; }
        public Vector3[] ResourceNodes { get; }

        /// <summary>Ore left in each resource node, shared by every drone and drained as they mine.</summary>
        public float[] MineStocks { get; }
    }
}
