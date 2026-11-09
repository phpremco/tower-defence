using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// A light strip along the top of the hero's pistol, in the ranged weapon's colour: the pulse weapons' painted bolt card
    /// (the same streak their shots fly as) laid from the strip's start to the muzzle and turned to the camera, so the small
    /// gun reads as a lit line from the high game camera wherever the hand points it. Shaped by
    /// <see cref="HeroArtDefinition"/>; a single card, so the Low profile keeps it (with the sheet's Low material).
    /// </summary>
    public sealed class PistolGlow
    {
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int FrameId = Shader.PropertyToID("_Frame");

        private readonly Transform _pistol;
        private readonly Transform _camera;
        private readonly MeshRenderer _strip;
        private readonly Vector3 _start;
        private readonly Vector3 _end;
        private readonly float _width;

        private PistolGlow(Transform owner, Transform pistol, HeroArtDefinition art, VfxSheet sheet, Color colour)
        {
            _pistol = pistol;
            _start = art.PistolStripStart;
            _end = art.Muzzle;
            _width = art.PistolStripWidth;
            _camera = Camera.main.transform;
            _strip = VfxQuad.Create(owner, "PistolStrip", VfxQuad.Centred);
            _strip.sharedMaterial = sheet.MaterialFor(ModelQuality.IsLow);
            var block = new MaterialPropertyBlock();
            block.SetColor(TintId, colour * art.PistolStripGlow);
            block.SetFloat(FrameId, 0f);
            _strip.SetPropertyBlock(block);
        }

        /// <summary>The strip, or null when the data hides it, there is no camera or no painted bolt to draw it with.</summary>
        public static PistolGlow Create(Transform owner, Transform pistol, HeroArtDefinition art, Color colour)
        {
            if (art.PistolStripGlow <= 0f || art.PistolStripWidth <= 0f || Camera.main == null || !Services.TryGet(out VfxLibrary library))
            {
                return null;
            }

            WeaponVfxDefinition bolt = library.WeaponFor(ProjectileStyle.Bolt);
            return bolt != null && bolt.Projectile != null ? new PistolGlow(owner, pistol, art, bolt.Projectile, colour) : null;
        }

        /// <summary>Lays the strip along the pistol as it is posed this frame; hidden while the pistol is.</summary>
        public void Tick(bool shown)
        {
            if (_strip.gameObject.activeSelf != shown)
            {
                _strip.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                return;
            }

            Vector3 start = _pistol.TransformPoint(_start);
            Vector3 end = _pistol.TransformPoint(_end);
            Vector3 axis = end - start;
            _strip.transform.SetPositionAndRotation((start + end) * 0.5f, VfxQuad.Along(axis, _camera.forward));
            // The painted streak fades out at its ends, so the card runs a little past both ends of the lit line.
            _strip.transform.localScale = new Vector3(axis.magnitude + _width, _width, 1f);
        }
    }
}
