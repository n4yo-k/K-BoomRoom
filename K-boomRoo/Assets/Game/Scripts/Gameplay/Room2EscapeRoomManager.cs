using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DefusalGame.Bomb;
using DefusalGame.Data;
using DefusalGame.Save;

namespace DefusalGame.Gameplay
{
    /// <summary>
    /// Controlador principal del nivel Room2 (Escape Room VR).
    /// Coordina la bomba de 4 dígitos, las 4 notas interactivas, la pizarra de misión
    /// y los eventos de victoria / derrota integrados con el sistema de guardado.
    /// </summary>
    public class Room2EscapeRoomManager : MonoBehaviour
    {
        public static Room2EscapeRoomManager Instance { get; private set; }

        [Header("Bomba y Teclado")]
        public BombController bombController;

        [Header("Notas Ocultas (4 Notas)")]
        public List<VRNoteInteractable> roomNotes = new List<VRNoteInteractable>();

        [Header("Pizarra de Misión en la Habitación")]
        public TextMeshPro missionBoardText;

        [Header("Iluminación y Efectos de Sala")]
        public Light roomMainLight;
        public Color armedLightColor = new Color(0.95f, 0.75f, 0.55f);
        public Color victoryLightColor = new Color(0.4f, 1.0f, 0.6f);
        public Color failureLightColor = new Color(1.0f, 0.2f, 0.2f);

        [Header("Audio")]
        public AudioSource ambientAudioSource;
        public AudioClip victoryFanfare;
        public AudioClip noteFoundChime;

        private bool hasHandledDefusal = false;
        private bool hasHandledExplosion = false;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        void Start()
        {
            if (bombController == null)
            {
                bombController = UnityEngine.Object.FindFirstObjectByType<BombController>();
            }

            if (ambientAudioSource == null)
            {
                ambientAudioSource = GetComponent<AudioSource>();
            }

            // Suscribirse a eventos del sistema de guardado
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.OnGameLoaded += OnSaveLoaded;
                GameSaveManager.Instance.OnSaveDataCleared += OnSaveCleared;
            }

            UpdateMissionBoard();
        }

        void OnDestroy()
        {
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.OnGameLoaded -= OnSaveLoaded;
                GameSaveManager.Instance.OnSaveDataCleared -= OnSaveCleared;
            }
        }

        void Update()
        {
            if (bombController == null) return;

            // Detectar victoria (Bomba desactivada)
            if (bombController.currentState == BombState.Defused && !hasHandledDefusal)
            {
                HandleVictory();
            }
            // Detectar detonación
            else if (bombController.currentState == BombState.Exploded && !hasHandledExplosion)
            {
                HandleDefeat();
            }
        }

        /// <summary>
        /// Llamado cuando una nota es descubierta o recogida por el jugador.
        /// </summary>
        public void OnNoteCollected(VRNoteInteractable note)
        {
            if (noteFoundChime != null && ambientAudioSource != null)
            {
                ambientAudioSource.PlayOneShot(noteFoundChime);
            }

            UpdateMissionBoard();

            // Guardado automático del progreso
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }
        }

        public bool AreAllNotesCollected()
        {
            if (roomNotes == null || roomNotes.Count < 4) return false;

            for (int i = 0; i < roomNotes.Count; i++)
            {
                var note = roomNotes[i];
                if (note == null || note.clueData == null) return false;

                bool isFound = note.clueData.isCollected ||
                    DefusalGameStateManager.CurrentState.collectedClueIds.Contains(note.clueData.clueId);
                if (!isFound) return false;
            }

            return true;
        }

        private void HandleVictory()
        {
            hasHandledDefusal = true;

            if (roomMainLight != null)
            {
                roomMainLight.color = victoryLightColor;
                roomMainLight.intensity = 1.4f;
            }

            if (victoryFanfare != null && ambientAudioSource != null)
            {
                ambientAudioSource.PlayOneShot(victoryFanfare);
            }

            UpdateMissionBoard();

            // Guardar partida con estado de victoria
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }

            Debug.Log("[Room2EscapeRoomManager] ¡ESCAPE ROOM COMPLETADO! Bomba desactivada.");
        }

        private void HandleDefeat()
        {
            hasHandledExplosion = true;

            if (roomMainLight != null)
            {
                roomMainLight.color = failureLightColor;
            }

            UpdateMissionBoard();

            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }

            Debug.Log("[Room2EscapeRoomManager] ¡Tiempo agotado! La bomba ha detonado.");
        }

        /// <summary>
        /// Actualiza el texto de la pizarra de la misión con las pistas encontradas.
        /// </summary>
        public void UpdateMissionBoard()
        {
            if (missionBoardText == null) return;

            int collectedCount = 0;
            string notesInfo = "";

            for (int i = 0; i < roomNotes.Count; i++)
            {
                var note = roomNotes[i];
                if (note != null && note.clueData != null)
                {
                    bool isFound = note.clueData.isCollected || 
                        DefusalGameStateManager.CurrentState.collectedClueIds.Contains(note.clueData.clueId);

                    if (isFound)
                    {
                        collectedCount++;
                        notesInfo += $"<color=#00FF66><b>[✓] NOTA #{i + 1}:</b></color> {note.clueData.clueName} -> <color=#FFEE00><b>[{note.clueData.revealedValue}]</b></color> (Pos #{note.clueData.sequenceIndex + 1})\n";
                    }
                    else
                    {
                        notesInfo += $"<color=#AAAAAA>[ ] NOTA #{i + 1}:</color> {note.clueData.clueName} (Por encontrar)\n";
                    }
                }
            }

            string bombStatus = "ARMADA Y EN CUENTA REGRESIVA";
            string statusColor = "#FF3333";

            if (bombController != null)
            {
                if (bombController.currentState == BombState.Defused)
                {
                    bombStatus = "¡NEUTRALIZADA CON ÉXITO!";
                    statusColor = "#00FF66";
                }
                else if (bombController.currentState == BombState.Exploded)
                {
                    bombStatus = "DETONACIÓN - MISIÓN FALLIDA";
                    statusColor = "#FF0000";
                }
            }

            missionBoardText.text =
                $"<align=center><size=130%><b>ESCAPE ROOM VR - SALA 2</b></size>\n" +
                $"<color={statusColor}><size=95%><b>ESTADO: {bombStatus}</b></size></color></align>\n" +
                $"----------------------------------------\n" +
                $"<b>OBJETIVO:</b> Encuentra las 4 notas ocultas en la habitación para obtener los 4 dígitos de desactivación.\n\n" +
                $"<b>PISTAS REUNIDAS ({collectedCount}/4):</b>\n" +
                $"{notesInfo}\n" +
                $"----------------------------------------\n" +
                $"<i>Introduce los 4 dígitos en el teclado de la bomba y presiona <b>ENT</b>.\n" +
                $"Guardar: Tecla <b>G</b> o Panel 3D | Cargar: <b>F9</b> | Reiniciar: <b>R</b></i>";
        }

        private void OnSaveLoaded(GameSaveData data)
        {
            hasHandledDefusal = data.isBombDefused;
            hasHandledExplosion = data.isBombExploded;

            if (hasHandledDefusal && roomMainLight != null)
            {
                roomMainLight.color = victoryLightColor;
            }

            UpdateMissionBoard();
        }

        private void OnSaveCleared()
        {
            hasHandledDefusal = false;
            hasHandledExplosion = false;
            if (roomMainLight != null)
            {
                roomMainLight.color = armedLightColor;
            }
            UpdateMissionBoard();
        }
    }
}
