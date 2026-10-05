namespace VoxelBuilder
{
    /// <summary>
    /// Identificador de tipo de bloque. Se guarda como byte en los chunks, por lo
    /// que este enum nunca debe superar 256 valores. Air siempre debe ser 0 para
    /// que un array de bytes a cero represente un chunk vacío por defecto.
    /// </summary>
    public enum BlockType : byte
    {
        Air = 0,
        Stone,
        Dirt,
        Grass,
        Sand,
        Wood,
        Leaves,
        Water
    }
}
