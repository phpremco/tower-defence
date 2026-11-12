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
        // Pieces overlap along the smear, so each is dimmer than a lone pool was.
        private const float Brightness = 0.9f;
        private const float SmearWidth = 0.8f;
        private const int LinkSearch = 8;
        private const float MaxLink = 1.5f;
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
            _bubbles = ParticleKit.UseCoveringCell(ParticleKit.Create(transform, "SlimeBubbles", Vector3.zero, Color.white, 0.6f, 1.2f, 0.03f, 0.07f, 0.15f, 0.4f, -0.05f, 60, 0.3f), MarkerArt.ParticleCell.Mote);
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
                float width = patch.Radius * Spread * (1f + Mathf.Sin(time * WobbleSpeed + _phases[i]) * Wobble);
                // Each piece lies flat on the ground and stretches back to the patch its layer laid before it, so the
                // overlapping soft pieces read as one smear instead of separate pools (Remco 2026-11-05: "schijven die
                // verticaal in de grond steken").
                Vector3 along = Link(patches, i);
                float gap = along.magnitude;
                Vector3 direction = gap > 0.001f ? along / gap : Quaternion.Euler(0f, _phases[i] * Mathf.Rad2Deg, 0f) * Vector3.forward;
                Vector3 centre = patch.Position - along * 0.5f;
                _quads[i].SetPositionAndRotation(new Vector3(centre.x, patch.Position.y + Height, centre.z), Quaternion.LookRotation(direction, Vector3.up));
                _quads[i].localScale = new Vector3(width * SmearWidth, 1f, gap + width);
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

        // The flat step back to the patch the same kind of layer dropped just before this one (zero for the first of a
        // trail): the newest patches of one layer lie a drop spacing apart, another layer's never that close in order.
        private static Vector3 Link(IReadOnlyList<SlimeField.Patch> patches, int index)
        {
            SlimeField.Patch patch = patches[index];
            int oldest = Mathf.Max(0, index - LinkSearch);
            for (int j = index - 1; j >= oldest; j--)
            {
                SlimeField.Patch before = patches[j];
                if (before.Layer != patch.Layer)
                {
                    continue;
                }

                Vector3 step = patch.Position - before.Position;
                step.y = 0f;
                if (step.sqrMagnitude <= MaxLink * MaxLink)
                {
                    return step;
                }
            }

            return Vector3.zero;
        }

        private void AddQuad(Color color)
        {
            if (_template == null)
            {
                _template = GlowDecal.RadialMaterial(color);
            }

            // The pool (flat already, one unit across) sits under a holder the view turns and stretches.
            var holder = new GameObject("SlimePiece").transform;
            holder.SetParent(transform, false);
            GameObject go = GlowDecal.PlacePool(holder, color, 0.5f, 0f);
            var material = new Material(_template);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            _quads.Add(holder);
            _materials.Add(material);
            _phases.Add(Random.Range(0f, Mathf.PI * 2f));
        }
    }
}
