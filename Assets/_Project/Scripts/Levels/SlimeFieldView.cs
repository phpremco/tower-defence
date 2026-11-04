using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Draws the slime on the road: a glowing smear per patch that lies on the ground, bubbles a little and dims as
    /// the patch ages. Pooled quads, so a long trail allocates nothing after the first. Reads <see cref="SlimeField"/> only.
    /// </summary>
    public sealed class SlimeFieldView : MonoBehaviour
    {
        private const float Height = 0.04f;
        private const float Brightness = 1.6f;
        private const float Spread = 2.6f;
        private const float Wobble = 0.08f;
        private const float WobbleSpeed = 2.5f;
        private const float BubbleRate = 1.5f;

        private readonly List<Transform> _quads = new List<Transform>(128);
        private readonly List<Material> _materials = new List<Material>(128);
        private readonly List<float> _phases = new List<float>(128);
        private SlimeField _field;
        private ParticleSystem _bubbles;
        private Material _template;
        private Color _color;
        private bool _colorSet;

        public void Initialize(SlimeField field)
        {
            _field = field;
            _bubbles = ParticleKit.Create(transform, "SlimeBubbles", Vector3.zero, Color.white, 0.6f, 1.2f, 0.03f, 0.07f, 0.15f, 0.4f, -0.05f, 60, 0.3f);
        }

        private void LateUpdate()
        {
            if (_field == null)
            {
                return;
            }

            IReadOnlyList<SlimeField.Patch> patches = _field.Patches;
            while (_quads.Count < patches.Count)
            {
                AddQuad(patches[_quads.Count].Color);
            }

            float time = Time.time;
            for (int i = 0; i < _quads.Count; i++)
            {
                bool shown = i < patches.Count;
                _quads[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                SlimeField.Patch patch = patches[i];
                float fade = 1f - patch.Fade;
                float size = patch.Radius * Spread * (1f + Mathf.Sin(time * WobbleSpeed + _phases[i]) * Wobble);
                _quads[i].position = new Vector3(patch.Position.x, Height, patch.Position.z);
                _quads[i].localScale = new Vector3(size, size * 0.8f, 1f);
                _quads[i].localRotation = Quaternion.Euler(90f, _phases[i] * 57f, 0f);
                GlowDecal.SetColour(_materials[i], patch.Color * (Brightness * fade * fade));
            }

            if (patches.Count > 0)
            {
                SlimeField.Patch fresh = patches[patches.Count - 1];
                if (!_colorSet)
                {
                    _colorSet = true;
                    ParticleSystem.MainModule main = _bubbles.main;
                    Color glow = fresh.Color * 1.4f;
                    glow.a = 1f;
                    main.startColor = glow;
                }

                _bubbles.transform.position = fresh.Position;
            }

            ParticleKit.SetRate(_bubbles, patches.Count > 0 ? BubbleRate * Mathf.Min(8, patches.Count) : 0f);
        }

        private void AddQuad(Color color)
        {
            if (_template == null)
            {
                _template = GlowDecal.RadialMaterial(color);
            }

            GameObject go = GlowDecal.PlacePool(transform, color, 1f, Height);
            var material = new Material(_template);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            _quads.Add(go.transform);
            _materials.Add(material);
            _phases.Add(Random.Range(0f, Mathf.PI * 2f));
        }
    }
}
