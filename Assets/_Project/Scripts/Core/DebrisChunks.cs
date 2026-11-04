using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// Thrown pieces of a structure that blows: the ten Blender chunks (README round 25b; plates, struts, pipes, hazard
    /// panels, rocks and two glowing hex pieces) at a random uniform size. Without the art a plain cube in the given
    /// colours. The caller pools and animates them.
    /// </summary>
    public static class DebrisChunks
    {
        private const float MinScale = 0.6f;
        private const float MaxScale = 1.6f;
        // The painted chunks are 0.1-0.2 across at scale 1, about the old cubes' size; base debris was about twice that.
        private const float FallbackSize = 0.12f;
        private const float HotFallbackEmission = 2.5f;

        /// <summary>A chunk under <paramref name="parent"/>, inactive; its uniform scale is returned in <paramref name="scale"/>.</summary>
        public static GameObject Create(Transform parent, bool hot, float sizeFactor, Color coldColour, Color hotColour, out Vector3 scale)
        {
            MarkerArt art = MarkerArt.Active;
            float size = Random.Range(MinScale, MaxScale) * sizeFactor;
            GameObject chunk;
            if (art != null && art.DebrisCount > art.HotChunks && art.DebrisMaterial != null)
            {
                int index = hot && art.HotChunks > 0 ? Random.Range(0, art.HotChunks) : Random.Range(art.HotChunks, art.DebrisCount);
                chunk = new GameObject("Debris");
                chunk.transform.SetParent(parent, false);
                chunk.AddComponent<MeshFilter>().sharedMesh = art.Debris(index);
                chunk.AddComponent<MeshRenderer>().sharedMaterial = art.DebrisMaterial;
                scale = Vector3.one * size;
            }
            else
            {
                scale = new Vector3(1f, 0.5f, 0.8f) * (size * FallbackSize);
                chunk = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, hot ? hotColour : coldColour, parent, Vector3.zero, scale, hot ? HotFallbackEmission : 0f);
                chunk.name = "Debris";
            }

            chunk.transform.localScale = scale;
            chunk.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            chunk.SetActive(false);
            return chunk;
        }
    }
}
