using System.Collections.Generic;
using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Drones;
using Game.Editor;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 64, Remco's second playtest. Armour follows one rule for every weapon of the game (what does not explode or
    /// pierce is reduced; the factor comes from the sheet's table per damage type), the armoured troops are mostly armour
    /// so the rule shows, a level's boss has a health scale of its own and is never as feeble as its crowd, the Core Wisp's
    /// static is under the other loops, and a level loads behind a screen whose bar eases and whose tips are the story's
    /// own hints.
    /// </summary>
    public sealed class Round64Tests
    {
        private const string DataRoot = "Assets/_Project/Data";
        private const int Hard = 2;

        // Coins times seconds a Laser or a Gun Turret spends on a Cragback, over what a Cannon spends: armour must make the
        // Cannon the clearly better buy against it (it was 1.05 and 1.14 before the round).
        private const float MinCannonEdge = 1.8f;

        // The boss Remco shot down "before the first gate with one Cannon and one Gun Turret" had 349 hit points; a level
        // boss on Hard now has at least this many, and the two of Shard Ridge at least this multiple of what they had.
        private const float MinHardBossHealth = 900f;
        private const float ShardRidgeFinalBossBefore = 349f;
        private const float ShardRidgeFirstBossBefore = 565f;
        private const float MinBossGrowth = 1.8f;

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{DataRoot}/{path}.asset");

        // ---- armour: one rule, every weapon ----------------------------------------------------------------------

        [Test]
        public void EveryWeapon_CarriesItsDamageType_AndReadsTheRuleTable()
        {
            // Round 67: no weapon carries a factor of its own; each has its damage type and a reference to the one rule table.
            var rules = AssetDatabase.LoadAssetAtPath<CombatRules>(CombatRules.AssetPath);
            Assert.IsNotNull(rules, "run ApplyRound67");
            foreach (KeyValuePair<string, DamageType> entry in ProjectGenerator.WeaponDamageTypes)
            {
                var so = new SerializedObject(Load<ScriptableObject>(entry.Key));
                Assert.AreEqual((int)entry.Value, so.FindProperty("_damageType").enumValueIndex, entry.Key + ": damage type (run ApplyRound67)");
                Assert.AreSame(rules, so.FindProperty("_rules").objectReferenceValue, entry.Key + ": not wired to the rule table");
                Assert.IsNull(so.FindProperty("_armourFactor"), entry.Key + ": still carries a factor of its own");
            }
        }

        [Test]
        public void NoWeaponIsLeftOutOfTheRule()
        {
            // Every asset that can hurt a creature is in the table, so none keeps a hand-typed factor (the attack drone had none at all).
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { DataRoot + "/Towers" }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (!tower.IsSupport)
                {
                    Assert.IsTrue(ProjectGenerator.WeaponDamageTypes.ContainsKey("Towers/" + tower.name), tower.name + " has no damage type in the table");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:PathObjectDefinition", new[] { DataRoot + "/PathObjects" }))
            {
                var gadget = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (gadget.Damage > 0f)
                {
                    Assert.IsTrue(ProjectGenerator.WeaponDamageTypes.ContainsKey("PathObjects/" + gadget.name), gadget.name + " has no damage type in the table");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:DroneDefinition", new[] { DataRoot + "/Drones" }))
            {
                var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (drone.Kind == DroneKind.Attack)
                {
                    Assert.IsTrue(ProjectGenerator.WeaponDamageTypes.ContainsKey("Drones/" + drone.name), drone.name + " has no damage type in the table");
                }
            }
        }

        [Test]
        public void OnlyWhatExplodesOrPierces_WorksOnArmourInFull()
        {
            foreach (DamageType type in System.Enum.GetValues(typeof(DamageType)))
            {
                float factor = AssetDatabase.LoadAssetAtPath<CombatRules>(CombatRules.AssetPath).RuleOf(type).Armour;
                bool reduced = type != DamageType.Explosive && type != DamageType.Piercing;
                Assert.AreEqual(reduced, factor < 1f, $"{type}: factor {factor}");
            }

            Assert.AreEqual(DamageType.Explosive, Load<TowerDefinition>("Towers/Cannon").DamageType);
            Assert.AreEqual(DamageType.Beam, Load<TowerDefinition>("Towers/Laser").DamageType);
            Assert.AreEqual(DamageType.Bullet, Load<DroneDefinition>("Drones/AttackDrone").DamageType);
            Assert.AreEqual(DamageType.Blade, Load<HeroWeaponDefinition>("Hero/Weapon_EnergySword").DamageType);
        }

        [Test]
        public void TheAttackDronesShots_AreTurnedByArmour()
        {
            var drone = Load<DroneDefinition>("Drones/AttackDrone");
            var crag = Load<EnemyDefinition>("Enemies/Cragback");
            Assert.Less(drone.Profile.Armour, 1f, "the drone's shots ignored armour until round 64");
            // One shot on a fresh Cragback: the body is not touched, the armour loses a fifth of the shot.
            var health = new Health(crag.MaxHealth, crag.Armor);
            health.TakeDamage(drone.Damage, drone.Profile.Armour);
            Assert.AreEqual(crag.MaxHealth, health.Current, 0.0001f);
            Assert.AreEqual(crag.Armor - drone.Damage * drone.Profile.Armour, health.Armor, 0.0001f);
            // And the plan model counts it: an attack drone on paper lands the share the wave's armour lets it.
            DroneStats stats = BalanceLedger.Drones(Load<LevelDefinition>("Levels/Level_C1_04")).Find(d => d.Role == DroneRole.Attack);
            Assert.IsNotNull(stats);
            Assert.AreEqual(drone.Profile.Armour, stats.ArmourFactor, 0.0001f);
        }

        [Test]
        public void ArmourMakesTheCannonTheBetterBuy_AndLightFireStillKills()
        {
            var crag = Load<EnemyDefinition>("Enemies/Cragback");
            Assert.AreEqual(ProjectGenerator.CragbackToughness, crag.MaxHealth + crag.Armor, 0.01f, "the toughness (what a shell must deal) is unchanged");
            Assert.AreEqual(BalanceSheet.ArmourOf(ProjectGenerator.CragbackToughness), crag.Armor, 0.01f);
            Assert.GreaterOrEqual(crag.Armor / (crag.MaxHealth + crag.Armor), 0.65f, "most of an armoured troop is armour");

            float cannon = CoinSeconds(Load<TowerDefinition>("Towers/Cannon"), crag);
            float laser = CoinSeconds(Load<TowerDefinition>("Towers/Laser"), crag);
            float gun = CoinSeconds(Load<TowerDefinition>("Towers/PulseRifle"), crag);
            Assert.GreaterOrEqual(laser / cannon, MinCannonEdge, $"Laser {laser:0} coin-seconds against the Cannon's {cannon:0}");
            Assert.GreaterOrEqual(gun / cannon, MinCannonEdge, $"Gun Turret {gun:0} coin-seconds against the Cannon's {cannon:0}");
            // Light fire still contributes: it kills a Cragback alone, and once shells have stripped the armour it lands in full.
            Assert.Less(Seconds(Load<TowerDefinition>("Towers/Laser"), crag.MaxHealth, crag.Armor), 30f);
            Assert.AreEqual(Seconds(Load<TowerDefinition>("Towers/Laser"), crag.MaxHealth, 0f), Seconds(Load<TowerDefinition>("Towers/Laser"), crag.MaxHealth, 0.0001f), 0.21f);
        }

        // Seconds a level-one tower needs for the creature, through the game's own Health, times its build price.
        private static float CoinSeconds(TowerDefinition tower, EnemyDefinition enemy) => Seconds(tower, enemy.MaxHealth, enemy.Armor) * tower.GetLevel(0).Cost;

        private static float Seconds(TowerDefinition tower, float body, float armour)
        {
            var health = new Health(body, armour);
            TowerLevel level = tower.GetLevel(0);
            int hits = 0;
            while (!health.IsDead && hits < 10000)
            {
                health.TakeDamage(level.Damage, tower.Profile.Armour);
                hits++;
            }

            return hits * level.FireInterval;
        }

        // ---- a level's boss ----------------------------------------------------------------------------------------

        [Test]
        public void ALevelBoss_HasAScaleOfItsOwn_InTheGameAndOnPaper()
        {
            // The spawner gives a boss the wave's multiplier times the wave's boss scale; the model reads the same two numbers.
            var boss = new EnemyStats { Id = "Boss", Layer = Layer.Ground, Health = 900f, Speed = 0.55f, IsBoss = true };
            var walker = new EnemyStats { Id = "Walker", Layer = Layer.Ground, Health = 20f, Speed = 1f };
            var wave = new WaveSpec { HealthMultiplier = 0.3f, BossHealthScale = 4f };
            wave.Groups.Add(new GroupSpec { Enemy = walker, Count = 10, Interval = 1f });
            wave.Groups.Add(new GroupSpec { Enemy = boss, Count = 1, Interval = 1f });
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            LevelInput input = loaded.Model.InputFor(0, 1);
            WaveLoad load = WaveLoad.Of(input, wave, loaded.Model.Difficulties[1], loaded.Model.Rules);
            Assert.AreEqual(900f * 0.3f * 4f * input.HealthScale, load.BossHealth, 0.5f, "the boss carries its own scale");
            WaveSpec stored = BalanceLedger.Spec(new WaveDefinition(new SpawnGroup[0], healthMultiplier: 0.3f, bossHealthScale: 4f), new Dictionary<string, EnemyStats>());
            Assert.AreEqual(4f, stored.BossHealthScale, 0.0001f, "a stored wave hands its boss scale to the model");
        }

        [Test]
        public void EveryLevelBoss_KeepsItsFloor_AndTheStoredScalesAreTheSolves()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            BalanceRules rules = loaded.Model.Rules;
            int bosses = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
                int index = loaded.Model.IndexOf(key);
                WaveScriptSpec script = WaveScripts.ByKey(key);
                if (index < 0 || script == null)
                {
                    continue;
                }

                List<List<WaveSpec>> sets = loaded.Model.BuildWaves(index, script, out _);
                for (int d = 0; d < sets.Count; d++)
                {
                    WaveDefinition[] stored = node.Level.GetWaves(loaded.Catalog.Difficulties[d]);
                    for (int i = 0; i < stored.Length; i++)
                    {
                        Assert.AreEqual(sets[d][i].BossHealthScale, stored[i].BossHealthScale, 0.0015f, $"{key} difficulty {d} wave {i + 1}: the stored boss scale is not what the solve gives (run the solve stage)");
                        foreach (SpawnGroup group in stored[i].Groups)
                        {
                            if (group.Enemy == null || !group.Enemy.IsBoss || group.Enemy.Boss != null)
                            {
                                continue;
                            }

                            bosses++;
                            float multiple = stored[i].HealthMultiplier * stored[i].BossHealthScale;
                            Assert.GreaterOrEqual(multiple, rules.LevelBossHealthFloorFor(d) - 0.002f, $"{key} difficulty {d} wave {i + 1}: {group.Enemy.name} is scaled to {multiple:0.000} of its book health");
                            if (d == Hard)
                            {
                                float health = group.Enemy.MaxHealth * multiple * loaded.Catalog.Difficulties[d].HealthMultiplier * node.Level.EnemyHealthScale;
                                Assert.GreaterOrEqual(health, MinHardBossHealth * group.Enemy.MaxHealth / 900f, $"{key} Hard wave {i + 1}: {group.Enemy.name} spawns with {health:0} hit points");
                            }
                        }
                    }
                }
            }

            Assert.Greater(bosses, 20, "the campaign's level bosses were not found");
        }

        [Test]
        public void ShardRidgesBossesOnHard_AreNoLongerShotDownByTwoGuns()
        {
            var level = Load<LevelDefinition>("Levels/Level_C1_03");
            var hard = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DataRoot}/Difficulty/Hard.asset");
            WaveDefinition[] waves = level.GetWaves(hard);
            var found = new List<float>();
            foreach (WaveDefinition wave in waves)
            {
                foreach (SpawnGroup group in wave.Groups)
                {
                    if (group.Enemy != null && group.Enemy.IsBoss)
                    {
                        found.Add(group.Enemy.MaxHealth * wave.HealthMultiplier * wave.BossHealthScale * hard.HealthMultiplier * level.EnemyHealthScale);
                    }
                }
            }

            Assert.AreEqual(2, found.Count, "Shard Ridge on Hard has two bosses");
            Assert.GreaterOrEqual(found[0], ShardRidgeFirstBossBefore * MinBossGrowth, $"the first boss spawns with {found[0]:0} hit points");
            Assert.GreaterOrEqual(found[1], ShardRidgeFinalBossBefore * MinBossGrowth, $"the last boss spawns with {found[1]:0} hit points");
        }

        // ---- the Core Wisp's static --------------------------------------------------------------------------------

        [Test]
        public void TheCoreWispsStatic_SitsUnderTheOtherBossLoops()
        {
            var core = AssetDatabase.LoadAssetAtPath<CreatureVoice>($"{DataRoot}/Audio/Voices/Voice_CoreWisp.asset");
            var brood = AssetDatabase.LoadAssetAtPath<CreatureVoice>($"{DataRoot}/Audio/Voices/Voice_BroodMother.asset");
            var wisp = AssetDatabase.LoadAssetAtPath<CreatureVoice>($"{DataRoot}/Audio/Voices/Voice_Wisp.asset");
            Assert.AreEqual(ProjectGenerator.CoreWispPresence, core.Presence, 0.0001f, "run ApplyRound64");
            Assert.AreEqual(ProjectGenerator.WispPresence, wisp.Presence, 0.0001f);
            // A loop's level is its presence times its measured gain (the chorus's cap is the same for every boss).
            Assert.Less(core.Presence * core.MoveLoop.Gain, 0.5f * brood.Presence * brood.MoveLoop.Gain, "the static loop is at most half the Brood Mother's loop");
        }

        // ---- the loading screen ------------------------------------------------------------------------------------

        [Test]
        public void TheLoadingBar_Eases_NeverRunsBack_AndOnlyFillsWhenTheLoadIsDone()
        {
            var bar = new LoadingProgress(5f, 0.02f, 0.97f, 1.5f);
            bar.Report(0.5f);
            float last = 0f;
            for (int i = 0; i < 30; i++)
            {
                bar.Tick(1f / 60f);
                Assert.GreaterOrEqual(bar.Shown, last, "the bar never runs backwards");
                Assert.LessOrEqual(bar.Shown - last, 0.05f, "no jump: a report of half the load is eased in over frames");
                last = bar.Shown;
            }

            Assert.Greater(bar.Shown, 0.3f, "half a second later most of the way there");
            Assert.Less(bar.Shown, 0.5f);
            bar.Report(0.2f);
            bar.Tick(1f / 60f);
            Assert.GreaterOrEqual(bar.Shown, last, "a lower report is ignored");

            bar.Report(1f);
            for (int i = 0; i < 600; i++)
            {
                bar.Tick(1f / 60f);
            }

            Assert.LessOrEqual(bar.Shown, 0.97f + 0.0001f, "not full before the load says it is done");
            Assert.IsFalse(bar.IsFull);
            bar.Complete();
            for (int i = 0; i < 6; i++)
            {
                bar.Tick(1f / 60f);
            }

            Assert.IsTrue(bar.IsFull, "once the load is done the bar fills at once, the player is not kept waiting on an ease");
        }

        [Test]
        public void TheLoadingTips_AreTheStorysOwnHints_AndThoseOfTheLevelComeFirst()
        {
            var catalog = Load<CampaignCatalog>("Levels/CampaignCatalog");
            var level = Load<LevelDefinition>("Levels/Level_C1_03");
            var easy = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DataRoot}/Difficulty/Easy.asset");
            List<LoadingTip> tips = LoadingTips.For(catalog.Story, level, easy);
            int about = LoadingTips.AboutLevel(catalog.Story, level, easy);
            Assert.Greater(tips.Count, 3, "the story has scan hints and gate tips");
            Assert.Greater(about, 0, "Shard Ridge sends creatures the story has a hint for");
            // Round 65: a tip is the hint's own loading tip (written to stand alone), no longer its closing line.
            var words = new HashSet<string>();
            foreach (ScanHint hint in catalog.Story.ScanHints)
            {
                words.Add(hint.Dialogue.LoadingTip.Trim());
            }

            for (int i = 0; i < tips.Count; i++)
            {
                Assert.IsNotEmpty(tips[i].Speaker);
                Assert.LessOrEqual(tips[i].Text.Length, LoadingTips.MaxLength, "a tip fits its box");
                if (i < about)
                {
                    Assert.IsTrue(words.Contains(tips[i].Text), "a level tip is a scan hint's loading tip");
                }
            }

            Assert.IsEmpty(LoadingTips.For(null, level, easy), "no story, no tips, and the screen shows none");
        }

        [Test]
        public void TheLoadingArt_HasNoHardEdge()
        {
            Texture2D planet = LoadingArt.Planet(64, Color.gray, Color.cyan, Color.white, new Vector3(-1f, 1f, 1f));
            Texture2D ring = LoadingArt.Ring(64, 1.5f, 0.3f, 0.2f);
            Texture2D glow = LoadingArt.Glow(32);
            try
            {
                foreach (Texture2D texture in new[] { planet, ring, glow })
                {
                    Color[] pixels = texture.GetPixels();
                    int size = texture.width;
                    for (int i = 0; i < size; i++)
                    {
                        // The outermost pixels are clear on every side: scaled up, the picture fades out instead of being cut.
                        Assert.Less(pixels[i].a, 0.02f, "bottom row");
                        Assert.Less(pixels[(size - 1) * size + i].a, 0.02f, "top row");
                        Assert.Less(pixels[i * size].a, 0.02f, "left column");
                        Assert.Less(pixels[i * size + size - 1].a, 0.02f, "right column");
                    }
                }

                Assert.Greater(planet.GetPixel(32, 32).a, 0.99f, "the planet is solid in the middle");
            }
            finally
            {
                Object.DestroyImmediate(planet);
                Object.DestroyImmediate(ring);
                Object.DestroyImmediate(glow);
            }
        }

        [Test]
        public void TheLevelsSceneryBuild_ReportsProgressThatOnlyRises()
        {
            var level = Load<LevelDefinition>("Levels/Level_C1_01");
            var parent = new GameObject("Round64Scenery");
            try
            {
                var routes = new Dictionary<int, GameObject>();
                IEnumerator<float> steps = LevelVisualBuilder.BuildInSteps(level, parent.transform, routes, 0f, false, true);
                float last = 0f;
                int count = 0;
                while (steps.MoveNext())
                {
                    Assert.GreaterOrEqual(steps.Current, last);
                    last = steps.Current;
                    count++;
                }

                Assert.AreEqual(1f, last, 0.0001f, "the last step reports the build done");
                Assert.Greater(count, 4, "the build is cut into steps a loading screen can draw between");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void TheMusicStreams()
        {
            foreach (string loop in new[] { "menu_loop", "game_calm_loop", "game_boss_loop" })
            {
                var importer = AssetImporter.GetAtPath($"Assets/_Project/Audio/Approved/Music/{loop}.wav") as AudioImporter;
                Assert.IsNotNull(importer, loop);
                Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType, loop + ": decoding it whole cost the level's first frame 1.6 s");
            }
        }
    }
}
