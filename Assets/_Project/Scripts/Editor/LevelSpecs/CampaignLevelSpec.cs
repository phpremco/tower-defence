using Game.Balance;

namespace Game.Editor
{
    /// <summary>
    /// One campaign level as data: names and briefing, the map template (<see cref="LayoutSpec"/>), the wave script, the
    /// reward, the story triggers and the economy. Pure C# with no engine types, like <see cref="WaveScripts"/>, so the
    /// BalanceLab tool compiles it too. The generator step "Campaign levels" turns each spec into
    /// <c>Data/Levels/Level_&lt;Key&gt;.asset</c>; the key is the asset id and never changes once shipped.
    /// </summary>
    public sealed class CampaignLevelSpec
    {
        public string Key = string.Empty;
        public string Name = string.Empty;

        /// <summary>The part of the continent the level takes, as the map panel names it under the continent.</summary>
        public string Area = string.Empty;

        /// <summary>The default LevelStart text, shown when the level has no opening dialogue.</summary>
        public string Briefing = string.Empty;

        public LayoutSpec Layout = new LayoutSpec();
        public WaveScriptSpec Waves = new WaveScriptSpec();

        /// <summary>The element whose colours tint the continent palette for this level (Water, Wood, Sand, Fire or Crystal).</summary>
        public string Tint = "Crystal";

        /// <summary>Tower id granted on the first clear, or null. A weapon debuts in the level after the one that grants it.</summary>
        public string RewardTower;

        /// <summary>Path object ids granted on the first clear.</summary>
        public string[] RewardGadgets = new string[0];

        /// <summary>Hero perk id (asset name) granted on the first clear, or null.</summary>
        public string RewardHeroPerk;

        /// <summary>Where the reward comes from in the story: a <c>Game.Levels.UnlockSource</c> name.</summary>
        public string Source = "None";

        /// <summary>The line on the result screen that tells where the new weapon came from.</summary>
        public string RewardStory = string.Empty;

        /// <summary>Who made what the level unlocks, shown on the result screen instead of the source text ("Rebuilt by Dr. Voss's lab").</summary>
        public string Credit = string.Empty;

        public StorySpec[] Story = new StorySpec[0];

        public int Money = 200;
        public int BaseHealth = 600;
    }
}
