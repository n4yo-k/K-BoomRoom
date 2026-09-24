using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DefusalGame.Bomb;
using DefusalGame.Data;
using DefusalGame.Save;

namespace DefusalGame.Gameplay
{
    /// <summary>
    /// Gestor integral de la interfaz de usuario para el nivel Room2 (Escape Room VR & PC).
    /// Controla:
    /// 1. Interfaz Superior (HUD): "DISPOSITIVO C4 TÁCTICO", cronómetro regresivo, estado y 4 ranuras de pistas.
    /// 2. Prompt de Interacción: "[E / Click] Inspeccionar..." al acercarse a las notas.
    /// 3. Ventana emergente (Pop-up): "¿Deseas guardar la partida?" con "Guardar" y "Cancelar" (Tecla G en PC / Botón en VR).
    /// 4. Notificación temporal en pantalla: "Partida Guardada Exitosamente".
    /// 5. Modales de Victoria y Derrota con reinicio rápido.
    /// </summary>
    public class Room2UIManager : MonoBehaviour
    {
        public static Room2UIManager Instance { get; private set; }

        [Header("Referencias de Bomba y Pistas (4 Ranuras)")]
        public BombController bomb;
        public ClueData clue1;
        public ClueData clue2;
        public ClueData clue3;
        public ClueData clue4;

        [Header("Cámara y Efectos")]
        public Camera playerCamera;
        private float shakeDuration = 0f;
        private float shakeIntensity = 0.35f;
        private Vector3 originalCamLocalPos = Vector3.zero;
        private bool hasSavedCamPos = false;
        private float explosionFlashAlpha = 0f;
        private bool hasTriggeredExplosionVisuals = false;

        [Header("Estado de Interacción")]
        [Tooltip("Texto contextual cuando el jugador apunta a una nota o botón")]
        public string currentHoverPrompt = "";

        [Header("Pop-up de Guardado")]
        public bool isSavePopupOpen = false;

        [Header("Notificación Temporal (Toast)")]
        public string toastMessage = "";
        private float toastTimer = 0f;
        private const float TOAST_DURATION = 3.5f;

        // Texturas procedimentales para la GUI
        private Texture2D panelTex;
        private Texture2D greenBadgeTex;
        private Texture2D pendingBadgeTex;
        private Texture2D whiteTex;
        private Texture2D gameOverBgTex;
        private Texture2D popupBgTex;
        private Texture2D toastBgTex;
        private Texture2D saveBtnTex;
        private Texture2D cancelBtnTex;

        // Estilos GUI
        private GUIStyle panelStyle;
        private GUIStyle timerStyle;
        private GUIStyle timerUrgentStyle;
        private GUIStyle badgeFoundStyle;
        private GUIStyle badgePendingStyle;
        private GUIStyle codeSummaryStyle;
        private GUIStyle codeReadyStyle;
        private GUIStyle promptStyle;
        private GUIStyle popupBoxStyle;
        private GUIStyle popupTitleStyle;
        private GUIStyle popupDescStyle;
        private GUIStyle saveButtonStyle;
        private GUIStyle cancelButtonStyle;
        private GUIStyle toastBoxStyle;
        private GUIStyle toastTextStyle;
        private GUIStyle gameOverBoxStyle;
        private GUIStyle gameOverTitleStyle;
        private GUIStyle gameOverReasonStyle;
        private GUIStyle victoryTitleStyle;
        private GUIStyle restartBtnStyle;

        private CursorLockMode previousCursorMode = CursorLockMode.Locked;
        private bool previousCursorVisibility = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            FindReferences();
            CreateTextures();
        }

        void Start()
        {
            FindReferences();

            // Suscribirse a eventos de GameSaveManager
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.OnGameSaved += OnGameSavedHandler;
                GameSaveManager.Instance.OnGameLoaded += OnGameLoadedHandler;
            }
        }

        void OnDestroy()
        {
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.OnGameSaved -= OnGameSavedHandler;
                GameSaveManager.Instance.OnGameLoaded -= OnGameLoadedHandler;
            }
        }

        public void FindReferences()
        {
            if (bomb == null) bomb = UnityEngine.Object.FindFirstObjectByType<BombController>();

            if (playerCamera == null)
            {
                var pc = UnityEngine.Object.FindFirstObjectByType<Player_PC_TestingFallback>();
                if (pc != null && pc.playerCamera != null) playerCamera = pc.playerCamera;
                else playerCamera = Camera.main;
            }

            if (playerCamera != null && !hasSavedCamPos)
            {
                originalCamLocalPos = playerCamera.transform.localPosition;
                hasSavedCamPos = true;
            }
        }

        private void OnGameSavedHandler(GameSaveData data)
        {
            ShowToast("Partida Guardada Exitosamente");
        }

        private void OnGameLoadedHandler(GameSaveData data)
        {
            ShowToast("Partida Cargada Correctamente");
        }

        public void ShowToast(string message)
        {
            toastMessage = message;
            toastTimer = TOAST_DURATION;
        }

        void Update()
        {
            if (bomb == null)
            {
                bomb = UnityEngine.Object.FindFirstObjectByType<BombController>();
            }

            // Actualizar temporizador del toast
            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f)
                {
                    toastMessage = "";
                }
            }

            // Detectar tecla 'G' para abrir/cerrar Pop-up de guardado
            bool gPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
                gPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.G))
                gPressed = true;
#endif

            if (gPressed && (bomb == null || (bomb.currentState != BombState.Exploded && bomb.currentState != BombState.Defused)))
            {
                ToggleSavePopup();
            }

            // Detectar tecla 'R' para reiniciar tras victoria o derrota
            bool rPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                rPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.R))
                rPressed = true;
#endif

            if (rPressed && bomb != null && (bomb.currentState == BombState.Exploded || bomb.currentState == BombState.Defused))
            {
                RestartLevel();
            }

            // Efectos de explosión
            if (bomb != null && bomb.currentState == BombState.Exploded && !hasTriggeredExplosionVisuals)
            {
                hasTriggeredExplosionVisuals = true;
                shakeDuration = 1.8f;
                explosionFlashAlpha = 1.0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Camera Shake en explosión
            if (shakeDuration > 0f && playerCamera != null && hasSavedCamPos)
            {
                float factor = shakeDuration / 1.8f;
                playerCamera.transform.localPosition = originalCamLocalPos + UnityEngine.Random.insideUnitSphere * (shakeIntensity * factor);
                shakeDuration -= Time.deltaTime;
                if (shakeDuration <= 0f)
                {
                    playerCamera.transform.localPosition = originalCamLocalPos;
                }
            }

            // Desvanecimiento del destello
            if (explosionFlashAlpha > 0f)
            {
                explosionFlashAlpha = Mathf.Max(0f, explosionFlashAlpha - Time.deltaTime * 0.6f);
            }
        }

        #region Control del Pop-up de Guardado
        public void OpenSavePopup()
        {
            if (isSavePopupOpen) return;

            isSavePopupOpen = true;
            previousCursorMode = Cursor.lockState;
            previousCursorVisibility = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseSavePopup()
        {
            if (!isSavePopupOpen) return;

            isSavePopupOpen = false;
            Cursor.lockState = previousCursorMode;
            Cursor.visible = previousCursorVisibility;
        }

        public void ToggleSavePopup()
        {
            if (isSavePopupOpen) CloseSavePopup();
            else OpenSavePopup();
        }

        public void ConfirmSave()
        {
            CloseSavePopup();
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }
            else
            {
                Debug.LogWarning("[Room2UIManager] GameSaveManager no presente en escena para guardar.");
                ShowToast("Partida Guardada Exitosamente");
            }
        }

        public void CancelSave()
        {
            CloseSavePopup();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        #endregion

        #region Renderizado GUI y HUD
        private void CreateTextures()
        {
            if (panelTex != null) return;

            panelTex = MakeColorTex(new Color(0.07f, 0.09f, 0.12f, 0.92f));
            pendingBadgeTex = MakeColorTex(new Color(0.16f, 0.18f, 0.22f, 0.90f));
            greenBadgeTex = MakeColorTex(new Color(0.04f, 0.32f, 0.14f, 0.95f)); // Verde esmeralda intenso
            whiteTex = MakeColorTex(Color.white);
            gameOverBgTex = MakeColorTex(new Color(0.14f, 0.02f, 0.02f, 0.96f));
            popupBgTex = MakeColorTex(new Color(0.08f, 0.11f, 0.15f, 0.98f));
            toastBgTex = MakeColorTex(new Color(0.02f, 0.40f, 0.18f, 0.95f));
            saveBtnTex = MakeColorTex(new Color(0.10f, 0.60f, 0.25f, 0.95f));
            cancelBtnTex = MakeColorTex(new Color(0.45f, 0.15f, 0.15f, 0.95f));
        }

        private Texture2D MakeColorTex(Color col)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void InitStyles()
        {
            if (panelStyle != null) return;
            CreateTextures();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTex },
                padding = new RectOffset(14, 14, 8, 8)
            };

            timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.85f, 0.25f) }
            };

            timerUrgentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.25f, 0.25f) }
            };

            badgePendingStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = pendingBadgeTex, textColor = new Color(0.80f, 0.82f, 0.85f) },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4)
            };

            badgeFoundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = greenBadgeTex, textColor = new Color(0.20f, 1.0f, 0.50f) },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4)
            };

            codeSummaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            codeReadyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 1.0f, 0.4f) }
            };

            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.92f, 0.4f) }
            };

            popupBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = popupBgTex },
                padding = new RectOffset(25, 25, 20, 20)
            };

            popupTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.85f, 1.0f) }
            };

            popupDescStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.95f, 1.0f) }
            };

            saveButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = saveBtnTex, textColor = Color.white }
            };

            cancelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = cancelBtnTex, textColor = Color.white }
            };

            toastBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = toastBgTex },
                padding = new RectOffset(16, 16, 8, 8)
            };

            toastTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            gameOverBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = gameOverBgTex },
                padding = new RectOffset(30, 30, 25, 25)
            };

            gameOverTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.25f, 0.20f) }
            };

            gameOverReasonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.80f) }
            };

            victoryTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.25f, 1.0f, 0.45f) }
            };

            restartBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        void OnGUI()
        {
            InitStyles();

            // 1. Destello por explosión
            if (explosionFlashAlpha > 0.01f)
            {
                Color flashCol = (explosionFlashAlpha > 0.6f) ? new Color(1f, 0.9f, 0.7f, explosionFlashAlpha) : new Color(0.85f, 0.15f, 0.05f, explosionFlashAlpha);
                GUI.color = flashCol;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
                GUI.color = Color.white;
            }

            // 2. Pantalla de Derrota (GAME OVER)
            if (bomb != null && bomb.currentState == BombState.Exploded)
            {
                DrawGameOverModal();
                return;
            }

            // 3. Pantalla de Victoria
            if (bomb != null && bomb.currentState == BombState.Defused)
            {
                DrawVictoryModal();
                return;
            }

            // 4. HUD Superior Réplica de la Bomba (DISPOSITIVO C4 TÁCTICO + 4 Ranuras)
            DrawTopHUD();

            // 5. Prompt contextual de interacción [E / Click]
            DrawInteractionPrompt();

            // 6. Pop-up flotante de guardado de partida
            if (isSavePopupOpen)
            {
                DrawSavePopupModal();
            }

            // 7. Notificación temporal (Toast) "Partida Guardada Exitosamente"
            if (toastTimer > 0f && !string.IsNullOrEmpty(toastMessage))
            {
                DrawToastNotification();
            }
        }

        private void DrawTopHUD()
        {
            float hudWidth = Mathf.Min(840f, Screen.width * 0.96f);
            float hudHeight = 98f;
            float x = (Screen.width - hudWidth) * 0.5f;
            float y = 10f;

            GUILayout.BeginArea(new Rect(x, y, hudWidth, hudHeight), panelStyle);

            // FILA 1: TÍTULO, CRONÓMETRO REGRESIVO Y ESTADO DE LA BOMBA
            GUILayout.BeginHorizontal();

            GUILayout.Label("DISPOSITIVO C4 TÁCTICO", GUILayout.Width(210f));

            float t = (bomb != null) ? bomb.timeRemaining : 300f;
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            int cents = Mathf.FloorToInt((t * 100f) % 100f);

            bool isUrgent = t < 60f;
            bool blink = (t < 30f) ? ((Time.time * 4f) % 1f < 0.5f) : true;

            string timerString = $"⏱ {mins:00}:{secs:00}.{cents:00}";
            if (isUrgent && !blink) timerString = "⏱ --:--.--";

            GUILayout.Label(timerString, isUrgent ? timerUrgentStyle : timerStyle, GUILayout.ExpandWidth(true));

            string stateLabel = "<color=yellow>ESTADO: ARMADA</color>";
            if (t <= 0f) stateLabel = "<color=red>ESTADO: ¡DETONANDO!</color>";
            else if (bomb != null && bomb.currentState == BombState.Defused) stateLabel = "<color=#00FF66>ESTADO: NEUTRALIZADA</color>";

            GUILayout.Label(stateLabel, GUILayout.Width(200f));

            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // FILA 2: LAS 4 RANURAS DE PISTAS DE ROOM2
            bool c1 = (clue1 != null && clue1.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains(clue1?.clueId ?? "CLUE_ROOM2_01");
            bool c2 = (clue2 != null && clue2.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains(clue2?.clueId ?? "CLUE_ROOM2_02");
            bool c3 = (clue3 != null && clue3.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains(clue3?.clueId ?? "CLUE_ROOM2_03");
            bool c4 = (clue4 != null && clue4.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains(clue4?.clueId ?? "CLUE_ROOM2_04");

            string val1 = (clue1 != null && !string.IsNullOrEmpty(clue1.revealedValue)) ? clue1.revealedValue : "7";
            string val2 = (clue2 != null && !string.IsNullOrEmpty(clue2.revealedValue)) ? clue2.revealedValue : "3";
            string val3 = (clue3 != null && !string.IsNullOrEmpty(clue3.revealedValue)) ? clue3.revealedValue : "9";
            string val4 = (clue4 != null && !string.IsNullOrEmpty(clue4.revealedValue)) ? clue4.revealedValue : "5";

            GUILayout.BeginHorizontal();

            // Ranura 1: Mueble
            string text1 = c1 ? $"✓ 1. Mueble: [ {val1} ]" : "[ ] 1. Mueble";
            GUILayout.Box(text1, c1 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            // Ranura 2: Retrato
            string text2 = c2 ? $"✓ 2. Retrato: [ {val2} ]" : "[ ] 2. Retrato";
            GUILayout.Box(text2, c2 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            // Ranura 3: Libros
            string text3 = c3 ? $"✓ 3. Libros: [ {val3} ]" : "[ ] 3. Libros";
            GUILayout.Box(text3, c3 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            // Ranura 4: Atrio / Sala
            string text4 = c4 ? $"✓ 4. Atrio: [ {val4} ]" : "[ ] 4. Atrio";
            GUILayout.Box(text4, c4 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // FILA 3: RESUMEN DEL CÓDIGO
            int count = (c1 ? 1 : 0) + (c2 ? 1 : 0) + (c3 ? 1 : 0) + (c4 ? 1 : 0);
            if (count == 4)
            {
                GUILayout.Label($"★ ¡SECUENCIA DESCUBIERTA: {val1} {val2} {val3} {val4}! Vuelve a la mesa de la bomba y presiona ENT ★", codeReadyStyle);
            }
            else
            {
                string s1 = c1 ? val1 : "_";
                string s2 = c2 ? val2 : "_";
                string s3 = c3 ? val3 : "_";
                string s4 = c4 ? val4 : "_";
                GUILayout.Label($"Clave de desactivación ({count}/4):  [ {s1} ]  [ {s2} ]  [ {s3} ]  [ {s4} ]  |  Guarda con [G] o botón físico en VR", codeSummaryStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawInteractionPrompt()
        {
            if (string.IsNullOrEmpty(currentHoverPrompt) || isSavePopupOpen) return;

            float boxW = 380f;
            float boxH = 34f;
            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = Screen.height * 0.5f + 30f;

            GUI.color = new Color(0f, 0f, 0f, 0.80f);
            GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(boxX, boxY, boxW, boxH), currentHoverPrompt, promptStyle);
        }

        private void DrawSavePopupModal()
        {
            // Atenuar fondo
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float winW = 440f;
            float winH = 220f;
            float x = (Screen.width - winW) * 0.5f;
            float y = (Screen.height - winH) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, winW, winH), popupBoxStyle);

            GUILayout.Label("SISTEMA DE PERSISTENCIA // ROOM 2", popupTitleStyle);
            GUILayout.Space(12);

            GUILayout.Label("¿Deseas guardar la partida actual?", popupDescStyle);
            GUILayout.Space(6);
            GUILayout.Label("<size=12>Se registrará el tiempo restante, las notas descubiertas y tu posición actual.</size>", popupDescStyle);
            GUILayout.Space(22);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("✓ Guardar", saveButtonStyle, GUILayout.Height(42), GUILayout.ExpandWidth(true)))
            {
                ConfirmSave();
            }

            GUILayout.Space(16);

            if (GUILayout.Button("✕ Cancelar", cancelButtonStyle, GUILayout.Height(42), GUILayout.ExpandWidth(true)))
            {
                CancelSave();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<size=11><color=#888888>[Pulsa G para alternar ventana en PC]</color></size>", popupDescStyle);

            GUILayout.EndArea();
        }

        private void DrawToastNotification()
        {
            float alpha = Mathf.Clamp01(toastTimer / 0.5f);
            if (toastTimer < 0.8f) alpha = toastTimer / 0.8f;

            float w = 340f;
            float h = 42f;
            float x = (Screen.width - w) * 0.5f;
            float y = 118f; // Justo debajo del HUD superior

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, toastBoxStyle);
            GUI.Label(new Rect(x, y, w, h), $"✓ {toastMessage}", toastTextStyle);
            GUI.color = Color.white;
        }

        private void DrawGameOverModal()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float winW = Mathf.Min(560f, Screen.width * 0.90f);
            float winH = 290f;
            float x = (Screen.width - winW) * 0.5f;
            float y = (Screen.height - winH) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, winW, winH), gameOverBoxStyle);

            GUILayout.Label("💥 ¡LA BOMBA HA EXPLOTADO! 💥", gameOverTitleStyle);
            GUILayout.Space(8);

            string reason = !string.IsNullOrEmpty(bomb?.explosionReason) ? bomb.explosionReason : "El dispositivo detonó antes de ser neutralizado.";
            GUILayout.Label($"Causa de la detonación: {reason}", gameOverReasonStyle);
            GUILayout.Space(8);

            GUILayout.Label("La misión en Room2 ha fracasado. Revisa bien las 4 notas antes de ingresar el código en el teclado.", GUILayout.ExpandWidth(true));
            GUILayout.Space(20);

            if (GUILayout.Button("REINTENTAR MISIÓN [R]", restartBtnStyle, GUILayout.Height(50)))
            {
                RestartLevel();
            }

            GUILayout.EndArea();
        }

        private void DrawVictoryModal()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float winW = Mathf.Min(560f, Screen.width * 0.90f);
            float winH = 290f;
            float x = (Screen.width - winW) * 0.5f;
            float y = (Screen.height - winH) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, winW, winH), panelStyle);

            GUILayout.Label("🏆 ¡BOMBA DESACTIVADA CON ÉXITO! 🏆", victoryTitleStyle);
            GUILayout.Space(8);

            float t = (bomb != null) ? bomb.timeRemaining : 0f;
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            int cents = Mathf.FloorToInt((t * 100f) % 100f);

            GUILayout.Label($"¡Has neutralizado el dispositivo C4 de Room2! Tiempo restante: {mins:00}:{secs:00}.{cents:00}", codeReadyStyle);
            GUILayout.Space(8);

            GUILayout.Label("Recolectaste las 4 evidencias e ingresaste la secuencia táctica de 4 dígitos a tiempo.", GUILayout.ExpandWidth(true));
            GUILayout.Space(20);

            if (GUILayout.Button("JUGAR DE NUEVO [R]", restartBtnStyle, GUILayout.Height(50)))
            {
                RestartLevel();
            }

            GUILayout.EndArea();
        }
        #endregion
    }
}
