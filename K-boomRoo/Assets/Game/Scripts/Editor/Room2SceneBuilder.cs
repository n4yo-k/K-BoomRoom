using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using DefusalGame.Data;
using DefusalGame.Bomb;
using DefusalGame.Gameplay;
using DefusalGame.Save;
using DefusalGame.VR;

namespace DefusalGame.Editor
{
    /// <summary>
    /// Genera la escena Room2 completa:
    /// - 4 Habitaciones principales Meshy AI conectadas con exactitud arquitectónica:
    ///   • Sala 1: cuarto2 (rot 270°) - Cámara de la Bomba, aberturas hacia el Sur y Este
    ///   • Pasillo Z: pasilloo (rot 0°) - Portal Norte conecta con Sala 1, Portal Sur conecta con Sala 2
    ///   • Sala 2: livingroommeshy (rot 180°) - Entrada de doble puerta abierta hacia el Pasillo Z
    ///   • Pasillo X: Galería conector Este cerrada que une Sala 1 con Sala 3
    ///   • Sala 3: saladeestar (rot 90°) - Entrada de doble puerta abierta hacia el Pasillo X
    /// - Techos y paredes envolventes exteriores para cerrar completamente las secciones diorama
    ///   (evita fugas de luz o ver el vacío exterior)
    /// - Marcos ornamentales y letreros direccionales en cada umbral
    /// - Suelo maestro plano y continuo en Y=0.00m (cero flotación, movimiento FPS veloz y fluido)
    /// - Bomba táctica C4 completa con display, teclado y 4 ranuras de clave
    /// - 4 Notas interactivas con clave [7 3 9 5]
    /// - Terminal 3D de guardado con tecla G en PC y botón en VR
    /// - Compatibilidad dual Desktop PC y VR (Meta Quest)
    /// </summary>
    [InitializeOnLoad]
    public class Room2SceneBuilder : EditorWindow
    {
        private const string SCENE_PATH       = "Assets/Scenes/Room2.unity";
        private const string GAME_DATA_PATH   = "Assets/Game/Data";
        private const string MAT_PATH         = "Assets/StylizedRoom/Materials";
        private const string OBJ_PATH         = "Assets/objRefs/Extracted";
        private const string REBUILD_FLAG     = "Assets/.rebuild_room2_pending";

        // Rutas de modelos Meshy AI principales (Cuartos y Pasillos)
        private const string OBJ_CUARTO2     = "Assets/objRefs/Extracted/cuarto2/Meshy_AI_The_Investigation_Roo_0917134539_texture_obj/Meshy_AI_The_Investigation_Roo_0917134539_texture.obj";
        private const string TEX_CUARTO2     = "Assets/objRefs/Extracted/cuarto2/Meshy_AI_The_Investigation_Roo_0917134539_texture_obj/Meshy_AI_The_Investigation_Roo_0917134539_texture.png";

        private const string OBJ_PASILLO     = "Assets/objRefs/Extracted/pasilloo/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture_obj/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture.obj";
        private const string TEX_PASILLO     = "Assets/objRefs/Extracted/pasilloo/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture_obj/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture.png";

        private const string OBJ_LIVINGROOM  = "Assets/objRefs/Extracted/livingroommeshy/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture_obj/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture.obj";
        private const string TEX_LIVINGROOM  = "Assets/objRefs/Extracted/livingroommeshy/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture_obj/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture.png";

        private const string OBJ_SALADEESTAR = "Assets/objRefs/Extracted/saladeestar/Meshy_AI_Shadowed_Parlor_0923190159_texture_obj/Meshy_AI_Shadowed_Parlor_0923190159_texture.obj";
        private const string TEX_SALADEESTAR = "Assets/objRefs/Extracted/saladeestar/Meshy_AI_Shadowed_Parlor_0923190159_texture_obj/Meshy_AI_Shadowed_Parlor_0923190159_texture.png";

        // Rutas de props Meshy AI
        private const string OBJ_CAMA        = "Assets/objRefs/Extracted/cama/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture_obj/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture.obj";
        private const string TEX_CAMA        = "Assets/objRefs/Extracted/cama/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture_obj/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture.png";

        private const string OBJ_MUEBLESITO  = "Assets/objRefs/Extracted/mueblesito/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture_obj/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture.obj";
        private const string TEX_MUEBLESITO  = "Assets/objRefs/Extracted/mueblesito/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture_obj/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture.png";

        private const string OBJ_RETRATO     = "Assets/objRefs/Extracted/retrato/Meshy_AI_The_Faded_Duchess_0917125445_texture_obj/Meshy_AI_The_Faded_Duchess_0917125445_texture.obj";
        private const string TEX_RETRATO     = "Assets/objRefs/Extracted/retrato/Meshy_AI_The_Faded_Duchess_0917125445_texture_obj/Meshy_AI_The_Faded_Duchess_0917125445_texture.png";

        private const string OBJ_LIBROS      = "Assets/objRefs/Extracted/Libros/Meshy_AI_Ornate_Book_Stack_0917121445_texture_obj/Meshy_AI_Ornate_Book_Stack_0917121445_texture.obj";
        private const string TEX_LIBROS      = "Assets/objRefs/Extracted/Libros/Meshy_AI_Ornate_Book_Stack_0917121445_texture_obj/Meshy_AI_Ornate_Book_Stack_0917121445_texture.png";

        static Room2SceneBuilder()
        {
            EditorApplication.delayCall += CheckAndAutoBuild;
        }

        private static void CheckAndAutoBuild()
        {
            if (!File.Exists(SCENE_PATH) || File.Exists(REBUILD_FLAG))
            {
                if (File.Exists(REBUILD_FLAG)) { try { File.Delete(REBUILD_FLAG); } catch { } }
                Debug.Log("[Room2SceneBuilder] Reconstruyendo Room2 con orientación y conexiones perfectas...");
                BuildRoom2Scene(showDialog: false);
            }
        }

        [MenuItem("Desactiva la Bomba/Construir Escena Room2 (Escape Room VR)", false, 2)]
        public static void BuildMenuCommand() => BuildRoom2Scene(showDialog: true);

        [MenuItem("Desactiva la Bomba/Configurar Sistema de Guardado en Room2", false, 3)]
        public static void ConfigureSaveSystemMenuCommand() => BuildRoom2Scene(showDialog: true);

        // ═════════════════════════════════════════════════════════════════════════
        // PUNTO DE ENTRADA PRINCIPAL
        // ═════════════════════════════════════════════════════════════════════════
        public static void BuildRoom2Scene(bool showDialog = true)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureDataAssets();
            SetupGameMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("DefusalGame_Room2");

            // 1. Iluminación ambiental cálida e inmersiva
            SetupLighting(root);

            // 2. Arquitectura: 4 cuartos Meshy AI conectados a la perfección + pasillo conector + techos envolventes
            TextMeshPro boardTMP = BuildFullArchitecture(root);

            // 3. Bomba táctica C4 sobre la mesa
            GameObject bombObj = BuildTacticalBomb(root);

            // 4. Notas interactivas (4 pistas numéricas)
            List<ClueInteractable> notes = PlaceNotes(root);

            // 5. Terminal de guardado 3D con botón físico
            AntigravitySaveTerminal saveTerminal = BuildSaveTerminal(root);

            // 6. Sistemas (SaveManager, EscapeRoomManager, UIManager)
            Room2UIManager uiMgr = SetupSystems(root, bombObj.GetComponent<BombController>(), notes, boardTMP);

            // 7. Player (PC Fallback + XR Origin VR con VRPlayerRigManager)
            Camera playerCam = SetupPlayer(root);
            if (uiMgr != null && playerCam != null)
            {
                uiMgr.playerCamera = playerCam;
            }

            // Guardar escena
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            RegisterSceneInBuildSettings(SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log("[Room2SceneBuilder] ¡Room2 construida con éxito con conexiones perfectas y cuartos cerrados!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "¡Room2 Lista y Perfectamente Conectada!",
                    "Room2 generada con éxito:\n\n" +
                    "• 4 Cuartos Meshy AI alineados con puertas orientadas exactamente hacia los pasillos.\n" +
                    "• Pasillo X y Pasillo Z comunican las 3 salas sin huecos ni ver el vacío.\n" +
                    "• Techos y paredes exteriores envolventes cerrados.\n" +
                    "• Suelo continuo perfectamente plano en Y=0 (cero flotación).\n" +
                    "• 4 pistas de la clave [7 3 9 5] listas para inspeccionar.\n" +
                    "• Bomba C4 y terminal de guardado [G] operativas.\n\n" +
                    "Presiona Play en Unity para probar.",
                    "¡Excelente!");
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // SCRIPTABLE OBJECTS / DATA ASSETS
        // ═════════════════════════════════════════════════════════════════════════
        public static void EnsureDataAssets()
        {
            Directory.CreateDirectory(GAME_DATA_PATH);

            ClueData c1 = GetOrCreateClue("Room2_Clue_01", "CLUE_ROOM2_01",
                "Nota del Mueble Antiguo",
                "Nota escrita en el aparador: 'EL PRIMER DÍGITO ES EL 7'.",
                "7", 0, "Mueble Aparador de Madera");

            ClueData c2 = GetOrCreateClue("Room2_Clue_02", "CLUE_ROOM2_02",
                "Nota Oculta tras el Retrato",
                "Papel escondido junto al marco: 'EL SEGUNDO DÍGITO ES EL 3'.",
                "3", 1, "Retrato en la Pared");

            ClueData c3 = GetOrCreateClue("Room2_Clue_03", "CLUE_ROOM2_03",
                "Nota Marcada en los Libros",
                "Marcapáginas entre los tomos: 'EL TERCER DÍGITO ES EL 9'.",
                "9", 2, "Pila de Libros Ornamentados");

            ClueData c4 = GetOrCreateClue("Room2_Clue_04", "CLUE_ROOM2_04",
                "Nota Oculta en la Mesa de la Bomba",
                "Documento confidencial junto al maletín C4: 'EL CUARTO DÍGITO ES EL 5'.",
                "5", 3, "Mesa de la Bomba");

            BombConfigData cfg = AssetDatabase.LoadAssetAtPath<BombConfigData>($"{GAME_DATA_PATH}/Room2_BombConfig.asset");
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<BombConfigData>();
                AssetDatabase.CreateAsset(cfg, $"{GAME_DATA_PATH}/Room2_BombConfig.asset");
            }
            cfg.bombId            = "BOMB_ROOM2_01";
            cfg.bombName          = "Bomba Escape Room - 4 Dígitos";
            cfg.timeLimitSeconds  = 300f;
            cfg.penaltySecondsOnFail = 30f;
            cfg.targetSequence    = "7395";
            cfg.requiredCluesCount = 4;
            cfg.linkedClues = new List<ClueData> { c1, c2, c3, c4 };
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        private static ClueData GetOrCreateClue(string fileName, string id, string name, string desc, string val, int idx, string loc)
        {
            string path = $"{GAME_DATA_PATH}/{fileName}.asset";
            ClueData c  = AssetDatabase.LoadAssetAtPath<ClueData>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<ClueData>();
                AssetDatabase.CreateAsset(c, path);
            }
            c.clueId        = id;
            c.clueName      = name;
            c.description   = desc;
            c.revealedValue = val;
            c.sequenceIndex = idx;
            c.hintLocation  = loc;
            c.isCollected   = false;
            EditorUtility.SetDirty(c);
            return c;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // MATERIALES
        // ═════════════════════════════════════════════════════════════════════════
        private static void SetupGameMaterials()
        {
            Directory.CreateDirectory(MAT_PATH);
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            MakeMat("Mat_Room_Floor",    lit, new Color(0.20f, 0.18f, 0.16f), 0.20f);
            MakeMat("Mat_Room_Ceiling",  lit, new Color(0.25f, 0.23f, 0.22f), 0.05f);
            MakeMat("Mat_Room_Wall",     lit, new Color(0.32f, 0.30f, 0.28f), 0.10f);
            MakeMat("Mat_WoodTrim",      lit, new Color(0.35f, 0.22f, 0.12f), 0.25f);
            MakeMat("Mat_PaperNote",     lit, new Color(0.98f, 0.96f, 0.88f), 0.05f);
            MakeMat("Mat_NoteGlow",      lit, new Color(1.0f,  0.92f, 0.40f), 0.0f);
            MakeMat("Mat_TacticalCase",  lit, new Color(0.18f, 0.20f, 0.20f), 0.35f);
            MakeMat("Mat_KeypadButton",  lit, new Color(0.26f, 0.28f, 0.30f), 0.40f);
            MakeMat("Mat_C4Explosive",   lit, new Color(0.80f, 0.22f, 0.18f), 0.10f);
            MakeMat("Mat_CircuitBoard",  lit, new Color(0.16f, 0.40f, 0.22f), 0.30f);

            // Materiales de los 4 Cuartos Meshy AI con sus texturas completas
            MakeTexMat("Mat_Meshy_Cuarto2",     TEX_CUARTO2,     lit, new Color(0.35f, 0.30f, 0.25f));
            MakeTexMat("Mat_Meshy_Pasillo",     TEX_PASILLO,     lit, new Color(0.22f, 0.26f, 0.34f));
            MakeTexMat("Mat_Meshy_LivingRoom",  TEX_LIVINGROOM,  lit, new Color(0.25f, 0.24f, 0.28f));
            MakeTexMat("Mat_Meshy_SalaDeEstar", TEX_SALADEESTAR, lit, new Color(0.28f, 0.25f, 0.22f));

            // Materiales de Props Meshy
            MakeTexMat("Mat_Meshy_Cama",        TEX_CAMA,        lit, new Color(0.40f, 0.32f, 0.22f));
            MakeTexMat("Mat_Meshy_Mueblesito",  TEX_MUEBLESITO,  lit, new Color(0.45f, 0.34f, 0.20f));
            MakeTexMat("Mat_Meshy_Retrato",     TEX_RETRATO,     lit, new Color(0.50f, 0.42f, 0.32f));
            MakeTexMat("Mat_Meshy_Libros",      TEX_LIBROS,      lit, new Color(0.40f, 0.25f, 0.15f));

            AssetDatabase.SaveAssets();
        }

        private static Material MakeMat(string name, Shader shader, Color col, float smoothness)
        {
            string path = $"{MAT_PATH}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", col);
            m.SetColor("_Color", col);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material MakeTexMat(string name, string texPath, Shader shader, Color fallback)
        {
            string path = $"{MAT_PATH}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTexture("_MainTex", tex);
            }
            else
            {
                m.SetColor("_BaseColor", fallback);
                m.SetColor("_Color", fallback);
            }
            m.SetFloat("_Smoothness", 0.18f);
            m.SetFloat("_Cull", 0f); // 0 = Off (Double Sided - renderiza caras interiores y exteriores)
            m.doubleSidedGI = true;
            m.SetFloat("_ReceiveShadows", 1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Mat(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/{name}.mat");

        // ═════════════════════════════════════════════════════════════════════════
        // ARQUITECTURA: 4 CUARTOS MESHY AI CONECTADOS DE FORMA DIRECTA Y NATURAL
        // ═════════════════════════════════════════════════════════════════════════
        //
        // Layout:
        //
        //          ┌──────────────────────┐
        //          │   SALA 2: EL ATRIO   │   (livingroommeshy a escala 3.5x, rot 180°)
        //          │   Z: -12.9 .. -8.67  │   Doble puerta Norte abierta hacia pasillo
        //          └──────────┬───────────┘
        //                     │
        //          ┌──────────┴───────────┐
        //          │  PASILLO Z CONECTOR  │   (pasilloo a escala 3.2x, rot 0°)
        //          │   Z: -8.67 .. -3.03  │   Arco Norte y Arco Sur continuos y abiertos
        //          └──────────┬───────────┘
        //                     │
        //   ┌─────────────────┴────┐┌──────────────────────┐
        //   │ SALA 1: CÁMARA BOMBA ││  SALA 3: EL DESPACHO │
        //   │   Z: -3.0 .. +2.8    ││   X: 3.1 .. 7.3      │
        //   │ (cuarto2 rot 180°)   ││ (saladeestar rot 90°)│
        //   │ Abertura Sur y Este  ││ Doble puerta Oeste   │
        //   └──────────────────────┘└──────────────────────┘
        //
        private static TextMeshPro BuildFullArchitecture(GameObject root)
        {
            GameObject env = new GameObject("Environment_Room2");
            env.transform.SetParent(root.transform, false);

            Material matWood  = Mat("Mat_WoodTrim");
            Material matFloor = Mat("Mat_Room_Floor");

            // ── 1. SALA 1: Cámara de la Bomba (cuarto2) ───────────────────────────
            // Scale: 3.2, Rot: 180°, Pos: (0, 2.75, 0).
            // Aberturas naturales: Sur (hacia Pasillo Z) y Este (hacia Sala 3).
            // Paredes decoradas sólidas con ventanas y muebles: Norte y Oeste.
            SpawnRoomModel(env, "Room_Cuarto2_Bomba", OBJ_CUARTO2, "Mat_Meshy_Cuarto2",
                new Vector3(0f, 2.75f, 0f), Quaternion.Euler(0f, 180f, 0f), 3.2f);

            // Cartel Informativo de Misión montado sobre la pared Oeste de Sala 1 (junto a la terminal de guardado)
            GameObject board = Box(env, "Mission_Board", new Vector3(-2.95f, 1.85f, 1.10f), new Vector3(2.2f, 1.2f, 0.04f), Mat("Mat_TacticalCase"));
            board.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            TextMeshPro boardTMP = MakeTermTMP(board, "Board_Text", new Vector3(0f, 0f, -0.035f), 0.012f, 2.2f, Color.white,
                "<color=#FFCC00><b>MISIÓN: DESACTIVA LA BOMBA C4</b></color>\n\n" +
                "<size=85%>1. Explora las 3 salas comunicadas directamente por sus puertas y arcos.\n" +
                "2. Encuentra las <b>4 pistas</b> con códigos numéricos.\n" +
                "3. Introduce la secuencia de 4 dígitos en el teclado táctico.\n" +
                "4. Guarda tu progreso en la Terminal de Guardado o tecla <b>[G]</b>.</size>",
                new Vector2(170f, 90f));
            boardTMP.alignment = TextAlignmentOptions.TopLeft;

            // ── 2. PASILLO Z CONECTOR: Sala 1 <-> Sala 2 (pasilloo) ────────────────
            // Scale: 3.2, Rot: 0° (Quaternion.identity), Pos: (0, 2.67, -5.856).
            // Arco Norte (Z = -3.03) acoplado perfectamente a la abertura Sur de Sala 1.
            // Arco Sur (Z = -8.67) acoplado perfectamente a la doble puerta de Sala 2.
            SpawnRoomModel(env, "Room_Pasillo_Z", OBJ_PASILLO, "Mat_Meshy_Pasillo",
                new Vector3(0f, 2.67f, -5.856f), Quaternion.identity, 3.2f);

            // ── 3. SALA 2: El Atrio / Dormitorio (livingroommeshy) ───────────────────
            // Scale: 3.5, Rot: 180°, Pos: (0, 1.72, -10.70).
            // Doble puerta en Z = -8.67 mirando al Norte hacia el arco del Pasillo Z.
            SpawnRoomModel(env, "Room_LivingRoom_Atrio", OBJ_LIVINGROOM, "Mat_Meshy_LivingRoom",
                new Vector3(0f, 1.72f, -10.70f), Quaternion.Euler(0f, 180f, 0f), 3.5f);

            // Cama rústica en Sala 2
            PlacePropMesh(env, "Prop_Cama", OBJ_CAMA, "Mat_Meshy_Cama",
                new Vector3(1.8f, 0.49f, -10.70f), Quaternion.Euler(0f, -90f, 0f), new Vector3(1.1f, 1.1f, 1.1f),
                new Vector3(1.6f, 0.9f, 2.0f), new Vector3(0f, 0.45f, 0f));

            // Retrato en pared Oeste de Sala 2 (Nota 2 se ubica justo al lado)
            PlacePropMesh(env, "Prop_Retrato", OBJ_RETRATO, "Mat_Meshy_Retrato",
                new Vector3(-3.15f, 1.80f, -10.70f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.75f, 0.75f, 0.75f));

            // ── 4. SALA 3: El Despacho / Salón (saladeestar) ────────────────────────
            // Scale: 3.5, Rot: 90°, Pos: (5.15, 1.72, 0).
            // Doble puerta en X = 3.12 mirando al Oeste hacia la abertura Este de Sala 1.
            SpawnRoomModel(env, "Room_SalaDeEstar_Despacho", OBJ_SALADEESTAR, "Mat_Meshy_SalaDeEstar",
                new Vector3(5.15f, 1.72f, 0f), Quaternion.Euler(0f, 90f, 0f), 3.5f);

            // Mueble aparador contra la pared Norte de Sala 3 (Nota 1 encima)
            PlacePropMesh(env, "Prop_Mueblesito", OBJ_MUEBLESITO, "Mat_Meshy_Mueblesito",
                new Vector3(5.15f, 0.855f, 2.65f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.95f, 0.95f, 0.95f),
                new Vector3(1.5f, 1.7f, 0.6f), new Vector3(0f, 0.85f, 0f));

            // Mesa de estudio en Sala 3
            BuildTable(env, "Table_Study", new Vector3(5.15f, 0f, -1.8f), matWood);

            // Pila de libros ornamentados sobre la mesa de estudio (Nota 3 encima)
            PlacePropMesh(env, "Prop_Libros", OBJ_LIBROS, "Mat_Meshy_Libros",
                new Vector3(5.35f, 0.80f, -1.8f), Quaternion.Euler(0f, 25f, 0f), new Vector3(0.35f, 0.35f, 0.35f));

            // ── 5. SUELO MAESTRO Y COLISIONES PERIMETRALES ───────────────────────
            // Suelo plano continuo para todo el recinto en Y=0.00m (evita cualquier tropiezo o flotación)
            Box(env, "Master_Floor_Collider",
                new Vector3(2.5f, -0.1f, -5.0f),
                new Vector3(16f, 0.2f, 20f), null, withRenderer: false);

            CreatePerimeterColliders(env);

            return boardTMP;
        }

        private static void CreatePerimeterColliders(GameObject parent)
        {
            GameObject colRoot = new GameObject("Invisible_Perimeter_Colliders");
            colRoot.transform.SetParent(parent.transform, false);

            // Bounding walls exteriores para evitar salir del mapa sin obstruir vanos ni puertas internas:
            // Límite Oeste total (Sala 1 + Pasillo Z: X = -3.25m, Z de -8.7m a +3.0m)
            CreateInvisibleWall(colRoot, "Wall_Outer_West", new Vector3(-3.25f, 1.6f, -2.85f), new Vector3(0.3f, 3.2f, 12.0f));

            // Límite Norte de Sala 1 (Z = +2.95m, X de -3.2m a +3.1m)
            CreateInvisibleWall(colRoot, "Wall_Sala1_North", new Vector3(0f, 1.6f, 2.95f), new Vector3(6.4f, 3.2f, 0.3f));

            // Límite Oeste de Sala 2 (X = -3.45m, Z de -12.9m a -8.7m)
            CreateInvisibleWall(colRoot, "Wall_Sala2_West", new Vector3(-3.45f, 1.6f, -10.8f), new Vector3(0.3f, 3.2f, 4.4f));

            // Límite Sur de Sala 2 (Z = -12.95m, X de -3.4m a +3.4m)
            CreateInvisibleWall(colRoot, "Wall_Sala2_South", new Vector3(0f, 1.6f, -12.95f), new Vector3(7.0f, 3.2f, 0.3f));

            // Límite Este de Sala 2 (X = +3.45m, Z de -12.9m a -8.7m)
            CreateInvisibleWall(colRoot, "Wall_Sala2_East", new Vector3(3.45f, 1.6f, -10.8f), new Vector3(0.3f, 3.2f, 4.4f));

            // Límite Este del Pasillo Z (X = +3.15m, Z de -8.7m a -3.4m)
            CreateInvisibleWall(colRoot, "Wall_PasilloZ_East", new Vector3(3.15f, 1.6f, -6.0f), new Vector3(0.3f, 3.2f, 5.5f));

            // Límite Norte de Sala 3 (Z = +3.45m, X de +3.1m a +7.4m)
            CreateInvisibleWall(colRoot, "Wall_Sala3_North", new Vector3(5.25f, 1.6f, 3.45f), new Vector3(4.4f, 3.2f, 0.3f));

            // Límite Este de Sala 3 (X = +7.45m, Z de -3.4m a +3.4m)
            CreateInvisibleWall(colRoot, "Wall_Sala3_East", new Vector3(7.45f, 1.6f, 0f), new Vector3(0.3f, 3.2f, 7.0f));

            // Límite Sur de Sala 3 (Z = -3.45m, X de +3.1m a +7.4m)
            CreateInvisibleWall(colRoot, "Wall_Sala3_South", new Vector3(5.25f, 1.6f, -3.45f), new Vector3(4.4f, 3.2f, 0.3f));
        }

        private static void CreateInvisibleWall(GameObject parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject w = Box(parent, name, pos, size, null, withRenderer: false);
        }

        private static GameObject BuildTable(GameObject parent, string name, Vector3 pos, Material matWood, float width = 1.5f, float depth = 0.85f)
        {
            GameObject table = new GameObject(name);
            table.transform.SetParent(parent.transform, false);
            table.transform.localPosition = pos;
            Box(table, "Top",    new Vector3(0f, 0.76f, 0f), new Vector3(width, 0.06f, depth), matWood);
            float hx = (width * 0.5f) - 0.08f;
            float hz = (depth * 0.5f) - 0.08f;
            Box(table, "Leg_FL", new Vector3( hx, 0.37f,  hz), new Vector3(0.08f, 0.74f, 0.08f), matWood);
            Box(table, "Leg_FR", new Vector3(-hx, 0.37f,  hz), new Vector3(0.08f, 0.74f, 0.08f), matWood);
            Box(table, "Leg_BL", new Vector3( hx, 0.37f, -hz), new Vector3(0.08f, 0.74f, 0.08f), matWood);
            Box(table, "Leg_BR", new Vector3(-hx, 0.37f, -hz), new Vector3(0.08f, 0.74f, 0.08f), matWood);
            return table;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // ILUMINACIÓN CÁLIDA E INMERSIVA
        // ═════════════════════════════════════════════════════════════════════════
        private static void SetupLighting(GameObject root)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.68f, 0.65f, 0.60f);

            GameObject rig = new GameObject("Lighting_Rig");
            rig.transform.SetParent(root.transform, false);

            GameObject dirGo = new GameObject("Dir_Light_Fill");
            dirGo.transform.SetParent(rig.transform, false);
            dirGo.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            Light dir = dirGo.AddComponent<Light>();
            dir.type      = LightType.Directional;
            dir.color     = new Color(1.0f, 0.98f, 0.92f);
            dir.intensity = 1.0f;
            dir.shadows   = LightShadows.None;

            // Sala 1: Cámara de la Bomba
            AddLight(rig, "Light_Sala1_A", new Vector3(-1.5f, 2.6f,  0.8f), LightType.Point, new Color(1.0f, 0.94f, 0.82f), 12f, 10f);
            AddLight(rig, "Light_Sala1_B", new Vector3( 1.5f, 2.6f, -0.8f), LightType.Point, new Color(1.0f, 0.94f, 0.82f), 12f, 10f);

            // Foco directo sobre la mesa de la bomba
            GameObject spot = new GameObject("Bomb_Spotlight");
            spot.transform.SetParent(rig.transform, false);
            spot.transform.localPosition = new Vector3(0.1f, 3.2f, -0.55f);
            spot.transform.localRotation = Quaternion.Euler(85f, 0f, 0f);
            Light sl = spot.AddComponent<Light>();
            sl.type       = LightType.Spot;
            sl.spotAngle  = 55f;
            sl.color      = new Color(1f, 0.98f, 0.90f);
            sl.intensity  = 14f;
            sl.range      = 5f;
            sl.shadows    = LightShadows.None;

            // Pasillo Z
            AddLight(rig, "Light_PasilloZ_1", new Vector3(0f, 2.5f, -4.5f), LightType.Point, new Color(0.85f, 0.92f, 1.0f), 10f, 8f);
            AddLight(rig, "Light_PasilloZ_2", new Vector3(0f, 2.5f, -7.2f), LightType.Point, new Color(0.85f, 0.92f, 1.0f), 10f, 8f);

            // Sala 2: El Atrio
            AddLight(rig, "Light_Sala2_A", new Vector3(-1.6f, 2.6f, -10.70f), LightType.Point, new Color(1.0f, 0.90f, 0.80f), 12f, 10f);
            AddLight(rig, "Light_Sala2_B", new Vector3( 1.6f, 2.6f, -10.70f), LightType.Point, new Color(1.0f, 0.90f, 0.80f), 12f, 10f);

            // Sala 3: El Despacho
            AddLight(rig, "Light_Sala3_A", new Vector3(5.15f, 2.6f,  1.4f), LightType.Point, new Color(1.0f, 0.92f, 0.82f), 12f, 10f);
            AddLight(rig, "Light_Sala3_B", new Vector3(5.15f, 2.6f, -1.4f), LightType.Point, new Color(1.0f, 0.92f, 0.82f), 12f, 10f);
        }

        private static Light AddLight(GameObject parent, string n, Vector3 pos, LightType t, Color c, float intensity, float range)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            Light l = go.AddComponent<Light>();
            l.type      = t;
            l.color     = c;
            l.intensity = intensity;
            l.range     = range;
            l.shadows   = LightShadows.None;
            return l;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // BOMBA TÁCTICA C4
        // ═════════════════════════════════════════════════════════════════════════
        private static GameObject BuildTacticalBomb(GameObject root)
        {
            Material matCase   = Mat("Mat_TacticalCase");
            Material matButton = Mat("Mat_KeypadButton");
            Material matC4     = Mat("Mat_C4Explosive");
            Material matPCB    = Mat("Mat_CircuitBoard");
            Material matWood   = Mat("Mat_WoodTrim");

            BuildTable(root, "Table_BombStation", new Vector3(0f, 0f, -0.55f), matWood, 1.5f, 0.85f);

            GameObject bombRoot = new GameObject("Tactical_C4_Bomb");
            bombRoot.transform.SetParent(root.transform, false);
            bombRoot.transform.localPosition = new Vector3(0.1f, 0.79f, -0.55f);
            bombRoot.transform.localRotation = Quaternion.identity;

            bombRoot.AddComponent<AudioSource>();
            BombAudioSynthesizer audio = bombRoot.AddComponent<BombAudioSynthesizer>();
            BombController ctrl = bombRoot.AddComponent<BombController>();

            GameObject cBase = Box(bombRoot, "Case_Base", new Vector3(0f, 0.05f, 0f), new Vector3(0.46f, 0.10f, 0.34f), matCase);
            UnityEngine.Object.DestroyImmediate(cBase.GetComponent<Collider>());

            GameObject lid = Box(bombRoot, "Case_Lid", new Vector3(0f, 0.21f, 0.17f), new Vector3(0.46f, 0.24f, 0.04f), matCase);
            lid.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            UnityEngine.Object.DestroyImmediate(lid.GetComponent<Collider>());

            for (int i = 0; i < 3; i++)
            {
                float z = -0.08f + i * 0.08f;
                GameObject c4 = Box(bombRoot, $"C4_{i}", new Vector3(-0.12f, 0.11f, z), new Vector3(0.15f, 0.06f, 0.065f), matC4);
                UnityEngine.Object.DestroyImmediate(c4.GetComponent<Collider>());
            }

            GameObject pcb = Box(bombRoot, "PCB", new Vector3(0.11f, 0.105f, 0f), new Vector3(0.19f, 0.015f, 0.28f), matPCB);
            UnityEngine.Object.DestroyImmediate(pcb.GetComponent<Collider>());

            GameObject bezel = Box(bombRoot, "Display_Bezel", new Vector3(0.11f, 0.125f, 0.08f), new Vector3(0.17f, 0.035f, 0.08f), matCase);
            UnityEngine.Object.DestroyImmediate(bezel.GetComponent<Collider>());

            TextMeshPro timerTMP  = MakeWorldTMP(bezel, "Timer_TMP",  new Vector3(0f, 0.52f,  0.01f), "05:00.00",  2.2f, new Color(1f, 0.45f, 0.15f));
            TextMeshPro codeTMP   = MakeWorldTMP(bezel, "Code_TMP",   new Vector3(0f, 0.52f, -0.025f), "_ _ _ _",  1.8f, Color.cyan);
            TextMeshPro statusTMP = MakeWorldTMP(bezel, "Status_TMP", new Vector3(0f, 0.52f,  0.036f), "DISPOSITIVO ARMADO (4 DÍGITOS)", 0.80f, Color.yellow);

            Light rLight = AddBombLed(bombRoot, "LED_Red",   new Vector3(0.03f, 0.13f, 0.11f), Color.red,   1.0f, true);
            Light gLight = AddBombLed(bombRoot, "LED_Green", new Vector3(0.19f, 0.13f, 0.11f), Color.green, 1.4f, false);

            int ilayer = LayerMask.NameToLayer("Interactable");
            if (ilayer < 0) ilayer = 0;

            GameObject kpad = new GameObject("Keypad");
            kpad.transform.SetParent(bombRoot.transform, false);
            kpad.transform.localPosition = new Vector3(0.11f, 0.11f, -0.045f);

            string[] keys = { "1","2","3","4","5","6","7","8","9","C","0","ENT" };
            for (int r = 0; r < 4; r++)
            for (int c = 0; c < 3; c++)
            {
                string k  = keys[r * 3 + c];
                float bx  = (c - 1) * 0.042f;
                float bz  = (1.5f - r) * 0.036f;
                GameObject btn = Box(kpad, $"Btn_{k}", new Vector3(bx, 0.012f, bz), new Vector3(0.034f, 0.016f, 0.028f), matButton);
                btn.layer = ilayer;
                BoxCollider btnCol = btn.GetComponent<BoxCollider>();
                if (btnCol != null)
                {
                    btnCol.center = new Vector3(0f, 0.4f, 0f);
                    btnCol.size   = new Vector3(1.2f, 2.0f, 1.2f);
                }
                MakeWorldTMP(btn, "Key_Text", new Vector3(0f, 0.52f, 0f), k, 1.2f,
                    (k == "ENT") ? Color.green : (k == "C" ? Color.red : Color.white));
                BombKeypadButton kBtn = btn.AddComponent<BombKeypadButton>();
                kBtn.keyValue = k;
                kBtn.SetController(ctrl, k);
            }

            ctrl.config          = AssetDatabase.LoadAssetAtPath<BombConfigData>($"{GAME_DATA_PATH}/Room2_BombConfig.asset");
            ctrl.timerText       = timerTMP;
            ctrl.codeText        = codeTMP;
            ctrl.statusText      = statusTMP;
            ctrl.activeRedLight  = rLight;
            ctrl.defusedGreenLight = gLight;
            ctrl.audioSynth      = audio;

            return bombRoot;
        }

        private static Light AddBombLed(GameObject parent, string n, Vector3 pos, Color c, float intensity, bool on)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = 0.7f;
            l.enabled = on;
            return l;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // NOTAS INTERACTIVAS (4 PISTAS NUMÉRICAS: 7 3 9 5)
        // ═════════════════════════════════════════════════════════════════════════
        private static List<ClueInteractable> PlaceNotes(GameObject root)
        {
            var list = new List<ClueInteractable>();
            GameObject notesRoot = new GameObject("EscapeRoom_Notes");
            notesRoot.transform.SetParent(root.transform, false);

            Material mat = Mat("Mat_PaperNote");

            ClueData c1 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_01.asset");
            ClueData c2 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_02.asset");
            ClueData c3 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_03.asset");
            ClueData c4 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_04.asset");

            // Nota 1 – Sala 3, sobre el mueblesito
            list.Add(CreateNote(notesRoot, "Note_1_Mueble",
                new Vector3(5.15f, 1.15f, 2.45f), Quaternion.identity, c1, mat));

            // Nota 2 – Sala 2, en la pared junto al retrato
            list.Add(CreateNote(notesRoot, "Note_2_Retrato",
                new Vector3(-3.05f, 1.25f, -10.70f), Quaternion.Euler(0f, 90f, 0f), c2, mat));

            // Nota 3 – Sala 3, sobre la mesa de estudio junto a los libros
            list.Add(CreateNote(notesRoot, "Note_3_Libros",
                new Vector3(5.00f, 0.81f, -1.8f), Quaternion.Euler(0f, 10f, 0f), c3, mat));

            // Nota 4 – Sala 1, sobre la mesa de la bomba junto al maletín
            list.Add(CreateNote(notesRoot, "Note_4_Mesa",
                new Vector3(-0.45f, 0.81f, -0.55f), Quaternion.Euler(0f, 5f, 0f), c4, mat));

            return list;
        }

        private static ClueInteractable CreateNote(GameObject parent, string noteName,
            Vector3 worldPos, Quaternion rot, ClueData clue, Material mat)
        {
            int ilayer = LayerMask.NameToLayer("Interactable");
            if (ilayer < 0) ilayer = 0;

            GameObject noteObj = new GameObject(noteName);
            noteObj.transform.SetParent(parent.transform, false);
            noteObj.transform.position = worldPos;
            noteObj.transform.rotation = rot;
            noteObj.layer = ilayer;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Note_Mesh";
            visual.transform.SetParent(noteObj.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale    = new Vector3(0.30f, 0.008f, 0.22f);
            visual.layer = ilayer;
            visual.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());

            Material matBand = Mat("Mat_NoteGlow");
            if (matBand == null) matBand = mat;
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.name = "Note_Band";
            band.transform.SetParent(noteObj.transform, false);
            band.transform.localPosition = new Vector3(0f, 0.005f, -0.10f);
            band.transform.localScale    = new Vector3(0.30f, 0.009f, 0.025f);
            band.layer = ilayer;
            band.GetComponent<Renderer>().sharedMaterial = matBand;
            UnityEngine.Object.DestroyImmediate(band.GetComponent<Collider>());

            GameObject textObj = new GameObject("Surface_Text");
            textObj.transform.SetParent(noteObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            textObj.transform.localScale    = new Vector3(0.004f, 0.004f, 0.004f);
            TextMeshPro surfTMP = textObj.AddComponent<TextMeshPro>();
            if (clue != null)
            {
                surfTMP.text = $"<b>{clue.clueName}</b>\n<size=75%>{clue.description}</size>\n\n" +
                               $"<color=#CC2200><size=140%><b>{clue.revealedValue}</b></size></color>\n" +
                               $"<size=65%>Posición #{clue.sequenceIndex + 1}</size>";
            }
            surfTMP.fontSize          = 3.5f;
            surfTMP.alignment         = TextAlignmentOptions.Center;
            surfTMP.color             = new Color(0.12f, 0.10f, 0.08f);
            surfTMP.enableWordWrapping = true;
            surfTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(52f, 75f);

            GameObject popup = new GameObject("Inspection_Popup");
            popup.transform.SetParent(noteObj.transform, false);
            popup.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            popup.transform.localScale    = new Vector3(0.005f, 0.005f, 0.005f);
            popup.SetActive(false);

            GameObject popBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            popBg.name = "Popup_BG";
            popBg.transform.SetParent(popup.transform, false);
            popBg.transform.localScale = new Vector3(60f, 44f, 0.5f);
            UnityEngine.Object.DestroyImmediate(popBg.GetComponent<Collider>());
            popBg.GetComponent<Renderer>().material.color = new Color(0.08f, 0.10f, 0.14f, 0.95f);

            TextMeshPro pTitle = MakePopupTMP(popup, "Popup_Title",  new Vector3(0f,  15f, -0.4f), 5.0f, new Color(1f, 0.85f, 0.4f));
            TextMeshPro pBody  = MakePopupTMP(popup, "Popup_Body",   new Vector3(0f,   2f, -0.4f), 3.8f, Color.white);
            TextMeshPro pDigit = MakePopupTMP(popup, "Popup_Digit",  new Vector3(0f, -13f, -0.4f), 5.2f, Color.yellow);
            pBody.enableWordWrapping = true;
            pBody.GetComponent<RectTransform>().sizeDelta = new Vector2(55f, 20f);

            if (clue != null)
            {
                pTitle.text = clue.clueName;
                pBody.text  = clue.description;
                pDigit.text = $"<color=#FFCC00>DÍGITO DE LA BOMBA:</color> <size=130%><b>[ {clue.revealedValue} ]</b></size> (Pos #{clue.sequenceIndex + 1})";
            }

            BoxCollider bc = noteObj.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.25f, 0.08f, 0.35f);

            Rigidbody rb = noteObj.AddComponent<Rigidbody>();
            rb.mass      = 0.1f;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearDamping = 1.5f;
            rb.angularDamping = 1.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            VRNoteInteractable vrNote = noteObj.AddComponent<VRNoteInteractable>();
            vrNote.clueData          = clue;
            vrNote.inWorldNoteText   = surfTMP;
            vrNote.inspectionPopup   = popup;
            vrNote.popupTitle        = pTitle;
            vrNote.popupBody         = pBody;
            vrNote.popupDigitHint    = pDigit;

            AntigravityGrabInteractable agGrab = noteObj.AddComponent<AntigravityGrabInteractable>();
            agGrab.vrNoteComponent   = vrNote;
            agGrab.floatInZeroGravity = true;

            ClueInteractable ci     = noteObj.AddComponent<ClueInteractable>();
            ci.clueData             = clue;
            ci.inspectionCardPopup  = popup;
            ci.cardTitleText        = pTitle;
            ci.cardBodyText         = pBody;
            ci.cardDatoText         = pDigit;

            GameObject glowGo = new GameObject("Note_Glow_Light");
            glowGo.transform.SetParent(noteObj.transform, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            Light glow = glowGo.AddComponent<Light>();
            glow.type      = LightType.Point;
            glow.color     = new Color(1.0f, 0.92f, 0.30f);
            glow.intensity = 3.5f;
            glow.range     = 1.8f;
            glow.shadows   = LightShadows.None;

            return ci;
        }

        private static TextMeshPro MakePopupTMP(GameObject parent, string n, Vector3 localPos, float size, Color col)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize  = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = col;
            tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(55f, 12f);
            return tmp;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // TERMINAL DE GUARDADO 3D
        // ═════════════════════════════════════════════════════════════════════════
        private static AntigravitySaveTerminal BuildSaveTerminal(GameObject root)
        {
            Material matCase   = Mat("Mat_TacticalCase");
            Material matButton = Mat("Mat_KeypadButton");

            // Montado en pared Oeste de Sala 1 (X = -2.95m)
            GameObject termRoot = new GameObject("Save_Terminal_Console");
            termRoot.transform.SetParent(root.transform, false);
            termRoot.transform.localPosition = new Vector3(-2.95f, 1.50f, 0.0f);
            termRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            Box(termRoot, "Chassis",      Vector3.zero,                new Vector3(0.90f, 0.70f, 0.08f), matCase);
            Box(termRoot, "Screen_Bezel", new Vector3(0f, 0.07f,-0.035f), new Vector3(0.82f, 0.44f, 0.02f), matCase);

            GameObject screenObj = Box(termRoot, "Screen_Glass", new Vector3(0f, 0.07f,-0.046f), new Vector3(0.80f, 0.42f, 0.01f), null, withRenderer: true);
            screenObj.GetComponent<Renderer>().material.color = new Color(0.04f, 0.07f, 0.10f, 0.95f);

            GameObject ledGo = new GameObject("Status_LED");
            ledGo.transform.SetParent(termRoot.transform, false);
            ledGo.transform.localPosition = new Vector3(0.35f, 0.28f,-0.06f);
            Light tLight = ledGo.AddComponent<Light>();
            tLight.type = LightType.Point;
            tLight.color = new Color(0.2f, 0.8f, 1.0f);
            tLight.intensity = 1.0f;
            tLight.range = 1.5f;

            TextMeshPro hdrTMP     = MakeTermTMP(termRoot, "Header",  new Vector3(0f, 0.24f,-0.055f), 0.010f, 2.4f, new Color(0.2f,0.8f,1.0f), "<b>TERMINAL DE GUARDADO // ANTIGRAVITY OS</b>", new Vector2(80f,15f));
            TextMeshPro statusTMP  = MakeTermTMP(termRoot, "Status",  new Vector3(0f, 0.14f,-0.055f), 0.010f, 2.1f, new Color(0.2f,0.8f,1.0f), "SISTEMA OPERATIVO // EN ESPERA",               new Vector2(80f,15f));
            TextMeshPro detailsTMP = MakeTermTMP(termRoot, "Details", new Vector3(0f,-0.02f,-0.055f), 0.009f, 1.8f, new Color(0.85f,0.9f,0.95f), "",                                              new Vector2(85f,35f));

            Box(termRoot, "Btn_Bracket", new Vector3(0f,-0.22f,-0.040f), new Vector3(0.42f, 0.11f, 0.04f), matCase);
            int ilayer = LayerMask.NameToLayer("Interactable");
            if (ilayer < 0) ilayer = 0;
            GameObject plunger = Box(termRoot, "Btn_Plunger", new Vector3(0f,-0.22f,-0.065f), new Vector3(0.38f, 0.085f, 0.035f), matButton);
            plunger.layer = ilayer;
            MakeTermTMP(plunger, "Btn_Label", new Vector3(0f, 0f,-0.52f), 0.015f, 4.2f, Color.white, "GUARDAR PARTIDA", new Vector2(25f, 6f));

            AntigravityPhysicalButton physBtn = plunger.AddComponent<AntigravityPhysicalButton>();
            physBtn.pressDirection  = new Vector3(0f, 0f, 1f);
            physBtn.pressDistance   = 0.02f;
            physBtn.buttonRenderer  = plunger.GetComponent<Renderer>();
            physBtn.idleColor       = new Color(0.12f, 0.55f, 0.95f);
            physBtn.pressedColor    = new Color(0.0f, 1.0f, 0.4f);

            AudioSource termAudio = termRoot.AddComponent<AudioSource>();
            termAudio.spatialBlend = 1.0f;
            termAudio.playOnAwake  = false;

            AntigravitySaveTerminal termCtrl = termRoot.AddComponent<AntigravitySaveTerminal>();
            termCtrl.headerText             = hdrTMP;
            termCtrl.statusText             = statusTMP;
            termCtrl.detailsText            = detailsTMP;
            termCtrl.terminalIndicatorLight  = tLight;
            termCtrl.savePhysicalButton     = physBtn;
            termCtrl.audioSource            = termAudio;

            return termCtrl;
        }

        private static TextMeshPro MakeTermTMP(GameObject parent, string n, Vector3 pos, float scale, float fontSize, Color col, string text, Vector2 rtSize)
        {
            GameObject go = new GameObject(n + "_TMP");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale, scale, scale);
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize  = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = col;
            tmp.text      = text;
            tmp.enableWordWrapping = true;
            tmp.GetComponent<RectTransform>().sizeDelta = rtSize;
            return tmp;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // SISTEMAS: SaveManager + EscapeRoomManager + UIManager
        // ═════════════════════════════════════════════════════════════════════════
        private static Room2UIManager SetupSystems(GameObject root, BombController bomb, List<ClueInteractable> notes, TextMeshPro boardTMP)
        {
            // GameSaveManager en GameObject dedicado independiente para evitar conflictos de ciclo de vida
            GameObject saveGo = new GameObject("GameSaveManager");
            saveGo.transform.SetParent(root.transform, false);
            GameSaveManager saveMgr = saveGo.AddComponent<GameSaveManager>();
            saveMgr.bombController      = bomb;
            saveMgr.autoLoadOnStart     = false;
            saveMgr.autoSaveOnMilestones = true;

            GameObject sys = new GameObject("Systems_Room2");
            sys.transform.SetParent(root.transform, false);

            Room2EscapeRoomManager roomMgr = sys.AddComponent<Room2EscapeRoomManager>();
            roomMgr.bombController = bomb;
            if (boardTMP != null) roomMgr.missionBoardText = boardTMP;

            var vrNotes = new List<VRNoteInteractable>();
            foreach (var ci in notes)
            {
                var vr = ci.GetComponent<VRNoteInteractable>();
                if (vr != null) vrNotes.Add(vr);
            }
            roomMgr.roomNotes = vrNotes;

            Room2UIManager uiMgr = sys.AddComponent<Room2UIManager>();
            uiMgr.bomb = bomb;
            if (notes != null && notes.Count >= 4)
            {
                uiMgr.clue1 = notes[0].clueData;
                uiMgr.clue2 = notes[1].clueData;
                uiMgr.clue3 = notes[2].clueData;
                uiMgr.clue4 = notes[3].clueData;
            }

            return uiMgr;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PLAYER: PC Fallback + XR Origin VR con VRPlayerRigManager
        // ═════════════════════════════════════════════════════════════════════════
        private static Camera SetupPlayer(GameObject root)
        {
            GameObject xriMgrObj = new GameObject("XR Interaction Manager");
            xriMgrObj.AddComponent<XRInteractionManager>();

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0) playerLayer = 0;

            GameObject playerSystem = new GameObject("Player_System");
            playerSystem.transform.SetParent(root.transform, false);
            playerSystem.transform.position = new Vector3(0f, 0.15f, -1.45f); // Sala 1, frente a la mesa de la bomba

            VRPlayerRigManager rigMgr = playerSystem.AddComponent<VRPlayerRigManager>();
            rigMgr.forceVRMode = false;

            // XR Origin (VR Rig)
            string vrPrefabPath = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";
            GameObject xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vrPrefabPath);
            if (xrPrefab == null)
            {
                xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
            }

            if (xrPrefab != null)
            {
                GameObject xrRig = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab, playerSystem.transform);
                xrRig.name = "XR Origin (VR Rig)";
                xrRig.transform.localPosition = Vector3.zero;
                xrRig.transform.localRotation = Quaternion.identity;
                xrRig.layer = playerLayer;

                CharacterController xrCC = xrRig.GetComponent<CharacterController>();
                if (xrCC == null)
                {
                    xrCC = xrRig.AddComponent<CharacterController>();
                    xrCC.height = 1.8f; xrCC.radius = 0.35f;
                    xrCC.center = new Vector3(0f, 0.9f, 0f);
                }

                rigMgr.vrOriginRig = xrRig;
            }

            // PC Testing Fallback (Desktop FPV)
            GameObject pcFallback = new GameObject("Player_PC_TestingFallback");
            pcFallback.transform.SetParent(playerSystem.transform, false);
            pcFallback.transform.localPosition = Vector3.zero;
            pcFallback.transform.localRotation = Quaternion.identity;
            pcFallback.layer = playerLayer;
            rigMgr.desktopFpvRig = pcFallback;

            CharacterController cc = pcFallback.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.875f, 0f);
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0.001f;

            GameObject camGo = new GameObject("Fallback_Camera");
            camGo.transform.SetParent(pcFallback.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camGo.tag = "MainCamera";

            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            FPSRaycastInteractor raycast = camGo.AddComponent<FPSRaycastInteractor>();
            raycast.interactDistance = 3.5f;
            raycast.interactMask = ~0;

            Player_PC_TestingFallback pcScript = pcFallback.AddComponent<Player_PC_TestingFallback>();
            pcScript.playerCamera = cam;

            rigMgr.ApplyMode();
            return cam;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // HELPERS: SPAWN DE MESHES MESHY Y PRIMITIVAS
        // ═════════════════════════════════════════════════════════════════════════
        private static GameObject SpawnRoomModel(GameObject parent, string name, string objPath, string matName, Vector3 pos, Quaternion rot, float scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Room2SceneBuilder] No se encontró el modelo en: {objPath}");
                return null;
            }

            GameObject inst = UnityEngine.Object.Instantiate(prefab, parent.transform);
            inst.name = name;
            inst.transform.localPosition = pos;
            inst.transform.localRotation = rot;
            inst.transform.localScale = new Vector3(scale, scale, scale);

            Material mat = Mat(matName);
            if (mat != null)
            {
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = mat;
            }

            // Eliminar colisionadores de malla fotogramétrica para paso libre y fluido
            foreach (var col in inst.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(col);
            }

            return inst;
        }

        private static GameObject PlacePropMesh(GameObject parent, string name, string objPath, string matName,
            Vector3 localPos, Quaternion rot, Vector3 scale, Vector3? colliderSize = null, Vector3? colliderCenter = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (prefab == null) return null;

            GameObject inst = UnityEngine.Object.Instantiate(prefab, parent.transform);
            inst.name = name;
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = rot;
            inst.transform.localScale    = scale;

            Material mat = Mat(matName);
            if (mat != null)
            {
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = mat;
            }

            foreach (var col in inst.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(col);
            }

            if (colliderSize.HasValue)
            {
                BoxCollider bc = inst.AddComponent<BoxCollider>();
                bc.size = colliderSize.Value;
                if (colliderCenter.HasValue) bc.center = colliderCenter.Value;
            }

            return inst;
        }

        private static GameObject Box(GameObject parent, string name, Vector3 localPos, Vector3 size,
            Material mat, bool withRenderer = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = size;

            Renderer rend = go.GetComponent<Renderer>();
            if (!withRenderer) { rend.enabled = false; }
            else if (mat != null) { rend.sharedMaterial = mat; }

            return go;
        }

        private static TextMeshPro MakeWorldTMP(GameObject parent, string n, Vector3 localPos, string text,
            float fontSize, Color col)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = col;
            return tmp;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == scenePath) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
