using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DefusalGame.Gameplay
{
    /// <summary>
    /// Controlador de locomoción y físicas Antigravity (Gravedad Cero / Microgravedad).
    /// Proporciona movimiento 3D con inercia, propulsión suave (thrusters), amortiguación cinemática
    /// y detección estricta de colisiones para impedir atravesar muros y suelos del modelo 3D.
    /// Funciona tanto en VR (mandos y visor) como en modo PC Fallback.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AntigravityPlayerController : MonoBehaviour
    {
        [Header("Propulsión y Flotación Antigravity")]
        [Tooltip("Velocidad de aceleración en traslación 3D")]
        public float thrustPower = 4.0f;

        [Tooltip("Velocidad máxima de desplazamiento en gravedad cero")]
        public float maxFloatSpeed = 5.5f;

        [Tooltip("Multiplicador de impulso o boost (ej. Shift en PC o botón de propulsión VR)")]
        public float boostMultiplier = 1.8f;

        [Tooltip("Fricción / Amortiguación inercial (0 = sin fricción, 1+ = frena suavemente)")]
        [Range(0.1f, 5f)]
        public float damping = 1.6f;

        [Tooltip("Fuerza de impulso vertical para flotar hacia arriba o abajo")]
        public float verticalThrust = 3.2f;

        [Header("Rotación y Orientación")]
        public Transform headTransform;
        public float rotationSpeed = 90f;
        public float mouseSensitivity = 0.12f;

        [Header("Colisiones y Capas")]
        [Tooltip("Capas con las que el cuerpo del jugador interactúa físicamente")]
        public LayerMask obstacleMask = ~0;

        [Tooltip("Fuerza de rebote suave al chocar con una superficie o propelerse contra una pared")]
        public float surfacePushOffForce = 2.5f;

        private CharacterController characterController;
        private Vector3 currentVelocity = Vector3.zero;
        private float pitch = 0f;
        private bool isVRMode = false;

        public Vector3 Velocity => currentVelocity;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (headTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) headTransform = cam.transform;
            }

            // Asegurar asignación de capa Player si existe
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                gameObject.layer = playerLayer;
            }

            // Excluir capa Player de los obstáculos a chocar consigo mismo
            if (playerLayer >= 0)
            {
                obstacleMask &= ~(1 << playerLayer);
            }
            int handsLayer = LayerMask.NameToLayer("Hands");
            if (handsLayer >= 0)
            {
                obstacleMask &= ~(1 << handsLayer);
            }
        }

        void Start()
        {
            CheckVRStatus();
        }

        private void CheckVRStatus()
        {
#if UNITY_2020_1_OR_NEWER
            var xrDisplay = UnityEngine.XR.XRSettings.isDeviceActive;
            var xrLoaded = UnityEngine.XR.Management.XRGeneralSettings.Instance != null &&
                           UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null &&
                           UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader != null;
            isVRMode = xrDisplay || xrLoaded;
#else
            isVRMode = UnityEngine.XR.XRDevice.isPresent;
#endif
        }

        void Update()
        {
            HandleRotation();
            HandleFloatingMotion();
        }

        private void HandleRotation()
        {
            // Si no estamos en VR y el cursor está bloqueado, controlar orientación con ratón
            if (!isVRMode && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 lookDelta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    lookDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
                }
#else
                try
                {
                    lookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (mouseSensitivity * 15f);
                }
                catch { }
#endif

                transform.Rotate(Vector3.up * lookDelta.x);

                pitch -= lookDelta.y;
                pitch = Mathf.Clamp(pitch, -85f, 85f);
                if (headTransform != null)
                {
                    headTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }
        }

        private void HandleFloatingMotion()
        {
            Vector3 inputThrust = Vector3.zero;
            bool isBoosted = false;

            // 1. Obtener entradas de teclado / gamepad
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputThrust.z += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputThrust.z -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputThrust.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputThrust.x += 1f;

                // Flotación vertical: Espacio = Arriba, C / Ctrl = Abajo
                if (Keyboard.current.spaceKey.isPressed) inputThrust.y += 1f;
                if (Keyboard.current.cKey.isPressed || Keyboard.current.leftCtrlKey.isPressed) inputThrust.y -= 1f;

                isBoosted = Keyboard.current.leftShiftKey.isPressed;
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                inputThrust.x += stick.x;
                inputThrust.z += stick.y;
                if (Gamepad.current.rightTrigger.isPressed) inputThrust.y += Gamepad.current.rightTrigger.ReadValue();
                if (Gamepad.current.leftTrigger.isPressed) inputThrust.y -= Gamepad.current.leftTrigger.ReadValue();
            }
#else
            try
            {
                inputThrust.x = Input.GetAxisRaw("Horizontal");
                inputThrust.z = Input.GetAxisRaw("Vertical");
                if (Input.GetKey(KeyCode.Space)) inputThrust.y += 1f;
                if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl)) inputThrust.y -= 1f;
                isBoosted = Input.GetKey(KeyCode.LeftShift);
            }
            catch { }
#endif

            // 2. Normalizar y transformar al espacio de vista relativo
            if (inputThrust.sqrMagnitude > 1f)
            {
                inputThrust.Normalize();
            }

            Transform refTransform = (headTransform != null) ? headTransform : transform;
            Vector3 worldThrust = refTransform.right * inputThrust.x + 
                                  refTransform.forward * inputThrust.z + 
                                  Vector3.up * inputThrust.y;

            float currentThrustPower = thrustPower * (isBoosted ? boostMultiplier : 1.0f);

            // 3. Aplicar aceleración por propulsión antigravitatoria
            if (worldThrust.sqrMagnitude > 0.01f)
            {
                currentVelocity += worldThrust * currentThrustPower * Time.deltaTime;
            }

            // 4. Aplicar amortiguación inercial
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, damping * Time.deltaTime);

            // 5. Limitar velocidad terminal
            float maxSpeed = maxFloatSpeed * (isBoosted ? boostMultiplier : 1.0f);
            if (currentVelocity.magnitude > maxSpeed)
            {
                currentVelocity = currentVelocity.normalized * maxSpeed;
            }

            // 6. Mover con CharacterController (resuelve colisiones físicas con paredes y suelos del OBJ)
            if (characterController != null && characterController.enabled)
            {
                CollisionFlags flags = characterController.Move(currentVelocity * Time.deltaTime);

                // Si choca de frente con un obstáculo o pared, amortiguar velocidad en esa dirección
                if ((flags & CollisionFlags.Sides) != 0 || (flags & CollisionFlags.Above) != 0 || (flags & CollisionFlags.Below) != 0)
                {
                    currentVelocity *= 0.5f;
                }
            }
        }

        /// <summary>
        /// Aplica un impulso cinético directo (ej. empujarse contra una pared o propulsión de mano).
        /// </summary>
        public void AddImpulse(Vector3 impulse)
        {
            currentVelocity += impulse;
        }

        /// <summary>
        /// Permite empujarse físicamente alejándose del punto de impacto contra un muro o superficie.
        /// </summary>
        public void PushOffSurface(Vector3 surfaceNormal, float strengthMultiplier = 1.0f)
        {
            Vector3 pushDir = surfaceNormal.normalized;
            AddImpulse(pushDir * (surfacePushOffForce * strengthMultiplier));
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Si colisiona con la arquitectura, evitar penetración y permitir amortiguar el rebote
            int roomLayer = LayerMask.NameToLayer("RoomArchitecture");
            if (roomLayer >= 0 && hit.gameObject.layer == roomLayer)
            {
                // Rebote suave elástico si la velocidad de impacto es considerable
                float dot = Vector3.Dot(currentVelocity, hit.normal);
                if (dot < -0.5f)
                {
                    currentVelocity -= hit.normal * dot * 0.4f;
                }
            }
        }
    }
}
