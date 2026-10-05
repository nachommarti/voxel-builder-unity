using System.Collections.Generic;
using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Mantiene generados y visibles solo los chunks alrededor del jugador,
    /// en vez de un mundo acotado generado entero de antemano. Los chunks que
    /// quedan fuera del radio de descarga se ocultan (WorldRenderer.SetChunkVisible)
    /// pero nunca se eliminan de World: sus datos (y las ediciones del jugador)
    /// se conservan en memoria mientras dure la partida, así que volver a una
    /// zona ya visitada no la regenera ni pierde nada.
    ///
    /// unloadRadius es mayor que loadRadius a propósito (histéresis): un chunk
    /// justo en el borde no aparece y desaparece en cada paso si el jugador se
    /// mueve hacia delante y atrás sobre la misma costura.
    ///
    /// No es un MonoBehaviour: GameBootstrap la posee y llama a Tick() desde su
    /// propio Update, igual que hace con WorldRenderer.
    /// </summary>
    public class ChunkStreamer
    {
        private readonly WorldRenderer worldRenderer;
        private readonly HashSet<Vector3Int> visibleCoords = new();

        private int loadRadius;
        private int unloadRadius;

        private World world;
        private Vector3Int? lastPlayerChunk;

        public ChunkStreamer(World world, WorldRenderer worldRenderer, int loadRadius, int unloadRadius)
        {
            this.worldRenderer = worldRenderer;
            this.loadRadius = loadRadius;
            this.unloadRadius = unloadRadius;
            AttachToWorld(world);
        }

        /// <summary>Cambia de World (al cargar un save o regenerar) olvidando qué había visible antes.</summary>
        public void AttachToWorld(World newWorld)
        {
            world = newWorld;
            visibleCoords.Clear();
            lastPlayerChunk = null; // fuerza recalcular en el próximo Tick, aunque el jugador no se haya movido
        }

        /// <summary>
        /// Cambia los radios de carga/descarga (p. ej. desde un menú) y
        /// recalcula ya mismo, usando el radio de carga también como límite de
        /// descarga (sin la holgura de histéresis). Un cambio explícito de
        /// radio debe verse reflejado al instante y sin desfases; la holgura
        /// entre loadRadius y unloadRadius solo tiene sentido para suavizar el
        /// movimiento orgánico del jugador (ver Tick), no un ajuste puntual:
        /// con ella, bajar el radio un solo paso nunca llegaba a cruzar el
        /// margen y el cambio "no se notaba" hasta el siguiente clic en la
        /// misma dirección.
        /// </summary>
        public void SetRadii(int newLoadRadius, int newUnloadRadius, Vector3Int playerChunk, int seed)
        {
            loadRadius = newLoadRadius;
            unloadRadius = newUnloadRadius;
            lastPlayerChunk = playerChunk;

            Recompute(playerChunk, seed, loadRadius);
        }

        /// <summary>
        /// Genera lo que falte alrededor de playerChunk y oculta lo que haya
        /// quedado demasiado lejos. No hace nada si el jugador sigue en el
        /// mismo chunk que la última vez: es la comprobación barata que evita
        /// recalcular las listas de carga/descarga en cada frame.
        /// </summary>
        public void Tick(Vector3Int playerChunk, int seed)
        {
            if (lastPlayerChunk.HasValue && lastPlayerChunk.Value == playerChunk) return;
            lastPlayerChunk = playerChunk;

            Recompute(playerChunk, seed, unloadRadius);
        }

        private void Recompute(Vector3Int playerChunk, int seed, int effectiveUnloadRadius)
        {
            for (int dx = -loadRadius; dx <= loadRadius; dx++)
            {
                for (int dz = -loadRadius; dz <= loadRadius; dz++)
                {
                    var coord = new Vector3Int(playerChunk.x + dx, 0, playerChunk.z + dz);

                    if (!world.TryGetChunk(coord, out _)) TerrainGenerator.GenerateChunkAt(world, coord, seed);
                    if (visibleCoords.Add(coord)) worldRenderer.SetChunkVisible(coord, true);
                }
            }

            visibleCoords.RemoveWhere(coord =>
            {
                if (ChebyshevDistance(coord, playerChunk) <= effectiveUnloadRadius) return false;

                worldRenderer.SetChunkVisible(coord, false);
                return true;
            });
        }

        private static int ChebyshevDistance(Vector3Int a, Vector3Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z));
    }
}
