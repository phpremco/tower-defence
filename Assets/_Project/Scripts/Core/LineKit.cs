using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The look of code-built lines and trails (boss and hero beams, bolts, sword, thruster and shot trails): one additive
    /// material from the Blender particle atlas wearing the beam profile (<see cref="SpriteKit.Beam"/>), a white-hot core
    /// inside a soft glowing edge, coloured by the renderer's own colours. It replaces the URP default particle material,
    /// which drew lines as flat strips in this project and is null in a player build (round 50).
    /// </summary>
    public static class LineKit
    {
        // HDR strengths of the core and the glow, so the bloom catches the core and the edge stays soft.
        private const float CoreStrength = 2.5f;
        private const float GlowStrength = 1.2f;
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");

        private static Material _beam;
        private static Material _beamLow;

        /// <summary>The shared beam material of the active quality; the default material only without registered art.</summary>
        public static Material Beam()
        {
            if (MarkerArt.Active == null)
            {
                return ParticleKit.FallbackMaterial;
            }

            bool low = ModelQuality.IsLow;
            ref Material cached = ref low ? ref _beamLow : ref _beam;
            if (cached == null)
            {
                cached = ParticleKit.AdditiveSprite(SpriteKit.Beam(), Color.white * CoreStrength);
                cached.SetColor(GlowColorId, Color.white * GlowStrength);
                cached.name = low ? "Lines_Beam_Low" : "Lines_Beam";
            }

            return cached;
        }

        /// <summary>Dresses a line in the beam, its profile stretched once along the whole line.</summary>
        public static LineRenderer UseBeam(LineRenderer line)
        {
            line.sharedMaterial = Beam();
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        /// <summary>Dresses a trail in the beam, its profile stretched once from head to tail.</summary>
        public static TrailRenderer UseBeam(TrailRenderer trail)
        {
            trail.sharedMaterial = Beam();
            trail.textureMode = LineTextureMode.Stretch;
            return trail;
        }
    }
}
