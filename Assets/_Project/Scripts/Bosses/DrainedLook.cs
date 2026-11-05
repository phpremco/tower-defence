using System.Collections.Generic;
using Game.Core;
using Game.Towers;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// A gate whose gun a boss drains wears the drained emission maps (README round 29: lit texels at 14 %, an uneven
    /// flicker, a faint violet residue) until it gets its gun back: each renderer whose emission map has a drained twin
    /// gets it through a property block, and loses the override again on release. The shared materials never change.
    /// </summary>
    public static class DrainedLook
    {
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        private static readonly List<Renderer> Renderers = new List<Renderer>(32);

        public static void Apply(Tower gate, bool drained)
        {
            BossFxArt art = BossFxArt.Active;
            if (gate == null || art == null)
            {
                return;
            }

            gate.GetComponentsInChildren(true, Renderers);
            foreach (Renderer renderer in Renderers)
            {
                Material shared = renderer.sharedMaterial;
                if (shared == null || !shared.HasProperty(EmissionMapId))
                {
                    continue;
                }

                Texture twin = art.DrainedFor(shared.GetTexture(EmissionMapId));
                if (twin == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(Block);
                if (drained)
                {
                    Block.SetTexture(EmissionMapId, twin);
                }
                else
                {
                    Block.SetTexture(EmissionMapId, shared.GetTexture(EmissionMapId));
                }

                renderer.SetPropertyBlock(Block);
            }

            Renderers.Clear();
        }
    }
}
