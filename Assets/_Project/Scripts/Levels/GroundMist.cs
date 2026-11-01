using UnityEngine;

namespace Game.Levels
{
    /// <summary>Drifts a mist layer's texture across the field, so the fog never sits still.</summary>
    public sealed class GroundMist : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private Material _material;
        private Vector2 _velocity;
        private Vector2 _offset;

        public void Initialize(Material material, Vector2 velocity)
        {
            _material = material;
            _velocity = velocity;
        }

        private void Update()
        {
            _offset += _velocity * Time.deltaTime;
            _material.SetTextureOffset(BaseMapId, _offset);
        }
    }
}
