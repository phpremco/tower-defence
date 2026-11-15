using System.Collections.Generic;
using System.Text;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the armour rule (round 64): what every weapon's damage is made of. One table for all of them
    /// (towers, the hero's weapons and abilities, the attack drone, the path objects); the share of a hit that works on
    /// armour follows from the type (<see cref="BalanceSheet.ArmourFactorOf"/>), so no weapon carries a hand-typed armour
    /// factor and a new weapon cannot forget one (the attack drone's shots ignored armour until this round).
    /// </summary>
    public static partial class ProjectGenerator
    {
        /// <summary>Asset path under the data root, without the extension, to the weapon's damage type.</summary>
        public static readonly IReadOnlyDictionary<string, DamageType> WeaponDamageTypes = new Dictionary<string, DamageType>
        {
            { "Towers/PulseRifle", DamageType.Bullet },
            { "Towers/BaseTurret", DamageType.Bullet },
            { "Towers/Blockade", DamageType.Bullet },
            { "Towers/Cannon", DamageType.Explosive },
            { "Towers/Artillery", DamageType.Explosive },
            // The Flak's shells burst; until round 64 they were rated half on armour like an arc.
            { "Towers/Rocket", DamageType.Explosive },
            { "Towers/Laser", DamageType.Beam },
            { "Towers/Cryo", DamageType.Beam },
            { "Towers/CryoBeam", DamageType.Beam },
            { "Towers/Tesla", DamageType.Arc },
            { "Towers/StormRifle", DamageType.Arc },
            { "Towers/Railgun", DamageType.Piercing },
            { "Hero/Weapon_Blaster", DamageType.Bullet },
            // The blade was heavy (one) until round 64; it does not explode, so armour turns half of it.
            { "Hero/Weapon_EnergySword", DamageType.Blade },
            { "Hero/Abilities/Ability_Shockwave", DamageType.Explosive },
            { "Drones/AttackDrone", DamageType.Bullet },
            { "PathObjects/Mine", DamageType.Explosive },
            { "PathObjects/ExplosiveBarrel", DamageType.Explosive },
            { "PathObjects/LavaBall", DamageType.Burn },
            { "PathObjects/GroundDrone", DamageType.Ram },
        };

        /// <summary>Writes a weapon's damage type and the armour factor that follows from it; an asset outside the table keeps a blast's full share.</summary>
        private static void StampDamage(SerializedObject so, string key)
        {
            DamageType type = WeaponDamageTypes.TryGetValue(key, out DamageType listed) ? listed : DamageType.Explosive;
            so.FindProperty("_damageType").enumValueIndex = (int)type;
            so.FindProperty("_armourFactor").floatValue = BalanceSheet.ArmourFactorOf(type);
        }

        /// <summary>
        /// Stamps every weapon asset in the table and nothing else: the round's targeted step, safe to run again. Returns
        /// one line per weapon whose factor changed.
        /// </summary>
        public static string ApplyDamageTypes()
        {
            var report = new StringBuilder();
            foreach (KeyValuePair<string, DamageType> entry in WeaponDamageTypes)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{DataRoot}/{entry.Key}.asset");
                if (asset == null)
                {
                    report.Append($"MISSING {entry.Key}\n");
                    continue;
                }

                var so = new SerializedObject(asset);
                float before = so.FindProperty("_armourFactor").floatValue;
                StampDamage(so, entry.Key);
                float after = so.FindProperty("_armourFactor").floatValue;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                report.Append($"{entry.Key}: {entry.Value}, armour factor {before:0.##} -> {after:0.##}{(Mathf.Approximately(before, after) ? string.Empty : "  CHANGED")}\n");
            }

            return report.ToString();
        }
    }
}
