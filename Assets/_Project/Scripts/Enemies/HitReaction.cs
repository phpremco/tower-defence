using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// When a creature flinches from a hit (Remco 2026-11-05: under rapid fire the hit clip restarted on every bullet and
    /// jittered). After a flinch it will not flinch again for a cooldown, and small chip damage only counts up until it
    /// adds to a noticeable share of its health. Damage, the health bar and the flash still follow every hit; only the
    /// body's reaction is rationed. Pure logic: the view ticks it and asks it.
    /// </summary>
    public sealed class HitReaction
    {
        /// <summary>Seconds between flinches of an ordinary creature (a little longer than its hit clip).</summary>
        public const float CreatureCooldown = 0.7f;

        /// <summary>A boss flinches seldom: it is big and it must not look nervous.</summary>
        public const float BossCooldown = 1.8f;

        /// <summary>The share of its health a creature must lose (since its last flinch) before it flinches.</summary>
        public const float CreatureChipShare = 0.04f;

        /// <summary>A boss's share: hits on it are many and small next to its health.</summary>
        public const float BossChipShare = 0.015f;

        private readonly float _cooldown;
        private readonly float _chipShare;
        private float _since;
        private float _gathered;

        public HitReaction(float cooldown, float chipShare)
        {
            _cooldown = Mathf.Max(0f, cooldown);
            _chipShare = Mathf.Max(0f, chipShare);
            Reset();
        }

        /// <summary>The rationing for a creature: its own cooldown when its data sets one, else the shared default.</summary>
        public static HitReaction For(EnemyDefinition definition, float hitClipSeconds)
        {
            bool boss = definition != null && definition.IsBoss;
            float cooldown = definition != null && definition.HitCooldown > 0f ? definition.HitCooldown : boss ? BossCooldown : CreatureCooldown;
            return new HitReaction(Mathf.Max(cooldown, hitClipSeconds), boss ? BossChipShare : CreatureChipShare);
        }

        public float Cooldown => _cooldown;

        /// <summary>Ready for a fresh creature from the pool: the first real hit flinches.</summary>
        public void Reset()
        {
            _since = float.PositiveInfinity;
            _gathered = 0f;
        }

        public void Tick(float deltaTime) => _since += deltaTime;

        /// <summary>
        /// A hit that took <paramref name="healthShare"/> of the creature's health: true when the body should react now.
        /// <paramref name="busy"/> (a mechanic or a strike playing) never reacts and does not use up the damage gathered.
        /// </summary>
        public bool Register(float healthShare, bool busy)
        {
            _gathered += Mathf.Max(0f, healthShare);
            if (busy || _since < _cooldown || _gathered < _chipShare)
            {
                return false;
            }

            _since = 0f;
            _gathered = 0f;
            return true;
        }
    }
}
