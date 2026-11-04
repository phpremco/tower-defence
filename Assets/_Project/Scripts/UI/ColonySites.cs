using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The colony lights of the level sites as a shader layer over a painted planet: every surface site that holds a
    /// level becomes a direction (on the unit sphere, the planet's own frame) with a reach and a warm colour, and
    /// <c>Game/Planet</c> draws a speckled glow round each. The sites come from the campaign (the painted layout puts
    /// them there), so moving a level never needs a map rebake.
    /// </summary>
    public sealed class ColonySites
    {
        /// <summary>The shader's array size (<c>COLONY_SITES</c> in Planet.shader); later sites are left dark.</summary>
        public const int MaxSites = 32;

        // Settlement reach in degrees of arc: the old baked glow's 7 degree land patch times its 0.55 light share.
        private const float ReachDegrees = 3.85f;
        // Warm colony light, a little of the region's colour mixed in so continents differ.
        private const float RegionShare = 0.3f;
        private static readonly Color LightColour = new Color(1f, 0.78f, 0.45f);

        private static readonly int SitesId = Shader.PropertyToID("_ColonySites");
        private static readonly int ColoursId = Shader.PropertyToID("_ColonySiteColors");
        private static readonly int CountId = Shader.PropertyToID("_ColonySiteCount");

        private readonly Vector4[] _sites = new Vector4[MaxSites];
        private readonly Vector4[] _colours = new Vector4[MaxSites];
        private int _count;

        public int Count => _count;

        /// <summary>Every surface site of the campaign that holds a level, up to <see cref="MaxSites"/>.</summary>
        public static ColonySites Of(CampaignCatalog catalog)
        {
            var sites = new ColonySites();
            if (catalog == null)
            {
                return sites;
            }

            float reach = ReachDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < catalog.Nodes.Count && sites._count < MaxSites; i++)
            {
                MapNode node = catalog.Nodes[i];
                if (node.Level == null || catalog.LayerOf(i) != MapLayer.Surface)
                {
                    continue;
                }

                Vector3 direction = node.Direction;
                sites._sites[sites._count] = new Vector4(direction.x, direction.y, direction.z, reach);
                // Linear, as the shader adds it straight to the lit colour.
                Color colour = Color.Lerp(LightColour, catalog.Regions[node.Region].Colour, RegionShare).linear;
                sites._colours[sites._count] = new Vector4(colour.r, colour.g, colour.b, 1f);
                sites._count++;
            }

            return sites;
        }

        /// <summary>Puts the sites on a planet surface material (the arrays are always set at full size, so their length never changes).</summary>
        public void Apply(Material surface)
        {
            surface.SetVectorArray(SitesId, _sites);
            surface.SetVectorArray(ColoursId, _colours);
            surface.SetFloat(CountId, _count);
        }
    }
}
