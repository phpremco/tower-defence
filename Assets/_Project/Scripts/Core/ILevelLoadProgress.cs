namespace Game.Core
{
    /// <summary>
    /// What a level's build tells whoever covers it (the loading screen): how far it is, and that it is done. Registered
    /// in <see cref="Services"/> only while a level loads behind a screen, so the build knows whether to spread itself
    /// over frames; the build never knows the screen.
    /// </summary>
    public interface ILevelLoadProgress
    {
        /// <summary>The share of the load that is done, from 0 to 1. A lower value than the last is ignored.</summary>
        void Report(float fraction);

        /// <summary>The level stands (or could not be built): the cover may lift.</summary>
        void Complete();
    }
}
