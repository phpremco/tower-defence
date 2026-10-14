using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Paints the ground texture of a level in code: soft patches of colour, bare rock on the steep parts, wet
    /// shores, glowing veins and a cobbled road with lit edges. Everything is derived from the terrain field and the
    /// level's theme, so a new level only needs new data.
    /// </summary>
    public static class TerrainPainter
    {
        private const float TexelsPerUnit = 36f;
        private const int MaxTextureSize = 2048;
        private const float GridLineWidth = 0.014f;
        private const float GridLineLift = 0.1f;
        private const float PadLift = 0.06f;
        private const float StoneScale = 6f;
        private const float EdgeGlowWidth = 0.055f;
        private const float EdgeGlowEmission = 1f;
        private const float VeinInside = 0.12f;
        private const float FadeStartCells = 5.5f;
        private const float FadeEndCells = 8.8f;

        public readonly struct Result
        {
            public readonly Texture2D Albedo;
            public readonly Texture2D Emission;

            public Result(Texture2D albedo, Texture2D emission)
            {
                Albedo = albedo;
                Emission = emission;
            }
        }

        public static Result Paint(LevelDefinition level, VisualTheme theme, TerrainField field, RoadShape road)
        {
            int width = Mathf.Min(MaxTextureSize, Mathf.RoundToInt(field.Width * TexelsPerUnit));
            int height = Mathf.Min(MaxTextureSize, Mathf.RoundToInt(field.Depth * TexelsPerUnit));
            var albedo = new Color32[width * height];
            var emission = new Color32[width * height];

            var context = new PaintContext(level, theme, field, road);
            for (int ty = 0; ty < height; ty++)
            {
                float z = field.MinZ + (ty + 0.5f) / height * field.Depth;
                for (int tx = 0; tx < width; tx++)
                {
                    float x = field.MinX + (tx + 0.5f) / width * field.Width;
                    context.Shade(x, z, out Color color, out Color glow);
                    albedo[ty * width + tx] = color;
                    emission[ty * width + tx] = glow;
                }
            }

            return new Result(ToTexture(albedo, width, height, "TerrainAlbedo"), ToTexture(emission, width, height, "TerrainEmission"));
        }

        private static Texture2D ToTexture(Color32[] pixels, int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }

        // Everything the per-pixel shading needs, resolved once.
        private sealed class PaintContext
        {
            private readonly TerrainField _field;
            private readonly RoadShape _road;
            private readonly GridMap _grid;
            private readonly TerrainMap _terrain;
            private readonly HashSet<Vector2Int> _blocked;
            private readonly HashSet<Vector2Int> _roadCells;
            private readonly Color _ground;
            private readonly Color _groundLight;
            private readonly Color _rock;
            private readonly Color _accent;
            private readonly Color _stone;
            private readonly Color _edgeGlow;
            private readonly Color _lava;
            private readonly Color _sand;
            private readonly Color _water;
            private readonly float _vein;
            private readonly float _halfWidth;
            private readonly float _seed;

            public PaintContext(LevelDefinition level, VisualTheme theme, TerrainField field, RoadShape road)
            {
                _field = field;
                _road = road;
                _grid = level.Grid;
                _terrain = level.Terrain;
                _blocked = new HashSet<Vector2Int>(level.BlockedCells);
                _roadCells = new HashSet<Vector2Int>(LevelValidator.RoadCells(level));
                _ground = theme.GroundColor;
                _groundLight = Color.Lerp(theme.GroundColor, Color.white, 0.18f);
                _rock = theme.RockColor;
                _accent = theme.AccentColor;
                _stone = Color.Lerp(theme.GroundColor, theme.PathColor, 0.4f);
                _edgeGlow = theme.PathColor;
                _lava = theme.LavaColor;
                _sand = theme.SandColor;
                _water = theme.WaterColor;
                _vein = theme.VeinStrength;
                _halfWidth = TerrainField.RoadHalfWidthCells * field.CellSize;
                _seed = Mathf.Abs(level.name.GetHashCode() % 997);
            }

            public void Shade(float x, float z, out Color color, out Color glow)
            {
                float large = TerrainNoise.Fbm(x * 0.2f, z * 0.2f, 3, _seed);
                float medium = TerrainNoise.Fbm(x * 1.1f + 17f, z * 1.1f, 3, _seed);
                float grain = Mathf.PerlinNoise(x * 7.3f + _seed, z * 7.3f);

                color = _ground * (0.95f + 0.6f * large);
                color = Color.Lerp(color, _groundLight, TerrainNoise.SmoothStep(0.55f, 0.78f, medium) * 0.55f);
                color *= 0.9f + 0.2f * grain;
                glow = Color.black;

                float outside = _field.OutsideAt(x, z);
                float slope = _field.SlopeAt(x, z);
                float plateau = _field.PlateauAt(x, z);
                float hills = TerrainNoise.SmoothStep(1.2f, 5f, outside);

                // Bare rock where it is steep and on the far hills; the top of a plateau is lighter.
                float rockiness = Mathf.Clamp01(TerrainNoise.SmoothStep(0.8f, 1.7f, slope) * 0.85f + hills * 0.7f + (medium - 0.5f) * 0.4f * hills);
                color = Color.Lerp(color, _rock * (0.65f + 0.7f * medium), rockiness);
                // Raised ground must read as raised from straight above: a dark cliff face and a paler, rockier top.
                float cliff = plateau * (1f - plateau) * 4f;
                color *= 1f - cliff * 0.5f;
                color = Color.Lerp(color, _rock * 1.3f, TerrainNoise.SmoothStep(0.7f, 0.97f, plateau) * 0.45f);

                ShadeLiquids(x, z, medium, ref color, ref glow);
                ShadeVeins(x, z, outside, ref color, ref glow);
                ShadeBuildPads(x, z, ref color);
                ShadeBlocked(x, z, medium, ref color);
                ShadeRoad(x, z, medium, ref color, ref glow);

                float fade = 1f - TerrainNoise.SmoothStep(FadeStartCells, FadeEndCells, outside);
                color *= fade;
                glow *= fade;
                color.a = 1f;
                glow.a = 1f;
            }

            private void ShadeLiquids(float x, float z, float noise, ref Color color, ref Color glow)
            {
                float water = _field.WaterAt(x, z);
                float lava = _field.LavaAt(x, z);
                float sand = _field.SandAt(x, z);

                // Shores: wet and dark around water, scorched and glowing around lava, pale and dry around sand.
                float waterEdge = TerrainNoise.SmoothStep(0.02f, 0.3f, water);
                color = Color.Lerp(color, color * 0.5f + _water * 0.2f, waterEdge);

                // Deeper water is darker; a pale band of foam marks where the ground meets the surface.
                color = Color.Lerp(color, _water * 0.6f, TerrainNoise.SmoothStep(0.35f, 0.95f, water));
                float foam = 1f - Mathf.Clamp01(Mathf.Abs(water - 0.2f) / 0.07f);
                color = Color.Lerp(color, Color.Lerp(_water, Color.white, 0.7f), foam * (0.5f + 0.4f * noise));

                float lavaEdge = TerrainNoise.SmoothStep(0.02f, 0.5f, lava);
                color = Color.Lerp(color, _rock * 0.35f, lavaEdge);
                float heat = lavaEdge * (1f - TerrainNoise.SmoothStep(0.55f, 0.95f, lava));
                glow += _lava * heat * (0.35f + 0.5f * noise);

                float sandEdge = TerrainNoise.SmoothStep(0.02f, 0.5f, sand);
                color = Color.Lerp(color, _sand * (0.75f + 0.4f * noise), sandEdge);
            }

            // Thin glowing cracks and roots in the ground: strong far from the field, a hint inside it.
            private void ShadeVeins(float x, float z, float outside, ref Color color, ref Color glow)
            {
                if (_vein <= 0f)
                {
                    return;
                }

                float ridge = TerrainNoise.Ridge(x * 0.85f, z * 0.85f, _seed + 5f);
                float vein = TerrainNoise.SmoothStep(0.955f, 0.995f, ridge);
                float strength = Mathf.Lerp(VeinInside, 1f, TerrainNoise.SmoothStep(0.5f, 3f, outside)) * _vein;
                vein *= strength * (1f - _field.RoadAt(x, z));
                color = Color.Lerp(color, _accent * 0.6f, vein * 0.5f);
                glow += _accent * vein * 1.3f;
            }

            // Faint cell edges and a slightly lighter pad show where a tower may be placed.
            private void ShadeBuildPads(float x, float z, ref Color color)
            {
                Vector2Int cell = _grid.WorldToCell(new Vector3(x, 0f, z));
                if (!_grid.Contains(cell) || _roadCells.Contains(cell) || _blocked.Contains(cell) || _terrain.IsLiquid(cell))
                {
                    return;
                }

                Vector3 centre = _grid.CellToWorld(cell);
                float half = _grid.CellSize * 0.5f;
                float edge = Mathf.Min(half - Mathf.Abs(x - centre.x), half - Mathf.Abs(z - centre.z));
                color *= 1f + PadLift;
                if (edge < GridLineWidth)
                {
                    color *= 1f + GridLineLift;
                }
            }

            private void ShadeBlocked(float x, float z, float noise, ref Color color)
            {
                Vector2Int cell = _grid.WorldToCell(new Vector3(x, 0f, z));
                if (_blocked.Contains(cell))
                {
                    Vector3 centre = _grid.CellToWorld(cell);
                    float distance = Mathf.Sqrt((x - centre.x) * (x - centre.x) + (z - centre.z) * (z - centre.z));
                    float patch = 1f - TerrainNoise.SmoothStep(0.25f, 0.6f, distance + (noise - 0.5f) * 0.3f);
                    color = Color.Lerp(color, _rock * 0.85f, patch * 0.4f);
                }
            }

            // A cobbled road: irregular stones with dark mortar, worn edges and a lit rim so the way is readable.
            private void ShadeRoad(float x, float z, float noise, ref Color color, ref Color glow)
            {
                float distance = _road.DistanceTo(x, z);
                float wobble = (Mathf.PerlinNoise(x * 2.4f + _seed, z * 2.4f) - 0.5f) * 0.1f * _field.CellSize;
                float half = _halfWidth + wobble;

                float shoulder = 1f - TerrainNoise.SmoothStep(half, half + _field.CellSize * 0.22f, distance);
                if (shoulder <= 0f)
                {
                    return;
                }

                // Trodden earth that fades into the surrounding ground.
                color = Color.Lerp(color, _stone * 0.55f * (0.8f + 0.4f * noise), shoulder * 0.7f);

                float body = 1f - TerrainNoise.SmoothStep(half - 0.02f, half + 0.025f, distance);
                if (body <= 0f)
                {
                    return;
                }

                CobbleAt(x, z, out float mortar, out float tone);
                Color stone = _stone * (0.7f + 0.55f * tone) * (0.9f + 0.2f * noise);
                Color road = Color.Lerp(stone, stone * 0.35f, mortar);
                color = Color.Lerp(color, road, body);

                float rim = 1f - Mathf.Clamp01(Mathf.Abs(distance - (half - EdgeGlowWidth * 0.5f)) / EdgeGlowWidth);
                glow += _edgeGlow * rim * rim * EdgeGlowEmission;
                color = Color.Lerp(color, _edgeGlow * 0.8f, rim * rim * 0.5f);
            }

            // Jittered-grid cells: the distance to the nearest and second nearest centre gives stone and mortar.
            private static void CobbleAt(float x, float z, out float mortar, out float tone)
            {
                float u = x * StoneScale;
                float v = z * StoneScale;
                int cx = Mathf.FloorToInt(u);
                int cz = Mathf.FloorToInt(v);
                float best = float.MaxValue;
                float second = float.MaxValue;
                int bestX = 0;
                int bestZ = 0;

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx;
                        int nz = cz + dz;
                        float px = nx + 0.2f + 0.6f * TerrainNoise.Hash(nx, nz, 1);
                        float pz = nz + 0.2f + 0.6f * TerrainNoise.Hash(nx, nz, 2);
                        float d = (u - px) * (u - px) + (v - pz) * (v - pz);
                        if (d < best)
                        {
                            second = best;
                            best = d;
                            bestX = nx;
                            bestZ = nz;
                        }
                        else if (d < second)
                        {
                            second = d;
                        }
                    }
                }

                float gap = Mathf.Sqrt(second) - Mathf.Sqrt(best);
                mortar = 1f - TerrainNoise.SmoothStep(0.03f, 0.16f, gap);
                tone = TerrainNoise.Hash(bestX, bestZ, 3);
            }
        }
    }
}
