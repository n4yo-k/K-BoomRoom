using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StylizedRoom
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 2.4f;
        public float runSpeed = 4.0f;
        public float gravity = -9.81f;

        [Header("Look")]
        public float mouseSensitivity = 0.12f;
        public Transform cameraTransform;

        private CharacterController controller;
        private float verticalVelocity = 0.0f;
        private float cameraPitch = 0.0f;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            HandleLook();
            HandleMovement();
            HandleCursor();
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            Vector2 lookDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                lookDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            }
#else
            lookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (mouseSensitivity * 15f);
#endif

            // Rotación horizontal (Yaw) en el cuerpo del personaje
            transform.Rotate(Vector3.up * lookDelta.x);

            // Rotación vertical (Pitch) en la cámara
            cameraPitch -= lookDelta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -80.0f, 80.0f);
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            Vector2 inputDir = Vector2.zero;
            bool isRunning = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDir.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDir.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDir.x += 1f;
                isRunning = Keyboard.current.leftShiftKey.isPressed;
            }
#else
            inputDir.x = Input.GetAxisRaw("Horizontal");
            inputDir.y = Input.GetAxisRaw("Vertical");
            isRunning = Input.GetKey(KeyCode.LeftShift);
#endif

            if (inputDir.sqrMagnitude > 1f)
            {
                inputDir.Normalize();
            }

            float speed = isRunning ? runSpeed : walkSpeed;
            Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.y;

            if (controller.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = (move * speed) + (Vector3.up * verticalVelocity);
            controller.Move(motion * Time.deltaTime);
        }

        private void HandleCursor()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
            }

            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
#endif
        }
    }
}
