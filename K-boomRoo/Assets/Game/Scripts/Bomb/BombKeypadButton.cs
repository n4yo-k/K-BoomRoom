using UnityEngine;

namespace DefusalGame.Bomb
{
    public class BombKeypadButton : MonoBehaviour
    {
        [Header("Valor del Botón")]
        [Tooltip("Dígito '0'-'9', 'CLR' para borrar, o 'ENT' para enviar")]
        public string keyValue = "1";

        private BombController controller;
        private Vector3 initialLocalPos;
        private bool isPressed = false;
        private Renderer btnRenderer;
        private Color originalColor = Color.white;

        void Awake()
        {
            initialLocalPos = transform.localPosition;
            controller = GetComponentInParent<BombController>();
            btnRenderer = GetComponent<Renderer>();
            if (btnRenderer != null && btnRenderer.sharedMaterial != null)
            {
                originalColor = btnRenderer.sharedMaterial.color;
            }
        }

        public void SetController(BombController ctrl, string key)
        {
            controller = ctrl;
            keyValue = key;
        }

        // Clic de ratón en Editor / PC
        void OnMouseDown()
        {
            Press();
        }

        // Detección física / Poke en VR
        void OnTriggerEnter(Collider other)
        {
            if (!isPressed && (other.CompareTag("Player") || other.name.Contains("Hand") || other.name.Contains("Controller") || other.name.Contains("Poke")))
            {
                Press();
            }
        }

        public void Press()
        {
            if (isPressed) return;
            if (controller == null) controller = GetComponentInParent<BombController>();
            if (controller != null)
            {
                controller.OnKeyPressed(keyValue);
            }
            else
            {
                var l3Bomb = GetComponentInParent<Level3MultiStageBomb>();
                if (l3Bomb != null)
                {
                    l3Bomb.OnKeyPressed(keyValue);
                }
            }

            StartCoroutine(ButtonFeedbackRoutine());
        }

        private System.Collections.IEnumerator ButtonFeedbackRoutine()
        {
            isPressed = true;
            // Desplazamiento hacia adentro (eje Z local o Y según orientación)
            Vector3 pressedPos = initialLocalPos + Vector3.back * 0.006f;
            transform.localPosition = pressedPos;

            if (btnRenderer != null)
            {
                btnRenderer.material.color = Color.yellow;
            }

            yield return new WaitForSeconds(0.12f);

            transform.localPosition = initialLocalPos;
            if (btnRenderer != null)
            {
                btnRenderer.material.color = originalColor;
            }

            isPressed = false;
        }
    }
}
