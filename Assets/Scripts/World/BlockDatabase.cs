using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Propiedades de un tipo de bloque. Struct de solo lectura: los datos de
    /// bloque no cambian en tiempo de ejecución.
    /// </summary>
    public readonly struct BlockData
    {
        public readonly bool IsSolid;
        public readonly bool IsTransparent;
        public readonly Color32 Color;

        public BlockData(bool isSolid, bool isTransparent, Color32 color)
        {
            IsSolid = isSolid;
            IsTransparent = isTransparent;
            Color = color;
        }
    }

    /// <summary>
    /// Fuente única de verdad para las propiedades de cada <see cref="BlockType"/>.
    /// Tabla estática indexada directamente por el valor byte del enum: acceso
    /// O(1) sin necesidad de diccionario ni de cargar un ScriptableObject.
    /// </summary>
    public static class BlockDatabase
    {
        private static readonly BlockData[] Data =
        {
            new BlockData(isSolid: false, isTransparent: true,  color: new Color32(0, 0, 0, 0)),          // Air
            new BlockData(isSolid: true,  isTransparent: false, color: new Color32(120, 120, 120, 255)),  // Stone
            new BlockData(isSolid: true,  isTransparent: false, color: new Color32(102, 71, 42, 255)),    // Dirt
            new BlockData(isSolid: true,  isTransparent: false, color: new Color32(86, 148, 58, 255)),    // Grass
            new BlockData(isSolid: true,  isTransparent: false, color: new Color32(219, 205, 150, 255)),  // Sand
            new BlockData(isSolid: true,  isTransparent: false, color: new Color32(133, 94, 52, 255)),    // Wood
            new BlockData(isSolid: true,  isTransparent: true,  color: new Color32(63, 114, 44, 200)),    // Leaves
            new BlockData(isSolid: false, isTransparent: true,  color: new Color32(64, 128, 200, 150)),   // Water
        };

        public static BlockData Get(BlockType type) => Data[(byte)type];
        public static bool IsSolid(BlockType type) => Data[(byte)type].IsSolid;
        public static bool IsTransparent(BlockType type) => Data[(byte)type].IsTransparent;
        public static Color32 GetColor(BlockType type) => Data[(byte)type].Color;
    }
}
