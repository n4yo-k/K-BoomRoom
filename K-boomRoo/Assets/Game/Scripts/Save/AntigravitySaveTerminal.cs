using System;
using System.Collections;
using UnityEngine;
using TMPro;
using DefusalGame.Data;
using DefusalGame.Bomb;
using DefusalGame.Gameplay;

namespace DefusalGame.Save
{
    /// <summary>
    /// Terminal y panel 3D en espacio de mundo para el sistema de guardado en Room2.
    /// Muestra el estado operativo de la misión y actualiza su interfaz visual a color verde neón
    /// con el mensaje "Progreso Guardado" cuando el botón físico es presionado.
    /// </summary>
    public class AntigravitySaveTerminal : MonoBehaviour
    {
        [Header("Elementos de la Interfaz 3D (World-Space)")]
        public TextMeshPro headerText;
        public TextMeshPro statusText;
        public TextMeshPro detailsText;
        public Light terminalIndicatorLight;
        public Renderer screenBezelRenderer;

        [Header("Botón Físico Integrado")]
        public AntigravityPhysicalButton savePhysicalButton;

        [Header("Colores de Estado")]
        public Color normalColor = new Color(0.2f, 0.8f, 1.0f);
        public Color savedSuccessColor = new Color(0.0f, 1.0f, 0.4f);
        public Color warningColor = new Color(1.0f, 0.4f, 0.1f);

        [Header("Audio y Notificación")]
        public AudioSource audioSource;
        public AudioClip saveSuccessClip;

        private Coroutine confirmationRoutine;
        private string lastSaveTimeString = "Ninguno";

        void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        void Start()
        {
            if (savePhysicalButton != null)
            {
                savePhysicalButton.onButtonPressed.AddListener(OnSaveButtonPressed);
            }

            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.OnGameSaved += HandleGameSaved;
                GameSaveManager.Instance.OnGameLoaded += HandleGameLoaded;
            }

            UpdateDisplay();
        }

        void OnDestroy()
        {
            if (savePhysicalButton != null)
            {
                savePhysicalButton.onButtonPressed.RemoveListener(OnSaveButtonPressed);
            }

            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.OnGameSaved -= HandleGameSaved;
                GameSaveManager.Instance.OnGameLoaded -= HandleGameLoaded;
            }
        }

        public void OnSaveButtonPressed()
        {
            if (Room2UIManager.Instance != null)
            {
                Room2UIManager.Instance.OpenSavePopup();
            }
            else if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }
            else
            {
                Debug.LogWarning("[AntigravitySaveTerminal] GameSaveManager / Room2UIManager no encontrado en escena.");
            }
        }

        private void HandleGameSaved(GameSaveData data)
        {
            lastSaveTimeString = DateTime.Now.ToString("HH:mm:ss");

            // Reproducir sonido de confirmación
            if (audioSource != null)
            {
                if (saveSuccessClip != null)
                {
                    audioSource.PlayOneShot(saveSuccessClip);
                }
                else
                {
                    SynthesizeChime(audioSource);
                }
            }

            if (confirmationRoutine != null) StopCoroutine(confirmationRoutine);
            confirmationRoutine = StartCoroutine(ShowSaveConfirmationRoutine(data));
        }

        private void HandleGameLoaded(GameSaveData data)
        {
            lastSaveTimeString = data.saveTimestamp;
            UpdateDisplay();
        }

        private IEnumerator ShowSaveConfirmationRoutine(GameSaveData data)
        {
            // 1. Mostrar confirmación visual requerida con cambio de color a VERDE
            if (statusText != null)
            {
                statusText.text = "<size=120%><b>✓ PROGRESO GUARDADO</b></size>\n<size=80%>Partida asegurada en almacenamiento local</size>";
                statusText.color = savedSuccessColor;
            }

            if (terminalIndicatorLight != null)
            {
                terminalIndicatorLight.color = savedSuccessColor;
                terminalIndicatorLight.intensity = 2.2f;
            }

            UpdateDetailsText(data, isJustSaved: true);

            yield return new WaitForSeconds(3.5f);

            // 2. Regresar al estado operativo normal
            if (statusText != null)
            {
                statusText.text = "SISTEMA OPERATIVO // EN ESPERA";
                statusText.color = normalColor;
            }

            if (terminalIndicatorLight != null)
            {
                terminalIndicatorLight.color = normalColor;
                terminalIndicatorLight.intensity = 1.0f;
            }

            UpdateDetailsText(data, isJustSaved: false);
            confirmationRoutine = null;
        }

        public void UpdateDisplay()
        {
            if (headerText != null)
            {
                headerText.text = "<b>TERMINAL TÁCTICO // ANTIGRAVITY OS</b>\n<size=70%>CONTROL DE MISIÓN Y TELEMETRÍA</size>";
                headerText.color = normalColor;
            }

            if (statusText != null)
            {
                statusText.text = "SISTEMA OPERATIVO // EN ESPERA";
                statusText.color = normalColor;
            }

            if (terminalIndicatorLight != null)
            {
                terminalIndicatorLight.color = normalColor;
                terminalIndicatorLight.intensity = 1.0f;
            }

            var data = (GameSaveManager.Instance != null) ? GameSaveManager.Instance.currentData : new GameSaveData();
            UpdateDetailsText(data, false);
        }

        private void UpdateDetailsText(GameSaveData data, bool isJustSaved)
        {
            if (detailsText == null) return;

            int clues = (data != null && data.collectedClueIds != null) ? data.collectedClueIds.Count : 0;
            string bombInfo = "ARMADA (Cuenta regresiva activa)";
            if (data != null && data.isBombDefused) bombInfo = "<color=#00FF66>NEUTRALIZADA</color>";
            else if (data != null && data.isBombExploded) bombInfo = "<color=#FF2222>DETONADA</color>";

            detailsText.text =
                $"<b>NIVEL:</b> SALA 2 (Escape Room VR)\n" +
                $"<b>PISTAS REUNIDAS:</b> {clues} / 3\n" +
                $"<b>ESTADO C4:</b> {bombInfo}\n" +
                $"<b>ÚLTIMO GUARDADO:</b> <color=#FFEE66>{lastSaveTimeString}</color>\n\n" +
                $"<i>[ Presiona el botón físico inferior para registrar progreso ]</i>";
        }

        private void SynthesizeChime(AudioSource src)
        {
            // Reproducir doble tono agudo de confirmación táctica
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.22f);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t < 0.10f) ? 880f : 1320f; // La -> Mi agudo
                float envelope = 1.0f - (t / 0.22f);
                samples[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * 0.4f;
            }

            AudioClip clip = AudioClip.Create("ChimeSynth", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            src.PlayOneShot(clip);
        }
    }
}
