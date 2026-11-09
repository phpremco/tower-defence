using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>One standing gun as the route-sampled kill model walks the creatures past it (round 27).</summary>
    public struct RouteGun
    {
        public TowerStats Tower;
        public int Level;
        public Cell Spot;

        /// <summary>The efficiency the gun fires at (resolved; the calibrated value when the caller gave none).</summary>
        public float Efficiency;

        /// <summary>A slow field's gain on this gun for the wave (1 without one).</summary>
        public float Slow;
    }

    /// <summary>What the guns killed along the routes: before the defence gate, behind it, and in the air; per tower type for the fit.</summary>
    public sealed class RouteResult
    {
        public float WalkStream;
        public float WalkSingle;
        public float WalkAir;
        public float BehindStream;
        public float BehindSingle;

        /// <summary>Health taken off the wave's creatures per tower type, walk and behind together.</summary>
        public readonly Dictionary<string, float> Dealt = new Dictionary<string, float>(StringComparer.Ordinal);

        internal void Credit(string id, float amount)
        {
            if (amount > 0f)
            {
                Dealt[id] = (Dealt.TryGetValue(id, out float known) ? known : 0f) + amount;
            }
        }
    }

    /// <summary>
    /// The route-sampled kill model (round 27): every group of a wave walks its own route cell by cell, and every gun fires on it
    /// over the cells of that route in its reach. A gun's fire on one creature is its rate times the time that creature spends in
    /// its reach, but no more than its share of the stream: with several creatures in reach at once a gun splits its fire between
    /// them (one over the arrival rate of the creatures still alive where the gun first sees them), so a gun with two cells of
    /// road in reach on a long, sparse wave is credited for its exposure, not for the whole spawn span. Damage accumulates along
    /// the route; a creature dies where the sum reaches its body and armour, and what is still alive at the defence gate is the
    /// leak the siege model (<see cref="Leak"/>) resolves. The guns' cells behind the gate meet only what got past it. Two passes:
    /// the second knows where the first pass's kills thinned the stream. Replaces the spawn-span-plus-pass credit of
    /// <see cref="Ledger.Capacity"/> in the siege's walk; the pressure lens keeps that capacity for purchases and the solver.
    /// </summary>
    public static class RouteKill
    {
        private const int Passes = 2;

        private sealed class Gun
        {
            public RouteGun Spec;
            public float Dps;
            public float GroundHit;
            public float AirHit;
            public bool Ground;
            public bool Air;
            public float SplashCap;
            public float Reach;
        }

        private sealed class Walker
        {
            public WaveGroup Group;
            public LevelGeometry.Route Route;
            public int Start;
            public int ApproachEnd;

            /// <summary>The route cell the group dies on in the last pass, or -1 while it reaches the gate alive.</summary>
            public int KillIndex = -1;
            public float Scale = 1f;
        }

        /// <summary>True when the level has a map with routes and the load carries its groups: the route model can walk the wave.</summary>
        public static bool Applies(LevelInput level, WaveLoad load) =>
            level.Geometry != null && level.Geometry.GroundRoutes.Count > 0 && load.Groups.Count > 0;

        public static RouteResult Walk(LevelInput level, WaveLoad load, BalanceRules rules, List<RouteGun> guns)
        {
            var result = new RouteResult();
            LevelGeometry geometry = level.Geometry;
            List<Gun> armed = Arm(level, rules, guns);
            List<Walker> walkers = Place(level, load);
            if (armed.Count == 0 || walkers.Count == 0)
            {
                return result;
            }

            float bossShare = 1f - Math.Min(rules.MaxBossMechanicShare, load.BossMechanicShare);
            var perCell = new float[LongestRoute(geometry)];
            var perGun = new float[armed.Count];
            for (int pass = 0; pass < Passes; pass++)
            {
                result = new RouteResult();
                foreach (Walker walker in walkers)
                {
                    WaveGroup group = walker.Group;
                    int end = walker.Route.Cells.Count - 1;
                    float speed = Math.Max(0.01f, group.Speed);
                    Array.Clear(perCell, 0, perCell.Length);

                    // Each gun's fire per creature of this group, spread over the cells of its reach on the approach.
                    for (int g = 0; g < armed.Count; g++)
                    {
                        perGun[g] = PerCell(armed[g], walker, load, rules, walkers, geometry, walker.Start, walker.ApproachEnd, speed, bossShare, perCell);
                    }

                    float dealt = March(walker, perCell, walker.Start, walker.ApproachEnd, out int killIndex, out float lastShare);
                    Credit(result, armed, walker, geometry, walker.Start, walker.ApproachEnd, perGun, killIndex, lastShare);
                    walker.KillIndex = killIndex;
                    float killed = group.Count * dealt * walker.Scale;
                    if (group.Flies)
                    {
                        result.WalkAir += killed;
                    }
                    else if (group.IsBoss)
                    {
                        result.WalkSingle += killed;
                    }
                    else
                    {
                        result.WalkStream += killed;
                    }

                    // Alive past the defence gate: the guns whose reach lies behind it meet what the gate let through.
                    if (killIndex >= 0 || group.Flies || walker.ApproachEnd >= end)
                    {
                        continue;
                    }

                    Array.Clear(perCell, 0, perCell.Length);
                    for (int g = 0; g < armed.Count; g++)
                    {
                        perGun[g] = PerCell(armed[g], walker, load, rules, walkers, geometry, walker.ApproachEnd + 1, end, speed, bossShare, perCell);
                    }

                    float behind = Math.Min(group.Total - dealt, March(walker, perCell, walker.ApproachEnd + 1, end, out int behindKill, out float behindShare));
                    Credit(result, armed, walker, geometry, walker.ApproachEnd + 1, end, perGun, behindKill, behindShare);
                    if (group.IsBoss)
                    {
                        result.BehindSingle += group.Count * behind * walker.Scale;
                    }
                    else
                    {
                        result.BehindStream += group.Count * behind * walker.Scale;
                    }
                }
            }

            return result;
        }

        // The gun's damage per creature of the group on each route cell of the segment in its reach (added into perCell); returns that per-cell amount.
        private static float PerCell(Gun gun, Walker walker, WaveLoad load, BalanceRules rules, List<Walker> walkers, LevelGeometry geometry, int from, int to, float speed, float bossShare, float[] perCell)
        {
            WaveGroup group = walker.Group;
            if (group.Flies ? !gun.Air : !gun.Ground)
            {
                return 0f;
            }

            int[] reach = geometry.InReach(gun.Spec.Spot, Reach(gun), walker.Route);
            int cells = 0;
            int first = -1;
            foreach (int index in reach)
            {
                if (index >= from && index <= to)
                {
                    cells++;
                    first = first < 0 ? index : first;
                }
            }

            if (cells == 0)
            {
                return 0f;
            }

            float factor = group.Flies ? gun.Spec.Tower.AirFactor : gun.Spec.Tower.GroundFactor;
            float hit = group.Flies ? gun.AirHit : gun.GroundHit;
            float rate = gun.Dps * gun.Spec.Efficiency * rules.Utilisation * rules.RouteFire * gun.Spec.Slow * factor * group.Share(gun.Spec.Tower.ArmourFactor) * Ledger.Overkill(hit, group.Total);
            float exposure = cells / speed;
            float seconds;
            if (group.IsBoss)
            {
                // A boss has the guns to itself; its mechanics take their share of the fight away.
                seconds = exposure * bossShare;
            }
            else
            {
                // The stream shares each gun: one creature's time in reach, at most the gun's time per arriving creature.
                float arrivals = Arrivals(gun, walker, walkers, geometry, group.Flies, first, group.Flies ? load.AirWindow : load.Span);
                seconds = arrivals > 0f ? Math.Min(exposure, 1f / arrivals) : exposure;
                if (gun.Spec.Tower.Splash > 0f)
                {
                    rate *= Ledger.Splash(gun.Spec.Tower.Splash, arrivals / speed, gun.SplashCap);
                }
            }

            float perCreature = rate * seconds / cells;
            foreach (int index in reach)
            {
                if (index >= from && index <= to)
                {
                    perCell[index] += perCreature;
                }
            }

            return perCreature;
        }

        // Creatures a second arriving where the gun first sees this group: every group of the same layer still alive there
        // whose route the gun also reaches (the last pass's kills thin it), over the wave's spawn span.
        private static float Arrivals(Gun gun, Walker walker, List<Walker> walkers, LevelGeometry geometry, bool flying, int first, float span)
        {
            float count = 0f;
            foreach (Walker other in walkers)
            {
                if (other.Group.Flies != flying || other.Group.IsBoss)
                {
                    continue;
                }

                int seen = other == walker ? first : FirstInReach(gun, other, geometry);
                if (seen < 0 || (other.KillIndex >= 0 && other.KillIndex < seen))
                {
                    continue;
                }

                count += other.Group.Count;
            }

            return count / Math.Max(1f, span);
        }

        private static int FirstInReach(Gun gun, Walker walker, LevelGeometry geometry)
        {
            foreach (int index in geometry.InReach(gun.Spec.Spot, Reach(gun), walker.Route))
            {
                if (index >= walker.Start && index <= walker.ApproachEnd)
                {
                    return index;
                }
            }

            return -1;
        }

        // Walks the segment: the damage per creature accumulated until the creature is dead; the share of the last cell's damage used.
        private static float March(Walker walker, float[] perCell, int from, int to, out int killIndex, out float lastShare)
        {
            float total = walker.Group.Total;
            float dealt = 0f;
            killIndex = -1;
            lastShare = 1f;
            for (int i = from; i <= to; i++)
            {
                float here = perCell[i];
                if (here <= 0f)
                {
                    continue;
                }

                if (dealt + here >= total)
                {
                    lastShare = (total - dealt) / here;
                    dealt = total;
                    killIndex = i;
                    break;
                }

                dealt += here;
            }

            return dealt;
        }

        // Each gun's share of what the group took on the segment: its per-cell fire on every cell walked alive, the kill cell in part.
        private static void Credit(RouteResult result, List<Gun> armed, Walker walker, LevelGeometry geometry, int from, int to, float[] perGun, int killIndex, float lastShare)
        {
            int last = killIndex >= 0 ? killIndex : to;
            for (int g = 0; g < armed.Count; g++)
            {
                if (perGun[g] <= 0f)
                {
                    continue;
                }

                float sum = 0f;
                foreach (int index in geometry.InReach(armed[g].Spec.Spot, Reach(armed[g]), walker.Route))
                {
                    if (index >= from && index <= last)
                    {
                        sum += perGun[g] * (index == killIndex ? lastShare : 1f);
                    }
                }

                result.Credit(armed[g].Spec.Tower.Id, sum * walker.Group.Count * walker.Scale);
            }
        }

        private static float Reach(Gun gun) => gun.Reach;

        private static List<Gun> Arm(LevelInput level, BalanceRules rules, List<RouteGun> guns)
        {
            var armed = new List<Gun>(guns.Count);
            foreach (RouteGun spec in guns)
            {
                TowerStats tower = spec.Tower;
                if (tower == null || spec.Level < 0 || spec.Level >= tower.Levels.Length)
                {
                    continue;
                }

                TowerLevelStats stats = tower.Levels[spec.Level];
                TowerBoost boost = level.BoostOf(tower.Id);
                float damage = stats.Damage * boost.Damage;
                RouteGun resolved = spec;
                resolved.Efficiency = spec.Efficiency >= 0f ? spec.Efficiency : tower.Efficiency;
                resolved.Slow = spec.Slow > 0f ? spec.Slow : 1f;
                armed.Add(new Gun
                {
                    Spec = resolved,
                    Dps = stats.Interval > 0f ? damage * boost.Rate / stats.Interval : 0f,
                    GroundHit = damage * tower.GroundFactor,
                    AirHit = damage * tower.AirFactor,
                    Ground = Layer.Has(tower.Targets, Layer.Ground),
                    Air = Layer.Has(tower.Targets, Layer.Air),
                    SplashCap = tower.Splash > 0f ? Ledger.SplashCap(tower, rules) : 1f,
                    Reach = stats.Range * boost.Range,
                });
            }

            return armed;
        }

        // Every group on its route: path spawns from the start, nest and pool creatures where they join the main road, a boss from the
        // level's entrance where that meets the road. Carried creatures of the wave before (Ledger.LoadOf) scale the stream groups up.
        private static List<Walker> Place(LevelInput level, WaveLoad load)
        {
            LevelGeometry geometry = level.Geometry;
            var walkers = new List<Walker>();
            float streamTotal = 0f;
            foreach (WaveGroup group in load.Groups)
            {
                if (group.Count <= 0 || group.Total <= 0f)
                {
                    continue;
                }

                LevelGeometry.Route route;
                int start = 0;
                if (group.Flies)
                {
                    // Flyers follow their flight path, or the road itself on a level without one (Crystal Plains); the gate never holds them.
                    route = geometry.AirRoutes.Count > 0 ? geometry.AirRouteFor(group.Path) : geometry.GroundRouteFor(group.Path);
                }
                else if (group.Source == WaveLoad.EntranceSource)
                {
                    route = geometry.GroundRoutes[0];
                    start = JoinIndex(route, level.Shape.EntranceRemaining);
                }
                else if (group.Source != 0)
                {
                    route = geometry.GroundRoutes[0];
                    start = JoinIndex(route, MeanNestRemaining(level.Shape));
                }
                else
                {
                    route = geometry.GroundRouteFor(group.Path);
                }

                if (route == null || route.Cells.Count == 0)
                {
                    continue;
                }

                int approachEnd = !group.Flies && route.GateIndex >= 0 && start <= route.GateIndex ? route.GateIndex : route.Cells.Count - 1;
                walkers.Add(new Walker { Group = group, Route = route, Start = start, ApproachEnd = approachEnd });
                streamTotal += !group.Flies && !group.IsBoss ? group.Count * group.Total : 0f;
            }

            float scale = streamTotal > 0f && load.StreamHealth > streamTotal ? load.StreamHealth / streamTotal : 1f;
            foreach (Walker walker in walkers)
            {
                walker.Scale = !walker.Group.Flies && !walker.Group.IsBoss ? scale : 1f;
            }

            return walkers;
        }

        private static int JoinIndex(LevelGeometry.Route route, float remaining) =>
            Math.Max(0, Math.Min(route.Cells.Count - 1, (int)Math.Round((1f - Math.Max(0f, Math.Min(1f, remaining))) * (route.Cells.Count - 1))));

        private static float MeanNestRemaining(LevelShape shape)
        {
            if (shape.NestRemaining.Count == 0)
            {
                return 1f;
            }

            float sum = 0f;
            foreach (float remaining in shape.NestRemaining)
            {
                sum += remaining;
            }

            return sum / shape.NestRemaining.Count;
        }

        private static int LongestRoute(LevelGeometry geometry)
        {
            int longest = 1;
            foreach (LevelGeometry.Route route in geometry.GroundRoutes)
            {
                longest = Math.Max(longest, route.Cells.Count);
            }

            foreach (LevelGeometry.Route route in geometry.AirRoutes)
            {
                longest = Math.Max(longest, route.Cells.Count);
            }

            return longest;
        }
    }
}
