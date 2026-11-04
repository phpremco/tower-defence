using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A route gate (README round 25b, <c>SpawnGate_Ground</c> or <c>_Air</c>): the living mound or pod breathing on its
    /// idle loop, its lips peeling open with the Spawn clip each time a creature comes through its path's gate. View only:
    /// a gate cannot be hit or targeted.
    /// </summary>
    public sealed class SpawnGateView : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private ArtModel _model;
        private LevelContext _context;
        private int _pathIndex;
        private int _spawnClip;

        public void Initialize(ModelArtDefinition art, int pathIndex, float glow)
        {
            _pathIndex = pathIndex;
            _model = ArtModel.Create(transform, art, true);
            SpawnOriginView.KeepBoundsLive(_model);
            _model.Material.SetColor(EmissionColorId, _model.Material.GetColor(EmissionColorId) * glow);
            _spawnClip = art.IndexOf(ModelClipRole.Spawn);
            _model.Player?.Loop(art.IndexOf(ModelClipRole.Idle), 1f);
        }

        /// <summary>The socket of that name on the model (the light, the spores), or the gate itself.</summary>
        public Transform Socket(string name) => _model.Find(name) ?? transform;

        public void Bind(LevelContext context)
        {
            _context = context;
            context.GateSpawned.Subscribe(OnGateSpawned);
        }

        private void OnGateSpawned(int pathIndex)
        {
            if (pathIndex == _pathIndex)
            {
                _model.Player?.PlayOnce(_spawnClip);
            }
        }

        private void Update() => _model?.Player?.Tick(Time.deltaTime);

        private void OnDestroy()
        {
            _context?.GateSpawned.Unsubscribe(OnGateSpawned);
            _model?.Dispose();
        }
    }
}
