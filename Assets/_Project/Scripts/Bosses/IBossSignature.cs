using System.Collections;
using Game.Combat;

namespace Game.Bosses
{
    /// <summary>
    /// The one mechanic that makes a continent boss play differently (Docs/BOSSES.md), on top of the shared body in
    /// <see cref="ContinentBossBrain"/> (walking, climbing, pools). The brain asks it, each step on the road, whether it
    /// wants to act; an act is a coroutine the brain runs to its end before it walks on. Chosen from the boss's data
    /// (<see cref="Game.Enemies.BossSignature"/>) by <see cref="BossSignatures"/>.
    /// </summary>
    public interface IBossSignature
    {
        /// <summary>A fresh fight: forget the last one (pooled bosses come back).</summary>
        void Begin();

        /// <summary>Its own entrance (a crack to the colony rig), or null when the shared one or the spawn is used.</summary>
        IEnumerator Entrance();

        /// <summary>The act to run now (a burrow, a hunt, a gate to freeze), or null to walk on.</summary>
        IEnumerator NextAct();

        /// <summary>What it does at a structure ahead instead of the shared climb, or null for the shared climb.</summary>
        IEnumerator AtStructure(IStructure structure);

        /// <summary>True when it should climb down off the structure it stands on (a burrow is due).</summary>
        bool WantsOffStructure { get; }

        /// <summary>A step on the road, between acts (its rage).</summary>
        void OnRoad(float deltaTime);

        /// <summary>Every frame while it lives, whatever it does (a trail of lava, eggs ripening).</summary>
        void Always(float deltaTime);

        /// <summary>It left the field: clear what it laid on it.</summary>
        void End();
    }
}
