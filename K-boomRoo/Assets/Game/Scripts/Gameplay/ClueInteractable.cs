using TMPro;
using UnityEngine;
using DefusalGame.Data;

namespace DefusalGame.Gameplay
{
    public class ClueInteractable : MonoBehaviour
    {
        [Header("Datos de la Evidencia")]
        public ClueData clueData;

        [Header("Feedback Visual")]
        public GameObject inspectionCardPopup;
        public TextMeshPro cardTitleText;
        public TextMeshPro cardBodyText;
        public TextMeshPro cardDatoText;

        private bool isInspecting = false;
        // private Outline outlineEffect;

        void Start()
        {
            if (inspectionCardPopup != null)
            {
                inspectionCardPopup.SetActive(false);
            }
        }

        // Clic en PC / VR Ray
        void OnMouseDown()
        {
            Interact();
        }

        public void Interact()
        {
            if (clueData == null) return;

            clueData.isCollected = true;
            DefusalGameStateManager.MarkClueCollected(clueData.clueId);

            ToggleInspection();
        }

        public void ToggleInspection()
        {
            isInspecting = !isInspecting;
            if (inspectionCardPopup != null)
            {
                inspectionCardPopup.SetActive(isInspecting);
                if (isInspecting)
                {
                    if (cardTitleText != null) cardTitleText.text = clueData.clueName;
                    if (cardBodyText != null) cardBodyText.text = clueData.description;
                    if (cardDatoText != null)
                    {
                        cardDatoText.text = $"<color=#FFCC00>DATO CLAVE:</color> <b>{clueData.revealedValue}</b> (Posición #{clueData.sequenceIndex + 1})";
                    }
                }
            }

            Debug.Log($"[ClueInteractable] Evidencia inspeccionada: {clueData.clueName} -> Dato: {clueData.revealedValue}");
        }
    }
}
