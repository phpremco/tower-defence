using System.Collections.Generic;
using Game.Towers;

namespace Game.Editor
{
    /// <summary>
    /// Which Blender tower exports the game imports and which tower definition wears each (import stage 2). Tiers, aim,
    /// shot origins and wreck debris follow <c>ArtSource/README.md</c> rounds 7, 9 and 11; the look numbers are tuned
    /// on in-game screenshots (imp2_*).
    /// </summary>
    public static class BlenderTowerCatalog
    {
        public const string WreckAsset = "Tower_Wreck";
        public const string StarAsset = "Tower_Star";

        private static List<BlenderTowerEntry> _towers;

        /// <summary>The tower rows, in roster order.</summary>
        public static IReadOnlyList<BlenderTowerEntry> Towers => _towers ??= CreateTowers();

        /// <summary>Every export folder the tower rows read, with the wreck and the star.</summary>
        public static IEnumerable<string> SyncedAssets()
        {
            foreach (BlenderTowerEntry entry in Towers)
            {
                yield return entry.Asset;
            }

            yield return WreckAsset;
            yield return StarAsset;
        }

        /// <summary>The tier files of every tower, by export folder (they sit beside the folder's own name).</summary>
        public static IReadOnlyDictionary<string, string[]> TierFiles()
        {
            var files = new Dictionary<string, string[]>();
            foreach (BlenderTowerEntry entry in Towers)
            {
                var names = new string[entry.Tiers.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = entry.ModelFile(entry.Tiers[i]);
                }

                files[entry.Asset] = names;
            }

            return files;
        }

        private static List<BlenderTowerEntry> CreateTowers()
        {
            string[] paths = BlenderTowerEntry.PathTiers;
            string[] plain = BlenderTowerEntry.PlainTiers;
            return new List<BlenderTowerEntry>
            {
                new BlenderTowerEntry("PulseRifle", "Tower_Gun", paths) { Debris = "Debris_Gun" },
                new BlenderTowerEntry("Cannon", "Tower_Cannon", paths) { Debris = "Debris_Cannon" },
                new BlenderTowerEntry("Laser", "Tower_Laser", paths) { Debris = "Debris_Laser" },
                // The Flak keeps the old Rocket id; its barrels rest at their modelled 40 degrees.
                new BlenderTowerEntry("Rocket", "Tower_Flak", paths) { Aim = TowerAimMode.YawOnly, Debris = "Debris_Flak" },
                // The gate's gun: its stars show on the beam (the gate has stars before it has a gun); no wreck of its own,
                // the gate collapses.
                new BlenderTowerEntry("Blockade", "Tower_GateGun", plain) { HideStars = true },
                new BlenderTowerEntry("BaseTurret", "Tower_BaseTurret", plain) { Debris = "Debris_Gun" },
                // The coil never aims: its idle clip turns it, and lightning leaves from the arc prong nearest the target.
                new BlenderTowerEntry("Tesla", "Tower_Tesla", paths)
                {
                    Aim = TowerAimMode.None,
                    MuzzlePrefix = "Arc",
                    MuzzleChoice = TowerMuzzleChoice.Nearest,
                    Debris = "Debris_Laser",
                },
                // The plasma channel brightens while the rails draw back (README round 11).
                new BlenderTowerEntry("Railgun", "Tower_Railgun", paths) { ChargeGlow = 2.5f, Debris = "Debris_Gun" },
                new BlenderTowerEntry("Cryo", "Tower_Cryo", plain) { Debris = "Debris_Laser" },
                new BlenderTowerEntry("Support", "Tower_Support", plain) { Aim = TowerAimMode.None, Debris = "Debris_Laser" },
                new BlenderTowerEntry("Artillery", "Tower_Artillery", plain) { Aim = TowerAimMode.YawOnly, Debris = "Debris_Cannon" },
                new BlenderTowerEntry("StormRifle", "Tower_StormRifle", plain) { Debris = "Debris_Gun" },
                new BlenderTowerEntry("CryoBeam", "Tower_CryoBeam", plain) { Debris = "Debris_Laser" },
                // Built ahead of their towers: no definition wears them yet.
                new BlenderTowerEntry(null, "Tower_Acid", paths) { Debris = "Debris_Cannon" },
                new BlenderTowerEntry(null, "Tower_Rocket", plain)
                {
                    Aim = TowerAimMode.YawOnly,
                    MuzzlePrefix = "Tube",
                    Debris = "Debris_Flak",
                },
            };
        }
    }
}
