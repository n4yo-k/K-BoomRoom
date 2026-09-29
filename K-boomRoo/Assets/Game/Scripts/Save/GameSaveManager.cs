using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DefusalGame.Bomb;
using DefusalGame.Data;
using DefusalGame.Gameplay;

namespace DefusalGame.Save
{
    /// <summary>
    /// Estado individual de cada nota/pista para serialización JSON.
    /// </summary>
    [Serializable]
    public class NoteSaveState
    {
        public string clueId = "";
        public string clueName = "";
        public bool isFound = false;
        public string revealedDigit = "";
        public int sequenceIndex = 0;
    }

    /// <summary>
    /// Estructura de datos serializable a JSON que contiene la información completa de la partida.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        [Header("Progreso y Escena")]
        public string currentScene = "SampleScene";
        public string levelName = "Nivel 1: El Despacho";
        public int levelProgress = 1;
        public string saveTimestamp = "";
        public List<string> completedLevels = new List<string>();

        [Header("Posición del Jugador (X, Y, Z)")]
        public float playerPosX = 0f;
        public float playerPosY = 1.0f;
        public float playerPosZ = 0f;

        [Header("Progreso de Notas / Pistas")]
        public int notesFoundCount = 0;
        public int totalNotesCount = 4;
        public float notesPercentage = 0f;
        public List<string> collectedClueIds = new List<string>();
        public List<NoteSaveState> notesState = new List<NoteSaveState>();

        [Header("Estado de la Bomba / Nivel")]
        public bool isBombDefused = false;
        public bool isBombExploded = false;
        public float timeRemaining = 300f;
        public string lastEnteredCode = "";

        [Header("Puntuación")]
        public int score = 0;
    }

    /// <summary>
    /// Gestor integral y persistente de guardado y carga de partidas en formato JSON local y PlayerPrefs.
    /// Guarda posición XYZ del jugador, porcentaje de notas encontradas, progreso general y escenas.
    /// Atajo global: Tecla [G] para guardar partida en cualquier momento.
    /// </summary>
    public class GameSaveManager : MonoBehaviour
    {
        public static GameSaveManager Instance { get; private set; }

        private const string PLAYER_PREFS_KEY = "DEFUSAL_GLOBAL_SAVE_JSON";
        private const string SAVE_FILE_NAME = "DefusalGame_SaveData.json";
        private const string LEGACY_ROOM2_FILE = "Room2_SaveData.json";

        [Header("Configuración de Guardado")]
        [Tooltip("Si es true, intenta cargar partida automáticamente al iniciar la escena")]
        public bool autoLoadOnStart = true;

        [Tooltip("Si es true, guarda automáticamente al recoger notas o completar objetivos")]
        public bool autoSaveOnMilestones = true;

        [Header("Referencias de Escena (Opcionales - Se autolocalizan si están vacías)")]
        public BombController bombController;

        [Header("Estado en Memoria")]
        public GameSaveData currentData = new GameSaveData();

        /// <summary>
        /// Localiza automáticamente el BombController si no está asignado.
        /// </summary>
        public void FindSceneReferences()
        {
            if (bombController == null)
            {
                bombController = UnityEngine.Object.FindFirstObjectByType<BombController>();
            }
        }

        [Header("Notificación en Pantalla")]
        public string toastTitle = "";
        public string toastSubtitle = "";
        public string toastDetails = "";
        public float toastTimer = 0f;
        private const float TOAST_DURATION = 4.5f;

        // Eventos C#
        public event Action<GameSaveData> OnGameSaved;
        public event Action<GameSaveData> OnGameLoaded;
        public event Action OnSaveDataCleared;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        private string LegacySaveFilePath => Path.Combine(Application.persistentDataPath, LEGACY_ROOM2_FILE);

        // GUI procedimental para Toast y Atajos
        private Texture2D toastBgTex;
        private Texture2D toastBorderTex;
        private GUIStyle toastBoxStyle;
        private GUIStyle toastTitleStyle;
        private GUIStyle toastSubStyle;
        private GUIStyle toastDetailStyle;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else if (Instance != this)
            {
                if (GetComponent<Room2UIManager>() != null || GetComponent<Room2EscapeRoomManager>() != null)
                {
                    Destroy(this);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }
        }

        void Start()
        {
            // Cargar datos previos si existen
            if (HasSaveData())
            {
                LoadDataWithoutApplying();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // No auto-restaurar en MainMenu
            if (scene.name.Equals("MainMenu", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (autoLoadOnStart && HasSaveData())
            {
                // Si la escena que se acaba de cargar coincide con la escena guardada, restaurar posición y notas
                if (currentData != null && currentData.currentScene == scene.name)
                {
                    RestoreSceneState();
                }
            }
        }

        void Update()
        {
            // Actualizar temporizador de notificación
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
            }

            // Detección de tecla [G] para guardar partida en cualquier nivel
            bool gPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                gPressed = true;
            }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    gPressed = true;
                }
            }
            catch { }
#endif

            // Teclas de prueba adicionales (F5 Guardar, F9 Cargar, F12 Borrar)
            bool f5Pressed = false;
            bool f9Pressed = false;
            bool f12Pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f5Key.wasPressedThisFrame) f5Pressed = true;
                if (Keyboard.current.f9Key.wasPressedThisFrame) f9Pressed = true;
                if (Keyboard.current.f12Key.wasPressedThisFrame) f12Pressed = true;
            }
#else
            try
            {
                if (Input.GetKeyDown(KeyCode.F5)) f5Pressed = true;
                if (Input.GetKeyDown(KeyCode.F9)) f9Pressed = true;
                if (Input.GetKeyDown(KeyCode.F12)) f12Pressed = true;
            }
            catch { }
#endif

            string activeScene = SceneManager.GetActiveScene().name;
            if (!activeScene.Equals("MainMenu", StringComparison.OrdinalIgnoreCase))
            {
                if (gPressed || f5Pressed)
                {
                    SaveGame();
                }
                else if (f9Pressed)
                {
                    LoadGame();
                }
                else if (f12Pressed)
                {
                    ClearSaveData();
                }
            }
        }

        /// <summary>
        /// Localiza el transform del jugador en la escena activa (VR, PC Fallback o Main Camera).
        /// </summary>
        public Transform FindPlayerTransform()
        {
            var pc = UnityEngine.Object.FindFirstObjectByType<Player_PC_TestingFallback>();
            if (pc != null && pc.isActiveAndEnabled) return pc.transform;

            var antigravity = UnityEngine.Object.FindFirstObjectByType<AntigravityPlayerController>();
            if (antigravity != null && antigravity.isActiveAndEnabled) return antigravity.transform;

            var xr = GameObject.Find("XR Origin (XR Rig)");
            if (xr != null && xr.activeInHierarchy) return xr.transform;

            if (pc != null) return pc.transform;
            if (antigravity != null) return antigravity.transform;

            var cam = Camera.main;
            if (cam != null)
            {
                return cam.transform.parent != null ? cam.transform.parent : cam.transform;
            }
            return null;
        }

        /// <summary>
        /// Guarda el estado completo de la partida en JSON y PlayerPrefs.
        /// </summary>
        [ContextMenu("Guardar Partida (SaveGame)")]
        public void SaveGame()
        {
            FindSceneReferences();
            string sceneName = SceneManager.GetActiveScene().name;
            currentData.currentScene = sceneName;
            currentData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 1. Asignar nombre descriptivo y número de nivel
            if (sceneName == "SampleScene")
            {
                currentData.levelName = "Nivel 1: El Despacho";
                currentData.levelProgress = 1;
            }
            else if (sceneName == "Room2")
            {
                currentData.levelName = "Nivel 2: Escape Room C4";
                currentData.levelProgress = 2;
            }
            else if (sceneName == "Level3_House")
            {
                currentData.levelName = "Nivel 3: La Casa Táctica";
                currentData.levelProgress = 3;
            }
            else
            {
                currentData.levelName = sceneName;
                currentData.levelProgress = 1;
            }

            // 2. Guardar posición del jugador
            Transform playerT = FindPlayerTransform();
            if (playerT != null)
            {
                currentData.playerPosX = playerT.position.x;
                currentData.playerPosY = playerT.position.y;
                currentData.playerPosZ = playerT.position.z;
            }

            // 3. Recopilar notas y calcular porcentaje
            currentData.notesState.Clear();
            var allVrNotes = UnityEngine.Object.FindObjectsByType<VRNoteInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allClues = UnityEngine.Object.FindObjectsByType<ClueInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            HashSet<string> collectedSet = new HashSet<string>(DefusalGameStateManager.CurrentState.collectedClueIds);

            foreach (var note in allVrNotes)
            {
                if (note != null && note.clueData != null)
                {
                    bool isFound = note.clueData.isCollected || collectedSet.Contains(note.clueData.clueId);
                    if (isFound) collectedSet.Add(note.clueData.clueId);

                    currentData.notesState.Add(new NoteSaveState
                    {
                        clueId = note.clueData.clueId,
                        clueName = note.clueData.clueName,
                        isFound = isFound,
                        revealedDigit = note.clueData.revealedValue,
                        sequenceIndex = note.clueData.sequenceIndex
                    });
                }
            }

            foreach (var clue in allClues)
            {
                if (clue != null && clue.clueData != null)
                {
                    bool isFound = clue.clueData.isCollected || collectedSet.Contains(clue.clueData.clueId);
                    if (isFound) collectedSet.Add(clue.clueData.clueId);

                    // Evitar duplicados en lista si ya estaba
                    if (!currentData.notesState.Exists(n => n.clueId == clue.clueData.clueId))
                    {
                        currentData.notesState.Add(new NoteSaveState
                        {
                            clueId = clue.clueData.clueId,
                            clueName = clue.clueData.clueName,
                            isFound = isFound,
                            revealedDigit = clue.clueData.revealedValue,
                            sequenceIndex = clue.clueData.sequenceIndex
                        });
                    }
                }
            }

            currentData.collectedClueIds = new List<string>(collectedSet);
            currentData.totalNotesCount = Mathf.Max(4, currentData.notesState.Count);
            currentData.notesFoundCount = currentData.collectedClueIds.Count;
            currentData.notesPercentage = currentData.totalNotesCount > 0 
                ? (float)currentData.notesFoundCount / currentData.totalNotesCount * 100f 
                : 0f;

            // 4. Estado de la bomba según el nivel
            var bombController = UnityEngine.Object.FindFirstObjectByType<BombController>();
            if (bombController != null)
            {
                currentData.timeRemaining = bombController.timeRemaining;
                currentData.isBombDefused = (bombController.currentState == BombState.Defused);
                currentData.isBombExploded = (bombController.currentState == BombState.Exploded);
                currentData.lastEnteredCode = bombController.enteredCode;

                if (currentData.isBombDefused && !currentData.completedLevels.Contains(sceneName))
                {
                    currentData.completedLevels.Add(sceneName);
                }
            }

            var l3Bomb = UnityEngine.Object.FindFirstObjectByType<Level3MultiStageBomb>();
            if (l3Bomb != null)
            {
                currentData.timeRemaining = l3Bomb.timeRemaining;
                currentData.isBombDefused = (l3Bomb.currentState == BombState.Defused);
                currentData.isBombExploded = (l3Bomb.currentState == BombState.Exploded);
                currentData.lastEnteredCode = l3Bomb.enteredCode;

                if (currentData.isBombDefused && !currentData.completedLevels.Contains(sceneName))
                {
                    currentData.completedLevels.Add(sceneName);
                }
            }

            // 5. Serializar a JSON y escribir en disco
            string json = JsonUtility.ToJson(currentData, true);

            try
            {
                File.WriteAllText(SaveFilePath, json);
                // Respaldo en nombre de archivo Room2 para compatibilidad
                File.WriteAllText(LegacySaveFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Error escribiendo archivo de guardado: {ex.Message}");
            }

            // 6. Guardar en PlayerPrefs
            PlayerPrefs.SetString(PLAYER_PREFS_KEY, json);
            PlayerPrefs.SetString("DEFUSAL_ROOM2_SAVE_JSON", json);
            PlayerPrefs.Save();

            // 7. Mostrar mensaje de progreso guardado
            ShowSaveToast(
                "💾 ¡PROGRESO GUARDADO CON ÉXITO!",
                $"{currentData.levelName}  |  Notas: {currentData.notesPercentage:F0}% ({currentData.notesFoundCount}/{currentData.totalNotesCount})",
                $"Posición XYZ: ({currentData.playerPosX:F2}, {currentData.playerPosY:F2}, {currentData.playerPosZ:F2})  |  {currentData.saveTimestamp}"
            );

            Debug.Log($"[GameSaveManager] Guardado exitoso: {currentData.levelName}, Pos: ({currentData.playerPosX:F2}, {currentData.playerPosY:F2}, {currentData.playerPosZ:F2}), Notas: {currentData.notesPercentage:F0}%");

            OnGameSaved?.Invoke(currentData);
        }

        /// <summary>
        /// Muestra la notificación flotante de guardado.
        /// </summary>
        public void ShowSaveToast(string title, string subtitle, string details)
        {
            toastTitle = title;
            toastSubtitle = subtitle;
            toastDetails = details;
            toastTimer = TOAST_DURATION;
        }

        /// <summary>
        /// Lee los datos guardados sin aplicarlos directamente.
        /// </summary>
        public bool LoadDataWithoutApplying()
        {
            string json = "";

            if (File.Exists(SaveFilePath))
            {
                try { json = File.ReadAllText(SaveFilePath); } catch { }
            }

            if (string.IsNullOrEmpty(json) && File.Exists(LegacySaveFilePath))
            {
                try { json = File.ReadAllText(LegacySaveFilePath); } catch { }
            }

            if (string.IsNullOrEmpty(json) && PlayerPrefs.HasKey(PLAYER_PREFS_KEY))
            {
                json = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
            }

            if (string.IsNullOrEmpty(json) && PlayerPrefs.HasKey("DEFUSAL_ROOM2_SAVE_JSON"))
            {
                json = PlayerPrefs.GetString("DEFUSAL_ROOM2_SAVE_JSON");
            }

            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                currentData = JsonUtility.FromJson<GameSaveData>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Carga los datos de guardado y carga la escena guardada si no estamos en ella.
        /// </summary>
        [ContextMenu("Cargar Partida (LoadGame)")]
        public bool LoadGame()
        {
            if (!LoadDataWithoutApplying())
            {
                Debug.LogWarning("[GameSaveManager] No hay datos guardados para cargar.");
                return false;
            }

            string activeScene = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(currentData.currentScene) && currentData.currentScene != activeScene)
            {
                SceneManager.LoadScene(currentData.currentScene);
            }
            else
            {
                RestoreSceneState();
            }

            ShowSaveToast(
                "📂 ¡PARTIDA CARGADA CORRECTAMENTE!",
                $"{currentData.levelName}  |  Notas: {currentData.notesPercentage:F0}% ({currentData.notesFoundCount}/{currentData.totalNotesCount})",
                $"Posición restaurada: ({currentData.playerPosX:F2}, {currentData.playerPosY:F2}, {currentData.playerPosZ:F2})"
            );

            OnGameLoaded?.Invoke(currentData);
            return true;
        }

        /// <summary>
        /// Restaura la posición del jugador, las notas recogidas y el estado de la bomba en la escena actual.
        /// </summary>
        public void RestoreSceneState()
        {
            // 1. Restaurar posición del jugador
            Transform playerT = FindPlayerTransform();
            if (playerT != null)
            {
                CharacterController cc = playerT.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerT.position = new Vector3(currentData.playerPosX, currentData.playerPosY, currentData.playerPosZ);
                if (cc != null) cc.enabled = true;
            }

            // 2. Restaurar pistas en DefusalGameStateManager
            if (currentData.collectedClueIds != null)
            {
                DefusalGameStateManager.CurrentState.collectedClueIds = new List<string>(currentData.collectedClueIds);
            }

            // 3. Restaurar pistas en componentes
            var allNotes = UnityEngine.Object.FindObjectsByType<VRNoteInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var note in allNotes)
            {
                if (note != null && note.clueData != null && currentData.collectedClueIds != null)
                {
                    bool wasFound = currentData.collectedClueIds.Contains(note.clueData.clueId);
                    note.clueData.isCollected = wasFound;
                }
            }

            var allClues = UnityEngine.Object.FindObjectsByType<ClueInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var clue in allClues)
            {
                if (clue != null && clue.clueData != null && currentData.collectedClueIds != null)
                {
                    bool wasFound = currentData.collectedClueIds.Contains(clue.clueData.clueId);
                    clue.clueData.isCollected = wasFound;
                }
            }

            // 4. Restaurar estado de bomba
            var bomb = UnityEngine.Object.FindFirstObjectByType<BombController>();
            if (bomb != null)
            {
                bomb.timeRemaining = Mathf.Max(1f, currentData.timeRemaining);
                bomb.enteredCode = currentData.lastEnteredCode ?? "";
                if (currentData.isBombDefused) bomb.currentState = BombState.Defused;
                else if (currentData.isBombExploded) bomb.currentState = BombState.Exploded;
                else bomb.currentState = BombState.Armed;
            }

            var l3Bomb = UnityEngine.Object.FindFirstObjectByType<Level3MultiStageBomb>();
            if (l3Bomb != null)
            {
                l3Bomb.timeRemaining = Mathf.Max(1f, currentData.timeRemaining);
                l3Bomb.enteredCode = currentData.lastEnteredCode ?? "";
                if (currentData.isBombDefused) l3Bomb.currentState = BombState.Defused;
                else if (currentData.isBombExploded) l3Bomb.currentState = BombState.Exploded;
                else l3Bomb.currentState = BombState.Armed;
            }

            Debug.Log($"[GameSaveManager] Estado restaurado: Posición ({currentData.playerPosX:F2}, {currentData.playerPosY:F2}, {currentData.playerPosZ:F2}), Notas: {currentData.collectedClueIds?.Count}");
        }

        /// <summary>
        /// Borra los datos guardados en disco y PlayerPrefs.
        /// </summary>
        [ContextMenu("Borrar Datos Guardados (ClearSaveData)")]
        public void ClearSaveData()
        {
            try { if (File.Exists(SaveFilePath)) File.Delete(SaveFilePath); } catch { }
            try { if (File.Exists(LegacySaveFilePath)) File.Delete(LegacySaveFilePath); } catch { }

            if (PlayerPrefs.HasKey(PLAYER_PREFS_KEY)) PlayerPrefs.DeleteKey(PLAYER_PREFS_KEY);
            if (PlayerPrefs.HasKey("DEFUSAL_ROOM2_SAVE_JSON")) PlayerPrefs.DeleteKey("DEFUSAL_ROOM2_SAVE_JSON");
            PlayerPrefs.Save();

            currentData = new GameSaveData();
            DefusalGameStateManager.ResetState();

            ShowSaveToast("🗑 DATOS DE GUARDADO BORRADOS", "Se ha reiniciado el progreso de la partida.", "");
            OnSaveDataCleared?.Invoke();
        }

        /// <summary>
        /// Retorna true si hay un archivo o PlayerPrefs de guardado válido.
        /// </summary>
        public bool HasSaveData()
        {
            return File.Exists(SaveFilePath) || File.Exists(LegacySaveFilePath) || PlayerPrefs.HasKey(PLAYER_PREFS_KEY) || PlayerPrefs.HasKey("DEFUSAL_ROOM2_SAVE_JSON");
        }

        /// <summary>
        /// Regresa al Menú Principal desbloqueando el cursor.
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Registra la recolección de una nota y opcionalmente autoguarda.
        /// </summary>
        public void NotifyClueCollected(string clueId)
        {
            if (!currentData.collectedClueIds.Contains(clueId))
            {
                currentData.collectedClueIds.Add(clueId);
            }

            if (autoSaveOnMilestones)
            {
                SaveGame();
            }
        }

        #region Renderizado GUI para Notificación de Guardado (Toast)
        private void InitToastStyles()
        {
            if (toastBoxStyle != null) return;

            toastBgTex = new Texture2D(1, 1);
            toastBgTex.SetPixel(0, 0, new Color(0.04f, 0.22f, 0.12f, 0.95f)); // Verde esmeralda oscuro
            toastBgTex.Apply();

            toastBorderTex = new Texture2D(1, 1);
            toastBorderTex.SetPixel(0, 0, new Color(0.15f, 0.85f, 0.40f, 1f));
            toastBorderTex.Apply();

            toastBoxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = toastBgTex },
                padding = new RectOffset(16, 16, 10, 10)
            };

            toastTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 1.0f, 0.6f) }
            };

            toastSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            toastDetailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.95f, 0.85f) }
            };
        }

        void OnGUI()
        {
            if (toastTimer <= 0f || string.IsNullOrEmpty(toastTitle)) return;

            InitToastStyles();

            float w = Mathf.Min(540f, Screen.width * 0.92f);
            float h = string.IsNullOrEmpty(toastDetails) ? 65f : 85f;
            float x = (Screen.width - w) * 0.5f;
            float y = 115f; // Justo debajo del HUD superior

            // Borde resaltado
            GUI.color = new Color(0.2f, 1.0f, 0.5f, Mathf.Clamp01(toastTimer));
            GUI.DrawTexture(new Rect(x - 2, y - 2, w + 4, h + 4), toastBorderTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x, y, w, h), toastBoxStyle);
            GUILayout.Label(toastTitle, toastTitleStyle);
            if (!string.IsNullOrEmpty(toastSubtitle)) GUILayout.Label(toastSubtitle, toastSubStyle);
            if (!string.IsNullOrEmpty(toastDetails)) GUILayout.Label(toastDetails, toastDetailStyle);
            GUILayout.EndArea();
        }
        #endregion
    }
}
