namespace Game.Levels
{
    /// <summary>Which set performs which action, so data can never ask a set for something it cannot show.</summary>
    public static class CutsceneActions
    {
        public static CutsceneSet SetOf(CutsceneAction action)
        {
            switch (action)
            {
                case CutsceneAction.EarthDrift:
                    return CutsceneSet.Earth;
                case CutsceneAction.ScanPlanet:
                    return CutsceneSet.Survey;
                case CutsceneAction.CapsulesFall:
                    return CutsceneSet.Orbit;
                case CutsceneAction.CrystalsTremble:
                case CutsceneAction.EyesOpen:
                    return CutsceneSet.Underground;
                default:
                    return CutsceneSet.Field;
            }
        }

        public static bool Supports(CutsceneSet set, CutsceneAction action) => SetOf(action) == set;

        /// <summary>Where an action comes in the story, for sets that pose everything before it as done.</summary>
        public static int Order(CutsceneAction action)
        {
            switch (action)
            {
                case CutsceneAction.MercerReacts:
                    return (int)CutsceneAction.SwarmPours + 1;
                case CutsceneAction.GateStand:
                case CutsceneAction.Handoff:
                    return (int)action + 1;
                default:
                    return (int)action;
            }
        }
    }
}
