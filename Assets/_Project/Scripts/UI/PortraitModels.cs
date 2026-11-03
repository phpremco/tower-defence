using Game.Core;
using Game.Hero;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The 3D busts the dialogue box shows, built in code for <see cref="ModelIconRenderer"/> with the middle of the
    /// bust at the origin, facing +z. Mercer is the hero figure itself; Dr. Voss is a scientist in a lab coat over a
    /// tech suit with a lit visor; ORA is a projected core in orbit rings; the company is a lit badge; Pioneer-1 is an
    /// old probe with a dish. Every surface carries the speaker's colour in its rim light.
    /// </summary>
    public static class PortraitModels
    {
        // Submesh slots of the scientist.
        private const int Coat = 0;
        private const int Suit = 1;
        private const int Skin = 2;
        private const int Hair = 3;
        private const int Glow = 4;
        private const int Metal = 5;
        private const int ScientistSlots = 6;

        // Submesh slots of the machines: hull, light and dark detail.
        private const int Hull = 0;
        private const int Light = 1;
        private const int Detail = 2;
        private const int MachineSlots = 3;

        private static readonly Color CoatColor = new Color(0.74f, 0.8f, 0.87f);
        private static readonly Color SuitColor = new Color(0.12f, 0.17f, 0.28f);
        private static readonly Color SkinColor = new Color(0.86f, 0.69f, 0.6f);
        private static readonly Color HairColor = new Color(0.3f, 0.15f, 0.11f);
        private static readonly Color VisorColor = new Color(0.35f, 0.9f, 1f);
        private static readonly Color MetalColor = new Color(0.22f, 0.25f, 0.3f);
        private static readonly Color ProbeHull = new Color(0.62f, 0.6f, 0.52f);
        private static readonly Color EmblemPlate = new Color(0.16f, 0.2f, 0.28f);

        // The bust is modelled from the base of the neck; the picture centres a little above it, on the chin.
        private const float ScientistCentre = 0.09f;

        // The probe is wider than a head; it shrinks so the wings stay nearly in the frame.
        private const float ProbeScale = 0.82f;
        private const float SuitSeamGlow = 0.5f;
        private const float VisorEmission = 2.4f;
        private const float HologramEmission = 3.2f;
        private const float RimStrength = 1.1f;
        private const float HologramShellAlpha = 0.25f;
        private const float HologramShellGlow = 0.9f;

        // Where the hero bust is centred: a little above the neck joint the head turns on, so helmet and pauldrons fit.
        private const float HeroBustRise = 0.06f;
        private const float HeroBustTurn = -14f;

        /// <summary>Builds the bust under <paramref name="stage"/>. The hero needs its definition; without one the frame stays empty.</summary>
        public static GameObject Build(Transform stage, PortraitModel model, Color role, HeroDefinition hero)
        {
            switch (model)
            {
                case PortraitModel.Hero:
                    return HeroBust(stage, hero, role);
                case PortraitModel.Scientist:
                    return Scientist(stage, role);
                case PortraitModel.Hologram:
                    return Hologram(stage, role);
                case PortraitModel.Emblem:
                    return Emblem(stage, role);
                case PortraitModel.Probe:
                    return Probe(stage, role);
                default:
                    return new GameObject("EmptyBust");
            }
        }

        private static GameObject HeroBust(Transform stage, HeroDefinition hero, Color role)
        {
            var root = new GameObject("HeroBust").transform;
            root.SetParent(stage, false);
            if (hero == null)
            {
                return root.gameObject;
            }

            GameObject figure = HeroView.BuildDisplayFigure(root, hero);
            Transform head = figure.transform.Find("Torso/Head");
            float centre = head != null ? root.InverseTransformPoint(head.position).y + HeroBustRise : 1.3f;
            figure.transform.localPosition += Vector3.down * centre;
            root.localRotation = Quaternion.Euler(0f, HeroBustTurn, 0f);
            Rim(root.gameObject, role);
            return root.gameObject;
        }

        // A woman in her forties: a high-collared tech suit under an open lab coat, dark hair pinned in a bun, a slim
        // lit visor over the eyes and an earpiece with a mic. Narrower shoulders and a longer neck than the commander.
        private static GameObject Scientist(Transform stage, Color role)
        {
            var b = new PropMeshBuilder(ScientistSlots);

            // Shoulders and chest under the coat; the coat falls open over the suit.
            b.AddTube(new[] { new Vector3(0f, -0.62f, -0.01f), new Vector3(0f, -0.36f, -0.01f), new Vector3(0f, -0.13f, -0.01f) }, new[] { 0.21f, 0.2f, 0.15f }, 16, Coat, false);
            b.AddBlob(new Vector3(0f, -0.25f, 0.0f), new Vector3(0.21f, 0.17f, 0.13f), 6, 14, 0f, 0, Coat, false);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBlob(new Vector3(0.15f * side, -0.12f, -0.01f), new Vector3(0.095f, 0.06f, 0.1f), 5, 12, 0f, 0, Coat, false);
                b.AddTube(new[] { new Vector3(0.2f * side, -0.14f, -0.01f), new Vector3(0.23f * side, -0.45f, 0.0f) }, new[] { 0.065f, 0.07f }, 12, Coat, false);
            }

            // The suit shows in a V down the front, with a lit seam and two status lights.
            b.AddBlob(new Vector3(0f, -0.24f, 0.08f), new Vector3(0.085f, 0.19f, 0.075f), 5, 12, 0f, 0, Suit, false);
            b.AddBlob(new Vector3(0f, -0.25f, 0.152f), new Vector3(0.01f, 0.12f, 0.01f), 3, 6, 0f, 0, Glow, false);
            b.AddBlob(new Vector3(0.04f, -0.13f, 0.145f), Vector3.one * 0.011f, 3, 6, 0f, 0, Glow, false);
            b.AddBlob(new Vector3(-0.04f, -0.13f, 0.145f), Vector3.one * 0.011f, 3, 6, 0f, 0, Glow, false);

            // Lapels folded back over the coat front.
            for (int side = -1; side <= 1; side += 2)
            {
                var lapel = new[] { new Vector3(0.065f * side, -0.07f, 0.11f), new Vector3(0.1f * side, -0.2f, 0.14f), new Vector3(0.095f * side, -0.38f, 0.14f) };
                b.AddBlade(lapel, new[] { 0.06f, 0.075f, 0.04f }, new Vector3(1f, 0f, -0.35f).normalized, Coat, 2);
            }

            // The coat's stand-up collar and the suit's ringed high collar inside it.
            b.AddTube(new[] { new Vector3(0f, -0.1f, -0.02f), new Vector3(0f, -0.01f, -0.03f) }, new[] { 0.115f, 0.105f }, 14, Coat, false);
            b.AddTube(new[] { new Vector3(0f, -0.08f, 0.01f), new Vector3(0f, 0.05f, 0.015f) }, new[] { 0.078f, 0.064f }, 12, Suit, false);
            b.AddTorus(new Vector3(0f, 0.045f, 0.015f), Vector3.up, 0.065f, 0.008f, 14, 4, Glow);

            // The badge on the coat: a lit card with a metal clip.
            b.AddBox(new Vector3(-0.15f, -0.27f, 0.155f), new Vector3(0.055f, 0.075f, 0.012f), Glow, 18f);
            b.AddBox(new Vector3(-0.152f, -0.225f, 0.16f), new Vector3(0.02f, 0.015f, 0.014f), Metal, 18f);

            // Neck and head: a slim neck, an oval face and a small nose.
            b.AddTube(new[] { new Vector3(0f, -0.02f, 0.01f), new Vector3(0f, 0.09f, 0.02f) }, new[] { 0.052f, 0.05f }, 10, Skin, false);
            // One smooth oval: a separate jaw would crease under the visor and read as a moustache.
            b.AddBlob(new Vector3(0f, 0.2f, 0.025f), new Vector3(0.088f, 0.122f, 0.103f), 8, 16, 0f, 0, Skin, false);
            b.AddBlob(new Vector3(0f, 0.2f, 0.125f), new Vector3(0.016f, 0.028f, 0.02f), 3, 6, 0f, 0, Skin, false);


            // Hair: a cap over the crown and the back, a side-swept fringe, locks over the ears and a bun at the back.
            b.AddBlob(new Vector3(0f, 0.245f, -0.02f), new Vector3(0.1f, 0.11f, 0.118f), 6, 14, 0.008f, 21, Hair, false);
            b.AddBlade(new[] { new Vector3(0.09f, 0.315f, 0.06f), new Vector3(0.02f, 0.31f, 0.1f), new Vector3(-0.07f, 0.27f, 0.105f) }, new[] { 0.035f, 0.05f, 0.03f }, Vector3.up, Hair, 2);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBlade(new[] { new Vector3(0.092f * side, 0.29f, 0.03f), new Vector3(0.102f * side, 0.19f, 0.02f), new Vector3(0.098f * side, 0.085f, 0.0f) }, new[] { 0.07f, 0.08f, 0.065f }, Vector3.forward, Hair, 2);
            }

            b.AddBlob(new Vector3(0f, 0.27f, -0.125f), new Vector3(0.062f, 0.058f, 0.055f), 5, 10, 0.012f, 22, Hair, false);
            b.AddBlob(new Vector3(0f, 0.15f, -0.05f), new Vector3(0.1f, 0.12f, 0.08f), 5, 12, 0.008f, 23, Hair, false);
            b.AddTorus(new Vector3(0f, 0.27f, -0.08f), Vector3.forward, 0.045f, 0.008f, 12, 4, Metal);

            // The visor: a slim lit band over the eyes on a metal frame that runs back to the ears.
            b.AddBlob(new Vector3(0f, 0.222f, 0.12f), new Vector3(0.082f, 0.019f, 0.02f), 3, 12, 0f, 0, Glow, false);
            b.AddTube(new[] { new Vector3(-0.092f, 0.235f, 0.09f), new Vector3(0.092f, 0.235f, 0.09f) }, new[] { 0.006f, 0.006f }, 5, Metal, false);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddTube(new[] { new Vector3(0.09f * side, 0.225f, 0.09f), new Vector3(0.1f * side, 0.215f, 0.0f) }, new[] { 0.007f, 0.006f }, 5, Metal, false);
            }

            // Earpiece with a mic boom along the cheek.
            b.AddBlob(new Vector3(0.105f, 0.205f, 0.0f), new Vector3(0.022f, 0.03f, 0.026f), 3, 8, 0f, 0, Metal, false);
            b.AddBlob(new Vector3(0.122f, 0.205f, 0.0f), Vector3.one * 0.009f, 3, 6, 0f, 0, Glow, false);
            b.AddTube(new[] { new Vector3(0.105f, 0.19f, 0.02f), new Vector3(0.095f, 0.16f, 0.07f), new Vector3(0.075f, 0.15f, 0.095f) }, new[] { 0.005f, 0.005f, 0.004f }, 5, Metal, false);

            var materials = new Material[ScientistSlots];
            // Plain cloth: a quilted skin would turn the coat into armour.
            materials[Coat] = SurfaceMaterials.Create(CoatColor, 0f, 0.25f);
            materials[Suit] = CreatureSkin.Dress(SurfaceMaterials.Create(SuitColor, 0f, 0.45f, 0.2f), CreatureSkin.Kind.Weave, VisorColor * SuitSeamGlow);
            materials[Skin] = SurfaceMaterials.Create(SkinColor, 0f, 0.32f);
            materials[Hair] = SurfaceMaterials.Create(HairColor, 0f, 0.55f);
            materials[Glow] = SurfaceMaterials.Create(VisorColor, VisorEmission, 0.8f);
            materials[Metal] = CreatureSkin.Dress(SurfaceMaterials.Create(MetalColor, 0f, 0.65f, 0.8f), CreatureSkin.Kind.Alloy, Color.black);

            // Ears, so the head reads as a head under the hair and the visor.
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddBlob(new Vector3(0.095f * side, 0.205f, 0.01f), new Vector3(0.016f, 0.026f, 0.018f), 3, 6, 0f, 0, Skin, false);
            }

            GameObject bust = Piece(stage, "ScientistBust", b.ToMesh("Portrait_scientist"), materials);
            bust.transform.localPosition = Vector3.down * ScientistCentre;
            Rim(bust, role);
            return bust;
        }

        // The base AI has no body: a bright core inside a darker shell, three tilted orbit rings with nodes and a
        // halo ring facing the viewer.
        private static GameObject Hologram(Transform stage, Color role)
        {
            var b = new PropMeshBuilder(MachineSlots);
            b.AddBlob(Vector3.zero, Vector3.one * 0.11f, 7, 14, 0f, 0, Light, false);
            b.AddBlob(Vector3.zero, Vector3.one * 0.15f, 6, 14, 0.01f, 4, Hull, false);
            Vector3[] axes = { new Vector3(0.3f, 1f, 0.1f), new Vector3(1f, 0.25f, 0.3f), new Vector3(-0.4f, 0.5f, 1f) };
            float[] radii = { 0.25f, 0.29f, 0.33f };
            for (int i = 0; i < axes.Length; i++)
            {
                Vector3 axis = axes[i].normalized;
                b.AddTorus(Vector3.zero, axis, radii[i], 0.007f, 40, 4, Light);
                Vector3 across = Vector3.Cross(axis, Vector3.forward).normalized;
                b.AddBlob(across * radii[i], Vector3.one * 0.018f, 3, 6, 0f, 0, Light, false);
                b.AddBlob(-across * radii[i], Vector3.one * 0.012f, 3, 6, 0f, 0, Light, false);
            }

            b.AddTorus(Vector3.zero, Vector3.forward, 0.19f, 0.004f, 36, 3, Light);
            b.AddTorus(new Vector3(0f, 0f, 0.02f), Vector3.forward, 0.05f, 0.01f, 20, 4, Detail);

            var materials = new Material[MachineSlots];
            materials[Hull] = PlaceholderVisuals.TransparentMaterialFor(role, HologramShellAlpha, HologramShellGlow);
            materials[Light] = SurfaceMaterials.Create(Color.Lerp(role, Color.white, 0.35f), HologramEmission, 0.9f);
            materials[Detail] = SurfaceMaterials.Create(Color.white, HologramEmission, 0.9f);
            GameObject core = Piece(stage, "HologramBust", b.ToMesh("Portrait_hologram"), materials);
            core.transform.localRotation = Quaternion.Euler(0f, -24f, 0f);
            return core;
        }

        // The company badge: a bevelled hexagonal plate with a lit rim and a rising chevron, seen nearly face on.
        private static GameObject Emblem(Transform stage, Color role)
        {
            var b = new PropMeshBuilder(MachineSlots);
            b.AddTube(new[] { new Vector3(0f, 0f, -0.03f), new Vector3(0f, 0f, 0.03f) }, new[] { 0.27f, 0.27f }, 6, Hull, true);
            b.AddTorus(new Vector3(0f, 0f, 0.035f), Vector3.forward, 0.25f, 0.012f, 6, 4, Light, true);
            b.AddBlade(new[] { new Vector3(-0.15f, -0.08f, 0.04f), new Vector3(0f, 0.08f, 0.04f), new Vector3(0.15f, -0.08f, 0.04f) }, new[] { 0.05f, 0.06f, 0.05f }, new Vector3(0f, 1f, 0f), Light);
            b.AddBlade(new[] { new Vector3(-0.1f, -0.16f, 0.04f), new Vector3(0f, -0.06f, 0.04f), new Vector3(0.1f, -0.16f, 0.04f) }, new[] { 0.03f, 0.04f, 0.03f }, new Vector3(0f, 1f, 0f), Detail);

            var materials = new Material[MachineSlots];
            materials[Hull] = CreatureSkin.Dress(SurfaceMaterials.Create(EmblemPlate, 0f, 0.7f, 0.85f), CreatureSkin.Kind.Alloy, Color.black);
            materials[Light] = SurfaceMaterials.Create(role, VisorEmission, 0.8f);
            materials[Detail] = SurfaceMaterials.Create(Color.Lerp(role, Color.white, 0.5f), VisorEmission, 0.8f);
            GameObject badge = Piece(stage, "EmblemBust", b.ToMesh("Portrait_emblem"), materials);
            badge.transform.localRotation = Quaternion.Euler(0f, -16f, 0f);
            Rim(badge, role);
            return badge;
        }

        // The old probe: a weathered drum with a dish turned to the viewer, a mast with a lit tip and a solar wing.
        private static GameObject Probe(Transform stage, Color role)
        {
            var b = new PropMeshBuilder(MachineSlots);
            // The body: a squat eight-sided drum with a banded middle, below and behind the dish.
            b.AddTube(new[] { new Vector3(0f, -0.3f, -0.08f), new Vector3(0f, -0.08f, -0.08f) }, new[] { 0.12f, 0.11f }, 8, Hull, true);
            b.AddTorus(new Vector3(0f, -0.19f, -0.08f), Vector3.up, 0.12f, 0.012f, 8, 4, Detail, true);
            // The dish faces the viewer: a shallow lens with a rim, three struts and a lit feed horn.
            b.AddBlob(new Vector3(0f, 0.06f, 0.0f), new Vector3(0.21f, 0.21f, 0.04f), 5, 18, 0f, 0, Hull, false);
            b.AddTorus(new Vector3(0f, 0.06f, 0.02f), Vector3.forward, 0.21f, 0.014f, 24, 4, Detail);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2f / 3f + Mathf.PI * 0.5f;
                var rim = new Vector3(Mathf.Cos(angle) * 0.19f, 0.06f + Mathf.Sin(angle) * 0.19f, 0.03f);
                b.AddTube(new[] { rim, new Vector3(0f, 0.06f, 0.17f) }, new[] { 0.006f, 0.005f }, 4, Detail, false);
            }

            b.AddTube(new[] { new Vector3(0f, 0.06f, 0.15f), new Vector3(0f, 0.06f, 0.2f) }, new[] { 0.02f, 0.014f }, 6, Detail, false);
            b.AddBlob(new Vector3(0f, 0.06f, 0.21f), Vector3.one * 0.016f, 3, 6, 0f, 0, Light, false);
            // A mast with a lit tip, and a solar wing either side with a lit strip along the cells.
            b.AddTube(new[] { new Vector3(-0.07f, -0.08f, -0.1f), new Vector3(-0.1f, 0.33f, -0.12f) }, new[] { 0.01f, 0.006f }, 5, Detail, false);
            b.AddBlob(new Vector3(-0.1f, 0.34f, -0.12f), Vector3.one * 0.016f, 3, 6, 0f, 0, Light, false);
            for (int side = -1; side <= 1; side += 2)
            {
                b.AddTube(new[] { new Vector3(0.1f * side, -0.2f, -0.08f), new Vector3(0.17f * side, -0.2f, -0.08f) }, new[] { 0.01f, 0.01f }, 5, Detail, false);
                b.AddBlade(new[] { new Vector3(0.17f * side, -0.2f, -0.08f), new Vector3(0.4f * side, -0.2f, -0.08f) }, new[] { 0.13f, 0.13f }, Vector3.up, Detail, 2);
                b.AddBlade(new[] { new Vector3(0.18f * side, -0.2f, -0.075f), new Vector3(0.39f * side, -0.2f, -0.075f) }, new[] { 0.012f, 0.012f }, Vector3.up, Light);
            }

            var materials = new Material[MachineSlots];
            materials[Hull] = CreatureSkin.Dress(SurfaceMaterials.Create(ProbeHull, 0f, 0.4f, 0.6f), CreatureSkin.Kind.Alloy, Color.black);
            materials[Light] = SurfaceMaterials.Create(role, VisorEmission, 0.8f);
            materials[Detail] = SurfaceMaterials.Create(MetalColor, 0f, 0.6f, 0.8f);
            GameObject probe = Piece(stage, "ProbeBust", b.ToMesh("Portrait_probe"), materials);
            probe.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);
            probe.transform.localScale = Vector3.one * ProbeScale;
            Rim(probe, role);
            return probe;
        }

        private static GameObject Piece(Transform stage, string name, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(stage, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        private static void Rim(GameObject root, Color role)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        SurfaceMaterials.SetRim(material, role, RimStrength);
                    }
                }
            }
        }
    }
}
