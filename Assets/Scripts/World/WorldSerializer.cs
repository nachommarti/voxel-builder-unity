using System.IO;
using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Guarda y carga el estado completo del mundo (semilla y los bloques de
    /// cada chunk explorado) en un archivo binario propio. Nada de JSON ni de
    /// librerías externas: los chunks ya son arrays de bytes, así que escribirlos
    /// tal cual es la representación más simple y compacta posible.
    ///
    /// Con streaming el mundo no tiene un tamaño fijo: se guardan todos los
    /// chunks que existan en World en ese momento (los explorados hasta la
    /// fecha), sean los que sean.
    /// </summary>
    public static class WorldSerializer
    {
        // v2: se quitó el tamaño de mundo fijo (worldSizeInChunksX/Z) del
        // formato al pasar a streaming; un save de la v1 se rechaza en vez de
        // leerse mal.
        private const int FormatVersion = 2;
        private const string FileName = "world.save";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(World world, int seed, string path = null)
        {
            path ??= DefaultPath;

            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);

            writer.Write(FormatVersion);
            writer.Write(seed);
            writer.Write(world.Chunks.Count);

            foreach (Chunk chunk in world.Chunks)
            {
                writer.Write(chunk.Coord.x);
                writer.Write(chunk.Coord.z);
                chunk.WriteBlocks(writer);
            }
        }

        /// <summary>
        /// Intenta cargar el mundo guardado. Devuelve false (sin lanzar) si no
        /// existe el archivo o si es de una versión de formato distinta: un
        /// save corrupto o de otra versión del juego no debe reventar el arranque.
        /// </summary>
        public static bool TryLoad(out WorldSaveData saveData, string path = null)
        {
            path ??= DefaultPath;
            saveData = null;

            if (!File.Exists(path)) return false;

            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream);

                int version = reader.ReadInt32();
                if (version != FormatVersion) return false;

                int seed = reader.ReadInt32();
                int chunkCount = reader.ReadInt32();

                var world = new World();
                for (int i = 0; i < chunkCount; i++)
                {
                    int chunkX = reader.ReadInt32();
                    int chunkZ = reader.ReadInt32();
                    Chunk chunk = world.CreateChunk(new Vector3Int(chunkX, 0, chunkZ));
                    chunk.ReadBlocks(reader);
                }

                saveData = new WorldSaveData(world, seed);
                return true;
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"[WorldSerializer] No se pudo leer el save ({exception.Message}); se ignora.");
                return false;
            }
        }
    }

    /// <summary>Resultado de cargar un mundo guardado: lo justo para no tener que regenerarlo.</summary>
    public sealed class WorldSaveData
    {
        public World World { get; }
        public int Seed { get; }

        public WorldSaveData(World world, int seed)
        {
            World = world;
            Seed = seed;
        }
    }
}
