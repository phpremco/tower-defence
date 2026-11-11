using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// What an acting clip of the hero needs besides its keys (the Blender report's <c>acting</c> section): how far back
    /// and how far round the root goes at the clip's first frame (the clips that travel or turn start offset by exactly
    /// that, so nothing pops), and the face's blend-shape tracks, which the FBX does not carry.
    /// </summary>
    [System.Serializable]
    public sealed class HeroActingClip
    {
        [SerializeField] private ModelClipRole _role;

        [Tooltip("Metres the root moves backwards at the clip's first frame (the body starts this far in front of it).")]
        [SerializeField] [Min(0f)] private float _startBack;

        [Tooltip("Degrees the root turns at the clip's first frame (Unity yaw: negative to the left); the body starts turned back by it.")]
        [SerializeField] private float _startTurn;

        [SerializeField] private FaceCurve[] _face = System.Array.Empty<FaceCurve>();

        public HeroActingClip()
        {
        }

        public HeroActingClip(ModelClipRole role, float startBack, float startTurn, FaceCurve[] face)
        {
            _role = role;
            _startBack = startBack;
            _startTurn = startTurn;
            _face = face;
        }

        public ModelClipRole Role => _role;
        public float StartBack => _startBack;
        public float StartTurn => _startTurn;
        public FaceCurve[] Face => _face ?? System.Array.Empty<FaceCurve>();
    }
}
