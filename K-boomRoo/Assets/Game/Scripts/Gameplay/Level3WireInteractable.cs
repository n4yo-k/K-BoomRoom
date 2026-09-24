using UnityEngine;
using DefusalGame.Bomb;

namespace DefusalGame.Gameplay
{
    public class Level3WireInteractable : MonoBehaviour
    {
        public string wireColor = "AZUL"; // ROJO, AZUL, VERDE, AMARILLO

        public void Interact()
        {
            var bomb = Object.FindAnyObjectByType<Level3MultiStageBomb>();
            if (bomb != null)
            {
                bomb.CutWire(wireColor);
            }
        }
    }
}
