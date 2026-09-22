using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using DefusalGame.Data;
using DefusalGame.Save;

namespace DefusalGame.Gameplay
{
    /// <summary>
    /// Componente de interacción para las notas de Escape Room en VR.
    /// Soporta agarre físico con XR Grab Interactable, inspección visual holográfica (TextMeshPro),
    /// y detección con clic / ratón en PC para pruebas ágiles en Editor.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VRNoteInteractable : MonoBehaviour
    {
        [Header("Datos de la Pista")]
        public ClueData clueData;

        [Header("Representación de Texto en la Nota")]
        [Tooltip("Texto impreso sobre la superficie 3D de la nota")]
        public TextMeshPro inWorldNoteText;

        [Header("Tarjeta Holográfica / Popup de Inspección")]
        public GameObject inspectionPopup;
        public TextMeshPro popupTitle;
        public TextMeshPro popupBody;
        public TextMeshPro popupDigitHint;

        [Header("Audio y Efectos")]
        public AudioClip pickupSound;
        private AudioSource audioSource;

        [Header("Estado")]
        public bool isInspecting = false;
        private bool hasBeenCollected = false;

        private XRGrabInteractable grabInteractable;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1.0f; // Audio 3D espacial en VR
            }

            SetupXRInteraction();
            UpdateNoteVisuals();
        }

        void Start()
        {
            if (inspectionPopup != null)
            {
                inspectionPopup.SetActive(false);
            }

            // Si ya estaba guardada como recogida, actualizar estado interno
            if (clueData != null && clueData.isCollected)
            {
                hasBeenCollected = true;
            }
        }

        private void SetupXRInteraction()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }

            // Configurar comportamiento de agarre suave en VR
            grabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            grabInteractable.selectEntered.AddListener(OnXRGrabbed);
            grabInteractable.selectExited.AddListener(OnXRReleased);
        }

        private void OnXRGrabbed(SelectEnterEventArgs args)
        {
            OnNoteInteracted();
            SetPopupActive(true);
        }

        private void OnXRReleased(SelectExitEventArgs args)
        {
            // Al soltar la nota cerramos el popup flotante
            SetPopupActive(false);
        }

        // Clic de ratón para pruebas directas en PC / Editor
        void OnMouseDown()
        {
            OnNoteInteracted();
            TogglePopup();
        }

        /// <summary>
        /// Ejecutado cuando el jugador interactúa con la nota (por agarre VR o clic)
        /// </summary>
        public void OnNoteInteracted()
        {
            if (clueData == null) return;

            clueData.isCollected = true;
            DefusalGameStateManager.MarkClueCollected(clueData.clueId);

            if (!hasBeenCollected)
            {
                hasBeenCollected = true;

                // Sonido de descubrimiento
                if (pickupSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }

                // Notificar al gestor de guardado para auto-guardado
                if (GameSaveManager.Instance != null)
                {
                    GameSaveManager.Instance.NotifyClueCollected(clueData.clueId);
                }

                // Notificar al controlador de Room2
                var roomMgr = UnityEngine.Object.FindFirstObjectByType<Room2EscapeRoomManager>();
                if (roomMgr != null)
                {
                    roomMgr.OnNoteCollected(this);
                }

                Debug.Log($"[VRNoteInteractable] ¡Nota descubierta! Clave: {clueData.clueName} -> Dígito: {clueData.revealedValue}");
            }
        }

        public void TogglePopup()
        {
            SetPopupActive(!isInspecting);
        }

        public void SetPopupActive(bool active)
        {
            isInspecting = active;
            if (inspectionPopup != null)
            {
                inspectionPopup.SetActive(isInspecting);
            }
        }

        /// <summary>
        /// Rellena los textos con la información del ClueData asociado.
        /// </summary>
        public void UpdateNoteVisuals()
        {
            if (clueData == null) return;

            // Texto en la nota física
            if (inWorldNoteText != null)
            {
                inWorldNoteText.text = $"<b>{clueData.clueName}</b>\n<size=75%>{clueData.description}</size>\n\n<color=#CC2200><size=140%><b>{clueData.revealedValue}</b></size></color>\n<size=65%>Posición #{clueData.sequenceIndex + 1}</size>";
            }

            // Texto en la ventana emergente / popup
            if (popupTitle != null) popupTitle.text = clueData.clueName;
            if (popupBody != null) popupBody.text = clueData.description;
            if (popupDigitHint != null)
            {
                popupDigitHint.text = $"<color=#FFCC00>DÍGITO DE LA BOMBA:</color> <size=130%><b>[ {clueData.revealedValue} ]</b></size> (Posición #{clueData.sequenceIndex + 1})";
            }
        }
    }
}
