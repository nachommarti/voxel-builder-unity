using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxelBuilder
{
    /// <summary>
    /// Movimiento en primera persona simple: WASD (u otros bindings del mismo
    /// action map) + mirar con ratón + gravedad/salto sobre un CharacterController.
    /// No sabe nada de bloques ni de interacción; eso vive en BlockInteractor.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Camera playerCamera;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;

        [Header("Cámara")]
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float pitchLimit = 89f;

        private CharacterController controller;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;

        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

            InputActionMap playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
            moveAction = playerMap.FindAction("Move");
            lookAction = playerMap.FindAction("Look");
            jumpAction = playerMap.FindAction("Jump");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            lookAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            lookAction.Disable();
            jumpAction.Disable();
        }

        private void Update()
        {
            // Con el cursor libre (Escape) no tiene sentido seguir mirando ni
            // moviéndose: el ratón se está usando para otra cosa.
            if (!CursorLockController.IsLocked) return;

            ApplyLook();
            ApplyMove();
        }

        private void ApplyLook()
        {
            Vector2 lookDelta = lookAction.ReadValue<Vector2>() * mouseSensitivity;

            transform.Rotate(Vector3.up, lookDelta.x);

            pitch = Mathf.Clamp(pitch - lookDelta.y, -pitchLimit, pitchLimit);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        private void ApplyMove()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 inputDirection = new(moveInput.x, 0f, moveInput.y);
            if (inputDirection.sqrMagnitude > 1f) inputDirection.Normalize();

            Vector3 horizontalVelocity = transform.TransformDirection(inputDirection) * moveSpeed;
            UpdateVerticalVelocity();

            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateVerticalVelocity()
        {
            if (controller.isGrounded)
            {
                // Un pequeño empuje hacia abajo mantiene isGrounded estable en vez
                // de oscilar true/false por el propio movimiento del controller.
                verticalVelocity = -2f;

                if (jumpAction.WasPressedThisFrame())
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }
    }
}
