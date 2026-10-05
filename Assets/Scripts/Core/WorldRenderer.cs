using System.Collections.Generic;
using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Traduce el estado de <see cref="World"/> a GameObjects de escena: un único
    /// GameObject con un mesh combinado por chunk, nunca uno por bloque. Crea el
    /// material una sola vez en tiempo de ejecución (sin depender de un asset
    /// .mat).
    ///
    /// Se suscribe a World.ChunkDirty y encola cada chunk en vez de recorrerlos
    /// todos cada frame. ProcessQueue reconstruye como máximo N por llamada, así
    /// que editar o generar muchos chunks de golpe no bloquea un único frame.
    /// Los GameObjects de chunk se guardan en `views` incluso si el chunk deja
    /// de existir tras una regeneración: se desactivan en vez de destruirse, y
    /// se reutilizan (pooling) si un World nuevo vuelve a tener un chunk en esa
    /// misma coordenada.
    ///
    /// La visibilidad (qué chunk se ve y cuál no) la decide siempre quien llama
    /// a SetChunkVisible (ChunkStreamer): se guarda en `desiredVisibility` para
    /// que, aunque un chunk se (re)construya más tarde, respete la última
    /// petición en vez de aparecer por defecto solo por haberse construido. Sin
    /// esto, cargar un save con muchos chunks explorados los mostraba todos de
    /// golpe (al reconstruirlos todos de una vez) en vez de solo los cercanos
    /// al punto de aparición, desincronizando lo que WorldRenderer activaba de
    /// lo que ChunkStreamer creía que estaba visible.
    ///
    /// No es un MonoBehaviour: GameBootstrap la posee y la llama explícitamente,
    /// igual que hace con World y TerrainGenerator.
    /// </summary>
    public class WorldRenderer
    {
        private readonly Transform parent;
        private readonly Material sharedMaterial;
        private readonly Dictionary<Vector3Int, ChunkView> views = new();
        private readonly Dictionary<Vector3Int, bool> desiredVisibility = new();
        private readonly Queue<Vector3Int> rebuildQueue = new();
        private readonly HashSet<Vector3Int> queuedCoords = new();

        private World world;

        public WorldRenderer(World world, Transform parent)
        {
            this.parent = parent;
            sharedMaterial = CreateVoxelMaterial();
            AttachToWorld(world);
        }

        /// <summary>
        /// Cambia de World (p. ej. al cargar un save o regenerar) reutilizando
        /// los GameObjects de chunk ya creados. Oculta todo el pool sin
        /// excepción y olvida qué se pidió mostrar antes: es quien llame a
        /// Tick() justo después (ChunkStreamer) quien decide, desde cero, qué
        /// volver a mostrar — así nunca queda nada visible por accidente.
        /// </summary>
        public void AttachToWorld(World newWorld)
        {
            if (world != null) world.ChunkDirty -= Enqueue;

            world = newWorld;
            world.ChunkDirty += Enqueue;

            rebuildQueue.Clear();
            queuedCoords.Clear();
            desiredVisibility.Clear();

            foreach (ChunkView view in views.Values) view.gameObject.SetActive(false);

            foreach (Chunk chunk in world.Chunks) Enqueue(chunk);
        }

        private void Enqueue(Chunk chunk)
        {
            if (queuedCoords.Add(chunk.Coord)) rebuildQueue.Enqueue(chunk.Coord);
        }

        /// <summary>
        /// Muestra u oculta el GameObject de un chunk, sin tocar su mesh ni sus
        /// datos. Usado por ChunkStreamer para alejar/acercar chunks del área
        /// visible sin destruir ni regenerar nada. Si el chunk todavía no se ha
        /// construido, la petición se recuerda y se aplica en cuanto termine de
        /// construirse (ver RebuildChunk).
        /// </summary>
        public void SetChunkVisible(Vector3Int chunkCoord, bool visible)
        {
            desiredVisibility[chunkCoord] = visible;
            if (views.TryGetValue(chunkCoord, out ChunkView view)) view.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Reconstruye como máximo <paramref name="maxChunksPerCall"/> chunks de
        /// la cola. Pasar int.MaxValue vacía la cola entera de una vez (se usa
        /// para el build inicial del mundo, un coste único al arrancar).
        /// </summary>
        public void ProcessQueue(int maxChunksPerCall)
        {
            int processed = 0;
            while (processed < maxChunksPerCall && rebuildQueue.Count > 0)
            {
                Vector3Int coord = rebuildQueue.Dequeue();
                queuedCoords.Remove(coord);

                // El chunk pudo haberse quedado obsoleto (regeneración) o ya
                // no estar dirty (encolado dos veces antes de procesarse).
                if (world.TryGetChunk(coord, out Chunk chunk) && chunk.IsDirty)
                {
                    RebuildChunk(chunk);
                    processed++;
                }
            }
        }

        private void RebuildChunk(Chunk chunk)
        {
            if (!views.TryGetValue(chunk.Coord, out ChunkView view))
            {
                view = CreateView(chunk.Coord);
                views[chunk.Coord] = view;
            }

            ChunkNeighborhood neighborhood = world.GetNeighborhood(chunk.Coord);
            MeshData meshData = ChunkMeshBuilder.Build(neighborhood);
            view.Apply(meshData);
            chunk.IsDirty = false;

            // Construirse no implica verse: sin una petición explícita de
            // ChunkStreamer, un chunk recién (re)construido se queda oculto.
            bool visible = desiredVisibility.TryGetValue(chunk.Coord, out bool requested) && requested;
            view.gameObject.SetActive(visible);
        }

        private ChunkView CreateView(Vector3Int chunkCoord)
        {
            var chunkObject = new GameObject($"Chunk {chunkCoord.x},{chunkCoord.z}");
            chunkObject.transform.SetParent(parent, worldPositionStays: false);
            chunkObject.transform.position = new Vector3(
                chunkCoord.x * ChunkConstants.Width, 0f, chunkCoord.z * ChunkConstants.Depth);

            var meshFilter = chunkObject.AddComponent<MeshFilter>();
            var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            var meshCollider = chunkObject.AddComponent<MeshCollider>();
            var view = chunkObject.AddComponent<ChunkView>();
            view.Initialize(meshFilter, meshRenderer, meshCollider, sharedMaterial);
            return view;
        }

        private static Material CreateVoxelMaterial()
        {
            Shader shader = RuntimeShader.FindWithFallback("VoxelBuilder/VertexColor", "Universal Render Pipeline/Unlit");
            return new Material(shader) { name = "VoxelMaterial (runtime)" };
        }
    }
}
