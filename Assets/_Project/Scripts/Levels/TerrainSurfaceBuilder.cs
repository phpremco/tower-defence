using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>Turns the terrain field into one smooth ground mesh wearing the painted texture.</summary>
    public static class TerrainSurfaceBuilder
    {
        private const float TextureSmoothness = 0.08f;
        private const float EmissionStrength = 1.5f;

        public static GameObject Build(LevelDefinition level, VisualTheme theme, TerrainField field, RoadShape road, Transform parent)
        {
            var go = new GameObject("Terrain");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildMesh(field);

            // The generator bakes the textures; painting at load time is only the fallback for a level without them.
            bool baked = level.TerrainAlbedo != null && level.TerrainEmission != null;
            TerrainPainter.Result textures = baked
                ? new TerrainPainter.Result(level.TerrainAlbedo, level.TerrainEmission)
                : TerrainPainter.Paint(level, theme, field, road);
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetTexture("_BaseMap", textures.Albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", TextureSmoothness);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", textures.Emission);
            material.SetColor("_EmissionColor", Color.white * EmissionStrength);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return go;
        }

        private static Mesh BuildMesh(TerrainField field)
        {
            int nx = field.Nx;
            int nz = field.Nz;
            var vertices = new Vector3[nx * nz];
            var uvs = new Vector2[vertices.Length];
            for (int iz = 0; iz < nz; iz++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    int i = field.Index(ix, iz);
                    vertices[i] = new Vector3(field.WorldX(ix), field.HeightAtVertex(ix, iz), field.WorldZ(iz));
                    uvs[i] = new Vector2(ix / (float)(nx - 1), iz / (float)(nz - 1));
                }
            }

            var triangles = new int[(nx - 1) * (nz - 1) * 6];
            int t = 0;
            for (int iz = 0; iz < nz - 1; iz++)
            {
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    int a = field.Index(ix, iz);
                    int b = field.Index(ix + 1, iz);
                    int c = field.Index(ix, iz + 1);
                    int d = field.Index(ix + 1, iz + 1);
                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;
                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = d;
                }
            }

            var mesh = new Mesh { name = "Terrain", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
