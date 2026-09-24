using TMPro;
using UnityEngine;
using DefusalGame.Gameplay;

namespace DefusalGame.Bomb
{
    public class Level3MultiStageBomb : MonoBehaviour
    {
        [Header("Tiempo Límite")]
        public float timeRemaining = 210f; // 03:30
        public BombState currentState = BombState.Armed;
        public string explosionReason = "";

        [Header("Fase 1: Sobretensión")]
        public bool isOvervoltageActive = true;
        public Light warningFlashLight;
        public ParticleSystem sparkParticles;

        [Header("Fase 2: Cables")]
        public bool isWireCut = false;
        public string correctWireColor = "AZUL"; // Serial SN-7492 es PAR -> AZUL
        public GameObject wireRed;
        public GameObject wireBlue;
        public GameObject wireGreen;
        public GameObject wireYellow;

        [Header("Fase 3: Teclado Final")]
        public string targetCode = "7531";
        public string enteredCode = "";
        public TextMeshPro timerText;
        public TextMeshPro codeText;
        public TextMeshPro statusText;
        public Light defusedGreenLight;

        [Header("Audio")]
        public BombAudioSynthesizer audioSynth;

        private float lastTickSecond = -1f;

        void Awake()
        {
            if (audioSynth == null) audioSynth = GetComponentInChildren<BombAudioSynthesizer>();
        }

        void Start()
        {
            timeRemaining = 210f;
            currentState = BombState.Armed;
            isOvervoltageActive = true;
            isWireCut = false;
            enteredCode = "";
            explosionReason = "";

            UpdateDisplays();
            if (warningFlashLight != null) warningFlashLight.enabled = true;
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
                TriggerExplosion("¡Se agotó el tiempo límite de 03:30!");
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

            // Parpadeo de advertencia si la sobretensión sigue activa
            if (warningFlashLight != null)
            {
                if (isOvervoltageActive)
                {
                    warningFlashLight.enabled = (Time.time * 6f) % 1f < 0.5f;
                }
                else
                {
                    warningFlashLight.enabled = (timeRemaining < 60f) ? ((Time.time * 3f) % 1f < 0.5f) : true;
                    warningFlashLight.color = Color.red;
                }
            }

            UpdateDisplays();
        }

        // ==========================================
        // FASE 1: DESVIAR SOBRETENSIÓN
        // ==========================================
        public void DisarmOvervoltage()
        {
            if (!isOvervoltageActive) return;

            isOvervoltageActive = false;
            if (sparkParticles != null) sparkParticles.Stop();
            if (warningFlashLight != null) warningFlashLight.color = Color.yellow;
            if (statusText != null) statusText.text = "<color=green>VOLTAJE ESTABILIZADO - FASE 2 ACTIVA</color>";

            Debug.Log("[Level3Bomb] ¡Sobretensión desviada! Ahora puedes manipular los cables.");
        }

        // ==========================================
        // FASE 2: CORTE DE CABLES
        // ==========================================
        public void CutWire(string wireColor)
        {
            if (currentState != BombState.Armed) return;

            // Si la sobretensión sigue activa, ¡recibe descarga y explota!
            if (isOvervoltageActive)
            {
                Debug.Log("[Level3Bomb] ¡Tocaste los cables con sobrevoltaje activo!");
                TriggerExplosion("Descarga eléctrica mortal: Tocaste los cables sin cortar la sobretensión en la cocina.");
                return;
            }

            var inv = PlayerInventory.Instance;
            if (inv == null || !inv.hasPliers)
            {
                if (statusText != null) statusText.text = "<color=orange>NECESITAS ALICATES (COCINA)</color>";
                Debug.Log("[Level3Bomb] Necesitas alicates para cortar el cable.");
                return;
            }

            if (isWireCut) return;

            if (wireColor == correctWireColor)
            {
                isWireCut = true;
                if (audioSynth != null) audioSynth.PlayButton();
                if (statusText != null) statusText.text = "<color=green>CAPACITOR NEUTRALIZADO - INTRODUCE CLAVE UV</color>";

                // Ocultar visual del cable cortado
                if (wireBlue != null) wireBlue.SetActive(false);

                Debug.Log("[Level3Bomb] ¡Cable correcto cortado! Teclado numérico desbloqueado.");
            }
            else
            {
                Debug.Log($"[Level3Bomb] ¡Cable erróneo cortado ({wireColor})!");
                TriggerExplosion($"Detonación por cable cortado incorrecto ({wireColor}).");
            }
        }

        // ==========================================
        // FASE 3: TECLADO Y CLAVE UV (7531)
        // ==========================================
        public void OnKeyPressed(string key)
        {
            if (currentState != BombState.Armed) return;

            if (audioSynth != null) audioSynth.PlayButton();

            if (!isWireCut)
            {
                if (statusText != null) statusText.text = "<color=red>TECLADO BLOQUEADO: CORTA EL CABLE PRIMERO</color>";
                return;
            }

            if (key == "C" || key == "CLR")
            {
                enteredCode = "";
            }
            else if (key == "ENT" || key == "#")
            {
                ValidateCode();
            }
            else
            {
                if (enteredCode.Length < 4)
                {
                    enteredCode += key;
                }
            }

            UpdateDisplays();
        }

        private void ValidateCode()
        {
            if (enteredCode == targetCode)
            {
                TriggerDefusal();
            }
            else
            {
                string inputStr = string.IsNullOrEmpty(enteredCode) ? "Vacío" : enteredCode;
                Debug.Log($"[Level3Bomb] Clave incorrecta: {inputStr}. ¡EXPLOSIÓN!");
                TriggerExplosion($"Código incorrecto '{inputStr}' en la fase final.");
            }
        }

        private void TriggerDefusal()
        {
            currentState = BombState.Defused;
            if (audioSynth != null) audioSynth.PlaySuccess();

            if (warningFlashLight != null) warningFlashLight.enabled = false;
            if (defusedGreenLight != null)
            {
                defusedGreenLight.enabled = true;
                defusedGreenLight.color = Color.green;
            }

            if (statusText != null) statusText.text = "<color=green>¡BOMBA TOTALMENTE DESACTIVADA!</color>";
            if (codeText != null) codeText.text = "<color=green>7531 - OK</color>";

            Debug.Log("[Level3Bomb] ¡Nivel 3 completado con éxito! Victoria épica.");
        }

        public void TriggerExplosion(string reason = "Detonación")
        {
            if (currentState == BombState.Exploded) return;

            currentState = BombState.Exploded;
            explosionReason = reason;

            if (audioSynth != null) audioSynth.PlayExplosion();

            if (warningFlashLight != null) warningFlashLight.enabled = true;
            if (defusedGreenLight != null) defusedGreenLight.enabled = false;

            if (statusText != null) statusText.text = "<color=red>¡DETONACIÓN TOTAL!</color>";
            if (codeText != null) codeText.text = "<color=red>FAIL</color>";

            Debug.Log($"[Level3Bomb] ¡LA BOMBA HA EXPLOTADO! Motivo: {reason}");
        }

        private void UpdateDisplays()
        {
            if (timerText != null)
            {
                int mins = Mathf.FloorToInt(timeRemaining / 60f);
                int secs = Mathf.FloorToInt(timeRemaining % 60f);
                int cents = Mathf.FloorToInt((timeRemaining * 100f) % 100f);
                timerText.text = $"{mins:00}:{secs:00}.<size=70%>{cents:00}</size>";
                timerText.color = (timeRemaining < 45f) ? Color.red : new Color(1f, 0.5f, 0.15f);
            }

            if (codeText != null && currentState == BombState.Armed)
            {
                if (!isWireCut)
                {
                    codeText.text = "<size=65%>[BLOQUEADO]</size>";
                }
                else
                {
                    string display = "";
                    for (int i = 0; i < 4; i++)
                    {
                        if (i < enteredCode.Length) display += enteredCode[i] + " ";
                        else display += "_ ";
                    }
                    codeText.text = display.Trim();
                }
            }
        }
    }
}
