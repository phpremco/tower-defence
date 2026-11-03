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
    }
}
