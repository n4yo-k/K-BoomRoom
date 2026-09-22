using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using DefusalGame.Data;

namespace DefusalGame.Gameplay
{
    /// <summary>
    /// Componente de agarre físico en microgravedad / gravedad cero para notas y objetos interactivos.
    /// Permite sostener la nota con mandos de VR o clic en PC, y al soltarla, flota suavemente en el espacio
    /// con amortiguación inercial (Antigravity Floating Physics).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class AntigravityGrabInteractable : MonoBehaviour
    {
        [Header("Comportamiento Antigravity al Soltar")]
        [Tooltip("Si es true, la nota flota en el aire sin caer al suelo al ser liberada")]
        public bool floatInZeroGravity = true;

        [Tooltip("Fricción del aire / amortiguación lineal en microgravedad")]
        public float floatingDrag = 1.2f;

        [Tooltip("Amortiguación rotacional")]
        public float floatingAngularDrag = 1.0f;

        [Header("Efecto de Impulso al Soltar")]
        public float releaseThrowMultiplier = 1.2f;

        [Header("Referencias")]
        public VRNoteInteractable vrNoteComponent;

        private Rigidbody rb;
        private XRGrabInteractable xriGrab;
        private bool isCurrentlyHeld = false;
        private Transform holdingHandTransform;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            vrNoteComponent = GetComponent<VRNoteInteractable>();

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                gameObject.layer = interactableLayer;
            }

            SetupPhysicsProperties();
            SetupXRIIntegration();
        }

        private void SetupPhysicsProperties()
        {
            if (rb != null)
            {
                rb.useGravity = !floatInZeroGravity;
                rb.linearDamping = floatingDrag;
                rb.angularDamping = floatingAngularDrag;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        private void SetupXRIIntegration()
        {
            xriGrab = GetComponent<XRGrabInteractable>();
            if (xriGrab == null)
            {
                xriGrab = gameObject.AddComponent<XRGrabInteractable>();
            }

            xriGrab.movementType = XRGrabInteractable.MovementType.VelocityTracking;
            xriGrab.selectEntered.AddListener(OnXRISelectEntered);
            xriGrab.selectExited.AddListener(OnXRISelectExited);
        }

        private void OnXRISelectEntered(SelectEnterEventArgs args)
        {
            isCurrentlyHeld = true;
            holdingHandTransform = args.interactorObject.transform;

            if (vrNoteComponent != null)
            {
                vrNoteComponent.OnNoteInteracted();
                vrNoteComponent.SetPopupActive(true);
            }
        }

        private void OnXRISelectExited(SelectExitEventArgs args)
        {
            isCurrentlyHeld = false;
            holdingHandTransform = null;

            if (rb != null && floatInZeroGravity)
            {
                rb.useGravity = false;
                rb.linearDamping = floatingDrag;
                rb.angularDamping = floatingAngularDrag;
            }

            if (vrNoteComponent != null)
            {
                vrNoteComponent.SetPopupActive(false);
            }
        }

        /// <summary>
        /// Llamado cuando una mano con física Antigravity toca o agarra la nota.
        /// </summary>
        public void OnTouchedByHand(AntigravityHandPhysics hand)
        {
            if (vrNoteComponent != null)
            {
                vrNoteComponent.OnNoteInteracted();
                vrNoteComponent.TogglePopup();
            }

            // Aplicar un pequeño impulso físico de flotación al tocarla
            if (rb != null && !isCurrentlyHeld)
            {
                Vector3 touchImpulse = (transform.position - hand.transform.position).normalized * 0.4f + Vector3.up * 0.2f;
                rb.AddForce(touchImpulse, ForceMode.Impulse);
            }
        }

        // Interacción directa en PC / Editor
        void OnMouseDown()
        {
            if (vrNoteComponent != null)
            {
                vrNoteComponent.OnNoteInteracted();
                vrNoteComponent.TogglePopup();
            }

            if (rb != null && floatInZeroGravity)
            {
                rb.useGravity = false;
                // Pequeña propulsión flotante al hacer clic
                rb.AddForce(Vector3.up * 0.35f, ForceMode.Impulse);
            }
        }
    }
}
