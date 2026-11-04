using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The file that holds the baked marker meshes as sub-assets (written by Tools/Generate/Blender Markers), so a
    /// re-run replaces each mesh in place and every reference to it stays valid.
    /// </summary>
    public sealed class MarkerMeshStore : ScriptableObject
    {
    }
}
