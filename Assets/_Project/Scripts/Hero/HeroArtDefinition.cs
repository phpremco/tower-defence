using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The imported (Blender) look of the commander: the body with its clips, the hand weapons modelled in the grip space
    /// of their sockets, which sockets they ride in, the spine bones the aim turns, and the portrait bust. A hero
    /// definition points at one; without it the hero keeps the procedural figure. Pure look data: logic never reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Art Definition", fileName = "HeroArt")]
    public sealed class HeroArtDefinition : ScriptableObject
    {
        [Header("Models")]
        [SerializeField] private ModelArtDefinition _body;
        [SerializeField] private ModelArtDefinition _pistol;
        [SerializeField] private ModelArtDefinition _swordHilt;
        [Tooltip("The blade meshes: drawn with an additive glow in the weapon's colour, not with this art's material.")]
        [SerializeField] private ModelArtDefinition _swordBlade;
        [Tooltip("Meshes of the weapon files kept for perks that do not exist yet (quick magazine, wide blade): hidden.")]
        [SerializeField] private string[] _hiddenParts = new string[0];

        [Header("Sockets")]
        [SerializeField] private string _handSocket = "Weapon_R";
        [SerializeField] private string _holsterSocket = "Holster_R";
        [SerializeField] private string _backSocket = "Back";
        [Tooltip("The bones the aim at a target turns, after the clips, sharing the turn equally.")]
        [SerializeField] private string[] _aimBones = { "Spine1", "Spine2" };
        [Tooltip("The bone the pack's thruster puffs and the down sparks come from.")]
        [SerializeField] private string _packBone = "Spine2";

        [Header("Weapons")]
        [Tooltip("The pistol's muzzle in its grip space.")]
        [SerializeField] private Vector3 _muzzle = new Vector3(0f, 0.062f, 0.192f);
        [Tooltip("Ground speed below which the hero walks instead of running.")]
        [SerializeField] [Min(0f)] private float _walkBelow = 1.5f;

        [Header("Portrait")]
        [SerializeField] private PortraitArtDefinition _bust;

        public ModelArtDefinition Body => _body;
        public ModelArtDefinition Pistol => _pistol;
        public ModelArtDefinition SwordHilt => _swordHilt;
        public ModelArtDefinition SwordBlade => _swordBlade;
        public string[] HiddenParts => _hiddenParts;
        public string HandSocket => _handSocket;
        public string HolsterSocket => _holsterSocket;
        public string BackSocket => _backSocket;
        public string[] AimBones => _aimBones;
        public string PackBone => _packBone;
        public Vector3 Muzzle => _muzzle;
        public float WalkBelow => _walkBelow;
        public PortraitArtDefinition Bust => _bust;

        private void OnValidate()
        {
            if (_body == null)
            {
                Debug.LogWarning($"HeroArtDefinition '{name}' has no body.", this);
            }
            else if (_body.IndexOf(ModelClipRole.Idle) < 0)
            {
                Debug.LogWarning($"HeroArtDefinition '{name}' has a body without an Idle clip.", this);
            }
        }
    }
}
