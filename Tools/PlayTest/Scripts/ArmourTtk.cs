using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Combat;
using Game.Drones;
using Game.Enemies;
using Game.Hero;
using Game.PathObjects;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Round 64, the armour audit (no assets written). Every damage source of the game against every armoured troop, from the
/// assets as they stand: damage per hit, hits per second, the share of a hit that works on armour, and the seconds one
/// source needs for one creature at wave multiplier one (the game's own Health class takes the hits), next to the coins
/// times seconds a tower spends on it (build price of that level). Overrides in Tools/PlayTest/r64_ttk_arg.txt, one per line:
/// "factor.&lt;AssetName&gt;=1" a source's armour factor (the state before a fix), "pool.&lt;EnemyId&gt;=160,60" a creature's body and
/// armour, "label=name" names the output Tools/PlayTest/r64_armour_ttk_&lt;name&gt;.txt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ArmourTtk.cs --entry ArmourTtk.Run --timeout_ms 120000 --timeout 180
/// </summary>
public static class ArmourTtk
{
    private const string ArgPath = "Tools/PlayTest/r64_ttk_arg.txt";
    private static readonly string[] Troops = { "Cragback", "Stinger", "Shardback", "Bulwark" };

    private sealed class Source
    {
        public string Name;
        public string Asset;
        public string Type;
        public float Damage;
        public float Interval;
        public float Factor;
        public int Cost;
        public string Note = string.Empty;
    }

    public static string Run()
    {
        var factors = new Dictionary<string, float>();
        var pools = new Dictionary<string, (float Health, float Armour)>();
        string label = "run";
        foreach (string raw in File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0])
        {
            string[] kv = raw.Trim().Split('=');
            if (kv.Length != 2)
            {
                continue;
            }

            string key = kv[0].Trim();
            if (key == "label")
            {
                label = kv[1].Trim();
            }
            else if (key.StartsWith("factor."))
            {
                factors[key.Substring(7)] = Parse(kv[1]);
            }
            else if (key.StartsWith("pool."))
            {
                string[] parts = kv[1].Split(',');
                pools[key.Substring(5)] = (Parse(parts[0]), Parse(parts[1]));
            }
        }

        var sources = new List<Source>();
        foreach (string id in new[] { "PulseRifle", "Laser", "Cannon", "Rocket", "Tesla", "Railgun", "Cryo", "Artillery", "StormRifle", "CryoBeam", "BaseTurret", "Blockade" })
        {
            var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{id}.asset");
            int spent = 0;
            for (int l = 0; l < TowerDefinition.LevelCount; l++)
            {
                TowerLevel level = tower.GetLevel(l);
                spent += level.Cost;
                if (level.Damage <= 0f)
                {
                    continue;
                }

                bool ground = (tower.Targets & Game.Core.TargetLayers.Ground) != 0;
                sources.Add(new Source
                {
                    Name = $"{tower.DisplayName} L{l + 1}", Asset = id, Type = tower.DamageType.ToString(), Damage = level.Damage * tower.GroundDamageFactor, Interval = level.FireInterval,
                    Factor = tower.ArmourFactor, Cost = spent, Note = ground ? (tower.SplashRadius > 0f ? $"splash, up to {(tower.SplashTargets > 0 ? tower.SplashTargets.ToString() : "all")} in the blast" : string.Empty) : "air only (no armoured flyer exists)",
                });
            }
        }

        foreach (string id in new[] { "Weapon_EnergySword", "Weapon_Blaster" })
        {
            var weapon = AssetDatabase.LoadAssetAtPath<HeroWeaponDefinition>($"Assets/_Project/Data/Hero/{id}.asset");
            sources.Add(new Source { Name = "Hero " + weapon.DisplayName.ToLowerInvariant(), Asset = id, Type = weapon.DamageType.ToString(), Damage = weapon.Damage, Interval = weapon.AttackInterval, Factor = weapon.ArmourFactor });
        }

        var shock = AssetDatabase.LoadAssetAtPath<HeroAbilityDefinition>("Assets/_Project/Data/Hero/Abilities/Ability_Shockwave.asset");
        sources.Add(new Source { Name = "Hero shockwave", Asset = "Ability_Shockwave", Type = shock.DamageType.ToString(), Damage = shock.Damage, Interval = shock.Cooldown, Factor = shock.ArmourFactor, Note = "one blast per cooldown, everything in its radius" });
        var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>("Assets/_Project/Data/Drones/AttackDrone.asset");
        sources.Add(new Source { Name = "Attack drone", Asset = "AttackDrone", Type = drone.DamageType.ToString(), Damage = drone.Damage, Interval = drone.FireInterval, Factor = drone.ArmourFactor, Cost = drone.Cost });
        foreach (string id in new[] { "Mine", "ExplosiveBarrel", "LavaBall", "GroundDrone" })
        {
            var gadget = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>($"Assets/_Project/Data/PathObjects/{id}.asset");
            bool blast = gadget.Kind == PathObjectKind.Mine;
            sources.Add(new Source
            {
                Name = gadget.DisplayName, Asset = id, Type = gadget.DamageType.ToString(), Damage = blast ? gadget.Damage : gadget.Damage * 0.1f, Interval = blast ? 0f : 0.1f, Factor = gadget.ArmourFactor, Cost = gadget.Cost,
                Note = blast ? "one blast" : $"{gadget.Damage:0} a second while it touches (damage over time)",
            });
        }

        foreach (Source source in sources)
        {
            if (factors.TryGetValue(source.Asset, out float factor))
            {
                source.Note = $"{(source.Note.Length > 0 ? source.Note + "; " : string.Empty)}factor overridden (asset {source.Factor.ToString("0.##", CultureInfo.InvariantCulture)})";
                source.Factor = factor;
            }
        }

        var text = new StringBuilder();
        text.Append($"Armour audit {DateTime.Now:yyyy-MM-dd HH:mm} ({label}): seconds for one source to kill one creature at wave multiplier one; in brackets coins x seconds / 1000 for a bought source.\n");
        text.Append("A blast is given as the share of the creature one blast takes. 'soft' is the same toughness without armour, for scale.\n\n");
        text.Append($"{"source",-22}{"type",-10}{"dmg",6}{"/s",7}{"dps",7}{"armour x",9}");
        var bodies = new List<(string Id, float Health, float Armour)>();
        foreach (string id in Troops)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
            (float Health, float Armour) pool = pools.TryGetValue(id, out (float, float) given) ? given : (enemy.MaxHealth, enemy.Armor);
            bodies.Add((id, pool.Health, pool.Armour));
            text.Append($"{id + " " + pool.Health.ToString("0") + "+" + pool.Armour.ToString("0"),22}");
        }

        text.Append($"{"soft 220",12}  note\n");
        foreach (Source source in sources)
        {
            float rate = source.Interval > 0f ? 1f / source.Interval : 0f;
            text.Append($"{source.Name,-22}{source.Type,-10}{F(source.Damage, "0.#"),6}{F(rate, "0.##"),7}{F(source.Damage * rate, "0.#"),7}{F(source.Factor, "0.##"),9}");
            foreach ((string Id, float Health, float Armour) body in bodies)
            {
                text.Append($"{Cell(source, body.Health, body.Armour),22}");
            }

            text.Append($"{Cell(source, 220f, 0f),12}  {source.Note}\n");
        }

        Directory.CreateDirectory("Tools/PlayTest");
        File.WriteAllText($"Tools/PlayTest/r64_armour_ttk_{label}.txt", text.ToString());
        return text.ToString();
    }

    // Seconds to kill through the game's own Health; a one-off blast shows the share of the creature it takes.
    private static string Cell(Source source, float body, float armour)
    {
        var health = new Health(body, armour);
        if (source.Interval <= 0f)
        {
            float dealt = health.TakeDamage(source.Damage, source.Factor);
            return health.IsDead ? "kills" : $"{dealt / (body + armour) * 100f:0}% of it";
        }

        int hits = 0;
        while (!health.IsDead && hits < 100000)
        {
            health.TakeDamage(source.Damage, source.Factor);
            hits++;
        }

        float seconds = hits * source.Interval;
        return source.Cost > 0 ? $"{F(seconds, "0.0")} s ({F(seconds * source.Cost / 1000f, "0.0")})" : $"{F(seconds, "0.0")} s";
    }

    private static float Parse(string value) => float.Parse(value.Trim(), CultureInfo.InvariantCulture);

    private static string F(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
}
