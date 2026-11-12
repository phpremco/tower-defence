using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// The painted muzzle flash of a weapon: a card that starts at the barrel's mouth and reaches out along the barrel,
    /// turned to the camera, showing a random variant of the weapon's row for a frame or three per shot, glowing in the
    /// tower's colour. One per gun, built once; a shot only moves it and sets a few properties. View only.
    /// </summary>
    [DefaultExecutionOrder(MuzzleFlashView.LateOrder)]
    public sealed class MuzzleFlashView : MonoBehaviour
    {
        // Placed after the views that pose the gun in their own LateUpdate (the hero's aim), so the card sits on the muzzle
        // where it is drawn this frame.
        public const int LateOrder = 100;
        private static readonly int FrameId = Shader.PropertyToID("_Frame");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        // Brighter at the first frame, then the flash dies within its few frames.
        private const float PeakIntensity = 1.6f;
        private const float LengthJitter = 0.2f;

        private MeshRenderer _card;
        private MaterialPropertyBlock _block;
        private WeaponVfxDefinition _weapon;
        private Transform _camera;
        private Transform _mouth;
        private Vector3 _localAxis;
        private float _length;
        private float _age = float.PositiveInfinity;

        /// <summary>False when the weapon has no painted flash: the caller keeps its own.</summary>
        public bool Initialize(WeaponVfxDefinition weapon, Color colour, bool low, Camera camera)
        {
            if (weapon == null || weapon.Muzzle == null || camera == null)
            {
                return false;
            }

            _weapon = weapon;
            _camera = camera.transform;
            VfxSheet sheet = weapon.Muzzle;
            _card = VfxQuad.Create(transform, "MuzzleFlash", VfxQuad.FromStart);
            _card.sharedMaterial = sheet.MaterialFor(low);
            _block = new MaterialPropertyBlock();
            _block.SetColor(TintId, colour);
            return true;
        }

        /// <summary>A shot left <paramref name="mouth"/> along its local <paramref name="localAxis"/>.</summary>
        public void Flash(Transform mouth, Vector3 localAxis)
        {
            _mouth = mouth;
            _localAxis = localAxis;
            _age = 0f;
            _length = _weapon.MuzzleLength * (1f + Random.Range(-LengthJitter, LengthJitter));
            VfxSheet sheet = _weapon.Muzzle;
            int column = Random.Range(0, sheet.Columns);
            _block.SetFloat(FrameId, _weapon.MuzzleRow * sheet.Columns + column);
            _block.SetFloat(IntensityId, PeakIntensity);
            _card.SetPropertyBlock(_block);
            _card.gameObject.SetActive(true);
            Place();
        }

        private void LateUpdate()
        {
            if (_card == null || !_card.gameObject.activeSelf)
            {
                return;
            }

            _age += FeedbackClock.DeltaTime;
            if (_age > _weapon.MuzzleSeconds || _mouth == null)
            {
                _card.gameObject.SetActive(false);
                return;
            }

            float fade = 1f - _age / _weapon.MuzzleSeconds;
            _block.SetFloat(IntensityId, PeakIntensity * (0.4f + 0.6f * fade));
            _card.SetPropertyBlock(_block);
            Place();
        }

        private void Place()
        {
            Transform card = _card.transform;
            card.SetPositionAndRotation(_mouth.position, VfxQuad.Along(_mouth.TransformDirection(_localAxis), _camera.forward));
            card.localScale = new Vector3(_length, _length * _weapon.Muzzle.CellAspect, 1f);
        }
    }
}
