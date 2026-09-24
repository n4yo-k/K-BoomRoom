using UnityEngine;
using DefusalGame.Bomb;

namespace DefusalGame.Gameplay
{
    public class FuseBoxInteractable : MonoBehaviour
    {
        [Header("Estado de los Interruptores")]
        public bool switchA1 = true;
        public bool switchB2 = true; // El objetivo: apagar este desvía el sobrevoltaje
        public bool switchC3 = true;
        public bool switchD4 = true;

        [Header("Referencias a Palancas")]
        public Transform leverA1;
        public Transform leverB2;
        public Transform leverC3;
        public Transform leverD4;

        [Header("Feedback")]
        public Light statusLed;

        public void FlipSwitch(string switchId)
        {
            var bomb = Object.FindAnyObjectByType<Level3MultiStageBomb>();

            if (switchId == "B2")
            {
                switchB2 = !switchB2;
                UpdateLeverVisual(leverB2, switchB2);

                if (!switchB2)
                {
                    // ¡Apagado con éxito!
                    if (statusLed != null) statusLed.color = Color.green;
                    if (bomb != null) bomb.DisarmOvervoltage();
                    Debug.Log("[FuseBox] ¡Circuito B2 desconectado! La sobretensión de la bomba ha cesado.");
                }
                else
                {
                    if (statusLed != null) statusLed.color = Color.red;
                }
            }
            else if (switchId == "A1")
            {
                switchA1 = !switchA1;
                UpdateLeverVisual(leverA1, switchA1);
                Debug.Log("[FuseBox] Circuito A1 conmutado (Luces de la despensa).");
            }
            else if (switchId == "C3")
            {
                switchC3 = !switchC3;
                UpdateLeverVisual(leverC3, switchC3);
                Debug.Log("[FuseBox] Circuito C3 conmutado (Tomas auxiliares).");
            }
            else if (switchId == "D4")
            {
                switchD4 = !switchD4;
                UpdateLeverVisual(leverD4, switchD4);
                Debug.Log("[FuseBox] Circuito D4 conmutado (Ventilación).");
            }
        }

        private void UpdateLeverVisual(Transform lever, bool stateOn)
        {
            if (lever != null)
            {
                lever.localRotation = Quaternion.Euler(stateOn ? 30f : -30f, 0f, 0f);
            }
        }
    }
}
