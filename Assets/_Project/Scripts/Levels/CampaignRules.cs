using Game.Economy;

namespace Game.Levels
{
    /// <summary>Which map nodes can be played. Pure rules, driven by the catalog and the saved progress.</summary>
    public static class CampaignRules
    {
        public static bool IsOpen(CampaignCatalog catalog, int nodeIndex, ProgressService progress)
        {
            MapNode node = catalog.Nodes[nodeIndex];
            if (node.Level == null)
            {
                return false;
            }

            if (nodeIndex == 0)
            {
                return true;
            }

            MapNode previous = catalog.Nodes[nodeIndex - 1];
            return previous.Level != null && progress.IsLevelCompleted(previous.Level.Id);
        }

        public static bool IsCompleted(CampaignCatalog catalog, int nodeIndex, ProgressService progress)
        {
            LevelDefinition level = catalog.Nodes[nodeIndex].Level;
            return level != null && progress.IsLevelCompleted(level.Id);
        }
    }
}
