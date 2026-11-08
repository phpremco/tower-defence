using Game.Core;
using Game.Hero;
using Game.Levels;

namespace Game.UI
{
    /// <summary>
    /// The name the interface calls the hero by. He is the only hero, so labels use his name from data
    /// (<see cref="HeroDefinition.DisplayName"/>) instead of a generic "Hero"; nothing in the UI spells it out.
    /// </summary>
    public static class HeroNaming
    {
        private const string Fallback = "Commander";

        /// <summary>The hero of this level, else the campaign's.</summary>
        public static string Of(LevelContext context)
        {
            if (context != null && context.Hero != null)
            {
                return context.Hero.Definition.DisplayName;
            }

            if (context != null && context.Level != null && context.Level.Hero != null)
            {
                return context.Level.Hero.DisplayName;
            }

            return Campaign();
        }

        /// <summary>The hero of the first campaign level that has one, for screens outside a level.</summary>
        public static string Campaign()
        {
            if (!Services.TryGet(out CampaignCatalog catalog))
            {
                return Fallback;
            }

            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null && node.Level.Hero != null)
                {
                    return node.Level.Hero.DisplayName;
                }
            }

            return Fallback;
        }
    }
}
