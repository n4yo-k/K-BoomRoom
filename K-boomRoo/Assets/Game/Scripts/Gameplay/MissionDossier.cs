using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using DefusalGame.Data;

namespace DefusalGame.Gameplay
{
    public class MissionDossier : MonoBehaviour
    {
        [Header("Referencias UI")]
        public TextMeshPro documentText;

        [Header("Configuración de Pistas a Rastrear")]
        public ClueData clue1;
        public ClueData clue2;
        public ClueData clue3;
        public ClueData clue4;

        [Header("Estado de Inspección")]
        public bool isInspecting = false;

        private GUIStyle boxStyle;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle clueFoundStyle;
        private GUIStyle cluePendingStyle;
        private GUIStyle hintStyle;
        private GUIStyle closeButtonStyle;

        void Start()
        {
            UpdateDossierContent();
        }

        void Update()
        {
            UpdateDossierContent();

            // Si estamos inspeccionando y el jugador presiona ESC, E o Espacio, cerramos la hoja
            if (isInspecting)
            {
#if ENABLE_INPUT_SYSTEM
                if ((Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
                    (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
                {
                    CloseInspection();
                }
#else
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
                {
                    CloseInspection();
                }
#endif
            }
        }

        public void ToggleInspection()
        {
            if (isInspecting)
                CloseInspection();
            else
                OpenInspection();
        }

        public void OpenInspection()
        {
            isInspecting = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseInspection()
        {
            isInspecting = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UpdateDossierContent()
        {
            bool c1Found = (clue1 != null && clue1.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_DESK_DRAWER");
            bool c2Found = (clue2 != null && clue2.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_CORKBOARD");
            bool c3Found = (clue3 != null && clue3.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_BOOKSHELF");
            bool c4Found = (clue4 != null && clue4.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_TOOLBOX");

            string t1 = c1Found ? "<color=#007700><b>[✓] 1. ESTUDIO:</b></color> <b>4</b> (#1)" : "<b>[ ] 1. ESTUDIO:</b> Revisa cajones.";
            string t2 = c2Found ? "<color=#007700><b>[✓] 2. PIZARRA:</b></color> <b>8</b> (#2)" : "<b>[ ] 2. PIZARRA:</b> Examina pizarra.";
            string t3 = c3Found ? "<color=#007700><b>[✓] 3. ARCHIVO:</b></color> <b>2</b> (#3)" : "<b>[ ] 3. ARCHIVO:</b> Revisa libros.";
            string t4 = c4Found ? "<color=#007700><b>[✓] 4. CAJA:</b></color> <b>6</b> (#4)" : "<b>[ ] 4. CAJA:</b> Revisa herramientas.";

            int count = (c1Found ? 1 : 0) + (c2Found ? 1 : 0) + (c3Found ? 1 : 0) + (c4Found ? 1 : 0);

            if (documentText != null)
            {
                documentText.text = 
                    "<align=center><size=115%><b>ORDEN DE MISIÓN</b></size>\n" +
                    "<color=#881111><size=80%><b>DESACTIVACIÓN DE EMERGENCIA</b></size></color></align>\n" +
                    "-------------------------\n" +
                    "<size=85%>Bomba activa. Cruza la <b>puerta a tu izquierda</b> para buscar las 4 claves:\n\n" +
                    $"{t1}\n" +
                    $"{t2}\n" +
                    $"{t3}\n" +
                    $"{t4}\n\n" +
                    $"<b>Progreso: {count}/4 pistas</b>\n" +
                    "-------------------------\n" +
                    "<i>Pulsa <b>ENT</b> tras ingresar el código.\nError: penaliza <b>-30s</b>.</i></size>";
            }
        }

        private void InitStyles()
        {
            if (boxStyle != null) return;

            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.12f, 0.11f, 0.09f, 0.96f));
            bgTex.Apply();

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bgTex },
                padding = new RectOffset(25, 25, 20, 20)
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.85f, 0.55f) }
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.90f, 0.35f, 0.35f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            clueFoundStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.95f, 0.45f) }
            };

            cluePendingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.90f, 0.80f, 0.70f) }
            };

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.70f, 0.70f, 0.70f) }
            };

            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        void OnGUI()
        {
            if (!isInspecting) return;

            InitStyles();

            // Fondo semitransparente oscuro en toda la pantalla
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Ventana modal central estilo dossier militar / informe secreto
            float winWidth = Mathf.Min(640f, Screen.width * 0.90f);
            float winHeight = Mathf.Min(560f, Screen.height * 0.90f);
            float x = (Screen.width - winWidth) * 0.5f;
            float y = (Screen.height - winHeight) * 0.5f;

            GUILayout.BeginArea(new Rect(x, y, winWidth, winHeight), boxStyle);

            GUILayout.Label("★ DOSSIER TÁCTICO DE MISIÓN ★", titleStyle);
            GUILayout.Label("PROTOCOLO DE DESACTIVACIÓN C4", headerStyle);
            GUILayout.Space(10);

            GUILayout.Label(
                "La bomba sobre la mesa está armada con un temporizador de 5 minutos.\n" +
                "Para neutralizarla debes explorar el edificio cruzando la puerta abierta a tu izquierda (hacia el pasillo) y conseguir los 4 dígitos secretos:",
                bodyStyle
            );
            GUILayout.Space(12);

            bool c1 = (clue1 != null && clue1.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_DESK_DRAWER");
            bool c2 = (clue2 != null && clue2.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_CORKBOARD");
            bool c3 = (clue3 != null && clue3.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_BOOKSHELF");
            bool c4 = (clue4 != null && clue4.isCollected) || DefusalGameStateManager.CurrentState.collectedClueIds.Contains("CLUE_TOOLBOX");

            GUILayout.Label(c1 ? "  [✓] 1. ESTUDIO (Cajonera): DÍGITO DESCUBIERTO -> [ 4 ] (Posición 1)" 
                               : "  [  ] 1. ESTUDIO: Entra al Estudio y busca la nota en el cajón del escritorio.", c1 ? clueFoundStyle : cluePendingStyle);

            GUILayout.Label(c2 ? "  [✓] 2. ESTUDIO (Pizarra de corcho): DÍGITO DESCUBIERTO -> [ 8 ] (Posición 2)" 
                               : "  [  ] 2. ESTUDIO: Examina la pizarra de corcho en la pared norte.", c2 ? clueFoundStyle : cluePendingStyle);

            GUILayout.Label(c3 ? "  [✓] 3. ARCHIVO (Estantería de libros): DÍGITO DESCUBIERTO -> [ 2 ] (Posición 3)" 
                               : "  [  ] 3. ARCHIVO: Cruza al fondo a la derecha y busca en los libros.", c3 ? clueFoundStyle : cluePendingStyle);

            GUILayout.Label(c4 ? "  [✓] 4. ARCHIVO (Caja de herramientas): DÍGITO DESCUBIERTO -> [ 6 ] (Posición 4)" 
                               : "  [  ] 4. ARCHIVO: Revisa la caja metálica en el suelo del archivo.", c4 ? clueFoundStyle : cluePendingStyle);

            GUILayout.Space(12);
            int count = (c1 ? 1 : 0) + (c2 ? 1 : 0) + (c3 ? 1 : 0) + (c4 ? 1 : 0);
            GUILayout.Label($"<b>Progreso actual de investigación: {count} de 4 pistas reunidas.</b>", bodyStyle);

            GUILayout.Space(6);
            GUILayout.Label("Una vez recopilados los 4 números, regresa a la mesa, pulsa las teclas en el teclado de la bomba y presiona ENT.", hintStyle);
            GUILayout.Label("<i>Nota: Introducir un código incorrecto penaliza 30 segundos del temporizador.</i>", hintStyle);

            GUILayout.Space(14);
            if (GUILayout.Button("ENTENDIDO - CERRAR [E / ESC / CLICK]", closeButtonStyle, GUILayout.Height(40)))
            {
                CloseInspection();
            }

            GUILayout.EndArea();
        }

        void OnMouseDown()
        {
            ToggleInspection();
        }
    }
}
