using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// New materials on the pipeline's stock lit shader, for looks built in code (placeholder shapes, terrain sheets, mist,
    /// props). The URP default material they used to copy exists in the editor only: a player build returns null for it
    /// (round 50). The lit shader itself is kept in the build by the Boot scene's shader keepers, so it is found by name.
    /// </summary>
    public static class PipelineMaterials
    {
        public const string LitShaderName = "Universal Render Pipeline/Lit";

        // The values in which URP's stock Lit material differs from its shader's defaults, so a fresh material looks exactly
        // like the copies it replaces for every caller that leaves them unset.
        private static readonly Color StockBaseColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color StockSpecColor = Color.white;
        private const float StockParallax = 0.02f;
        private const float StockGlossiness = 0.5f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SpecColorId = Shader.PropertyToID("_SpecColor");
        private static readonly int ParallaxId = Shader.PropertyToID("_Parallax");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        private static readonly int GlossMapScaleId = Shader.PropertyToID("_GlossMapScale");
        private static readonly int GlossyReflectionsId = Shader.PropertyToID("_GlossyReflections");

        private static Shader _lit;

        /// <summary>A fresh material on the stock lit shader, set up like URP's default material.</summary>
        public static Material Lit()
        {
            if (_lit == null)
            {
                _lit = Shader.Find(LitShaderName);
            }

            var material = new Material(_lit);
            material.SetColor(BaseColorId, StockBaseColor);
            material.SetColor(ColorId, StockBaseColor);
            material.SetColor(SpecColorId, StockSpecColor);
            material.SetFloat(ParallaxId, StockParallax);
            material.SetFloat(GlossinessId, StockGlossiness);
            material.SetFloat(GlossMapScaleId, 1f);
            material.SetFloat(GlossyReflectionsId, 1f);
            return material;
        }
    }
}
