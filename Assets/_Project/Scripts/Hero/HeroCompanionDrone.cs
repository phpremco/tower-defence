using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The small repair drone that keeps the hero company once the upgrade is bought: it hovers at his shoulder, trails
    /// a little behind when he moves, and while his health climbs back it lays a cone of light on him with a soft glow
    /// where it lands; both fade in and out. Shaped by <see cref="HeroGearArtDefinition"/>. View only: the faster healing
    /// itself is the commander's.
    /// </summary>
    public sealed class HeroCompanionDrone
    {
        private const float TurnDegreesPerSecond = 360f;

        private readonly HeroGearArtDefinition _art;
        private readonly Transform _holder;
        private readonly ArtModel _model;
        private readonly Transform[] _rotors;
        private readonly Transform _beamSocket;
        private readonly Transform _mendBone;
        private readonly MeshRenderer _cone;
        private readonly MaterialPropertyBlock _coneBlock = new MaterialPropertyBlock();
        private readonly BeamFlare _glow;
        private readonly BeamFlare _runningLight;
        private readonly float _bobPhase;
        private float _fade;

        private HeroCompanionDrone(HeroGearArtDefinition art, Transform holder, ArtModel model, Transform mendBone, bool live)
        {
            _art = art;
            _holder = holder;
            _model = model;
            _rotors = model.Numbered(art.DroneRotorPrefix);
            _beamSocket = model.Find(art.DroneBeamSocket);
            _mendBone = mendBone;
            _bobPhase = Random.value * Mathf.PI * 2f;
            if (!live)
            {
                return;
            }

            MarkerArt markers = MarkerArt.Active;
            _cone = markers != null ? markers.Place(MarkerKind.ToolBeam, holder.parent, "CompanionBeam") : null;
            if (_cone != null)
            {
                _cone.gameObject.SetActive(false);
            }

            _glow = BeamFlare.Create(holder.parent, "CompanionGlow");
            _glow.Hide();
            if (art.DroneLightSize > 0f && art.DroneLightStrength > 0f)
            {
                _runningLight = BeamFlare.Create(holder.parent, "CompanionLight");
            }
        }

        /// <summary>The drone's root, for owners that place or measure it.</summary>
        public Transform Holder => _holder;

        /// <summary>How far the mending light is up, 0 to 1 (for tests and probes).</summary>
        public float Fade => _fade;

        /// <summary>
        /// The companion in the level: it lives beside the hero (under <paramref name="parent"/>, not under his figure) so it
        /// can lag behind him. Null when the gear has no drone or the art has no model for it.
        /// </summary>
        public static HeroCompanionDrone Create(Transform parent, Transform mendBone, HeroGearArtDefinition art, HeroGear gear, Vector3 heroPosition, Vector3 facing)
        {
            if (art == null || art.DroneBody == null || gear.RepairDroneTier <= 0)
            {
                return null;
            }

            Transform holder = NewHolder(parent, art);
            var drone = new HeroCompanionDrone(art, holder, ArtModel.CreateStill(holder, art.DroneBody), mendBone, live: true);
            holder.SetPositionAndRotation(drone.Place(heroPosition, facing, 0f), Quaternion.LookRotation(facing));
            return drone;
        }

        /// <summary>The companion beside a figure shown in a menu: fixed at its place, no light.</summary>
        public static HeroCompanionDrone CreateStill(Transform figure, HeroGearArtDefinition art, HeroGear gear)
        {
            if (art == null || art.DroneBody == null || gear.RepairDroneTier <= 0)
            {
                return null;
            }

            Transform holder = NewHolder(figure, art);
            holder.localPosition = art.DroneOffset;
            return new HeroCompanionDrone(art, holder, ArtModel.CreateStill(holder, art.DroneBody), null, live: false);
        }

        private static Transform NewHolder(Transform parent, HeroGearArtDefinition art)
        {
            var holder = new GameObject("CompanionDrone").transform;
            holder.SetParent(parent, false);
            holder.localScale = Vector3.one * art.DroneScale;
            return holder;
        }

        /// <summary>Follows the hero and shows or fades the mending light. Call after the hero's body has been posed.</summary>
        public void Tick(float deltaTime, Vector3 heroPosition, Vector3 facing, bool mending)
        {
            Vector3 target = Place(heroPosition, facing, Mathf.Sin(Time.time * _art.DroneBobSpeed + _bobPhase) * _art.DroneBobHeight);
            float follow = 1f - Mathf.Exp(-_art.DroneFollow * deltaTime);
            _holder.position = Vector3.Lerp(_holder.position, target, follow);
            _holder.rotation = Quaternion.RotateTowards(_holder.rotation, Quaternion.LookRotation(facing), TurnDegreesPerSecond * deltaTime);
            for (int i = 0; i < _rotors.Length; i++)
            {
                _rotors[i].Rotate(0f, _art.DroneRotorDegreesPerSecond * deltaTime, 0f, Space.Self);
            }

            _fade = Mathf.MoveTowards(_fade, mending ? 1f : 0f, deltaTime / _art.MendFadeSeconds);
            Light();
        }

        public void SetVisible(bool visible)
        {
            if (_holder.gameObject.activeSelf != visible)
            {
                _holder.gameObject.SetActive(visible);
            }
        }

        public void Dispose()
        {
            _model.Dispose();
            if (_cone != null)
            {
                Object.Destroy(_cone.gameObject);
            }

            if (_glow != null)
            {
                Object.Destroy(_glow.GameObject);
            }

            if (_runningLight != null)
            {
                Object.Destroy(_runningLight.GameObject);
            }

            if (_holder != null)
            {
                Object.Destroy(_holder.gameObject);
            }
        }

        private Vector3 Place(Vector3 heroPosition, Vector3 facing, float bob)
        {
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            Vector3 offset = _art.DroneOffset;
            return heroPosition + right * offset.x + Vector3.up * (offset.y + bob) + facing * offset.z;
        }

        // The cone runs from the drone's emitter to the spot on his chest; the smoothed fade is squared so the light eases in.
        private void Light()
        {
            float strength = _fade * _fade;
            bool shown = _holder.gameObject.activeSelf;
            Vector3 from = _beamSocket != null ? _beamSocket.position : _holder.position;
            if (_runningLight != null)
            {
                if (shown)
                {
                    _runningLight.Show(from, _art.DroneLightSize, _art.MendColor, _art.DroneLightStrength);
                }
                else
                {
                    _runningLight.Hide();
                }
            }

            bool lit = strength > 0.001f && _mendBone != null && shown;
            if (_cone != null && _cone.gameObject.activeSelf != lit)
            {
                _cone.gameObject.SetActive(lit);
            }

            if (!lit)
            {
                _glow?.Hide();
                return;
            }

            Vector3 to = _mendBone.position + _mendBone.up * _art.MendAlong;
            Vector3 delta = to - from;
            if (_cone != null && delta.sqrMagnitude > 0.0001f)
            {
                float mouth = _art.MendBeamWidth;
                _cone.transform.SetPositionAndRotation(from, Quaternion.LookRotation(delta));
                _cone.transform.localScale = new Vector3(mouth, mouth, delta.magnitude);
                MarkerArt.SetTint(_cone, _coneBlock, _art.MendColor * (_art.MendBeamGlow * strength));
            }

            _glow?.Show(to, _art.MendGlowSize, _art.MendColor, _art.MendGlowStrength * strength);
        }
    }
}
