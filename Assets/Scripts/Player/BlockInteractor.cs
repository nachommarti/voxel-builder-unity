using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxelBuilder
{
    /// <summary>
    /// Selección y edición de bloques mediante raycast desde la cámara. Vive en
    /// la cámara del jugador (no en el cuerpo): la mira es la dirección de
    /// vista, no la de movimiento. Clic izquierdo (Attack) rompe, clic derecho
    /// (Interact) coloca el bloque seleccionado.
    /// </summary>
    public class BlockInteractor : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private BlockHighlightRenderer highlight;

        [Header("Interacción")]
        [SerializeField] private float reach = 6f;

        // Solo se usa si no hay Hotbar en la escena (p. ej. probando este
        // componente de forma aislada). Con Hotbar presente, manda siempre
        // Hotbar.Instance.SelectedBlock.
        [SerializeField] private BlockType fallbackBlock = BlockType.Stone;

        // La cámara está dentro de la propia cápsula del CharacterController
        // (altura de ojos ~1.6 de una cápsula que va de 0 a 2). Con "Queries
        // Start In Colliders" activado (por defecto en Unity), un rayo que nace
        // dentro de un collider golpea ESE MISMO collider a distancia casi 0.
        // Sin excluir la capa del jugador, mirar hacia abajo detecta el propio
        // cuerpo en vez del bloque real.
        [SerializeField] private LayerMask raycastMask = ~0;

        private Camera cam;
        private CharacterController player;
        private InputAction breakAction;
        private InputAction placeAction;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            player = GetComponentInParent<CharacterController>();

            InputActionMap playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
            breakAction = playerMap.FindAction("Attack");
            placeAction = playerMap.FindAction("Interact");
        }

        private void OnEnable()
        {
            breakAction.Enable();
            placeAction.Enable();
        }

        private void OnDisable()
        {
            breakAction.Disable();
            placeAction.Disable();
        }

        private void Update()
        {
            // Con el cursor libre, ese mismo clic solo debe recapturarlo (ver
            // CursorLockController), no interpretarse como romper/colocar.
            if (!CursorLockController.IsLocked) return;

            UpdateHighlight();

            if (breakAction.WasPressedThisFrame()) TryBreakBlock();
            if (placeAction.WasPressedThisFrame()) TryPlaceBlock();
        }

        private void UpdateHighlight()
        {
            if (highlight == null) return;

            if (TryRaycast(out Vector3Int hitBlock, out _)) highlight.ShowAt(hitBlock);
            else highlight.Hide();
        }

        private void TryBreakBlock()
        {
            if (!TryRaycast(out Vector3Int hitBlock, out _)) return;
            World.Instance.SetBlock(hitBlock, BlockType.Air);
        }

        private void TryPlaceBlock()
        {
            if (!TryRaycast(out _, out Vector3Int placeBlock)) return;
            if (OverlapsPlayer(placeBlock)) return;

            BlockType blockToPlace = Hotbar.Instance != null ? Hotbar.Instance.SelectedBlock : fallbackBlock;
            World.Instance.SetBlock(placeBlock, blockToPlace);
        }

        /// <summary>
        /// Bloque golpeado y celda vacía adyacente donde colocaría uno nuevo.
        /// El punto de impacto cae justo en el plano de la cara, así que se
        /// desplaza medio bloque a cada lado de la normal antes de redondear
        /// hacia abajo: evita la ambigüedad de flotantes justo en el borde.
        /// </summary>
        private bool TryRaycast(out Vector3Int hitBlock, out Vector3Int placeBlock)
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, reach, raycastMask))
            {
                hitBlock = FloorToBlock(hit.point - hit.normal * 0.5f);
                placeBlock = FloorToBlock(hit.point + hit.normal * 0.5f);
                return true;
            }

            hitBlock = default;
            placeBlock = default;
            return false;
        }

        private bool OverlapsPlayer(Vector3Int blockPos)
        {
            if (player == null) return false;
            var blockBounds = new Bounds(blockPos + new Vector3(0.5f, 0.5f, 0.5f), Vector3.one);
            return blockBounds.Intersects(player.bounds);
        }

        private static Vector3Int FloorToBlock(Vector3 point) => new(
            Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y), Mathf.FloorToInt(point.z));
    }
}
