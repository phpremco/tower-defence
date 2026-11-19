using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;

/// <summary>
/// Tower duel (no assets written): one tower beside a straight road against a stream of one species, simulated frame by
/// frame with the game's own rules (Tower.Update with Cadence, the furthest-along target, homing shots that are wasted on
/// a creature that died before they land, the beam's lock on a new target). Prints per tower level and species the useful
/// damage per second (health actually removed, per second that a creature was in reach), the share of the data's DPS
/// that is, and that per coin of the tower's total price. By default the Laser plays as its asset says (sticky beam, lock
/// and sweep from TowerDefinition); variants in Tools/PlayTest/duel_arg.txt (optional): "lock=", "swing=" (10 = no sweep),
/// "sticky=0|1", "speed=", "l1d=/l1i=" (damage/interval per level). The game before 2026-11-05: sticky=0, swing=10, speed=40.
/// unity command run_script --file Tools/PlayTest/Scripts/LaserDuel.cs --entry LaserDuel.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class LaserDuel
{
    private const string ArgPath = "Tools/PlayTest/duel_arg.txt";
    private const string OutPath = "Tools/PlayTest/laser_duel.txt";
    private const float Dt = 1f / 60f;
    private const float TowerSide = 1.8f;
    private const float LaneHalfWidth = 0.6f;
    private const float StartX = -14f;
    private const float EndX = 14f;
    private const float HitDistance = 0.15f;
    private const int Count = 60;
    private const float NoSweep = 10f;

    private sealed class Gun
    {
        public string Name;
        public int Cost;
        public float Damage;
        public float Interval;
        public float Range;
        public float Speed;
        public bool Beam;
        public float Lock;
        public float Swing;
        public bool Sticky;

        /// <summary>Round 85: the pulse that kills costs the share of it the creature needed (Tower.ReleaseBeam); false is the beam of rounds 23 to 84.</summary>
        public bool Conserve = true;
    }

    private sealed class Creature
    {
        public float X;
        public float Z;
        public float Hp;
        public float Spawn;
        public bool Alive = true;
        public bool Entered;
    }

    private sealed class Shot
    {
        public float X;
        public float Z;
        public Creature Target;
        public float Damage;
    }

    private static readonly Dictionary<string, float[]> Overrides = new Dictionary<string, float[]>();

    public static string Run()
    {
        var args = new Dictionary<string, float>();
        if (File.Exists(ArgPath))
        {
            foreach (string raw in File.ReadAllLines(ArgPath))
            {
                string[] kv = raw.Split('=');
                if (kv.Length == 2)
                {
                    args[kv[0].Trim()] = float.Parse(kv[1].Trim(), CultureInfo.InvariantCulture);
                }
            }
        }

        float Arg(string key, float fallback) => args.TryGetValue(key, out float v) ? v : fallback;

        Overrides.Clear();
        if (args.ContainsKey("speed"))
        {
            Overrides["speed"] = new[] { args["speed"] };
        }

        foreach (string key in new[] { "l1", "l2", "l3" })
        {
            if (args.ContainsKey(key + "d"))
            {
                Overrides[key] = new[] { args[key + "d"], args[key + "i"] };
            }
        }

        var guns = new List<Gun>();
        AddLevels(guns, "Gun", "Assets/_Project/Data/Towers/PulseRifle.asset", false, 0f, 0f, false);
        var laserAsset = AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/Laser.asset");
        float defaultSwing = laserAsset.BeamSweepSeconds > 0f ? laserAsset.BeamSweepSeconds : NoSweep;
        AddLevels(guns, "Laser", "Assets/_Project/Data/Towers/Laser.asset", true, Arg("lock", laserAsset.BeamLockSeconds), Arg("swing", defaultSwing), Arg("sticky", 1f) > 0.5f);
        if (args.ContainsKey("cmpLock"))
        {
            AddLevels(guns, "LaserB", "Assets/_Project/Data/Towers/Laser.asset", true, Arg("cmpLock", 0.35f), Arg("cmpSwing", 0.35f), Arg("cmpSticky", 0f) > 0.5f);
        }

        // Species of Crystal Plains: health x the wave multiplier, walk speed in reach, spawn interval in the group.
        var species = new (string Name, float Health, float Speed, float Interval)[]
        {
            ("Skitter", 11f, 1.6f, 0.2f),
            ("Skitter", 11f, 1.6f, 0.45f),
            ("Wisp", 36f, 1.2f, 1.3f),
            ("Prowler", 62f, 2.4f, 0.6f),
            ("Roller", 110f, 4.13f, 1f),
            ("Cragback", 160f, 0.6f, 2.6f),
            ("Boss", 3000f, 0.55f, 60f),
        };
        float[] multipliers = { 1f, 1.5f, 2.2f };

        var sb = new StringBuilder();
        sb.AppendLine($"Tower duel {DateTime.Now:yyyy-MM-dd HH:mm}, laser lock {Arg("lock", laserAsset.BeamLockSeconds)} swing {Arg("swing", defaultSwing)} sticky {Arg("sticky", 1f)}");
        sb.AppendLine("useful DPS (share of data DPS) [per 100 coins of total price]; columns: wave health x1, x1.5, x2.2");
        foreach (var s in species)
        {
            sb.AppendLine($"{s.Name} {s.Health} hp, speed {s.Speed}, every {s.Interval}s");
            foreach (Gun gun in guns)
            {
                sb.Append($"  {gun.Name,-9}");
                foreach (float m in multipliers)
                {
                    float useful = Duel(gun, s.Health * m, s.Speed, s.Interval, s.Name == "Boss" ? 1 : Count);
                    float nominal = gun.Damage / gun.Interval;
                    sb.Append($" {useful,6:0.0} ({useful / nominal * 100f,3:0}%) [{useful / gun.Cost * 100f,5:0.0}]");
                }

                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("Holding power: toughest creature (health) stopped with at most 10% leaking, and that as health per second per 100 coins");
        foreach (var s in species)
        {
            if (s.Name == "Boss")
            {
                continue;
            }

            sb.AppendLine($"{s.Name}, speed {s.Speed}, every {s.Interval}s");
            foreach (Gun gun in guns)
            {
                float held = Hold(gun, s.Speed, s.Interval);
                sb.AppendLine($"  {gun.Name,-9} {held,7:0.0} hp  [{held / s.Interval / gun.Cost * 100f,5:0.0}]");
            }
        }

        File.WriteAllText(OutPath, sb.ToString());
        return sb.ToString();
    }

    private const string ShareOutPath = "Tools/PlayTest/laser_share.txt";
    private const float LightFireOnArmour = 0.2f;

    /// <summary>
    /// Round 85: what a Laser is worth beside a Gun Turret per difficulty, on the ground creatures of the stored waves as
    /// they spawn (health x the difficulty factor x the wave multiplier; armour at what it costs light fire). Per
    /// difficulty, weighted by the health the waves send: the useful damage per second of each gun as a share of its data
    /// DPS, and the useful DPS per coin of the Laser over that of the Gun Turret, with the beam of rounds 23 to 84
    /// ("before": every kill costs the sweep time alone) and the beam of round 85 ("after"). Levels: "levels=C1_01,C1_02"
    /// in Tools/PlayTest/duel_arg.txt (default C1_01, the level Remco played). No asset is written.
    /// unity command run_script --file Tools/PlayTest/Scripts/LaserDuel.cs --entry LaserDuel.Difficulties --timeout_ms 900000 --timeout 960
    /// </summary>
    public static string Difficulties()
    {
        string levels = "C1_01";
        if (File.Exists(ArgPath))
        {
            foreach (string raw in File.ReadAllLines(ArgPath))
            {
                if (raw.StartsWith("levels=", StringComparison.Ordinal))
                {
                    levels = raw.Substring("levels=".Length).Trim();
                }
            }
        }

        Overrides.Clear();
        var gun = new List<Gun>();
        AddLevels(gun, "Gun", "Assets/_Project/Data/Towers/PulseRifle.asset", false, 0f, 0f, false);
        var laserAsset = AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/Laser.asset");
        var after = new List<Gun>();
        AddLevels(after, "Laser", "Assets/_Project/Data/Towers/Laser.asset", true, laserAsset.BeamLockSeconds, laserAsset.BeamSweepSeconds, true);
        var before = new List<Gun>();
        AddLevels(before, "Laser", "Assets/_Project/Data/Towers/Laser.asset", true, laserAsset.BeamLockSeconds, laserAsset.BeamSweepSeconds, true);
        before.ForEach(g => g.Conserve = false);
        int[] picks = { 0, gun.Count - 1 };

        var sb = new StringBuilder();
        sb.AppendLine($"Laser beside Gun Turret per difficulty {DateTime.Now:yyyy-MM-dd HH:mm}; levels {levels}; ground creatures of the stored waves as spawned, weighted by health sent.");
        sb.AppendLine("useful = damage that removed health per second something was in reach; share = useful over the data DPS; coin = useful per coin of the total price of the tower.");
        sb.AppendLine("| Difficulty | Median toughness as spawned (light fire) | Gun L1 share | Gun L3 share | Laser L1 share before -> after | Laser L3 share before -> after | Laser/Gun per coin L1 before -> after | Laser/Gun per coin L3 before -> after | Laser share of the damage of a Gun+Laser pair, L1 before -> after | L3 before -> after |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        string[] names = { "Easy", "Medium", "Hard", "Psycho" };
        var species = new StringBuilder();
        species.AppendLine();
        species.AppendLine("Per species, at the median toughness it spawns with on that difficulty (bodies = its share of the ground creatures sent):");
        species.AppendLine("| Difficulty | Species | Bodies | Toughness (light fire) | Pulses to kill, Laser L1 / L3 | Laser L1 share before -> after | Laser L3 share before -> after | Gun L1 / L3 share | Laser/Gun per coin L1 before -> after | L3 before -> after |");
        species.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        var packed = new StringBuilder();
        packed.AppendLine();
        packed.AppendLine("Kill rate in a packed stream (a body every 0.05 s, so the gun never waits for a target): health removed per second as a multiple of the data DPS, per tower level, at the toughness the species spawns with. A gun that wastes part of a shot on a small body reads under x1; the beam of rounds 23 to 84 read over x1, because a kill cost it the sweep time alone.");
        packed.AppendLine("| Difficulty | Species | Toughness | Gun L1 / L2 / L3 | Laser L1 before -> after | Laser L2 before -> after | Laser L3 before -> after |");
        packed.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
        var holds = new StringBuilder();
        holds.AppendLine();
        holds.AppendLine("Holding power: one tower alone beside a stream of that species at its spacing; holds = the multiple of the health it spawns with that the tower still stops (at most a tenth leaking). Above x1 one tower stops the stream as the level sends it.");
        holds.AppendLine("| Difficulty | Species (spacing) | Gun L1 holds | Gun L3 holds | Laser L1 holds before -> after | Laser L3 holds before -> after | Laser/Gun held per coin L1 before -> after | L3 before -> after |");
        holds.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (string name in names)
        {
            var kinds = new Dictionary<string, (List<(float Health, int Count)> Groups, float Speed, float Interval)>();
            int bodies = 0;
            var difficulty = AssetDatabase.LoadAssetAtPath<Game.Levels.DifficultyProfile>("Assets/_Project/Data/Difficulty/" + name + ".asset");
            // (species, bucket of toughness) -> weight in health, with the speed and spacing of the group.
            var buckets = new Dictionary<string, (float Health, float Speed, float Interval, float Weight)>();
            var all = new List<(float Health, float Weight)>();
            foreach (string key in levels.Split(','))
            {
                var waves = AssetDatabase.LoadAssetAtPath<Game.Levels.LevelWaves>("Assets/_Project/Data/Waves/Waves_" + key.Trim() + ".asset");
                var level = AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>("Assets/_Project/Data/Levels/Level_" + key.Trim() + ".asset");
                foreach (Game.Levels.WaveDefinition wave in waves.For(difficulty))
                {
                    foreach (Game.Levels.SpawnGroup group in wave.Groups)
                    {
                        if (group.Enemy == null || group.Enemy.IsBoss || group.Enemy.Flies)
                        {
                            continue;
                        }

                        float scale = difficulty.HealthMultiplier * level.EnemyHealthScale * wave.HealthMultiplier;
                        float tough = (group.Enemy.MaxHealth + group.Enemy.Armor / LightFireOnArmour) * scale;
                        float weight = tough * group.Count;
                        // Buckets a sixth apart, so a few hundred groups are a few dozen duels.
                        double step = Math.Round(Math.Log(tough) / Math.Log(1.17));
                        string id = group.Enemy.name + "|" + step.ToString(CultureInfo.InvariantCulture);
                        buckets.TryGetValue(id, out var b);
                        buckets[id] = ((float)Math.Pow(1.17, step), group.Enemy.Speed, Math.Max(0.2f, group.Interval), b.Weight + weight);
                        all.Add((tough, weight));
                        if (!kinds.TryGetValue(group.Enemy.name, out var kind))
                        {
                            kind = (new List<(float Health, int Count)>(), group.Enemy.Speed, Math.Max(0.2f, group.Interval));
                            kinds[group.Enemy.name] = kind;
                        }

                        kind.Groups.Add((tough, group.Count));
                        bodies += group.Count;
                    }
                }
            }

            all.Sort((a, b) => a.Health.CompareTo(b.Health));
            float total = 0f;
            all.ForEach(a => total += a.Weight);
            float half = 0f;
            float median = 0f;
            foreach (var a in all)
            {
                half += a.Weight;
                if (half >= total * 0.5f)
                {
                    median = a.Health;
                    break;
                }
            }

            // Per gun: health-weighted useful DPS.
            var useful = new Dictionary<string, float>();
            void Add(string id, Gun g, float health, float speed, float interval, float weight)
            {
                useful.TryGetValue(id, out float sum);
                useful[id] = sum + Duel(g, health, speed, interval, 30) * weight / total;
            }

            foreach (var pair in buckets)
            {
                var b = pair.Value;
                foreach (int i in picks)
                {
                    Add("gun" + i, gun[i], b.Health, b.Speed, b.Interval, b.Weight);
                    Add("before" + i, before[i], b.Health, b.Speed, b.Interval, b.Weight);
                    Add("after" + i, after[i], b.Health, b.Speed, b.Interval, b.Weight);
                }
            }

            string Share(float value, Gun g) => (value / (g.Damage / g.Interval) * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
            string Coin(float laser, Gun l, float pulse, Gun p) => "x" + (laser / l.Cost / (pulse / p.Cost)).ToString("0.00", CultureInfo.InvariantCulture);
            string Pair(float laser, float pulse) => (laser / (laser + pulse) * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
            int lo = picks[0];
            int hi = picks[1];
            foreach (var kind in kinds)
            {
                var groups = kind.Value.Groups;
                groups.Sort((a, b) => a.Health.CompareTo(b.Health));
                int count = 0;
                groups.ForEach(g => count += g.Count);
                int seen = 0;
                float tough = 0f;
                foreach (var g in groups)
                {
                    seen += g.Count;
                    if (seen * 2 >= count)
                    {
                        tough = g.Health;
                        break;
                    }
                }

                float D(Gun g) => Duel(g, tough, kind.Value.Speed, kind.Value.Interval, 30);
                float g1 = D(gun[lo]);
                float g3 = D(gun[hi]);
                float b1 = D(before[lo]);
                float b3 = D(before[hi]);
                float a1 = D(after[lo]);
                float a3 = D(after[hi]);
                string Packed(Gun g) => "x" + (Duel(g, tough, kind.Value.Speed, 0.05f, 240) / (g.Damage / g.Interval)).ToString("0.00", CultureInfo.InvariantCulture);
                packed.AppendLine($"| {name} | {kind.Key} | {tough.ToString("0", CultureInfo.InvariantCulture)} | {Packed(gun[0])} / {Packed(gun[1])} / {Packed(gun[2])} | {Packed(before[0])} -> {Packed(after[0])} | {Packed(before[1])} -> {Packed(after[1])} | {Packed(before[2])} -> {Packed(after[2])} |");
                float hg1 = Hold(gun[lo], kind.Value.Speed, kind.Value.Interval);
                float hg3 = Hold(gun[hi], kind.Value.Speed, kind.Value.Interval);
                float hb1 = Hold(before[lo], kind.Value.Speed, kind.Value.Interval);
                float hb3 = Hold(before[hi], kind.Value.Speed, kind.Value.Interval);
                float ha1 = Hold(after[lo], kind.Value.Speed, kind.Value.Interval);
                float ha3 = Hold(after[hi], kind.Value.Speed, kind.Value.Interval);
                string X(float held) => "x" + (held / tough).ToString("0.0", CultureInfo.InvariantCulture);
                holds.AppendLine($"| {name} | {kind.Key} ({kind.Value.Interval.ToString("0.##", CultureInfo.InvariantCulture)} s) | {X(hg1)} | {X(hg3)} | {X(hb1)} -> {X(ha1)} | {X(hb3)} -> {X(ha3)} | {Coin(hb1, before[lo], hg1, gun[lo])} -> {Coin(ha1, after[lo], hg1, gun[lo])} | {Coin(hb3, before[hi], hg3, gun[hi])} -> {Coin(ha3, after[hi], hg3, gun[hi])} |");
                species.AppendLine($"| {name} | {kind.Key} | {(count * 100f / bodies).ToString("0", CultureInfo.InvariantCulture)}% | {tough.ToString("0", CultureInfo.InvariantCulture)} | {Math.Ceiling(tough / after[lo].Damage)} / {Math.Ceiling(tough / after[hi].Damage)} | {Share(b1, before[lo])} -> {Share(a1, after[lo])} | {Share(b3, before[hi])} -> {Share(a3, after[hi])} | {Share(g1, gun[lo])} / {Share(g3, gun[hi])} | {Coin(b1, before[lo], g1, gun[lo])} -> {Coin(a1, after[lo], g1, gun[lo])} | {Coin(b3, before[hi], g3, gun[hi])} -> {Coin(a3, after[hi], g3, gun[hi])} |");
            }

            sb.AppendLine($"| {name} | {median.ToString("0", CultureInfo.InvariantCulture)} | {Share(useful["gun" + lo], gun[lo])} | {Share(useful["gun" + hi], gun[hi])} | {Share(useful["before" + lo], before[lo])} -> {Share(useful["after" + lo], after[lo])} | {Share(useful["before" + hi], before[hi])} -> {Share(useful["after" + hi], after[hi])} | {Coin(useful["before" + lo], before[lo], useful["gun" + lo], gun[lo])} -> {Coin(useful["after" + lo], after[lo], useful["gun" + lo], gun[lo])} | {Coin(useful["before" + hi], before[hi], useful["gun" + hi], gun[hi])} -> {Coin(useful["after" + hi], after[hi], useful["gun" + hi], gun[hi])} | {Pair(useful["before" + lo], useful["gun" + lo])} -> {Pair(useful["after" + lo], useful["gun" + lo])} | {Pair(useful["before" + hi], useful["gun" + hi])} -> {Pair(useful["after" + hi], useful["gun" + hi])} |");
        }

        sb.Append(species);
        sb.Append(packed);
        sb.Append(holds);
        sb.AppendLine();
        sb.AppendLine($"Data: Gun Turret L1 {gun[0].Damage}/{gun[0].Interval}s ({gun[0].Cost} coins), L3 {gun[gun.Count - 1].Damage}/{gun[gun.Count - 1].Interval}s ({gun[gun.Count - 1].Cost}); Laser L1 {after[0].Damage}/{after[0].Interval}s ({after[0].Cost}), L3 {after[after.Count - 1].Damage}/{after[after.Count - 1].Interval}s ({after[after.Count - 1].Cost}), sweep {laserAsset.BeamSweepSeconds}s.");
        File.WriteAllText(ShareOutPath, sb.ToString());
        return sb.ToString();
    }

    private static void AddLevels(List<Gun> guns, string name, string path, bool beam, float lockSeconds, float swing, bool sticky)
    {
        var def = AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>(path);
        int total = 0;
        for (int i = 0; i <= def.MaxLevelIndex; i++)
        {
            Game.Towers.TowerLevel level = def.GetLevel(i);
            total += level.Cost;
            float damage = level.Damage;
            float interval = level.FireInterval;
            if (beam && Overrides.TryGetValue("l" + (i + 1), out float[] o))
            {
                damage = o[0];
                interval = o[1];
            }

            guns.Add(new Gun
            {
                Name = $"{name} L{i + 1}", Cost = total, Damage = damage, Interval = interval, Range = level.Range,
                Speed = beam && Overrides.TryGetValue("speed", out float[] sp) ? sp[0] : def.ProjectileSpeed, Beam = beam, Lock = lockSeconds, Swing = swing, Sticky = sticky,
            });
        }
    }

    // Useful damage per second a creature was in reach.
    private static float Duel(Gun gun, float health, float speed, float interval, int count) => Duel(gun, health, speed, interval, count, out _);

    private static float Duel(Gun gun, float health, float speed, float interval, int count, out int leaked)
    {
        leaked = 0;
        var rng = new Random(7);
        var creatures = new List<Creature>();
        for (int i = 0; i < count; i++)
        {
            float jitter = (float)(rng.NextDouble() - 0.5) * 0.9f * interval;
            creatures.Add(new Creature { X = StartX, Z = (float)(rng.NextDouble() * 2 - 1) * LaneHalfWidth, Hp = health, Spawn = i * interval + jitter + interval });
        }

        var shots = new List<Shot>();
        float cooldown = 0f;
        float spare = 0f;
        Creature beamTarget = null;
        bool beamFiring = false;
        float useful = 0f;
        float busy = 0f;
        float rangeSqr = gun.Range * gun.Range;
        for (float t = 0f; t < 600f; t += Dt)
        {
            bool any = false;
            bool pending = false;
            foreach (Creature c in creatures)
            {
                if (!c.Alive)
                {
                    continue;
                }

                if (t >= c.Spawn)
                {
                    c.X += speed * Dt;
                    c.Entered = true;
                }

                if (c.X > EndX)
                {
                    leaked++;
                    c.Alive = false;
                    continue;
                }

                pending = true;
                any |= c.Entered && InRange(c, rangeSqr);
            }

            if (!pending && shots.Count == 0)
            {
                break;
            }

            if (any)
            {
                busy += Dt;
            }

            // Shots fly first (the projectiles update in the same frame as the tower; order does not matter at 60 fps).
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                float dx = shot.Target.X - shot.X;
                float dz = shot.Target.Z - shot.Z;
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                float step = gun.Speed * Dt;
                if (dist <= Math.Max(step, HitDistance))
                {
                    if (shot.Target.Alive && shot.Target.Hp > 0f)
                    {
                        float dealt = Math.Min(shot.Target.Hp, shot.Damage);
                        useful += dealt;
                        shot.Target.Hp -= shot.Damage;
                        if (shot.Target.Hp <= 0f)
                        {
                            shot.Target.Alive = false;
                        }
                    }

                    shots.RemoveAt(i);
                    continue;
                }

                shot.X += dx / dist * step;
                shot.Z += dz / dist * step;
            }

            // The beam lets go of a creature that died or walked out of reach.
            bool swingNow = false;
            if (gun.Beam && gun.Sticky && beamTarget != null && !(beamTarget.Alive && InRange(beamTarget, rangeSqr)))
            {
                if (!beamTarget.Alive && beamFiring)
                {
                    cooldown = Math.Min(cooldown, gun.Conserve ? Math.Max(gun.Swing, cooldown - spare) : gun.Swing);
                    swingNow = true;
                }

                spare = 0f;
                beamTarget = null;
            }

            cooldown -= Dt;
            if (cooldown > 0f)
            {
                continue;
            }

            for (int n = 0; cooldown <= 0f && n < 4; n++)
            {
                Creature target = gun.Beam && gun.Sticky && beamTarget != null ? beamTarget : FindBest(creatures, rangeSqr);
                if (target == null)
                {
                    beamTarget = null;
                    beamFiring = false;
                    break;
                }

                if (gun.Beam && !ReferenceEquals(target, beamTarget))
                {
                    bool chained = gun.Sticky && (swingNow || beamFiring);
                    beamTarget = target;
                    if (!chained)
                    {
                        beamFiring = false;
                        cooldown = gun.Lock;
                        break;
                    }
                }

                spare = gun.Beam ? gun.Interval * (1f - Math.Min(1f, Math.Max(0f, target.Hp / gun.Damage))) : 0f;
                shots.Add(new Shot { X = 0f, Z = TowerSide, Target = target, Damage = gun.Damage });
                beamFiring = true;
                cooldown += gun.Interval;
            }

            if (cooldown < 0f)
            {
                cooldown = 0f;
            }
        }

        return busy > 0f ? useful / busy : 0f;
    }

    private static float Hold(Gun gun, float speed, float interval)
    {
        float lo = 0.5f;
        float hi = 4000f;
        for (int i = 0; i < 24; i++)
        {
            float mid = (float)Math.Sqrt(lo * hi);
            Duel(gun, mid, speed, interval, Count, out int leaked);
            if (leaked <= Count / 10)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private static bool InRange(Creature c, float rangeSqr)
    {
        float dz = c.Z - TowerSide;
        return c.X * c.X + dz * dz <= rangeSqr;
    }

    private static Creature FindBest(List<Creature> creatures, float rangeSqr)
    {
        Creature best = null;
        foreach (Creature c in creatures)
        {
            if (c.Alive && c.Entered && InRange(c, rangeSqr) && (best == null || c.X > best.X))
            {
                best = c;
            }
        }

        return best;
    }
}
