using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>Who a creature can be hit by, and what a gun can hit. Same bits as <c>Game.Core.TargetLayers</c>.</summary>
    public static class Layer
    {
        public const int Ground = 1;
        public const int Air = 2;

        public static bool Has(int mask, int layer) => (mask & layer) != 0;
    }

    /// <summary>The numbers of one creature that matter for balance.</summary>
    public sealed class EnemyStats
    {
        public string Id = string.Empty;
        public int Layer = Balance.Layer.Ground;
        public bool Flies;
        public float Health = 1f;
        public float Armor;
        public float Speed = 1f;

        /// <summary>
        /// Speed while the defence hits it, when that differs from the walk (0 = <see cref="Speed"/>): a pack that rages at
        /// every death around it crosses the towers' reach that much faster.
        /// </summary>
        public float FightSpeed;

        public float InRangeSpeed => FightSpeed > 0f ? FightSpeed : Speed;
        public int Reward;
        public int BaseDamage = 1;
        public float AttackDamage;
        public float AttackInterval = 1f;
        public float BlockadeDamage;
        public bool IsBoss;

        /// <summary>
        /// Seconds of the whole defence's firepower a continent boss's mechanic takes away over one fight (hidden under the
        /// ground, a thrown tower silent), from its boss data; zero for every other creature.
        /// </summary>
        public float MechanicLossSeconds;

        /// <summary>True for a continent boss: its health takes the difficulty's boss share (<see cref="DifficultyStats.BossHealth"/>).</summary>
        public bool IsContinentBoss;

        /// <summary>Health plus armour: armour soaks that many points before it is gone, whatever its share per hit.</summary>
        public float EffectiveHealth => Health + Armor;
    }

    /// <summary>One upgrade level of a tower.</summary>
    public struct TowerLevelStats
    {
        public int Cost;
        public float Damage;
        public float Range;
        public float Interval;
        public float Slow;

        public TowerLevelStats(int cost, float damage, float range, float interval, float slow = 0f)
        {
            Cost = cost;
            Damage = damage;
            Range = range;
            Interval = interval;
            Slow = slow;
        }

        public float Dps => Interval > 0f ? Damage / Interval : 0f;
    }

    /// <summary>The numbers of one tower that matter for balance.</summary>
    public sealed class TowerStats
    {
        public string Id = string.Empty;
        public int Targets = Layer.Ground;
        public float Splash;
        public bool IsSupport;
        public bool BlocksGround;
        public bool IsBaseDefence;
        public float SupportRateBonus;
        public TowerLevelStats[] Levels = new TowerLevelStats[0];

        /// <summary>A gun the player builds: not a support mast, not a gate, not the base's own turret.</summary>
        public bool IsGun => !IsSupport && !BlocksGround && !IsBaseDefence && Levels.Length > 0;
    }

    /// <summary>The commander: the blade at a gate and the blaster for whatever is out of reach.</summary>
    public sealed class HeroStats
    {
        public float BladeDamage;
        public float BladeInterval = 1f;
        public float GunDamage;

        /// <summary>Seconds between two shots of the blaster, without its reload.</summary>
        public float GunInterval = 1f;

        /// <summary>Shots before the blaster reloads; zero means it never does.</summary>
        public int Magazine;
        public float ReloadSeconds;

        /// <summary>The perks the hero can earn that change the blaster.</summary>
        public List<HeroPerkStats> Perks = new List<HeroPerkStats>();

        /// <summary>Seconds per shot over a whole magazine and its reload, with the owned perks applied.</summary>
        public float EffectiveGunInterval(ICollection<string> ownedPerks)
        {
            float interval = GunInterval;
            float reload = ReloadSeconds;
            foreach (HeroPerkStats perk in Perks)
            {
                if (ownedPerks != null && ownedPerks.Contains(perk.Id))
                {
                    interval *= perk.IntervalMultiplier;
                    reload *= perk.ReloadMultiplier;
                }
            }

            return Magazine > 0 ? (Magazine * interval + reload) / Magazine : interval;
        }
    }

    /// <summary>A difficulty profile: how it scales the waves and how it pays.</summary>
    public sealed class DifficultyStats
    {
        public string Id = string.Empty;
        public int Index;
        public float Health = 1f;
        public float Count = 1f;
        public float Interval = 1f;
        public float Research = 1f;
        public float Power;
        public float StructureDamage = 1f;

        /// <summary>In-level income (kill bounties and harvested ore) as a share of what the creatures and mines pay.</summary>
        public float Income = 1f;

        /// <summary>Starting money as a share of the level's own.</summary>
        public float StartMoney = 1f;

        /// <summary>Extra health share of a continent boss on this difficulty, over the plain creatures' <see cref="Health"/>.</summary>
        public float BossHealth = 1f;

        /// <summary>The health factor of one creature beyond <see cref="Health"/>: the boss share for a continent boss, else one.</summary>
        public float ShareFor(EnemyStats enemy) => enemy.IsContinentBoss ? BossHealth : 1f;
    }

    /// <summary>A permanent upgrade bought with research or power between levels.</summary>
    public sealed class UpgradeStats
    {
        public string Id = string.Empty;
        public UpgradeKind Kind;
        public string TowerId = string.Empty;
        public bool PaidInPower;
        public float BonusPerLevel;
        public int[] Costs = new int[0];

        /// <summary>The continent whose research or power pays for this upgrade; -1 for a dormant upgrade outside every budget.</summary>
        public int Continent = -1;

        public int TotalCost
        {
            get
            {
                int sum = 0;
                foreach (int cost in Costs)
                {
                    sum += cost;
                }

                return sum;
            }
        }
    }

    /// <summary>What a permanent upgrade improves, as far as the balance model is concerned.</summary>
    public enum UpgradeKind
    {
        Other = 0,
        TowerDamage = 1,
        TowerRange = 2,
        TowerFireRate = 3,
        HeroDamage = 4,
        GateHealth = 5,
        BaseHealth = 6,
        BaseWeapon = 7,
    }

    /// <summary>One run of identical creatures inside a wave, as the game stores it.</summary>
    public struct GroupSpec
    {
        public EnemyStats Enemy;
        public int Count;
        public int Path;
        public int Source;
        public float Interval;
        public float Delay;
        public float Stagger;
    }

    /// <summary>One wave as the game stores it.</summary>
    public sealed class WaveSpec
    {
        public readonly List<GroupSpec> Groups = new List<GroupSpec>();
        public float StartDelay = 8f;
        public float HealthMultiplier = 1f;
    }
}
