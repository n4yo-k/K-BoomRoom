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
using StylizedRoom;

namespace DefusalGame.Editor
{
    /// <summary>
    /// Generador automatizado de la escena Room2 (Escape Room VR).
    /// Integra los assets 3D de Meshy AI, la bomba de 3 dígitos, las 3 notas interactivas con XRGrabInteractable,
    /// el sistema de guardado en JSON, la iluminación de escape room y el XR Rig.
    /// </summary>
    [InitializeOnLoad]
    public class Room2SceneBuilder : EditorWindow
    {
        private const string SCENE_PATH = "Assets/Scenes/Room2.unity";
        private const string GAME_DATA_PATH = "Assets/Game/Data";
        private const string MAT_PATH = "Assets/StylizedRoom/Materials";
        private const string EXTRACTED_OBJS_PATH = "Assets/objRefs/Extracted";
        private const string REBUILD_FLAG_PATH = "Assets/.rebuild_room2_pending";

        static Room2SceneBuilder()
        {
            EditorApplication.delayCall += CheckAndAutoBuild;
        }

        private static void CheckAndAutoBuild()
        {
            if (!File.Exists(SCENE_PATH) || File.Exists(REBUILD_FLAG_PATH))
            {
                if (File.Exists(REBUILD_FLAG_PATH))
                {
                    try { File.Delete(REBUILD_FLAG_PATH); } catch { }
                }
                Debug.Log("[Room2SceneBuilder] Actualizando y reconstruyendo Room2 con componentes Antigravity y Guardado 3D...");
                BuildRoom2Scene(showDialog: false);
            }
        }

        [MenuItem("Desactiva la Bomba/Construir Escena Room2 (Escape Room VR)", false, 2)]
        public static void BuildMenuCommand()
        {
            BuildRoom2Scene(showDialog: true);
        }

        [MenuItem("Desactiva la Bomba/Configurar Antigravity y Guardado en Room2", false, 3)]
        public static void ConfigureAntigravityMenuCommand()
        {
            BuildRoom2Scene(showDialog: true);
        }

        public static void BuildRoom2Scene(bool showDialog = true)
        {
            // Forzar actualización sincrónica del AssetDatabase para importar los OBJ y texturas
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            EnsureDataAssets();
            SetupGameMaterials();

            // 1. Crear o abrir la nueva escena Room2
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Construir jerarquía completa
            GameObject root = new GameObject("DefusalGame_Room2");

            // Iluminación
            SetupLighting(root);

            // Arquitectura y Props de Meshy AI
            GameObject env = BuildArchitectureAndProps(root);

            // Bomba Táctica con Teclado de 3 Dígitos
            GameObject bombObj = BuildTacticalBomb(root);

            // 3 Notas Interactivas (XRGrabInteractable + Antigravity)
            List<VRNoteInteractable> notes = PlaceEscapeNotes(root);

            // Pizarra de Misión
            TextMeshPro missionBoardTMP = BuildMissionBoard(root);

            // Terminal 3D de Guardado Antigravity (World-Space UI y Botón Físico)
            AntigravitySaveTerminal saveTerminal = BuildSaveTerminal(root);

            // Sistemas de Guardado y Escape Room Manager
            SetupSystems(root, bombObj.GetComponent<BombController>(), notes, missionBoardTMP);

            // Configurar VR Rig (XR Origin + Controles) y soporte PC Play Mode con Antigravity
            SetupVRAndPlayerRig(root);

            // 3. Guardar la escena en Assets/Scenes/Room2.unity
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);

            // 4. Registrar en EditorBuildSettings
            RegisterSceneInBuildSettings(SCENE_PATH);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            Debug.Log("[Room2SceneBuilder] ¡Escena Room2 construida y guardada exitosamente en " + SCENE_PATH + "!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "¡Room2 (Escape Room VR) Lista!",
                    "La escena Room2 se ha generado con éxito:\n\n" +
                    "• Arquitectura base con 'cuarto2.obj' y colliders continuos.\n" +
                    "• Decoración con assets Meshy AI: Cama, Mueblesito, Retrato, Libros y Pizarra.\n" +
                    "• Bomba Táctica C4 configurada para 3 DÍGITOS (Código: 739).\n" +
                    "• 3 Notas interactivas con XR Grab Interactable y textos legibles.\n" +
                    "• Pizarra de estado y progreso en tiempo real (0/3 pistas).\n" +
                    "• Sistema modular de Guardado/Carga JSON (F5 guardar, F9 cargar, F12 reiniciar).\n" +
                    "• XR Rig configurado para VR directa y soporte de prueba en Editor.",
                    "¡Excelente!"
                );
            }
        }

        #region ScriptableObjects Data
        public static void EnsureDataAssets()
        {
            Directory.CreateDirectory(GAME_DATA_PATH);

            // Nota 1: Mueble Antiguo (Dígito 7)
            ClueData c1 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_01.asset");
            if (c1 == null)
            {
                c1 = ScriptableObject.CreateInstance<ClueData>();
                c1.clueId = "CLUE_ROOM2_01";
                c1.clueName = "Nota del Mueble Antiguo";
                c1.description = "Nota escrita con tinta en el aparador: 'EL PRIMER DÍGITO ES EL 7'.";
                c1.revealedValue = "7";
                c1.sequenceIndex = 0;
                c1.hintLocation = "Mueble Aparador de Madera";
                AssetDatabase.CreateAsset(c1, $"{GAME_DATA_PATH}/Room2_Clue_01.asset");
            }

            // Nota 2: Retrato de la Duquesa (Dígito 3)
            ClueData c2 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_02.asset");
            if (c2 == null)
            {
                c2 = ScriptableObject.CreateInstance<ClueData>();
                c2.clueId = "CLUE_ROOM2_02";
                c2.clueName = "Nota Oculta tras el Retrato";
                c2.description = "Papel escondido junto al marco de la pintura: 'EL SEGUNDO DÍGITO ES EL 3'.";
                c2.revealedValue = "3";
                c2.sequenceIndex = 1;
                c2.hintLocation = "Retrato en la Pared";
                AssetDatabase.CreateAsset(c2, $"{GAME_DATA_PATH}/Room2_Clue_02.asset");
            }

            // Nota 3: Libros Antiguos (Dígito 9)
            ClueData c3 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_03.asset");
            if (c3 == null)
            {
                c3 = ScriptableObject.CreateInstance<ClueData>();
                c3.clueId = "CLUE_ROOM2_03";
                c3.clueName = "Nota Marcada en los Libros";
                c3.description = "Marcapáginas entre los tomos: 'EL TERCER DÍGITO ES EL 9'.";
                c3.revealedValue = "9";
                c3.sequenceIndex = 2;
                c3.hintLocation = "Pila de Libros Ornamentados";
                AssetDatabase.CreateAsset(c3, $"{GAME_DATA_PATH}/Room2_Clue_03.asset");
            }

            // Configuración de la Bomba para 3 Dígitos (Código "739")
            BombConfigData cfg = AssetDatabase.LoadAssetAtPath<BombConfigData>($"{GAME_DATA_PATH}/Room2_BombConfig.asset");
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<BombConfigData>();
                cfg.bombId = "BOMB_ROOM2_01";
                cfg.bombName = "Bomba Escape Room - 3 Dígitos";
                cfg.timeLimitSeconds = 300f; // 5 minutos
                cfg.penaltySecondsOnFail = 30f;
                cfg.targetSequence = "739";
                cfg.requiredCluesCount = 3;
                cfg.linkedClues = new List<ClueData>() { c1, c2, c3 };
                AssetDatabase.CreateAsset(cfg, $"{GAME_DATA_PATH}/Room2_BombConfig.asset");
            }

            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Materials
        private static void SetupGameMaterials()
        {
            Directory.CreateDirectory(MAT_PATH);
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            // Materiales de Meshy AI con sus texturas PNG
            CreateTexturedMat("Mat_Meshy_Cuarto2", $"{EXTRACTED_OBJS_PATH}/cuarto2/Meshy_AI_The_Investigation_Roo_0917134539_texture_obj/Meshy_AI_The_Investigation_Roo_0917134539_texture.png", litShader);
            CreateTexturedMat("Mat_Meshy_Cama", $"{EXTRACTED_OBJS_PATH}/cama/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture_obj/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture.png", litShader);
            CreateTexturedMat("Mat_Meshy_Mueblesito", $"{EXTRACTED_OBJS_PATH}/mueblesito/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture_obj/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture.png", litShader);
            CreateTexturedMat("Mat_Meshy_Retrato", $"{EXTRACTED_OBJS_PATH}/retrato/Meshy_AI_The_Faded_Duchess_0917125445_texture_obj/Meshy_AI_The_Faded_Duchess_0917125445_texture.png", litShader);
            CreateTexturedMat("Mat_Meshy_Libros", $"{EXTRACTED_OBJS_PATH}/Libros/Meshy_AI_Ornate_Book_Stack_0917121445_texture_obj/Meshy_AI_Ornate_Book_Stack_0917121445_texture.png", litShader);

            // Material para la pizarra de proyección
            GetOrCreateMat("Mat_Meshy_Pizarra", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.20f, 0.22f, 0.24f));
                m.SetFloat("_Smoothness", 0.1f);
            });

            // Material de nota de papel
            GetOrCreateMat("Mat_PaperNote", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.96f, 0.93f, 0.83f));
                m.SetFloat("_Smoothness", 0.05f);
            });

            AssetDatabase.SaveAssets();
        }

        private static Material CreateTexturedMat(string matName, string texPath, Shader shader)
        {
            string path = $"{MAT_PATH}/{matName}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            if (m == null)
            {
                m = new Material(shader);
                if (tex != null)
                {
                    m.SetTexture("_BaseMap", tex);
                    m.SetTexture("_MainTex", tex);
                }
                m.SetFloat("_Smoothness", 0.25f);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                if (tex != null)
                {
                    m.SetTexture("_BaseMap", tex);
                    m.SetTexture("_MainTex", tex);
                }
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        private static Material GetOrCreateMat(string name, Shader shader, Action<Material> cfg)
        {
            string path = $"{MAT_PATH}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                cfg(m);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = shader;
                cfg(m);
                EditorUtility.SetDirty(m);
            }
            return m;
        }
        #endregion

        #region Architecture & Meshy Props
        private static GameObject BuildArchitectureAndProps(GameObject root)
        {
            int archLayer = LayerMask.NameToLayer("RoomArchitecture");
            if (archLayer < 0) archLayer = 0;

            GameObject env = new GameObject("Environment_Room2");
            env.transform.SetParent(root.transform, false);
            env.layer = archLayer;

            // 1. Habitación Principal (cuarto2.obj)
            string cuarto2Path = $"{EXTRACTED_OBJS_PATH}/cuarto2/Meshy_AI_The_Investigation_Roo_0917134539_texture_obj/Meshy_AI_The_Investigation_Roo_0917134539_texture.obj";
            GameObject cuartoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cuarto2Path);
            if (cuartoPrefab != null)
            {
                GameObject roomMesh = UnityEngine.Object.Instantiate(cuartoPrefab, env.transform);
                roomMesh.name = "Room2_BaseArchitecture_Meshy";
                float scale = 4.0f; // Tamaño amplio y espacioso para VR
                roomMesh.transform.localScale = new Vector3(scale, scale, scale);
                roomMesh.transform.localPosition = new Vector3(0f, 3.48f, 0f); // Centrado con suelo en Y=0
                roomMesh.layer = archLayer;

                Material cuartoMat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_Meshy_Cuarto2.mat");
                foreach (var rend in roomMesh.GetComponentsInChildren<Renderer>(true))
                {
                    if (cuartoMat != null) rend.sharedMaterial = cuartoMat;
                }

                // Blindaje físico estricto: MeshColliders en cada submalla de cuarto2.obj para evitar traspaso de paredes
                foreach (var mf in roomMesh.GetComponentsInChildren<MeshFilter>(true))
                {
                    mf.gameObject.layer = archLayer;
                    if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                    {
                        MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
            }

            // 2. Colisionadores Sólidos Perimetrales de Respaldo (Suelo, Techo y 4 Paredes)
            GameObject collidersRoot = new GameObject("Boundary_Colliders");
            collidersRoot.transform.SetParent(env.transform, false);
            collidersRoot.layer = archLayer;

            // Suelo continuo
            CreateBoxPrimitive(collidersRoot, "Floor_Collider", new Vector3(0f, -0.05f, 0f), new Vector3(7.6f, 0.1f, 7.6f), null, true, false).layer = archLayer;
            // Techo
            CreateBoxPrimitive(collidersRoot, "Ceiling_Collider", new Vector3(0f, 7.0f, 0f), new Vector3(7.6f, 0.1f, 7.6f), null, true, false).layer = archLayer;
            // Pared Norte (Z = 3.8m)
            CreateBoxPrimitive(collidersRoot, "Wall_North_Collider", new Vector3(0f, 3.5f, 3.8f), new Vector3(7.6f, 7.0f, 0.2f), null, true, false).layer = archLayer;
            // Pared Sur (Z = -3.8m)
            CreateBoxPrimitive(collidersRoot, "Wall_South_Collider", new Vector3(0f, 3.5f, -3.8f), new Vector3(7.6f, 7.0f, 0.2f), null, true, false).layer = archLayer;
            // Pared Este (X = 3.8m)
            CreateBoxPrimitive(collidersRoot, "Wall_East_Collider", new Vector3(3.8f, 3.5f, 0f), new Vector3(0.2f, 7.0f, 7.6f), null, true, false).layer = archLayer;
            // Pared Oeste (X = -3.8m)
            CreateBoxPrimitive(collidersRoot, "Wall_West_Collider", new Vector3(-3.8f, 3.5f, 0f), new Vector3(0.2f, 7.0f, 7.6f), null, true, false).layer = archLayer;

            // 3. Props de Meshy AI
            GameObject propsGroup = new GameObject("Props_MeshyAI");
            propsGroup.transform.SetParent(env.transform, false);
            propsGroup.layer = archLayer;

            // Cama rústica
            SpawnMeshyProp(propsGroup, "Prop_Cama", 
                $"{EXTRACTED_OBJS_PATH}/cama/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture_obj/Meshy_AI_Rustic_Celestial_Bed_0917123520_texture.obj",
                "Mat_Meshy_Cama",
                new Vector3(2.1f, 0.70f, -2.1f),
                Quaternion.Euler(0f, -90f, 0f),
                new Vector3(1.4f, 1.4f, 1.4f),
                new Vector3(2.5f, 1.4f, 2.5f)
            );

            // Mueble aparador (Mueblesito) - Escondite de la Nota 1
            SpawnMeshyProp(propsGroup, "Prop_Mueblesito", 
                $"{EXTRACTED_OBJS_PATH}/mueblesito/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture_obj/Meshy_AI_Weathered_Wooden_Hutc_0917124533_texture.obj",
                "Mat_Meshy_Mueblesito",
                new Vector3(-3.0f, 1.05f, 1.2f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(1.1f, 1.1f, 1.1f),
                new Vector3(1.4f, 2.1f, 1.1f)
            );

            // Retrato de la Duquesa en la pared - Escondite de la Nota 2
            SpawnMeshyProp(propsGroup, "Prop_Retrato", 
                $"{EXTRACTED_OBJS_PATH}/retrato/Meshy_AI_The_Faded_Duchess_0917125445_texture_obj/Meshy_AI_The_Faded_Duchess_0917125445_texture.obj",
                "Mat_Meshy_Retrato",
                new Vector3(-3.65f, 2.6f, -1.5f),
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(0.9f, 0.9f, 0.9f),
                new Vector3(1.5f, 1.8f, 0.3f)
            );

            // Pizarra / Pantalla en la pared Norte
            SpawnMeshyProp(propsGroup, "Prop_Pizarra", 
                $"{EXTRACTED_OBJS_PATH}/Pizarra/Meshy_AI_Blank_Projection_Scre_0917120921_generate_obj/Meshy_AI_Blank_Projection_Scre_0917120921_generate.obj",
                "Mat_Meshy_Pizarra",
                new Vector3(0f, 2.6f, 3.65f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(1.3f, 1.3f, 1.3f),
                new Vector3(2.5f, 1.4f, 0.2f)
            );

            // Mesa de Desactivación de la Bomba (al centro-derecha de la sala)
            BuildBombTable(env);

            // Pila de Libros sobre la mesa - Escondite de la Nota 3
            SpawnMeshyProp(propsGroup, "Prop_Libros", 
                $"{EXTRACTED_OBJS_PATH}/Libros/Meshy_AI_Ornate_Book_Stack_0917121445_texture_obj/Meshy_AI_Ornate_Book_Stack_0917121445_texture.obj",
                "Mat_Meshy_Libros",
                new Vector3(-0.45f, 0.88f, 0.35f),
                Quaternion.Euler(0f, 25f, 0f),
                new Vector3(0.40f, 0.40f, 0.40f),
                new Vector3(0.7f, 0.45f, 0.6f)
            );

            return env;
        }

        private static GameObject SpawnMeshyProp(GameObject parent, string name, string objPath, string matName, Vector3 pos, Quaternion rot, Vector3 scale, Vector3 colSize)
        {
            int archLayer = LayerMask.NameToLayer("RoomArchitecture");
            if (archLayer < 0) archLayer = 0;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            GameObject inst;
            if (prefab != null)
            {
                inst = UnityEngine.Object.Instantiate(prefab, parent.transform);
                inst.name = name;
            }
            else
            {
                inst = GameObject.CreatePrimitive(PrimitiveType.Cube);
                inst.name = name;
                inst.transform.SetParent(parent.transform, false);
            }

            inst.transform.localPosition = pos;
            inst.transform.localRotation = rot;
            inst.transform.localScale = scale;
            inst.layer = archLayer;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/{matName}.mat");
            if (mat != null)
            {
                foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                }
            }

            BoxCollider bc = inst.GetComponent<BoxCollider>();
            if (bc == null) bc = inst.AddComponent<BoxCollider>();
            bc.size = colSize;

            foreach (Transform t in inst.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = archLayer;
            }

            return inst;
        }

        private static void BuildBombTable(GameObject parent)
        {
            int archLayer = LayerMask.NameToLayer("RoomArchitecture");
            if (archLayer < 0) archLayer = 0;

            Material matWood = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_WoodTrim.mat");
            if (matWood == null) matWood = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_ClipboardWood.mat");

            GameObject table = new GameObject("Table_BombStation");
            table.transform.SetParent(parent.transform, false);
            table.transform.localPosition = new Vector3(0f, 0f, 0.25f);
            table.layer = archLayer;

            // Tablero principal
            CreateBoxPrimitive(table, "Table_Top", new Vector3(0f, 0.74f, 0f), new Vector3(1.80f, 0.06f, 1.10f), matWood).layer = archLayer;
            // 4 Patas
            CreateBoxPrimitive(table, "Leg_FL", new Vector3(0.80f, 0.37f, 0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood).layer = archLayer;
            CreateBoxPrimitive(table, "Leg_FR", new Vector3(-0.80f, 0.37f, 0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood).layer = archLayer;
            CreateBoxPrimitive(table, "Leg_BL", new Vector3(0.80f, 0.37f, -0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood).layer = archLayer;
            CreateBoxPrimitive(table, "Leg_BR", new Vector3(-0.80f, 0.37f, -0.45f), new Vector3(0.08f, 0.74f, 0.08f), matWood).layer = archLayer;
        }
        #endregion

        #region Tactical Bomb & 3-Digit Keypad
        private static GameObject BuildTacticalBomb(GameObject root)
        {
            Material matCase = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_TacticalCase.mat");
            Material matButton = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_KeypadButton.mat");
            Material matC4 = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_C4Explosive.mat");
            Material matPCB = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_CircuitBoard.mat");

            GameObject bombRoot = new GameObject("Tactical_C4_Bomb");
            bombRoot.transform.SetParent(root.transform, false);
            // Colocada sobre la mesa central
            bombRoot.transform.localPosition = new Vector3(0.15f, 0.77f, 0.25f);
            bombRoot.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);

            // Maletín
            CreateBoxPrimitive(bombRoot, "Case_Base", new Vector3(0f, 0.05f, 0f), new Vector3(0.46f, 0.10f, 0.34f), matCase);
            GameObject lid = CreateBoxPrimitive(bombRoot, "Case_Lid", new Vector3(0f, 0.21f, 0.17f), new Vector3(0.46f, 0.24f, 0.04f), matCase);
            lid.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            // C4 Explosives
            for (int i = 0; i < 3; i++)
            {
                float z = -0.08f + (i * 0.08f);
                GameObject c4 = CreateBoxPrimitive(bombRoot, $"C4_{i}", new Vector3(-0.12f, 0.11f, z), new Vector3(0.15f, 0.06f, 0.065f), matC4);
                CreateBoxPrimitive(c4, "Tape1", new Vector3(-0.04f, 0, 0), new Vector3(0.02f, 0.065f, 0.07f), matCase);
                CreateBoxPrimitive(c4, "Tape2", new Vector3(0.04f, 0, 0), new Vector3(0.02f, 0.065f, 0.07f), matCase);
            }

            // Circuito PCB
            CreateBoxPrimitive(bombRoot, "Circuit_Board", new Vector3(0.11f, 0.105f, 0f), new Vector3(0.19f, 0.015f, 0.28f), matPCB);

            // Display LCD
            GameObject bezel = CreateBoxPrimitive(bombRoot, "Display_Bezel", new Vector3(0.11f, 0.125f, 0.08f), new Vector3(0.17f, 0.035f, 0.08f), matCase);

            // Temporizador
            GameObject timerObj = new GameObject("Timer_TMP");
            timerObj.transform.SetParent(bezel.transform, false);
            timerObj.transform.localPosition = new Vector3(0f, 0.52f, 0.01f);
            timerObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro timerTMP = timerObj.AddComponent<TextMeshPro>();
            timerTMP.text = "05:00.00";
            timerTMP.fontSize = 2.2f;
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.color = new Color(1f, 0.45f, 0.15f);

            // Código para 3 Dígitos: "_ _ _"
            GameObject codeObj = new GameObject("Code_TMP");
            codeObj.transform.SetParent(bezel.transform, false);
            codeObj.transform.localPosition = new Vector3(0f, 0.52f, -0.025f);
            codeObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro codeTMP = codeObj.AddComponent<TextMeshPro>();
            codeTMP.text = "_ _ _";
            codeTMP.fontSize = 1.8f;
            codeTMP.alignment = TextAlignmentOptions.Center;
            codeTMP.color = Color.cyan;

            // Estado de la bomba
            GameObject statusObj = new GameObject("Status_TMP");
            statusObj.transform.SetParent(bezel.transform, false);
            statusObj.transform.localPosition = new Vector3(0f, 0.52f, 0.036f);
            statusObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro statusTMP = statusObj.AddComponent<TextMeshPro>();
            statusTMP.text = "DISPOSITIVO ARMADO (3 DÍGITOS)";
            statusTMP.fontSize = 0.80f;
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = Color.yellow;

            // LEDs
            GameObject redLed = new GameObject("LED_Active_Red");
            redLed.transform.SetParent(bombRoot.transform, false);
            redLed.transform.localPosition = new Vector3(0.03f, 0.13f, 0.11f);
            Light rLight = redLed.AddComponent<Light>();
            rLight.type = LightType.Point;
            rLight.color = Color.red;
            rLight.intensity = 1.0f;
            rLight.range = 0.7f;
            CreateBoxPrimitive(redLed, "Red_Bulb", Vector3.zero, new Vector3(0.012f, 0.012f, 0.012f), matC4);

            GameObject grnLed = new GameObject("LED_Defused_Green");
            grnLed.transform.SetParent(bombRoot.transform, false);
            grnLed.transform.localPosition = new Vector3(0.19f, 0.13f, 0.11f);
            Light gLight = grnLed.AddComponent<Light>();
            gLight.type = LightType.Point;
            gLight.color = Color.green;
            gLight.intensity = 1.4f;
            gLight.range = 0.7f;
            gLight.enabled = false;
            CreateBoxPrimitive(grnLed, "Grn_Bulb", Vector3.zero, new Vector3(0.012f, 0.012f, 0.012f), matPCB);

            // Teclado 3x4 Numérico
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer < 0) interactableLayer = 0;

            GameObject keypadRoot = new GameObject("Keypad");
            keypadRoot.transform.SetParent(bombRoot.transform, false);
            keypadRoot.transform.localPosition = new Vector3(0.11f, 0.11f, -0.045f);
            keypadRoot.layer = interactableLayer;

            string[] keys = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "ENT" };
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    string k = keys[r * 3 + c];
                    float bx = (c - 1) * 0.042f;
                    float bz = (1.5f - r) * 0.036f;
                    GameObject btn = CreateBoxPrimitive(keypadRoot, $"Btn_{k}", new Vector3(bx, 0.012f, bz), new Vector3(0.034f, 0.016f, 0.028f), matButton);
                    btn.layer = interactableLayer;

                    // Etiqueta del botón
                    GameObject lbl = new GameObject("Key_Text");
                    lbl.transform.SetParent(btn.transform, false);
                    lbl.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                    lbl.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    TextMeshPro bTMP = lbl.AddComponent<TextMeshPro>();
                    bTMP.text = k;
                    bTMP.fontSize = 1.2f;
                    bTMP.alignment = TextAlignmentOptions.Center;
                    bTMP.color = (k == "ENT") ? Color.green : ((k == "C") ? Color.red : Color.white);

                    btn.AddComponent<BombKeypadButton>().keyValue = k;
                }
            }

            // Audio sintetizado
            bombRoot.AddComponent<AudioSource>();
            BombAudioSynthesizer audio = bombRoot.AddComponent<BombAudioSynthesizer>();

            // Controlador de la Bomba
            BombController ctrl = bombRoot.AddComponent<BombController>();
            ctrl.config = AssetDatabase.LoadAssetAtPath<BombConfigData>($"{GAME_DATA_PATH}/Room2_BombConfig.asset");
            ctrl.timerText = timerTMP;
            ctrl.codeText = codeTMP;
            ctrl.statusText = statusTMP;
            ctrl.activeRedLight = rLight;
            ctrl.defusedGreenLight = gLight;
            ctrl.audioSynth = audio;

            return bombRoot;
        }
        #endregion

        #region Escape Notes (XRGrabInteractable)
        private static List<VRNoteInteractable> PlaceEscapeNotes(GameObject root)
        {
            List<VRNoteInteractable> notes = new List<VRNoteInteractable>();
            GameObject notesRoot = new GameObject("EscapeRoom_Notes");
            notesRoot.transform.SetParent(root.transform, false);

            Material matPaper = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_PaperNote.mat");

            // =========================================================================
            // NOTA 1 (DÍGITO 7 - Posición 1): Oculta en el Mueblesito (estante de madera)
            // =========================================================================
            ClueData c1 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_01.asset");
            VRNoteInteractable n1 = CreateInteractiveVRNote(
                notesRoot,
                "Note_1_Mueble",
                new Vector3(-2.85f, 1.22f, 1.25f),
                Quaternion.Euler(0f, 45f, 0f),
                c1,
                matPaper
            );
            notes.Add(n1);

            // =========================================================================
            // NOTA 2 (DÍGITO 3 - Posición 2): Oculta cerca del Retrato / Cabecera Cama
            // =========================================================================
            ClueData c2 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_02.asset");
            VRNoteInteractable n2 = CreateInteractiveVRNote(
                notesRoot,
                "Note_2_Retrato",
                new Vector3(-3.50f, 1.75f, -1.55f),
                Quaternion.Euler(0f, 90f, 0f),
                c2,
                matPaper
            );
            notes.Add(n2);

            // =========================================================================
            // NOTA 3 (DÍGITO 9 - Posición 3): Oculta en la Pila de Libros en la mesa
            // =========================================================================
            ClueData c3 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Room2_Clue_03.asset");
            VRNoteInteractable n3 = CreateInteractiveVRNote(
                notesRoot,
                "Note_3_Libros",
                new Vector3(-0.35f, 0.95f, 0.45f),
                Quaternion.Euler(0f, 15f, 0f),
                c3,
                matPaper
            );
            notes.Add(n3);

            return notes;
        }

        private static VRNoteInteractable CreateInteractiveVRNote(GameObject parent, string name, Vector3 pos, Quaternion rot, ClueData clue, Material mat)
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer < 0) interactableLayer = 0;

            GameObject noteObj = new GameObject(name);
            noteObj.transform.SetParent(parent.transform, false);
            noteObj.transform.localPosition = pos;
            noteObj.transform.localRotation = rot;
            noteObj.layer = interactableLayer;

            // Malla 3D de la nota (hoja de papel)
            GameObject visual = CreateBoxPrimitive(noteObj, "Note_Mesh", Vector3.zero, new Vector3(0.20f, 0.005f, 0.28f), mat, false);
            visual.layer = interactableLayer;

            // Colisionador para agarrar en VR
            BoxCollider col = noteObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.22f, 0.05f, 0.30f);

            // Rigidbody cinemático / físico para XR Grab y Antigravity
            Rigidbody rb = noteObj.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            rb.useGravity = false;
            rb.linearDamping = 1.2f;
            rb.angularDamping = 1.0f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Texto sobre la nota física
            GameObject textObj = new GameObject("Surface_Text");
            textObj.transform.SetParent(noteObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            textObj.transform.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);

            TextMeshPro surfaceTMP = textObj.AddComponent<TextMeshPro>();
            surfaceTMP.fontSize = 3.5f;
            surfaceTMP.alignment = TextAlignmentOptions.Center;
            surfaceTMP.color = new Color(0.12f, 0.10f, 0.08f);
            surfaceTMP.enableWordWrapping = true;
            RectTransform srt = surfaceTMP.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(52f, 75f);

            // Popup flotante para lectura clara a distancia en VR
            GameObject popupRoot = new GameObject("Inspection_Popup");
            popupRoot.transform.SetParent(noteObj.transform, false);
            popupRoot.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            popupRoot.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

            // Fondo semitransparente oscuro
            GameObject popupBg = CreateBoxPrimitive(popupRoot, "Popup_BG", Vector3.zero, new Vector3(60f, 42f, 0.5f), null, false);
            Renderer bgRend = popupBg.GetComponent<Renderer>();
            bgRend.material.color = new Color(0.10f, 0.12f, 0.15f, 0.95f);

            // Título popup
            GameObject pTitleObj = new GameObject("Popup_Title");
            pTitleObj.transform.SetParent(popupRoot.transform, false);
            pTitleObj.transform.localPosition = new Vector3(0f, 14f, -0.6f);
            TextMeshPro pTitle = pTitleObj.AddComponent<TextMeshPro>();
            pTitle.fontSize = 5.0f;
            pTitle.fontStyle = FontStyles.Bold;
            pTitle.alignment = TextAlignmentOptions.Center;
            pTitle.color = new Color(1f, 0.85f, 0.4f);

            // Cuerpo popup
            GameObject pBodyObj = new GameObject("Popup_Body");
            pBodyObj.transform.SetParent(popupRoot.transform, false);
            pBodyObj.transform.localPosition = new Vector3(0f, 2f, -0.6f);
            TextMeshPro pBody = pBodyObj.AddComponent<TextMeshPro>();
            pBody.fontSize = 3.8f;
            pBody.alignment = TextAlignmentOptions.Center;
            pBody.color = Color.white;
            pBody.enableWordWrapping = true;
            pBody.GetComponent<RectTransform>().sizeDelta = new Vector2(54f, 20f);

            // Dígito destacado
            GameObject pDigitObj = new GameObject("Popup_Digit");
            pDigitObj.transform.SetParent(popupRoot.transform, false);
            pDigitObj.transform.localPosition = new Vector3(0f, -12f, -0.6f);
            TextMeshPro pDigit = pDigitObj.AddComponent<TextMeshPro>();
            pDigit.fontSize = 5.2f;
            pDigit.fontStyle = FontStyles.Bold;
            pDigit.alignment = TextAlignmentOptions.Center;
            pDigit.color = Color.yellow;

            // Componente de interacción VR
            VRNoteInteractable vrNote = noteObj.AddComponent<VRNoteInteractable>();
            vrNote.clueData = clue;
            vrNote.inWorldNoteText = surfaceTMP;
            vrNote.inspectionPopup = popupRoot;
            vrNote.popupTitle = pTitle;
            vrNote.popupBody = pBody;
            vrNote.popupDigitHint = pDigit;
            // Componente Antigravity Grab Interactable (gravedad cero y amortiguación inercial)
            AntigravityGrabInteractable agGrab = noteObj.AddComponent<AntigravityGrabInteractable>();
            agGrab.vrNoteComponent = vrNote;
            agGrab.floatInZeroGravity = true;

            // Compatibilidad con ClueInteractable para raycast en PC
            ClueInteractable ci = noteObj.AddComponent<ClueInteractable>();
            ci.clueData = clue;
            ci.inspectionCardPopup = popupRoot;
            ci.cardTitleText = pTitle;
            ci.cardBodyText = pBody;
            ci.cardDatoText = pDigit;

            return vrNote;
        }
        #endregion

        #region Mission Board & Systems
        private static TextMeshPro BuildMissionBoard(GameObject root)
        {
            GameObject board = new GameObject("EscapeRoom_MissionBoard");
            board.transform.SetParent(root.transform, false);
            // Montada en la pared Norte junto a la Pizarra de Meshy AI
            board.transform.localPosition = new Vector3(0f, 2.6f, 3.50f);
            board.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            board.transform.localScale = new Vector3(0.007f, 0.007f, 0.007f);

            TextMeshPro tmp = board.AddComponent<TextMeshPro>();
            tmp.fontSize = 4.2f;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            RectTransform rt = tmp.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 180f);

            return tmp;
        }

        #region Save Terminal 3D
        private static AntigravitySaveTerminal BuildSaveTerminal(GameObject root)
        {
            Material matCase = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_TacticalCase.mat");
            Material matButton = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_KeypadButton.mat");

            int archLayer = LayerMask.NameToLayer("RoomArchitecture");
            if (archLayer < 0) archLayer = 0;
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer < 0) interactableLayer = 0;

            GameObject terminalRoot = new GameObject("Save_Terminal_Console");
            terminalRoot.transform.SetParent(root.transform, false);
            terminalRoot.transform.localPosition = new Vector3(-1.85f, 1.55f, 3.65f);
            terminalRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            terminalRoot.layer = archLayer;

            // 1. Chasis del Terminal (Gabinete de control)
            GameObject chassis = CreateBoxPrimitive(terminalRoot, "Terminal_Chassis", Vector3.zero, new Vector3(0.90f, 0.70f, 0.08f), matCase);
            chassis.layer = archLayer;

            // Marco de la pantalla
            GameObject screenFrame = CreateBoxPrimitive(terminalRoot, "Screen_Bezel", new Vector3(0f, 0.07f, -0.035f), new Vector3(0.82f, 0.44f, 0.02f), matCase);
            screenFrame.layer = archLayer;

            // Pantalla LCD
            GameObject screenObj = CreateBoxPrimitive(terminalRoot, "Screen_Glass", new Vector3(0f, 0.07f, -0.046f), new Vector3(0.80f, 0.42f, 0.01f), null, false, true);
            Renderer screenRend = screenObj.GetComponent<Renderer>();
            screenRend.material.color = new Color(0.04f, 0.07f, 0.10f, 0.95f);
            screenObj.layer = archLayer;

            // Luz indicadora de estado
            GameObject ledObj = new GameObject("Status_Indicator_LED");
            ledObj.transform.SetParent(terminalRoot.transform, false);
            ledObj.transform.localPosition = new Vector3(0.35f, 0.28f, -0.06f);
            Light terminalLight = ledObj.AddComponent<Light>();
            terminalLight.type = LightType.Point;
            terminalLight.color = new Color(0.2f, 0.8f, 1.0f);
            terminalLight.intensity = 1.0f;
            terminalLight.range = 1.5f;

            // Textos en el World-Space Canvas
            // Header
            GameObject headerObj = new GameObject("Header_TMP");
            headerObj.transform.SetParent(terminalRoot.transform, false);
            headerObj.transform.localPosition = new Vector3(0f, 0.24f, -0.055f);
            headerObj.transform.localRotation = Quaternion.identity;
            headerObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            TextMeshPro headerTMP = headerObj.AddComponent<TextMeshPro>();
            headerTMP.fontSize = 2.4f;
            headerTMP.alignment = TextAlignmentOptions.Center;
            headerTMP.color = new Color(0.2f, 0.8f, 1.0f);
            headerTMP.text = "<b>TERMINAL DE GUARDADO // ANTIGRAVITY OS</b>";
            headerTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 15f);

            // Status Text (Cambia a verde en confirmación de guardado)
            GameObject statusObj = new GameObject("Status_TMP");
            statusObj.transform.SetParent(terminalRoot.transform, false);
            statusObj.transform.localPosition = new Vector3(0f, 0.14f, -0.055f);
            statusObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            TextMeshPro statusTMP = statusObj.AddComponent<TextMeshPro>();
            statusTMP.fontSize = 2.1f;
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.color = new Color(0.2f, 0.8f, 1.0f);
            statusTMP.text = "SISTEMA OPERATIVO // EN ESPERA";
            statusTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 15f);

            // Details Text
            GameObject detailsObj = new GameObject("Details_TMP");
            detailsObj.transform.SetParent(terminalRoot.transform, false);
            detailsObj.transform.localPosition = new Vector3(0f, -0.02f, -0.055f);
            detailsObj.transform.localScale = new Vector3(0.009f, 0.009f, 0.009f);
            TextMeshPro detailsTMP = detailsObj.AddComponent<TextMeshPro>();
            detailsTMP.fontSize = 1.8f;
            detailsTMP.alignment = TextAlignmentOptions.Center;
            detailsTMP.color = new Color(0.85f, 0.9f, 0.95f);
            detailsTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(85f, 35f);

            // 3. Botón 3D Físico "Guardar Partida"
            GameObject btnBracket = CreateBoxPrimitive(terminalRoot, "Button_Bracket", new Vector3(0f, -0.22f, -0.04f), new Vector3(0.42f, 0.11f, 0.04f), matCase);
            btnBracket.layer = archLayer;

            // Émbolo / Botón hundible
            GameObject btnPlunger = CreateBoxPrimitive(terminalRoot, "Button_Plunger", new Vector3(0f, -0.22f, -0.065f), new Vector3(0.38f, 0.085f, 0.035f), matButton);
            btnPlunger.layer = interactableLayer;

            // Etiqueta del botón
            GameObject btnLabel = new GameObject("Button_Label_TMP");
            btnLabel.transform.SetParent(btnPlunger.transform, false);
            btnLabel.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            btnLabel.transform.localRotation = Quaternion.identity;
            btnLabel.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
            TextMeshPro btnTMP = btnLabel.AddComponent<TextMeshPro>();
            btnTMP.fontSize = 4.2f;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.color = Color.white;
            btnTMP.text = "GUARDAR PARTIDA";
            btnTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(25f, 6f);

            // Componente del botón físico
            AntigravityPhysicalButton physBtn = btnPlunger.AddComponent<AntigravityPhysicalButton>();
            physBtn.pressDirection = new Vector3(0f, 0f, 1f); // Se hunde hacia el interior de la pared
            physBtn.pressDistance = 0.02f;
            physBtn.buttonRenderer = btnPlunger.GetComponent<Renderer>();
            physBtn.idleColor = new Color(0.12f, 0.55f, 0.95f);
            physBtn.pressedColor = new Color(0.0f, 1.0f, 0.4f);

            // AudioSource para el terminal
            AudioSource termAudio = terminalRoot.AddComponent<AudioSource>();
            termAudio.spatialBlend = 1.0f;
            termAudio.playOnAwake = false;

            // Componente maestro del Terminal de Guardado
            AntigravitySaveTerminal termCtrl = terminalRoot.AddComponent<AntigravitySaveTerminal>();
            termCtrl.headerText = headerTMP;
            termCtrl.statusText = statusTMP;
            termCtrl.detailsText = detailsTMP;
            termCtrl.terminalIndicatorLight = terminalLight;
            termCtrl.savePhysicalButton = physBtn;
            termCtrl.audioSource = termAudio;

            return termCtrl;
        }
        #endregion

        private static void SetupSystems(GameObject root, BombController bomb, List<VRNoteInteractable> notes, TextMeshPro boardTMP)
        {
            GameObject sys = new GameObject("Systems_Room2");
            sys.transform.SetParent(root.transform, false);

            // 1. Gestor Modular de Guardado / Carga en JSON
            GameSaveManager saveMgr = sys.AddComponent<GameSaveManager>();
            saveMgr.bombController = bomb;
            saveMgr.autoLoadOnStart = false;
            saveMgr.autoSaveOnMilestones = true;

            // 2. Escape Room Level Manager
            Room2EscapeRoomManager roomMgr = sys.AddComponent<Room2EscapeRoomManager>();
            roomMgr.bombController = bomb;
            roomMgr.roomNotes = notes;
            roomMgr.missionBoardText = boardTMP;

            Light mainLight = root.GetComponentInChildren<Light>();
            if (mainLight != null) roomMgr.roomMainLight = mainLight;

            roomMgr.UpdateMissionBoard();
        }
        #endregion

        #region Lighting
        private static void SetupLighting(GameObject root)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.36f, 0.44f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.23f, 0.20f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.10f, 0.09f);

            GameObject lights = new GameObject("Lighting_Rig");
            lights.transform.SetParent(root.transform, false);

            // Luz cenital suave sobre la habitación
            GameObject mainRoomLight = new GameObject("Room_MainLight");
            mainRoomLight.transform.SetParent(lights.transform, false);
            mainRoomLight.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            Light l = mainRoomLight.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.88f, 0.72f);
            l.intensity = 1.1f;
            l.range = 8.5f;
            l.shadows = LightShadows.Soft;

            // Foco puntual directo sobre la mesa de la bomba
            GameObject bombSpot = new GameObject("Bomb_Table_Spotlight");
            bombSpot.transform.SetParent(lights.transform, false);
            bombSpot.transform.localPosition = new Vector3(0f, 2.8f, 0.25f);
            bombSpot.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            Light s = bombSpot.AddComponent<Light>();
            s.type = LightType.Spot;
            s.spotAngle = 65f;
            s.innerSpotAngle = 45f;
            s.color = new Color(1.0f, 0.95f, 0.85f);
            s.intensity = 1.6f;
            s.range = 4.5f;
            s.shadows = LightShadows.Soft;

            // Luz ambiental tenue en el mueble aparador
            GameObject hutchLight = new GameObject("Hutch_Light");
            hutchLight.transform.SetParent(lights.transform, false);
            hutchLight.transform.localPosition = new Vector3(-2.8f, 2.2f, 1.2f);
            Light hl = hutchLight.AddComponent<Light>();
            hl.type = LightType.Point;
            hl.color = new Color(0.95f, 0.82f, 0.65f);
            hl.intensity = 0.6f;
            hl.range = 4.0f;
        }
        #endregion

        #region VR Rig & Player Setup
        private static void SetupVRAndPlayerRig(GameObject root)
        {
            // 1. XR Interaction Manager
            GameObject xriMgrObj = new GameObject("XR Interaction Manager");
            xriMgrObj.AddComponent<XRInteractionManager>();

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0) playerLayer = 0;

            // 2. Intentar instanciar el prefab de XR Origin del proyecto
            string xrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            GameObject xrOriginPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrOriginPrefabPath);

            GameObject xrRigInstance = null;
            if (xrOriginPrefab != null)
            {
                xrRigInstance = (GameObject)PrefabUtility.InstantiatePrefab(xrOriginPrefab);
                xrRigInstance.name = "XR Origin (XR Rig)";
                xrRigInstance.transform.position = new Vector3(0f, 0.05f, -1.25f); // 1.5m frente a la mesa de la bomba
                xrRigInstance.transform.rotation = Quaternion.identity;
                xrRigInstance.layer = playerLayer;

                // CharacterController para el XR Rig si no lo tiene
                CharacterController xrCC = xrRigInstance.GetComponent<CharacterController>();
                if (xrCC == null)
                {
                    xrCC = xrRigInstance.AddComponent<CharacterController>();
                    xrCC.height = 1.8f;
                    xrCC.radius = 0.35f;
                    xrCC.center = new Vector3(0f, 0.9f, 0f);
                }

                // Componente Antigravity Player Controller en XR Rig
                AntigravityPlayerController agCtrl = xrRigInstance.GetComponent<AntigravityPlayerController>();
                if (agCtrl == null) agCtrl = xrRigInstance.AddComponent<AntigravityPlayerController>();
                Camera xrCam = xrRigInstance.GetComponentInChildren<Camera>();
                if (xrCam != null) agCtrl.headTransform = xrCam.transform;

                // Físicas de manos Antigravity en mandos VR
                SetupHandPhysicsOnXRRig(xrRigInstance, agCtrl);
            }

            // 3. Controlador de pruebas PC Fallback con soporte Antigravity e Input System seguro
            GameObject pcFallback = new GameObject("Player_PC_TestingFallback");
            pcFallback.transform.position = new Vector3(0f, 0.05f, -1.25f);
            pcFallback.layer = playerLayer;

            CharacterController cc = pcFallback.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.875f, 0f);

            GameObject camObj = new GameObject("Fallback_Camera");
            camObj.transform.SetParent(pcFallback.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camObj.tag = "MainCamera";

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            camObj.AddComponent<AudioListener>();

            // Antigravity Locomotion en PC
            AntigravityPlayerController pcAg = pcFallback.AddComponent<AntigravityPlayerController>();
            pcAg.headTransform = camObj.transform;

            // Script de fallback para PC libre de excepciones de Input
            Player_PC_TestingFallback pcScript = pcFallback.AddComponent<Player_PC_TestingFallback>();
            pcScript.playerCamera = cam;

            // Mano virtual / sensor táctil para PC
            GameObject pcHand = new GameObject("PC_VirtualHand");
            pcHand.transform.SetParent(camObj.transform, false);
            pcHand.transform.localPosition = new Vector3(0f, 0f, 0.8f);
            SphereCollider sc = pcHand.AddComponent<SphereCollider>();
            sc.radius = 0.18f;
            sc.isTrigger = true;
            AntigravityHandPhysics hp = pcHand.AddComponent<AntigravityHandPhysics>();
            hp.playerController = pcAg;

            // Si se instanció el XR Rig, delegar según presencia de visor VR
            if (xrRigInstance != null)
            {
                pcScript.CheckAndConfigureVRFallback();
            }
        }

        private static void SetupHandPhysicsOnXRRig(GameObject xrRig, AntigravityPlayerController playerCtrl)
        {
            int handsLayer = LayerMask.NameToLayer("Hands");
            if (handsLayer < 0) handsLayer = 0;

            foreach (Transform t in xrRig.GetComponentsInChildren<Transform>(true))
            {
                bool isLeft = t.name.Contains("Left Controller") || t.name.Contains("LeftHand") || t.name.Contains("Left Hand");
                bool isRight = t.name.Contains("Right Controller") || t.name.Contains("RightHand") || t.name.Contains("Right Hand");

                if (isLeft || isRight)
                {
                    t.gameObject.layer = handsLayer;
                    SphereCollider col = t.GetComponent<SphereCollider>();
                    if (col == null)
                    {
                        col = t.gameObject.AddComponent<SphereCollider>();
                        col.radius = 0.08f;
                        col.isTrigger = true;
                    }

                    AntigravityHandPhysics hp = t.GetComponent<AntigravityHandPhysics>();
                    if (hp == null) hp = t.gameObject.AddComponent<AntigravityHandPhysics>();
                    hp.handType = isLeft ? AntigravityHandPhysics.HandType.Left : AntigravityHandPhysics.HandType.Right;
                    hp.playerController = playerCtrl;
                }
            }
        }
        #endregion

        #region Helpers & Build Settings
        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return; // Ya registrada
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[Room2SceneBuilder] Escena '{scenePath}' registrada en EditorBuildSettings.");
        }

        private static GameObject CreateBoxPrimitive(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat, bool withCollider = true, bool withRenderer = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = pos;
            box.transform.localScale = size;

            if (mat != null && withRenderer)
            {
                box.GetComponent<Renderer>().sharedMaterial = mat;
            }
            else if (!withRenderer)
            {
                Renderer r = box.GetComponent<Renderer>();
                if (r != null) r.enabled = false;
            }

            if (!withCollider)
            {
                Collider c = box.GetComponent<Collider>();
                if (c != null) UnityEngine.Object.DestroyImmediate(c);
            }

            return box;
        }
        #endregion
    }
}
