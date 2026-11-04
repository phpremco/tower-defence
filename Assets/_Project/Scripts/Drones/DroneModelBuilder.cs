using Game.Core;
using UnityEngine;

namespace Game.Drones
{
    /// <summary>
    /// Puts a drone model together under a parent: skinned hull, one rotor object per pivot with a blur disc under
    /// it, the cargo pod, the drill, navigation lights, thruster glow and the muzzle points. The in-level view
    /// animates it; the HUD and the upgrade screen render the same thing into a picture.
    /// </summary>
    public static class DroneModelBuilder
    {
        private const float RotorBlurAlpha = 0.09f;
        private const float RotorBlurHeight = 0.004f;
        private const float NavLightSize = 0.028f;
        private const float NavLightEmission = 5f;
        private const float ThrusterSize = 0.05f;
        private const float ThrusterEmission = 3f;
        private const float SeamGlow = 0.35f;
        private const float AlloyTiling = 2f;
        private static readonly Color HullBase = new Color(0.72f, 0.76f, 0.84f);
        private static readonly Color MetalColor = new Color(0.78f, 0.82f, 0.9f);
        private static readonly Color DarkColor = new Color(0.1f, 0.11f, 0.15f);
        private static readonly Color PortColor = new Color(1f, 0.2f, 0.15f);
        private static readonly Color StarboardColor = new Color(0.2f, 1f, 0.35f);
        private static readonly Color StrobeColor = new Color(1f, 1f, 1f);
        private static readonly Color BeaconColor = new Color(1f, 0.7f, 0.15f);

        public sealed class Rig
        {
            public GameObject Root;
            public Material[] Materials;
            public Transform[] Rotors;
            public Vector3 RotorAxis;
            public Renderer Cargo;
            public Transform Drill;
            public Vector3[] Muzzles;

            /// <summary>Navigation light objects, in the same order as the factory's lights.</summary>
            public GameObject[] Lights;
            public DroneModelFactory.LightKind[] LightKinds;

            /// <summary>One material per thruster, whose emission the view scales with speed.</summary>
            public Material[] ThrusterMaterials;
            public Transform[] Thrusters;

            /// <summary>Where sparks fly from while the medic welds, in model space.</summary>
            public Vector3 Tool;
        }

        /// <summary>
        /// The drone for a menu picture or turntable: the imported model when the drone has one (its rotors listed so the
        /// turntable can spin them), else the procedural model. The cutscene keeps the procedural one through <see cref="Build"/>.
        /// </summary>
        public static Rig BuildDisplay(Transform parent, DroneDefinition definition)
        {
            if (definition.Art == null)
            {
                return Build(parent, definition);
            }

            DroneArtRig art = DroneArtRig.CreateStill(parent, definition.Art);
            return new Rig
            {
                Root = art.Holder.gameObject,
                Materials = new[] { art.Model.Material },
                Rotors = art.Rotors,
                RotorAxis = Vector3.up,
                Muzzles = new Vector3[0],
                Lights = new GameObject[0],
                LightKinds = new DroneModelFactory.LightKind[0],
                ThrusterMaterials = new Material[0],
                Thrusters = new Transform[0],
            };
        }

        public static Rig Build(Transform parent, DroneDefinition definition)
        {
            DroneModelFactory.Parts parts = DroneModelFactory.For((int)definition.Kind);
            Material[] materials = Materials(definition);

            var root = new GameObject("DroneModel");
            root.transform.SetParent(parent, false);
            Part("Hull", root.transform, parts.Hull, materials, Vector3.zero);

            var rotors = new Transform[parts.RotorPivots.Length];
            Quaternion spinFrame = Quaternion.FromToRotation(Vector3.up, parts.RotorAxis);
            for (int i = 0; i < rotors.Length; i++)
            {
                Transform rotor = Part("Rotor", root.transform, parts.Rotor, materials, parts.RotorPivots[i]).transform;
                // The blade mesh lies flat (spinning around +Y); a tail fan spins around its own axis instead.
                rotor.localRotation = spinFrame;
                rotors[i] = rotor;

                // A faint disc under the blades stands in for the motion blur of a rotor at speed.
                GameObject blur = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, definition.Color, root.transform, parts.RotorPivots[i], new Vector3(parts.RotorRadius * 2f, RotorBlurHeight, parts.RotorRadius * 2f));
                blur.name = "RotorBlur";
                blur.transform.localRotation = spinFrame;
                blur.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(Color.Lerp(definition.Color, new Color(0.3f, 0.32f, 0.38f), 0.6f), RotorBlurAlpha, 0f);
            }

            Renderer cargo = null;
            if (parts.Cargo != null)
            {
                cargo = Part("Cargo", root.transform, parts.Cargo, materials, parts.CargoPivot);
            }

            Transform drill = null;
            if (parts.Drill != null)
            {
                drill = Part("Drill", root.transform, parts.Drill, materials, parts.DrillPivot).transform;
            }

            var lights = new GameObject[parts.Lights.Length];
            var kinds = new DroneModelFactory.LightKind[parts.Lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                DroneModelFactory.NavLight light = parts.Lights[i];
                lights[i] = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, LightColor(light.Kind), root.transform, light.Position, Vector3.one * NavLightSize, NavLightEmission);
                lights[i].name = "NavLight";
                kinds[i] = light.Kind;
            }

            var thrusters = new Transform[parts.Thrusters.Length];
            var thrusterMaterials = new Material[parts.Thrusters.Length];
            for (int i = 0; i < thrusters.Length; i++)
            {
                GameObject flame = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, definition.Color, root.transform, parts.Thrusters[i], new Vector3(ThrusterSize, ThrusterSize, ThrusterSize * 1.6f), ThrusterEmission);
                flame.name = "Thruster";
                var renderer = flame.GetComponent<Renderer>();
                // Its own material, so the view can turn the glow up with speed without touching other drones.
                renderer.material = SurfaceMaterials.Create(definition.Color, ThrusterEmission, 0.6f);
                thrusterMaterials[i] = renderer.material;
                thrusters[i] = flame.transform;
            }

            return new Rig
            {
                Root = root,
                Materials = materials,
                Rotors = rotors,
                RotorAxis = parts.RotorAxis,
                Cargo = cargo,
                Drill = drill,
                Muzzles = parts.Muzzles,
                Lights = lights,
                LightKinds = kinds,
                ThrusterMaterials = thrusterMaterials,
                Thrusters = thrusters,
                Tool = parts.Tool,
            };
        }

        public static Color LightColor(DroneModelFactory.LightKind kind)
        {
            switch (kind)
            {
                case DroneModelFactory.LightKind.Port:
                    return PortColor;
                case DroneModelFactory.LightKind.Starboard:
                    return StarboardColor;
                case DroneModelFactory.LightKind.Beacon:
                    return BeaconColor;
                default:
                    return StrobeColor;
            }
        }

        // Drones wear their own colour on the hull, dressed in a skin from the data, and their markings in the
        // accent colour, so the three kinds read apart at a glance and up close look built, not cast.
        private static Material[] Materials(DroneDefinition definition)
        {
            Color color = definition.Color;
            Material hull = CreatureSkin.Dress(SurfaceMaterials.Create(Color.Lerp(HullBase, color, 0.55f), 0f, 0.5f, 0.2f), definition.Skin, color * SeamGlow);
            Material metal = CreatureSkin.Dress(SurfaceMaterials.Create(MetalColor, 0f, 0.55f, 0.3f), CreatureSkin.Kind.Alloy, Color.black);
            Material dark = CreatureSkin.Dress(SurfaceMaterials.Create(DarkColor, 0f, 0.35f, 0.2f), CreatureSkin.Kind.Alloy, Color.black);
            Material accent = CreatureSkin.Dress(SurfaceMaterials.Create(definition.AccentColor, 0f, 0.5f, 0.1f), CreatureSkin.Kind.Plate, Color.black);
            // The alloy lattice is drawn for hero-sized gear; on a drone it goes finer.
            metal.SetTextureScale("_BaseMap", Vector2.one * AlloyTiling);
            dark.SetTextureScale("_BaseMap", Vector2.one * AlloyTiling);
            return new[]
            {
                hull,
                SurfaceMaterials.Create(color, 2.2f, 0.6f),
                metal,
                dark,
                accent,
            };
        }

        private static Renderer Part(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }
    }
}
