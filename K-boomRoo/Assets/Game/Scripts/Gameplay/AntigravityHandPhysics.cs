using System;
using UnityEngine;
#if UNITY_XR_MANAGEMENT
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace DefusalGame.Gameplay
{
    /// <summary>
    /// Componente de física para las manos virtuales del jugador en Antigravity.
    /// Permite interacción táctil directa con botones físicos (Poke/Press), colisión con objetos
    /// y propulsión inercial al empujarse contra paredes o mesas de la sala.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AntigravityHandPhysics : MonoBehaviour
    {
        public enum HandType { Left, Right, Generic }

        [Header("Configuración de Mano")]
        public HandType handType = HandType.Generic;
        public float touchRadius = 0.04f;
        public bool enablePushOffWalls = true;
        public float pushOffImpulseFactor = 1.0f;

        [Header("Referencias")]
        public AntigravityPlayerController playerController;

        private Collider handCollider;
        private Vector3 lastPosition;
        private Vector3 handVelocity;

        void Awake()
        {
            int handsLayer = LayerMask.NameToLayer("Hands");
            if (handsLayer >= 0)
            {
                gameObject.layer = handsLayer;
            }

            handCollider = GetComponent<Collider>();
            if (handCollider != null)
            {
                handCollider.isTrigger = true; // Permite eventos OnTriggerEnter precisos para poke / interactables
            }

            if (playerController == null)
            {
                playerController = GetComponentInParent<AntigravityPlayerController>();
            }

            lastPosition = transform.position;
        }

        void FixedUpdate()
        {
            // Calcular velocidad física del movimiento de la mano
            if (Time.fixedDeltaTime > 0f)
            {
                handVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
                lastPosition = transform.position;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHandTouch(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // Opcional: empuje continuo si se presiona activamente contra una pared
            if (enablePushOffWalls && playerController != null)
            {
                int archLayer = LayerMask.NameToLayer("RoomArchitecture");
                if (archLayer >= 0 && other.gameObject.layer == archLayer)
                {
                    // Si la mano se mueve hacia la pared y el jugador empuja
                    Vector3 toSurface = (other.ClosestPoint(transform.position) - transform.position).normalized;
                    float approachSpeed = Vector3.Dot(handVelocity, toSurface);
                    if (approachSpeed < -0.3f)
                    {
                        Vector3 pushDirection = -toSurface;
                        playerController.PushOffSurface(pushDirection, pushOffImpulseFactor * 0.4f);
                    }
                }
            }
        }

        private void ProcessHandTouch(Collider other)
        {
            // 1. Botón Físico de Guardado Antigravity
            var saveBtn = other.GetComponentInParent<Save.AntigravityPhysicalButton>();
            if (saveBtn != null)
            {
                saveBtn.Press();
                TriggerHapticFeedback();
                return;
            }

            // 2. Botón del Teclado de la Bomba (C4 Keypad)
            var keypadBtn = other.GetComponentInParent<Bomb.BombKeypadButton>();
            if (keypadBtn != null)
            {
                keypadBtn.Press();
                TriggerHapticFeedback();
                return;
            }

            // 3. Notas / Interactuables (Requieren click o E explícito, no auto-trigger por proximidad)

            // 4. Empujarse contra paredes de la arquitectura
            if (enablePushOffWalls && playerController != null)
            {
                int archLayer = LayerMask.NameToLayer("RoomArchitecture");
                if (archLayer >= 0 && other.gameObject.layer == archLayer)
                {
                    Vector3 contactPoint = other.ClosestPoint(transform.position);
                    Vector3 normal = (transform.position - contactPoint).normalized;
                    if (normal == Vector3.zero) normal = -transform.forward;

                    playerController.PushOffSurface(normal, pushOffImpulseFactor);
                    TriggerHapticFeedback(0.3f, 0.1f);
                }
            }
        }

        public void TriggerHapticFeedback(float intensity = 0.5f, float duration = 0.12f)
        {
            // Soporte de háptica si se encuentra un interactor o controlador XR
            var interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>();
            if (interactor != null)
            {
                interactor.SendHapticImpulse(intensity, duration);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = (handType == HandType.Left) ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
        }
    }
}
