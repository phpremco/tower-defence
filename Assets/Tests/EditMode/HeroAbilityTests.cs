using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The hero ability framework (S4 spec 1): cooldowns, money paid only when the ability fires, slots from data, the
    /// generated abilities keeping the old numbers, and the gate for pause and dialogue.
    /// </summary>
    public sealed class HeroAbilityTests
    {
        private const string HeroPath = "Assets/_Project/Data/Hero/Commander.asset";

        private sealed class FakeGuarded : IGuardable
        {
            public Vector3 Position { get; set; }

            public IDamageGuard Guard { get; set; }

            public bool CanRestore { get; set; } = true;

            public float Restored { get; private set; }

            public void Restore(float fraction) => Restored += fraction;
        }

        private sealed class FakeTarget : ITargetable
        {
            public Vector3 Position { get; set; }

            public TargetLayers Layer { get; set; } = TargetLayers.Ground;

            public float PathProgress => 0f;

            public bool IsAlive => true;

            public float Taken { get; private set; }

            public void TakeDamage(float amount) => Taken += amount;

            public void ApplySlow(float speedFactor, float seconds)
            {
            }
        }

        private readonly List<Object> _created = new List<Object>();
        private FakeGuarded _gate;
        private TargetRegistry _targets;

        public static HeroAbilityDefinition CreateAbility(HeroAbilityKind kind, float cooldown, int cost, float damage = 0f, float radius = 0f, float fraction = 0f)
        {
            var ability = ScriptableObject.CreateInstance<HeroAbilityDefinition>();
            var so = new SerializedObject(ability);
            so.FindProperty("_displayName").stringValue = kind.ToString();
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_cooldown").floatValue = cooldown;
            so.FindProperty("_moneyCost").intValue = cost;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_fraction").floatValue = fraction;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ability;
        }

        private Commander CreateHero(params HeroAbilityDefinition[] abilities)
        {
            var definition = ScriptableObject.CreateInstance<HeroDefinition>();
            var weapon = ScriptableObject.CreateInstance<HeroWeaponDefinition>();
            _created.Add(definition);
            _created.Add(weapon);
            var so = new SerializedObject(definition);
            so.FindProperty("_weapon").objectReferenceValue = weapon;
            SerializedProperty slots = so.FindProperty("_abilities");
            slots.arraySize = abilities.Length;
            for (int i = 0; i < abilities.Length; i++)
            {
                _created.Add(abilities[i]);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = abilities[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            var go = new GameObject("Hero");
            _created.Add(go);
            var hero = go.AddComponent<Commander>();
            _gate = new FakeGuarded();
            _targets = new TargetRegistry();
            var posts = new List<HeroPost>
            {
                new HeroPost(_gate, new Vector3(-0.65f, 0f, 0f), Vector3.right),
                new HeroPost(new FakeGuarded { Position = new Vector3(5f, 0f, 0f) }, new Vector3(4.35f, 0f, 0f), Vector3.right),
            };
            hero.Initialize(definition, _targets, posts, 0, PlayerModifiers.None);
            return hero;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Cooldown_CountsDownAndFreesTheSlot()
        {
            var state = new HeroAbilityState(CreateAbility(HeroAbilityKind.Shockwave, 20f, 0, 40f, 3.2f));
            _created.Add(state.Definition);
            Assert.IsTrue(state.Ready);
            state.Begin();
            Assert.IsFalse(state.Ready);
            Assert.AreEqual(0f, state.ReadyFraction, 0.001f);
            state.Tick(10f);
            Assert.AreEqual(0.5f, state.ReadyFraction, 0.001f);
            state.Tick(10f);
            Assert.IsTrue(state.Ready);
            Assert.IsFalse(state.Tick(1f), "a ready slot has nothing to count");
        }

        [Test]
        public void Slots_FollowTheDefinitionOrder()
        {
            Commander hero = CreateHero(CreateAbility(HeroAbilityKind.Shockwave, 20f, 0, 40f, 3.2f), CreateAbility(HeroAbilityKind.GateRepair, 25f, 30, fraction: 0.4f));
            var near = new FakeTarget { Position = new Vector3(0f, 0f, 1f) };
            _targets.Add(near);

            Assert.IsTrue(hero.TryUse(0));
            Assert.AreEqual(40f, near.Taken, 0.001f, "slot 0 is the shockwave");
            Assert.AreEqual(0f, _gate.Restored, 0.001f);

            Assert.IsTrue(hero.TryUse(1));
            Assert.AreEqual(0.4f, _gate.Restored, 0.001f, "slot 1 is the repair");
        }

        [Test]
        public void Money_IsPaidOnlyWhenTheAbilityFires()
        {
            Commander hero = CreateHero(CreateAbility(HeroAbilityKind.Shockwave, 20f, 0, 40f, 3.2f), CreateAbility(HeroAbilityKind.GateRepair, 25f, 30, fraction: 0.4f));
            var wallet = new Wallet(100, false);

            Assert.IsTrue(HeroController.TryAbility(hero, 1, true, wallet));
            Assert.AreEqual(70, wallet.Money);
            Assert.IsFalse(HeroController.TryAbility(hero, 1, true, wallet), "cooling down");
            Assert.AreEqual(70, wallet.Money, "a press during the cooldown costs nothing");

            var poor = new Wallet(10, false);
            Commander other = CreateHero(CreateAbility(HeroAbilityKind.GateRepair, 25f, 30, fraction: 0.4f));
            Assert.IsFalse(HeroController.TryAbility(other, 0, true, poor));
            Assert.AreEqual(10, poor.Money);
            Assert.IsTrue(other.CanUse(0), "an unpaid press does not start the cooldown");
        }

        [Test]
        public void BlockedInput_UsesNothing()
        {
            Commander hero = CreateHero(CreateAbility(HeroAbilityKind.GateRepair, 25f, 30, fraction: 0.4f));
            var wallet = new Wallet(100, false);

            Assert.IsFalse(HeroController.TryAbility(hero, 0, false, wallet));
            Assert.AreEqual(100, wallet.Money);
            Assert.AreEqual(0f, _gate.Restored, 0.001f);
        }

        [Test]
        public void GateRepair_OnAnUnhurtOrWreckedGate_CostsNothing()
        {
            Commander hero = CreateHero(CreateAbility(HeroAbilityKind.GateRepair, 25f, 30, fraction: 0.4f));
            var wallet = new Wallet(100, false);
            _gate.CanRestore = false;

            Assert.IsFalse(hero.CanUse(0), "the button shows not ready");
            Assert.IsFalse(HeroController.TryAbility(hero, 0, true, wallet));
            Assert.AreEqual(100, wallet.Money, "no money taken");
            Assert.AreEqual(0f, _gate.Restored, 0.001f);

            _gate.CanRestore = true;
            Assert.IsTrue(hero.CanUse(0), "no cooldown was started by the refused press");
            Assert.IsTrue(HeroController.TryAbility(hero, 0, true, wallet));
            Assert.AreEqual(70, wallet.Money);
        }

        [Test]
        public void BaseStructure_CanBeRestoredOnlyWhileHurtAndStanding()
        {
            var structure = new BaseStructure(Vector3.zero, new Health(100f));
            Assert.IsFalse(structure.CanRestore, "full health");
            structure.TakeDamage(10f);
            Assert.IsTrue(structure.CanRestore, "hurt");
            structure.TakeDamage(200f);
            Assert.IsFalse(structure.CanRestore, "destroyed");
        }

        [Test]
        public void PostOrder_IsGatedLikeTheAbilities()
        {
            Commander hero = CreateHero();

            Assert.IsFalse(HeroController.TrySend(hero, 1, false), "paused or held by a dialogue");
            Assert.AreEqual(0, hero.PostIndex);
            Assert.IsFalse(hero.IsWalking);

            Assert.IsFalse(HeroController.TrySend(hero, 2, true), "no such post");
            Assert.AreEqual(0, hero.PostIndex);

            Assert.IsTrue(HeroController.TrySend(hero, 1, true));
            Assert.AreEqual(1, hero.PostIndex);
            Assert.IsTrue(hero.IsWalking);
        }

        [Test]
        public void Cooldown_ReportsAChangeOnlyWhenTheShownSecondsChange()
        {
            var state = new HeroAbilityState(CreateAbility(HeroAbilityKind.Shockwave, 20f, 0, 40f, 3.2f));
            _created.Add(state.Definition);
            state.Begin();
            Assert.AreEqual(20, state.SecondsLeft);

            Assert.IsFalse(state.Tick(0.4f), "still shows 20");
            Assert.IsFalse(state.Tick(0.5f), "still shows 20");
            Assert.IsTrue(state.Tick(0.2f), "now shows 19");
            Assert.AreEqual(19, state.SecondsLeft);

            int changes = 0;
            for (int frame = 0; frame < 2000 && !state.Ready; frame++)
            {
                if (state.Tick(1f / 60f))
                {
                    changes++;
                }
            }

            Assert.IsTrue(state.Ready);
            Assert.AreEqual(19, changes, "one refresh a second, the last when the slot becomes ready");
        }

        [Test]
        public void LockedSlot_StaysEmptyUntilItsPerk()
        {
            var perk = ScriptableObject.CreateInstance<HeroPerkDefinition>();
            _created.Add(perk);
            HeroAbilityDefinition locked = CreateAbility(HeroAbilityKind.GateRepair, 25f, 0, fraction: 0.4f);
            var so = new SerializedObject(locked);
            so.FindProperty("_requiresPerk").objectReferenceValue = perk;
            so.ApplyModifiedPropertiesWithoutUndo();
            Commander hero = CreateHero(locked);

            Assert.IsNull(hero.AbilityAt(0));
            Assert.IsFalse(hero.CanUse(0));
        }

        [Test]
        public void GeneratedAbilities_KeepTheOldNumbers()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(HeroPath);
            Assert.IsNotNull(hero, "run Tools/Generate/Scenes and Starter Data");
            Assert.AreEqual("Spencer", hero.DisplayName);
            Assert.AreEqual(2, hero.Abilities.Length);

            HeroAbilityDefinition shockwave = hero.Abilities[0];
            Assert.AreEqual("Ability_Shockwave", shockwave.Id);
            Assert.AreEqual(HeroAbilityKind.Shockwave, shockwave.Kind);
            Assert.AreEqual(40f, shockwave.Damage, 0.001f);
            Assert.AreEqual(3.2f, shockwave.Radius, 0.001f);
            Assert.AreEqual(20f, shockwave.Cooldown, 0.001f);
            Assert.AreEqual(TargetLayers.Ground | TargetLayers.Air, shockwave.Targets);
            Assert.AreEqual(0, shockwave.MoneyCost);

            HeroAbilityDefinition repair = hero.Abilities[1];
            Assert.AreEqual("Ability_GateRepair", repair.Id);
            Assert.AreEqual(HeroAbilityKind.GateRepair, repair.Kind);
            Assert.AreEqual(0.4f, repair.Fraction, 0.001f);
            Assert.AreEqual(25f, repair.Cooldown, 0.001f);
            Assert.AreEqual(30, repair.MoneyCost);
            Assert.IsNull(shockwave.Problem());
            Assert.IsNull(repair.Problem());
        }
    }
}
