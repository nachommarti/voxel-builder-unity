using System;
using System.IO;
using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Almacena los bloques de un chunk en un array plano de bytes y expone
    /// acceso local (x,y,z). No sabe nada de mesh, render ni física: esas
    /// responsabilidades viven en ChunkMeshBuilder y en los componentes de
    /// escena que consuman su mesh.
    ///
    /// Las coordenadas fuera de rango nunca lanzan excepción: se tratan como
    /// aire en lectura y se ignoran en escritura. Igual que en Minecraft, "fuera
    /// del chunk" es indistinguible de "vacío", lo que evita que un caller (mesh
    /// builder, generador, interacción del jugador) pueda romper el juego por un
    /// índice mal calculado en un borde.
    /// </summary>
    public sealed class Chunk
    {
        private readonly byte[] blocks = new byte[ChunkConstants.BlockCount];

        /// <summary>Coordenada del chunk en la rejilla de chunks (no en unidades de mundo).</summary>
        public Vector3Int Coord { get; }

        /// <summary>True si el mesh visual/collider necesita reconstruirse.</summary>
        public bool IsDirty { get; set; }

        public Chunk(Vector3Int coord)
        {
            Coord = coord;
            IsDirty = true; // recién creado: aún no tiene mesh generado
        }

        public BlockType GetBlock(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z)) return BlockType.Air;
            return (BlockType)blocks[GetIndex(x, y, z)];
        }

        /// <summary>Escribe un bloque local. Devuelve true si el valor cambió realmente.</summary>
        public bool SetBlock(int x, int y, int z, BlockType type)
        {
            if (!IsInBounds(x, y, z)) return false;

            int index = GetIndex(x, y, z);
            byte value = (byte)type;
            if (blocks[index] == value) return false;

            blocks[index] = value;
            IsDirty = true;
            return true;
        }

        /// <summary>Escribe los bloques crudos del chunk (para guardar a disco).</summary>
        public void WriteBlocks(BinaryWriter writer) => writer.Write(blocks);

        /// <summary>
        /// Sustituye los bloques del chunk por los leídos de disco y lo marca
        /// dirty para que se reconstruya su mesh. Copia a un array existente en
        /// vez de reasignar `blocks` para no romper el `readonly`.
        /// </summary>
        public void ReadBlocks(BinaryReader reader)
        {
            byte[] loaded = reader.ReadBytes(ChunkConstants.BlockCount);
            Array.Copy(loaded, blocks, ChunkConstants.BlockCount);
            IsDirty = true;
        }

        public static bool IsInBounds(int x, int y, int z) =>
            x >= 0 && x < ChunkConstants.Width &&
            y >= 0 && y < ChunkConstants.Height &&
            z >= 0 && z < ChunkConstants.Depth;

        private static int GetIndex(int x, int y, int z) =>
            x + z * ChunkConstants.Width + y * ChunkConstants.Width * ChunkConstants.Depth;
    }
}
