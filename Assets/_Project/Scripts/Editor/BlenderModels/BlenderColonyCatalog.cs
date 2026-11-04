using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.PathObjects;

namespace Game.Editor
{
    /// <summary>
    /// Which Blender exports of our own side the game imports in stage 3a (README rounds 9, 11 and 14): the gate, the base,
    /// the three drones, the path objects and the resource node, and which definition wears each. Exports that no game
    /// data uses yet are built as ready art without an owner (the R.A.T. and its carrier).
    /// </summary>
    public static class BlenderColonyCatalog
    {
        public const string GateAsset = "Gate";
        public const string BaseAsset = "Base";
        public const string ResourceNodeAsset = "Env/ResourceNode";
        public static readonly string[] GateLevels = { "G1", "G2", "G3" };
        public const string GateWreck = "Gate_Wreck";
        public const string BaseDamage = "Base_Damage";
        public const string BaseWreck = "Base_Wreck";

        /// <summary>The model each drone kind wears.</summary>
        public static readonly IReadOnlyDictionary<DroneKind, string> Drones = new Dictionary<DroneKind, string>
        {
            { DroneKind.Repair, "Drone_Repair" },
            { DroneKind.Harvest, "Drone_Harvest" },
            { DroneKind.Attack, "Drone_Attack" },
        };

        /// <summary>The model each path object look wears; looks not listed keep their procedural model.</summary>
        public static readonly IReadOnlyDictionary<PathObjectLook, string> PathObjects = new Dictionary<PathObjectLook, string>
        {
            { PathObjectLook.Mine, "Gadget_Mine" },
            { PathObjectLook.Chrono, "Gadget_Chrono" },
            { PathObjectLook.LavaBall, "Gadget_LavaBall" },
            { PathObjectLook.GroundDrone, "Gadget_GroundDrone" },
            { PathObjectLook.Barrel, "Gadget_Barrel" },
            { PathObjectLook.AirNet, "Gadget_AirNet" },
        };

        /// <summary>Built ahead of their data: no path object or drone wears them yet.</summary>
        public static readonly string[] Unworn = { "Gadget_RAT", "Gadget_RATCarrier" };

        /// <summary>What each take is for, by its name in the exports.</summary>
        public static readonly IReadOnlyDictionary<string, ModelClipRole> Roles = new Dictionary<string, ModelClipRole>
        {
            { "Idle", ModelClipRole.Idle },
            { "Fly", ModelClipRole.Move },
            { "Drive", ModelClipRole.Move },
            { "Carry", ModelClipRole.Move },
            { "Work", ModelClipRole.Work },
            { "Hit", ModelClipRole.Hit },
            { "Repair", ModelClipRole.Repair },
            { "Build", ModelClipRole.Build },
            { "Collapse", ModelClipRole.Death },
            { "Arm", ModelClipRole.Deploy },
            { "Deploy", ModelClipRole.Deploy },
            { "Drop", ModelClipRole.Deploy },
            { "Land", ModelClipRole.Deploy },
            { "Fire", ModelClipRole.Fire },
        };

        /// <summary>Every export folder stage 3a reads.</summary>
        public static IEnumerable<string> SyncedAssets()
        {
            yield return GateAsset;
            yield return BaseAsset;
            yield return ResourceNodeAsset;
            foreach (string drone in Drones.Values)
            {
                yield return drone;
            }

            foreach (string gadget in PathObjects.Values)
            {
                yield return gadget;
            }

            foreach (string gadget in Unworn)
            {
                yield return gadget;
            }
        }

        /// <summary>The further model files beside an export's own FBX: the gate levels and wreck, the base overlay and wreck.</summary>
        public static IReadOnlyDictionary<string, string[]> ExtraModels()
        {
            var levels = new List<string>();
            foreach (string level in GateLevels)
            {
                levels.Add($"{GateAsset}_{level}.fbx");
            }

            levels.Add(GateWreck + ".fbx");
            return new Dictionary<string, string[]>
            {
                { GateAsset, levels.ToArray() },
                { BaseAsset, new[] { BaseDamage + ".fbx", BaseWreck + ".fbx" } },
            };
        }
    }
}
