using UnityEngine;
using TMPro;

namespace DefusalGame.Gameplay
{
    public class InteractiveMicrowave : MonoBehaviour
    {
        [Header("Estado del Microondas")]
        public bool isIcePlaced = false;
        public bool isCooking = false;
        public bool isCookFinished = false;
        public float cookingTimeRemaining = 3.0f;

        [Header("Referencias Visuales")]
        public GameObject interiorLight;
        public GameObject frozenIceVisual;
        public GameObject pliersResultVisual;
        public TextMeshPro screenTimerText;

        private AudioSource audioSrc;

        void Awake()
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();

            UpdateVisuals();
        }

        void Update()
        {
            if (isCooking)
            {
                cookingTimeRemaining -= Time.deltaTime;
                if (screenTimerText != null)
                {
                    screenTimerText.text = $"00:{Mathf.CeilToInt(cookingTimeRemaining):00}";
                }

                if (cookingTimeRemaining <= 0f)
                {
                    FinishCooking();
                }
            }
        }

        public void Interact()
        {
            var inv = PlayerInventory.Instance;

            // 1. Si no hay hielo puesto pero el jugador tiene el bloque
            if (!isIcePlaced && !isCookFinished)
            {
                if (inv != null && inv.hasFrozenIceBlock)
                {
                    inv.hasFrozenIceBlock = false;
                    isIcePlaced = true;
                    UpdateVisuals();
                    Debug.Log("[Microwave] Bloque de hielo colocado dentro. Pulsa de nuevo para calentar.");
                    return;
                }
                else
                {
                    Debug.Log("[Microwave] Está vacío. Necesitas el bloque de hielo del congelador.");
                    return;
                }
            }

            // 2. Si hay hielo puesto y no está cocinando
            if (isIcePlaced && !isCooking && !isCookFinished)
            {
                StartCooking();
                return;
            }

            // 3. Si ya terminó de cocinar, recoger los alicates
            if (isCookFinished && pliersResultVisual != null && pliersResultVisual.activeSelf)
            {
                if (inv != null) inv.CollectPliers();
                pliersResultVisual.SetActive(false);
                Debug.Log("[Microwave] ¡Has recogido los alicates descongelados!");
                return;
            }
        }

        private void StartCooking()
        {
            isCooking = true;
            cookingTimeRemaining = 3.0f;
            if (interiorLight != null) interiorLight.SetActive(true);
            Debug.Log("[Microwave] Calentando a máxima potencia... (3 segundos)");
        }

        private void FinishCooking()
        {
            isCooking = false;
            isCookFinished = true;
            if (interiorLight != null) interiorLight.SetActive(false);
            if (frozenIceVisual != null) frozenIceVisual.SetActive(false);
            if (pliersResultVisual != null) pliersResultVisual.SetActive(true);

            if (screenTimerText != null) screenTimerText.text = "¡LISTO!";
            Debug.Log("[Microwave] *DING* ¡El hielo se ha derretido! Alicates listos.");
        }

        public void UpdateVisuals()
        {
            if (interiorLight != null) interiorLight.SetActive(isCooking);
            if (frozenIceVisual != null) frozenIceVisual.SetActive(isIcePlaced && !isCookFinished);
            if (pliersResultVisual != null) pliersResultVisual.SetActive(isCookFinished && !(PlayerInventory.Instance != null && PlayerInventory.Instance.hasPliers));
        }
    }
}
