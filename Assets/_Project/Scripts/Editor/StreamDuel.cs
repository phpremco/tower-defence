using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// One tower beside a straight road and a stream of one kind of creature walking past it (round 87; the lab script
    /// LaserDuel of rounds 85 and 86 made into a tool the tests can call). It plays the firing rules a tower plays in
    /// the game through the same pure classes (<see cref="Cadence"/>, <see cref="BeamBudget"/>, <see cref="Retarget"/>,
    /// <see cref="Health"/> with the weapon's <see cref="DamageProfile"/>): a turret shoots the creature furthest along
    /// and swings to the next, a beam holds one and pays its kill pulse by the share, a shell flies and bursts. What
    /// comes out is the bar (body plus armour) the tower takes off per second something was in reach: what its damage
    /// per second is really worth on that stream, with nothing wasted on dead bodies counted and no kill for free.
    /// A model of the firing rules, not of a level: no gates, no slows, no other towers.
    /// </summary>
    public static class StreamDuel
    {
        private const float TowerSide = 1.8f;
        private const float LaneHalfWidth = 0.6f;
        private const float RoadHalfLength = 14f;
        private const float HitDistance = 0.15f;
        private const float MaxSeconds = 600f;
        private const int Seed = 7;

        /// <summary>The frame length the game runs at, and the balance probe's step: a rule must give the same at both.</summary>
        public const float GameStep = 1f / 60f;
        public const float ProbeStep = 0.2f;

        /// <summary>A body every twentieth of a second: more than any tower can shoot, so it never waits for a target.</summary>
        public const float PackedSeconds = 0.05f;

        /// <summary>The stream that walks past the tower.</summary>
        public struct Stream
        {
            public float Body;
            public float Armour;
            public float Speed;

            /// <summary>Seconds between two creatures.</summary>
            public float Spacing;
            public int Count;
            public TargetLayers Layer;
            public CreatureFamily Family;

            public static Stream Of(EnemyDefinition enemy, float healthScale, float spacingSeconds, int count) => new Stream
            {
                Body = enemy.MaxHealth * healthScale, Armour = enemy.Armor * healthScale, Speed = enemy.AverageSpeed,
                Spacing = spacingSeconds, Count = count, Layer = enemy.Layer, Family = enemy.Family,
            };
        }

        private sealed class Walker
        {
            public float X;
            public float Z;
            public float Spawn;
            public Health Health;
            public bool Gone;
        }

        private struct Shot
        {
            public float X;
            public float Z;
            public float AimX;
            public float AimZ;
            public Walker Target;
        }

        /// <summary>
        /// The health scale a kind of creature walks in with on a difficulty, as the stored waves of the campaign send it:
        /// the mean over every one of them, weighted by head count. Zero when no wave sends it.
        /// </summary>
        public static float MeanHealthScale(CampaignCatalog catalog, DifficultyProfile difficulty, EnemyDefinition enemy)
        {
            double sum = 0.0;
            double heads = 0.0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (WaveDefinition wave in node.Level.GetWaves(difficulty))
                {
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        if (group.Enemy == enemy)
                        {
                            int count = Mathf.Max(1, Mathf.CeilToInt(group.Count * difficulty.CountMultiplier));
                            sum += difficulty.HealthMultiplier * node.Level.EnemyHealthScale * wave.HealthMultiplier * count;
                            heads += count;
                        }
                    }
                }
            }

            return heads > 0.0 ? (float)(sum / heads) : 0f;
        }

        /// <summary>Arrivals carry this many times the damage per second of the tower a stream is sized to overload.</summary>
        public const float Overload = 1.25f;

        /// <summary>
        /// Seconds between two of these bodies so that the stream is more than the tower's data can kill: both it and
        /// any tower beside it then fire without a pause, and what comes out is firepower, not how little there was to shoot.
        /// </summary>
        public static float OverloadSpacing(TowerDefinition tower, int levelIndex, float bar)
        {
            TowerLevel level = tower.GetLevel(levelIndex);
            return bar / (Overload * level.Damage / level.FireInterval);
        }

        /// <summary>Every coin spent on a tower to stand at a level.</summary>
        public static int Coins(TowerDefinition tower, int levelIndex)
        {
            int coins = 0;
            for (int i = 0; i <= levelIndex; i++)
            {
                coins += tower.GetLevel(i).Cost;
            }

            return coins;
        }

        /// <summary>Bar (body plus armour) taken off per second a creature was in reach.</summary>
        public static float Dps(TowerDefinition tower, int levelIndex, Stream stream, float step) =>
            Dps(tower, levelIndex, stream, step, tower.SplashTargetsAt(levelIndex));

        /// <summary>As <see cref="Dps(TowerDefinition, int, Stream, float)"/> with another blast cap than the tower's data has at that level (a lab's what-if; zero is the whole radius).</summary>
        public static float Dps(TowerDefinition tower, int levelIndex, Stream stream, float step, int splashTargets)
        {
            TowerLevel level = tower.GetLevel(levelIndex);
            DamageProfile profile = tower.Profile;
            float landed = level.Damage * profile.Against(stream.Layer, stream.Family);
            float rangeSqr = level.Range * level.Range;
            var random = new System.Random(Seed);
            var walkers = new List<Walker>(stream.Count);
            for (int i = 0; i < stream.Count; i++)
            {
                float jitter = (float)(random.NextDouble() - 0.5) * 0.9f * stream.Spacing;
                walkers.Add(new Walker
                {
                    X = -RoadHalfLength, Z = (float)(random.NextDouble() * 2.0 - 1.0) * LaneHalfWidth,
                    Spawn = (i + 1) * stream.Spacing + jitter, Health = new Health(stream.Body, stream.Armour),
                });
            }

            var shots = new List<Shot>();
            var doomed = new List<Walker>();
            var caught = new List<Walker>();
            float cooldown = 0f;
            Walker held = null;
            Walker aimed = null;
            bool live = false;
            float useful = 0f;
            float busy = 0f;
            for (float t = 0f; t < MaxSeconds; t += step)
            {
                bool pending = false;
                bool any = false;
                foreach (Walker walker in walkers)
                {
                    if (walker.Gone || walker.Health.IsDead)
                    {
                        continue;
                    }

                    if (t >= walker.Spawn)
                    {
                        walker.X += stream.Speed * step;
                    }

                    if (walker.X > RoadHalfLength)
                    {
                        walker.Gone = true;
                        continue;
                    }

                    pending = true;
                    any |= InReach(walker, rangeSqr);
                }

                if (!pending && shots.Count == 0)
                {
                    break;
                }

                busy += any ? step : 0f;
                useful += Fly(shots, walkers, caught, tower, splashTargets, landed, profile.Armour, step);
                if (held != null && (held.Gone || held.Health.IsDead || !InReach(held, rangeSqr)))
                {
                    held = null;
                }

                cooldown -= step;
                if (cooldown > 0f)
                {
                    continue;
                }

                // The same loop as Tower.Update: a long frame may owe more than one shot (Cadence).
                doomed.Clear();
                float underWay = 0f;
                for (int n = 0; Cadence.Due(cooldown, n); n++)
                {
                    Walker target = held ?? Best(walkers, rangeSqr, doomed);
                    if (target == null)
                    {
                        held = null;
                        live = false;
                        aimed = null;
                        break;
                    }

                    if (!tower.IsBeam)
                    {
                        float swing = Retarget.Seconds(aimed != null, ReferenceEquals(target, aimed), tower.RetargetSeconds);
                        aimed = target;
                        if (swing > 0f)
                        {
                            cooldown = Cadence.Rearm(cooldown, swing);
                            continue;
                        }

                        shots.Add(new Shot { X = 0f, Z = TowerSide, AimX = target.X, AimZ = target.Z, Target = target });
                        cooldown = Cadence.Rearm(cooldown, level.FireInterval);
                        continue;
                    }

                    if (!ReferenceEquals(target, held))
                    {
                        held = target;
                        underWay = 0f;
                        if (!live)
                        {
                            cooldown = tower.BeamLockSeconds;
                            live = true;
                            break;
                        }
                    }

                    float needed = BeamBudget.Needed(target.Health.HitToKill(profile.Armour) / Mathf.Max(0.0001f, profile.Against(stream.Layer, stream.Family)), underWay);
                    shots.Add(new Shot { X = 0f, Z = TowerSide, AimX = target.X, AimZ = target.Z, Target = target });
                    live = true;
                    cooldown = Cadence.Rearm(cooldown, BeamBudget.Seconds(level.FireInterval, needed, level.Damage, tower.BeamSweepSeconds));
                    if (BeamBudget.Kills(needed, level.Damage))
                    {
                        doomed.Add(target);
                        held = null;
                        underWay = 0f;
                    }
                    else
                    {
                        underWay += level.Damage;
                    }
                }

                cooldown = Cadence.Idle(cooldown);
            }

            return busy > 0f ? useful / busy : 0f;
        }

        // Moves every shot a frame on and lands the ones that arrive; returns the bar they took off.
        private static float Fly(List<Shot> shots, List<Walker> walkers, List<Walker> caught, TowerDefinition tower, int splashTargets, float landed, float armourFactor, float step)
        {
            float useful = 0f;
            float reach = tower.ProjectileSpeed * step;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                bool follows = tower.Homing && !shot.Target.Gone && !shot.Target.Health.IsDead;
                float aimX = follows ? shot.Target.X : shot.AimX;
                float aimZ = follows ? shot.Target.Z : shot.AimZ;
                float dx = aimX - shot.X;
                float dz = aimZ - shot.Z;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                if (distance > Mathf.Max(reach, HitDistance))
                {
                    shot.X += dx / distance * reach;
                    shot.Z += dz / distance * reach;
                    shot.AimX = aimX;
                    shot.AimZ = aimZ;
                    shots[i] = shot;
                    continue;
                }

                shots.RemoveAt(i);
                if (tower.SplashRadius <= 0f)
                {
                    useful += shot.Target.Gone ? 0f : shot.Target.Health.TakeDamage(landed, armourFactor);
                    continue;
                }

                // A blast takes the creatures nearest its burst point, up to its cap at the tower's level (Projectile).
                caught.Clear();
                foreach (Walker walker in walkers)
                {
                    float wx = walker.X - aimX;
                    float wz = walker.Z - aimZ;
                    if (!walker.Gone && !walker.Health.IsDead && wx * wx + wz * wz <= tower.SplashRadius * tower.SplashRadius)
                    {
                        caught.Add(walker);
                    }
                }

                caught.Sort((a, b) => Sqr(a, aimX, aimZ).CompareTo(Sqr(b, aimX, aimZ)));
                int most = splashTargets > 0 ? Mathf.Min(splashTargets, caught.Count) : caught.Count;
                for (int c = 0; c < most; c++)
                {
                    useful += caught[c].Health.TakeDamage(landed, armourFactor);
                }
            }

            return useful;
        }

        private static float Sqr(Walker walker, float x, float z) => (walker.X - x) * (walker.X - x) + (walker.Z - z) * (walker.Z - z);

        private static bool InReach(Walker walker, float rangeSqr)
        {
            float dz = walker.Z - TowerSide;
            return walker.X > -RoadHalfLength && walker.X * walker.X + dz * dz <= rangeSqr;
        }

        // The creature furthest along in reach, passing over the ones the beam's pulses of this frame are about to kill.
        private static Walker Best(List<Walker> walkers, float rangeSqr, List<Walker> except)
        {
            Walker best = null;
            foreach (Walker walker in walkers)
            {
                if (!walker.Gone && !walker.Health.IsDead && InReach(walker, rangeSqr) && (best == null || walker.X > best.X) && !except.Contains(walker))
                {
                    best = walker;
                }
            }

            return best;
        }
    }
}
