using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DefusalGame.Bomb;

namespace DefusalGame.Gameplay
{
    public class Level3HUD : MonoBehaviour
    {
        [Header("Referencias")]
        public Level3MultiStageBomb bomb;
        public Camera playerCamera;

        // Camera Shake y Flash de Explosión
        private float shakeDuration = 0f;
        private float shakeIntensity = 0.4f;
        private Vector3 originalCamLocalPos;
        private bool hasSavedCamPos = false;
        private float explosionFlashAlpha = 0f;
        private bool hasTriggeredExplosion = false;

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
        private GUIStyle invStatusStyle;
        private GUIStyle gameOverBoxStyle;
        private GUIStyle gameOverTitleStyle;
        private GUIStyle gameOverReasonStyle;
        private GUIStyle victoryTitleStyle;
        private GUIStyle restartBtnStyle;

        void Awake()
        {
            if (bomb == null) bomb = Object.FindAnyObjectByType<Level3MultiStageBomb>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;

            if (playerCamera != null)
            {
                originalCamLocalPos = playerCamera.transform.localPosition;
                hasSavedCamPos = true;
            }

            CreateTextures();
        }

        private void CreateTextures()
        {
            if (panelTex != null) return;

            panelTex = MakeColorTex(new Color(0.06f, 0.07f, 0.09f, 0.92f));
            pendingBadgeTex = MakeColorTex(new Color(0.18f, 0.19f, 0.22f, 0.90f));
            greenBadgeTex = MakeColorTex(new Color(0.06f, 0.32f, 0.14f, 0.95f));
            whiteTex = MakeColorTex(Color.white);
            gameOverBgTex = MakeColorTex(new Color(0.14f, 0.02f, 0.02f, 0.97f));
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
                padding = new RectOffset(12, 12, 6, 6)
            };

            timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.82f, 0.2f) }
            };

            timerUrgentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.2f, 0.2f) }
            };

            badgePendingStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = pendingBadgeTex, textColor = new Color(0.85f, 0.85f, 0.80f) },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4)
            };

            badgeFoundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = greenBadgeTex, textColor = new Color(0.2f, 1.0f, 0.5f) },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 4, 4)
            };

            invStatusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.80f, 0.85f) }
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
                normal = { textColor = new Color(1.0f, 0.2f, 0.15f) }
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
            if (bomb == null) bomb = Object.FindAnyObjectByType<Level3MultiStageBomb>();
            if (bomb == null) return;

            if (bomb.currentState == BombState.Exploded)
            {
                if (!hasTriggeredExplosion)
                {
                    hasTriggeredExplosion = true;
                    shakeDuration = 1.6f;
                    explosionFlashAlpha = 1.0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

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

            if (explosionFlashAlpha > 0f)
            {
                explosionFlashAlpha = Mathf.Max(0f, explosionFlashAlpha - Time.deltaTime * 0.6f);
            }

            // Tecla R para reiniciar
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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnGUI()
        {
            InitStyles();

            if (explosionFlashAlpha > 0.01f)
            {
                Color flashCol = (explosionFlashAlpha > 0.6f) ? new Color(1f, 0.9f, 0.7f, explosionFlashAlpha) : new Color(0.85f, 0.15f, 0.05f, explosionFlashAlpha);
                GUI.color = flashCol;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
                GUI.color = Color.white;
            }

            if (bomb != null && bomb.currentState == BombState.Exploded)
            {
                DrawGameOverModal();
                return;
            }

            if (bomb != null && bomb.currentState == BombState.Defused)
            {
                DrawVictoryModal();
                return;
            }

            DrawTopHUD();
        }

        private void DrawTopHUD()
        {
            float hudWidth = Mathf.Min(840f, Screen.width * 0.96f);
            float hudHeight = 98f;
            float x = (Screen.width - hudWidth) * 0.5f;
            float y = 10f;

            GUILayout.BeginArea(new Rect(x, y, hudWidth, hudHeight), panelStyle);

            // FILA 1: CRONÓMETRO Y ESTADO
            GUILayout.BeginHorizontal();
            GUILayout.Label("PROTOCOLO SOBRECARGA (NIVEL 3)", GUILayout.Width(240f));

            float t = (bomb != null) ? bomb.timeRemaining : 210f;
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            int cents = Mathf.FloorToInt((t * 100f) % 100f);

            bool isUrgent = t < 45f;
            string timerString = $"⏱ {mins:00}:{secs:00}.{cents:00}";
            GUILayout.Label(timerString, isUrgent ? timerUrgentStyle : timerStyle, GUILayout.ExpandWidth(true));

            string st = (bomb != null && !bomb.isOvervoltageActive) ? "<color=green>VOLTAJE OK</color>" : "<color=red>¡SOBRETENSIÓN!</color>";
            GUILayout.Label(st, GUILayout.Width(180f));
            GUILayout.EndHorizontal();

            GUILayout.Space(3);

            // FILA 2: LAS 3 FASES HARDCORE
            bool f1 = bomb != null && !bomb.isOvervoltageActive;
            bool f2 = bomb != null && bomb.isWireCut;
            bool f3 = bomb != null && bomb.currentState == BombState.Defused;

            GUILayout.BeginHorizontal();

            string t1 = f1 ? "✓ FASE 1: Sobretensión Desviada" : "[ ] FASE 1: Cocina (Disyuntor B2)";
            GUILayout.Box(t1, f1 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));

            string t2 = f2 ? "✓ FASE 2: Cable Azul Cortado" : "[ ] FASE 2: Estudio (Alicates / Terminal)";
            GUILayout.Box(t2, f2 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));

            string t3 = f3 ? "✓ FASE 3: Clave [7531] Aceptada" : "[ ] FASE 3: Cuarto (Linterna UV / Clave)";
            GUILayout.Box(t3, f3 ? badgeFoundStyle : badgePendingStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));

            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // FILA 3: INVENTARIO Y GUÍA DE ACCIÓN
            var inv = PlayerInventory.Instance;
            string invHielo = (inv != null && inv.hasFrozenIceBlock) ? "<color=yellow>Hielo</color>" : "---";
            string invPliers = (inv != null && inv.hasPliers) ? "<color=green>Alicates</color>" : "---";
            string invUV = (inv != null && inv.hasUVLight) ? "<color=#8844FF>Linterna UV [F]</color>" : "---";

            string statusMsg = "";
            if (!f1) statusMsg = "Paso 1: Ve a la Cocina -> descongela alicates en microondas y baja el disyuntor B2.";
            else if (!f2) statusMsg = "Paso 2: Ve al Estudio -> revisa la Terminal PC y corta el cable AZUL con los alicates.";
            else statusMsg = "Paso 3: Ve al Cuarto -> usa la Linterna UV [F] para ver los 4 números e introdúcelos.";

            GUILayout.Label($"{statusMsg}  |  Inventario: [{invHielo}] [{invPliers}] [{invUV}]", invStatusStyle);

            GUILayout.EndArea();
        }

        private void DrawGameOverModal()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float winW = Mathf.Min(580f, Screen.width * 0.90f);
            float winH = 280f;
            float x = (Screen.width - winW) * 0.5f;
            float y = (Screen.height - winH) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, winW, winH), gameOverBoxStyle);

            GUILayout.Label("💥 ¡LA BOMBA HA EXPLOTADO! 💥", gameOverTitleStyle);
            GUILayout.Space(8);

            string reason = (bomb != null && !string.IsNullOrEmpty(bomb.explosionReason)) ? bomb.explosionReason : "El dispositivo detonó por fallo en el protocolo.";
            GUILayout.Label($"Causa: {reason}", gameOverReasonStyle);
            GUILayout.Space(12);

            if (GUILayout.Button("REINTENTAR NIVEL 3 [R]", restartBtnStyle, GUILayout.Height(50)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }

        private void DrawVictoryModal()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float winW = Mathf.Min(580f, Screen.width * 0.90f);
            float winH = 280f;
            float x = (Screen.width - winW) * 0.5f;
            float y = (Screen.height - winH) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, winW, winH), panelStyle);

            GUILayout.Label("🏆 ¡NIVEL 3 COMPLETADO! 🏆", victoryTitleStyle);
            GUILayout.Space(8);

            float t = (bomb != null) ? bomb.timeRemaining : 0f;
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            int cents = Mathf.FloorToInt((t * 100f) % 100f);

            GUILayout.Label($"¡Has superado el nivel más difícil! Tiempo restante: {mins:00}:{secs:00}.{cents:00}", timerStyle);
            GUILayout.Space(12);

            if (GUILayout.Button("JUGAR DE NUEVO [R]", restartBtnStyle, GUILayout.Height(50)))
            {
                RestartGame();
            }

            GUILayout.EndArea();
        }
    }
}
