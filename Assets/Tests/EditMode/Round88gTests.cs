using Game.Combat;
using Game.Core;
using Game.Drones;
using Game.Editor;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88g, Remco's playtest of 2026-11-23: the repair crew goes first to what the player marked
    /// (<see cref="IRepairFocus"/>, <see cref="Tower.RepairFocus"/>, <see cref="RepairDispatcher"/>), a gun's health grows
    /// with its stars (<see cref="BalanceSheet.GunHealthPerStar"/>), a drone's state and battery for the HUD
    /// (<see cref="DroneStates"/>), and Psycho's small nudge (a gun's share of a blow).
    /// </summary>
    public sealed class Round88gTests
    {
        private const string TowersFolder = "Assets/_Project/Data/Towers";
        private const string CannonPath = "Assets/_Project/Data/Towers/Cannon.asset";
        private const string GunTurretPath = "Assets/_Project/Data/Towers/PulseRifle.asset";
        private const string BlockadePath = "Assets/_Project/Data/Towers/Blockade.asset";
        private const string BaseTurretPath = "Assets/_Project/Data/Towers/BaseTurret.asset";
        private const string PsychoPath = "Assets/_Project/Data/Difficulty/Psycho.asset";
        private const float Speed = 4f;

        private sealed class Fake : IStructure, IRepairFocus, ICrewRaisable
        {
            private const float Max = 100f;
            private float _current;

            public Fake(Vector3 position, float fraction, bool blocks = false, bool isBase = false, bool raisable = false)
            {
                Position = position;
                _current = fraction * Max;
                BlocksGround = blocks;
                IsBase = isBase;
                Raisable = raisable;
            }

            public Vector3 Position { get; }
            public bool IsAlive => _current > 0f;
            public bool BlocksGround { get; }
            public bool IsBase { get; }
            public bool Raisable { get; }
            public bool RepairFocus { get; set; }
            public bool AwaitsCrew => Raisable && !IsAlive;
            public float HealthFraction => _current / Max;
            public void TakeDamage(float amount) => _current = Mathf.Max(0f, _current - amount);
            public void Repair(float amount) => _current = Mathf.Min(Max, _current + amount);
        }

        private readonly object _droneA = new object();
        private readonly object _droneB = new object();
        private readonly System.Collections.Generic.List<GameObject> _made = new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made)
            {
                Object.DestroyImmediate(go);
            }

            _made.Clear();
        }

        private static RepairDispatcher Crew(out StructureRegistry registry, params Fake[] structures)
        {
            registry = new StructureRegistry();
            foreach (Fake structure in structures)
            {
                registry.Add(structure);
            }

            return new RepairDispatcher(registry);
        }

        // ---- the crew's order with a mark ----------------------------------------------------------------------------

        [Test]
        public void AMarkedGun_GoesBeforeAWorseGunAndACriticalGate()
        {
            var marked = new Fake(new Vector3(30f, 0f, 0f), 0.9f) { RepairFocus = true };
            var worse = new Fake(new Vector3(1f, 0f, 0f), 0.2f);
            var gate = new Fake(new Vector3(2f, 0f, 0f), 0.1f, blocks: true);
            RepairDispatcher crew = Crew(out _, worse, gate, marked);

            Assert.AreSame(marked, crew.Assign(_droneA, Vector3.zero, Speed, null), "far away and barely scratched, but the player said: this one first");
        }

        [Test]
        public void AWholeMarkedGun_ChangesNothing()
        {
            var marked = new Fake(new Vector3(1f, 0f, 0f), 1f) { RepairFocus = true };
            var hurt = new Fake(new Vector3(5f, 0f, 0f), 0.5f);
            RepairDispatcher crew = Crew(out _, marked, hurt);

            Assert.AreSame(hurt, crew.Assign(_droneA, Vector3.zero, Speed, null), "a mark on a whole gun holds no drone");
            Assert.IsFalse(crew.IsClaimed(marked));
        }

        [Test]
        public void ADroneOnOrdinaryWork_LeavesItTheMomentAMarkedGunIsHurt_AndComesBackAfter()
        {
            var marked = new Fake(new Vector3(10f, 0f, 0f), 1f) { RepairFocus = true };
            var job = new Fake(Vector3.zero, 0.2f);
            RepairDispatcher crew = Crew(out _, marked, job);
            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(job, current);

            marked.TakeDamage(5f);
            current = crew.Assign(_droneA, Vector3.zero, Speed, current);
            Assert.AreSame(marked, current, "a critical job under way still gives way to the mark");
            Assert.IsFalse(crew.IsClaimed(job), "and is free for another drone");

            marked.Repair(100f);
            Assert.AreSame(job, crew.Assign(_droneA, Vector3.zero, Speed, current), "the mark is whole: back to the ordinary order");
        }

        [Test]
        public void SeveralMarkedGuns_AreTakenByNeedOverFlightTime_LikeAnyOtherWork()
        {
            var light = new Fake(new Vector3(2f, 0f, 0f), 0.9f) { RepairFocus = true };
            var heavy = new Fake(new Vector3(3f, 0f, 0f), 0.4f) { RepairFocus = true };
            var farHeavy = new Fake(new Vector3(60f, 0f, 0f), 0.35f) { RepairFocus = true };
            RepairDispatcher crew = Crew(out _, light, heavy, farHeavy);

            IStructure first = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(heavy, first, "the badly hurt one nearby before the scratch and before the one across the map");

            // It finishes what it started, as on ordinary work, while the others wait their turn.
            heavy.Repair(10f);
            Assert.AreSame(heavy, crew.Assign(_droneA, Vector3.zero, Speed, first));
            heavy.Repair(100f);
            IStructure second = crew.Assign(_droneA, heavy.Position, Speed, first);
            Assert.IsTrue(ReferenceEquals(second, light) || ReferenceEquals(second, farHeavy), "then another marked one");
            Assert.AreNotSame(heavy, second);
        }

        [Test]
        public void TwoDrones_OneTakesTheMarkedGun_TheOtherStaysOnOrdinaryDuty()
        {
            var marked = new Fake(new Vector3(5f, 0f, 0f), 0.5f) { RepairFocus = true };
            var ordinary = new Fake(new Vector3(8f, 0f, 0f), 0.6f);
            RepairDispatcher crew = Crew(out _, marked, ordinary);

            IStructure first = crew.Assign(_droneA, Vector3.zero, Speed, null);
            IStructure second = crew.Assign(_droneB, Vector3.zero, Speed, null);
            Assert.AreSame(marked, first);
            Assert.AreSame(ordinary, second, "one claim per structure: the second drone does not wait beside the first, it takes the next job");

            // Asked again, both keep what they have.
            Assert.AreSame(marked, crew.Assign(_droneA, Vector3.zero, Speed, first));
            Assert.AreSame(ordinary, crew.Assign(_droneB, Vector3.zero, Speed, second));
        }

        [Test]
        public void TwoDrones_TwoMarkedGuns_EachTakesOne()
        {
            var one = new Fake(new Vector3(5f, 0f, 0f), 0.5f) { RepairFocus = true };
            var two = new Fake(new Vector3(9f, 0f, 0f), 0.7f) { RepairFocus = true };
            var ordinary = new Fake(new Vector3(1f, 0f, 0f), 0.1f);
            RepairDispatcher crew = Crew(out _, one, two, ordinary);

            IStructure first = crew.Assign(_droneA, Vector3.zero, Speed, null);
            IStructure second = crew.Assign(_droneB, Vector3.zero, Speed, null);
            Assert.AreSame(one, first);
            Assert.AreSame(two, second, "while a marked gun is free, no drone takes ordinary work, however critical");
        }

        [Test]
        public void TheSecondDrone_LeavesOrdinaryWork_WhenTheMarkedGunsDroneIsLost()
        {
            var marked = new Fake(new Vector3(5f, 0f, 0f), 0.5f) { RepairFocus = true };
            var ordinary = new Fake(new Vector3(8f, 0f, 0f), 0.6f);
            RepairDispatcher crew = Crew(out _, marked, ordinary);
            IStructure first = crew.Assign(_droneA, Vector3.zero, Speed, null);
            IStructure second = crew.Assign(_droneB, Vector3.zero, Speed, null);

            // Drone A is shot down on the job (its battery is empty): it gives the claim back and goes home to charge.
            crew.Release(_droneA, first);
            second = crew.Assign(_droneB, Vector3.zero, Speed, second);
            Assert.AreSame(marked, second, "the mark is free again, so the drone still flying takes it over");

            // Charged, drone A comes back and finds the mark held: it takes the ordinary job.
            Assert.AreSame(ordinary, crew.Assign(_droneA, Vector3.zero, Speed, null));
        }

        [Test]
        public void TheMarkTakenOffMidRepair_TheDroneFinishesUnlessAnotherMarkWaits()
        {
            var first = new Fake(new Vector3(5f, 0f, 0f), 0.5f) { RepairFocus = true };
            var other = new Fake(new Vector3(6f, 0f, 0f), 0.9f);
            RepairDispatcher crew = Crew(out _, first, other);
            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(first, current);

            first.RepairFocus = false;
            Assert.AreSame(first, crew.Assign(_droneA, first.Position, Speed, current), "now ordinary work, and ordinary work is finished before switching");

            var marked = new Fake(new Vector3(20f, 0f, 0f), 0.95f) { RepairFocus = true };
            RepairDispatcher again = Crew(out _, first, other, marked);
            current = again.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(marked, current);
            marked.RepairFocus = false;
            first.RepairFocus = true;
            Assert.AreSame(first, again.Assign(_droneA, marked.Position, Speed, current), "the mark moved to another gun: the drone follows it");
        }

        [Test]
        public void TheMarkPutOnMidRepairOfAnotherGun_PullsTheDroneOver()
        {
            var job = new Fake(new Vector3(1f, 0f, 0f), 0.25f);
            var later = new Fake(new Vector3(12f, 0f, 0f), 0.8f);
            RepairDispatcher crew = Crew(out _, job, later);
            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(job, current);

            later.RepairFocus = true;
            Assert.AreSame(later, crew.Assign(_droneA, job.Position, Speed, current));
        }

        [Test]
        public void AMarkedGunThatIsSold_IsNoJob_AndTheDroneMovesOn()
        {
            var marked = new Fake(new Vector3(5f, 0f, 0f), 0.5f) { RepairFocus = true };
            var ordinary = new Fake(new Vector3(8f, 0f, 0f), 0.6f);
            RepairDispatcher crew = Crew(out StructureRegistry registry, marked, ordinary);
            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(marked, current);

            registry.Remove(marked);
            Assert.AreSame(ordinary, crew.Assign(_droneA, Vector3.zero, Speed, current), "sold under repair: it left the field, mark and all");
            Assert.IsFalse(crew.IsClaimed(marked));
        }

        [Test]
        public void AMarkedBaseTurretWreck_IsRaisedBeforeADamagedGun_AnUnmarkedOneStillWaits()
        {
            var wreck = new Fake(new Vector3(5f, 0f, 0f), 0f, raisable: true);
            var gun = new Fake(new Vector3(1f, 0f, 0f), 0.5f);
            RepairDispatcher crew = Crew(out _, wreck, gun);
            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(gun, current, "round 88e's order without a mark: the standing gun first");

            wreck.RepairFocus = true;
            Assert.AreSame(wreck, crew.Assign(_droneA, Vector3.zero, Speed, current), "marked, the wreck is the player's first wish");
        }

        [Test]
        public void TheSecondDrone_TakesTheNextRank_InsteadOfIdling_WhileTheFirstHoldsTheOnlyGun()
        {
            // Round 88e's known gap: the second drone waited beside the base while the first mended the only damaged gun.
            var gun = new Fake(new Vector3(5f, 0f, 0f), 0.5f);
            var home = new Fake(Vector3.zero, 0.5f, blocks: true, isBase: true);
            var wreck = new Fake(new Vector3(2f, 0f, 0f), 0f, raisable: true);
            RepairDispatcher crew = Crew(out StructureRegistry registry, gun, home, wreck);
            var droneC = new object();

            Assert.AreSame(gun, crew.Assign(_droneA, Vector3.zero, Speed, null));
            IStructure second = crew.Assign(_droneB, Vector3.zero, Speed, null);
            Assert.AreSame(home, second, "the base is next in the order");
            Assert.AreSame(wreck, crew.Assign(droneC, Vector3.zero, Speed, null), "and the wreck after it");

            // A second gun is hurt: the drone on the base goes to it, the base waits again.
            var newcomer = new Fake(new Vector3(6f, 0f, 0f), 0.7f);
            registry.Add(newcomer);
            Assert.AreSame(newcomer, crew.Assign(_droneB, Vector3.zero, Speed, second), "a free gun still goes before the base");
            Assert.IsFalse(crew.IsClaimed(home), "which waits again");
        }

        // ---- the mark on a real tower --------------------------------------------------------------------------------

        private Tower Build(string definitionPath, StructureRegistry registry = null)
        {
            var definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>(definitionPath);
            Assert.IsNotNull(definition, definitionPath);
            var go = new GameObject("Tower");
            _made.Add(go);
            Tower tower = go.AddComponent<Tower>();
            tower.enabled = false;
            var targets = new TargetRegistry();
            tower.Initialize(definition, targets, new ProjectilePool(targets, go.transform), PlayerModifiers.None);
            if (registry != null)
            {
                tower.Register(registry);
            }

            return tower;
        }

        [Test]
        public void TheMark_StaysThroughDamageRepairAndUpgrade_AndTellsItsListeners()
        {
            Tower gun = Build(CannonPath);
            int raised = 0;
            gun.RepairFocusChanged += _ => raised++;
            Assert.IsFalse(gun.RepairFocus, "off until the player asks");

            gun.SetRepairFocus(true);
            gun.SetRepairFocus(true);
            Assert.IsTrue(gun.RepairFocus);
            Assert.AreEqual(1, raised, "told once, not on every press that changes nothing");

            gun.TakeDamage(gun.Health.Max * 0.5f);
            gun.RepairFully();
            gun.Upgrade();
            Assert.IsTrue(gun.RepairFocus, "the same gun with one star more is still the player's first wish");

            gun.SetRepairFocus(false);
            Assert.IsFalse(gun.RepairFocus);
            Assert.AreEqual(2, raised);
        }

        [Test]
        public void AMarkedGunThatIsWrecked_LosesItsMark_AndItsWreckTakesNone()
        {
            var registry = new StructureRegistry();
            Tower gun = Build(CannonPath, registry);
            Tower other = Build(GunTurretPath, registry);
            other.transform.position = new Vector3(3f, 0f, 0f);
            other.TakeDamage(other.Health.Max * 0.3f);
            var crew = new RepairDispatcher(registry);
            int raised = 0;
            gun.RepairFocusChanged += _ => raised++;
            gun.SetRepairFocus(true);
            gun.TakeDamage(gun.Health.Max * 0.5f);
            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(gun, current);

            gun.TakeDamage(gun.Health.Max * 2f);
            Assert.IsTrue(gun.IsWrecked);
            Assert.IsFalse(gun.RepairFocus, "a gun's wreck is scrapped; what is built on the cell is another gun");
            Assert.AreEqual(2, raised, "the HUD hears the mark go");
            Assert.IsFalse(gun.CanFocusRepair);
            gun.SetRepairFocus(true);
            Assert.IsFalse(gun.RepairFocus, "and the wreck cannot be marked");
            Assert.AreSame(other, crew.Assign(_droneA, Vector3.zero, Speed, current), "the drone goes on with the ordinary work");
        }

        [Test]
        public void AMarkedGate_KeepsItsMarkThroughAWreckAndItsRebuild()
        {
            var registry = new StructureRegistry();
            Tower gate = Build(BlockadePath, registry);
            Tower gun = Build(CannonPath, registry);
            gun.transform.position = new Vector3(2f, 0f, 0f);
            gun.TakeDamage(gun.Health.Max * 0.2f);
            var crew = new RepairDispatcher(registry);
            gate.SetRepairFocus(true);

            gate.TakeDamage(gate.Health.Max * 2f);
            Assert.IsTrue(gate.IsWrecked);
            Assert.IsTrue(gate.RepairFocus, "the gate comes back on its own cell: the mark waits for it");
            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(gun, current, "a wrecked gate is the player's to rebuild: the mark holds no drone on it");

            gate.RepairFully();
            Assert.IsNull(FirstMarkedJob(crew, current), "rebuilt whole: still nothing to do there");
            gate.TakeDamage(10f);
            Assert.AreSame(gate, crew.Assign(_droneA, Vector3.zero, Speed, current), "hurt again, the marked gate goes first again");
        }

        private IStructure FirstMarkedJob(RepairDispatcher crew, IStructure current)
        {
            IStructure job = crew.Assign(_droneA, Vector3.zero, Speed, current);
            return job is IRepairFocus marked && marked.RepairFocus ? job : null;
        }

        [Test]
        public void AMarkedBaseTurret_IsRaisedFirst_AndKeepsItsMark()
        {
            var registry = new StructureRegistry();
            Tower turret = Build(BaseTurretPath, registry);
            Tower gun = Build(CannonPath, registry);
            gun.transform.position = new Vector3(2f, 0f, 0f);
            gun.TakeDamage(gun.Health.Max * 0.5f);
            var crew = new RepairDispatcher(registry);
            turret.SetRepairFocus(true);
            turret.TakeDamage(turret.Health.Max * 2f);
            Assert.IsTrue(turret.AwaitsCrew);
            Assert.IsTrue(turret.RepairFocus, "the turret is part of the base: its mark stays on the wreck");
            Assert.IsTrue(turret.CanFocusRepair);

            IStructure current = crew.Assign(_droneA, Vector3.zero, Speed, null);
            Assert.AreSame(turret, current, "marked, the wreck is raised before the damaged gun");
            turret.Repair(turret.Health.Max + 1f);
            Assert.IsFalse(turret.IsWrecked);
            Assert.IsTrue(turret.RepairFocus);
            Assert.AreSame(gun, crew.Assign(_droneA, Vector3.zero, Speed, current));
        }

        // ---- health with the stars -----------------------------------------------------------------------------------

        [Test]
        public void EveryGun_GainsTheSameShareOfItsHealthWithEveryStar_GatesAndTheBaseTurretKeepTheirOwn()
        {
            int guns = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { TowersFolder }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (tower.BlocksGroundEnemies || tower.IsBaseDefence)
                {
                    continue;
                }

                guns++;
                float star = Mathf.Round(tower.MaxHealth * BalanceSheet.GunHealthPerStar);
                Assert.Greater(star, 0f, tower.name);
                Assert.AreEqual(0f, tower.GetLevel(0).HealthBonus, tower.name + ": built at its own health");
                for (int level = 1; level <= tower.MaxLevelIndex; level++)
                {
                    Assert.AreEqual(star, tower.GetLevel(level).HealthBonus, 0.001f, $"{tower.name} level {level + 1}");
                }

                Assert.IsNull(tower.StepProblem(), tower.name + ": the health of a star is no step of its own");
            }

            Assert.GreaterOrEqual(guns, 8, "the eight guns and the hybrids");
            Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<TowerDefinition>(BaseTurretPath).GetLevel(1).HealthBonus, "the base's turret is refitted between levels, not in one");
        }

        [Test]
        public void AnUpgradedGun_HasTheLargerBar_ComesOutOfTheRefitWhole_AndItsRepairFollowsTheBar()
        {
            Tower gun = Build(CannonPath);
            TowerDefinition cannon = gun.Definition;
            float built = cannon.MaxHealth;
            float star = Mathf.Round(built * BalanceSheet.GunHealthPerStar);
            Assert.AreEqual(built, gun.Health.Max, 0.001f);

            gun.TakeDamage(built * 0.5f);
            gun.Upgrade();
            Assert.AreEqual(built + star, gun.Health.Max, 0.001f, "two stars");
            Assert.AreEqual(gun.Health.Max, gun.Health.Current, 0.001f, "a refit patches every dent");
            gun.Upgrade();
            Assert.AreEqual(built + star * 2f, gun.Health.Max, 0.001f, "three stars");
            Assert.AreEqual(1f + 2f * BalanceSheet.GunHealthPerStar, gun.Health.Max / built, 0.01f);

            // The same blow leaves an upgraded gun with a larger share of its bar, and costs the same coins to mend.
            float blow = built * 0.5f;
            gun.TakeDamage(blow);
            Assert.Greater(gun.HealthFraction, 0.5f);
            Assert.AreEqual(Mathf.CeilToInt(blow * cannon.RepairCostPerHealth), gun.RepairCost, "repair is priced per health missing");

            // From the last hit point the whole larger bar is paid for, and the crew works it off at its rate.
            gun.TakeDamage(gun.Health.Current - 1f);
            Assert.AreEqual(Mathf.CeilToInt((gun.Health.Max - 1f) * cannon.RepairCostPerHealth), gun.RepairCost);
            gun.Repair(built);
            Assert.Less(gun.Health.Current, gun.Health.Max, "the work that mended a new gun does not fill a three-star bar");
            gun.RepairFully();
            Assert.AreEqual(gun.Health.Max, gun.Health.Current, 0.001f);

            // Selling and scrap are shares of the coins spent, as before: the stars' health is in neither.
            int spent = cannon.GetLevel(0).Cost + cannon.GetLevel(1).Cost + cannon.GetLevel(2).Cost;
            Assert.AreEqual(Mathf.FloorToInt(spent * cannon.SellRefundFraction), gun.SellValue);
            gun.TakeDamage(gun.Health.Max * 2f);
            Assert.AreEqual(Mathf.FloorToInt(spent * cannon.ScrapFraction), gun.SellValue);
        }

        [Test]
        public void ATowerStartedAtAnInheritedLevel_HasEveryStarsHealth()
        {
            Tower gun = Build(GunTurretPath);
            gun.SetLevel(2);
            float built = gun.Definition.MaxHealth;
            Assert.AreEqual(built + 2f * Mathf.Round(built * BalanceSheet.GunHealthPerStar), gun.Health.Max, 0.001f, "a hybrid built at its parents' level");
        }

        [Test]
        public void TheNextStepsLine_NamesTheHealthAStarAdds()
        {
            var cannon = AssetDatabase.LoadAssetAtPath<TowerDefinition>(CannonPath);
            string line = TowerStepText.Next(cannon, 0);
            StringAssert.Contains(Loc.Format("tower.step.health", cannon.GetLevel(1).HealthBonus), line);
            Assert.AreEqual(string.Empty, TowerStepText.Next(cannon, cannon.MaxLevelIndex));
        }

        // ---- Psycho's nudge ------------------------------------------------------------------------------------------

        [Test]
        public void OnPsycho_AGunTakesFourFifthsOfABlow()
        {
            var psycho = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(PsychoPath);
            Assert.AreEqual(0.8f, psycho.TowerDamageFactor, 0.0001f, "round 88g: 0.7 -> 0.8");

            var registry = new StructureRegistry { GunDamageShare = psycho.TowerDamageFactor };
            Tower gun = Build(CannonPath, registry);
            gun.TakeDamage(100f);
            Assert.AreEqual(gun.Health.Max - 80f, gun.Health.Current, 0.001f);
        }

        // ---- the drone strip -----------------------------------------------------------------------------------------

        [Test]
        public void EveryActivity_FoldsIntoOneOfTheFourStatesOfTheStrip()
        {
            Assert.AreEqual(DroneState.Idle, DroneStates.Of(DroneActivity.Idle));
            Assert.AreEqual(DroneState.Flying, DroneStates.Of(DroneActivity.Flying));
            Assert.AreEqual(DroneState.Flying, DroneStates.Of(DroneActivity.Hauling), "carrying a load home is flying");
            Assert.AreEqual(DroneState.Working, DroneStates.Of(DroneActivity.Repairing));
            Assert.AreEqual(DroneState.Working, DroneStates.Of(DroneActivity.Mining));
            Assert.AreEqual(DroneState.Working, DroneStates.Of(DroneActivity.Fighting));
            Assert.AreEqual(DroneState.Charging, DroneStates.Of(DroneActivity.Recharging));
        }

        [Test]
        public void EveryStateWord_FitsACard()
        {
            foreach (string key in new[] { "drone.state.idle", "drone.state.flying", "drone.state.working", "drone.state.charging" })
            {
                string word = Loc.Get(key);
                Assert.AreNotEqual(key, word, key + " has no text");
                Assert.LessOrEqual(word.Length, DroneDefinition.MaxRoleLength, word + ": the chip is as wide as a card");
            }
        }
    }
}
