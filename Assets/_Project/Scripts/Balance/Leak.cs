using System;

namespace Game.Balance
{
    /// <summary>What one wave's creatures that got past the guns on the walk did to the structures, on paper (the siege model).</summary>
    public struct SiegeLine
    {
        /// <summary>Health of the walkers and bosses that reached the defence gate's queue alive, and of the flyers that reached the base.</summary>
        public float GroundLeak;
        public float AirLeak;

        /// <summary>Ground health that got past the defence gate (it fell or there was none).</summary>
        public float RestLeak;

        /// <summary>What the siege took off the trunk gates (all of them) and off the base.</summary>
        public float GateDamage;
        public float BaseDamage;

        /// <summary>True when the hero went down during this wave's siege.</summary>
        public bool HeroFell;
    }

    /// <summary>
    /// The defence as the siege sees it: what the guns kill on the walk (before the defence gate), and their damage per second at
    /// the defence gate's queue and at the base's doorstep (the base's own turrets included). Built by <see cref="Leak.AddTower"/>
    /// and <see cref="Leak.AddBaseTurrets"/>; the hero is added by the siege itself from the hero data.
    /// </summary>
    public struct SiegeGuards
    {
        public float WalkStream;
        public float WalkSingle;
        public float WalkAir;
        public float QueueStream;
        public float QueueSingle;
        public float DoorStream;
        public float DoorSingle;
        public float DoorAir;
    }

    /// <summary>The structures and the hero between waves: what the siege wears down and what a repair restores.</summary>
    public sealed class SiegeState
    {
        public int TrunkGates;

        /// <summary>Health of the first trunk gate (the defence gate, the hero's post) and of the other trunk gates together.</summary>
        public float DefenceGate;
        public float RestGates;
        public float BaseLeft;

        /// <summary>Seconds before a downed hero stands again (zero: he is up).</summary>
        public float HeroDownLeft;

        /// <summary>The hero's health carried from wave to wave (round 24): a siege wears it down, the quiet between waves heals it.</summary>
        public float HeroHealth;

        public float GatesLeft => DefenceGate + RestGates;

        public bool HeroUp => HeroDownLeft <= 0f;

        public static SiegeState Start(LevelInput level, int trunkGates) => new SiegeState
        {
            TrunkGates = trunkGates,
            DefenceGate = trunkGates > 0 ? level.GateHealth : 0f,
            RestGates = trunkGates > 1 ? level.GateHealth * (trunkGates - 1) : 0f,
            BaseLeft = level.BaseHealth,
            HeroHealth = level.Hero != null ? level.Hero.MaxHealth : 0f,
        };

        /// <summary>Every trunk gate back to full (a repair step).</summary>
        public void RepairGates(LevelInput level)
        {
            DefenceGate = TrunkGates > 0 ? level.GateHealth : 0f;
            RestGates = TrunkGates > 1 ? level.GateHealth * (TrunkGates - 1) : 0f;
        }

        /// <summary>Restores <paramref name="budget"/> health: the defence gate first, then the other gates, then the base (the repair drone).</summary>
        public float Restore(LevelInput level, float budget)
        {
            float gate = TrunkGates > 0 ? Math.Min(budget, level.GateHealth - DefenceGate) : 0f;
            DefenceGate += gate;
            budget -= gate;
            float rest = TrunkGates > 1 ? Math.Min(budget, level.GateHealth * (TrunkGates - 1) - RestGates) : 0f;
            RestGates += rest;
            budget -= rest;
            float toBase = BaseLeft > 0f ? Math.Min(budget, level.BaseHealth - BaseLeft) : 0f;
            BaseLeft += toBase;
            return gate + rest + toBase;
        }
    }

    /// <summary>
    /// The siege model (round 22c). A creature the guns do not kill on the walk does not vanish: it stands at the defence
    /// gate's queue and hammers the gate with its structure damage until the guns that reach the queue and the hero at his post
    /// have killed it, or the gate falls. What gets past walks on to the other trunk gates (nothing shoots there but the hero,
    /// who falls back once his gate is gone) and then to the base's doorstep, where the guns in reach of the last road cells,
    /// the base's own turrets and the hero (when no gate stands) kill it while it hammers the base. The hero is explicit data:
    /// his blade and blaster, his health against the guard share of the hits on his gate, his revive time. The one measured
    /// number is <see cref="BalanceRules.SiegeFactor"/>, the structure damage per second of a siege over the paper hammer.
    /// </summary>
    public static class Leak
    {
        /// <summary>Share of a wave that gets past the defence at a pressure: nothing at one or below, all of it with no defence.</summary>
        public static float Share(float pressure) => pressure <= 1f ? 0f : float.IsInfinity(pressure) ? 1f : 1f - 1f / pressure;

        /// <summary>
        /// Adds one standing gun to the guards: what it kills on the walk from its spot (the cover without the gate's hold), and its
        /// damage per second at the queue when it reaches the defence gate and at the doorstep when it reaches the base.
        /// <paramref name="efficiency"/> below zero takes the tower's calibrated value; <paramref name="slow"/> is a slow field's gain.
        /// </summary>
        public static void AddTower(ref SiegeGuards guards, LevelInput level, WaveLoad load, BalanceRules rules, int towerIndex, int towerLevel, SpotCover cover, float efficiency, float slow)
        {
            SpotCover walk = cover;
            walk.Gate = false;
            Ledger.Option option = Ledger.Capacity(level, load, rules, towerIndex, towerLevel, walk, efficiency);
            guards.WalkStream += option.Stream * slow;
            guards.WalkSingle += option.Single * slow;
            guards.WalkAir += option.Air;
            if (!cover.Gate && !cover.Base)
            {
                return;
            }

            TowerStats tower = level.Towers[towerIndex];
            Dps(level, load, rules, tower, towerLevel, efficiency >= 0f ? efficiency : tower.Efficiency, out float stream, out float single, out float air);
            if (cover.Gate)
            {
                guards.QueueStream += stream * slow;
                guards.QueueSingle += single * slow;
            }

            if (cover.Base)
            {
                guards.DoorStream += stream * slow;
                guards.DoorSingle += single * slow;
                guards.DoorAir += air;
            }
        }

        /// <summary>The base's own turrets (the permanent base weapon upgrade) stand at the doorstep.</summary>
        public static void AddBaseTurrets(ref SiegeGuards guards, LevelInput level, WaveLoad load, BalanceRules rules)
        {
            if (level.BaseTurret == null || level.BaseTurrets <= 0 || level.BaseTurret.Levels.Length == 0)
            {
                return;
            }

            int turretLevel = Math.Min(level.BaseTurretLevel, level.BaseTurret.Levels.Length - 1);
            Dps(level, load, rules, level.BaseTurret, turretLevel, level.BaseTurret.Efficiency, out float stream, out float single, out float air);
            guards.DoorStream += stream * level.BaseTurrets;
            guards.DoorSingle += single * level.BaseTurrets;
            guards.DoorAir += air * level.BaseTurrets;
        }

        // A gun's damage per second against stationary creatures of this wave, per layer: plating and overkill per hit, the
        // splash cap on a bunched queue, the tower's efficiency. No utilisation: that is the share of a pass a gun spends hurting
        // something; a creature hammering a gate or the base stands in reach until it dies.
        private static void Dps(LevelInput level, WaveLoad load, BalanceRules rules, TowerStats tower, int towerLevel, float efficiency, out float stream, out float single, out float air)
        {
            TowerLevelStats stats = tower.Levels[towerLevel];
            TowerBoost boost = level.BoostOf(tower.Id);
            float damage = stats.Damage * boost.Damage;
            float dps = stats.Interval > 0f ? damage * boost.Rate / stats.Interval : 0f;
            float field = dps * efficiency;
            bool ground = Layer.Has(tower.Targets, Layer.Ground);
            bool sky = Layer.Has(tower.Targets, Layer.Air);
            float groundHit = damage * tower.GroundFactor;
            float airHit = damage * tower.AirFactor;
            float splash = tower.Splash > 0f ? Ledger.SplashCap(tower, rules) : 1f;
            stream = ground ? field * tower.GroundFactor * load.StreamArmour.Share(tower.ArmourFactor) * Ledger.Overkill(groundHit, load.MeanHealth) * splash : 0f;
            single = ground ? field * tower.GroundFactor * load.BossArmour.Share(tower.ArmourFactor) : 0f;
            air = sky ? field * tower.AirFactor * load.AirArmour.Share(tower.ArmourFactor) * Ledger.Overkill(airHit, load.MeanAirHealth) * splash : 0f;
        }

        /// <summary>
        /// Resolves one wave's siege against the guards and the state: the stream and the bosses at the defence gate, what gets
        /// past at the other gates and the base, the flyers at the base. Wears the state's gates, base and hero down;
        /// <paramref name="waveSeconds"/> is the wave's length on the clock (countdown and window), for the hero's revive.
        /// </summary>
        public static SiegeLine Resolve(LevelInput level, WaveLoad load, BalanceRules rules, SiegeGuards guards, SiegeState state, float waveSeconds)
        {
            var line = new SiegeLine();
            bool fell = false;

            float stream = Math.Max(0f, load.StreamHealth - guards.WalkStream);
            float bosses = Math.Max(0f, load.BossHealth - guards.WalkSingle);
            line.GroundLeak = stream + bosses;
            float attackers = Attackers(stream, load.MeanHealth, rules);
            float bossAttackers = bosses > 0f ? Math.Min(Math.Max(1, load.BossCount), rules.AttackersPerGate) : 0f;

            // The hero against each group: blade, blaster and the pulse on the attackers in front of him, at the share the
            // group's armour lets each of them land (round 24); against flyers at the base the blaster only.
            float heroGround = state.HeroUp ? Ledger.HeroDps(level, load.StreamArmour, attackers) : 0f;
            float heroBoss = state.HeroUp ? Ledger.HeroDps(level, load.BossArmour, bossAttackers) : 0f;
            float heroAir = state.HeroUp && level.Hero != null ? Ledger.HeroGunDps(level) * load.AirArmour.Share(level.Hero.GunArmourFactor) : 0f;

            // The defence gate: the queue guns and the hero at his post against the stream, then the bosses.
            if (state.TrunkGates > 0 && state.DefenceGate > 0f)
            {
                stream = Fight(stream, attackers, load.GroundStrike, guards.QueueStream, heroGround, level.Hero, rules, ref state.DefenceGate, ref state.HeroHealth, ref fell, out float gate1);
                bosses = Fight(bosses, bossAttackers, load.BossStrike, guards.QueueSingle, fell ? 0f : heroBoss, level.Hero, rules, ref state.DefenceGate, ref state.HeroHealth, ref fell, out float gate2);
                line.GateDamage += gate1 + gate2;
            }

            line.RestLeak = stream + bosses;

            // The other trunk gates: nothing shoots there but the hero, fallen back from his wrecked gate.
            if (state.TrunkGates > 1 && state.RestGates > 0f)
            {
                bool hero = state.DefenceGate <= 0f && !fell;
                stream = Fight(stream, attackers, load.GroundStrike, 0f, hero ? heroGround : 0f, level.Hero, rules, ref state.RestGates, ref state.HeroHealth, ref fell, out float rest1);
                bosses = Fight(bosses, bossAttackers, load.BossStrike, 0f, hero && !fell ? heroBoss : 0f, level.Hero, rules, ref state.RestGates, ref state.HeroHealth, ref fell, out float rest2);
                line.GateDamage += rest1 + rest2;
            }

            // The doorstep: the guns in reach of the last road cells, the base's turrets, the hero when no gate stands.
            bool heroAtBase = state.GatesLeft <= 0f && !fell;
            Fight(stream, attackers, load.GroundStrike, guards.DoorStream, heroAtBase ? heroGround : 0f, level.Hero, rules, ref state.BaseLeft, ref state.HeroHealth, ref fell, out float base1);
            Fight(bosses, bossAttackers, load.BossStrike, guards.DoorSingle, heroAtBase && !fell ? heroBoss : 0f, level.Hero, rules, ref state.BaseLeft, ref state.HeroHealth, ref fell, out float base2);

            // Flyers pass the gates and lay siege to the base.
            line.AirLeak = Math.Max(0f, load.AirHealth - guards.WalkAir);
            Fight(line.AirLeak, Attackers(line.AirLeak, load.MeanAirHealth, rules), load.AirStrike, guards.DoorAir, heroAtBase && !fell ? heroAir : 0f, level.Hero, rules, ref state.BaseLeft, ref state.HeroHealth, ref fell, out float base3);
            line.BaseDamage = base1 + base2 + base3;
            line.HeroFell = fell;

            // The clock: a hero who fell midway through the wave has the rest of it to recover, and stands up whole.
            if (fell && level.Hero != null)
            {
                state.HeroDownLeft = Math.Max(0f, level.Hero.ReviveSeconds - 0.5f * waveSeconds);
                state.HeroHealth = level.Hero.MaxHealth;
            }
            else
            {
                state.HeroDownLeft = Math.Max(0f, state.HeroDownLeft - waveSeconds);
                Recover(level, load, state, waveSeconds);
            }

            return line;
        }

        // The quiet between the wave's fight and the next wave (the clock less the window, less the delay before the
        // regeneration starts) heals the hero at his rate, as Commander.Recover does; a hero who never fights is whole.
        private static void Recover(LevelInput level, WaveLoad load, SiegeState state, float waveSeconds)
        {
            if (level.Hero == null || level.Hero.RegenPerSecond <= 0f)
            {
                return;
            }

            float quiet = Math.Max(0f, waveSeconds - load.Window - level.Hero.RegenDelaySeconds);
            state.HeroHealth = Math.Min(level.Hero.MaxHealth, state.HeroHealth + level.Hero.RegenPerSecond * quiet);
        }

        /// <summary>Leakers hammering a structure at once: as many as the leak holds, at most the rules' attackers.</summary>
        public static float Attackers(float leakHealth, float meanHealth, BalanceRules rules) =>
            leakHealth <= 0f ? 0f : Math.Min(leakHealth / Math.Max(1f, meanHealth), rules.AttackersPerGate);

        // One fight at one structure: the attackers hammer it at their strike (times the siege factor) while the guard guns and the
        // hero kill them. The hero in front of the structure takes his guard share of the hammer instead of it, and goes down when
        // his health is gone; from then on the structure takes the whole hammer. Returns the health the structure's fall let
        // through (zero when the leak was cleared), and takes the damage off the structure.
        private static float Fight(float health, float attackers, float strike, float guardDps, float heroDps, HeroStats hero, BalanceRules rules, ref float structure, ref float heroHealth, ref bool fell, out float damage)
        {
            damage = 0f;
            if (health <= 0f || structure <= 0f)
            {
                return structure <= 0f ? health : 0f;
            }

            float hammer = attackers * strike * rules.SiegeFactor;
            // The guards land only their fitted share on a siege (BalanceRules.QueueGuardShare); the hero fights as modelled.
            guardDps *= rules.QueueGuardShare;
            float kill = guardDps + heroDps;
            if (hammer <= 0f)
            {
                // Harmless creatures: the guard clears them in time or they stand there doing nothing.
                return 0f;
            }

            // He goes down when the health he carries into this fight (round 24: worn by the sieges before it) is spent.
            bool guarded = heroDps > 0f && hero != null;
            float heroDown = guarded && hero.GuardShare * hammer > 0f ? heroHealth / (hero.GuardShare * hammer) : float.PositiveInfinity;
            float shield = guarded ? 1f - hero.GuardShare : 1f;
            float killAt;
            if (kill <= 0f)
            {
                killAt = float.PositiveInfinity;
            }
            else if (health <= kill * heroDown)
            {
                killAt = health / kill;
            }
            else
            {
                killAt = guardDps > 0f ? heroDown + (health - kill * heroDown) / guardDps : float.PositiveInfinity;
            }

            // The structure falls while the hero still shields it, or after he is down.
            float shielded = structure - hammer * shield * heroDown;
            float breakAt = shielded <= 0f || float.IsInfinity(heroDown) ? structure / (hammer * shield) : heroDown + shielded / hammer;
            float seconds = Math.Min(killAt, breakAt);
            if (guarded)
            {
                heroHealth = Math.Max(0f, heroHealth - hero.GuardShare * hammer * Math.Min(seconds, heroDown));
                if (heroDown < seconds)
                {
                    fell = true;
                }
            }

            damage = Math.Min(structure, hammer * (shield * Math.Min(seconds, heroDown) + Math.Max(0f, seconds - heroDown)));
            structure -= damage;
            if (killAt <= breakAt)
            {
                return 0f;
            }

            float killed = kill * Math.Min(seconds, heroDown) + guardDps * Math.Max(0f, seconds - heroDown);
            return Math.Max(0f, health - killed);
        }

        /// <summary>Stars for a base share left, as the game awards them: one for the win, one per threshold reached.</summary>
        public static int Stars(float baseLeft, float baseStart, float[] thresholds)
        {
            if (baseLeft <= 0f)
            {
                return 0;
            }

            int stars = 1;
            float fraction = baseStart > 0f ? baseLeft / baseStart : 0f;
            foreach (float threshold in thresholds)
            {
                stars += fraction >= threshold ? 1 : 0;
            }

            return Math.Min(3, stars);
        }

        /// <summary>
        /// The score a run or a plan is ranked by, the same on paper and in play: stars, base left and coins for a win; for a
        /// loss, how far it got.
        /// </summary>
        public static float Score(bool won, int stars, float baseLeft, int coins, int waveCount, int waveReached) =>
            won ? stars * 1000f + baseLeft + 0.1f * coins : -100f * Math.Max(0, waveCount - waveReached) - 1f;
    }
}
