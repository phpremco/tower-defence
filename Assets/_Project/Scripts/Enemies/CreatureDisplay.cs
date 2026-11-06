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
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
            // Shown as framed before the creatures were turned to walk head first: the face to the camera side (-Z).
            display._model.Root.localRotation = Quaternion.identity;
            display._clips = new CreatureClips(display._model.Player, art, System.Array.Empty<string>());
            display._clips.Restart();
            display._model.Player.Evaluate();
            Expose(display._model, art.DisplayExposure);
            if (art.BodyCentrePivot)
            {
                // A flyer's origin is its body centre: lift it so it hovers just clear of the floor it is shown on.
                display._model.Root.localPosition = Vector3.up * (art.Height * display._scale);
            }

            return root;
        }

        // A pale body (the Frost Warden's stone and ice) washes out white under a display stage's lights: its painted colour
        // is toned down on the display's own material copies, the glow left as it is.
        private static void Expose(CreatureModel model, float exposure)
        {
            if (Mathf.Approximately(exposure, 1f))
            {
                return;
            }

            foreach (Material material in model.Materials)
            {
                if (material.HasProperty(BaseColorId))
                {
                    Color colour = material.GetColor(BaseColorId);
                    material.SetColor(BaseColorId, new Color(colour.r * exposure, colour.g * exposure, colour.b * exposure, colour.a));
                }
            }
        }

        private void Update() => _clips.Tick(Time.deltaTime, 0f, false, false, _scale);

        private void OnDestroy() => _model?.Dispose();
    }
}
