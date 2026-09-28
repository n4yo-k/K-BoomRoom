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
        public Light uvFillLight;

        private AudioSource audioSrc;

        void Awake()
        {
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();

            if (uvLightSource == null)
            {
                Transform childLight = transform.Find("Player_UV_SpotLight");
                if (childLight == null) childLight = transform.Find("UV_SpotLight");
                if (childLight != null) uvLightSource = childLight.GetComponent<Light>();
            }

            if (uvFillLight == null)
            {
                Transform childFill = transform.Find("Player_UV_FillLight");
                if (childFill != null) uvFillLight = childFill.GetComponent<Light>();
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
            PlayClickSound(true);
            Debug.Log("[UVFlashlight] ¡Linterna UV activada! Pulsa [F] o Clic Derecho para encender/apagar.");
        }

        public void ToggleUV()
        {
            if (!hasPickedUpUV) return;

            isUVActive = !isUVActive;
            UpdateLightState();
            PlayClickSound(isUVActive);
            Debug.Log($"[UVFlashlight] Linterna UV {(isUVActive ? "ENCENDIDA" : "APAGADA")}.");
        }

        private void UpdateLightState()
        {
            bool active = hasPickedUpUV && isUVActive;
            if (uvLightSource != null)
            {
                uvLightSource.enabled = active;
            }
            if (uvFillLight != null)
            {
                uvFillLight.enabled = active;
            }
        }

        private void PlayClickSound(bool turningOn)
        {
            if (audioSrc == null) return;
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.04f);
            AudioClip clip = AudioClip.Create("UV_Click", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = turningOn ? (1400f - t * 500f) : (900f + t * 400f);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * (1f - t) * 0.45f;
            }
            clip.SetData(data, 0);
            audioSrc.PlayOneShot(clip, 0.7f);
        }
    }
}
