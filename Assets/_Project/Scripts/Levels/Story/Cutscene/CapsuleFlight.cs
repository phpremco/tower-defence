using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Round 59 (Remco chose the salvo): one capsule of the orbit launch as data. Where on the planet it comes down, how big it
    /// is next to the pathfinder's capsule and how its hull and markings are coloured, so the cargo capsules read apart from
    /// Spencer's without a model of their own.
    /// </summary>
    [Serializable]
    public sealed class CapsuleFlight
    {
        [Tooltip("What it carries (for the designer; not shown).")]
        [SerializeField] private string _cargo = string.Empty;
        [Tooltip("Where it comes down, in planet radii from the top of the planet: x across the lens (right is +), y away from the lens. Keep the point behind the planet's rim as the orbit lens sees it, so the capsule sinks out of sight instead of vanishing on the visible face.")]
        [SerializeField] private Vector2 _aim;
        [Tooltip("Its size as a share of the pathfinder's capsule.")]
        [SerializeField] [Range(0.4f, 1.2f)] private float _scale = 1f;
        [Tooltip("Multiplies the hull's colour; white keeps the imported look.")]
        [SerializeField] private Color _hullTint = Color.white;
        [Tooltip("The colour of its marker light (HDR); black means no marker light.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _markings = Color.black;

        public CapsuleFlight()
        {
        }

        public CapsuleFlight(string cargo, Vector2 aim, float scale, Color hullTint, Color markings)
        {
            _cargo = cargo;
            _aim = aim;
            _scale = scale;
            _hullTint = hullTint;
            _markings = markings;
        }

        public string Cargo => _cargo;
        public Vector2 Aim => _aim;
        public float Scale => _scale;
        public Color HullTint => _hullTint;
        public Color Markings => _markings;

        /// <summary>True when the capsule wears a marker light.</summary>
        public bool HasMarkings => _markings.maxColorComponent > 0f;
    }
}
