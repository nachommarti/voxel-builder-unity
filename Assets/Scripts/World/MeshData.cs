using System.Collections.Generic;
using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Buffers intermedios de una malla antes de subirse a un <see cref="Mesh"/>
    /// de Unity. Mantener esto como listas planas (en vez de construir el Mesh
    /// directamente) es lo que permite que ChunkMeshBuilder sea una función pura
    /// y testeable sin depender de la escena ni del pipeline de render.
    /// </summary>
    public sealed class MeshData
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<int> Triangles = new();
        public readonly List<Color32> Colors = new();

        public bool IsEmpty => Vertices.Count == 0;

        public void ApplyTo(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(Vertices);
            mesh.SetTriangles(Triangles, 0);
            mesh.SetColors(Colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
