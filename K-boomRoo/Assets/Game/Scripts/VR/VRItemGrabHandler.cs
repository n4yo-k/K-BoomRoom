using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using DefusalGame.Gameplay;

namespace DefusalGame.VR
{
    /// <summary>
    /// Manejador para objetos que se pueden agarrar físicamente con las manos en Realidad Virtual
    /// (Linterna UV, Bloque de Hielo con Alicates, Pinzas descongeladas).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class VRItemGrabHandler : MonoBehaviour
    {
        public Level3ItemType itemType = Level3ItemType.UVFlashlight;

        [Header("Específico de Linterna UV")]
        public Light uvLightSource;
        public bool isUVOn = false;

        private XRGrabInteractable grabInteractable;
        private AudioSource audioSrc;

        void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();

            // Configurar Rigidbody para física agradable en RV
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            // Configurar interactable
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            grabInteractable.activated.AddListener(OnActivated);

            // Si es linterna, buscar luz hija
            if (itemType == Level3ItemType.UVFlashlight && uvLightSource == null)
            {
                uvLightSource = GetComponentInChildren<Light>();
                if (uvLightSource != null)
                {
                    uvLightSource.enabled = isUVOn;
                }
            }
        }

        void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
                grabInteractable.activated.RemoveListener(OnActivated);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            TriggerHaptic(args.interactorObject, 0.5f, 0.1f);

            var inv = PlayerInventory.Instance;
            if (inv != null)
            {
                switch (itemType)
                {
                    case Level3ItemType.UVFlashlight:
                        inv.CollectUV();
                        // Activar UVFlashlightController si existe en el jugador
                        var uvCtrl = Object.FindAnyObjectByType<UVFlashlightController>();
                        if (uvCtrl != null)
                        {
                            uvCtrl.hasPickedUpUV = true;
                            uvCtrl.isUVActive = isUVOn;
                        }
                        break;

                    case Level3ItemType.FrozenIceBlock:
                        inv.CollectIceBlock();
                        break;

                    case Level3ItemType.Pliers:
                        inv.CollectPliers();
                        break;
                }
            }

            Debug.Log($"[VRItemGrab] Objeto agarrado en RV: {itemType}");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            TriggerHaptic(args.interactorObject, 0.3f, 0.05f);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            // Evento al presionar el Gatillo (Trigger) con el objeto sostenido en la mano
            if (itemType == Level3ItemType.UVFlashlight)
            {
                ToggleUVFlashlight(args.interactorObject);
            }
        }

        public void ToggleUVFlashlight(IXRInteractor interactor = null)
        {
            isUVOn = !isUVOn;
            if (uvLightSource != null)
            {
                uvLightSource.enabled = isUVOn;
            }

            // Sincronizar con el controlador global para que los marcadores de pared reaccionen
            var uvCtrl = Object.FindAnyObjectByType<UVFlashlightController>();
            if (uvCtrl != null)
            {
                uvCtrl.isUVActive = isUVOn;
                if (uvCtrl.uvLightSource != null)
                {
                    uvCtrl.uvLightSource.enabled = isUVOn;
                }
            }

            // Efecto de sonido click
            if (audioSrc != null)
            {
                PlayClickSound();
            }

            if (interactor != null)
            {
                TriggerHaptic(interactor, 0.7f, 0.08f);
            }

            Debug.Log($"[VRItemGrab] Linterna UV en mano: {(isUVOn ? "ENCENDIDA" : "APAGADA")}");
        }

        private void PlayClickSound()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.05f);
            AudioClip clip = AudioClip.Create("VR_Click", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = isUVOn ? (1200f - t * 400f) : (800f + t * 400f);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * (1f - t) * 0.4f;
            }
            clip.SetData(data, 0);
            audioSrc.PlayOneShot(clip, 0.6f);
        }

        private void TriggerHaptic(IXRInteractor interactor, float amplitude, float duration)
        {
            if (interactor is XRBaseInputInteractor controllerInteractor)
            {
                controllerInteractor.SendHapticImpulse(amplitude, duration);
            }
        }
    }
}
