using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Round 60 (Remco: "the ship flies in with the doors at the bottom open"): the mothership's clamshell bay doors are
    /// modelled swung open (Tools/Blender/build_mothership2.py, <c>bay()</c>), baked into the one hull mesh. At build time
    /// each door's pieces (the plate, its inner skin and the hazard strip, mesh islands of their own) are lifted off the hull
    /// onto a hinge of their own, so the ship arrives with its bay shut, the doors swing open eased just before the salvo
    /// and close again once the last capsule is clear. The hull keeps its hinge rods. A hull without such doors (another
    /// model, or one whose mesh cannot be read) is left as it is.
    /// </summary>
    public sealed class MothershipDoors
    {
        // The bay in the hull mesh's own space (x across the ship, y along it, z down out of the bay), from bay(): each
        // door hangs from a hinge on a bay wall's lower edge, half the bay's 1.5 m wide, swung 62 degrees out and down.
        private const float HingeX = 0.75f;
        private const float HingeZ = 1.05f;
        private const float BayCentreY = 0.54f;
        private const float BayHalfLength = 1.4f;
        private const float DoorWidth = 0.75f;
        private const float OpenDegrees = 62f;
        // A vertex belongs to a door when it lies on the door's plate: within this of its width and length, and this far
        // either side of its middle (5 cm plate, the inner skin 3 cm under it, the hazard strip 5.4 cm). The hinge rod
        // (5 cm round the hinge line) reaches further back than the tolerance, so it stays on the hull.
        private const float SlabTolerance = 0.02f;
        private const float SlabThickness = 0.045f;
        // The export splits vertices along seams; positions this close (1/2000 m) count as one when finding the pieces.
        private const float WeldScale = 2000f;

        // The schedule on the launch shot's clock: the doors start opening this long before the first release (the bay
        // doors' sound starts at 0.1 s) and take this long; they start closing this long after the last release, when
        // its capsule is metres below the hull, and take this long.
        private const float OpenLead = 1.05f;
        private const float OpenSeconds = 0.9f;
        private const float CloseDelay = 1.6f;
        private const float CloseSeconds = 1.8f;

        private readonly Transform[] _hinges;
        private readonly Quaternion[] _shut;

        private MothershipDoors(Transform[] hinges, Quaternion[] shut)
        {
            _hinges = hinges;
            _shut = shut;
        }

        /// <summary>How many doors were found and hinged (2 on the imported mothership).</summary>
        public int Count => _hinges.Length;

        /// <summary>
        /// Lifts the doors off every skinned mesh of the ship (the near and the far one) and hinges the near mesh's doors;
        /// null when the ship has no readable mesh with doors.
        /// </summary>
        public static MothershipDoors Build(Transform ship)
        {
            SkinnedMeshRenderer[] skins = ship.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer near = null;
            foreach (SkinnedMeshRenderer skin in skins)
            {
                if (Usable(skin) && (near == null || skin.sharedMesh.vertexCount > near.sharedMesh.vertexCount))
                {
                    near = skin;
                }
            }

            if (near == null)
            {
                return null;
            }

            MothershipDoors doors = null;
            foreach (SkinnedMeshRenderer skin in skins)
            {
                if (!Usable(skin))
                {
                    continue;
                }

                List<int>[] doorTriangles = StripDoors(skin);
                if (skin == near && doorTriangles != null)
                {
                    doors = Hinge(skin, doorTriangles);
                }
            }

            return doors;
        }

        /// <summary>0 shut, 1 open as modelled.</summary>
        public void Pose(float open)
        {
            float eased = CutsceneMath.Smooth(Mathf.Clamp01(open));
            for (int i = 0; i < _hinges.Length; i++)
            {
                _hinges[i].localRotation = Quaternion.Slerp(_shut[i], Quaternion.identity, eased);
            }
        }

        /// <summary>
        /// How far open the doors are at <paramref name="time"/> of the launch shot (0 shut, 1 open, before easing): shut
        /// until just before <paramref name="firstRelease"/>, open over the salvo, shut again after <paramref name="lastRelease"/>.
        /// </summary>
        public static float Opening(float time, float firstRelease, float lastRelease)
        {
            float opening = CutsceneMath.Span(time, firstRelease - OpenLead, firstRelease - OpenLead + OpenSeconds);
            float closing = CutsceneMath.Span(time, lastRelease + CloseDelay, lastRelease + CloseDelay + CloseSeconds);
            return opening * (1f - closing);
        }

        /// <summary>Whether the doors are fully open at <paramref name="time"/>, for tests: every release must fall in it.</summary>
        public static bool FullyOpen(float time, float firstRelease, float lastRelease) => Opening(time, firstRelease, lastRelease) >= 1f;

        private static bool Usable(SkinnedMeshRenderer skin) => skin.sharedMesh != null && skin.sharedMesh.isReadable && skin.bones.Length > 0 && skin.sharedMesh.bindposes.Length > 0;

        // Where door <paramref name="side"/> (-1 port, +1 starboard) hangs, its open direction and its face.
        private static Vector3 HingeOf(int side) => new Vector3(side * HingeX, BayCentreY, HingeZ);

        private static Vector3 OpenDirection(int side)
        {
            float a = OpenDegrees * Mathf.Deg2Rad;
            return new Vector3(side * Mathf.Cos(a), 0f, Mathf.Sin(a));
        }

        private static Vector3 FaceOf(int side)
        {
            float a = OpenDegrees * Mathf.Deg2Rad;
            return new Vector3(side * Mathf.Sin(a), 0f, -Mathf.Cos(a));
        }

        // Which door a vertex lies on: -1, +1, or 0 for none.
        private static int DoorOf(Vector3 vertex)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 local = vertex - HingeOf(side);
                float across = Vector3.Dot(local, OpenDirection(side));
                float off = Vector3.Dot(local, FaceOf(side));
                if (across >= -SlabTolerance && across <= DoorWidth + SlabTolerance && Mathf.Abs(off) <= SlabThickness && Mathf.Abs(local.y) <= BayHalfLength + SlabTolerance)
                {
                    return side;
                }
            }

            return 0;
        }

        // Gives the renderer a copy of its mesh without the doors and returns the doors' triangles (vertex indices of the
        // original mesh) per side, port first; null when the mesh has none.
        private static List<int>[] StripDoors(SkinnedMeshRenderer skin)
        {
            Mesh mesh = skin.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] island = Islands(mesh, vertices);
            var sideOf = new Dictionary<int, int>();
            for (int v = 0; v < vertices.Length; v++)
            {
                int door = DoorOf(vertices[v]);
                sideOf[island[v]] = sideOf.TryGetValue(island[v], out int seen) ? (seen == door ? seen : 0) : door;
            }

            var doors = new[] { new List<int>(), new List<int>() };
            var kept = new List<int>[mesh.subMeshCount];
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] triangles = mesh.GetTriangles(sub);
                kept[sub] = new List<int>(triangles.Length);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int door = sideOf[island[triangles[t]]];
                    List<int> into = door == 0 ? kept[sub] : doors[door < 0 ? 0 : 1];
                    into.Add(triangles[t]);
                    into.Add(triangles[t + 1]);
                    into.Add(triangles[t + 2]);
                }
            }

            if (doors[0].Count == 0 && doors[1].Count == 0)
            {
                return null;
            }

            Mesh hull = Object.Instantiate(mesh);
            hull.name = mesh.name + "_BayShut";
            for (int sub = 0; sub < kept.Length; sub++)
            {
                hull.SetTriangles(kept[sub], sub, false);
            }

            skin.sharedMesh = hull;
            return doors;
        }

        // The connected pieces of the mesh: a representative vertex per vertex, joined along triangles and across seams.
        private static int[] Islands(Mesh mesh, Vector3[] vertices)
        {
            var parent = new int[vertices.Length];
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = i;
            }

            var welded = new Dictionary<Vector3Int, int>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3Int key = Vector3Int.RoundToInt(vertices[i] * WeldScale);
                if (welded.TryGetValue(key, out int other))
                {
                    Join(parent, i, other);
                }
                else
                {
                    welded[key] = i;
                }
            }

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] triangles = mesh.GetTriangles(sub);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Join(parent, triangles[t], triangles[t + 1]);
                    Join(parent, triangles[t], triangles[t + 2]);
                }
            }

            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = Root(parent, i);
            }

            return parent;
        }

        private static int Root(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        private static void Join(int[] parent, int a, int b)
        {
            a = Root(parent, a);
            b = Root(parent, b);
            if (a != b)
            {
                parent[a] = b;
            }
        }

        // The doors as meshes of their own on hinges in the hull mesh's space (the Root bone's bind pose), wearing the hull's
        // material; shut is the turn about the hinge that lays each door flat across the bay, its edge meeting the other's.
        private static MothershipDoors Hinge(SkinnedMeshRenderer skin, List<int>[] doorTriangles)
        {
            Matrix4x4 bind = skin.sharedMesh.bindposes[0];
            var frame = new GameObject("BayDoors").transform;
            frame.SetParent(skin.bones[0], false);
            frame.localPosition = bind.GetColumn(3);
            frame.localRotation = bind.rotation;
            frame.localScale = bind.lossyScale;

            var hinges = new List<Transform>();
            var shut = new List<Quaternion>();
            for (int d = 0; d < doorTriangles.Length; d++)
            {
                if (doorTriangles[d].Count == 0)
                {
                    continue;
                }

                int side = d == 0 ? -1 : 1;
                var hinge = new GameObject(side < 0 ? "Hinge_Port" : "Hinge_Starboard").transform;
                hinge.SetParent(frame, false);
                hinge.localPosition = HingeOf(side);
                var door = new GameObject("Door");
                door.transform.SetParent(hinge, false);
                door.AddComponent<MeshFilter>().sharedMesh = DoorMesh(skin.sharedMesh, doorTriangles[d], HingeOf(side), side);
                var renderer = door.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = skin.sharedMaterial;
                renderer.shadowCastingMode = skin.shadowCastingMode;
                renderer.receiveShadows = skin.receiveShadows;
                hinges.Add(hinge);
                shut.Add(Quaternion.FromToRotation(OpenDirection(side), new Vector3(-side, 0f, 0f)));
            }

            return new MothershipDoors(hinges.ToArray(), shut.ToArray());
        }

        // The door's triangles with only the vertices they use, placed about the hinge; every vertex channel the hull has.
        private static Mesh DoorMesh(Mesh source, List<int> triangles, Vector3 hinge, int side)
        {
            var remap = new Dictionary<int, int>();
            var used = new List<int>();
            var indices = new int[triangles.Count];
            for (int i = 0; i < triangles.Count; i++)
            {
                if (!remap.TryGetValue(triangles[i], out int index))
                {
                    index = used.Count;
                    remap[triangles[i]] = index;
                    used.Add(triangles[i]);
                }

                indices[i] = index;
            }

            Vector3[] positions = source.vertices;
            var mesh = new Mesh { name = side < 0 ? "BayDoor_Port" : "BayDoor_Starboard" };
            mesh.SetVertices(Pick(positions, used, -hinge));
            mesh.SetNormals(Pick(source.normals, used, Vector3.zero));
            if (source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
            {
                mesh.SetTangents(Pick(source.tangents, used));
            }

            if (source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color))
            {
                mesh.SetColors(Pick(source.colors, used));
            }

            var uvs = new List<Vector4>();
            for (int channel = 0; channel < 4; channel++)
            {
                if (!source.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0 + channel))
                {
                    continue;
                }

                source.GetUVs(channel, uvs);
                mesh.SetUVs(channel, Pick(uvs.ToArray(), used));
            }

            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static List<Vector3> Pick(Vector3[] values, List<int> used, Vector3 offset)
        {
            var picked = new List<Vector3>(used.Count);
            foreach (int index in used)
            {
                picked.Add(values[index] + offset);
            }

            return picked;
        }

        private static List<T> Pick<T>(T[] values, List<int> used)
        {
            var picked = new List<T>(used.Count);
            foreach (int index in used)
            {
                picked.Add(values[index]);
            }

            return picked;
        }
    }
}
