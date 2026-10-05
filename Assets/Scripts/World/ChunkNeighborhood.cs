using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Vista de un chunk junto con sus 4 vecinos horizontales. Existe para que
    /// ChunkMeshBuilder pueda decidir si dibujar una cara en el borde del chunk
    /// sin que ni Chunk ni el propio builder necesiten conocer World.
    ///
    /// Los vecinos ausentes (borde del mundo generado) se tratan como aire, y
    /// por debajo de y=0 se trata como opaco: esa cara nunca la ve el jugador,
    /// así que ocultarla ahorra geometría sin coste visual.
    /// </summary>
    public readonly struct ChunkNeighborhood
    {
        private readonly Chunk west;
        private readonly Chunk east;
        private readonly Chunk south;
        private readonly Chunk north;

        public Chunk Center { get; }

        public ChunkNeighborhood(Chunk center, Chunk west, Chunk east, Chunk south, Chunk north)
        {
            Center = center;
            this.west = west;
            this.east = east;
            this.south = south;
            this.north = north;
        }

        /// <summary>Bloque en coordenadas locales al chunk central, tolerando un paso fuera de sus límites en X/Z.</summary>
        public BlockType GetBlock(int x, int y, int z)
        {
            if (y < 0) return BlockType.Stone;
            if (y >= ChunkConstants.Height) return BlockType.Air;

            if (x < 0) return west != null ? west.GetBlock(ChunkConstants.Width - 1, y, z) : BlockType.Air;
            if (x >= ChunkConstants.Width) return east != null ? east.GetBlock(0, y, z) : BlockType.Air;
            if (z < 0) return south != null ? south.GetBlock(x, y, ChunkConstants.Depth - 1) : BlockType.Air;
            if (z >= ChunkConstants.Depth) return north != null ? north.GetBlock(x, y, 0) : BlockType.Air;

            return Center.GetBlock(x, y, z);
        }
    }
}
