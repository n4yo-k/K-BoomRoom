using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DefusalGame.Save;

namespace DefusalGame.UI
{
    public enum MenuState
    {
        MainMenu,
        Levels
    }

    /// <summary>
    /// Gestor interactivo para la Pantalla de Menú y Pantalla de Niveles.
    /// Utiliza las imágenes de objRefs: MenuImg.jpg y LevelsImg.jpg.
    /// Soporta navegación fluida, botones interactivos, carga de progreso y selección de niveles.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager Instance { get; private set; }

        [Header("Imágenes de Fondo (objRefs)")]
        [Tooltip("Textura del Menú Principal (objRefs/MenuImg.jpg)")]
        public Texture2D menuImageTexture;
        [Tooltip("Textura de la Pantalla de Niveles (objRefs/LevelsImg.jpg)")]
        public Texture2D levelsImageTexture;

        [Header("Referencias Canvas UI (Opcional)")]
        public Canvas menuCanvas;
        public GameObject mainMenuPanel;
        public GameObject levelsPanel;
        public RawImage menuBackgroundRawImage;
        public RawImage levelsBackgroundRawImage;
        public Button playButton;
        public Button continueButton;
        public Button levelsButton;
        public Button quitButton;
        public Button level1Button;
        public Button level2Button;
        public Button level3Button;
        public Button backToMenuButton;

        [Header("Estado Actual del Menú")]
        public MenuState currentState = MenuState.MainMenu;

        [Header("Efectos de Audio")]
        public AudioClip clickSound;
        public AudioClip hoverSound;
        private AudioSource audioSource;

        // Texturas y estilos GUI procedimentales
        private Texture2D overlayTex;
        private Texture2D btnNormalTex;
        private Texture2D btnHoverTex;
        private Texture2D cardBgTex;
        private Texture2D greenBadgeTex;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle btnPrimaryStyle;
        private GUIStyle btnSecondaryStyle;
        private GUIStyle cardStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardDescStyle;
        private GUIStyle saveInfoStyle;

        private bool hasSaveData = false;
        private GameSaveData currentSaveData;

        void Awake()
        {
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Desbloquear cursor para navegación con ratón
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            LoadDefaultTextures();
            CreateProceduralTextures();
        }

        void Start()
        {
            // Asegurar que GameSaveManager existe
            if (GameSaveManager.Instance == null)
            {
                GameObject gsmObj = new GameObject("GameSaveManager");
                gsmObj.AddComponent<GameSaveManager>();
            }

            CheckSaveData();
            SetupCanvasBindings();
            ShowMenuState(MenuState.MainMenu);
        }

        private void LoadDefaultTextures()
        {
            if (menuImageTexture == null)
            {
                menuImageTexture = Resources.Load<Texture2D>("MenuImg");
                if (menuImageTexture == null)
                {
#if UNITY_EDITOR
                    menuImageTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/objRefs/MenuImg.jpg");
#endif
                }
            }

            if (levelsImageTexture == null)
            {
                levelsImageTexture = Resources.Load<Texture2D>("LevelsImg");
                if (levelsImageTexture == null)
                {
#if UNITY_EDITOR
                    levelsImageTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/objRefs/LevelsImg.jpg");
#endif
                }
            }
        }

        public void CheckSaveData()
        {
            if (GameSaveManager.Instance != null)
            {
                hasSaveData = GameSaveManager.Instance.HasSaveData();
                if (hasSaveData)
                {
                    GameSaveManager.Instance.LoadDataWithoutApplying();
                    currentSaveData = GameSaveManager.Instance.currentData;
                }
            }
        }

        private void SetupCanvasBindings()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButton.gameObject.SetActive(hasSaveData);
            }
            if (levelsButton != null) levelsButton.onClick.AddListener(OnLevelsClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (level1Button != null) level1Button.onClick.AddListener(OnLevel1Clicked);
            if (level2Button != null) level2Button.onClick.AddListener(OnLevel2Clicked);
            if (level3Button != null) level3Button.onClick.AddListener(OnLevel3Clicked);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (menuBackgroundRawImage != null && menuImageTexture != null)
            {
                menuBackgroundRawImage.texture = menuImageTexture;
            }

            if (levelsBackgroundRawImage != null && levelsImageTexture != null)
            {
                levelsBackgroundRawImage.texture = levelsImageTexture;
            }
        }

        public void ShowMenuState(MenuState state)
        {
            currentState = state;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(state == MenuState.MainMenu);
            if (levelsPanel != null) levelsPanel.SetActive(state == MenuState.Levels);
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #region Acciones de Botones
        /// <summary>
        /// Botón "JUGAR": Inicia la partida en la escena SampleScene (Nivel 1).
        /// </summary>
        public void OnPlayClicked()
        {
            PlaySound(clickSound);
            Debug.Log("[MainMenuManager] Iniciando juego en SampleScene...");
            SceneManager.LoadScene("SampleScene");
        }

        /// <summary>
        /// Botón "CONTINUAR": Carga la última escena guardada restaurando el progreso.
        /// </summary>
        public void OnContinueClicked()
        {
            PlaySound(clickSound);
            if (GameSaveManager.Instance != null && hasSaveData)
            {
                Debug.Log($"[MainMenuManager] Continuando partida guardada: {currentSaveData?.currentScene}");
                GameSaveManager.Instance.LoadGame();
            }
            else
            {
                SceneManager.LoadScene("SampleScene");
            }
        }

        /// <summary>
        /// Botón "NIVELES" / "OPCIONES": Muestra la pantalla de selección de niveles con LevelsImg.
        /// </summary>
        public void OnLevelsClicked()
        {
            PlaySound(clickSound);
            ShowMenuState(MenuState.Levels);
        }

        /// <summary>
        /// Botón "VOLVER AL MENÚ": Regresa al Menú Principal.
        /// </summary>
        public void OnBackToMenuClicked()
        {
            PlaySound(clickSound);
            ShowMenuState(MenuState.MainMenu);
        }

        /// <summary>
        /// Botón 1 de Niveles: Carga "SampleScene" (Nivel 1: El Despacho).
        /// </summary>
        public void OnLevel1Clicked()
        {
            PlaySound(clickSound);
            Debug.Log("[MainMenuManager] Cargando Nivel 1: SampleScene");
            SceneManager.LoadScene("SampleScene");
        }

        /// <summary>
        /// Botón 2 de Niveles: Carga "Room2" (Nivel 2: Escape Room C4).
        /// </summary>
        public void OnLevel2Clicked()
        {
            PlaySound(clickSound);
            Debug.Log("[MainMenuManager] Cargando Nivel 2: Room2");
            SceneManager.LoadScene("Room2");
        }

        /// <summary>
        /// Botón 3 de Niveles: Carga "Level3_House" (Nivel 3: La Casa Táctica).
        /// </summary>
        public void OnLevel3Clicked()
        {
            PlaySound(clickSound);
            Debug.Log("[MainMenuManager] Cargando Nivel 3: Level3_House");
            SceneManager.LoadScene("Level3_House");
        }

        /// <summary>
        /// Botón "SALIR": Cierra el juego.
        /// </summary>
        public void OnQuitClicked()
        {
            PlaySound(clickSound);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion

        #region Renderizado GUI y Fallback Visual
        private void CreateProceduralTextures()
        {
            overlayTex = MakeColorTex(new Color(0.04f, 0.05f, 0.08f, 0.82f));
            btnNormalTex = MakeColorTex(new Color(0.10f, 0.15f, 0.22f, 0.92f));
            btnHoverTex = MakeColorTex(new Color(0.18f, 0.55f, 0.35f, 0.95f));
            cardBgTex = MakeColorTex(new Color(0.06f, 0.08f, 0.12f, 0.88f));
            greenBadgeTex = MakeColorTex(new Color(0.05f, 0.40f, 0.18f, 0.95f));
        }

        private Texture2D MakeColorTex(Color col)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void InitGUIStyles()
        {
            if (titleStyle != null) return;
            CreateProceduralTextures();

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.055f), 28, 48),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.85f, 0.2f) } // Amarillo / Dorado K-Boom
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.025f), 14, 20),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 0.85f, 1.0f) }
            };

            btnPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.028f), 15, 22),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = btnNormalTex, textColor = Color.white },
                hover = { background = btnHoverTex, textColor = new Color(1f, 1f, 0.6f) },
                active = { background = greenBadgeTex, textColor = Color.yellow },
                padding = new RectOffset(16, 16, 10, 10)
            };

            btnSecondaryStyle = new GUIStyle(btnPrimaryStyle)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 13, 18),
                normal = { background = MakeColorTex(new Color(0.18f, 0.20f, 0.25f, 0.90f)), textColor = new Color(0.85f, 0.90f, 0.95f) }
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = cardBgTex },
                padding = new RectOffset(16, 16, 12, 12)
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.3f, 0.85f, 1.0f) }
            };

            cardDescStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) }
            };

            saveInfoStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = greenBadgeTex, textColor = Color.white },
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 6, 6)
            };
        }

        void OnGUI()
        {
            // Renderizado nativo asegurando que las imágenes MenuImg y LevelsImg se visualicen siempre
            InitGUIStyles();

            if (currentState == MenuState.MainMenu)
            {
                DrawMainMenuGUI();
            }
            else if (currentState == MenuState.Levels)
            {
                DrawLevelsGUI();
            }
        }

        private void DrawMainMenuGUI()
        {
            // 1. Imagen de fondo MenuImg (objRefs/MenuImg.jpg)
            if (menuImageTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), menuImageTexture, ScaleMode.ScaleAndCrop);
            }

            // Capa de oscurecimiento suave para legibilidad
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);
            GUI.color = Color.white;

            // 2. Título principal
            float headerH = Screen.height * 0.18f;
            GUILayout.BeginArea(new Rect(20, Screen.height * 0.06f, Screen.width - 40, headerH));
            GUILayout.Label("💣 K-BOOM ROOM 💣", titleStyle);
            GUILayout.Label("ESCAPE ROOM & DESACTIVA LA BOMBA", subtitleStyle);
            GUILayout.EndArea();

            // 3. Panel de Botones Central / Inferior Izquierdo
            float menuW = Mathf.Min(380f, Screen.width * 0.85f);
            float btnH = Mathf.Clamp(Screen.height * 0.065f, 44f, 60f);
            float spacing = Mathf.Clamp(Screen.height * 0.015f, 8f, 15f);

            float menuX = (Screen.width - menuW) * 0.5f;
            float menuY = Screen.height * 0.32f;

            GUILayout.BeginArea(new Rect(menuX, menuY, menuW, Screen.height * 0.60f));

            // Botón JUGAR
            if (GUILayout.Button("▶  JUGAR  (NUEVA PARTIDA)", btnPrimaryStyle, GUILayout.Height(btnH)))
            {
                OnPlayClicked();
            }
            GUILayout.Space(spacing);

            // Botón CONTINUAR (si hay guardado)
            if (hasSaveData && currentSaveData != null)
            {
                string infoStr = $"Cargar: {currentSaveData.levelName} ({currentSaveData.notesPercentage:F0}% Notas)";
                if (GUILayout.Button($"💾  CONTINUAR PARTIDA\n<size=11>{infoStr}</size>", btnPrimaryStyle, GUILayout.Height(btnH + 10)))
                {
                    OnContinueClicked();
                }
                GUILayout.Space(spacing);
            }

            // Botón OPCIONES / NIVELES
            if (GUILayout.Button("📑  SELECCIÓN DE NIVELES", btnSecondaryStyle, GUILayout.Height(btnH)))
            {
                OnLevelsClicked();
            }
            GUILayout.Space(spacing);

            // Botón SALIR
            if (GUILayout.Button("✕  SALIR DEL JUEGO", btnSecondaryStyle, GUILayout.Height(btnH)))
            {
                OnQuitClicked();
            }

            GUILayout.EndArea();

            // Mensaje de pie de página
            string hint = "Presiona [G] durante cualquier nivel para guardar tu progreso, posición y notas.";
            GUI.Label(new Rect(20, Screen.height - 35, Screen.width - 40, 25), hint, cardDescStyle);
        }

        private void DrawLevelsGUI()
        {
            // 1. Imagen de fondo LevelsImg (objRefs/LevelsImg.jpg)
            if (levelsImageTexture != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), levelsImageTexture, ScaleMode.ScaleAndCrop);
            }

            // Capa oscura suave
            GUI.color = new Color(0f, 0f, 0f, 0.40f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);
            GUI.color = Color.white;

            // 2. Encabezado
            GUILayout.BeginArea(new Rect(20, Screen.height * 0.05f, Screen.width - 40, Screen.height * 0.16f));
            GUILayout.Label("📑 SELECCIÓN DE NIVELES 📑", titleStyle);
            GUILayout.Label("Elige una misión para comenzar o practica cualquier habitación", subtitleStyle);
            GUILayout.EndArea();

            // 3. Grid de los 3 Niveles
            float totalW = Mathf.Min(1000f, Screen.width * 0.92f);
            float cardW = (totalW - 40f) / 3f;
            float cardH = Mathf.Clamp(Screen.height * 0.48f, 240f, 320f);
            float startX = (Screen.width - totalW) * 0.5f;
            float startY = Screen.height * 0.24f;

            // TARJETA 1: SampleScene
            DrawLevelCard(
                new Rect(startX, startY, cardW, cardH),
                "1. EL DESPACHO",
                "SampleScene",
                "Dificultad: Normal ★☆☆\n\nInvestiga el Estudio y el Archivo. Localiza las 4 evidencias ocultas para descubrir la clave de 4 dígitos y desactivar la bomba táctica C4.",
                OnLevel1Clicked
            );

            // TARJETA 2: Room2
            DrawLevelCard(
                new Rect(startX + cardW + 20f, startY, cardW, cardH),
                "2. ESCAPE ROOM C4",
                "Room2",
                "Dificultad: Avanzada ★★☆\n\nHabitación cerrada con sistema de notas VR, pistas con linterna ultravioleta, terminal de seguridad y cronómetro regresivo de alta tensión.",
                OnLevel2Clicked
            );

            // TARJETA 3: Level3_House
            DrawLevelCard(
                new Rect(startX + (cardW + 20f) * 2f, startY, cardW, cardH),
                "3. LA CASA TÁCTICA",
                "Level3_House",
                "Dificultad: Extrema ★★★\n\nBomba de etapas múltiples: desvía la sobretensión en la cocina, descongela alicates, corta el cable azul correcto e introduce la clave ultravioleta.",
                OnLevel3Clicked
            );

            // 4. Botón inferior VOLVER AL MENÚ
            float returnW = Mathf.Min(320f, Screen.width * 0.70f);
            float returnH = 46f;
            float returnX = (Screen.width - returnW) * 0.5f;
            float returnY = startY + cardH + 20f;

            if (GUI.Button(new Rect(returnX, returnY, returnW, returnH), "⬅  VOLVER AL MENÚ PRINCIPAL", btnPrimaryStyle))
            {
                OnBackToMenuClicked();
            }
        }

        private void DrawLevelCard(Rect rect, string title, string sceneTarget, string description, Action onPlay)
        {
            GUILayout.BeginArea(rect, cardStyle);

            GUILayout.Label(title, cardTitleStyle);
            GUILayout.Label($"<color=#888888>Escena: {sceneTarget}</color>", cardDescStyle);
            GUILayout.Space(6);

            GUILayout.Label(description, cardDescStyle, GUILayout.ExpandHeight(true));
            GUILayout.Space(10);

            if (GUILayout.Button("▶ JUGAR NIVEL", btnPrimaryStyle, GUILayout.Height(42)))
            {
                onPlay?.Invoke();
            }

            GUILayout.EndArea();
        }
        #endregion
    }
}
