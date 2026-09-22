using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DefusalGame.Bomb;
using DefusalGame.Data;

namespace DefusalGame.Gameplay
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Referencias")]
        public BombController bomb;

        [Header("Pistas a Seguir en el HUD")]
        public ClueData clue1;
        public ClueData clue2;
        public ClueData clue3;
        public ClueData clue4;

        [Header("Efectos")]
        public Camera playerCamera;

        // Camera Shake y Flash de Explosión
        private float shakeDuration = 0f;
        private float shakeIntensity = 0.35f;
        private Vector3 originalCamLocalPos = Vector3.zero;
        private bool hasSavedCamPos = false;
        private float explosionFlashAlpha = 0f;
        private bool hasTriggeredExplosionVisuals = false;

        // Texturas GUI procedimentales
        private Texture2D panelTex;
        private Texture2D greenBadgeTex;
        private Texture2D pendingBadgeTex;
        private Texture2D whiteTex;
        private Texture2D gameOverBgTex;

        // Estilos GUI
        private GUIStyle panelStyle;
        private GUIStyle timerStyle;
        private GUIStyle timerUrgentStyle;
        private GUIStyle badgeFoundStyle;
        private GUIStyle badgePendingStyle;
        private GUIStyle codeSummaryStyle;
        private GUIStyle codeReadyStyle;
        private GUIStyle gameOverBoxStyle;
        private GUIStyle gameOverTitleStyle;
        private GUIStyle gameOverReasonStyle;
        private GUIStyle victoryTitleStyle;
        private GUIStyle restartBtnStyle;

        void Awake()
        {
            if (bomb == null) bomb = Object.FindAnyObjectByType<BombController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;

            if (playerCamera != null)
            {
                originalCamLocalPos = playerCamera.transform.localPosition;
                hasSavedCamPos = true;
            }

            CreateTextures();
        }

        void Start()
        {
            if (bomb == null) bomb = Object.FindAnyObjectByType<BombController>();
        }

        private void CreateTextures()
        {
            if (panelTex != null) return;

            panelTex = MakeColorTex(new Color(0.08f, 0.09f, 0.11f, 0.90f));
            pendingBadgeTex = MakeColorTex(new Color(0.18f, 0.19f, 0.22f, 0.92f));
            greenBadgeTex = MakeColorTex(new Color(0.06f, 0.28f, 0.12f, 0.95f));
            whiteTex = MakeColorTex(Color.white);
            gameOverBgTex = MakeColorTex(new Color(0.12f, 0.02f, 0.02f, 0.96f));
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
                padding = new RectOffset(12, 12, 8, 8)
            };

            timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.85f, 0.25f) } // Ámbar / Oro
            };

            timerUrgentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.2f, 0.2f) } // Rojo Alerta
            };

            badgePendingStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = pendingBadgeTex, textColor = new Color(0.85f, 0.85f, 0.80f) },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4)
            };

            badgeFoundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = greenBadgeTex, textColor = new Color(0.15f, 1.0f, 0.45f) }, // Verde esmeralda brillante
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

            gameOverBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = gameOverBgTex },
                padding = new RectOffset(30, 30, 25, 25)
            };

            gameOverTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.25f, 0.20f) }
            };

            gameOverReasonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.80f) }
            };

            victoryTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
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

        void Update()
        {
            if (bomb == null)
            {
                bomb = Object.FindAnyObjectByType<BombController>();
                if (bomb == null) return;
            }

            // Detección de Explosión para activar sacudida de pantalla y fogonazo
            if (bomb.currentState == BombState.Exploded)
            {
                if (!hasTriggeredExplosionVisuals)
                {
                    hasTriggeredExplosionVisuals = true;
                    shakeDuration = 1.6f;
                    explosionFlashAlpha = 1.0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            // Manejo de Camera Shake
            if (shakeDuration > 0f && playerCamera != null && hasSavedCamPos)
            {
                float factor = shakeDuration / 1.6f;
                playerCamera.transform.localPosition = originalCamLocalPos + Random.insideUnitSphere * (shakeIntensity * factor);
                shakeDuration -= Time.deltaTime;
                if (shakeDuration <= 0f)
                {
                    playerCamera.transform.localPosition = originalCamLocalPos;
                }
            }

            // Desvanecimiento del fogonazo
            if (explosionFlashAlpha > 0f)
            {
                explosionFlashAlpha = Mathf.Max(0f, explosionFlashAlpha - Time.deltaTime * 0.6f);
            }

            // Tecla 'R' para reiniciar la misión rápidamente
            bool pressRestart = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                pressRestart = true;
#else
            if (Input.GetKeyDown(KeyCode.R))
                pressRestart = true;
#endif

            if (pressRestart && (bomb.currentState == BombState.Exploded || bomb.currentState == BombState.Defused))
            {
                RestartGame();
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnGUI()
        {
            InitStyles();

            // 1. Fogonazo visual de explosión
            if (explosionFlashAlpha > 0.01f)
            {
                Color flashCol = (explosionFlashAlpha > 0.6f) ? new Color(1f, 0.9f, 0.7f, explosionFlashAlpha) : new Color(0.85f, 0.15f, 0.05f, explosionFlashAlpha);
                GUI.color = flashCol;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
                GUI.color = Color.white;
            }

            // 2. Pantalla Final si la bomba explotó (GAME OVER)
            if (bomb != null && bomb.currentState == BombState.Exploded)
            {
                DrawGameOverModal();
                return;
            }

            // 3. Pantalla Final si la bomba fue desactivada (VICTORIA)
            if (bomb != null && bomb.currentState == BombState.Defused)
            {
                DrawVictoryModal();
                return;
            }

            // 4. Si el dossier de la mesa está en inspección modal grande, ocultamos el HUD para no saturar
            var activeDossier = Object.FindAnyObjectByType<MissionDossier>();
            if (activeDossier != null && activeDossier.isInspecting)
            {
                return;
            }

            // 5. HUD SUPERIOR COMPLETO DEL JUGADOR
            DrawTopHUD();
        }

        private void DrawTopHUD()
        {
            // Panel superior centrado
            float hudWidth = Mathf.Min(820f, Screen.width * 0.96f);
            float hudHeight = 98f;
            float x = (Screen.width - hudWidth) * 0.5f;
            float y = 10f;

            GUILayout.BeginArea(new Rect(x, y, hudWidth, hudHeight), panelStyle);

            // FILA 1: CRONÓMETRO Y ESTADO DE LA BOMBA
            GUILayout.BeginHorizontal();

            GUILayout.Label("DISPOSITIVO C4 TÁCTICO", GUILayout.Width(200f));

            // Tiempo restante
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
            GUILayout.Label(stateLabel, GUILayout.Width(200f));

            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // FILA 2: LAS 4 PISTAS / OBJETIVOS A BUSCAR
            bool c1 = (clue1 != null && clue1.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_DESK_DRAWER");
            bool c2 = (clue2 != null && clue2.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_CORKBOARD");
            bool c3 = (clue3 != null && clue3.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_BOOKSHELF");
            bool c4 = (clue4 != null && clue4.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_TOOLBOX");

            GUILayout.BeginHorizontal();

            // Pista 1: Estudio (Cajón)
            string text1 = c1 ? "✓ 1. Cajón: [ 4 ]" : "[ ] 1. Estudio (Cajón)";
            GUILayout.Box(text1, c1 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            // Pista 2: Estudio (Pizarra)
            string text2 = c2 ? "✓ 2. Pizarra: [ 8 ]" : "[ ] 2. Estudio (Pizarra)";
            GUILayout.Box(text2, c2 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            // Pista 3: Archivo (Libros)
            string text3 = c3 ? "✓ 3. Libros: [ 2 ]" : "[ ] 3. Archivo (Libros)";
            GUILayout.Box(text3, c3 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            // Pista 4: Archivo (Herramientas)
            string text4 = c4 ? "✓ 4. Caja: [ 6 ]" : "[ ] 4. Archivo (Caja)";
            GUILayout.Box(text4, c4 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));

            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // FILA 3: RESUMEN DE CÓDIGO REUNIDO
            int count = (c1 ? 1 : 0) + (c2 ? 1 : 0) + (c3 ? 1 : 0) + (c4 ? 1 : 0);
            if (count == 4)
            {
                GUILayout.Label("★ ¡SECUENCIA DESCUBIERTA: 4 8 2 6! Vuelve a la mesa, pulsa las teclas y presiona ENT ★", codeReadyStyle);
            }
            else
            {
                string s1 = c1 ? "4" : "_";
                string s2 = c2 ? "8" : "_";
                string s3 = c3 ? "2" : "_";
                string s4 = c4 ? "6" : "_";
                GUILayout.Label($"Clave de desactivación ({count}/4):  [ {s1} ]  [ {s2} ]  [ {s3} ]  [ {s4} ]  |  Explora el Estudio y el Archivo", codeSummaryStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawGameOverModal()
        {
            // Fondo oscuro semitransparente
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

            string reason = !string.IsNullOrEmpty(bomb.explosionReason) ? bomb.explosionReason : "El dispositivo detonó antes de ser neutralizado.";
            GUILayout.Label($"Causa de la detonación: {reason}", gameOverReasonStyle);
            GUILayout.Space(8);

            GUILayout.Label("La misión ha fracasado. Recuerda revisar bien todas las evidencias antes de introducir la clave final.", GUILayout.ExpandWidth(true));
            GUILayout.Space(20);

            if (GUILayout.Button("REINTENTAR MISIÓN [R]", restartBtnStyle, GUILayout.Height(50)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }

        private void DrawVictoryModal()
        {
            // Fondo oscuro semitransparente
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

            GUILayout.Label($"¡Has salvado las instalaciones! Tiempo restante: {mins:00}:{secs:00}.{cents:00}", codeReadyStyle);
            GUILayout.Space(8);

            GUILayout.Label("Lograste recolectar las 4 evidencias e ingresar la secuencia correcta a tiempo.", GUILayout.ExpandWidth(true));
            GUILayout.Space(20);

            if (GUILayout.Button("JUGAR DE NUEVO [R]", restartBtnStyle, GUILayout.Height(50)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }
    }
}
