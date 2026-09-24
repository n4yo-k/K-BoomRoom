using UnityEngine;
using TMPro;

namespace DefusalGame.Gameplay
{
    public class UVClueMarker : MonoBehaviour
    {
        [Header("Contenido de la Pista")]
        public string clueText = "7";
        public string labelDescription = "DÍGITO 1 = [ 7 ]";

        [Header("Componentes Visuales")]
        public TextMeshPro textMesh;
        public Renderer surfaceRenderer;

        [Header("Ajustes de Revelación")]
        public float revealDistance = 3.5f;

        private Color targetEmissionColor = new Color(0.1f, 1.0f, 0.6f); // Verde neón fluorescente
        private float currentAlpha = 0.0f;

        void Awake()
        {
            if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = labelDescription;
                textMesh.color = new Color(targetEmissionColor.r, targetEmissionColor.g, targetEmissionColor.b, 0.0f);
            }
        }

        void Update()
        {
            var uvController = Object.FindAnyObjectByType<UVFlashlightController>();
            bool isIlluminated = false;

            if (uvController != null && uvController.isUVActive)
            {
                float dist = Vector3.Distance(transform.position, uvController.transform.position);
                if (dist <= revealDistance)
                {
                    Vector3 toMarker = (transform.position - uvController.transform.position).normalized;
                    float angle = Vector3.Angle(uvController.transform.forward, toMarker);
                    if (angle < 45f) // Dentro del cono del foco UV
                    {
                        isIlluminated = true;
                    }
                }
            }

            // Suave transición de desvanecimiento
            float targetA = isIlluminated ? 1.0f : 0.0f;
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetA, Time.deltaTime * 3.5f);

            if (textMesh != null)
            {
                textMesh.color = new Color(targetEmissionColor.r, targetEmissionColor.g, targetEmissionColor.b, currentAlpha);
            }
        }
    }
}
