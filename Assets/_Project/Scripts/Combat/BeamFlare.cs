using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// The soft round glow where a beam leaves its source (a muzzle bloom) or meets its target (an impact glow): one card in
    /// <see cref="LineKit.Flare"/>, turned to the camera, sized by the caller from the beam's own width so the glow joins the
    /// beam instead of swamping what it hits. View only; built once, a shot only moves and tints it.
    /// </summary>
    public sealed class BeamFlare
    {
        private readonly MeshRenderer _card;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Transform _camera;

        private BeamFlare(MeshRenderer card)
        {
            _card = card;
        }

        /// <summary>The card's object, for owners that track what lives beside them.</summary>
        public GameObject GameObject => _card.gameObject;

        public static BeamFlare Create(Transform parent, string name)
        {
            MeshRenderer card = VfxQuad.Create(parent, name, VfxQuad.Centred);
            card.sharedMaterial = LineKit.Flare();
            return new BeamFlare(card);
        }

        /// <summary>Shows the glow at <paramref name="position"/>, <paramref name="size"/> across, in <paramref name="colour"/>.</summary>
        public void Show(Vector3 position, float size, Color colour, float strength = 1f)
        {
            if (_camera == null)
            {
                Camera main = Camera.main;
                if (main == null)
                {
                    return;
                }

                _camera = main.transform;
            }

            Transform card = _card.transform;
            card.SetPositionAndRotation(position, _camera.rotation);
            card.localScale = new Vector3(size, size, 1f);
            LineKit.SetFlareColour(_block, colour, strength);
            _card.SetPropertyBlock(_block);
            if (!_card.gameObject.activeSelf)
            {
                _card.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_card != null && _card.gameObject.activeSelf)
            {
                _card.gameObject.SetActive(false);
            }
        }
    }
}
