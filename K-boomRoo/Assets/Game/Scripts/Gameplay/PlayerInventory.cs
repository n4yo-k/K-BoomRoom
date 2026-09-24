using UnityEngine;

namespace DefusalGame.Gameplay
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Objetos en Posesión")]
        public bool hasFrozenIceBlock = false;
        public bool hasPliers = false;
        public bool hasUVLight = false;

        void Awake()
        {
            Instance = this;
        }

        public void CollectIceBlock()
        {
            hasFrozenIceBlock = true;
            Debug.Log("[Inventory] ¡Bloque de hielo recogido! Contiene los alicates congelados dentro.");
        }

        public void CollectPliers()
        {
            hasPliers = true;
            Debug.Log("[Inventory] ¡Alicates aislados recogidos! Listos para cortar cables.");
        }

        public void CollectUV()
        {
            hasUVLight = true;
            var uv = GetComponentInChildren<UVFlashlightController>();
            if (uv != null) uv.PickUpFlashlight();
        }
    }
}
