using TMPro;
using UnityEngine;
using DefusalGame.Data;
using DefusalGame.Gameplay;

namespace DefusalGame.Bomb
{
    public enum BombState
    {
        Idle,
        Armed,
        Defused,
        Exploded
    }

    public class BombController : MonoBehaviour
    {
        [Header("Configuración")]
        public BombConfigData config;

        [Header("Estado")]
        public BombState currentState = BombState.Armed;
        public float timeRemaining = 300f;
        public string enteredCode = "";

        [Header("Displays UI")]
        public TextMeshPro timerText;
        public TextMeshPro codeText;
        public TextMeshPro statusText;

        [Header("Luces Indicadoras")]
        public Light activeRedLight;
        public Light defusedGreenLight;

        [Header("Audio")]
        public BombAudioSynthesizer audioSynth;

        [Header("Detonación")]
        public string explosionReason = "";

        private float lastTickSecond = -1f;
        private float redLightBlinkTimer = 0f;

        void Start()
        {
            if (audioSynth == null) audioSynth = GetComponentInChildren<BombAudioSynthesizer>();

            if (config != null)
            {
                timeRemaining = config.timeLimitSeconds;
                if (config.linkedClues != null)
                {
                    foreach (var c in config.linkedClues)
                    {
                        if (c != null) c.isCollected = false;
                    }
                }
            }
            else
            {
                timeRemaining = 300f;
            }

            DefusalGameStateManager.ResetState(timeRemaining, (config != null) ? config.bombId : "BOMB_CHAMBER_01");

            currentState = BombState.Armed;
            enteredCode = "";
            explosionReason = "";
            UpdateDisplays();

            if (activeRedLight != null) activeRedLight.enabled = true;
            if (defusedGreenLight != null) defusedGreenLight.enabled = false;
        }

        void Update()
        {
            if (currentState != BombState.Armed) return;

            // Descontar tiempo
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                TriggerExplosion("¡Se agotó el tiempo límite!");
                return;
            }

            // Tic-tac sonoro cada segundo
            int currentSecond = Mathf.FloorToInt(timeRemaining);
            if (currentSecond != (int)lastTickSecond)
            {
                lastTickSecond = currentSecond;
                bool isUrgent = timeRemaining < 60f;
                if (audioSynth != null) audioSynth.PlayTick(isUrgent);
            }

            // Parpadeo de luz roja activa
            redLightBlinkTimer += Time.deltaTime;
            float blinkInterval = (timeRemaining < 60f) ? 0.3f : 0.8f;
            if (activeRedLight != null)
            {
                activeRedLight.enabled = (redLightBlinkTimer % blinkInterval) < (blinkInterval * 0.5f);
            }

            UpdateDisplays();
        }

        public void OnKeyPressed(string key)
        {
            if (currentState != BombState.Armed) return;

            if (audioSynth != null) audioSynth.PlayButton();

            if (key == "CLR" || key == "C")
            {
                enteredCode = "";
                if (statusText != null) statusText.text = "CODIGO BORRADO";
            }
            else if (key == "ENT" || key == "#")
            {
                ValidateCode();
            }
            else
            {
                // Dígito 0-9
                int maxLen = (config != null && !string.IsNullOrEmpty(config.targetSequence)) ? config.targetSequence.Length : 4;
                if (enteredCode.Length < maxLen)
                {
                    enteredCode += key;
                }
            }

            UpdateDisplays();
        }

        private void ValidateCode()
        {
            var room2Manager = UnityEngine.Object.FindFirstObjectByType<Room2EscapeRoomManager>();
            if (room2Manager != null && !room2Manager.AreAllNotesCollected())
            {
                if (statusText != null) statusText.text = "ENCUENTRA LAS 4 NOTAS";
                Debug.Log("[BombController] Código bloqueado: aún faltan notas de Room2.");
                return;
            }

            string target = (config != null) ? config.targetSequence : "4826";
            if (config != null && config.ValidateCode(enteredCode))
            {
                // Código Correcto -> DESACTIVACIÓN
                TriggerDefusal();
            }
            else
            {
                // Código Erróneo -> EXPLOSIÓN DIRECTA (El usuario solicitó: si se equivoca en algo, quiero que explote)
                string inputStr = string.IsNullOrEmpty(enteredCode) ? "Vacío" : enteredCode;
                Debug.Log($"[BombController] Código incorrecto ingresado: '{inputStr}'. ¡EXPLOSIÓN INMEDIATA!");
                if (statusText != null) statusText.text = "<color=red>¡CÓDIGO ERRÓNEO!</color>";
                TriggerExplosion($"Código incorrecto ingresado: '{inputStr}'");
            }
        }

        private void TriggerDefusal()
        {
            currentState = BombState.Defused;
            if (audioSynth != null) audioSynth.PlaySuccess();

            if (activeRedLight != null) activeRedLight.enabled = false;
            if (defusedGreenLight != null)
            {
                defusedGreenLight.enabled = true;
                defusedGreenLight.color = Color.green;
            }

            if (statusText != null) statusText.text = "<color=green>¡BOMBA DESACTIVADA!</color>";
            if (codeText != null) codeText.text = "<color=green>SUCCESS</color>";

            Debug.Log("[BombController] ¡Bomba desactivada con éxito!");
        }

        public void TriggerExplosion(string reason = "Se agotó el tiempo límite")
        {
            if (currentState == BombState.Exploded) return;

            currentState = BombState.Exploded;
            explosionReason = reason;

            if (audioSynth != null) audioSynth.PlayExplosion();

            if (activeRedLight != null) activeRedLight.enabled = true;
            if (defusedGreenLight != null) defusedGreenLight.enabled = false;

            if (statusText != null) statusText.text = "<color=red>¡DETONACIÓN!</color>";
            if (codeText != null) codeText.text = "<color=red>0000</color>";

            Debug.Log($"[BombController] ¡La bomba ha detonado! Causa: {reason}");
        }

        private void UpdateDisplays()
        {
            // Temporizador: MM:SS.ms
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                int centiseconds = Mathf.FloorToInt((timeRemaining * 100f) % 100f);
                timerText.text = $"{minutes:00}:{seconds:00}.<size=70%>{centiseconds:00}</size>";
                timerText.color = (timeRemaining < 60f) ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.5f, 0.2f); // Naranja/Rojo LED
            }

            // Código ingresado: muestra caracteres o guiones
            if (codeText != null && currentState == BombState.Armed)
            {
                int maxLen = (config != null && !string.IsNullOrEmpty(config.targetSequence)) ? config.targetSequence.Length : 4;
                string display = "";
                for (int i = 0; i < maxLen; i++)
                {
                    if (i < enteredCode.Length) display += enteredCode[i] + " ";
                    else display += "_ ";
                }
                codeText.text = display.Trim();
            }
        }
    }
}
