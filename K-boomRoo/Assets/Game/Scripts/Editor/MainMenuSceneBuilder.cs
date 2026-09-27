using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DefusalGame.UI;
using DefusalGame.Save;

namespace DefusalGame.Editor
{
    [InitializeOnLoad]
    public class MainMenuSceneBuilder : EditorWindow
    {
        private const string SCENE_DIR = "Assets/Scenes";
        private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/MainMenu.unity";
        private const string MENU_IMG_PATH = "Assets/objRefs/MenuImg.jpg";
        private const string LEVELS_IMG_PATH = "Assets/objRefs/LevelsImg.jpg";

        static MainMenuSceneBuilder()
        {
            EditorApplication.delayCall += EnsureMainMenuExists;
        }

        public static void EnsureMainMenuExists()
        {
            if (!File.Exists(MAIN_MENU_SCENE_PATH))
            {
                BuildMainMenuScene();
            }
            else
            {
                ConfigureBuildSettings();
            }
        }

        [MenuItem("K-Boom/Crear Escena MainMenu y Configurar Build Settings", false, 0)]
        public static void BuildMainMenuScene()
        {
            Directory.CreateDirectory(SCENE_DIR);

            // 1. Crear nueva escena vacía
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Crear Cámara Principal 2D/3D
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.10f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10f);
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            // 3. Crear Gestor de Guardado
            GameObject saveManagerObj = new GameObject("GameSaveManager");
            GameSaveManager saveManager = saveManagerObj.AddComponent<GameSaveManager>();

            // 4. Crear Gestor del Menú Principal
            GameObject menuManagerObj = new GameObject("MainMenuManager");
            MainMenuManager menuManager = menuManagerObj.AddComponent<MainMenuManager>();

            // Asignar texturas de objRefs
            Texture2D menuTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MENU_IMG_PATH);
            Texture2D levelsTex = AssetDatabase.LoadAssetAtPath<Texture2D>(LEVELS_IMG_PATH);
            Sprite menuSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MENU_IMG_PATH);
            Sprite levelsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LEVELS_IMG_PATH);

            menuManager.menuImageTexture = menuTex;
            menuManager.levelsImageTexture = levelsTex;

            // 5. Crear EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

            // 6. Crear Canvas UI
            GameObject canvasObj = new GameObject("Canvas_MainMenu");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            menuManager.menuCanvas = canvas;

            // --- PANEL MENÚ PRINCIPAL ---
            GameObject mainMenuPanel = new GameObject("MainMenuPanel");
            mainMenuPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform mainRect = mainMenuPanel.AddComponent<RectTransform>();
            SetFullStretch(mainRect);
            menuManager.mainMenuPanel = mainMenuPanel;

            // Fondo MenuImg
            GameObject menuBgObj = new GameObject("Background_MenuImg");
            menuBgObj.transform.SetParent(mainMenuPanel.transform, false);
            RectTransform menuBgRect = menuBgObj.AddComponent<RectTransform>();
            SetFullStretch(menuBgRect);
            RawImage menuRawImg = menuBgObj.AddComponent<RawImage>();
            menuRawImg.texture = menuTex;
            menuRawImg.color = Color.white;
            menuManager.menuBackgroundRawImage = menuRawImg;

            // Overlay oscurecedor suave
            GameObject menuOverlay = new GameObject("DarkOverlay");
            menuOverlay.transform.SetParent(mainMenuPanel.transform, false);
            RectTransform overlayRect = menuOverlay.AddComponent<RectTransform>();
            SetFullStretch(overlayRect);
            Image overlayImg = menuOverlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.35f);

            // Título
            GameObject titleObj = CreateTMPText(mainMenuPanel.transform, "TitleText", "💣 K-BOOM ROOM 💣", 54, TextAlignmentOptions.Center, new Vector2(0.5f, 0.85f), new Vector2(900, 80));
            titleObj.GetComponent<TextMeshProUGUI>().color = new Color(1.0f, 0.85f, 0.2f);

            GameObject subObj = CreateTMPText(mainMenuPanel.transform, "SubtitleText", "ESCAPE ROOM & DESACTIVA LA BOMBA", 22, TextAlignmentOptions.Center, new Vector2(0.5f, 0.76f), new Vector2(900, 50));
            subObj.GetComponent<TextMeshProUGUI>().color = new Color(0.3f, 0.85f, 1.0f);

            // Botones Menú Principal
            GameObject btnPlay = CreateUIButton(mainMenuPanel.transform, "Btn_Jugar", "▶  JUGAR  (NUEVA PARTIDA)", new Vector2(0.5f, 0.52f), new Vector2(380, 58), new Color(0.10f, 0.45f, 0.25f));
            menuManager.playButton = btnPlay.GetComponent<Button>();

            GameObject btnContinue = CreateUIButton(mainMenuPanel.transform, "Btn_Continuar", "💾  CONTINUAR PARTIDA", new Vector2(0.5f, 0.41f), new Vector2(380, 58), new Color(0.15f, 0.35f, 0.55f));
            menuManager.continueButton = btnContinue.GetComponent<Button>();

            GameObject btnLevels = CreateUIButton(mainMenuPanel.transform, "Btn_Niveles", "📑  SELECCIÓN DE NIVELES", new Vector2(0.5f, 0.30f), new Vector2(380, 58), new Color(0.20f, 0.25f, 0.32f));
            menuManager.levelsButton = btnLevels.GetComponent<Button>();

            GameObject btnQuit = CreateUIButton(mainMenuPanel.transform, "Btn_Salir", "✕  SALIR DEL JUEGO", new Vector2(0.5f, 0.19f), new Vector2(380, 58), new Color(0.40f, 0.15f, 0.15f));
            menuManager.quitButton = btnQuit.GetComponent<Button>();

            // Pie de página
            GameObject footerObj = CreateTMPText(mainMenuPanel.transform, "FooterHint", "Presiona [G] durante cualquier nivel para guardar tu posición y notas.", 15, TextAlignmentOptions.Center, new Vector2(0.5f, 0.06f), new Vector2(800, 40));
            footerObj.GetComponent<TextMeshProUGUI>().color = new Color(0.8f, 0.85f, 0.9f);

            // --- PANEL SELECCIÓN DE NIVELES ---
            GameObject levelsPanel = new GameObject("LevelsPanel");
            levelsPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform levelsRect = levelsPanel.AddComponent<RectTransform>();
            SetFullStretch(levelsRect);
            menuManager.levelsPanel = levelsPanel;
            levelsPanel.SetActive(false);

            // Fondo LevelsImg
            GameObject levelsBgObj = new GameObject("Background_LevelsImg");
            levelsBgObj.transform.SetParent(levelsPanel.transform, false);
            RectTransform levelsBgRect = levelsBgObj.AddComponent<RectTransform>();
            SetFullStretch(levelsBgRect);
            RawImage levelsRawImg = levelsBgObj.AddComponent<RawImage>();
            levelsRawImg.texture = levelsTex;
            levelsRawImg.color = Color.white;
            menuManager.levelsBackgroundRawImage = levelsRawImg;

            // Overlay niveles
            GameObject levelsOverlay = new GameObject("DarkOverlay_Levels");
            levelsOverlay.transform.SetParent(levelsPanel.transform, false);
            RectTransform lvlOverlayRect = levelsOverlay.AddComponent<RectTransform>();
            SetFullStretch(lvlOverlayRect);
            Image lvlOverlayImg = levelsOverlay.AddComponent<Image>();
            lvlOverlayImg.color = new Color(0f, 0f, 0f, 0.40f);

            // Título Niveles
            GameObject lvlTitleObj = CreateTMPText(levelsPanel.transform, "LevelsTitle", "📑 SELECCIÓN DE NIVELES 📑", 48, TextAlignmentOptions.Center, new Vector2(0.5f, 0.88f), new Vector2(900, 70));
            lvlTitleObj.GetComponent<TextMeshProUGUI>().color = new Color(1.0f, 0.85f, 0.2f);

            GameObject lvlSubObj = CreateTMPText(levelsPanel.transform, "LevelsSubtitle", "Selecciona una habitación para comenzar tu misión", 20, TextAlignmentOptions.Center, new Vector2(0.5f, 0.80f), new Vector2(900, 40));
            lvlSubObj.GetComponent<TextMeshProUGUI>().color = new Color(0.3f, 0.85f, 1.0f);

            // Tarjeta Nivel 1: SampleScene
            GameObject btnLvl1 = CreateLevelCard(levelsPanel.transform, "Card_Lvl1", "1. EL DESPACHO", "SampleScene", "Investiga el Estudio y el Archivo.\nLocaliza las 4 evidencias para neutralizar el C4.", new Vector2(0.23f, 0.48f), new Vector2(280, 320), new Color(0.08f, 0.22f, 0.35f));
            menuManager.level1Button = btnLvl1.GetComponentInChildren<Button>();

            // Tarjeta Nivel 2: Room2
            GameObject btnLvl2 = CreateLevelCard(levelsPanel.transform, "Card_Lvl2", "2. ESCAPE ROOM C4", "Room2", "Habitación cerrada con notas VR,\npistas de luz UV y terminal de desactivación.", new Vector2(0.50f, 0.48f), new Vector2(280, 320), new Color(0.12f, 0.28f, 0.20f));
            menuManager.level2Button = btnLvl2.GetComponentInChildren<Button>();

            // Tarjeta Nivel 3: Level3_House
            GameObject btnLvl3 = CreateLevelCard(levelsPanel.transform, "Card_Lvl3", "3. LA CASA TÁCTICA", "Level3_House", "Bomba de etapas múltiples:\nSobretensión, corte de cables y clave final.", new Vector2(0.77f, 0.48f), new Vector2(280, 320), new Color(0.35f, 0.15f, 0.15f));
            menuManager.level3Button = btnLvl3.GetComponentInChildren<Button>();

            // Botón Volver al Menú
            GameObject btnBack = CreateUIButton(levelsPanel.transform, "Btn_VolverMenu", "⬅  VOLVER AL MENÚ PRINCIPAL", new Vector2(0.5f, 0.12f), new Vector2(360, 52), new Color(0.15f, 0.20f, 0.28f));
            menuManager.backToMenuButton = btnBack.GetComponent<Button>();

            // 7. Guardar Escena
            EditorSceneManager.SaveScene(scene, MAIN_MENU_SCENE_PATH);
            AssetDatabase.SaveAssets();

            // 8. Configurar EditorBuildSettings
            ConfigureBuildSettings();

            Debug.Log($"[MainMenuSceneBuilder] ¡Escena {MAIN_MENU_SCENE_PATH} generada exitosamente y registrada como primera escena en Build Settings!");
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MAIN_MENU_SCENE_PATH, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Room2.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Level3_House.unity", true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[MainMenuSceneBuilder] Build Settings actualizados con 4 escenas en orden correcto.");
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateTMPText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchor, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.fontStyle = FontStyles.Bold;
            return obj;
        }

        private static GameObject CreateUIButton(Transform parent, string name, string text, Vector2 anchor, Vector2 size, Color bgColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = obj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.3f;
            colors.pressedColor = Color.yellow;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            SetFullStretch(textRt);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return obj;
        }

        private static GameObject CreateLevelCard(Transform parent, string name, string title, string sceneName, string description, Vector2 anchor, Vector2 size, Color cardColor)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);
            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            Image img = cardObj.AddComponent<Image>();
            img.color = new Color(cardColor.r, cardColor.g, cardColor.b, 0.90f);

            // Título
            CreateTMPText(cardObj.transform, "Title", title, 20, TextAlignmentOptions.Center, new Vector2(0.5f, 0.86f), new Vector2(size.x - 20, 36));

            // Nombre Escena
            CreateTMPText(cardObj.transform, "Scene", $"Escena: {sceneName}", 13, TextAlignmentOptions.Center, new Vector2(0.5f, 0.74f), new Vector2(size.x - 20, 26));

            // Descripción
            GameObject descObj = CreateTMPText(cardObj.transform, "Desc", description, 14, TextAlignmentOptions.Center, new Vector2(0.5f, 0.46f), new Vector2(size.x - 30, 90));
            descObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;

            // Botón Jugar
            GameObject btnObj = CreateUIButton(cardObj.transform, "Btn_PlayLevel", "▶ JUGAR NIVEL", new Vector2(0.5f, 0.16f), new Vector2(size.x - 40, 46), new Color(0.12f, 0.55f, 0.28f));

            return btnObj;
        }
    }
}
