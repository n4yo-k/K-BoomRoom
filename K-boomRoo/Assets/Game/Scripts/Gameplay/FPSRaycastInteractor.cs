using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DefusalGame.Bomb;
using DefusalGame.Gameplay;

namespace DefusalGame.Gameplay
{
    public class FPSRaycastInteractor : MonoBehaviour
    {
        [Header("Distancia de Interaccion")]
        public float interactDistance = 3.0f;
        public LayerMask interactMask = ~0;

        private Camera playerCam;
        private Texture2D dotTexture;
        private string hoverPrompt = "";
        private GUIStyle promptStyle;

        void Awake()
        {
            playerCam = GetComponent<Camera>();
            if (playerCam == null) playerCam = Camera.main;

            dotTexture = new Texture2D(1, 1);
            dotTexture.SetPixel(0, 0, Color.white);
            dotTexture.Apply();
        }

        void Update()
        {
            hoverPrompt = "";

            if (playerCam == null) return;

            // Comprobar si hay alguna hoja abierta inspeccionándose
            var activeDossier = Object.FindAnyObjectByType<MissionDossier>();
            if (activeDossier != null && activeDossier.isInspecting)
            {
                return;
            }

            Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
            bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask);

            BombKeypadButton hoveredBtn = null;
            ClueInteractable hoveredClue = null;
            MissionDossier hoveredDossier = null;

            if (hasHit)
            {
                hoveredBtn = hit.collider.GetComponentInParent<BombKeypadButton>();
                hoveredClue = hit.collider.GetComponentInParent<ClueInteractable>();
                hoveredDossier = hit.collider.GetComponentInParent<MissionDossier>();

                if (hoveredDossier != null)
                {
                    hoverPrompt = "[E / Click] Leer Hoja de Instrucciones";
                }
                else if (hoveredBtn != null)
                {
                    hoverPrompt = $"[E / Click] Pulsar Tecla [{hoveredBtn.keyValue}]";
                }
                else if (hoveredClue != null)
                {
                    hoverPrompt = $"[E / Click] Inspeccionar: {hoveredClue.clueData?.clueName ?? "Evidencia"}";
                }
            }

            bool triggerInteraction = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
                triggerInteraction = true;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                triggerInteraction = true;
#else
            if ((Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked) || Input.GetKeyDown(KeyCode.E))
                triggerInteraction = true;
#endif

            if (triggerInteraction && hasHit)
            {
                if (hoveredDossier != null)
                {
                    hoveredDossier.ToggleInspection();
                    return;
                }

                if (hoveredBtn != null)
                {
                    hoveredBtn.Press();
                    return;
                }

                if (hoveredClue != null)
                {
                    hoveredClue.Interact();
                    return;
                }
            }
        }

        void OnGUI()
        {
            var activeDossier = Object.FindAnyObjectByType<MissionDossier>();
            if (activeDossier != null && activeDossier.isInspecting)
            {
                return;
            }

            // Reticula central cuando el raton esta bloqueado
            if (Cursor.lockState == CursorLockMode.Locked && dotTexture != null)
            {
                float size = 4f;
                float x = (Screen.width - size) * 0.5f;
                float y = (Screen.height - size) * 0.5f;
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                GUI.DrawTexture(new Rect(x, y, size, size), dotTexture);

                // Tooltip dinámico sobre el objeto apuntado
                if (!string.IsNullOrEmpty(hoverPrompt))
                {
                    if (promptStyle == null)
                    {
                        promptStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 16,
                            fontStyle = FontStyle.Bold,
                            alignment = TextAnchor.UpperCenter,
                            normal = { textColor = new Color(1f, 0.95f, 0.4f) }
                        };
                    }

                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.Box(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f + 25f, 360f, 30f), GUIContent.none);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f + 28f, 360f, 30f), hoverPrompt, promptStyle);
                }
            }
        }
    }
}
