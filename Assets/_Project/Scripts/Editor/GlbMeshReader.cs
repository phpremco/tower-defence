using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>
    /// Minimal reader for single-mesh .glb files with positions and triangle indices, which is what many AI model
    /// generators export. Unity does not import .glb itself, and this avoids a package for untextured models.
    /// Models with textures or several materials need a real glTF importer instead.
    /// </summary>
    public static class GlbMeshReader
    {
        private const uint GlbMagic = 0x46546C67;
        private const uint JsonChunk = 0x4E4F534A;
        private const uint BinChunk = 0x004E4942;
        private const int ComponentUnsignedShort = 5123;
        private const int ComponentUnsignedInt = 5125;
        private const int HeaderBytes = 12;
        private const int ChunkHeaderBytes = 8;

        [Serializable]
        private class Gltf
        {
            public Accessor[] accessors;
            public BufferView[] bufferViews;
            public GltfMesh[] meshes;
        }

        [Serializable]
        private class Accessor
        {
            public int bufferView;
            public int byteOffset;
            public int componentType;
            public int count;
        }

        [Serializable]
        private class BufferView
        {
            public int byteOffset;
            public int byteLength;
        }

        [Serializable]
        private class GltfMesh
        {
            public Primitive[] primitives;
        }

        [Serializable]
        private class Primitive
        {
            public Attributes attributes;
            public int indices = -1;
        }

        [Serializable]
        private class Attributes
        {
            public int POSITION = -1;
        }

        /// <summary>Reads the first mesh of the file. Returns null and logs a warning when the file is not supported.</summary>
        public static Mesh Read(string path, float scale = 1f)
        {
            byte[] data = File.ReadAllBytes(path);
            if (data.Length < HeaderBytes || BitConverter.ToUInt32(data, 0) != GlbMagic)
            {
                Debug.LogWarning($"'{path}' is not a binary glTF (.glb) file.");
                return null;
            }

            string json = null;
            int binStart = -1;
            int offset = HeaderBytes;
            while (offset + ChunkHeaderBytes <= data.Length)
            {
                int length = (int)BitConverter.ToUInt32(data, offset);
                uint type = BitConverter.ToUInt32(data, offset + 4);
                int start = offset + ChunkHeaderBytes;
                if (type == JsonChunk)
                {
                    json = Encoding.UTF8.GetString(data, start, length);
                }
                else if (type == BinChunk)
                {
                    binStart = start;
                }

                offset = start + length;
            }

            if (json == null || binStart < 0)
            {
                Debug.LogWarning($"'{path}' has no JSON or binary chunk.");
                return null;
            }

            Gltf gltf = JsonUtility.FromJson<Gltf>(json);
            if (gltf.meshes == null || gltf.meshes.Length == 0 || gltf.meshes[0].primitives.Length == 0)
            {
                Debug.LogWarning($"'{path}' contains no mesh.");
                return null;
            }

            Primitive primitive = gltf.meshes[0].primitives[0];
            if (primitive.attributes.POSITION < 0 || primitive.indices < 0)
            {
                Debug.LogWarning($"'{path}' needs positions and indices.");
                return null;
            }

            return Build(data, binStart, gltf, primitive, scale);
        }

        private static Mesh Build(byte[] data, int binStart, Gltf gltf, Primitive primitive, float scale)
        {
            Accessor positions = gltf.accessors[primitive.attributes.POSITION];
            Accessor indices = gltf.accessors[primitive.indices];
            int positionStart = binStart + gltf.bufferViews[positions.bufferView].byteOffset + positions.byteOffset;
            int indexStart = binStart + gltf.bufferViews[indices.bufferView].byteOffset + indices.byteOffset;

            // glTF is right handed: mirror x and flip the winding so the model keeps its shape in Unity.
            var vertices = new Vector3[positions.count];
            for (int i = 0; i < vertices.Length; i++)
            {
                int at = positionStart + i * 12;
                vertices[i] = new Vector3(-BitConverter.ToSingle(data, at), BitConverter.ToSingle(data, at + 4), BitConverter.ToSingle(data, at + 8)) * scale;
            }

            var triangles = new int[indices.count];
            bool wide = indices.componentType == ComponentUnsignedInt;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = ReadIndex(data, indexStart, i, wide);
                int b = ReadIndex(data, indexStart, i + 1, wide);
                int c = ReadIndex(data, indexStart, i + 2, wide);
                triangles[i] = a;
                triangles[i + 1] = c;
                triangles[i + 2] = b;
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int ReadIndex(byte[] data, int start, int index, bool wide)
        {
            return wide ? (int)BitConverter.ToUInt32(data, start + index * 4) : BitConverter.ToUInt16(data, start + index * 2);
        }
    }
}
