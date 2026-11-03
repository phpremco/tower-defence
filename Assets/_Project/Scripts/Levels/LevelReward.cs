using System;
using Game.Hero;
using Game.PathObjects;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// What finishing a level gives. Points are scaled by the difficulty profile; the weapon, the gadgets and the hero
    /// perk are given on the first clear only.
    /// </summary>
    [Serializable]
    public struct LevelReward
    {
        [Min(0)] public int ResearchPoints;
        [Min(0)] public int PowerCurrency;
        public TowerDefinition NewTower;
        public UnlockSource Source;
        [TextArea] public string Story;

        /// <summary>Who made what this level unlocks, for the result screen ("Rebuilt by Dr. Voss's lab"); empty falls back to the source text.</summary>
        public string Credit;

        /// <summary>Gadgets (path objects) unlocked for the loadout from now on.</summary>
        public PathObjectDefinition[] NewPathObjects;

        /// <summary>A hero perk unlocked from now on, or null.</summary>
        public HeroPerkDefinition HeroPerk;
    }
}
