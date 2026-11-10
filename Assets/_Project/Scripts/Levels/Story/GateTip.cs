using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One of Dr. Voss's gate tips (Remco 2026-11-08, playtest note 12): a conversation that plays once per save the first
    /// time its moment comes in a level, teaching the in-level gate upgrade or moving the hero to the gate under pressure.
    /// </summary>
    [Serializable]
    public struct GateTip
    {
        public GateTipKind Kind;
        public DialogueDefinition Dialogue;

        /// <summary>A gate counts as hurt below this share of its health.</summary>
        [Range(0.05f, 1f)] public float HurtBelow;

        /// <summary>MoveHero only: the gate the hero guards counts as quiet at or above this share of its health.</summary>
        [Range(0f, 1f)] public float QuietAbove;

        public GateTip(GateTipKind kind, DialogueDefinition dialogue, float hurtBelow, float quietAbove)
        {
            Kind = kind;
            Dialogue = dialogue;
            HurtBelow = hurtBelow;
            QuietAbove = quietAbove;
        }
    }
}
