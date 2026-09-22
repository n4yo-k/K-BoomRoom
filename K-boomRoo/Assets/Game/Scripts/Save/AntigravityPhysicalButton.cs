using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DefusalGame.Save
{
    /// <summary>
    /// Botón 3D físico interactuable mediante contacto directo de manos Antigravity,
    /// pokes de VR o clics en PC. Simula recorrido mecánico, rebote de resorte y feedback sonoro.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AntigravityPhysicalButton : MonoBehaviour
    {
        [Header("Parámetros de Pulsación 3D")]
        [Tooltip("Dirección local en la que el botón se hunde")]
        public Vector3 pressDirection = new Vector3(0f, 0f, -1f);

        [Tooltip("Distancia de compresión en metros")]
        public float pressDistance = 0.025f;

        [Tooltip("Tiempo en segundos que tarda en rebotar")]
        public float returnDuration = 0.15f;

        [Header("Colores y Material")]
        public Renderer buttonRenderer;
        public Color idleColor = new Color(0.2f, 0.65f, 1.0f);
        public Color pressedColor = new Color(0.1f, 1.0f, 0.4f);

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip pressSound;

        [Header("Eventos")]
        public UnityEvent onButtonPressed;

        private Vector3 restingLocalPos;
        private bool isPressed = false;
        private Coroutine pressRoutine;

        void Awake()
        {
            restingLocalPos = transform.localPosition;

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                gameObject.layer = interactableLayer;
            }

            if (buttonRenderer == null)
            {
                buttonRenderer = GetComponent<Renderer>();
            }

            if (buttonRenderer != null && buttonRenderer.material != null)
            {
                buttonRenderer.material.color = idleColor;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        public void Press()
        {
            if (isPressed) return;

            PlayPressFeedback();
            onButtonPressed?.Invoke();

            if (pressRoutine != null) StopCoroutine(pressRoutine);
            pressRoutine = StartCoroutine(AnimatePressRoutine());
        }

        private void PlayPressFeedback()
        {
            if (pressSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pressSound);
            }
            else if (audioSource != null)
            {
                // Síntesis de sonido táctico si no hay AudioClip asignado
                audioSource.pitch = 1.25f;
                audioSource.Play();
            }
        }

        private IEnumerator AnimatePressRoutine()
        {
            isPressed = true;

            // Hundir botón
            Vector3 targetPos = restingLocalPos + pressDirection.normalized * pressDistance;
            transform.localPosition = targetPos;

            if (buttonRenderer != null && buttonRenderer.material != null)
            {
                buttonRenderer.material.color = pressedColor;
            }

            yield return new WaitForSeconds(0.12f);

            // Retornar suavemente al reposo (resorte mecánico)
            float elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                transform.localPosition = Vector3.Lerp(targetPos, restingLocalPos, t);
                yield return null;
            }

            transform.localPosition = restingLocalPos;
            if (buttonRenderer != null && buttonRenderer.material != null)
            {
                buttonRenderer.material.color = idleColor;
            }

            isPressed = false;
            pressRoutine = null;
        }

        // Detección por colisión de manos / dedos VR
        private void OnTriggerEnter(Collider other)
        {
            if (!isPressed && (other.CompareTag("Player") || other.name.Contains("Hand") || other.name.Contains("Controller") || other.name.Contains("Poke") || other.gameObject.layer == LayerMask.NameToLayer("Hands")))
            {
                Press();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isPressed && (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Hand") || collision.gameObject.layer == LayerMask.NameToLayer("Hands")))
            {
                Press();
            }
        }

        // Clic de ratón para pruebas en PC
        private void OnMouseDown()
        {
            Press();
        }
    }
}
