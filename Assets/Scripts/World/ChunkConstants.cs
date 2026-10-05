namespace VoxelBuilder
{
    /// <summary>
    /// Dimensiones fijas de un chunk. Cada chunk cubre toda la altura del mundo
    /// (Height): el mundo es una rejilla 2D de columnas de chunks (ver
    /// ChunkStreamer, que las genera bajo demanda alrededor del jugador), no
    /// una rejilla 3D. Esto evita tener que gestionar vecinos verticales.
    /// </summary>
    public static class ChunkConstants
    {
        public const int Width = 16;   // eje X
        public const int Depth = 16;   // eje Z
        public const int Height = 64;  // eje Y, columna completa del mundo

        public const int BlockCount = Width * Height * Depth;
    }
}
