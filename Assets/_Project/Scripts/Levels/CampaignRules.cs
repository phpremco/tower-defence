using Game.Economy;

namespace Game.Levels
{
    /// <summary>Which map nodes and regions can be played. Pure rules, driven by the catalog and the saved progress.</summary>
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

        /// <summary>A region can be entered once its first level can be played.</summary>
        public static bool IsRegionOpen(CampaignCatalog catalog, int region, ProgressService progress)
        {
            int first = catalog.FirstNode(region);
            return first >= 0 && IsOpen(catalog, first, progress);
        }

        /// <summary>How many levels of the region have been beaten on any difficulty.</summary>
        public static int CompletedInRegion(CampaignCatalog catalog, int region, ProgressService progress)
        {
            int count = 0;
            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                if (catalog.Nodes[i].Region == region && IsCompleted(catalog, i, progress))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
