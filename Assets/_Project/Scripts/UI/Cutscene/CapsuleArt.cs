using Game.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// What the cutscene needs from the imported landing capsule (README round 13): its heat shield is its own mesh, so it
    /// gets its own material copy whose glow the falling and the cooling drive, apart from the hull's lights.
    /// </summary>
    public static class CapsuleArt
    {
        private const string ShieldSuffix = "_Shield";
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>Gives the shield meshes a material of their own, dark until the heat lights it, and returns it.</summary>
        public static Material OwnShield(ArtModel capsule)
        {
            var shield = new Material(capsule.Material) { name = capsule.Art.name + ShieldSuffix };
            shield.SetColor(EmissionColorId, Color.black);
            foreach (Renderer renderer in capsule.RenderersEndingWith(ShieldSuffix))
            {
                renderer.sharedMaterial = shield;
            }

            return shield;
        }
    }
}
