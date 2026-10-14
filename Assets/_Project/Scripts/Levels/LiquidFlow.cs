using UnityEngine;

namespace Game.Levels
{
    /// <summary>Slides a liquid's texture so water ripples, lava creeps and sand slowly churns. Pure view code.</summary>
    public sealed class LiquidFlow : MonoBehaviour
    {
        private Material _material;
        private Vector2 _speed;
        private Vector2 _offset;

        public void Initialize(Material material, Vector2 speed)
        {
            _material = material;
            _speed = speed;
        }

        private void Update()
        {
            _offset += _speed * Time.deltaTime;
            _material.SetTextureOffset("_BaseMap", _offset);
            _material.SetTextureOffset("_EmissionMap", _offset);
        }
    }
}
