using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DefusalGame.Save;
using DefusalGame.Bomb;

namespace DefusalGame.Gameplay
{
    /// <summary>
    /// Controlador robusto para pruebas en PC / Editor sin necesidad de visor VR.
    /// Garantiza cero excepciones del Input System (soporta tanto el nuevo Input System como el clásico),
    /// auto-desactivándose elegantemente si un visor VR está conectado.
    /// Incorpora locomoción Antigravity (WASD + Espacio/C) y atajos de guardado (F5, F9, F12).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Player_PC_TestingFallback : MonoBehaviour
    {
        [Header("Cámara y Vista")]
        public Camera playerCamera;
        public float mouseSensitivity = 0.12f;
        public float interactRange = 3.2f;

        [Header("Locomoción FPS")]
        public float moveSpeed = 3.2f;
        public float runSpeed = 5.0f;
        public float gravity = -9.81f;
        public float jumpForce = 4.5f;

        [Header("Interacción y Retícula")]
        public bool showCrosshair = true;
        public LayerMask interactableMask = ~0;

        private CharacterController characterController;
        private float verticalVelocity = 0f;
        private float cameraPitch = 0f;
        private bool isVRActive = false;
        private Texture2D crosshairTexture;
        private string hoverInfoText = "";
        private GUIStyle tooltipStyle;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                gameObject.layer = playerLayer;
            }

            // Textura para la retícula
            crosshairTexture = new Texture2D(1, 1);
            crosshairTexture.SetPixel(0, 0, Color.white);
            crosshairTexture.Apply();
        }

        void Start()
        {
            CheckAndConfigureVRFallback();
        }

        public void CheckAndConfigureVRFallback()
        {
            bool hasVRDevice = IsVRHeadsetConnected();

            if (hasVRDevice)
            {
                DeactivatePCFallback();
                return;
            }

            // Si hay un XR Origin activo con cámara en la escena y un headset activo, VR toma la prioridad total
            var xrCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in xrCameras)
            {
                if (c != playerCamera && (c.name.Contains("XR") || c.transform.parent?.name.Contains("XR") == true))
                {
                    if (hasVRDevice)
                    {
                        DeactivatePCFallback();
                        return;
                    }
                }
            }

            // Modo PC Editor activo sólo si NO hay visor VR
            ActivatePCMode();
        }

        private bool IsVRHeadsetConnected()
        {
            try
            {
                if (UnityEngine.XR.XRSettings.isDeviceActive) return true;

                var head = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
                if (head.isValid) return true;

                var leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                var rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                if (leftHand.isValid || rightHand.isValid) return true;

                if (UnityEngine.XR.Management.XRGeneralSettings.Instance != null &&
                    UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null &&
                    UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        private void DeactivatePCFallback()
        {
            isVRActive = true;
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                playerCamera.tag = "Untagged";
            }
            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
            if (characterController != null) characterController.enabled = false;

            // Desbloquear cursor para que la PC no capture el ratón ni la cámara en VR
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            enabled = false;
            Debug.Log("[Player_PC_TestingFallback] ¡Visor Meta Quest detectado! Desactivando WASD/Ratón de PC. Controles VR activos.");
        }

        private void ActivatePCMode()
        {
            isVRActive = false;
            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.tag = "MainCamera";
                var listener = playerCamera.GetComponent<AudioListener>();
                if (listener == null) playerCamera.gameObject.AddComponent<AudioListener>();
                else listener.enabled = true;
            }

            if (characterController != null) characterController.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[Player_PC_TestingFallback] Modo PC Editor activado con locomoción Antigravity. (Click para bloquear cursor, ESC para liberar).");
        }

        void Update()
        {
            if (isVRActive) return;

            // Soporte de tecla G para guardar partida en cualquier nivel
            bool gPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) gPressed = true;
#else
            try { if (Input.GetKeyDown(KeyCode.G)) gPressed = true; } catch { }
#endif
            if (gPressed)
            {
                if (GameSaveManager.Instance != null)
                {
                    GameSaveManager.Instance.SaveGame();
                }
            }

            // Si la ventana emergente de guardado está abierta, suspender movimiento y rotación de cámara
            if (Room2UIManager.Instance != null && Room2UIManager.Instance.isSavePopupOpen)
            {
                hoverInfoText = "";
                return;
            }

            HandleCursorToggle();
            HandleInputShortcuts();
            HandleMouseLook();
            HandleMovement();
            HandleRaycastInteraction();
        }

        private void HandleCursorToggle()
        {
            bool escapePressed = false;
            bool clickPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escapePressed = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) clickPressed = true;
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) escapePressed = true;
                if (Input.GetMouseButtonDown(0)) clickPressed = true;
            }
            catch { }
#endif

            if (escapePressed)
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
            }

            if (clickPressed && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleInputShortcuts()
        {
            // Atajos de Guardado sin lanzar excepciones
            bool savePressed = false;
            bool loadPressed = false;
            bool resetPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f5Key.wasPressedThisFrame) savePressed = true;
                if (Keyboard.current.f9Key.wasPressedThisFrame) loadPressed = true;
                if (Keyboard.current.f12Key.wasPressedThisFrame) resetPressed = true;
            }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.F5)) savePressed = true;
                if (Input.GetKeyDown(KeyCode.F9)) loadPressed = true;
                if (Input.GetKeyDown(KeyCode.F12)) resetPressed = true;
            }
            catch { }
#endif

            if (savePressed && GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }
            else if (loadPressed && GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.LoadGame();
            }
            else if (resetPressed && GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.ClearSaveData();
            }

            // Atajos de teclado físico (0-9, Backspace/C, Enter) para la bomba cuando el jugador está cerca
            HandleBombKeypadShortcuts();
        }

        private void HandleBombKeypadShortcuts()
        {
            var bomb = UnityEngine.Object.FindAnyObjectByType<BombController>();
            if (bomb == null || bomb.currentState != BombState.Armed) return;

            // Solo si el jugador está en la sala de la bomba (a menos de 4.5 metros)
            if (Vector3.Distance(transform.position, bomb.transform.position) > 4.5f) return;

            string keyToSend = null;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) keyToSend = "1";
                else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) keyToSend = "2";
                else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) keyToSend = "3";
                else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) keyToSend = "4";
                else if (Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame) keyToSend = "5";
                else if (Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame) keyToSend = "6";
                else if (Keyboard.current.digit7Key.wasPressedThisFrame || Keyboard.current.numpad7Key.wasPressedThisFrame) keyToSend = "7";
                else if (Keyboard.current.digit8Key.wasPressedThisFrame || Keyboard.current.numpad8Key.wasPressedThisFrame) keyToSend = "8";
                else if (Keyboard.current.digit9Key.wasPressedThisFrame || Keyboard.current.numpad9Key.wasPressedThisFrame) keyToSend = "9";
                else if (Keyboard.current.digit0Key.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame) keyToSend = "0";
                else if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame) keyToSend = "C";
                else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame) keyToSend = "ENT";
            }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) keyToSend = "1";
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) keyToSend = "2";
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) keyToSend = "3";
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) keyToSend = "4";
                else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) keyToSend = "5";
                else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) keyToSend = "6";
                else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) keyToSend = "7";
                else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) keyToSend = "8";
                else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) keyToSend = "9";
                else if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) keyToSend = "0";
                else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Backspace)) keyToSend = "C";
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) keyToSend = "ENT";
            }
            catch { }
#endif

            if (!string.IsNullOrEmpty(keyToSend))
            {
                bomb.OnKeyPressed(keyToSend);

                // Animar el botón correspondiente si está visible
                var buttons = UnityEngine.Object.FindObjectsByType<BombKeypadButton>(FindObjectsSortMode.None);
                foreach (var b in buttons)
                {
                    if (b.keyValue == keyToSend)
                    {
                        b.Press();
                        break;
                    }
                }
            }
        }

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || playerCamera == null) return;

            Vector2 delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            }
#else
            try
            {
                delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * (mouseSensitivity * 15f);
            }
            catch { }
#endif

            transform.Rotate(Vector3.up * delta.x);

            cameraPitch -= delta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);
            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
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
            try
            {
                inputDir.x = Input.GetAxisRaw("Horizontal");
                inputDir.y = Input.GetAxisRaw("Vertical");
                isRunning = Input.GetKey(KeyCode.LeftShift);
            }
            catch { }
#endif

            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            float speed = isRunning ? runSpeed : moveSpeed;
            Vector3 move = (transform.right * inputDir.x) + (transform.forward * inputDir.y);

            if (characterController != null && characterController.enabled)
            {
                if (characterController.isGrounded)
                {
                    if (verticalVelocity < 0f) verticalVelocity = -2.0f;

                    bool jump = false;
#if ENABLE_INPUT_SYSTEM
                    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jump = true;
#else
                    try { if (Input.GetKeyDown(KeyCode.Space)) jump = true; } catch { }
#endif
                    if (jump) verticalVelocity = jumpForce;
                }

                verticalVelocity += gravity * Time.deltaTime;
                Vector3 motion = (move * speed) + (Vector3.up * verticalVelocity);
                characterController.Move(motion * Time.deltaTime);
            }
        }

        private void HandleRaycastInteraction()
        {
            hoverInfoText = "";
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableMask);

            BombKeypadButton hoveredBtn = null;
            VRNoteInteractable hoveredNote = null;
            AntigravityPhysicalButton hoveredSaveBtn = null;

            if (hasHit)
            {
                hoveredBtn = hit.collider.GetComponentInParent<BombKeypadButton>();
                hoveredNote = hit.collider.GetComponentInParent<VRNoteInteractable>();
                hoveredSaveBtn = hit.collider.GetComponentInParent<AntigravityPhysicalButton>();
            }

            // Asistencia magnética con SphereCast (6cm de radio) para apuntar cómodamente a las teclas
            if (hoveredBtn == null && hoveredNote == null && hoveredSaveBtn == null)
            {
                if (Physics.SphereCast(ray, 0.06f, out RaycastHit sphereHit, interactRange, interactableMask))
                {
                    var sBtn = sphereHit.collider.GetComponentInParent<BombKeypadButton>();
                    var sNote = sphereHit.collider.GetComponentInParent<VRNoteInteractable>();
                    var sSave = sphereHit.collider.GetComponentInParent<AntigravityPhysicalButton>();
                    if (sBtn != null || sNote != null || sSave != null)
                    {
                        hoveredBtn = sBtn;
                        hoveredNote = sNote;
                        hoveredSaveBtn = sSave;
                        hasHit = true;
                    }
                }
            }

            if (hoveredSaveBtn != null)
            {
                hoverInfoText = "[CLICK / E] Guardar Partida (Terminal 3D)";
            }
            else if (hoveredBtn != null)
            {
                hoverInfoText = $"[CLICK / E] Pulsar Tecla [{hoveredBtn.keyValue}]";
            }
            else if (hoveredNote != null)
            {
                hoverInfoText = "[E / Click] Inspeccionar...";
            }

            if (Room2UIManager.Instance != null)
            {
                Room2UIManager.Instance.currentHoverPrompt = hoverInfoText;
            }

            bool trigger = false;
#if ENABLE_INPUT_SYSTEM
            if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked) ||
                (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame))
            {
                trigger = true;
            }
#else
            try
            {
                if ((Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked) || Input.GetKeyDown(KeyCode.E))
                {
                    trigger = true;
                }
            }
            catch { }
#endif

            if (trigger && hasHit)
            {
                if (hoveredSaveBtn != null)
                {
                    hoveredSaveBtn.Press();
                }
                else if (hoveredBtn != null)
                {
                    hoveredBtn.Press();
                }
                else if (hoveredNote != null)
                {
                    hoveredNote.OnNoteInteracted();
                    hoveredNote.TogglePopup();
                }
            }
        }

        void OnGUI()
        {
            if (isVRActive || !showCrosshair || Cursor.lockState != CursorLockMode.Locked) return;

            // Retícula central
            float dotSize = 4f;
            float x = (Screen.width - dotSize) * 0.5f;
            float y = (Screen.height - dotSize) * 0.5f;
            GUI.color = new Color(0.2f, 1f, 0.8f, 0.9f);
            if (crosshairTexture != null)
            {
                GUI.DrawTexture(new Rect(x, y, dotSize, dotSize), crosshairTexture);
            }

            // Prompt de interacción
            if (!string.IsNullOrEmpty(hoverInfoText))
            {
                if (tooltipStyle == null)
                {
                    tooltipStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(1f, 0.92f, 0.4f) }
                    };
                }

                float boxW = 340f;
                float boxH = 30f;
                float boxX = (Screen.width - boxW) * 0.5f;
                float boxY = Screen.height * 0.5f + 25f;

                GUI.color = new Color(0f, 0f, 0f, 0.75f);
                GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(boxX, boxY, boxW, boxH), hoverInfoText, tooltipStyle);
            }
        }
    }
}
