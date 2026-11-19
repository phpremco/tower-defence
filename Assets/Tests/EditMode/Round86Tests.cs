using System.Collections.Generic;
using System.Text;
using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Editor;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Round 86: the one boss rule (a level's boss costs a multiple of the toughest ordinary creature before it, under
    /// shells and under light fire), held on every stored boss; a cell's scalar as a plain factor on boss and crowd alike;
    /// a boss's phases on its whole bar and a shard's share of the armour; the beam's budget for the pulse that kills,
    /// settled as the pulse leaves.
    /// </summary>
    public sealed class Round86Tests
    {
        private const string ReportPath = "Tools/PlayTest/boss_rule.txt";

        /// <summary>The stored multipliers are rounded to three decimals, the boss scale too: one per cent covers both.</summary>
        private const float RuleSlack = 0.01f;

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

        // ---- The boss rule ---------------------------------------------------------------------------------------

        [Test]
        public void ABoss_CostsItsMultiple_UnderShellsAndUnderLightFire()
        {
            BalanceRules rules = BalanceRules.Default;
            var heavy = new EnemyStats { Health = 60f, Armor = 160f };
            var soft = new EnemyStats { Health = 300f };
            var boss = new EnemyStats { Health = 800f, Armor = 200f, IsBoss = true };
            var wave = new WaveSpec();
            wave.Groups.Add(new GroupSpec { Enemy = heavy, Count = 3 });
            wave.Groups.Add(new GroupSpec { Enemy = soft, Count = 3 });
            var yardstick = new BossYardstick(rules);
            Assert.IsFalse(yardstick.HasMet(false), "nothing met yet");
            yardstick.Meet(wave, 2f);

            // The soft creature has the bigger bar (600 against 440), the plated one costs light fire more (1720 against 600).
            Assert.AreEqual(600f, yardstick.ToughestBar(false), 0.01f);
            Assert.AreEqual((60f + 160f / rules.LightFireOnArmour) * 2f, yardstick.ToughestLight(false), 0.01f);

            float multiple = yardstick.Multiple(boss, 3f);
            (float bar, float light) = yardstick.Over(boss, multiple);
            Assert.GreaterOrEqual(bar, 3f - 0.001f, "three times the toughest bar at least");
            Assert.GreaterOrEqual(light, 3f - 0.001f, "three times the toughest creature under light fire at least");
            Assert.AreEqual(3f, System.Math.Min(bar, light), 0.001f, "and no bigger than the row that asks most needs");
        }

        [Test]
        public void AWalkingBoss_IsNotHeldAgainstFlyers_AFlyingBossIs()
        {
            BalanceRules rules = BalanceRules.Default;
            var walker = new EnemyStats { Health = 100f };
            var flyer = new EnemyStats { Health = 400f, Flies = true };
            var wave = new WaveSpec();
            wave.Groups.Add(new GroupSpec { Enemy = walker, Count = 1 });
            wave.Groups.Add(new GroupSpec { Enemy = flyer, Count = 1 });
            var yardstick = new BossYardstick(rules);
            yardstick.Meet(wave, 1f);
            Assert.AreEqual(100f, yardstick.ToughestBar(false), 0.01f, "a raid's flyers do not size a boss that walks");
            Assert.AreEqual(400f, yardstick.ToughestBar(true), 0.01f, "a flying boss stands over everything met");
        }

        [Test]
        public void TheLightFireRow_IsTheCombatRulesOwn()
        {
            CombatRules table = RoleMatrix.Rules();
            BalanceRules rules = BalanceRules.Default;
            Assert.AreEqual(table.ProfileOf(DamageType.Bullet, string.Empty).Armour, rules.LightFireOnArmour, 0.0001f, "bullets on armour");
            Assert.AreEqual(table.ProfileOf(DamageType.Beam, string.Empty).Armour, rules.LightFireOnArmour, 0.0001f, "beams on armour");
        }

        /// <summary>
        /// The rule itself, on the waves as stored: every level boss costs BossOverRegular times the toughest ordinary
        /// creature met before it under both rows, and stands on that multiple (it is the one thing that sizes it). A wave
        /// that carries a surprise counts as the solver counts it: as the same wave without its surprise
        /// (CampaignModel.CountsAs). The report also shows each boss against the creatures as they really walked in.
        /// </summary>
        [Test]
        public void EveryLevelBoss_StandsOnItsMultiple()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            CampaignModel model = loaded.Model;
            BalanceRules rules = model.Rules;
            var report = new StringBuilder("Every level boss as stored against the toughest ordinary creature met before it (round 86). 'rule' reads a surprise wave as the solver does (the same wave without its surprise); 'as walked in' reads every creature at its own multiplier.\n");
            var failures = new List<string>();
            int bosses = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
                int index = model.IndexOf(key);
                WaveScriptSpec script = WaveScripts.ByKey(key);
                if (index < 0 || script == null)
                {
                    continue;
                }

                // The solve leaves the flyer floors it found on the script (the Easy purse rule, the first-flyer rule); the
                // calm reference below is read with them, as the stored waves were.
                model.BuildWaves(index, script, out _);
                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    var stored = new List<WaveSpec>();
                    foreach (WaveDefinition wave in node.Level.GetWaves(loaded.Catalog.Difficulties[d]))
                    {
                        stored.Add(BalanceLedger.Spec(wave, model.Enemies));
                    }

                    float[] counts = model.CountsAs(index, script, d, stored);
                    float book = rules.BossBookShare * rules.DifficultyScaleFor(d) * script.TargetScaleFor(d);
                    float over = rules.BossOverRegularFor(d);
                    var ruleStick = new BossYardstick(rules);
                    var walkedStick = new BossYardstick(rules);
                    for (int i = 0; i < stored.Count; i++)
                    {
                        WaveSpec wave = stored[i];
                        foreach (GroupSpec group in wave.Groups)
                        {
                            EnemyStats boss = group.Enemy;
                            if (boss == null || !boss.IsBoss || boss.IsContinentBoss)
                            {
                                continue;
                            }

                            bosses++;
                            float multiple = wave.HealthMultiplier * (wave.BossHealthScale > 0f ? wave.BossHealthScale : 1f);
                            BossYardstick stick = ruleStick;
                            if (!stick.HasMet(boss.Flies))
                            {
                                // Nothing before it: the toughest creature of its own wave.
                                stick = new BossYardstick(rules);
                                stick.Meet(wave, wave.HealthMultiplier);
                            }

                            (float bar, float light) = stick.Over(boss, multiple);
                            (float walkedBar, float walkedLight) = walkedStick.HasMet(boss.Flies) ? walkedStick.Over(boss, multiple) : (bar, light);
                            // What the rule gives: the multiple that meets both rows, or the book floor where that is more.
                            float expected = System.Math.Max(stick.Multiple(boss, over), book);
                            string where = $"{node.Level.name} {DifficultyNames[d]} wave {i + 1} {boss.Id}";
                            report.Append(Line($"  {where}: rule x{bar:0.00} bars, x{light:0.00} under light fire | as walked in x{walkedBar:0.00} bars, x{walkedLight:0.00} under light fire\n"));
                            if (bar < over * (1f - RuleSlack) || light < over * (1f - RuleSlack))
                            {
                                failures.Add(Line($"{where}: x{bar:0.00} bars and x{light:0.00} under light fire of the toughest creature before it, under x{over:0.##}"));
                            }
                            else if (System.Math.Abs(multiple / expected - 1f) > RuleSlack)
                            {
                                failures.Add(Line($"{where}: walks in at x{multiple:0.000} of its book where the rule gives x{expected:0.000} (x{over:0.##} on the row that asks most, or its book health x{book:0.000} as stored): something else sized it"));
                            }
                        }

                        if (!wave.AirRaid)
                        {
                            ruleStick.Meet(wave, counts[i]);
                            walkedStick.Meet(wave, wave.HealthMultiplier);
                        }
                    }
                }
            }

            System.IO.File.WriteAllText(ReportPath, report.ToString());
            Assert.Greater(bosses, 20, "the campaign's level bosses were not found");
            Assert.IsEmpty(failures, string.Join("\n", failures) + "\nSee " + ReportPath);
        }

        /// <summary>
        /// A cell's scalar is a plain factor on creature health, a boss included: the solver gives the same multipliers and
        /// the same boss scales whatever factor the caller stores them with. Until round 86 a boss stood on a floor on the
        /// health as stored, so the early bosses of Psycho did not move with the scalar and the crowd around them jumped
        /// (Glass Meadows Psycho wave 5: x0.41, x1.75 and x0.55 at the scalars 0.72, 0.94 and 0.96). Easy is left out: its
        /// raids keep a floor on the stored health by rule (BalanceRules.RaidHealthFloor).
        /// </summary>
        [Test]
        public void ACellsScalar_MovesNoWaveOfTheSolve_OnMediumHardAndPsycho()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            CampaignModel model = loaded.Model;
            int cells = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
                int index = model.IndexOf(key);
                WaveScriptSpec script = WaveScripts.ByKey(key);
                if (index < 0 || script == null)
                {
                    continue;
                }

                for (int d = 1; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    if (model.Rules.RaidHealthFloorFor(d) > 0f)
                    {
                        continue;
                    }

                    WaveSolver.Solution soft = Solve(model, index, script, d, 0.7f);
                    WaveSolver.Solution tough = Solve(model, index, script, d, 1.4f);
                    for (int i = 0; i < soft.Healths.Length; i++)
                    {
                        Assert.AreEqual(soft.Healths[i], tough.Healths[i], 0.0011f, $"{key} {DifficultyNames[d]} wave {i + 1}: the multiplier of the solve moves with the stored factor");
                        Assert.AreEqual(soft.BossScales[i], tough.BossScales[i], 0.0011f, $"{key} {DifficultyNames[d]} wave {i + 1}: the boss scale of the solve moves with the stored factor");
                    }

                    cells++;
                }
            }

            Assert.Greater(cells, 0);
        }

        private static WaveSolver.Solution Solve(CampaignModel model, int index, WaveScriptSpec script, int d, float stored) =>
            WaveSolver.Solve(script, model.Enemies, model.InputFor(index, d), model.Difficulties[d], model.Facts[index].SecondRoad, model.Rules.HumanTowersOnly(), model.Rules.HumanWithHero(), model.ModelTarget(index, d), stored);

        private static string Line(System.FormattableString text) => System.FormattableString.Invariant(text);

        // ---- Health: the whole bar, a shard's armour, the hit that kills ---------------------------------------

        [Test]
        public void TheBarFraction_CountsBodyAndArmour()
        {
            var health = new Health(800f, 200f);
            Assert.AreEqual(1f, health.BarFraction, 0.0001f);
            // A shell strips armour point for point: 200 armour and 50 body gone is a quarter of the bar.
            health.TakeDamage(250f, 1f);
            Assert.AreEqual(0.75f, health.BarFraction, 0.0001f, "a quarter of the bar is gone");
            Assert.AreEqual(750f / 800f, health.Fraction, 0.0001f, "while the body alone has lost a sixteenth: a phase read on it would wait");
        }

        [Test]
        public void TheHitToKill_IsTheBodyPlusTheArmourAtTheWeaponsPrice()
        {
            var plated = new Health(60f, 160f);
            Assert.AreEqual(220f, plated.HitToKill(1f), 0.001f, "a shell pays armour point for point");
            Assert.AreEqual(60f + 160f / 0.2f, plated.HitToKill(0.2f), 0.001f, "light fire pays five points for one");
            Assert.IsTrue(float.IsPositiveInfinity(plated.HitToKill(0f)), "a weapon that cannot touch armour never kills through it");

            // The hit of that size kills, and one a point smaller does not.
            var a = new Health(60f, 160f);
            a.TakeDamage(a.HitToKill(0.2f), 0.2f);
            Assert.IsTrue(a.IsDead);
            var b = new Health(60f, 160f);
            b.TakeDamage(b.HitToKill(0.2f) - 1f, 0.2f);
            Assert.IsFalse(b.IsDead);

            var soft = new Health(40f);
            soft.TakeDamage(15f);
            Assert.AreEqual(25f, soft.HitToKill(0.2f), 0.001f, "without armour it is what the body has left");
        }

        [Test]
        public void AShard_TakesItsShareOfBodyAndArmour_AndTheGiantFollowsBothPools()
        {
            var giant = new Health(900f, 300f);
            giant.TakeDamage(150f, 1f);
            var shards = new[] { new Health(1f), new Health(1f), new Health(1f) };
            foreach (Health shard in shards)
            {
                shard.Refill(giant.Current / shards.Length, giant.Armor / shards.Length);
            }

            Assert.AreEqual(300f, shards[0].Max, 0.001f);
            Assert.AreEqual(50f, shards[0].ArmorMax, 0.001f, "a third of the armour the giant had left");
            Assert.AreEqual(50f, shards[0].Armor, 0.001f);

            // One shard is shot through its plates and half its body; the giant's pools follow, pool by pool.
            shards[0].TakeDamage(200f, 1f);
            giant.LowerTo(shards[0].Current + shards[1].Current + shards[2].Current, shards[0].Armor + shards[1].Armor + shards[2].Armor);
            Assert.AreEqual(750f, giant.Current, 0.001f);
            Assert.AreEqual(100f, giant.Armor, 0.001f, "the armour of the two untouched shards is still on the giant");

            giant.LowerTo(2000f, 2000f);
            Assert.AreEqual(750f, giant.Current, 0.001f, "never up");
            giant.LowerTo(0f, 0f);
            Assert.IsTrue(giant.IsDead, "with every shard dead the giant is");
        }

        // ---- The beam's budget ---------------------------------------------------------------------------------

        [Test]
        public void ThePulseThatKills_CostsTheShareTheCreatureNeeded_NeverUnderTheSweep()
        {
            const float interval = 0.2f;
            const float sweep = 0.05f;
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, 30f, 8f, sweep), 0.0001f, "a pulse that does not kill costs the interval");
            Assert.AreEqual(interval * 0.5f, BeamBudget.Seconds(interval, 4f, 8f, sweep), 0.0001f, "half a pulse needed, half an interval paid");
            Assert.AreEqual(sweep, BeamBudget.Seconds(interval, 0.8f, 8f, sweep), 0.0001f, "a tenth needed: the sweep to the next creature");
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, 8f, 8f, sweep), 0.0001f, "the whole pulse needed, the whole interval paid");
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, 4f, 8f, 0f), 0.0001f, "a beam that does not sweep pays in full");
            Assert.AreEqual(interval, BeamBudget.Seconds(interval, float.PositiveInfinity, 8f, sweep), 0.0001f, "a warded creature is not killed");
        }

        /// <summary>
        /// A stream of small creatures through the beam, one long frame at a time and many short ones: the same pulses, so
        /// the same seconds. Round 85 settled the kill a frame later and at twenty times speed (the probe's 0.2 s steps)
        /// a second pulse owed in the same frame went into the dying creature at a stale price.
        /// </summary>
        [Test]
        public void TheBeamsSeconds_DoNotDependOnTheFrameLength()
        {
            const float interval = 1f / 9f;
            const float sweep = 0.05f;
            const float damage = 18f;
            float[] bodies = { 22f, 22f, 51f, 12f, 22f, 51f, 12f, 12f, 22f, 51f, 22f, 12f };
            float coarse = SecondsToClear(bodies, damage, interval, sweep, 0.2f);
            float fine = SecondsToClear(bodies, damage, interval, sweep, 1f / 60f);
            Assert.AreEqual(fine, coarse, 0.2f + 0.001f, "within one coarse frame of each other");

            // And what it pays is its data: the health of the stream over its damage per second, plus sweeps for one-pulse bodies.
            float health = 0f;
            foreach (float body in bodies)
            {
                health += body;
            }

            Assert.GreaterOrEqual(fine, health / (damage / interval) - 1f / 60f, "no kill is free");
        }

        // The tower's loop without a scene (Tower.Update): pulses owed in a frame are fired in it, a pulse lands before
        // the next frame, the killing pulse is paid by BeamBudget and the beam lets go of the creature as the pulse leaves.
        private static float SecondsToClear(float[] bodies, float damage, float interval, float sweep, float frame)
        {
            var left = (float[])bodies.Clone();
            float cooldown = 0f;
            float clock = 0f;
            int target = 0;
            const int guard = 100000;
            for (int step = 0; step < guard && target < left.Length; step++)
            {
                clock += frame;
                cooldown -= frame;
                float underWay = 0f;
                for (int shots = 0; target < left.Length && Cadence.Due(cooldown, shots); shots++)
                {
                    float needed = BeamBudget.Needed(left[target], underWay);
                    cooldown = Cadence.Rearm(cooldown, BeamBudget.Seconds(interval, needed, damage, sweep));
                    if (BeamBudget.Kills(needed, damage))
                    {
                        left[target] = 0f;
                        target++;
                        underWay = 0f;
                    }
                    else
                    {
                        underWay += damage;
                    }
                }

                // The pulses under way land before the next frame.
                if (target < left.Length)
                {
                    left[target] -= underWay;
                }

                cooldown = Cadence.Idle(cooldown);
            }

            return clock;
        }
    }
}
