using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// S-69-CRYS for the space sets: the planet painted in Blender (the campaign's <see cref="PlanetTextureSet"/>) on a
    /// <see cref="PlanetBody"/>, turned so the first
    /// landing site comes round to face <c>faceTowards</c> after <c>startTurn / degreesPerSecond</c> seconds. Without the
    /// painted set it paints a plain globe in code, so the set still works on fresh data.
    /// </summary>
    public static class CrysPlanet
    {
        private const int FallbackWidth = 512;

        /// <param name="faceTowards">Where the landing site ends up facing: back (-z) for a planet seen whole, up for
        /// one seen from low orbit.</param>
        public static PlanetBody Create(Transform root, CampaignCatalog catalog, Vector3 faceTowards, Vector3 centre, float radius, float cloudAlpha, float startTurn, float degreesPerSecond)
        {
            PlanetStyle style = StyleOf(catalog);
            int first = catalog != null ? catalog.FirstNode(0) : -1;
            Vector3 site = first >= 0 ? catalog.Nodes[first].Direction : Vector3.forward;
            Vector2 turn = PlanetCoordinates.TurnToFront(site);
            Quaternion tilt = Quaternion.FromToRotation(Vector3.back, faceTowards) * PlanetCoordinates.Rotation(turn.x + startTurn, turn.y);

            PlanetTextureSet painted = catalog != null ? catalog.PlanetTextures : null;
            if (painted != null)
            {
                Material surface = PlanetMaterials.Surface(painted, ColonySites.Of(catalog), style);
                Texture2D cover = painted.For(ModelQuality.IsLow).Clouds;
                return new PlanetBody(root, "S-69-CRYS", centre, radius, surface, cover, cloudAlpha * painted.CutsceneCloudShare, style, tilt, degreesPerSecond, painted.CloudWisps);
            }

            GlobePainter.Maps maps = GlobePainter.Paint(FallbackWidth, FallbackWidth / 2, new GlobePainter.Region[0]);
            Texture2D clouds = GlobePainter.PaintClouds(FallbackWidth, FallbackWidth / 2, 0f);
            return new PlanetBody(root, "S-69-CRYS", centre, radius, PlanetMaterials.Surface(maps, clouds, style), clouds, cloudAlpha, style, tilt, degreesPerSecond);
        }

        /// <summary>The planet style of the campaign, or the agreed look when there is no catalog.</summary>
        public static PlanetStyle StyleOf(CampaignCatalog catalog) => catalog != null ? catalog.PlanetStyle : PlanetStyle.Crys();

        /// <summary>The space style of the campaign, or the default sky when there is no catalog.</summary>
        public static SpaceStyle SpaceOf(CampaignCatalog catalog) => catalog != null ? catalog.SpaceStyle : new SpaceStyle();
    }
}
