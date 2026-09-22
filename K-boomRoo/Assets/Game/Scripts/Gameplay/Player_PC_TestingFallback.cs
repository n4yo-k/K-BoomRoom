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

        [Header("Locomoción Antigravity")]
        public float moveSpeed = 3.5f;
        public float verticalSpeed = 2.8f;
        public float boostMultiplier = 1.8f;
        public float damping = 1.8f;

        [Header("Interacción y Retícula")]
        public bool showCrosshair = true;
        public LayerMask interactableMask = ~0;

        private CharacterController characterController;
        private Vector3 currentVelocity = Vector3.zero;
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
            bool hasVRDevice = false;

            try
            {
#if UNITY_2020_1_OR_NEWER
                hasVRDevice = UnityEngine.XR.XRSettings.isDeviceActive || 
                             (UnityEngine.XR.Management.XRGeneralSettings.Instance != null &&
                              UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager != null &&
                              UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader != null);
#else
                hasVRDevice = UnityEngine.XR.XRDevice.isPresent;
#endif
            }
            catch
            {
                hasVRDevice = false;
            }

            // Si hay un XR Origin activo en la escena con cámara activa, asumimos que VR toma prioridad
            var xrCameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in xrCameras)
            {
                if (c != playerCamera && (c.name.Contains("XR") || c.name.Contains("Main Camera") || c.transform.parent?.name.Contains("XR") == true))
                {
                    if (hasVRDevice)
                    {
                        // Desactivar fallback PC para evitar cámaras duplicadas en VR
                        DeactivatePCFallback();
                        return;
                    }
                }
            }

            // Modo PC Editor activo
            ActivatePCMode();
        }

        private void DeactivatePCFallback()
        {
            isVRActive = true;
            if (playerCamera != null) playerCamera.enabled = false;
            var listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
            if (characterController != null) characterController.enabled = false;
            enabled = false;
            Debug.Log("[Player_PC_TestingFallback] Visor VR detectado. Desactivando controlador PC Fallback.");
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

            HandleCursorToggle();
            HandleInputShortcuts();
            HandleMouseLook();
            HandleAntigravityMovement();
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

        private void HandleAntigravityMovement()
        {
            Vector3 inputDir = Vector3.zero;
            bool isBoost = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDir.z += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDir.z -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDir.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDir.x += 1f;

                // Elevación / Descenso en gravedad cero
                if (Keyboard.current.spaceKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.cKey.isPressed || Keyboard.current.leftCtrlKey.isPressed) inputDir.y -= 1f;

                isBoost = Keyboard.current.leftShiftKey.isPressed;
            }
#else
            try
            {
                inputDir.x = Input.GetAxisRaw("Horizontal");
                inputDir.z = Input.GetAxisRaw("Vertical");
                if (Input.GetKey(KeyCode.Space)) inputDir.y += 1f;
                if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl)) inputDir.y -= 1f;
                isBoost = Input.GetKey(KeyCode.LeftShift);
            }
            catch { }
#endif

            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            Transform camT = (playerCamera != null) ? playerCamera.transform : transform;
            Vector3 worldMove = (transform.right * inputDir.x) + (transform.forward * inputDir.z) + (Vector3.up * inputDir.y);

            float speed = (isBoost ? moveSpeed * boostMultiplier : moveSpeed);
            if (worldMove.sqrMagnitude > 0.01f)
            {
                currentVelocity += worldMove * speed * 3.5f * Time.deltaTime;
            }

            // Amortiguación inercial
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, damping * Time.deltaTime);

            if (characterController != null && characterController.enabled)
            {
                CollisionFlags flags = characterController.Move(currentVelocity * Time.deltaTime);
                if (flags != CollisionFlags.None)
                {
                    currentVelocity *= 0.5f;
                }
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
                    hoverInfoText = $"[CLICK / E] Inspeccionar: {hoveredNote.clueData?.clueName ?? "Nota"}";
                }
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
