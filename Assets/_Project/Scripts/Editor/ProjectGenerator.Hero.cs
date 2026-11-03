using Game.Core;
using Game.Hero;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for the hero (Cole "Pathfinder" Mercer; the asset id stays Commander), its weapons and its abilities.</summary>
    public static partial class ProjectGenerator
    {
        private const string AbilityFolder = DataRoot + "/Hero/Abilities";

        // The blaster's magazine and reload. The shot interval is shortened so a full magazine plus its reload takes as
        // long as the old steady 0.35 s per shot: the same damage per second without the perk (curator decision 2).
        private const int BlasterMagazine = 8;
        private const float BlasterReload = 1.2f;
        private const float BlasterSteadyInterval = 0.35f;
        private const float BlasterInterval = (BlasterMagazine * BlasterSteadyInterval - BlasterReload) / BlasterMagazine;

        // Quick Reload halves the reload: a full cycle drops from 0.35 s to 0.275 s per shot, +27% damage per second.
        private const float QuickReloadMultiplier = 0.5f;

        private static void GenerateHero(GeneratorContext context)
        {
            HeroWeaponDefinition sword = CreateHeroWeapon("EnergySword", "Energy sword", HeroWeaponKind.Melee, HeroWeaponStyle.EnergySword, 18f, 1.5f, 0.7f, TargetLayers.Ground, new Color(0.3f, 0.95f, 1f));
            // The sidearm: the damage of a level-one pulse rifle, so the hero is worth one simple turret at range.
            HeroWeaponDefinition blaster = CreateHeroWeapon("Blaster", "Blaster", HeroWeaponKind.Ranged, HeroWeaponStyle.Blaster, 4f, 3.5f, BlasterInterval, TargetLayers.Ground | TargetLayers.Air, new Color(1f, 0.6f, 0.25f));
            var gun = new SerializedObject(blaster);
            gun.FindProperty("_magazine").intValue = BlasterMagazine;
            gun.FindProperty("_reloadSeconds").floatValue = BlasterReload;
            gun.ApplyModifiedPropertiesWithoutUndo();

            HeroAbilityDefinition shockwave = CreateAbility("Ability_Shockwave", "Shockwave", HeroAbilityKind.Shockwave, 20f, 0, 40f, 3.2f, 0f, new Color(0.4f, 0.8f, 1f),
                "A ring of force around Mercer that hits everything close by, on the ground and in the air.");
            HeroAbilityDefinition repair = CreateAbility("Ability_GateRepair", "Repair", HeroAbilityKind.GateRepair, 25f, 30, 0f, 0f, 0.4f, new Color(0.35f, 0.9f, 0.5f),
                "Mercer welds the gate he guards, giving back 40% of its health.");

            HeroDefinition hero = LoadOrCreate<HeroDefinition>($"{DataRoot}/Hero/Commander.asset");
            var heroProps = new SerializedObject(hero);
            heroProps.FindProperty("_displayName").stringValue = "Mercer";
            heroProps.FindProperty("_weapon").objectReferenceValue = sword;
            heroProps.FindProperty("_rangedWeapon").objectReferenceValue = blaster;
            heroProps.FindProperty("_reviveSeconds").floatValue = HeroReviveSeconds;
            heroProps.FindProperty("_color").colorValue = new Color(1f, 0.85f, 0.3f);
            SerializedProperty abilities = heroProps.FindProperty("_abilities");
            abilities.arraySize = 2;
            abilities.GetArrayElementAtIndex(0).objectReferenceValue = shockwave;
            abilities.GetArrayElementAtIndex(1).objectReferenceValue = repair;
            heroProps.ApplyModifiedPropertiesWithoutUndo();
            context.Hero = hero;
        }

        private static HeroAbilityDefinition CreateAbility(string id, string displayName, HeroAbilityKind kind, float cooldown, int cost, float damage, float radius, float fraction, Color tint, string description)
        {
            EnsureFolder(AbilityFolder);
            var ability = LoadOrCreate<HeroAbilityDefinition>($"{AbilityFolder}/{id}.asset");
            var so = new SerializedObject(ability);
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_cooldown").floatValue = cooldown;
            so.FindProperty("_moneyCost").intValue = cost;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_fraction").floatValue = fraction;
            so.FindProperty("_targets").intValue = (int)(TargetLayers.Ground | TargetLayers.Air);
            so.FindProperty("_tint").colorValue = tint;
            so.FindProperty("_description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ability;
        }

        // The perks the hero can earn, in campaign order; runs after the perk assets exist.
        private static void AssignHeroPerks(GeneratorContext context)
        {
            var so = new SerializedObject(context.Hero);
            SerializedProperty perks = so.FindProperty("_perks");
            perks.arraySize = context.HeroPerks.Count;
            int i = 0;
            foreach (HeroPerkDefinition perk in context.HeroPerks.Values)
            {
                perks.GetArrayElementAtIndex(i++).objectReferenceValue = perk;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
