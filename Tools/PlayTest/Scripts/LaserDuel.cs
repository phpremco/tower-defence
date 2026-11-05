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
                    cooldown = Math.Min(cooldown, gun.Swing);
                    swingNow = true;
                }

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
