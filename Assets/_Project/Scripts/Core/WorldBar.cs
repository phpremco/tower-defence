using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A thin health line (and optional armour line) over a creature, tower or hero, on a dark plate: the Blender bar
    /// pieces (README round 25b) turned to the fixed camera. The lines are cut from the right in the shader (UV2.x
    /// against <see cref="MarkerArt.Fill"/>), so the rounded ends stay round and nothing is rescaled. Updated only when
    /// the value changes; hidden at full health by its owner.
    /// </summary>
    public sealed class WorldBar
    {
        /// <summary>Size of the painted line at scale 1 (README: the 2026-11-01 night values).</summary>
        public const float MeshWidth = 0.85f;
        public const float MeshHeight = 0.085f;

        private static readonly Color PlateTint = new Color(0.02f, 0.02f, 0.04f, 0.32f);

        private readonly Transform _root;
        private readonly MeshRenderer _plate;
        private readonly MeshRenderer _fill;
        private readonly MeshRenderer _armour;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _shownFill = -1f;
        private float _shownArmour = -1f;

        private WorldBar(Transform root, MeshRenderer plate, MeshRenderer fill, MeshRenderer armour)
        {
            _root = root;
            _plate = plate;
            _fill = fill;
            _armour = armour;
        }

        public Transform Root => _root;

        /// <summary>
        /// A bar under <paramref name="parent"/> at <paramref name="localPosition"/> (the health line's centre), as wide
        /// and tall as given; null when the art has no bar pieces (the owner keeps its fallback).
        /// </summary>
        public static WorldBar Create(MarkerArt art, Transform parent, Vector3 localPosition, float width, float height, Color fill, Color armour, bool withArmour)
        {
            if (art == null || !art.Has(MarkerKind.BarFill) || !art.Has(MarkerKind.BarPlate))
            {
                return null;
            }

            var root = new GameObject("HealthBar").transform;
            root.SetParent(parent, false);
            root.localPosition = localPosition;
            root.localScale = new Vector3(width / MeshWidth, height / MeshHeight, 1f);
            Camera camera = Camera.main;
            root.rotation = camera != null ? camera.transform.rotation : Quaternion.identity;
            var bar = new WorldBar(root, art.Place(MarkerKind.BarPlate, root), art.Place(MarkerKind.BarFill, root), withArmour ? art.Place(MarkerKind.BarArmour, root) : null);
            MarkerArt.SetTint(bar._plate, bar._block, PlateTint);
            MarkerArt.SetTint(bar._fill, bar._block, fill);
            if (bar._armour != null)
            {
                MarkerArt.SetTint(bar._armour, bar._block, armour);
            }

            bar.Show(false, false);
            return bar;
        }

        /// <summary>Sets the lines (0..1) and whether the bar shows; a line at zero hides.</summary>
        public void Set(float fraction, float armour, bool visible)
        {
            SetFill(_fill, fraction, ref _shownFill);
            if (_armour != null)
            {
                SetFill(_armour, armour, ref _shownArmour);
            }

            Show(visible, visible && armour > 0f);
        }

        /// <summary>Recolours the health line (a tower's line runs from orange to green with its health).</summary>
        public void SetColour(Color colour) => MarkerArt.SetTint(_fill, _block, colour);

        public void Show(bool visible, bool armour)
        {
            _root.gameObject.SetActive(visible);
            if (_armour != null)
            {
                _armour.gameObject.SetActive(armour);
            }
        }

        private void SetFill(MeshRenderer renderer, float value, ref float shown)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(value, shown))
            {
                return;
            }

            shown = value;
            renderer.GetPropertyBlock(_block);
            // A line at zero would still draw its first texel column: cut it just below the start.
            _block.SetFloat(MarkerArt.Fill, value > 0f ? value : -1f);
            renderer.SetPropertyBlock(_block);
        }
    }
}
