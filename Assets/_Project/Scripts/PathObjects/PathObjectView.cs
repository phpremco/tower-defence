using Game.Core;
using UnityEngine;

namespace Game.PathObjects
{
    /// <summary>Placeholder look of a path object, chosen by kind: a dark disc with a red light, a see-through field or a glowing ball.</summary>
    [RequireComponent(typeof(PathObject))]
    public sealed class PathObjectView : MonoBehaviour
    {
        private const float MineEmission = 1.5f;
        private const float BallEmission = 2.5f;
        private const float FieldAlpha = 0.22f;
        private const float FieldEmission = 1.2f;
        private const float PulseSpeed = 3f;
        private const float MineLightSize = 0.16f;
        private static readonly Color MineBase = new Color(0.12f, 0.13f, 0.16f);

        private Transform _light;
        private PathObject _object;

        public void Initialize(PathObject pathObject)
        {
            _object = pathObject;
            PathObjectDefinition definition = pathObject.Definition;
            switch (definition.Kind)
            {
                case PathObjectKind.Mine:
                    PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, MineBase, transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.5f, 0.05f, 0.5f));
                    _light = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, definition.Color, transform, new Vector3(0f, 0.16f, 0f), Vector3.one * MineLightSize, MineEmission).transform;
                    break;
                case PathObjectKind.SlowField:
                    GameObject field = PlaceholderVisuals.CreatePrimitive(
                        PrimitiveType.Cylinder, definition.Color, transform, new Vector3(0f, 0.05f, 0f), new Vector3(definition.Radius * 2f, 0.01f, definition.Radius * 2f));
                    field.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(definition.Color, FieldAlpha, FieldEmission);
                    _light = field.transform;
                    break;
                default:
                    PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, definition.Color, transform, new Vector3(0f, 0.35f, 0f), Vector3.one * Mathf.Max(0.5f, definition.Radius * 0.8f), BallEmission);
                    break;
            }
        }

        // The light of a mine and the surface of a field breathe slowly, so placed objects read as active.
        private void Update()
        {
            if (_light == null || _object == null)
            {
                return;
            }

            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * PulseSpeed);
            if (_object.Definition.Kind == PathObjectKind.Mine)
            {
                _light.localScale = Vector3.one * MineLightSize * pulse;
            }
            else
            {
                float diameter = _object.Definition.Radius * 2f * pulse;
                _light.localScale = new Vector3(diameter, 0.01f, diameter);
            }
        }
    }
}
