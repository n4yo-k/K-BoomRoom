using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;

namespace DefusalGame.VR
{
    /// <summary>
    /// Gestiona de forma inteligente el modo de juego entre Realidad Virtual (RV) y Escritorio (PC FPV).
    /// - Si detecta un casco de RV activo (Meta Quest / OpenXR / Quest Link), activa el Rig de Realidad Virtual y el HUD espacial 3D.
    /// - Si no hay casco conectado, activa automáticamente el modo Escritorio (WASD + Ratón) con su HUD en pantalla.
    /// </summary>
    public class VRPlayerRigManager : MonoBehaviour
    {
        [Header("Referencias de Rigs")]
        public GameObject vrOriginRig;
        public GameObject desktopFpvRig;
        public GameObject spatialHUD;

        [Header("Modo de Juego")]
        [Tooltip("Por defecto false para permitir jugar inmediatamente en PC con teclado/ratón. Si se activa, fuerza el Rig de RV.")]
        public bool forceVRMode = false;

        void Awake()
        {
            ApplyMode();
        }

        public void ApplyMode()
        {
            bool isVR = CheckVRPresence() || forceVRMode;

            if (isVR && vrOriginRig != null)
            {
                vrOriginRig.SetActive(true);
                if (desktopFpvRig != null) desktopFpvRig.SetActive(false);
                if (spatialHUD != null) spatialHUD.SetActive(true);
                Debug.Log("[VRPlayerRigManager] Modo Realidad Virtual (RV) ACTIVO. Mandos y visor activados.");
            }
            else
            {
                if (desktopFpvRig != null) desktopFpvRig.SetActive(true);
                if (vrOriginRig != null) vrOriginRig.SetActive(false);
                if (spatialHUD != null) spatialHUD.SetActive(false);
                Debug.Log("[VRPlayerRigManager] Modo Escritorio PC (FPV) ACTIVO. Controles WASD + Ratón.");
            }
        }

        private bool CheckVRPresence()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays)
            {
                if (d.running) return true;
            }
            return false;
        }
    }
}
