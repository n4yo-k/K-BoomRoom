using UnityEngine;
using TMPro;
using DefusalGame.VR;

namespace DefusalGame.Gameplay
{
    public class UVClueMarker : MonoBehaviour
    {
        [Header("Contenido de la Pista")]
        public string clueText = "7 5 3 1";
        public string labelDescription = "<size=70%>CÓDIGO DE DESACTIVACIÓN:</size>\n<b>7  5  3  1</b>";

        [Header("Componentes Visuales")]
        public TextMeshPro textMesh;

        [Header("Ajustes de Revelación")]
        public float revealDistance = 8.5f;
        public float revealAngle = 70f;

        private Color targetEmissionColor = new Color(0.2f, 1.0f, 0.6f); // Verde neón fluorescente intenso
        private float currentAlpha = 0.0f;

        void Awake()
        {
            if (textMesh == null) textMesh = GetComponentInChildren<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = labelDescription;
                textMesh.color = new Color(targetEmissionColor.r, targetEmissionColor.g, targetEmissionColor.b, 0.0f);
                textMesh.alpha = 0.0f;
            }
        }

        void Update()
        {
            bool isIlluminated = false;

            // 1. Modo Escritorio (Linterna acoplada a la cámara del jugador)
            var uvController = Object.FindAnyObjectByType<UVFlashlightController>();
            if (uvController != null && uvController.isUVActive)
            {
                Transform lightT = (uvController.uvLightSource != null) ? uvController.uvLightSource.transform : uvController.transform;
                if (CheckIllumination(lightT))
                {
                    isIlluminated = true;
                }
            }

            // 2. Modo Realidad Virtual (Linterna sostenida con la mano del avatar en RV)
            if (!isIlluminated)
            {
                var vrGrab = Object.FindAnyObjectByType<VRItemGrabHandler>();
                if (vrGrab != null && vrGrab.itemType == Level3ItemType.UVFlashlight && vrGrab.isUVOn)
                {
                    Transform lightT = (vrGrab.uvLightSource != null) ? vrGrab.uvLightSource.transform : vrGrab.transform;
                    if (CheckIllumination(lightT))
                    {
                        isIlluminated = true;
                    }
                }
            }

            // 3. Fallback adicional si el inventario tiene la linterna y está activa
            if (!isIlluminated && PlayerInventory.Instance != null && PlayerInventory.Instance.hasUVLight)
            {
                if (uvController != null && uvController.isUVActive)
                {
                    float dist = Vector3.Distance(transform.position, uvController.transform.position);
                    if (dist < 4.0f)
                    {
                        isIlluminated = true;
                    }
                }
            }

            // Transición suave de fluorescencia
            float targetA = isIlluminated ? 1.0f : 0.0f;
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetA, Time.deltaTime * 5.0f);

            if (textMesh != null)
            {
                textMesh.color = new Color(targetEmissionColor.r, targetEmissionColor.g, targetEmissionColor.b, currentAlpha);
                textMesh.alpha = currentAlpha;
            }
        }

        private bool CheckIllumination(Transform lightTransform)
        {
            if (lightTransform == null) return false;

            float dist = Vector3.Distance(transform.position, lightTransform.position);
            if (dist <= revealDistance)
            {
                Vector3 toMarker = (transform.position - lightTransform.position).normalized;
                float angle = Vector3.Angle(lightTransform.forward, toMarker);
                if (angle < revealAngle)
                {
                    return true;
                }
                // Si está muy cerca (menos de 2.5m) en la habitación, iluminar con ángulo más amplio
                if (dist <= 2.5f && angle < 85f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
