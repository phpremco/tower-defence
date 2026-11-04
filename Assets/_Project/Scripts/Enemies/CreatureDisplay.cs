using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// An imported creature shown outside a fight (a menu turntable, the codex, an icon): its model standing on the
    /// ground in its idle cycle, posed at once so a single rendered frame already shows it at rest. Frees the model's
    /// graphs and material with its object.
    /// </summary>
    public sealed class CreatureDisplay : MonoBehaviour
    {
        private CreatureModel _model;
        private CreatureClips _clips;
        private float _scale;

        public static Transform Build(Transform parent, EnemyDefinition definition)
        {
            var root = new GameObject($"{definition.name}_display").transform;
            root.SetParent(parent, false);
            var display = root.gameObject.AddComponent<CreatureDisplay>();
            ModelArtDefinition art = definition.Art;
            display._scale = definition.Scale * art.Scale;
            display._model = CreatureModel.Create(root, art, display._scale, ModelQuality.IsLow);
            display._clips = new CreatureClips(display._model.Player, art, System.Array.Empty<string>());
            display._clips.Restart();
            display._model.Player.Evaluate();
            if (art.BodyCentrePivot)
            {
                // A flyer's origin is its body centre: lift it so it hovers just clear of the floor it is shown on.
                display._model.Root.localPosition = Vector3.up * (art.Height * display._scale);
            }

            return root;
        }

        private void Update() => _clips.Tick(Time.deltaTime, 0f, false, false, _scale);

        private void OnDestroy() => _model?.Dispose();
    }
}
