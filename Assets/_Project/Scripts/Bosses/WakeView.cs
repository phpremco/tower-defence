using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// What shows of a boss hidden under a liquid (Docs/BOSSES.md): a glow under the surface, a rising stream of bubbles
    /// and, with the round-29 art, the liquid's own wake sheet lying on the surface over it (water foam, a lava crust that
    /// splits and glows, bog scum with gas, a quicksand funnel, ice slush), its top towards where it swims. The sheets it
    /// leaves behind play out where they were laid, so the wake trails it. Off while it is not under.
    /// </summary>
    public sealed class WakeView : MonoBehaviour
    {
        private const float GlowRadius = 1.6f;
        private const float BubbleRate = 40f;
        private const float BubbleSpread = 0.9f;
        private const float SurfaceLift = 0.04f;

        private ParticleSystem _bubbles;
        private GameObject _glow;
        private readonly Dictionary<TerrainKind, FlipbookFx> _sheets = new Dictionary<TerrainKind, FlipbookFx>();
        private FlipbookFx _sheet;
        private float _size = 3f;
        private bool _on;

        public static WakeView Create(Transform parent, Color glow, Color bubbles)
        {
            var go = new GameObject("Wake");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<WakeView>();
            view._glow = GlowDecal.PlacePool(go.transform, glow, GlowRadius, 0.05f);
            view._bubbles = ParticleKit.Create(go.transform, "Bubbles", Vector3.up * 0.02f, bubbles, 0.5f, 1.1f, 0.05f, 0.16f, 0.4f, 1.1f, -0.3f, 120, BubbleSpread);
            view.Show(false, Vector3.zero);
            return view;
        }

        /// <summary>The liquid it is under next, and the sheet's size across.</summary>
        public void SetKind(TerrainKind kind, float size)
        {
            _size = size;
            FlipbookFx next = SheetFor(kind);
            if (next == _sheet)
            {
                return;
            }

            _sheet?.Loop(false, _size);
            _sheet = next;
            _sheet?.Loop(_on, _size);
        }

        public void Show(bool on, Vector3 at)
        {
            _on = on;
            transform.position = at;
            _glow.SetActive(on);
            ParticleKit.SetRate(_bubbles, on ? BubbleRate : 0f);
            _sheet?.Loop(on, _size);
        }

        public void Follow(Vector3 at) => transform.position = at;

        /// <summary>Turns the next sheets so their top points the way it swims.</summary>
        public void Face(Vector3 heading)
        {
            heading.y = 0f;
            if (_sheet == null || heading.sqrMagnitude < 0.0001f)
            {
                return;
            }

            ParticleSystem.MainModule main = _sheet.System.main;
            main.startRotation = Mathf.Atan2(heading.x, heading.z);
        }

        /// <summary>Recolours the glow under the surface (pink sand, orange lava, pale ice).</summary>
        public void SetGlow(Color glow)
        {
            if (_glow.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = GlowDecal.RadialMaterial(glow);
            }
        }

        private FlipbookFx SheetFor(TerrainKind kind)
        {
            if (_sheets.TryGetValue(kind, out FlipbookFx sheet))
            {
                return sheet;
            }

            BossFxArt.Sheet? which = kind switch
            {
                TerrainKind.Water => BossFxArt.Sheet.WakeWater,
                TerrainKind.Lava => BossFxArt.Sheet.WakeLava,
                TerrainKind.Bog => BossFxArt.Sheet.WakeBog,
                TerrainKind.Quicksand => BossFxArt.Sheet.WakeQuicksand,
                TerrainKind.Ice => BossFxArt.Sheet.WakeIce,
                _ => null,
            };
            sheet = which.HasValue ? FlipbookFx.Create(transform, "Wake" + kind, which.Value, true) : null;
            if (sheet != null)
            {
                sheet.System.transform.localPosition = Vector3.up * SurfaceLift;
            }

            _sheets[kind] = sheet;
            return sheet;
        }
    }
}
