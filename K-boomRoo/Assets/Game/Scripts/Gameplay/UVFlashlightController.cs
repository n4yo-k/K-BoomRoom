using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DefusalGame.Gameplay
{
    public class UVFlashlightController : MonoBehaviour
    {
        [Header("Estado de la Linterna UV")]
        public bool hasPickedUpUV = false;
        public bool isUVActive = false;

        [Header("Componentes de Luz")]
        public Light uvLightSource;

        private AudioSource audioSrc;

        void Awake()
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();

            if (uvLightSource == null)
            {
                Transform childLight = transform.Find("UV_SpotLight");
                if (childLight != null) uvLightSource = childLight.GetComponent<Light>();
            }

            UpdateLightState();
        }

        void Update()
        {
            if (!hasPickedUpUV) return;

            bool toggle = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                toggle = true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                toggle = true;
#else
            if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1))
                toggle = true;
#endif

            if (toggle)
            {
                ToggleUV();
            }
        }

        public void PickUpFlashlight()
        {
            hasPickedUpUV = true;
            isUVActive = true;
            UpdateLightState();
            Debug.Log("[UVFlashlight] ¡Linterna de luz ultravioleta recogida! Pulsa [F] o Clic Derecho para encender/apagar.");
        }

        public void ToggleUV()
        {
            if (!hasPickedUpUV) return;

            isUVActive = !isUVActive;
            UpdateLightState();
        }

        private void UpdateLightState()
        {
            if (uvLightSource != null)
            {
                uvLightSource.enabled = hasPickedUpUV && isUVActive;
            }
        }
    }
}
