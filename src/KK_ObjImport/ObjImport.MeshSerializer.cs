﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// "borrowed" from https://github.com/kaorun55/HoloLens-Samples/blob/master/Unity/WorldAnchorDemo/Assets/HoloToolkit/SpatialMapping/Scripts/SimpleMeshSerializer.cs
// appended to serialize the first set of UV coordinates.
// appended to serialize normals.

using System.Collections.Generic;
using SysDiag = System.Diagnostics;
using System.IO;
using UnityEngine;

namespace ObjImport
{
    /// <summary>
    /// SimpleMeshSerializer converts a UnityEngine.Mesh object to and from an array of bytes.
    /// This class saves minimal mesh data (vertices and triangle indices) in the following format:
    ///    File header: vertex count (32 bit integer), triangle count (32 bit integer)
    ///    Vertex list: vertex.x, vertex.y, vertex.z (all 32 bit float)
    ///    Triangle index list: 32 bit integers
    /// </summary>
    public static class SimpleMeshSerializer
    {
        /// <summary>
        /// The mesh header consists of two 32 bit integers.
        /// </summary>
        private static int HeaderSize = sizeof(int) * 2;

        /// <summary>
        /// Serializes a list of Mesh objects into a byte array.
        /// </summary>
        /// <param name="meshes">List of Mesh objects to be serialized.</param>
        /// <returns>Binary representation of the Mesh objects.</returns>
        public static byte[] Serialize(IEnumerable<Mesh> meshes)
        {
            byte[] data = null;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    foreach (Mesh mesh in meshes)
                    {
                        WriteMesh(writer, mesh);
                    }

                    stream.Position = 0;
                    data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                }
            }

            return data;
        }
        /// <summary>
        /// Serializes a Mesh object into a byte array.
        /// </summary>
        /// <param name="mesh">Mesh to be serialized.</param>
        /// <returns></returns>
        public static byte[] Serialize(Mesh mesh)
        {
            byte[] data = null;

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    WriteMesh(writer, mesh);
                    stream.Position = 0;
                    data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                }
            }

            return data;
        }

        /// <summary>
        /// Deserializes a list of Mesh objects from the provided byte array.
        /// </summary>
        /// <param name="data">Binary data to be deserialized into a list of Mesh objects.</param>
        /// <returns>List of Mesh objects.</returns>
        public static IEnumerable<Mesh> Deserialize(byte[] data)
        {
            List<Mesh> meshes = new List<Mesh>();

            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    while (reader.BaseStream.Length - reader.BaseStream.Position >= HeaderSize)
                    {
                        meshes.Add(ReadMesh(reader));
                    }
                }
            }

            return meshes;
        }

        /// <summary>
        /// Writes a Mesh object to the data stream.
        /// </summary>
        /// <param name="writer">BinaryWriter representing the data stream.</param>
        /// <param name="mesh">The Mesh object to be written.</param>
        private static void WriteMesh(BinaryWriter writer, Mesh mesh)
        {
            SysDiag.Debug.Assert(writer != null);

            // Write the mesh data.
            WriteMeshHeader(writer, mesh.vertexCount, mesh.triangles.Length);
            WriteVertices(writer, mesh.vertices);
            WriteTriangleIndicies(writer, mesh.triangles);
            WriteUVs(writer, mesh.uv);
            WriteNormals(writer, mesh.normals);
            WriteExtendedMeshData(writer, 1, mesh);
        }

        /// <summary>
        /// Reads a single Mesh object from the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <returns>Mesh object read from the stream.</returns>
        private static Mesh ReadMesh(BinaryReader reader)
        {
            SysDiag.Debug.Assert(reader != null);

            int vertexCount = 0;
            int triangleIndexCount = 0;

            // Read the mesh data.
            ReadMeshHeader(reader, out vertexCount, out triangleIndexCount);
            Vector3[] vertices = ReadVertices(reader, vertexCount);
            int[] triangleIndices = ReadTriangleIndicies(reader, triangleIndexCount);
            Vector2[] uvs = ReadUVs(reader, vertexCount);
            Vector3[] normals = ReadNormals(reader, vertexCount);
            Mesh extMesh = ReadExtendedMeshData(reader);

            // Create the mesh.
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangleIndices;
            mesh.uv = uvs;
            mesh.normals = normals;
            if (extMesh != null)
            {
                mesh.name = extMesh.name;
            }

            return mesh;
        }

        /// <summary>
        /// Writes a mesh header to the data stream.
        /// </summary>
        /// <param name="writer">BinaryWriter representing the data stream.</param>
        /// <param name="vertexCount">Count of vertices in the mesh.</param>
        /// <param name="triangleIndexCount">Count of triangle indices in the mesh.</param>
        private static void WriteMeshHeader(BinaryWriter writer, int vertexCount, int triangleIndexCount)
        {
            SysDiag.Debug.Assert(writer != null);

            writer.Write(vertexCount);
            writer.Write(triangleIndexCount);

        }

        /// <summary>
        /// Reads a mesh header from the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="vertexCount">Count of vertices in the mesh.</param>
        /// <param name="triangleIndexCount">Count of triangle indices in the mesh.</param>
        private static void ReadMeshHeader(BinaryReader reader, out int vertexCount, out int triangleIndexCount)
        {
            SysDiag.Debug.Assert(reader != null);

            vertexCount = reader.ReadInt32();
            triangleIndexCount = reader.ReadInt32();
        }

        /// <summary>
        /// Writes a mesh's vertices to the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="vertices">Array of Vector3 structures representing each vertex.</param>
        private static void WriteVertices(BinaryWriter writer, Vector3[] vertices)
        {
            SysDiag.Debug.Assert(writer != null);

            foreach (Vector3 vertex in vertices)
            {
                writer.Write(vertex.x);
                writer.Write(vertex.y);
                writer.Write(vertex.z);
            }
        }

        /// <summary>
        /// Reads a mesh's vertices from the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="vertexCount">Count of vertices to read.</param>
        /// <returns>Array of Vector3 structures representing the mesh's vertices.</returns>
        private static Vector3[] ReadVertices(BinaryReader reader, int vertexCount)
        {
            SysDiag.Debug.Assert(reader != null);

            Vector3[] vertices = new Vector3[vertexCount];

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(reader.ReadSingle(),
                                        reader.ReadSingle(),
                                        reader.ReadSingle());
            }

            return vertices;
        }

        /// <summary>
        /// Writes a mesh's vertices to the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="normals">Array of Vector3 structures representing each normal.</param>
        private static void WriteNormals(BinaryWriter writer, Vector3[] normals)
        {
            SysDiag.Debug.Assert(writer != null);

            foreach (Vector3 normal in normals)
            {
                writer.Write(normal.x);
                writer.Write(normal.y);
                writer.Write(normal.z);
            }
        }

        /// <summary>
        /// Reads a mesh's vertices from the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="vertexCount">Count of normals to read. Should be the same as vertexCount.</param>
        /// <returns>Array of Vector3 structures representing the mesh's normals.</returns>
        private static Vector3[] ReadNormals(BinaryReader reader, int vertexCount)
        {
            SysDiag.Debug.Assert(reader != null);

            Vector3[] normals = new Vector3[vertexCount];

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = new Vector3(reader.ReadSingle(),
                                        reader.ReadSingle(),
                                        reader.ReadSingle());
            }

            return normals;
        }

        /// <summary>
        /// Writes a mesh's uvs to the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="uvs">Array of Vector2 structures representing each uv coordinate.</param>
        private static void WriteUVs(BinaryWriter writer, Vector2[] uvs)
        {
            SysDiag.Debug.Assert(writer != null);

            foreach (Vector3 coordinate in uvs)
            {
                writer.Write(coordinate.x);
                writer.Write(coordinate.y);
            }
        }

        /// <summary>
        /// Reads a mesh's uvs from the data stream.
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="uvCount">Count of uvs to read. Should be the same as vertexCount.</param>
        /// <returns>Array of Vector2 structures representing the mesh's uvs.</returns>
        private static Vector2[] ReadUVs(BinaryReader reader, int uvCount)
        {
            SysDiag.Debug.Assert(reader != null);

            Vector2[] uvs = new Vector2[uvCount];

            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(reader.ReadSingle(),
                                        reader.ReadSingle());
            }

            return uvs;
        }

        /// <summary>
        /// Writes the vertex indices that represent a mesh's triangles to the data stream
        /// </summary>
        /// <param name="writer">BinaryWriter representing the data stream.</param>
        /// <param name="triangleIndices">Array of integers that describe how the vertex indices form triangles.</param>
        private static void WriteTriangleIndicies(BinaryWriter writer, int[] triangleIndices)
        {
            SysDiag.Debug.Assert(writer != null);

            foreach (int index in triangleIndices)
            {
                writer.Write(index);
            }
        }

        /// <summary>
        /// Reads the vertex indices that represent a mesh's triangles from the data stream
        /// </summary>
        /// <param name="reader">BinaryReader representing the data stream.</param>
        /// <param name="triangleIndexCount">Count of indices to read.</param>
        /// <returns>Array of integers that describe how the vertex indices form triangles.</returns>
        private static int[] ReadTriangleIndicies(BinaryReader reader, int triangleIndexCount)
        {
            SysDiag.Debug.Assert(reader != null);

            int[] triangleIndices = new int[triangleIndexCount];

            for (int i = 0; i < triangleIndices.Length; i++)
            {
                triangleIndices[i] = reader.ReadInt32();
            }

            return triangleIndices;
        }

        private static void WriteExtendedMeshData(BinaryWriter writer, int version, Mesh mesh)
        {
            writer.Write(version);
            writer.Write(mesh.name);
        }

        private static Mesh ReadExtendedMeshData(BinaryReader reader)
        {
            Mesh mesh = new Mesh();
            // backwards compatibility
            try
            {
                int version = reader.ReadInt32();

                if (version == 1)
                {
                    mesh.name = reader.ReadString();
                }
                else
                {
                    reader.BaseStream.Position -= sizeof(int);
                    return null;
                }
                return mesh;

            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }
    }
}