using UnityEngine;

namespace Game.Core
{
    /// <summary>Switches for testing a level quickly. Lives in an asset so nothing is hardcoded in logic.</summary>
    [CreateAssetMenu(menuName = "Game/Playtest Settings", fileName = "PlaytestSettings")]
    public sealed class PlaytestSettings : ScriptableObject
    {
        [SerializeField] private bool _unlockAllWeapons;
        [SerializeField] private bool _infiniteMoney;

        public bool UnlockAllWeapons => _unlockAllWeapons;
        public bool InfiniteMoney => _infiniteMoney;
    }
}


