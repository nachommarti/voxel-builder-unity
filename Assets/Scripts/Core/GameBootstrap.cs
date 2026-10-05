using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace VoxelBuilder
{
    /// <summary>
    /// Punto de arranque de la escena: crea el World, carga un save si existe
    /// (o genera terreno nuevo si no) y construye la representación visual
    /// (WorldRenderer) y el streaming de chunks alrededor del jugador
    /// (ChunkStreamer). También deja constancia en la consola del resultado
    /// para poder validarlo a mano, y expone controles de desarrollo: R
    /// regenera el mundo con otra semilla, F5 guarda, F9 recarga desde disco, y
    /// Escape abre el menú de pausa (PauseMenuUI) para ajustar el radio de
    /// carga del streaming y salir del juego.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // El jugador siempre aparece en la columna central del chunk (0,0,0):
        // con streaming no hay un "centro del mundo" con sentido, así que el
        // spawn es un punto fijo del que el mundo crece en cualquier dirección.
        private const int SpawnColumnX = ChunkConstants.Width / 2;
        private const int SpawnColumnZ = ChunkConstants.Depth / 2;

        [Header("Mundo (streaming)")]
        [SerializeField] private int seed = 12345;
        [Tooltip("Radio (en chunks) alrededor del jugador que se mantiene generado y visible. Ajustable en caliente desde el menú de pausa (Escape).")]
        [SerializeField] private int loadRadiusInChunks = 4;
        [Tooltip("Radio (en chunks) a partir del cual un chunk se oculta. Mayor que el de carga para evitar parpadeo en el borde.")]
        [SerializeField] private int unloadRadiusInChunks = 5;

        [Header("Jugador")]
        [SerializeField] private CharacterController player;
        [SerializeField] private float spawnClearance = 0.1f;

        [Header("Rendimiento")]
        [Tooltip("Chunks reconstruidos como máximo por frame tras el build inicial (edición/streaming en tiempo real).")]
        [SerializeField] private int maxChunkRebuildsPerFrame = 2;

        private World world;
        private WorldRenderer worldRenderer;
        private ChunkStreamer streamer;

        private void Start()
        {
            world = new World();
            worldRenderer = new WorldRenderer(world, transform);
            streamer = new ChunkStreamer(world, worldRenderer, loadRadiusInChunks, unloadRadiusInChunks);

            if (!TryLoadSavedWorld()) FinishLoadingWorld();

            EnsureEventSystemExists();

            new GameObject("Crosshair").AddComponent<CrosshairUI>();
            new GameObject("Hotbar").AddComponent<Hotbar>();
            new GameObject("CursorLock").AddComponent<CursorLockController>();

            var pauseMenu = new GameObject("PauseMenu").AddComponent<PauseMenuUI>();
            pauseMenu.Initialize(this, loadRadiusInChunks);
        }

        /// <summary>
        /// uGUI necesita un EventSystem en la escena para que los botones
        /// reciban clics. Crear un Canvas por código (Crosshair, Hotbar,
        /// PauseMenu) no lo crea automáticamente como sí hace el menú del
        /// Editor, así que hay que añadirlo a mano una vez.
        /// </summary>
        private static void EnsureEventSystemExists()
        {
            if (EventSystem.current != null) return;

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Cambia el radio de carga en caliente (desde el menú de pausa); la descarga se deduce como radio+1.</summary>
        public void SetLoadRadius(int newLoadRadius)
        {
            loadRadiusInChunks = newLoadRadius;
            unloadRadiusInChunks = newLoadRadius + 1;

            if (player != null)
            {
                Vector3Int playerChunk = World.WorldToChunkCoord(Vector3Int.FloorToInt(player.transform.position));
                streamer.SetRadii(loadRadiusInChunks, unloadRadiusInChunks, playerChunk, seed);
            }

            // Cambiar el radio desde el menú es una acción puntual, no
            // edición continua: se construye/oculta todo de golpe aquí mismo,
            // en vez de repartirlo en varios frames a 2 chunks/frame.
            worldRenderer.ProcessQueue(int.MaxValue);
        }

        private void TickStreamer()
        {
            if (player == null) return;
            Vector3Int playerChunk = World.WorldToChunkCoord(Vector3Int.FloorToInt(player.transform.position));
            streamer.Tick(playerChunk, seed);
        }

        private void Update()
        {
            worldRenderer.ProcessQueue(maxChunkRebuildsPerFrame);
            TickStreamer();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[Key.R].wasPressedThisFrame) RegenerateWorld();
            if (keyboard[Key.F5].wasPressedThisFrame) SaveWorld();
            if (keyboard[Key.F9].wasPressedThisFrame) LoadWorldFromDisk();
        }

        /// <summary>Crea un World nuevo con otra semilla, reutilizando (pooling) los GameObjects de chunk ya creados.</summary>
        private void RegenerateWorld()
        {
            // Rango acotado a propósito: NoiseUtils desplaza la muestra sumando
            // seed*0.1013f a la coordenada; con semillas cercanas a int.MaxValue
            // esa suma pierde precisión en float y el ruido sale casi plano.
            seed = Random.Range(0, 1_000_000);

            world = new World();
            worldRenderer.AttachToWorld(world);
            streamer.AttachToWorld(world);

            FinishLoadingWorld();
        }

        private void SaveWorld()
        {
            WorldSerializer.Save(world, seed);
            Debug.Log($"[GameBootstrap] Mundo guardado en {WorldSerializer.DefaultPath} ({world.Chunks.Count} chunks explorados).");
        }

        private void LoadWorldFromDisk()
        {
            if (!TryLoadSavedWorld())
                Debug.LogWarning("[GameBootstrap] No hay ningún mundo guardado (o es de otra versión de formato).");
        }

        /// <summary>Intenta cargar el save; si lo consigue, sustituye el World actual (con pooling) y deja todo listo para jugar.</summary>
        private bool TryLoadSavedWorld()
        {
            if (!WorldSerializer.TryLoad(out WorldSaveData saveData)) return false;

            seed = saveData.Seed;

            world = saveData.World;
            worldRenderer.AttachToWorld(world);
            streamer.AttachToWorld(world);

            FinishLoadingWorld();
            return true;
        }

        private void FinishLoadingWorld()
        {
            // Primer Tick centrado en el spawn: genera esa zona si hace falta
            // (mundo nuevo, o un save que no llegaba hasta aquí) y oculta lo
            // que un save grande traiga fuera del radio de descarga.
            Vector3Int spawnChunk = World.WorldToChunkCoord(new Vector3Int(SpawnColumnX, 0, SpawnColumnZ));
            streamer.Tick(spawnChunk, seed);

            // Coste único al arrancar/cargar/regenerar: se drena la cola entera
            // de golpe en vez de repartirla en varios frames, para que el
            // jugador no caiga a través de chunks todavía sin mesh/collider.
            worldRenderer.ProcessQueue(int.MaxValue);

            LogGenerationSummary();
            SpawnPlayer();
        }

        private void LogGenerationSummary()
        {
            int surfaceY = FindSurfaceHeight(SpawnColumnX, SpawnColumnZ);
            string surfaceInfo = surfaceY < 0
                ? "sin bloque sólido en la columna de spawn"
                : $"superficie en y={surfaceY} ({world.GetBlock(new Vector3Int(SpawnColumnX, surfaceY, SpawnColumnZ))})";

            Debug.Log(
                $"[GameBootstrap] Mundo en streaming: {world.Chunks.Count} chunks cargados, seed={seed}. " +
                $"Columna de spawn ({SpawnColumnX},{SpawnColumnZ}) -> {surfaceInfo}.");
        }

        private void SpawnPlayer()
        {
            if (player == null) return;

            int surfaceY = FindSurfaceHeight(SpawnColumnX, SpawnColumnZ);
            if (surfaceY < 0) surfaceY = 0; // sin superficie encontrada: cae desde el suelo del mundo en vez de fallar

            Vector3 spawnPosition = new(SpawnColumnX + 0.5f, surfaceY + 1f + spawnClearance, SpawnColumnZ + 0.5f);

            // Desactivar/reactivar el CharacterController evita que su estado
            // interno de colisión interfiera con el teletransporte de spawn.
            player.enabled = false;
            player.transform.position = spawnPosition;
            player.enabled = true;
        }

        /// <summary>Bloque sólido más alto de la columna, o -1 si toda la columna es aire.</summary>
        private int FindSurfaceHeight(int worldX, int worldZ)
        {
            for (int y = ChunkConstants.Height - 1; y >= 0; y--)
            {
                if (world.GetBlock(new Vector3Int(worldX, y, worldZ)) != BlockType.Air) return y;
            }
            return -1;
        }
    }
}
