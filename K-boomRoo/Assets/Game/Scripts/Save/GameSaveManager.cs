using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DefusalGame.Bomb;
using DefusalGame.Data;
using DefusalGame.Gameplay;

namespace DefusalGame.Save
{
    /// <summary>
    /// Estado individual de cada una de las 4 notas para serialización JSON.
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
    /// Estructura de datos serializable a JSON que contiene toda la información de la partida.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        [Header("Progreso y Escena")]
        public string currentScene = "Room2";
        public string levelName = "Room2_EscapeRoom";
        public int levelProgress = 2;
        public string saveTimestamp = "";

        [Header("Posición del Jugador (X, Y, Z)")]
        public float playerPosX = 0f;
        public float playerPosY = 0.05f;
        public float playerPosZ = -1.25f;

        [Header("Estado de la Bomba")]
        public bool isBombDefused = false;
        public bool isBombExploded = false;
        public float timeRemaining = 300f;
        public string lastEnteredCode = "";

        [Header("Estado de las 4 Notas / Pistas")]
        public List<string> collectedClueIds = new List<string>();
        public List<NoteSaveState> notesState = new List<NoteSaveState>();

        [Header("Puntuación")]
        public int score = 0;
    }

    /// <summary>
    /// Gestor modular y reutilizable de guardado y carga de partidas en formato JSON local.
    /// Soporta persistencia en disco (Application.persistentDataPath) y PlayerPrefs como fallback/redundancia.
    /// </summary>
    public class GameSaveManager : MonoBehaviour
    {
        public static GameSaveManager Instance { get; private set; }

        private const string PLAYER_PREFS_KEY = "DEFUSAL_ROOM2_SAVE_JSON";
        private const string SAVE_FILE_NAME = "Room2_SaveData.json";

        [Header("Configuración de Guardado")]
        [Tooltip("Si es true, intenta cargar partida automáticamente al iniciar la escena si existe un guardado")]
        public bool autoLoadOnStart = false;

        [Tooltip("Si es true, guarda automáticamente cuando se recoge una pista o se desactiva la bomba")]
        public bool autoSaveOnMilestones = true;

        [Header("Referencias de Escena (Opcionales - Se autolocalizan si están vacías)")]
        public BombController bombController;

        [Header("Estado en Memoria")]
        public GameSaveData currentData = new GameSaveData();

        // Eventos C# para conectar con la UI u otros subsistemas de forma reactiva
        public event Action<GameSaveData> OnGameSaved;
        public event Action<GameSaveData> OnGameLoaded;
        public event Action OnSaveDataCleared;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

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

            FindSceneReferences();
        }

        void Start()
        {
            if (autoLoadOnStart && HasSaveData())
            {
                LoadGame();
            }
        }

        void Update()
        {
            // Atajos de prueba en teclado para facilitar verificación en PC / Editor / SteamVR:
            // F5: Guardar Partida
            // F9: Cargar Partida
            // F12: Borrar Datos de Guardado
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
            }
            else if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame();
            }
            else if (Input.GetKeyDown(KeyCode.F12))
            {
                ClearSaveData();
            }
        }

        /// <summary>
        /// Localiza automáticamente los componentes clave de la escena si no fueron asignados en el Inspector.
        /// </summary>
        public void FindSceneReferences()
        {
            if (bombController == null)
            {
                bombController = UnityEngine.Object.FindFirstObjectByType<BombController>();
            }
        }

        /// <summary>
        /// Localiza automáticamente el transform del jugador activo (VR u ordenador).
        /// </summary>
        public Transform FindPlayerTransform()
        {
            var pc = UnityEngine.Object.FindFirstObjectByType<Player_PC_TestingFallback>();
            if (pc != null && pc.isActiveAndEnabled) return pc.transform;

            var xr = GameObject.Find("XR Origin (XR Rig)");
            if (xr != null && xr.activeInHierarchy) return xr.transform;

            if (pc != null) return pc.transform;

            var cam = Camera.main;
            if (cam != null)
            {
                return cam.transform.parent != null ? cam.transform.parent : cam.transform;
            }
            return null;
        }

        /// <summary>
        /// Guarda el estado actual de la partida en formato JSON en disco y PlayerPrefs.
        /// Serializa: Nombre de Escena (Room2), Tiempo de Bomba, Estado de las 4 Notas y Posición XYZ del jugador.
        /// </summary>
        [ContextMenu("Guardar Partida (SaveGame)")]
        public void SaveGame()
        {
            FindSceneReferences();

            // 1. Recopilar datos de escena y nivel
            currentData.currentScene = SceneManager.GetActiveScene().name;
            currentData.levelName = "EscapeRoom_Room2";
            currentData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 2. Recopilar posición del jugador en X, Y, Z
            Transform playerT = FindPlayerTransform();
            if (playerT != null)
            {
                currentData.playerPosX = playerT.position.x;
                currentData.playerPosY = playerT.position.y;
                currentData.playerPosZ = playerT.position.z;
            }

            // 3. Recopilar datos de la bomba
            if (bombController != null)
            {
                currentData.timeRemaining = bombController.timeRemaining;
                currentData.isBombDefused = (bombController.currentState == BombState.Defused);
                currentData.isBombExploded = (bombController.currentState == BombState.Exploded);
                currentData.lastEnteredCode = bombController.enteredCode;
            }

            // 4. Recopilar estado de las 4 notas / pistas (cuáles encontradas y dígitos revelados)
            currentData.collectedClueIds = new List<string>(DefusalGameStateManager.CurrentState.collectedClueIds);
            currentData.notesState.Clear();

            var allVrNotes = UnityEngine.Object.FindObjectsByType<VRNoteInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var note in allVrNotes)
            {
                if (note != null && note.clueData != null)
                {
                    bool isFound = note.clueData.isCollected || currentData.collectedClueIds.Contains(note.clueData.clueId);
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

            // 5. Calcular puntaje (Tiempo restante * 10 + 500 por cada nota)
            int clueBonus = currentData.collectedClueIds.Count * 500;
            int timeBonus = Mathf.Max(0, Mathf.FloorToInt(currentData.timeRemaining * 10f));
            currentData.score = timeBonus + clueBonus + (currentData.isBombDefused ? 2000 : 0);

            // 6. Serializar a JSON
            string json = JsonUtility.ToJson(currentData, true);

            // 7. Guardar en disco persistente (Local JSON)
            try
            {
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[GameSaveManager] Partida guardada con éxito en JSON: {SaveFilePath}\nPosición: ({currentData.playerPosX:F2}, {currentData.playerPosY:F2}, {currentData.playerPosZ:F2}), Tiempo: {currentData.timeRemaining:F1}s, Notas: {currentData.notesState.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Error al escribir archivo de guardado: {ex.Message}");
            }

            // 8. Guardar en PlayerPrefs (Respaldo redundante para máxima compatibilidad)
            PlayerPrefs.SetString(PLAYER_PREFS_KEY, json);
            PlayerPrefs.Save();

            // 9. Notificar evento a la UI para mostrar notificación temporal
            OnGameSaved?.Invoke(currentData);
        }

        /// <summary>
        /// Carga la partida guardada desde JSON y restaura el estado en la escena Room2.
        /// Retorna true si se cargó correctamente, false si no había datos guardados.
        /// </summary>
        [ContextMenu("Cargar Partida (LoadGame)")]
        public bool LoadGame()
        {
            FindSceneReferences();

            string json = "";

            // 1. Intentar leer desde archivo en disco
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    json = File.ReadAllText(SaveFilePath);
                    Debug.Log($"[GameSaveManager] Datos leídos desde archivo: {SaveFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameSaveManager] Falló lectura de archivo, intentando PlayerPrefs: {ex.Message}");
                }
            }

            // 2. Si no hubo archivo o falló, buscar en PlayerPrefs
            if (string.IsNullOrEmpty(json) && PlayerPrefs.HasKey(PLAYER_PREFS_KEY))
            {
                json = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
                Debug.Log("[GameSaveManager] Datos leídos desde PlayerPrefs de respaldo.");
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[GameSaveManager] No se encontró ningún archivo de guardado ni datos en PlayerPrefs.");
                return false;
            }

            // 3. Deserializar
            try
            {
                currentData = JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSaveManager] Error deserializando JSON: {ex.Message}");
                return false;
            }

            // 4. Restaurar posición del jugador (X, Y, Z)
            Transform playerT = FindPlayerTransform();
            if (playerT != null)
            {
                CharacterController cc = playerT.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerT.position = new Vector3(currentData.playerPosX, currentData.playerPosY, currentData.playerPosZ);
                if (cc != null) cc.enabled = true;
            }

            // 5. Restaurar estado de la bomba
            if (bombController != null)
            {
                bombController.timeRemaining = Mathf.Max(1f, currentData.timeRemaining);
                bombController.enteredCode = currentData.lastEnteredCode ?? "";

                if (currentData.isBombDefused)
                {
                    bombController.currentState = BombState.Defused;
                    if (bombController.activeRedLight != null) bombController.activeRedLight.enabled = false;
                    if (bombController.defusedGreenLight != null) bombController.defusedGreenLight.enabled = true;
                    if (bombController.statusText != null) bombController.statusText.text = "<color=green>BOMBA DESACTIVADA (CARGADO)</color>";
                }
                else if (currentData.isBombExploded)
                {
                    bombController.currentState = BombState.Exploded;
                    if (bombController.statusText != null) bombController.statusText.text = "<color=red>DETONADA (CARGADO)</color>";
                }
                else
                {
                    bombController.currentState = BombState.Armed;
                }
            }

            // 6. Restaurar estado de las notas en DefusalGameStateManager
            DefusalGameStateManager.CurrentState.collectedClueIds = new List<string>(currentData.collectedClueIds);

            // 7. Restaurar pistas en los componentes VRNoteInteractable y ClueInteractable
            var allNotes = UnityEngine.Object.FindObjectsByType<VRNoteInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var note in allNotes)
            {
                if (note != null && note.clueData != null)
                {
                    bool wasFound = currentData.collectedClueIds.Contains(note.clueData.clueId);
                    note.clueData.isCollected = wasFound;
                }
            }

            var allClues = UnityEngine.Object.FindObjectsByType<ClueInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var clue in allClues)
            {
                if (clue != null && clue.clueData != null)
                {
                    bool wasFound = currentData.collectedClueIds.Contains(clue.clueData.clueId);
                    clue.clueData.isCollected = wasFound;
                }
            }

            Debug.Log($"[GameSaveManager] ¡Partida cargada con éxito! Nivel: {currentData.currentScene}, Notas encontradas: {currentData.collectedClueIds.Count}, Tiempo restante: {currentData.timeRemaining:F1}s, Posición: ({currentData.playerPosX:F2}, {currentData.playerPosY:F2}, {currentData.playerPosZ:F2})");

            // 7. Notificar evento
            OnGameLoaded?.Invoke(currentData);
            return true;
        }

        /// <summary>
        /// Borra todos los datos de guardado existentes (archivo físico y PlayerPrefs).
        /// </summary>
        [ContextMenu("Borrar Datos Guardados (ClearSaveData)")]
        public void ClearSaveData()
        {
            bool hadFile = false;

            if (File.Exists(SaveFilePath))
            {
                try
                {
                    File.Delete(SaveFilePath);
                    hadFile = true;
                    Debug.Log($"[GameSaveManager] Archivo de guardado eliminado: {SaveFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameSaveManager] Error al eliminar archivo de guardado: {ex.Message}");
                }
            }

            if (PlayerPrefs.HasKey(PLAYER_PREFS_KEY))
            {
                PlayerPrefs.DeleteKey(PLAYER_PREFS_KEY);
                PlayerPrefs.Save();
                hadFile = true;
                Debug.Log("[GameSaveManager] Clave PlayerPrefs de guardado eliminada.");
            }

            currentData = new GameSaveData();
            DefusalGameStateManager.ResetState();

            if (hadFile)
            {
                Debug.Log("[GameSaveManager] Datos de partida reiniciados por completo.");
            }

            OnSaveDataCleared?.Invoke();
        }

        /// <summary>
        /// Retorna true si existe un archivo de guardado o datos en PlayerPrefs.
        /// </summary>
        public bool HasSaveData()
        {
            return File.Exists(SaveFilePath) || PlayerPrefs.HasKey(PLAYER_PREFS_KEY);
        }

        /// <summary>
        /// Notifica al sistema que una nota fue recogida para guardado automático si está habilitado.
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
    }
}
