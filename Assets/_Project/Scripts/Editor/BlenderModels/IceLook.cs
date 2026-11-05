using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The look of ice on a creature (<see cref="BlenderArtEntry.Ice"/>): a tint over the baked albedo (darker and colder,
    /// so the body behind seems to show through), a smoothness for small sharp highlights, a cold rim along the silhouette
    /// (the lit edge of clear ice) and the glow of the cracks. Numbers from game-camera screenshots (round 5 boss look).
    /// </summary>
    public sealed class IceLook
    {
        public Color Tint { get; set; } = new Color(0.32f, 0.52f, 0.72f);
        public float Smoothness { get; set; } = 0.85f;
        public Color Rim { get; set; } = new Color(0.62f, 0.92f, 1f);
        public float RimStrength { get; set; } = 1.5f;
        public float RimPower { get; set; } = 3.2f;
        public Color Glow { get; set; } = new Color(0.45f, 0.85f, 1f);
        public float GlowStrength { get; set; } = 0.35f;

        /// <summary>
        /// Drops the baked albedo and glow maps (keeps the normal and mask maps): the round-28 ice bakes carry warm brown and
        /// orange patches that read as dirt or glowing cards on ice. The ice is then one cold colour shaped by its normals.
        /// </summary>
        public bool FlatColour { get; set; } = true;

        public void Apply(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.SetColor("_BaseColor", Tint);
            material.SetFloat("_Smoothness", Smoothness);
            material.SetColor("_RimColor", Rim);
            material.SetFloat("_RimStrength", RimStrength);
            material.SetFloat("_RimPower", RimPower);
            material.SetColor("_EmissionColor", Glow * GlowStrength);
            if (FlatColour)
            {
                material.SetTexture("_BaseMap", null);
                material.SetTexture("_EmissionMap", null);
            }
        }
    }
}
