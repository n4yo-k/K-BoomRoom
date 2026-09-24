using UnityEngine;

namespace DefusalGame.Gameplay
{
    public enum Level3ItemType
    {
        FrozenIceBlock,
        Pliers,
        UVFlashlight
    }

    public class Level3ItemPickup : MonoBehaviour
    {
        public Level3ItemType itemType;
        public string itemName = "Objeto";

        public void PickUp()
        {
            var inv = PlayerInventory.Instance;
            if (inv != null)
            {
                switch (itemType)
                {
                    case Level3ItemType.FrozenIceBlock:
                        inv.CollectIceBlock();
                        break;
                    case Level3ItemType.Pliers:
                        inv.CollectPliers();
                        break;
                    case Level3ItemType.UVFlashlight:
                        inv.CollectUV();
                        break;
                }
            }

            gameObject.SetActive(false);
            Debug.Log($"[Pickup] Has recogido: {itemName}");
        }
    }
}
