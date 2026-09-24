using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using DefusalGame.Bomb;
using DefusalGame.Gameplay;

namespace DefusalGame.VR
{
    /// <summary>
    /// Puente que conecta los interactores de Realidad Virtual (Ray Interactor, Direct Interactor y Poke)
    /// con los componentes de la bomba, cuadro de fusibles, cables y microondas.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class VRBombInteractableBridge : MonoBehaviour
    {
        [Header("Tipo de Acción")]
        public bool isKeypadButton = false;
        public bool isFuseSwitch = false;
        public bool isBombWire = false;
        public bool isMicrowave = false;

        private XRSimpleInteractable interactable;
        private AudioSource audioSrc;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            audioSrc = GetComponent<AudioSource>();
            if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();

            // Configurar interactable
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.hoverEntered.AddListener(OnHoverEntered);
        }

        void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelectEntered);
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Vibración háptica en el mando de RV
            TriggerHaptic(args.interactorObject, 0.6f, 0.12f);

            // 1. Botón de Teclado Numérico de la Bomba
            if (isKeypadButton || GetComponent<BombKeypadButton>() != null)
            {
                var btn = GetComponent<BombKeypadButton>();
                if (btn != null)
                {
                    btn.Press();
                    return;
                }
            }

            // 2. Interruptor del Cuadro Eléctrico
            if (isFuseSwitch || GetComponent<Level3FuseSwitchInteractable>() != null)
            {
                var sw = GetComponent<Level3FuseSwitchInteractable>();
                if (sw != null)
                {
                    sw.Interact();
                    return;
                }
            }

            // 3. Cable de la Bomba
            if (isBombWire || GetComponent<Level3WireInteractable>() != null)
            {
                var wire = GetComponent<Level3WireInteractable>();
                if (wire != null)
                {
                    wire.Interact();
                    return;
                }
            }

            // 4. Microondas Interactivo
            if (isMicrowave || GetComponent<InteractiveMicrowave>() != null)
            {
                var mw = GetComponent<InteractiveMicrowave>();
                if (mw != null)
                {
                    mw.Interact();
                    return;
                }
            }

            // Fallback: Si tiene ItemPickup
            var pickup = GetComponent<Level3ItemPickup>();
            if (pickup != null)
            {
                pickup.PickUp();
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // Suave pulsación háptica al apuntar al objeto
            TriggerHaptic(args.interactorObject, 0.2f, 0.05f);
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
