namespace Game.Platform
{
    /// <summary>Achievement reporting. Steam code lives behind this interface and never in gameplay code.</summary>
    public interface IAchievementService
    {
        void Unlock(string achievementId);
    }
}
