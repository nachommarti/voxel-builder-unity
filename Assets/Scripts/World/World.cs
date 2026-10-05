using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Orquesta la colección de chunks del mundo y resuelve el acceso a bloques
    /// en coordenadas globales. No genera terreno (eso es responsabilidad de
    /// TerrainGenerator) ni construye meshes (ChunkMeshBuilder); solo conoce la
    /// estructura de datos y la conversión de coordenadas.
    ///
    /// Solo existen los chunks que alguien ha creado explícitamente con
    /// CreateChunk (ChunkStreamer los va creando bajo demanda alrededor del
    /// jugador). Consultar o escribir fuera de esos chunks se trata como aire,
    /// nunca como error, para no reventar en una zona todavía no generada.
    /// </summary>
    public sealed class World
    {
        /// <summary>
        /// Acceso de conveniencia para sistemas que solo necesitan "el mundo
        /// actual" (jugador, UI, interacción). Solo existe un World por partida,
        /// así que un service locator simple es proporcionado a la complejidad
        /// del proyecto en vez de inyectar dependencias en cada MonoBehaviour.
        /// </summary>
        public static World Instance { get; private set; }

        /// <summary>
        /// Se dispara cuando un chunk pasa a necesitar reconstrucción de mesh
        /// (recién creado o editado). WorldRenderer se suscribe para encolarlo
        /// en vez de tener que recorrer todos los chunks cada frame buscando
        /// cuáles están dirty.
        /// </summary>
        public event Action<Chunk> ChunkDirty;

        private readonly Dictionary<Vector3Int, Chunk> chunks = new();

        public World()
        {
            Instance = this;
        }

        public IReadOnlyCollection<Chunk> Chunks => chunks.Values;

        /// <summary>Crea (o reemplaza) el chunk en esa coordenada de chunk y lo registra en el mundo.</summary>
        public Chunk CreateChunk(Vector3Int chunkCoord)
        {
            var chunk = new Chunk(chunkCoord);
            chunks[chunkCoord] = chunk;
            ChunkDirty?.Invoke(chunk);
            return chunk;
        }

        public bool TryGetChunk(Vector3Int chunkCoord, out Chunk chunk) =>
            chunks.TryGetValue(chunkCoord, out chunk);

        /// <summary>Empaqueta el chunk en esa coordenada junto con sus 4 vecinos horizontales, para construir su mesh.</summary>
        public ChunkNeighborhood GetNeighborhood(Vector3Int chunkCoord)
        {
            chunks.TryGetValue(chunkCoord, out Chunk center);
            chunks.TryGetValue(chunkCoord + Vector3Int.left, out Chunk west);
            chunks.TryGetValue(chunkCoord + Vector3Int.right, out Chunk east);
            chunks.TryGetValue(chunkCoord + new Vector3Int(0, 0, -1), out Chunk south);
            chunks.TryGetValue(chunkCoord + new Vector3Int(0, 0, 1), out Chunk north);
            return new ChunkNeighborhood(center, west, east, south, north);
        }

        public BlockType GetBlock(Vector3Int worldPos)
        {
            if (worldPos.y < 0 || worldPos.y >= ChunkConstants.Height) return BlockType.Air;

            Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
            if (!chunks.TryGetValue(chunkCoord, out Chunk chunk)) return BlockType.Air;

            Vector3Int local = WorldToLocalCoord(worldPos);
            return chunk.GetBlock(local.x, local.y, local.z);
        }

        /// <summary>
        /// Coloca/elimina un bloque en coordenadas de mundo. Devuelve false si la
        /// posición cae fuera del mundo generado o si el bloque ya tenía ese valor.
        /// </summary>
        public bool SetBlock(Vector3Int worldPos, BlockType type)
        {
            if (worldPos.y < 0 || worldPos.y >= ChunkConstants.Height) return false;

            Vector3Int chunkCoord = WorldToChunkCoord(worldPos);
            if (!chunks.TryGetValue(chunkCoord, out Chunk chunk)) return false;

            Vector3Int local = WorldToLocalCoord(worldPos);
            bool changed = chunk.SetBlock(local.x, local.y, local.z, type);
            if (changed)
            {
                ChunkDirty?.Invoke(chunk);
                MarkBorderNeighborsDirty(chunkCoord, local);
            }
            return changed;
        }

        public static Vector3Int WorldToChunkCoord(Vector3Int worldPos) => new(
            FloorDiv(worldPos.x, ChunkConstants.Width),
            0,
            FloorDiv(worldPos.z, ChunkConstants.Depth));

        public static Vector3Int WorldToLocalCoord(Vector3Int worldPos) => new(
            Mod(worldPos.x, ChunkConstants.Width),
            worldPos.y,
            Mod(worldPos.z, ChunkConstants.Depth));

        /// <summary>
        /// Si el bloque editado está en la cara límite de su chunk, el chunk
        /// vecino comparte esa costura en su propio mesh (culled faces mira al
        /// bloque de al lado) y también debe reconstruirse para no dejar huecos
        /// o caras de más.
        /// </summary>
        private void MarkBorderNeighborsDirty(Vector3Int chunkCoord, Vector3Int local)
        {
            if (local.x == 0) MarkDirtyIfExists(chunkCoord + Vector3Int.left);
            else if (local.x == ChunkConstants.Width - 1) MarkDirtyIfExists(chunkCoord + Vector3Int.right);

            if (local.z == 0) MarkDirtyIfExists(chunkCoord + new Vector3Int(0, 0, -1));
            else if (local.z == ChunkConstants.Depth - 1) MarkDirtyIfExists(chunkCoord + new Vector3Int(0, 0, 1));
        }

        private void MarkDirtyIfExists(Vector3Int chunkCoord)
        {
            if (chunks.TryGetValue(chunkCoord, out Chunk neighbor))
            {
                neighbor.IsDirty = true;
                ChunkDirty?.Invoke(neighbor);
            }
        }

        private static int FloorDiv(int value, int size) =>
            value >= 0 ? value / size : (value - size + 1) / size;

        private static int Mod(int value, int size)
        {
            int m = value % size;
            return m < 0 ? m + size : m;
        }
    }
}
