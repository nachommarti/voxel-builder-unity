using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Construye la malla de un chunk a partir de sus bloques, emitiendo solo
    /// las caras contiguas a aire o a un bloque transparente distinto ("culled
    /// faces"): la optimización mínima indispensable frente a dibujar las 6
    /// caras de cada bloque. Es una función pura (chunk + vecinos -> MeshData),
    /// sin MonoBehaviour ni referencias a la escena, por lo que es testeable de
    /// forma aislada.
    /// </summary>
    public static class ChunkMeshBuilder
    {
        private readonly struct FaceDefinition
        {
            public readonly Vector3Int Direction;
            public readonly Vector3Int C0, C1, C2, C3;
            public readonly float Shade;

            public FaceDefinition(Vector3Int direction, Vector3Int c0, Vector3Int c1, Vector3Int c2, Vector3Int c3, float shade)
            {
                Direction = direction;
                C0 = c0;
                C1 = c1;
                C2 = c2;
                C3 = c3;
                Shade = shade;
            }
        }

        // Esquinas de un cubo unitario, nombradas por su patrón de bits (x,y,z).
        private static readonly Vector3Int C000 = new(0, 0, 0);
        private static readonly Vector3Int C100 = new(1, 0, 0);
        private static readonly Vector3Int C010 = new(0, 1, 0);
        private static readonly Vector3Int C110 = new(1, 1, 0);
        private static readonly Vector3Int C001 = new(0, 0, 1);
        private static readonly Vector3Int C101 = new(1, 0, 1);
        private static readonly Vector3Int C011 = new(0, 1, 1);
        private static readonly Vector3Int C111 = new(1, 1, 1);

        // Cada cara lista sus 4 esquinas en el orden que produce, por producto
        // vectorial, una normal hacia fuera del cubo (comprobado a mano una vez
        // aquí para no depender de RecalculateNormals adivinando la dirección).
        //
        // Shade multiplica el color del bloque según la orientación de la cara
        // (arriba más clara, abajo más oscura). No hay luces reales en la escena
        // (el shader es unlit), así que sin esto todas las caras de un bloque
        // salen exactamente del mismo color y el terreno se ve plano.
        private static readonly FaceDefinition[] Faces =
        {
            new(new Vector3Int(1, 0, 0), C100, C110, C111, C101, 0.6f),   // +X (este)
            new(new Vector3Int(-1, 0, 0), C000, C001, C011, C010, 0.6f),  // -X (oeste)
            new(new Vector3Int(0, 1, 0), C010, C011, C111, C110, 1.0f),   // +Y (arriba)
            new(new Vector3Int(0, -1, 0), C000, C100, C101, C001, 0.5f),  // -Y (abajo)
            new(new Vector3Int(0, 0, 1), C001, C101, C111, C011, 0.8f),   // +Z (norte)
            new(new Vector3Int(0, 0, -1), C000, C010, C110, C100, 0.8f),  // -Z (sur)
        };

        public static MeshData Build(ChunkNeighborhood neighborhood)
        {
            var meshData = new MeshData();
            Chunk chunk = neighborhood.Center;

            for (int x = 0; x < ChunkConstants.Width; x++)
            {
                for (int y = 0; y < ChunkConstants.Height; y++)
                {
                    for (int z = 0; z < ChunkConstants.Depth; z++)
                    {
                        BlockType block = chunk.GetBlock(x, y, z);
                        if (block == BlockType.Air) continue;

                        Color32 color = BlockDatabase.GetColor(block);
                        var blockPos = new Vector3Int(x, y, z);

                        foreach (FaceDefinition face in Faces)
                        {
                            BlockType neighborBlock = neighborhood.GetBlock(
                                x + face.Direction.x, y + face.Direction.y, z + face.Direction.z);

                            if (!IsFaceVisible(block, neighborBlock)) continue;

                            AppendFace(meshData, blockPos, face, color);
                        }
                    }
                }
            }

            return meshData;
        }

        private static bool IsFaceVisible(BlockType block, BlockType neighbor)
        {
            if (neighbor == BlockType.Air) return true;
            // Evita coser dos bloques transparentes iguales (agua-agua, hojas-hojas);
            // sí dibuja la cara entre un sólido y un transparente distinto.
            return BlockDatabase.IsTransparent(neighbor) && neighbor != block;
        }

        private static void AppendFace(MeshData meshData, Vector3Int blockPos, FaceDefinition face, Color32 color)
        {
            int baseIndex = meshData.Vertices.Count;

            meshData.Vertices.Add(blockPos + face.C0);
            meshData.Vertices.Add(blockPos + face.C1);
            meshData.Vertices.Add(blockPos + face.C2);
            meshData.Vertices.Add(blockPos + face.C3);

            Color32 shaded = ApplyShade(color, face.Shade);
            meshData.Colors.Add(shaded);
            meshData.Colors.Add(shaded);
            meshData.Colors.Add(shaded);
            meshData.Colors.Add(shaded);

            meshData.Triangles.Add(baseIndex);
            meshData.Triangles.Add(baseIndex + 1);
            meshData.Triangles.Add(baseIndex + 2);

            meshData.Triangles.Add(baseIndex);
            meshData.Triangles.Add(baseIndex + 2);
            meshData.Triangles.Add(baseIndex + 3);
        }

        private static Color32 ApplyShade(Color32 color, float shade) => new(
            (byte)Mathf.RoundToInt(color.r * shade),
            (byte)Mathf.RoundToInt(color.g * shade),
            (byte)Mathf.RoundToInt(color.b * shade),
            color.a);
    }
}
