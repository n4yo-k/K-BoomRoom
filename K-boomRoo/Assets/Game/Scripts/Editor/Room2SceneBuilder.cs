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

namespace DefusalGame.Editor
{
    /// <summary>
    /// Genera la escena Room2 completa con:
    /// - 3 cuartos cerrados + pasillo conector (paredes/suelo/techo sólidos con primitivas)
    /// - Meshes Meshy AI colocados DENTRO de cada cuarto
    /// - 4 notas interactivas con ClueInteractable + FPSRaycastInteractor en PC
    /// - HUD de bomba replicado (4 ranuras, verde+✓ al recoger)
    /// - Pop-up de guardado (tecla G / botón 3D VR)
    /// - Sistema de guardado JSON
    /// </summary>
    [InitializeOnLoad]
    public class Room2SceneBuilder : EditorWindow
    {
        private const string SCENE_PATH       = "Assets/Scenes/Room2.unity";
        private const string GAME_DATA_PATH   = "Assets/Game/Data";
        private const string MAT_PATH         = "Assets/StylizedRoom/Materials";
        private const string OBJ_PATH         = "Assets/objRefs/Extracted";
        private const string REBUILD_FLAG     = "Assets/.rebuild_room2_pending";

        // ─── Dimensiones de cada sala ─────────────────────────────────────────────
        // Sala 1: Cámara de la Bomba   (centro world = 0, 0, 0)         12×4×10
        // Pasillo Conector             (centro world = 0, 0, -8)         4×4×6
        // Sala 2: El Atrio             (centro world = 0, 0, -14)       10×4×10
        // Sala 3: El Despacho          (centro world = 10, 0, -7)       10×4×10
        // (el pasillo lateral une Sala1 con Sala3 en X)

        static Room2SceneBuilder()
        {
            EditorApplication.delayCall += CheckAndAutoBuild;
        }

        private static void CheckAndAutoBuild()
        {
            if (!File.Exists(SCENE_PATH) || File.Exists(REBUILD_FLAG))
            {
                if (File.Exists(REBUILD_FLAG)) { try { File.Delete(REBUILD_FLAG); } catch { } }
                Debug.Log("[Room2SceneBuilder] Reconstruyendo Room2...");
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

            // 1. Iluminación ambiental
            SetupLighting(root);

            // 2. Arquitectura: suelos/paredes/techos sólidos + meshes dentro
            BuildFullArchitecture(root);

            // 3. Bomba táctica sobre la mesa
            GameObject bombObj = BuildTacticalBomb(root);

            // 4. Notas interactivas (4)
            List<ClueInteractable> notes = PlaceNotes(root);

            // 5. Terminal de guardado 3D
            AntigravitySaveTerminal saveTerminal = BuildSaveTerminal(root);

            // 6. Sistemas (Save, EscapeRoomManager, UIManager)
            SetupSystems(root, bombObj.GetComponent<BombController>(), notes);

            // 7. Player (PC fallback + XR Origin)
            SetupPlayer(root);

            // Guardar
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            RegisterSceneInBuildSettings(SCENE_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();

            Debug.Log("[Room2SceneBuilder] ¡Room2 construida y guardada en " + SCENE_PATH + "!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "¡Room2 Lista!",
                    "Room2 generada con:\n\n" +
                    "• 3 cuartos cerrados + pasillo conector.\n" +
                    "• 4 notas con [E / Click] Inspeccionar.\n" +
                    "• HUD de bomba (4 ranuras → verde ✓).\n" +
                    "• Pop-up guardado con tecla G.\n" +
                    "• Sistema JSON de guardado.\n\n" +
                    "Presiona Play y prueba con WASD + E + G.",
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
                "Nota Oculta en el Salón",
                "Documento secreto junto a los sillones: 'EL CUARTO DÍGITO ES EL 5'.",
                "5", 3, "Mesa de la Sala de Estar");

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

            // Paredes / suelos: colores más claros para evitar oscuridad excesiva
            MakeMat("Mat_Room_Floor",   lit, new Color(0.45f, 0.38f, 0.30f), 0.05f);
            MakeMat("Mat_Room_Ceiling", lit, new Color(0.72f, 0.70f, 0.65f), 0.0f);
            MakeMat("Mat_Room_Wall",    lit, new Color(0.62f, 0.55f, 0.48f), 0.05f);
            MakeMat("Mat_Room_Wall2",   lit, new Color(0.55f, 0.58f, 0.62f), 0.05f);
            MakeMat("Mat_WoodTrim",     lit, new Color(0.55f, 0.38f, 0.22f), 0.2f);
            MakeMat("Mat_PaperNote",    lit, new Color(0.98f, 0.96f, 0.88f), 0.0f);
            MakeMat("Mat_NoteGlow",     lit, new Color(1.0f,  0.92f, 0.40f), 0.0f);   // amarillo para indicadores
            MakeMat("Mat_TacticalCase",  lit, new Color(0.20f, 0.22f, 0.22f), 0.3f);
            MakeMat("Mat_KeypadButton",  lit, new Color(0.28f, 0.32f, 0.32f), 0.4f);
            MakeMat("Mat_C4Explosive",   lit, new Color(0.80f, 0.22f, 0.18f), 0.1f);
            MakeMat("Mat_CircuitBoard",  lit, new Color(0.18f, 0.42f, 0.25f), 0.3f);

            // Meshy AI con texturas (si existen)
            MakeTexMat("Mat_Meshy_Cuarto2",    $"{OBJ_PATH}/cuarto2/Meshy_AI_The_Investigation_Roo_0917134539_texture_obj/Meshy_AI_The_Investigation_Roo_0917134539_texture.png", lit, new Color(0.35f, 0.30f, 0.25f));
            MakeTexMat("Mat_Meshy_Cama",       $"{OBJ_PATH}/cama/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture_obj/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture.png",   lit, new Color(0.40f, 0.32f, 0.22f));
            MakeTexMat("Mat_Meshy_Mueblesito", $"{OBJ_PATH}/mueblesito/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture_obj/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture.png", lit, new Color(0.45f, 0.34f, 0.20f));
            MakeTexMat("Mat_Meshy_Retrato",    $"{OBJ_PATH}/retrato/Meshy_AI_The_Faded_Duchess_0917125445_texture_obj/Meshy_AI_The_Faded_Duchess_0917125445_texture.png",    lit, new Color(0.50f, 0.42f, 0.32f));
            MakeTexMat("Mat_Meshy_Libros",     $"{OBJ_PATH}/Libros/Meshy_AI_Ornate_Book_Stack_0917121445_texture_obj/Meshy_AI_Ornate_Book_Stack_0917121445_texture.png",     lit, new Color(0.40f, 0.25f, 0.15f));
            MakeTexMat("Mat_Meshy_LivingRoom", $"{OBJ_PATH}/livingroommeshy/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture_obj/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture.png", lit, new Color(0.20f, 0.22f, 0.28f));
            MakeTexMat("Mat_Meshy_Pasillo",    $"{OBJ_PATH}/pasilloo/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture_obj/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture.png",  lit, new Color(0.22f, 0.26f, 0.34f));
            MakeTexMat("Mat_Meshy_SalaDeEstar",$"{OBJ_PATH}/saladeestar/Meshy_AI_Shadowed_Parlor_0923190159_texture_obj/Meshy_AI_Shadowed_Parlor_0923190159_texture.png",   lit, new Color(0.18f, 0.18f, 0.22f));

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
            m.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Mat(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/{name}.mat");

        // ═════════════════════════════════════════════════════════════════════════
        // ARQUITECTURA COMPLETA: 3 CUARTOS CERRADOS + PASILLOS
        // ═════════════════════════════════════════════════════════════════════════
        //
        // Layout (vista desde arriba) — escala humana (~3m altura, salas ~8×6m):
        //
        //   ┌──────────────┐
        //   │  SALA 2      │   Z: -8..-14
        //   │  El Atrio    │
        //   └──────┬───────┘
        //          │ Pasillo Z (4×3)
        //   ┌──────┴───────┐      ┌─────────────┐
        //   │  SALA 1      ├──────┤  SALA 3     │
        //   │  Bomba       │PasX  │  Despacho   │
        //   └──────────────┘      └─────────────┘
        //   Z: -3..3 / X:-4..4   X: 8..16 / Z:-3..3
        //
        private static void BuildFullArchitecture(GameObject root)
        {
            GameObject env = new GameObject("Environment_Room2");
            env.transform.SetParent(root.transform, false);

            // ── SALA 1: Cámara de la Bomba ──────────────────────────────────────
            // 8 ancho (X:-4..4), 6 fondo (Z:-3..3), 3.2m altura
            BuildRoom(env, "Sala1_Bomba",
                center: new Vector3(0f, 0f, 0f), roomW: 8f, roomD: 6f, wallH: 3.2f,
                doorS: true, doorN: false, doorE: true, doorW: false);

            // Mesh cuarto2 como decoración de pared/suelo
            PlaceMesh(env, "Mesh_Cuarto2",
                $"{OBJ_PATH}/cuarto2/Meshy_AI_The_Investigation_Roo_0917134539_texture_obj/Meshy_AI_The_Investigation_Roo_0917134539_texture.obj",
                "Mat_Meshy_Cuarto2",
                new Vector3(0f, 0f, 0f), Quaternion.identity, new Vector3(1.4f, 1.4f, 1.4f));

            // ── PASILLO Z: Sala1 → Sala2 ─────────────────────────────────────────
            // 4 ancho (X:-2..2), 5 fondo (Z: -8..-3), 3.2m altura
            BuildRoom(env, "PasilloZ",
                center: new Vector3(0f, 0f, -5.5f), roomW: 4f, roomD: 5f, wallH: 3.2f,
                doorS: true, doorN: true, doorE: false, doorW: false);

            PlaceMesh(env, "Mesh_Pasillo",
                $"{OBJ_PATH}/pasilloo/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture_obj/Meshy_AI_Blue_Parlor_Overhead_0923185946_texture.obj",
                "Mat_Meshy_Pasillo",
                new Vector3(0f, 0f, -5.5f), Quaternion.identity, new Vector3(0.8f, 0.8f, 0.8f));

            // ── SALA 2: El Atrio ────────────────────────────────────────────────
            // 8 ancho (X:-4..4), 6 fondo (Z:-8..-14), 3.2m altura
            BuildRoom(env, "Sala2_Atrio",
                center: new Vector3(0f, 0f, -11f), roomW: 8f, roomD: 6f, wallH: 3.2f,
                doorS: false, doorN: true, doorE: false, doorW: false);

            PlaceMesh(env, "Mesh_LivingRoom",
                $"{OBJ_PATH}/livingroommeshy/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture_obj/Meshy_AI_Dark_Atrium_Overlook_0922163706_texture.obj",
                "Mat_Meshy_LivingRoom",
                new Vector3(0f, 0f, -11f), Quaternion.identity, new Vector3(1.0f, 1.0f, 1.0f));

            // Cama en Sala 2
            PlaceMesh(env, "Prop_Cama",
                $"{OBJ_PATH}/cama/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture_obj/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture.obj",
                "Mat_Meshy_Cama",
                new Vector3(2.5f, 0f, -12.5f), Quaternion.Euler(0f, -90f, 0f), new Vector3(1.0f, 1.0f, 1.0f));

            // Retrato en pared Oeste de Sala 2 (nota 2 pegada aquí)
            PlaceMesh(env, "Prop_Retrato",
                $"{OBJ_PATH}/retrato/Meshy_AI_The_Faded_Duchess_0917125445_texture_obj/Meshy_AI_The_Faded_Duchess_0917125445_texture.obj",
                "Mat_Meshy_Retrato",
                new Vector3(-3.6f, 1.5f, -10.5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.8f, 0.8f, 0.8f));

            // ── PASILLO X: Sala1 → Sala3 ─────────────────────────────────────────
            // 5 largo (X:4..9), 3 fondo (Z:-1.5..1.5), 3.2m altura
            BuildRoom(env, "PasilloX",
                center: new Vector3(6.5f, 0f, 0f), roomW: 5f, roomD: 3f, wallH: 3.2f,
                doorS: false, doorN: false, doorE: true, doorW: true);

            // ── SALA 3: El Despacho / Salón ─────────────────────────────────────
            // 8 ancho (X:9..17), 6 fondo (Z:-3..3), 3.2m altura
            BuildRoom(env, "Sala3_Despacho",
                center: new Vector3(13f, 0f, 0f), roomW: 8f, roomD: 6f, wallH: 3.2f,
                doorS: false, doorN: false, doorE: false, doorW: true);

            PlaceMesh(env, "Mesh_SalaDeEstar",
                $"{OBJ_PATH}/saladeestar/Meshy_AI_Shadowed_Parlor_0923190159_texture_obj/Meshy_AI_Shadowed_Parlor_0923190159_texture.obj",
                "Mat_Meshy_SalaDeEstar",
                new Vector3(13f, 0f, 0f), Quaternion.identity, new Vector3(1.0f, 1.0f, 1.0f));

            // Mueblesito en Sala 3 (nota 1 encima)
            PlaceMesh(env, "Prop_Mueblesito",
                $"{OBJ_PATH}/mueblesito/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture_obj/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture.obj",
                "Mat_Meshy_Mueblesito",
                new Vector3(10f, 0f, -1.5f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.9f, 0.9f, 0.9f));

            // Libros en Sala 3 (nota 3 encima)
            PlaceMesh(env, "Prop_Libros",
                $"{OBJ_PATH}/Libros/Meshy_AI_Ornate_Book_Stack_0917121445_texture_obj/Meshy_AI_Ornate_Book_Stack_0917121445_texture.obj",
                "Mat_Meshy_Libros",
                new Vector3(16f, 0.4f, 2f), Quaternion.Euler(0f, 20f, 0f), new Vector3(0.45f, 0.45f, 0.45f));

            // ── SUELO MAESTRO (colisionador que cubre todo el layout) ────────────
            // Cubre: X[-4..17] Z[-14..3]
            Box(env, "Master_Floor_Collider",
                new Vector3(6.5f, -0.12f, -5.5f),
                new Vector3(22f, 0.2f, 18f), null, withRenderer: false);
        }

        /// <summary>
        /// Construye un cuarto cerrado. doorS/N/E/W = tiene hueco de puerta en esa pared.
        /// </summary>
        private static void BuildRoom(GameObject parent, string roomName,
            Vector3 center, float roomW, float roomD, float wallH,
            bool doorS, bool doorN, bool doorE, bool doorW)
        {
            GameObject room = new GameObject(roomName);
            room.transform.SetParent(parent.transform, false);

            float cx = center.x, cz = center.z, y0 = center.y;
            float hw = roomW * 0.5f, hd = roomD * 0.5f;

            Material mF = Mat("Mat_Room_Floor");
            Material mC = Mat("Mat_Room_Ceiling");
            Material mW = Mat("Mat_Room_Wall");
            Material mW2= Mat("Mat_Room_Wall2");
            const float T = 0.25f; // espesor de pared

            // Suelo y techo
            Box(room, "Floor",   new Vector3(cx, y0,          cz), new Vector3(roomW, T, roomD), mF);
            Box(room, "Ceiling", new Vector3(cx, y0 + wallH,  cz), new Vector3(roomW, T, roomD), mC);

            // Paredes Norte/Sur (alineadas en X, normal en Z)
            BuildWallWithDoor(room, "Wall_N", cx, y0, cz + hd, roomW, wallH, T, true,  doorN, mW);
            BuildWallWithDoor(room, "Wall_S", cx, y0, cz - hd, roomW, wallH, T, true,  doorS, mW);
            // Paredes Este/Oeste (alineadas en Z, normal en X)
            BuildWallWithDoor(room, "Wall_E", cx + hw, y0, cz, roomD, wallH, T, false, doorE, mW2);
            BuildWallWithDoor(room, "Wall_W", cx - hw, y0, cz, roomD, wallH, T, false, doorW, mW2);
        }

        /// <summary>
        /// Construye una sección de pared, opcionalmente con vano de puerta centrado (2m × 2.2m).
        /// isZAligned=true → la pared se extiende en X (paredes Norte/Sur).
        /// isZAligned=false → la pared se extiende en Z (paredes Este/Oeste).
        /// </summary>
        private static void BuildWallWithDoor(GameObject parent, string wName,
            float cx, float y0, float cz, float wallLen, float wallH, float wallThick,
            bool isZAligned, bool hasDoor, Material mat)
        {
            const float DW = 1.8f;   // ancho del vano de puerta
            const float DH = 2.2f;   // alto del vano de puerta

            if (!hasDoor)
            {
                // Pared sólida sin puerta
                if (isZAligned)
                    Box(parent, wName, new Vector3(cx, y0 + wallH * 0.5f, cz), new Vector3(wallLen, wallH, wallThick), mat);
                else
                    Box(parent, wName, new Vector3(cx, y0 + wallH * 0.5f, cz), new Vector3(wallThick, wallH, wallLen), mat);
                return;
            }

            // Pared con hueco de puerta central
            float sideLen = (wallLen - DW) * 0.5f;
            float sideOff = sideLen * 0.5f + DW * 0.5f;

            if (isZAligned) // Norte / Sur
            {
                Box(parent, wName + "_L",   new Vector3(cx - sideOff, y0 + wallH * 0.5f, cz), new Vector3(sideLen, wallH, wallThick), mat);
                Box(parent, wName + "_R",   new Vector3(cx + sideOff, y0 + wallH * 0.5f, cz), new Vector3(sideLen, wallH, wallThick), mat);
                Box(parent, wName + "_Top", new Vector3(cx, y0 + DH + (wallH - DH) * 0.5f, cz), new Vector3(DW, wallH - DH, wallThick), mat);
            }
            else // Este / Oeste
            {
                Box(parent, wName + "_L",   new Vector3(cx, y0 + wallH * 0.5f, cz - sideOff), new Vector3(wallThick, wallH, sideLen), mat);
                Box(parent, wName + "_R",   new Vector3(cx, y0 + wallH * 0.5f, cz + sideOff), new Vector3(wallThick, wallH, sideLen), mat);
                Box(parent, wName + "_Top", new Vector3(cx, y0 + DH + (wallH - DH) * 0.5f, cz), new Vector3(wallThick, wallH - DH, DW), mat);
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // ILUMINACIÓN — URP necesita intensidades altas (8-20) para cerrar cuartos
        // ═════════════════════════════════════════════════════════════════════════
        private static void SetupLighting(GameObject root)
        {
            // Luz ambiental plana y clara (casi blanca) — evita que las sombras sean negras
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.52f, 0.48f);

            GameObject rig = new GameObject("Lighting_Rig");
            rig.transform.SetParent(root.transform, false);

            // ── Luz Direccional (relleno general) ────────────────────────────────
            GameObject dirGo = new GameObject("Dir_Light_Fill");
            dirGo.transform.SetParent(rig.transform, false);
            dirGo.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            Light dir = dirGo.AddComponent<Light>();
            dir.type      = LightType.Directional;
            dir.color     = new Color(1.0f, 0.95f, 0.88f);
            dir.intensity = 1.2f;
            dir.shadows   = LightShadows.None; // sin sombras en las rooms cerradas

            // ── Luces de Sala — intensidad alta para URP ─────────────────────────
            // Sala 1: Cámara de la Bomba  (centro ≈ 0, 2.8, 1)
            AddLight(rig, "Light_Sala1_A", new Vector3(-2f, 2.8f,  3f),  LightType.Point, new Color(1.0f, 0.92f, 0.80f), 12f, 14f);
            AddLight(rig, "Light_Sala1_B", new Vector3( 2f, 2.8f, -1f),  LightType.Point, new Color(1.0f, 0.92f, 0.80f), 10f, 14f);

            // Pasillo Z (Sala1→Sala2)
            AddLight(rig, "Light_PasilloZ", new Vector3(0f, 2.5f, -6.5f), LightType.Point, new Color(0.85f, 0.90f, 1.0f), 10f, 8f);

            // Sala 2: El Atrio
            AddLight(rig, "Light_Sala2_A", new Vector3(-2f, 2.8f,-12f),  LightType.Point, new Color(1.0f, 0.88f, 0.75f), 12f, 14f);
            AddLight(rig, "Light_Sala2_B", new Vector3( 2f, 2.8f,-15f),  LightType.Point, new Color(1.0f, 0.88f, 0.75f), 10f, 14f);

            // Pasillo X (Sala1→Sala3)
            AddLight(rig, "Light_PasilloX", new Vector3(9f, 2.5f, 1f),   LightType.Point, new Color(0.85f, 0.88f, 1.0f), 10f, 8f);

            // Sala 3: El Despacho
            AddLight(rig, "Light_Sala3_A", new Vector3(14f, 2.8f, 3f),   LightType.Point, new Color(1.0f, 0.90f, 0.75f), 12f, 14f);
            AddLight(rig, "Light_Sala3_B", new Vector3(20f, 2.8f,-1f),   LightType.Point, new Color(1.0f, 0.90f, 0.75f), 10f, 14f);

            // ── Foco dramático sobre la mesa de la bomba ─────────────────────────
            GameObject spot = new GameObject("Bomb_Spotlight");
            spot.transform.SetParent(rig.transform, false);
            spot.transform.localPosition = new Vector3(0f, 3.2f, 2.2f);
            spot.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            Light sl = spot.AddComponent<Light>();
            sl.type       = LightType.Spot;
            sl.spotAngle  = 55f;
            sl.color      = new Color(1f, 0.98f, 0.90f);
            sl.intensity  = 15f;
            sl.range      = 6f;
            sl.shadows    = LightShadows.None;
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
        // BOMBA TÁCTICA
        // ═════════════════════════════════════════════════════════════════════════
        private static GameObject BuildTacticalBomb(GameObject root)
        {
            Material matCase   = Mat("Mat_TacticalCase");
            Material matButton = Mat("Mat_KeypadButton");
            Material matC4     = Mat("Mat_C4Explosive");
            Material matPCB    = Mat("Mat_CircuitBoard");
            Material matWood   = Mat("Mat_WoodTrim");

            // Mesa de la bomba (en Sala 1)
            GameObject table = new GameObject("Table_BombStation");
            table.transform.SetParent(root.transform, false);
            table.transform.localPosition = new Vector3(0f, 0f, 2f);
            Box(table, "Top",    new Vector3(0f, 0.76f, 0f), new Vector3(1.8f, 0.06f, 1.1f), matWood);
            Box(table, "Leg_FL", new Vector3( 0.8f, 0.37f,  0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood);
            Box(table, "Leg_FR", new Vector3(-0.8f, 0.37f,  0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood);
            Box(table, "Leg_BL", new Vector3( 0.8f, 0.37f, -0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood);
            Box(table, "Leg_BR", new Vector3(-0.8f, 0.37f, -0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood);

            // Bomba sobre la mesa
            GameObject bombRoot = new GameObject("Tactical_C4_Bomb");
            bombRoot.transform.SetParent(root.transform, false);
            bombRoot.transform.localPosition = new Vector3(0.1f, 0.79f, 2f);
            bombRoot.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);

            // Maletín
            GameObject cBase = Box(bombRoot, "Case_Base", new Vector3(0f, 0.05f, 0f),   new Vector3(0.46f, 0.10f, 0.34f), matCase);
            UnityEngine.Object.DestroyImmediate(cBase.GetComponent<Collider>());

            GameObject lid = Box(bombRoot, "Case_Lid", new Vector3(0f, 0.21f, 0.17f), new Vector3(0.46f, 0.24f, 0.04f), matCase);
            lid.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            UnityEngine.Object.DestroyImmediate(lid.GetComponent<Collider>());

            // C4 blocks
            for (int i = 0; i < 3; i++)
            {
                float z = -0.08f + i * 0.08f;
                GameObject c4 = Box(bombRoot, $"C4_{i}", new Vector3(-0.12f, 0.11f, z), new Vector3(0.15f, 0.06f, 0.065f), matC4);
                UnityEngine.Object.DestroyImmediate(c4.GetComponent<Collider>());
            }

            // PCB
            GameObject pcb = Box(bombRoot, "PCB", new Vector3(0.11f, 0.105f, 0f), new Vector3(0.19f, 0.015f, 0.28f), matPCB);
            UnityEngine.Object.DestroyImmediate(pcb.GetComponent<Collider>());

            // Display bezel
            GameObject bezel = Box(bombRoot, "Display_Bezel", new Vector3(0.11f, 0.125f, 0.08f), new Vector3(0.17f, 0.035f, 0.08f), matCase);
            UnityEngine.Object.DestroyImmediate(bezel.GetComponent<Collider>());

            TextMeshPro timerTMP  = MakeWorldTMP(bezel, "Timer_TMP",  new Vector3(0f, 0.52f,  0.01f), "05:00.00",  2.2f, new Color(1f, 0.45f, 0.15f));
            TextMeshPro codeTMP   = MakeWorldTMP(bezel, "Code_TMP",   new Vector3(0f, 0.52f, -0.025f), "_ _ _ _",  1.8f, Color.cyan);
            TextMeshPro statusTMP = MakeWorldTMP(bezel, "Status_TMP", new Vector3(0f, 0.52f,  0.036f), "DISPOSITIVO ARMADO (4 DÍGITOS)", 0.80f, Color.yellow);

            // LEDs
            Light rLight = AddBombLed(bombRoot, "LED_Red",   new Vector3(0.03f, 0.13f, 0.11f), Color.red,   1.0f, true);
            Light gLight = AddBombLed(bombRoot, "LED_Green", new Vector3(0.19f, 0.13f, 0.11f), Color.green, 1.4f, false);

            // Teclado 4×3
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
                if (btnCol != null) btnCol.size = new Vector3(0.042f, 0.030f, 0.036f);
                MakeWorldTMP(btn, "Key_Text", new Vector3(0f, 0.52f, 0f), k, 1.2f,
                    (k == "ENT") ? Color.green : (k == "C" ? Color.red : Color.white));
                btn.AddComponent<BombKeypadButton>().keyValue = k;
            }

            bombRoot.AddComponent<AudioSource>();
            BombAudioSynthesizer audio = bombRoot.AddComponent<BombAudioSynthesizer>();
            BombController ctrl = bombRoot.AddComponent<BombController>();
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
        // NOTAS INTERACTIVAS — posiciones en escala humana, con luz indicadora
        // ═════════════════════════════════════════════════════════════════════════
        //
        // Sala 1 (X:-4..4, Z:-3..3):
        //   Nota 4 → sobre la mesa de la bomba:   (0.6, 0.82, 1.2)
        // Sala 2 (X:-4..4, Z:-8..-14):
        //   Nota 2 → sobre la cama / retrato:     (-1.0, 1.05, -11.0)
        // Sala 3 (X:9..17, Z:-3..3):
        //   Nota 1 → encima del mueblesito:       (10.5, 1.15, -1.5)
        //   Nota 3 → encima de los libros:        (16.0, 0.90,  2.0)
        //
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
                new Vector3(10.5f, 1.15f, -1.5f), Quaternion.Euler(0f, 20f, 0f), c1, mat));

            // Nota 2 – Sala 2, sobre la cama / pared del retrato
            list.Add(CreateNote(notesRoot, "Note_2_Retrato",
                new Vector3(-1.0f, 1.05f, -11.0f), Quaternion.Euler(0f, 0f, 0f), c2, mat));

            // Nota 3 – Sala 3, sobre los libros
            list.Add(CreateNote(notesRoot, "Note_3_Libros",
                new Vector3(16.0f, 0.90f, 2.0f), Quaternion.Euler(0f, -10f, 0f), c3, mat));

            // Nota 4 – Sala 1, sobre la mesa de la bomba
            list.Add(CreateNote(notesRoot, "Note_4_Mesa",
                new Vector3(-0.8f, 0.82f, 0.5f), Quaternion.Euler(0f, 5f, 0f), c4, mat));

            return list;
        }

        private static ClueInteractable CreateNote(GameObject parent, string noteName,
            Vector3 worldPos, Quaternion rot, ClueData clue, Material mat)
        {
            int ilayer = LayerMask.NameToLayer("Interactable");
            if (ilayer < 0) ilayer = 0;

            // Objeto raíz de la nota
            GameObject noteObj = new GameObject(noteName);
            noteObj.transform.SetParent(parent.transform, false);
            noteObj.transform.position = worldPos;
            noteObj.transform.rotation = rot;
            noteObj.layer = ilayer;

            // Visual (hoja de papel — A4, fácil de ver y clicar)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Note_Mesh";
            visual.transform.SetParent(noteObj.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale    = new Vector3(0.30f, 0.008f, 0.22f);
            visual.layer = ilayer;
            visual.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Banda superior amarilla (indicador visual de nota interactiva)
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

            // Texto en la superficie
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

            // Popup flotante (aparece al interactuar)
            GameObject popup = new GameObject("Inspection_Popup");
            popup.transform.SetParent(noteObj.transform, false);
            popup.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            popup.transform.localScale    = new Vector3(0.005f, 0.005f, 0.005f);
            popup.SetActive(false);

            // Fondo del popup
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

            // Collider principal de la nota (para raycast desde FPSRaycastInteractor)
            BoxCollider bc = noteObj.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.25f, 0.08f, 0.35f);

            // Rigidbody para XR Grab (kinematic por defecto para que no salga volando por colisiones)
            Rigidbody rb = noteObj.AddComponent<Rigidbody>();
            rb.mass      = 0.1f;
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearDamping = 1.5f;
            rb.angularDamping = 1.2f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // VRNoteInteractable (agarre VR)
            VRNoteInteractable vrNote = noteObj.AddComponent<VRNoteInteractable>();
            vrNote.clueData          = clue;
            vrNote.inWorldNoteText   = surfTMP;
            vrNote.inspectionPopup   = popup;
            vrNote.popupTitle        = pTitle;
            vrNote.popupBody         = pBody;
            vrNote.popupDigitHint    = pDigit;

            // Antigravity Grab
            AntigravityGrabInteractable agGrab = noteObj.AddComponent<AntigravityGrabInteractable>();
            agGrab.vrNoteComponent   = vrNote;
            agGrab.floatInZeroGravity = true;

            // ClueInteractable → para FPSRaycastInteractor en PC
            ClueInteractable ci     = noteObj.AddComponent<ClueInteractable>();
            ci.clueData             = clue;
            ci.inspectionCardPopup  = popup;
            ci.cardTitleText        = pTitle;
            ci.cardBodyText         = pBody;
            ci.cardDatoText         = pDigit;

            // ── Luz indicadora amarilla sobre la nota (hace la nota visible) ────
            // Se desactiva al ser recogida (Room2EscapeRoomManager lo gestiona)
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

            // Montado en pared Oeste de Sala 1
            GameObject termRoot = new GameObject("Save_Terminal_Console");
            termRoot.transform.SetParent(root.transform, false);
            termRoot.transform.localPosition = new Vector3(-5.7f, 1.8f, 1f);
            termRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            Box(termRoot, "Chassis",      Vector3.zero,                new Vector3(0.90f, 0.70f, 0.08f), matCase);
            Box(termRoot, "Screen_Bezel", new Vector3(0f, 0.07f,-0.035f), new Vector3(0.82f, 0.44f, 0.02f), matCase);

            GameObject screenObj = Box(termRoot, "Screen_Glass", new Vector3(0f, 0.07f,-0.046f), new Vector3(0.80f, 0.42f, 0.01f), null, withRenderer: true);
            screenObj.GetComponent<Renderer>().material.color = new Color(0.04f, 0.07f, 0.10f, 0.95f);

            // LED de estado
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

            // Botón físico
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
        private static void SetupSystems(GameObject root, BombController bomb, List<ClueInteractable> notes)
        {
            GameObject sys = new GameObject("Systems_Room2");
            sys.transform.SetParent(root.transform, false);

            // Save Manager
            GameSaveManager saveMgr = sys.AddComponent<GameSaveManager>();
            saveMgr.bombController      = bomb;
            saveMgr.autoLoadOnStart     = false;
            saveMgr.autoSaveOnMilestones = true;

            // Room2 Escape Room Manager
            Room2EscapeRoomManager roomMgr = sys.AddComponent<Room2EscapeRoomManager>();
            roomMgr.bombController = bomb;
            // Mapear ClueInteractable → VRNoteInteractable (están en el mismo GameObject)
            var vrNotes = new List<VRNoteInteractable>();
            foreach (var ci in notes)
            {
                var vr = ci.GetComponent<VRNoteInteractable>();
                if (vr != null) vrNotes.Add(vr);
            }
            roomMgr.roomNotes = vrNotes;

            // Room2 UI Manager (HUD + popup guardado + toast)
            Room2UIManager uiMgr = sys.AddComponent<Room2UIManager>();
            uiMgr.bomb = bomb;
            if (notes != null && notes.Count >= 4)
            {
                uiMgr.clue1 = notes[0].clueData;
                uiMgr.clue2 = notes[1].clueData;
                uiMgr.clue3 = notes[2].clueData;
                uiMgr.clue4 = notes[3].clueData;
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // PLAYER: PC Fallback + XR Origin
        // ═════════════════════════════════════════════════════════════════════════
        private static void SetupPlayer(GameObject root)
        {
            // ── XR Interaction Manager ────────────────────────────────────────────
            GameObject xriMgrObj = new GameObject("XR Interaction Manager");
            xriMgrObj.AddComponent<XRInteractionManager>();

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0) playerLayer = 0;

            // ── XR Origin (VR) ────────────────────────────────────────────────────
            string xrPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            GameObject xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrPrefabPath);
            if (xrPrefab != null)
            {
                GameObject xrRig = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
                xrRig.name = "XR Origin (XR Rig)";
                xrRig.transform.position = new Vector3(0f, 0.05f, 0f); // frente a la mesa de la bomba
                xrRig.layer = playerLayer;

                CharacterController xrCC = xrRig.GetComponent<CharacterController>();
                if (xrCC == null)
                {
                    xrCC = xrRig.AddComponent<CharacterController>();
                    xrCC.height = 1.8f; xrCC.radius = 0.35f;
                    xrCC.center = new Vector3(0f, 0.9f, 0f);
                }

                AntigravityPlayerController agXR = xrRig.GetComponent<AntigravityPlayerController>();
                if (agXR == null) agXR = xrRig.AddComponent<AntigravityPlayerController>();
                Camera xrCam = xrRig.GetComponentInChildren<Camera>();
                if (xrCam != null) agXR.headTransform = xrCam.transform;

                SetupXRHandPhysics(xrRig, agXR);
            }

            // ── PC TestingFallback ────────────────────────────────────────────────
            // Colocar al jugador a 1.5m al sur de la bomba, mirando hacia ella
            Vector3 spawnPos = new Vector3(0f, 0.05f, 0f);

            GameObject pcFallback = new GameObject("Player_PC_TestingFallback");
            pcFallback.transform.position = spawnPos;
            pcFallback.layer = playerLayer;

            CharacterController cc = pcFallback.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.875f, 0f);

            // Cámara
            GameObject camGo = new GameObject("Fallback_Camera");
            camGo.transform.SetParent(pcFallback.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camGo.tag = "MainCamera";

            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            // IMPORTANTE: FPSRaycastInteractor sobre la cámara para que el raycast funcione
            FPSRaycastInteractor raycast = camGo.AddComponent<FPSRaycastInteractor>();
            raycast.interactDistance = 3.5f;
            raycast.interactMask = ~0;

            // Antigravity locomotion
            AntigravityPlayerController pcAg = pcFallback.AddComponent<AntigravityPlayerController>();
            pcAg.headTransform = camGo.transform;

            // Script PC fallback
            Player_PC_TestingFallback pcScript = pcFallback.AddComponent<Player_PC_TestingFallback>();
            pcScript.playerCamera = cam;

            // Mano virtual PC
            GameObject pcHand = new GameObject("PC_VirtualHand");
            pcHand.transform.SetParent(camGo.transform, false);
            pcHand.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            SphereCollider sc = pcHand.AddComponent<SphereCollider>();
            sc.radius = 0.18f;
            sc.isTrigger = true;
            AntigravityHandPhysics hp = pcHand.AddComponent<AntigravityHandPhysics>();
            hp.playerController = pcAg;
        }

        private static void SetupXRHandPhysics(GameObject xrRig, AntigravityPlayerController ctrl)
        {
            int handsLayer = LayerMask.NameToLayer("Hands");
            if (handsLayer < 0) handsLayer = 0;

            foreach (Transform t in xrRig.GetComponentsInChildren<Transform>(true))
            {
                bool isLeft  = t.name.Contains("Left Controller")  || t.name.Contains("LeftHand")  || t.name.Contains("Left Hand");
                bool isRight = t.name.Contains("Right Controller") || t.name.Contains("RightHand") || t.name.Contains("Right Hand");
                if (!isLeft && !isRight) continue;

                t.gameObject.layer = handsLayer;
                SphereCollider col = t.GetComponent<SphereCollider>();
                if (col == null) { col = t.gameObject.AddComponent<SphereCollider>(); col.radius = 0.08f; col.isTrigger = true; }
                AntigravityHandPhysics ahp = t.GetComponent<AntigravityHandPhysics>();
                if (ahp == null) ahp = t.gameObject.AddComponent<AntigravityHandPhysics>();
                ahp.handType = isLeft ? AntigravityHandPhysics.HandType.Left : AntigravityHandPhysics.HandType.Right;
                ahp.playerController = ctrl;
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════════

        /// <summary>Crea un cubo primitivo y lo hijo de parent.</summary>
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

        /// <summary>Instancia un mesh Meshy AI y le asigna material.</summary>
        private static GameObject PlaceMesh(GameObject parent, string name, string objPath, string matName,
            Vector3 worldPos, Quaternion rot, Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            GameObject inst;
            if (prefab != null)
            {
                inst = UnityEngine.Object.Instantiate(prefab, parent.transform);
                inst.name = name;
            }
            else
            {
                // Fallback: cubo de placeholder si el OBJ no se encontró
                inst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                inst.name = name + "_PLACEHOLDER";
                inst.transform.SetParent(parent.transform, false);
            }

            inst.transform.position   = worldPos;
            inst.transform.rotation   = rot;
            inst.transform.localScale = scale;

            Material mat = Mat(matName);
            if (mat != null)
            {
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = mat;
            }

            // Agregar MeshColliders para que los props sean sólidos
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh  = mf.sharedMesh;
                }
            }

            return inst;
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
