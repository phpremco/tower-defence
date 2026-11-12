using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>Small particle systems built in code for view components: bursts of sparks, puffs of smoke and the like.</summary>
    public static class ParticleKit
    {
        private const string AtlasFrameKeyword = "_ATLAS_FRAME";
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int ThirdColorId = Shader.PropertyToID("_ThirdColor");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int AtlasFrameId = Shader.PropertyToID("_AtlasFrame");
        private static readonly int FrameId = Shader.PropertyToID("_Frame");

        private static Material _smoke;
        private static Material _smokeLow;

        /// <summary>
        /// A system that emits nothing on its own; call <see cref="ParticleSystem.Emit(int)"/> for a burst or
        /// <see cref="SetRate"/> for a steady stream. Negative gravity makes particles rise and swell, like smoke.
        /// </summary>
        public static ParticleSystem Create(Transform parent, string name, Vector3 localPosition, Color color, float minLife, float maxLife, float minSize, float maxSize, float minSpeed, float maxSpeed, float gravity, int max, float radius = 0.2f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, gravity < 0f ? 2.5f : 0.2f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            // A glowing soft mote by default (sparks, embers, thrusters); smoke and dust callers redress it covering.
            return UseCell(system, MarkerArt.ParticleCell.Mote);
        }

        /// <summary>
        /// The URP default particle material, only for a renderer the atlas cannot dress (no art registered, as in a bare
        /// EditMode test). It exists in the editor only: a player build returns null, and in this project's game view it drew
        /// nothing (round 49), so every game effect is dressed from the atlas instead.
        /// </summary>
        public static Material FallbackMaterial => GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;

        /// <summary>
        /// Dresses a smoke, dust, steam or mist system in the ragged vapour puff (<see cref="SpriteKit.Puff"/>), drawn covering:
        /// the atlas's soft mote is a small bright point and read as glowing dots at smoke size (round 49). One material per
        /// quality is shared by every smoke; without registered art the system keeps its look.
        /// </summary>
        public static ParticleSystem UseSmoke(ParticleSystem system)
        {
            Material material = SmokeMaterial();
            if (material == null)
            {
                return UseCell(system, MarkerArt.ParticleCell.Mote, null);
            }

            system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
            sheet.enabled = false;
            return system;
        }

        private static Material SmokeMaterial()
        {
            if (MarkerArt.Active == null)
            {
                return null;
            }

            bool low = ModelQuality.IsLow;
            ref Material cached = ref low ? ref _smokeLow : ref _smoke;
            if (cached == null)
            {
                cached = SpriteMaterial(SpriteKit.Puff());
                cached.name = low ? "Particles_Smoke_Low" : "Particles_Smoke";
            }

            return cached;
        }

        /// <summary>
        /// A new covering material (alpha-blended, like the old default) for a system that draws its own white sprite
        /// without the atlas's sheet animation: the sprite plain, or without one the atlas's soft mote as a fixed cell. Built
        /// from the atlas's covering material; the default material only without registered art; null when neither exists.
        /// </summary>
        public static Material SpriteMaterial(Texture2D sprite)
        {
            MarkerArt art = MarkerArt.Active;
            Material source = art != null ? art.ParticleMaterial(true) : FallbackMaterial;
            if (source == null)
            {
                return null;
            }

            var material = new Material(source);
            if (art == null)
            {
                if (sprite != null)
                {
                    material.SetTexture(BaseMapId, sprite);
                }

                return material;
            }

            if (sprite != null)
            {
                // A white sprite carries its shape in alpha; its red channel alone lights it, at the particle's colour.
                material.SetTexture(MainTexId, sprite);
                material.SetColor(CoreColorId, Color.white);
                material.SetColor(GlowColorId, Color.clear);
                material.SetColor(ThirdColorId, Color.clear);
            }
            else
            {
                material.EnableKeyword(AtlasFrameKeyword);
                material.SetFloat(AtlasFrameId, 1f);
                material.SetFloat(FrameId, (float)MarkerArt.ParticleCell.Mote);
            }

            return material;
        }

        /// <summary>
        /// A new additive material that lights a white sprite in <paramref name="colour"/> (its red channel, faded by its
        /// alpha), for glows and soft lines built outside a particle system. Built from the atlas's glow material; the default
        /// material only without registered art (bare EditMode tests).
        /// </summary>
        public static Material AdditiveSprite(Texture2D sprite, Color colour)
        {
            MarkerArt art = MarkerArt.Active;
            if (art == null)
            {
                var plain = new Material(FallbackMaterial);
                plain.SetTexture(BaseMapId, sprite);
                plain.SetColor(BaseColorId, colour);
                return plain;
            }

            var material = new Material(art.ParticleMaterial(false));
            material.SetTexture(MainTexId, sprite);
            material.SetColor(CoreColorId, colour);
            material.SetColor(GlowColorId, Color.clear);
            material.SetColor(ThirdColorId, Color.clear);
            return material;
        }

        /// <summary>
        /// A new covering material that shows a painted picture unlit in its own colours (a backdrop): each channel of the
        /// atlas shader lights its own primary. Built from the atlas's covering material; the default material only without
        /// registered art.
        /// </summary>
        public static Material Picture(Texture2D picture)
        {
            MarkerArt art = MarkerArt.Active;
            if (art == null)
            {
                var plain = new Material(FallbackMaterial);
                plain.SetTexture(BaseMapId, picture);
                return plain;
            }

            var material = new Material(art.ParticleMaterial(true));
            material.SetTexture(MainTexId, picture);
            material.SetColor(CoreColorId, Color.red);
            material.SetColor(GlowColorId, Color.green);
            material.SetColor(ThirdColorId, Color.blue);
            material.SetColor(TintId, Color.white);
            return material;
        }

        /// <summary>
        /// Dresses a system in one cell of the Blender particle atlas (README round 25b): additive for motes, embers, frost and
        /// chrono motes, covering for drips, splats and goo. Without registered art it keeps its material, or takes the
        /// <see cref="FallbackMaterial"/> when it has none.
        /// </summary>
        public static ParticleSystem UseCell(ParticleSystem system, MarkerArt.ParticleCell cell)
        {
            MarkerArt art = MarkerArt.Active;
            return UseCell(system, cell, art != null ? art.Particles(cell) : null);
        }

        /// <summary>Dresses a system in a cell of the atlas drawn with the covering material (smoke from a soft mote).</summary>
        public static ParticleSystem UseCoveringCell(ParticleSystem system, MarkerArt.ParticleCell cell)
        {
            MarkerArt art = MarkerArt.Active;
            return UseCell(system, cell, art != null ? art.ParticleMaterial(true) : null);
        }

        private static ParticleSystem UseCell(ParticleSystem system, MarkerArt.ParticleCell cell, Material material)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            if (material == null)
            {
                if (renderer.sharedMaterial == null)
                {
                    renderer.sharedMaterial = FallbackMaterial;
                }

                return system;
            }

            renderer.sharedMaterial = material;
            ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = MarkerArt.ParticleColumns;
            sheet.numTilesY = MarkerArt.ParticleRows;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            // The grid counts frames from the top-left like the atlas, so the cell index is the frame.
            float frame = ((int)cell + 0.5f) / (MarkerArt.ParticleColumns * MarkerArt.ParticleRows);
            sheet.frameOverTime = new ParticleSystem.MinMaxCurve(frame);
            sheet.cycleCount = 1;
            return system;
        }

        public static void SetRate(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = rate > 0f;
            emission.rateOverTime = rate;
        }
    }
}
