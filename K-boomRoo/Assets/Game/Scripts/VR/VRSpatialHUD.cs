using UnityEngine;
using TMPro;
using DefusalGame.Bomb;
using DefusalGame.Gameplay;

namespace DefusalGame.VR
{
    /// <summary>
    /// HUD táctico en el espacio 3D (World-Space) que acompaña la vista o la muñeca del jugador en RV.
    /// Muestra el tiempo restante con alerta cromática, el estado de las 3 fases y el inventario virtual.
    /// </summary>
    public class VRSpatialHUD : MonoBehaviour
    {
        [Header("Seguimiento en RV")]
        public Transform targetCamera;
        public Vector3 offset = new Vector3(0f, 0.45f, 2.2f);
        public float followSpeed = 5f;
        public bool lockPitch = true;
        public bool faceCamera = true;

        [Header("Elementos de UI TMP")]
        public TextMeshPro timerText;
        public TextMeshPro phase1Badge;
        public TextMeshPro phase2Badge;
        public TextMeshPro phase3Badge;
        public TextMeshPro inventoryBadge;

        private Level3MultiStageBomb bomb;

        void Start()
        {
            FindActiveCamera();
            bomb = Object.FindAnyObjectByType<Level3MultiStageBomb>();
            UpdateHUD();
        }

        private void FindActiveCamera()
        {
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
            else
            {
                Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var c in cams)
                {
                    if (c.isActiveAndEnabled)
                    {
                        targetCamera = c.transform;
                        break;
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (targetCamera == null || !targetCamera.gameObject.activeInHierarchy)
            {
                FindActiveCamera();
                if (targetCamera == null) return;
            }

            // Seguir suavemente a la cámara para que flote en el campo de visión cómodo
            Vector3 targetPos = targetCamera.position + (targetCamera.forward * offset.z) + (targetCamera.right * offset.x) + (Vector3.up * offset.y);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            // Orientar de frente al jugador (mismo ángulo que la cámara)
            if (faceCamera)
            {
                if (lockPitch)
                {
                    transform.rotation = Quaternion.Euler(0f, targetCamera.eulerAngles.y, 0f);
                }
                else
                {
                    transform.rotation = targetCamera.rotation;
                }
            }

            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (bomb == null) bomb = Object.FindAnyObjectByType<Level3MultiStageBomb>();

            if (bomb != null)
            {
                // 1. Cronómetro
                if (timerText != null)
                {
                    if (bomb.currentState == BombState.Defused)
                    {
                        timerText.text = "<color=#00FF66>00:00 - ¡DESARMADA!</color>";
                    }
                    else if (bomb.currentState == BombState.Exploded)
                    {
                        timerText.text = "<color=#FF0000>¡¡DETONADA!!</color>";
                    }
                    else
                    {
                        float t = Mathf.Max(0f, bomb.timeRemaining);
                        int min = Mathf.FloorToInt(t / 60f);
                        int sec = Mathf.FloorToInt(t % 60f);
                        int ms = Mathf.FloorToInt((t * 100f) % 100f);

                        string col = (t <= 60f) ? "<color=#FF2222>" : "<color=#FFCC00>";
                        timerText.text = $"{col}⏱ {min:00}:{sec:00}.{ms:00}</color>";
                    }
                }

                // 2. Fases
                if (phase1Badge != null)
                {
                    phase1Badge.text = !bomb.isOvervoltageActive
                        ? "<color=#00FF66>✔ [1. SOBRETENSIÓN: DESACTIVADA (B2)]</color>"
                        : "<color=#FF6666>✖ [1. SOBRETENSIÓN: PANEL COCINA -> B2]</color>";
                }

                if (phase2Badge != null)
                {
                    phase2Badge.text = bomb.isWireCut
                        ? "<color=#00FF66>✔ [2. CABLEADO: AZUL CORTADO SEGURO]</color>"
                        : (!bomb.isOvervoltageActive
                            ? "<color=#FFCC00>⚡ [2. CABLEADO: REVISA PC -> CORTA AZUL]</color>"
                            : "<color=#888888>🔒 [2. CABLEADO: BLOQUEADO (PELIGRO)]</color>");
                }

                if (phase3Badge != null)
                {
                    phase3Badge.text = (bomb.currentState == BombState.Defused)
                        ? "<color=#00FF66>✔ [3. TECLADO: BOMBA DESARMADA]</color>"
                        : (bomb.isWireCut
                            ? $"<color=#00DDFF>🔑 [3. TECLADO UV]: {(string.IsNullOrEmpty(bomb.enteredCode) ? "_ _ _ _" : bomb.enteredCode)}</color>"
                            : "<color=#888888>🔒 [3. TECLADO: BLOQUEADO POR CABLES]</color>");
                }
            }

            // 3. Inventario
            if (inventoryBadge != null)
            {
                var inv = PlayerInventory.Instance;
                if (inv != null)
                {
                    string ice = inv.hasFrozenIceBlock ? "<color=#00DDFF>[HIELO]</color>" : "<color=#444444>[HIELO]</color>";
                    string pliers = inv.hasPliers ? "<color=#FFAA00>[PINZAS]</color>" : "<color=#444444>[PINZAS]</color>";
                    string uv = inv.hasUVLight ? "<color=#B026FF>[LINTERNA UV]</color>" : "<color=#444444>[LINTERNA UV]</color>";

                    inventoryBadge.text = $"MOCHILA RV: {ice}  {pliers}  {uv}";
                }
            }
        }
    }
}
