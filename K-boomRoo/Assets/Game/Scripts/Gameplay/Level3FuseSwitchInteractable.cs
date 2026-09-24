using UnityEngine;

namespace DefusalGame.Gameplay
{
    public class Level3FuseSwitchInteractable : MonoBehaviour
    {
        public string switchId = "B2";

        public void Interact()
        {
            var fuseBox = GetComponentInParent<FuseBoxInteractable>();
            if (fuseBox != null)
            {
                fuseBox.FlipSwitch(switchId);
            }
        }
    }
}
