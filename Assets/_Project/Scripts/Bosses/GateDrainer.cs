using Game.Combat;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The one gate whose gun a boss drains for a phase (Docs/BOSSES.md, the Sealed One): the gate keeps blocking but holds
    /// its fire, and gets its gun back when the phase ends, whichever gate the next phase drains. One gate at a time.
    /// </summary>
    public sealed class GateDrainer
    {
        /// <summary>The gate drained now, or null.</summary>
        public Tower Drained { get; private set; }

        /// <summary>Gives the last gate its gun back and drains the next one (none for null).</summary>
        public void Drain(Tower gate)
        {
            Release();
            if (gate == null)
            {
                return;
            }

            gate.SetDrained(true);
            Drained = gate;
        }

        /// <summary>The drained gate gets its gun back.</summary>
        public void Release()
        {
            if (Drained != null)
            {
                Drained.SetDrained(false);
            }

            Drained = null;
        }

        /// <summary>
        /// The standing gate with a gun nearest a point, another than the one drained last when there is another: each
        /// phase takes a different gate's fire.
        /// </summary>
        public static Tower Choose(StructureRegistry structures, Vector3 from, Tower previous)
        {
            Tower best = null;
            Tower fallback = null;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < structures.Count; i++)
            {
                if (!(structures[i] is Tower gate) || !gate.Definition.BlocksGroundEnemies || !gate.IsAlive || !HasGun(gate))
                {
                    continue;
                }

                if (gate == previous)
                {
                    fallback = gate;
                    continue;
                }

                float sqr = (gate.Position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = gate;
                }
            }

            return best != null ? best : fallback;
        }

        private static bool HasGun(Tower gate) => gate.Definition.GetLevel(gate.LevelIndex).Damage > 0f;
    }
}
