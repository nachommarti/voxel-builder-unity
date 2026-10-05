using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Genera el terreno de un chunk escribiendo bloques a través de la API
    /// pública de Chunk. No conoce nada de mesh, rendering, ni de qué chunks
    /// existen alrededor (eso es responsabilidad de World/ChunkStreamer): su
    /// única función es decidir qué bloque va en cada celda de un chunk dado.
    ///
    /// Se escribe directamente en el Chunk recién creado (no vía World.SetBlock)
    /// porque aquí no hace falta el marcado de vecinos dirty: es un chunk nuevo
    /// que aún no tiene mesh, así que se construirá igual la primera vez. Ese
    /// marcado de vecinos es importante en cambio para las ediciones del
    /// jugador en tiempo real (ver World.SetBlock).
    /// </summary>
    public static class TerrainGenerator
    {
        private const int BaseHeight = 20;
        private const float HeightVariation = 18f;
        private const int DirtLayerThickness = 4;

        private const int NoiseOctaves = 4;
        private const float NoisePersistence = 0.5f;
        private const float NoiseScale = 0.02f;

        /// <summary>Crea y rellena el chunk en esa coordenada de la rejilla de chunks.</summary>
        public static Chunk GenerateChunkAt(World world, Vector3Int chunkCoord, int seed)
        {
            Chunk chunk = world.CreateChunk(chunkCoord);
            FillChunk(chunk, chunkCoord.x, chunkCoord.z, seed);
            return chunk;
        }

        private static void FillChunk(Chunk chunk, int chunkX, int chunkZ, int seed)
        {
            for (int x = 0; x < ChunkConstants.Width; x++)
            {
                for (int z = 0; z < ChunkConstants.Depth; z++)
                {
                    int worldX = chunkX * ChunkConstants.Width + x;
                    int worldZ = chunkZ * ChunkConstants.Depth + z;
                    int surfaceHeight = SurfaceHeightAt(worldX, worldZ, seed);
                    FillColumn(chunk, x, z, surfaceHeight);
                }
            }
        }

        private static int SurfaceHeightAt(int worldX, int worldZ, int seed)
        {
            float noise = NoiseUtils.OctavePerlin(worldX, worldZ, seed, NoiseOctaves, NoisePersistence, NoiseScale);
            int height = BaseHeight + Mathf.RoundToInt(noise * HeightVariation);
            return Mathf.Clamp(height, 1, ChunkConstants.Height - 1);
        }

        private static void FillColumn(Chunk chunk, int x, int z, int surfaceHeight)
        {
            for (int y = 0; y <= surfaceHeight; y++)
            {
                BlockType type;
                if (y == surfaceHeight) type = BlockType.Grass;
                else if (y >= surfaceHeight - DirtLayerThickness) type = BlockType.Dirt;
                else type = BlockType.Stone;

                chunk.SetBlock(x, y, z, type);
            }
        }
    }
}
