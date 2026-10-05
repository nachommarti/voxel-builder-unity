using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelBuilder
{
    /// <summary>
    /// Componente de escena que aloja el mesh combinado de un chunk. No conoce
    /// bloques ni generación: solo recibe un <see cref="MeshData"/> ya calculado
    /// y lo sube al MeshFilter/MeshCollider. Un único GameObject por chunk, nunca
    /// uno por bloque.
    /// </summary>
    public class ChunkView : MonoBehaviour
    {
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;
        private Mesh mesh;

        public void Initialize(MeshFilter filter, MeshRenderer renderer, MeshCollider collider, Material material)
        {
            meshFilter = filter;
            meshCollider = collider;

            // UInt32 admite más de 65k vértices: un chunk denso con muchas caras
            // expuestas puede superar ese límite con IndexFormat.UInt16 por defecto.
            mesh = new Mesh { name = "ChunkMesh", indexFormat = IndexFormat.UInt32 };

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
            renderer.sharedMaterial = material;
        }

        public void Apply(MeshData meshData)
        {
            meshData.ApplyTo(mesh);

            // MeshCollider no detecta cambios in-place del mismo Mesh; hay que
            // reasignarlo para forzar el recálculo de la geometría de colisión.
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }
}
