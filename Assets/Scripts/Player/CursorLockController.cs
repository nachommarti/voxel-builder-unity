using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VoxelBuilder
{
    /// <summary>
    /// Único responsable de bloquear/liberar el cursor. En el Editor, Unity
    /// libera el cursor automáticamente al pulsar Escape en el Game View —
    /// eso es una cortesía exclusiva del Editor, no ocurre en un build
    /// standalone. Sin este componente, en el ejecutable final el jugador
    /// quedaría con el cursor atrapado sin forma cómoda de soltarlo.
    ///
    /// Escape libera el cursor; un clic fuera de cualquier UI (izquierdo o
    /// derecho) lo recaptura. Un clic sobre un botón del menú de pausa NO
    /// recaptura: si no se excluyera, el mismo clic que pulsa "Salir" o "+/-"
    /// cerraría el menú de golpe antes de que el botón llegara a reaccionar.
    /// Otros sistemas (PlayerController, BlockInteractor) consultan IsLocked
    /// para no aplicar mirar/mover/interactuar mientras el cursor está libre.
    /// </summary>
    public class CursorLockController : MonoBehaviour
    {
        public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        private void OnEnable() => Lock();

        // En LateUpdate a propósito: así, el mismo clic que recaptura el
        // cursor ya ha pasado por el Update() de BlockInteractor con el
        // cursor todavía libre, y ese clic no se interpreta como "romper" o
        // "colocar" — solo sirve para recuperar el control.
        private void LateUpdate()
        {
            if (IsLocked)
            {
                if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame) Unlock();
            }
            else if (Mouse.current != null &&
                     (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame) &&
                     !IsPointerOverUI())
            {
                Lock();
            }
        }

        private static bool IsPointerOverUI() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private static void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
