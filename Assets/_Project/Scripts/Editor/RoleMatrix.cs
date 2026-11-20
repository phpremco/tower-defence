using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Drones;
using Game.Enemies;
using Game.Hero;
using Game.PathObjects;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The role matrix (round 67): every damage source in the data against the reference fight of every creature trait
    /// (<see cref="CombatRules.Scenarios"/>), played through the game's own <see cref="Health"/> and the weapon's own
    /// <see cref="DamageProfile"/>, so the armour pool, the air share, the blast cap, the wasted part of a big hit on a
    /// small creature and the unguided shell are all in the number. The role tests assert on it and the rules page
    /// (Docs/COMBAT_RULES.md) prints it. It reads the assets it finds, so a new weapon or creature is in it by existing,
    /// and it takes the rules it is handed, so a test can ask what a changed rule would break.
    /// </summary>
    public static class RoleMatrix
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const float CoinUnit = 100f;

        // A shot is fired from about this share of the tower's reach: how long an unguided shell is in the air.
        private const float TypicalShotReach = 0.7f;

        // Past this many shots a source is not clearing the group at all.
        private const int MaxShots = 50000;

        public enum SourceKind
        {
            Tower,
            Hybrid,
            Structure,
            Hero,
            Drone,
            PathObject,
        }

        /// <summary>One damage source at one level, with everything the reference fights need.</summary>
        public sealed class Source
        {
            public string Id;
            public string Name;
            public SourceKind Kind;

            /// <summary>The tower behind it, or null.</summary>
            public TowerDefinition Tower;
            public int Level;

            /// <summary>Coins it stands for: everything spent to reach the level (a hybrid: both parents and the recipe); a free helper the rules' reference coins.</summary>
            public int Coins;
            public float Damage;
            public float Interval;
            public DamageProfile Profile;
            public TargetLayers Targets;
            public float SplashRadius;
            public int SplashTargets;
            public bool Homing = true;
            public float ProjectileSpeed = 1f;
            public float Range = 1f;

            /// <summary>A path object: spent after one use (a mine's blast, a roller's one pass).</summary>
            public bool SingleUse;

            /// <summary>A roller: its speed along the road; zero for everything that stands still.</summary>
            public float RollSpeed;

            public bool IsHelper => Kind != SourceKind.Tower && Kind != SourceKind.Hybrid;
        }

        /// <summary>The towers' numbers scaled for a later moment: every permanent upgrade bought.</summary>
        public readonly struct Boost
        {
            public static readonly Boost None = new Boost(1f, 1f);

            public Boost(float damage, float rate)
            {
                Damage = damage;
                Rate = rate;
            }

            public float Damage { get; }
            public float Rate { get; }
        }

        public static CombatRules Rules() => AssetDatabase.LoadAssetAtPath<CombatRules>(CombatRules.AssetPath);

        public static EnemyDefinition Enemy(string id) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{DataRoot}/Enemies/{id}.asset");

        public static EnemyTraits TraitsOf(CombatRules rules, EnemyDefinition enemy) =>
            rules.TraitsOf(enemy.MaxHealth, enemy.Armor, enemy.AverageSpeed, enemy.Layer, enemy.IsBoss);

        /// <summary>Every tower the player can build or combine, at a level; gates and the base's turret come as structures.</summary>
        public static List<Source> Towers(CombatRules rules, int level)
        {
            var recipes = new List<CombineRecipe>();
            foreach (string guid in AssetDatabase.FindAssets("t:CombineRecipe", new[] { DataRoot + "/Recipes" }))
            {
                recipes.Add(AssetDatabase.LoadAssetAtPath<CombineRecipe>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            var sources = new List<Source>();
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { DataRoot + "/Towers" }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                int at = Mathf.Min(level, tower.MaxLevelIndex);
                TowerLevel stats = tower.GetLevel(at);
                if (tower.IsSupport || stats.Damage <= 0f)
                {
                    continue;
                }

                CombineRecipe recipe = recipes.Find(r => r.Result == tower);
                int coins = recipe != null ? recipe.Cost + recipe.First.BuildCost + recipe.Second.BuildCost : 0;
                for (int i = 0; i <= at; i++)
                {
                    coins += tower.GetLevel(i).Cost;
                }

                bool structure = tower.BlocksGroundEnemies || tower.IsBaseDefence;
                sources.Add(new Source
                {
                    Id = tower.name,
                    Name = tower.DisplayName,
                    Kind = structure ? SourceKind.Structure : recipe != null ? SourceKind.Hybrid : SourceKind.Tower,
                    Tower = tower,
                    Level = at,
                    Coins = structure ? Mathf.Max(coins, rules.FreeHelperCoins) : coins,
                    Damage = stats.Damage,
                    Interval = stats.FireInterval,
                    Profile = rules.ProfileOf(tower.DamageType, tower.name),
                    Targets = tower.Targets,
                    SplashRadius = tower.SplashRadius,
                    SplashTargets = tower.SplashTargets,
                    Homing = tower.Homing,
                    ProjectileSpeed = tower.ProjectileSpeed,
                    Range = stats.Range,
                });
            }

            sources.Sort((a, b) => a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.Coins.CompareTo(b.Coins));
            return sources;
        }

        /// <summary>Everything else that hurts a creature: the hero's blade, blaster and shockwave, the attack drone, the path objects.</summary>
        public static List<Source> Helpers(CombatRules rules)
        {
            var sources = new List<Source>();
            foreach (string guid in AssetDatabase.FindAssets("t:HeroWeaponDefinition", new[] { DataRoot + "/Hero" }))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<HeroWeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                // A magazine spreads its reload over its shots.
                float interval = weapon.Magazine > 0 ? weapon.AttackInterval + weapon.ReloadSeconds / weapon.Magazine : weapon.AttackInterval;
                sources.Add(new Source
                {
                    Id = weapon.name, Name = "Spencer: " + weapon.DisplayName, Kind = SourceKind.Hero, Coins = rules.FreeHelperCoins,
                    Damage = weapon.Damage, Interval = interval, Profile = rules.ProfileOf(weapon.DamageType, weapon.name), Targets = weapon.Targets, Range = weapon.Reach,
                });
            }

            foreach (string guid in AssetDatabase.FindAssets("t:HeroAbilityDefinition", new[] { DataRoot + "/Hero" }))
            {
                var ability = AssetDatabase.LoadAssetAtPath<HeroAbilityDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (ability.Kind != HeroAbilityKind.Shockwave)
                {
                    continue;
                }

                sources.Add(new Source
                {
                    Id = ability.name, Name = "Spencer: " + ability.DisplayName, Kind = SourceKind.Hero, Coins = rules.FreeHelperCoins,
                    Damage = ability.Damage, Interval = ability.Cooldown, Profile = rules.ProfileOf(ability.DamageType, ability.name), Targets = ability.Targets,
                    SplashRadius = ability.Radius, Range = ability.Radius,
                });
            }

            foreach (string guid in AssetDatabase.FindAssets("t:DroneDefinition", new[] { DataRoot + "/Drones" }))
            {
                var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (drone.Kind != DroneKind.Attack)
                {
                    continue;
                }

                sources.Add(new Source
                {
                    Id = drone.name, Name = drone.DisplayName, Kind = SourceKind.Drone, Coins = drone.Cost,
                    Damage = drone.Damage, Interval = drone.FireInterval, Profile = rules.ProfileOf(drone.DamageType, drone.name), Targets = drone.Targets,
                    ProjectileSpeed = drone.ProjectileSpeed, Range = drone.Range,
                });
            }

            foreach (string guid in AssetDatabase.FindAssets("t:PathObjectDefinition", new[] { DataRoot + "/PathObjects" }))
            {
                var gadget = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (gadget.Damage <= 0f)
                {
                    continue;
                }

                sources.Add(new Source
                {
                    Id = gadget.name, Name = gadget.DisplayName, Kind = SourceKind.PathObject, Coins = gadget.Cost,
                    Damage = gadget.Damage, Interval = 1f, Profile = rules.ProfileOf(gadget.DamageType, gadget.name), Targets = gadget.Targets,
                    SplashRadius = gadget.Radius, Range = gadget.Radius, SingleUse = true,
                    RollSpeed = gadget.Kind == PathObjectKind.Roller ? gadget.Speed : 0f,
                });
            }

            return sources;
        }

        /// <summary>
        /// Seconds the source needs to kill the scenario's group, firing without a pause; infinity when it cannot (it
        /// cannot aim at the creature's layer, its damage never reaches it, or its shell lands where the runner was).
        /// </summary>
        public static float ClearSeconds(Source source, RoleScenario scenario, EnemyDefinition enemy, float healthScale, Boost boost)
        {
            if (source.SingleUse || (source.Targets & enemy.Layer) == 0 || source.Damage * source.Profile.Against(enemy.Layer, enemy.Family) <= 0f)
            {
                return float.PositiveInfinity;
            }

            Health[] group = Group(scenario, enemy, healthScale);
            float damage = source.Damage * boost.Damage;
            float interval = source.Interval / boost.Rate;
            // An unguided shell bursts where its creature stood when it was fired: that far behind the creature by the time it lands.
            float lag = !source.Homing && scenario.Moving ? enemy.AverageSpeed * TypicalShotReach * source.Range / source.ProjectileSpeed : 0f;
            for (int shot = 0; shot < MaxShots; shot++)
            {
                int lead = Lead(group);
                if (lead < 0)
                {
                    return shot * interval;
                }

                if (source.SplashRadius <= 0f)
                {
                    Hit(group[lead], damage, source.Profile, enemy);
                    continue;
                }

                Blast(group, scenario.Spacing, -lead * scenario.Spacing - lag, source.SplashRadius, source.SplashTargets, damage, source.Profile, enemy);
            }

            return float.PositiveInfinity;
        }

        /// <summary>The group cleared by a hundred coins of the source in one second (higher is better): what the role margins compare.</summary>
        public static float ClearRate(Source source, RoleScenario scenario, EnemyDefinition enemy, float healthScale, Boost boost)
        {
            float seconds = ClearSeconds(source, scenario, enemy, healthScale, boost);
            return float.IsInfinity(seconds) || seconds <= 0f ? 0f : CoinUnit / (seconds * Mathf.Max(1, source.Coins));
        }

        /// <summary>
        /// Groups a hundred coins of the source clear over the helper horizon: a tower fires for all of it, a path
        /// object is spent after its one use (the share of the group that use takes), so a gadget is weighed as what it is.
        /// </summary>
        public static float GroupsOverHorizon(Source source, RoleScenario scenario, EnemyDefinition enemy, CombatRules rules)
        {
            float per = CoinUnit / Mathf.Max(1, source.Coins);
            if (!source.SingleUse)
            {
                float seconds = ClearSeconds(source, scenario, enemy, 1f, Boost.None);
                return float.IsInfinity(seconds) || seconds <= 0f ? 0f : rules.HelperHorizonSeconds / seconds * per;
            }

            if ((source.Targets & enemy.Layer) == 0)
            {
                return 0f;
            }

            Health[] group = Group(scenario, enemy, 1f);
            float total = 0f;
            float taken = 0f;
            // A mine bursts under the first creature; a roller touches every creature for the time its width takes to pass.
            float perCreature = source.RollSpeed > 0f
                ? source.Damage * 2f * source.SplashRadius / (source.RollSpeed + (scenario.Moving ? enemy.AverageSpeed : 0f))
                : source.Damage;
            for (int i = 0; i < group.Length; i++)
            {
                total += group[i].Max + group[i].ArmorMax;
                bool reached = source.RollSpeed > 0f || i * scenario.Spacing <= source.SplashRadius;
                taken += reached ? group[i].TakeDamage(perCreature * source.Profile.Against(enemy.Layer, enemy.Family), source.Profile.Armour) : 0f;
            }

            return total > 0f ? taken / total * per : 0f;
        }

        private static Health[] Group(RoleScenario scenario, EnemyDefinition enemy, float healthScale)
        {
            var group = new Health[Mathf.Max(1, scenario.Count)];
            for (int i = 0; i < group.Length; i++)
            {
                group[i] = new Health(enemy.MaxHealth * healthScale, enemy.Armor * healthScale);
            }

            return group;
        }

        private static int Lead(Health[] group)
        {
            for (int i = 0; i < group.Length; i++)
            {
                if (!group[i].IsDead)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void Hit(Health health, float damage, DamageProfile profile, EnemyDefinition enemy) =>
            health.TakeDamage(damage * profile.Against(enemy.Layer, enemy.Family), profile.Armour);

        // Hurts the living creatures within the radius of the burst point, nearest first, up to the cap (zero: all of them).
        private static void Blast(Health[] group, float spacing, float centre, float radius, int cap, float damage, DamageProfile profile, EnemyDefinition enemy)
        {
            int hit = 0;
            // Creature i stands at -i * spacing; walk outwards from the burst point so the nearest are taken first.
            int nearest = Mathf.Clamp(Mathf.RoundToInt(-centre / spacing), 0, group.Length - 1);
            for (int ring = 0; ring < group.Length && (cap <= 0 || hit < cap); ring++)
            {
                for (int side = -1; side <= 1 && (cap <= 0 || hit < cap); side += 2)
                {
                    int i = nearest + side * ring;
                    if ((ring == 0 && side > 0) || i < 0 || i >= group.Length || group[i].IsDead || Mathf.Abs(-i * spacing - centre) > radius)
                    {
                        continue;
                    }

                    Hit(group[i], damage, profile, enemy);
                    hit++;
                }
            }
        }
    }
}
