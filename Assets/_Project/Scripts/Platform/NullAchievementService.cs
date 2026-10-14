using UnityEngine;

namespace Game.Platform
{
    /// <summary>Placeholder until a Steam implementation exists.</summary>
    public sealed class NullAchievementService : IAchievementService
    {
        public void Unlock(string achievementId)
        {
            Debug.Log($"Achievement unlocked (no backend): {achievementId}");
        }
    }
}
