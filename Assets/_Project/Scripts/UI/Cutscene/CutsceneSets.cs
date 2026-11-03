using Game.Levels;

namespace Game.UI
{
    /// <summary>
    /// Makes the set for a place. A plain switch instead of reflection, so it survives IL2CPP stripping; a new set is
    /// one class and one line here. Three sets play in space (Earth, the survey and the orbit), one under
    /// the field (the eyes) and one on the field itself.
    /// </summary>
    public static class CutsceneSets
    {
        public static ICutsceneSet Create(CutsceneSet set)
        {
            switch (set)
            {
                case CutsceneSet.Earth:
                    return new EarthSet();
                case CutsceneSet.Survey:
                    return new SurveySet();
                case CutsceneSet.Orbit:
                    return new OrbitSet();
                case CutsceneSet.Underground:
                    return new UndergroundSet();
                default:
                    return new FieldSet();
            }
        }
    }
}
