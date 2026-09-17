using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using TMPro;
using DefusalGame.Data;
using DefusalGame.Bomb;
using DefusalGame.Gameplay;
using StylizedRoom;

namespace DefusalGame.Editor
{
    public class ScenarioAndBombBuilder : EditorWindow
    {
        private const string GAME_DATA_PATH = "Assets/Game/Data";
        private const string MAT_PATH = "Assets/StylizedRoom/Materials";
        private const string PROPS_PATH = "Assets/StylizedRoom/Props";
        private const string PREFAB_PATH = "Assets/Game/Prefabs";

        [MenuItem("Desactiva la Bomba/Generar Escenario y Bomba Completa", false, 1)]
        public static void BuildCompleteGame()
        {
            CleanAllOldObjects();
            EnsureDataAssets();
            SetupGameMaterials();

            GameObject scenarioRoot = BuildArchitecture();
            BuildTacticalBombAndDossier(scenarioRoot);
            PlaceCluesInRooms(scenarioRoot);
            SetupLighting(scenarioRoot);
            SetupPlayer(scenarioRoot);

            Directory.CreateDirectory(PREFAB_PATH);
            string prefabFile = $"{PREFAB_PATH}/DefusalGame_Scenario.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(scenarioRoot, prefabFile, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = scenarioRoot;
            SceneView.FrameLastActiveSceneView();

            Debug.Log("[DesactivaLaBomba] ¡Escenario conectado, bomba táctica y hoja de misión generados con éxito!");
            EditorUtility.DisplayDialog(
                "¡Escenario y Misión Listos!",
                "La escena ha sido construida con arquitectura limpia:\n\n" +
                "• SALA DE LA BOMBA: En la mesa tienes la BOMBA ACTIVA y la HOJA DE INSTRUCCIONES.\n" +
                "• PASILLO CONECTOR: Conecta fluidamente las tres habitaciones.\n" +
                "• EL ESTUDIO: Con escritorio de cajones (Pista 1) y pizarra de corcho (Pista 2).\n" +
                "• EL ARCHIVO: Con estanterías de libros (Pista 3) y caja de herramientas (Pista 4).\n\n" +
                "¡Dale al botón PLAY para comenzar la investigación con WASD!",
                "¡Empezar!"
            );
        }

        private static void CleanAllOldObjects()
        {
            string[] names = new string[] {
                "DefusalGame_Scenario",
                "Stylized_HauntedRoom",
                "StylizedHauntedRoom_FPV",
                "Stylized_Room",
                "Room",
                "Player_FPV",
                "Executive_Desk",
                "High_Archive_Shelves",
                "Investigation_Corkboard",
                "Chamber_Table",
                "Chamber_Chair",
                "Environment_Rooms",
                "Lighting_Rig",
                "Tactical_C4_Bomb",
                "Mission_Dossier_Sheet",
                // Eliminar objetos de la plantilla VR que tienen BoxColliders gigantescos bloqueando el paso:
                "Environment",
                "Interactables",
                "Teleport Area Setup",
                "UI",
                "Grid",
                "Table"
            };

            foreach (string name in names)
            {
                GameObject obj = GameObject.Find(name);
                while (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                    obj = GameObject.Find(name);
                }
            }

            foreach (var fpc in Object.FindObjectsByType<FirstPersonController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (fpc != null && fpc.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(fpc.gameObject);
                }
            }

            foreach (var bc in Object.FindObjectsByType<BombController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (bc != null && bc.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(bc.gameObject);
                }
            }
        }

        private static void EnsureDataAssets()
        {
            Directory.CreateDirectory(GAME_DATA_PATH);

            ClueData c1 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_01_DeskDrawer.asset");
            if (c1 == null)
            {
                c1 = ScriptableObject.CreateInstance<ClueData>();
                c1.clueId = "CLUE_DESK_DRAWER";
                c1.clueName = "Nota rasgada del cajón";
                c1.description = "Fragmento de documento secreto. Indica con tinta roja: 'DÍGITO INICIAL: 4'.";
                c1.revealedValue = "4";
                c1.sequenceIndex = 0;
                c1.hintLocation = "Escritorio del Estudio (Cajonera)";
                AssetDatabase.CreateAsset(c1, $"{GAME_DATA_PATH}/Clue_01_DeskDrawer.asset");
            }

            ClueData c2 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_02_Corkboard.asset");
            if (c2 == null)
            {
                c2 = ScriptableObject.CreateInstance<ClueData>();
                c2.clueId = "CLUE_CORKBOARD";
                c2.clueName = "Diagrama en la pizarra";
                c2.description = "Esquema fijado con chinchetas. Destaca un 8 en la segunda posición del bus de datos.";
                c2.revealedValue = "8";
                c2.sequenceIndex = 1;
                c2.hintLocation = "Pizarra de corcho del Estudio";
                AssetDatabase.CreateAsset(c2, $"{GAME_DATA_PATH}/Clue_02_Corkboard.asset");
            }

            ClueData c3 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_03_Bookshelf.asset");
            if (c3 == null)
            {
                c3 = ScriptableObject.CreateInstance<ClueData>();
                c3.clueId = "CLUE_BOOKSHELF";
                c3.clueName = "Cifra en lomo de libro";
                c3.description = "Libro encuadernado en piel oscura. Entre sus hojas hay un sello con el número 2 para la 3ra casilla.";
                c3.revealedValue = "2";
                c3.sequenceIndex = 2;
                c3.hintLocation = "Estantería de la Biblioteca / Archivo";
                AssetDatabase.CreateAsset(c3, $"{GAME_DATA_PATH}/Clue_03_Bookshelf.asset");
            }

            ClueData c4 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_04_Toolbox.asset");
            if (c4 == null)
            {
                c4 = ScriptableObject.CreateInstance<ClueData>();
                c4.clueId = "CLUE_TOOLBOX";
                c4.clueName = "Etiqueta en caja metálica";
                c4.description = "Placa metálica grabada en la caja de herramientas con la clave final: 'TERMINADOR: 6'.";
                c4.revealedValue = "6";
                c4.sequenceIndex = 3;
                c4.hintLocation = "Caja de herramientas en el suelo del Archivo";
                AssetDatabase.CreateAsset(c4, $"{GAME_DATA_PATH}/Clue_04_Toolbox.asset");
            }

            BombConfigData cfg = AssetDatabase.LoadAssetAtPath<BombConfigData>($"{GAME_DATA_PATH}/DefaultBombConfig.asset");
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<BombConfigData>();
                cfg.bombId = "BOMB_CHAMBER_01";
                cfg.bombName = "Dispositivo C4 Táctico";
                cfg.timeLimitSeconds = 300f;
                cfg.penaltySecondsOnFail = 30f;
                cfg.targetSequence = "4826";
                cfg.requiredCluesCount = 4;
                cfg.linkedClues = new List<ClueData>() { c1, c2, c3, c4 };
                AssetDatabase.CreateAsset(cfg, $"{GAME_DATA_PATH}/DefaultBombConfig.asset");
            }

            AssetDatabase.SaveAssets();
        }

        private static void SetupGameMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            GetOrCreateMat("Mat_TacticalCase", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.12f, 0.13f, 0.15f));
                m.SetFloat("_Smoothness", 0.45f);
                m.SetFloat("_Metallic", 0.3f);
            });

            GetOrCreateMat("Mat_KeypadButton", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.25f, 0.26f, 0.28f));
                m.SetFloat("_Smoothness", 0.25f);
                m.SetFloat("_Metallic", 0.1f);
            });

            GetOrCreateMat("Mat_C4Explosive", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.70f, 0.22f, 0.18f));
                m.SetFloat("_Smoothness", 0.2f);
            });

            GetOrCreateMat("Mat_CircuitBoard", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.10f, 0.35f, 0.18f));
                m.SetFloat("_Smoothness", 0.6f);
            });

            GetOrCreateMat("Mat_Corkboard", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.72f, 0.54f, 0.36f));
                m.SetFloat("_Smoothness", 0.05f);
            });

            GetOrCreateMat("Mat_PaperNote", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.95f, 0.92f, 0.82f));
                m.SetFloat("_Smoothness", 0.05f);
            });

            GetOrCreateMat("Mat_ClipboardWood", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.35f, 0.22f, 0.15f));
                m.SetFloat("_Smoothness", 0.3f);
            });

            GetOrCreateMat("Mat_MetalShelf", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.28f, 0.30f, 0.32f));
                m.SetFloat("_Smoothness", 0.5f);
                m.SetFloat("_Metallic", 0.8f);
            });

            GetOrCreateMat("Mat_WallStudy", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.68f, 0.65f, 0.58f)); // Beige estudio
                m.SetFloat("_Smoothness", 0.15f);
            });

            GetOrCreateMat("Mat_WallArchive", litShader, m => {
                m.SetColor("_BaseColor", new Color(0.55f, 0.52f, 0.48f)); // Archivo piedra/cemento
                m.SetFloat("_Smoothness", 0.12f);
            });

            AssetDatabase.SaveAssets();
        }

        private static Material GetOrCreateMat(string name, Shader shader, System.Action<Material> cfg)
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

        private static GameObject BuildArchitecture()
        {
            GameObject old = GameObject.Find("DefusalGame_Scenario");
            if (old != null) Undo.DestroyObjectImmediate(old);

            GameObject root = new GameObject("DefusalGame_Scenario");
            Undo.RegisterCreatedObjectUndo(root, "Create Defusal Scenario");

            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_Floor.mat");
            Material matLeftWall = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_LeftWall.mat");
            Material matRightWall = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_RightWall.mat");
            Material matTrim = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_WoodTrim.mat");
            Material matWallStudy = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_WallStudy.mat");
            Material matWallArchive = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_WallArchive.mat");
            Material matCork = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_Corkboard.mat");
            Material matShelf = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_MetalShelf.mat");

            GameObject envRoot = new GameObject("Environment_Rooms");
            envRoot.transform.SetParent(root.transform, false);

            // =========================================================================
            // SUELO MAESTRO UNIFICADO CON COLISIONADOR CONTINUO
            // (Elimina 100% las micro-costuras entre cuartos para que el CharacterController nunca se trabe)
            // =========================================================================
            GameObject masterFloor = CreateBoxPrimitive(envRoot, "Master_Floor_Collider", new Vector3(-3.65f, -0.02f, 1.25f), new Vector3(12.6f, 0.04f, 7.5f), null, true);
            masterFloor.GetComponent<Renderer>().enabled = false;

            // ==========================================
            // 1. SALA DE LA BOMBA (X: -2.0 a 2.0, Z: -2.0 a 2.0, Y: 0 a 2.8m)
            // ==========================================
            GameObject chamber = new GameObject("Room1_BombChamber");
            chamber.transform.SetParent(envRoot.transform, false);

            CreateFloor(chamber, "Floor_Chamber", new Vector3(0, 0, 0), new Vector2(4.0f, 4.0f), matFloor);
            CreateCeiling(chamber, "Ceiling_Chamber", new Vector3(0, 2.8f, 0), new Vector2(4.0f, 4.0f), matTrim);

            // Pared Norte (Ventana iluminada por la luna)
            CreateWall(chamber, "Wall_North_Window", new Vector3(0, 1.4f, 2.0f), new Vector3(4.0f, 2.8f, 0.1f), matRightWall);
            // Pared Sur
            CreateWall(chamber, "Wall_South", new Vector3(0, 1.4f, -2.0f), new Vector3(4.0f, 2.8f, 0.1f), matRightWall);
            // Pared Este (Sólida)
            CreateWall(chamber, "Wall_East", new Vector3(2.0f, 1.4f, 0), new Vector3(0.1f, 2.8f, 4.0f), matLeftWall);

            // Pared Oeste: PUERTA AMPLIA ABIERTA HACIA EL PASILLO (Z [-0.7, 0.7], Ancho 1.4m)
            CreateWall(chamber, "Wall_West_North", new Vector3(-2.0f, 1.4f, 1.35f), new Vector3(0.1f, 2.8f, 1.30f), matLeftWall);
            CreateWall(chamber, "Wall_West_South", new Vector3(-2.0f, 1.4f, -1.35f), new Vector3(0.1f, 2.8f, 1.30f), matLeftWall);
            CreateWall(chamber, "Wall_West_Lintel", new Vector3(-2.0f, 2.55f, 0f), new Vector3(0.1f, 0.5f, 1.40f), matTrim);

            // Marco decorativo de madera en la puerta (sin colisionador)
            CreateDoorFrame(chamber, "Doorframe_ChamberToCorridor", new Vector3(-2.0f, 0f, 0f), false, matTrim);

            // Puerta visual entreabierta hacia el pasillo (sin colisionador para paso libre)
            GameObject doorAjar = CreateBoxPrimitive(chamber, "Door_Ajar_Visual", new Vector3(-2.06f, 1.10f, 0.68f), new Vector3(0.04f, 2.20f, 0.90f), matTrim, false);
            doorAjar.transform.localRotation = Quaternion.Euler(0f, 75f, 0f);

            // Mobiliario en la Sala 1
            SpawnChamberFurniture(chamber);

            // ==========================================
            // 2. PASILLO CENTRAL (X: -2.0 a -5.5m, Z: -1.1 a 1.1m, Y: 0 a 2.8m)
            // ==========================================
            GameObject corridor = new GameObject("Corridor_Central");
            corridor.transform.SetParent(envRoot.transform, false);

            CreateFloor(corridor, "Floor_Corridor", new Vector3(-3.75f, 0, 0), new Vector2(3.5f, 2.2f), matFloor);
            CreateCeiling(corridor, "Ceiling_Corridor", new Vector3(-3.75f, 2.8f, 0), new Vector2(3.5f, 2.2f), matTrim);

            // Pared Sur del pasillo (sólida)
            CreateWall(corridor, "Wall_Corridor_South", new Vector3(-3.75f, 1.4f, -1.1f), new Vector3(3.5f, 2.8f, 0.1f), matLeftWall);

            // Pared Norte del pasillo (puerta amplia hacia el Estudio en X: -4.5 a -3.0m, Ancho 1.5m)
            CreateWall(corridor, "Wall_Corridor_North_East", new Vector3(-2.5f, 1.4f, 1.1f), new Vector3(1.0f, 2.8f, 0.1f), matLeftWall);
            CreateWall(corridor, "Wall_Corridor_North_West", new Vector3(-5.0f, 1.4f, 1.1f), new Vector3(1.0f, 2.8f, 0.1f), matLeftWall);
            CreateWall(corridor, "Wall_Corridor_North_Lintel", new Vector3(-3.75f, 2.55f, 1.1f), new Vector3(1.50f, 0.5f, 0.1f), matTrim);
            CreateDoorFrame(corridor, "Doorframe_CorridorToStudy", new Vector3(-3.75f, 0f, 1.1f), true, matTrim);

            // Pared Oeste del pasillo (puerta amplia hacia el Archivo en Z: -0.7 a 0.7m, Ancho 1.4m)
            CreateWall(corridor, "Wall_Corridor_West_North", new Vector3(-5.5f, 1.4f, 0.9f), new Vector3(0.1f, 2.8f, 0.4f), matLeftWall);
            CreateWall(corridor, "Wall_Corridor_West_South", new Vector3(-5.5f, 1.4f, -0.9f), new Vector3(0.1f, 2.8f, 0.4f), matLeftWall);
            CreateWall(corridor, "Wall_Corridor_West_Lintel", new Vector3(-5.5f, 2.55f, 0f), new Vector3(0.1f, 0.5f, 1.40f), matTrim);
            CreateDoorFrame(corridor, "Doorframe_CorridorToArchive", new Vector3(-5.5f, 0f, 0f), false, matTrim);

            // ==========================================
            // 3. EL ESTUDIO (X: -5.5 a -2.0m, Z: 1.1 a 4.5m, Y: 0 a 2.8m)
            // ==========================================
            GameObject study = new GameObject("Room2_TheStudy");
            study.transform.SetParent(envRoot.transform, false);

            CreateFloor(study, "Floor_Study", new Vector3(-3.75f, 0, 2.8f), new Vector2(3.5f, 3.4f), matFloor);
            CreateCeiling(study, "Ceiling_Study", new Vector3(-3.75f, 2.8f, 2.8f), new Vector2(3.5f, 3.4f), matTrim);

            // Pared Norte (Pizarra de corcho con pistas)
            CreateWall(study, "Wall_Study_North", new Vector3(-3.75f, 1.4f, 4.5f), new Vector3(3.5f, 2.8f, 0.1f), matWallStudy);
            // Pared Este (Sólida)
            CreateWall(study, "Wall_Study_East", new Vector3(-2.0f, 1.4f, 2.8f), new Vector3(0.1f, 2.8f, 3.4f), matWallStudy);
            // Pared Oeste (Escritorio ejecutivo)
            CreateWall(study, "Wall_Study_West", new Vector3(-5.5f, 1.4f, 2.8f), new Vector3(0.1f, 2.8f, 3.4f), matWallStudy);
            // NOTA: La pared Sur del estudio ya está construida limpiamente por la pared Norte del pasillo.

            // Mobiliario del Estudio: Escritorio con cajones y Pizarra
            BuildStudyFurniture(study, matTrim, matCork);

            // ==========================================
            // 4. EL ARCHIVO (X: -9.2 a -5.5m, Z: -1.8 a 1.8m, Y: 0 a 2.8m)
            // ==========================================
            GameObject archive = new GameObject("Room3_TheArchive");
            archive.transform.SetParent(envRoot.transform, false);

            CreateFloor(archive, "Floor_Archive", new Vector3(-7.35f, 0, 0f), new Vector2(3.7f, 3.6f), matFloor);
            CreateCeiling(archive, "Ceiling_Archive", new Vector3(-7.35f, 2.8f, 0f), new Vector2(3.7f, 3.6f), matTrim);

            // Pared Oeste (Fondo con estanterías)
            CreateWall(archive, "Wall_Archive_West", new Vector3(-9.2f, 1.4f, 0f), new Vector3(0.1f, 2.8f, 3.6f), matWallArchive);
            // Pared Norte
            CreateWall(archive, "Wall_Archive_North", new Vector3(-7.35f, 1.4f, 1.8f), new Vector3(3.7f, 2.8f, 0.1f), matWallArchive);
            // Pared Sur
            CreateWall(archive, "Wall_Archive_South", new Vector3(-7.35f, 1.4f, -1.8f), new Vector3(3.7f, 2.8f, 0.1f), matWallArchive);

            // Pared Este (Cerramientos exteriores fuera de la zona del pasillo Z: [-1.1, 1.1])
            CreateWall(archive, "Wall_Archive_East_North", new Vector3(-5.5f, 1.4f, 1.45f), new Vector3(0.1f, 2.8f, 0.7f), matWallArchive);
            CreateWall(archive, "Wall_Archive_East_South", new Vector3(-5.5f, 1.4f, -1.45f), new Vector3(0.1f, 2.8f, 0.7f), matWallArchive);

            // Mobiliario del Archivo: Estanterías metálicas y Cajas
            BuildArchiveFurniture(archive, matShelf, matTrim);

            return root;
        }

        private static void CreateDoorFrame(GameObject parent, string name, Vector3 pos, bool alongX, Material mat)
        {
            GameObject frame = new GameObject(name);
            frame.transform.SetParent(parent.transform, false);
            frame.transform.localPosition = pos;

            if (alongX)
            {
                // Jamba izquierda (sin colisionador para no trabar al jugador)
                CreateBoxPrimitive(frame, "Jamb_Left", new Vector3(-0.75f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 0.14f), mat, false);
                // Jamba derecha
                CreateBoxPrimitive(frame, "Jamb_Right", new Vector3(0.75f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 0.14f), mat, false);
                // Dintel
                CreateBoxPrimitive(frame, "Header", new Vector3(0f, 2.22f, 0f), new Vector3(1.60f, 0.08f, 0.16f), mat, false);
            }
            else
            {
                // Jamba sur (sin colisionador)
                CreateBoxPrimitive(frame, "Jamb_South", new Vector3(0f, 1.1f, -0.75f), new Vector3(0.14f, 2.2f, 0.08f), mat, false);
                // Jamba norte
                CreateBoxPrimitive(frame, "Jamb_North", new Vector3(0f, 1.1f, 0.75f), new Vector3(0.14f, 2.2f, 0.08f), mat, false);
                // Dintel
                CreateBoxPrimitive(frame, "Header", new Vector3(0f, 2.22f, 0f), new Vector3(0.16f, 0.08f, 1.60f), mat, false);
            }
        }

        private static void SpawnChamberFurniture(GameObject chamber)
        {
            Material matChair = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_OrnateChair.mat");
            Material matTable = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_RusticTable.mat");

            // Mesa redonda rústica
            string tablePath = $"{PROPS_PATH}/Table/Meshy_AI_Rustic_Round_Wooden_T_0915165031_texture.obj";
            GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tablePath);
            if (tablePrefab != null)
            {
                GameObject tableInst = Object.Instantiate(tablePrefab, chamber.transform);
                tableInst.name = "Chamber_Table";
                float st = 0.74f;
                tableInst.transform.localScale = new Vector3(st, st, st);
                float yTable = 0.478407f * st;
                tableInst.transform.localPosition = new Vector3(0f, yTable, 0.25f);
                tableInst.transform.localRotation = Quaternion.identity;

                foreach (var r in tableInst.GetComponentsInChildren<Renderer>(true))
                {
                    if (matTable != null) r.sharedMaterial = matTable;
                }
                BoxCollider tc = tableInst.AddComponent<BoxCollider>();
                tc.size = new Vector3(1.90f, 0.96f, 1.90f);
            }

            // Silla ornada
            string chairPath = $"{PROPS_PATH}/Chair/Meshy_AI_Ornate_Wooden_Chair_0915164651_texture.obj";
            GameObject chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(chairPath);
            if (chairPrefab != null)
            {
                GameObject chairInst = Object.Instantiate(chairPrefab, chamber.transform);
                chairInst.name = "Chamber_Chair";
                float sc = 0.58f;
                chairInst.transform.localScale = new Vector3(sc, sc, sc);
                float yLegs = 0.9514f * sc;
                chairInst.transform.localPosition = new Vector3(0.85f, yLegs, 0.25f);
                chairInst.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

                foreach (var r in chairInst.GetComponentsInChildren<Renderer>(true))
                {
                    if (matChair != null) r.sharedMaterial = matChair;
                }
                BoxCollider cc = chairInst.AddComponent<BoxCollider>();
                cc.size = new Vector3(0.85f, 1.90f, 0.95f);
            }
        }

        private static void BuildStudyFurniture(GameObject study, Material matWood, Material matCork)
        {
            GameObject deskRoot = new GameObject("Executive_Desk");
            deskRoot.transform.SetParent(study.transform, false);
            deskRoot.transform.localPosition = new Vector3(-4.8f, 0f, 2.75f);
            deskRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // Tablero
            CreateBoxPrimitive(deskRoot, "Desk_Top", new Vector3(0f, 0.74f, 0f), new Vector3(1.5f, 0.05f, 0.75f), matWood);
            // Laterales
            CreateBoxPrimitive(deskRoot, "Desk_Left_Side", new Vector3(-0.7f, 0.37f, 0f), new Vector3(0.06f, 0.74f, 0.7f), matWood);
            CreateBoxPrimitive(deskRoot, "Desk_Right_Side", new Vector3(0.7f, 0.37f, 0f), new Vector3(0.06f, 0.74f, 0.7f), matWood);
            // Cajonera derecha
            CreateBoxPrimitive(deskRoot, "Desk_Drawer_Block", new Vector3(0.42f, 0.38f, 0f), new Vector3(0.48f, 0.66f, 0.68f), matWood);
            // Cajón inferior interactivo
            CreateBoxPrimitive(deskRoot, "Drawer_Interactive_Lower", new Vector3(0.42f, 0.18f, 0.08f), new Vector3(0.44f, 0.20f, 0.66f), matWood);

            // Pizarra de Corcho en la pared Norte del estudio (Z = 4.5m)
            GameObject corkboard = new GameObject("Investigation_Corkboard");
            corkboard.transform.SetParent(study.transform, false);
            corkboard.transform.localPosition = new Vector3(-3.75f, 1.65f, 4.44f);

            CreateBoxPrimitive(corkboard, "Cork_Frame", Vector3.zero, new Vector3(2.0f, 1.2f, 0.04f), matWood);
            CreateBoxPrimitive(corkboard, "Cork_Surface", new Vector3(0f, 0f, -0.015f), new Vector3(1.9f, 1.1f, 0.02f), matCork);
        }

        private static void BuildArchiveFurniture(GameObject archive, Material matMetal, Material matWood)
        {
            GameObject shelvesRoot = new GameObject("High_Archive_Shelves");
            shelvesRoot.transform.SetParent(archive.transform, false);
            shelvesRoot.transform.localPosition = new Vector3(-8.75f, 0f, 0f);

            // Estantería metálica industrial pegada a la pared Oeste
            CreateBoxPrimitive(shelvesRoot, "Frame_Back", new Vector3(0f, 1.25f, 0f), new Vector3(0.04f, 2.5f, 2.6f), matMetal);
            CreateBoxPrimitive(shelvesRoot, "Post_North", new Vector3(0.35f, 1.25f, 1.25f), new Vector3(0.06f, 2.5f, 0.06f), matMetal);
            CreateBoxPrimitive(shelvesRoot, "Post_South", new Vector3(0.35f, 1.25f, -1.25f), new Vector3(0.06f, 2.5f, 0.06f), matMetal);

            for (int i = 0; i < 4; i++)
            {
                float y = 0.35f + (i * 0.65f);
                CreateBoxPrimitive(shelvesRoot, $"Shelf_Tier_{i}", new Vector3(0.18f, y, 0f), new Vector3(0.40f, 0.04f, 2.5f), matWood);
            }

            // Caja de herramientas en el suelo del Archivo
            CreateBoxPrimitive(archive, "Metal_Toolbox", new Vector3(-6.5f, 0.18f, -1.0f), new Vector3(0.55f, 0.35f, 0.35f), matMetal);
        }

        private static void BuildTacticalBombAndDossier(GameObject root)
        {
            Material matCase = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_TacticalCase.mat");
            Material matButton = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_KeypadButton.mat");
            Material matC4 = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_C4Explosive.mat");
            Material matPCB = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_CircuitBoard.mat");
            Material matPaper = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_PaperNote.mat");
            Material matBoard = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_ClipboardWood.mat");

            // ==========================================
            // A) LA BOMBA TÁCTICA (Sobre la mesa a la derecha)
            // ==========================================
            GameObject bombRoot = new GameObject("Tactical_C4_Bomb");
            bombRoot.transform.SetParent(root.transform, false);
            bombRoot.transform.localPosition = new Vector3(0.18f, 0.74f, 0.28f);
            bombRoot.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

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

            // Cables de colores
            CreateWire(bombRoot, "Wire_R", new Vector3(-0.04f, 0.12f, -0.06f), new Vector3(0.04f, 0.12f, -0.06f), Color.red);
            CreateWire(bombRoot, "Wire_B", new Vector3(-0.04f, 0.12f, 0.0f), new Vector3(0.04f, 0.12f, 0.0f), Color.blue);
            CreateWire(bombRoot, "Wire_Y", new Vector3(-0.04f, 0.12f, 0.06f), new Vector3(0.04f, 0.12f, 0.06f), Color.yellow);

            // Display LCD
            GameObject bezel = CreateBoxPrimitive(bombRoot, "Display_Bezel", new Vector3(0.11f, 0.125f, 0.08f), new Vector3(0.17f, 0.035f, 0.08f), matCase);

            GameObject timerObj = new GameObject("Timer_TMP");
            timerObj.transform.SetParent(bezel.transform, false);
            timerObj.transform.localPosition = new Vector3(0f, 0.52f, 0.01f);
            timerObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro timerTMP = timerObj.AddComponent<TextMeshPro>();
            timerTMP.text = "05:00.00";
            timerTMP.fontSize = 2.2f;
            timerTMP.alignment = TextAlignmentOptions.Center;
            timerTMP.color = new Color(1f, 0.45f, 0.15f);

            GameObject codeObj = new GameObject("Code_TMP");
            codeObj.transform.SetParent(bezel.transform, false);
            codeObj.transform.localPosition = new Vector3(0f, 0.52f, -0.025f);
            codeObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro codeTMP = codeObj.AddComponent<TextMeshPro>();
            codeTMP.text = "_ _ _ _";
            codeTMP.fontSize = 1.6f;
            codeTMP.alignment = TextAlignmentOptions.Center;
            codeTMP.color = Color.cyan;

            GameObject statusObj = new GameObject("Status_TMP");
            statusObj.transform.SetParent(bezel.transform, false);
            statusObj.transform.localPosition = new Vector3(0f, 0.52f, 0.036f);
            statusObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro statusTMP = statusObj.AddComponent<TextMeshPro>();
            statusTMP.text = "DISPOSITIVO ARMADO";
            statusTMP.fontSize = 0.85f;
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

            // Teclado 3x4
            GameObject keypadRoot = new GameObject("Keypad");
            keypadRoot.transform.SetParent(bombRoot.transform, false);
            keypadRoot.transform.localPosition = new Vector3(0.11f, 0.11f, -0.045f);

            string[] keys = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "ENT" };
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    string k = keys[r * 3 + c];
                    float bx = (c - 1) * 0.042f;
                    float bz = (1.5f - r) * 0.036f;
                    GameObject btn = CreateBoxPrimitive(keypadRoot, $"Btn_{k}", new Vector3(bx, 0.012f, bz), new Vector3(0.034f, 0.016f, 0.028f), matButton);

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

            bombRoot.AddComponent<AudioSource>();
            BombAudioSynthesizer audio = bombRoot.AddComponent<BombAudioSynthesizer>();

            BombController ctrl = bombRoot.AddComponent<BombController>();
            ctrl.config = AssetDatabase.LoadAssetAtPath<BombConfigData>($"{GAME_DATA_PATH}/DefaultBombConfig.asset");
            ctrl.timerText = timerTMP;
            ctrl.codeText = codeTMP;
            ctrl.statusText = statusTMP;
            ctrl.activeRedLight = rLight;
            ctrl.defusedGreenLight = gLight;
            ctrl.audioSynth = audio;

            // ==========================================
            // B) HOJA DE INSTRUCCIONES / DOSSIER EN LA MESA
            // ==========================================
            GameObject dossierRoot = new GameObject("Mission_Dossier_Sheet");
            dossierRoot.transform.SetParent(root.transform, false);
            // Colocada a la izquierda de la bomba en la mesa, inclinada hacia el jugador para fácil lectura
            dossierRoot.transform.localPosition = new Vector3(-0.25f, 0.76f, 0.20f);
            dossierRoot.transform.localRotation = Quaternion.Euler(26f, -12f, 0f);

            // Tablilla de apoyo (portapapeles de madera)
            CreateBoxPrimitive(dossierRoot, "Clipboard_Board", new Vector3(0f, 0.004f, 0f), new Vector3(0.32f, 0.008f, 0.42f), matBoard, false);
            // Clip metálico superior
            CreateBoxPrimitive(dossierRoot, "Clipboard_Clip", new Vector3(0f, 0.012f, 0.185f), new Vector3(0.12f, 0.012f, 0.04f), matCase, false);

            // Hoja de papel
            CreateBoxPrimitive(dossierRoot, "Paper_Sheet", new Vector3(0f, 0.009f, -0.01f), new Vector3(0.28f, 0.002f, 0.36f), matPaper, false);

            // Colisionador para apuntar con el cursor y hacer click / pulsar E
            BoxCollider dCol = dossierRoot.AddComponent<BoxCollider>();
            dCol.center = new Vector3(0f, 0.02f, 0f);
            dCol.size = new Vector3(0.35f, 0.06f, 0.44f);

            // Texto de instrucciones
            GameObject dossierTextObj = new GameObject("Dossier_TMP");
            dossierTextObj.transform.SetParent(dossierRoot.transform, false);
            dossierTextObj.transform.localPosition = new Vector3(0f, 0.011f, -0.01f);
            dossierTextObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dossierTextObj.transform.localScale = new Vector3(0.0036f, 0.0036f, 0.0036f);

            TextMeshPro dTMP = dossierTextObj.AddComponent<TextMeshPro>();
            dTMP.fontSize = 3.3f;
            dTMP.lineSpacing = -12f;
            dTMP.enableWordWrapping = true;
            dTMP.overflowMode = TextOverflowModes.Truncate;
            dTMP.alignment = TextAlignmentOptions.TopLeft;
            dTMP.color = new Color(0.10f, 0.08f, 0.08f);

            RectTransform rt = dTMP.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(62f, 84f);

            // Componente que actualiza dinámicamente el progreso
            MissionDossier md = dossierRoot.AddComponent<MissionDossier>();
            md.documentText = dTMP;
            md.clue1 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_01_DeskDrawer.asset");
            md.clue2 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_02_Corkboard.asset");
            md.clue3 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_03_Bookshelf.asset");
            md.clue4 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_04_Toolbox.asset");
            md.UpdateDossierContent();
        }

        private static void PlaceCluesInRooms(GameObject root)
        {
            Material matPaper = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_PaperNote.mat");

            // Pista 1: En el escritorio del Estudio
            Transform study = root.transform.Find("Environment_Rooms/Room2_TheStudy");
            if (study != null)
            {
                ClueData c1 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_01_DeskDrawer.asset");
                // En la cajonera del escritorio
                GameObject c1Obj = CreateBoxPrimitive(study.gameObject, "Clue1_DeskNote", new Vector3(-4.8f, 0.22f, 2.75f), new Vector3(0.18f, 0.01f, 0.22f), matPaper);
                ClueInteractable ci1 = c1Obj.AddComponent<ClueInteractable>();
                ci1.clueData = c1;
                AddClueLabel(c1Obj, "1er Dígito: [ 4 ]");

                // Pista 2: En la pizarra de corcho
                ClueData c2 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_02_Corkboard.asset");
                GameObject c2Obj = CreateBoxPrimitive(study.gameObject, "Clue2_CorkboardNote", new Vector3(-3.5f, 1.65f, 4.41f), new Vector3(0.22f, 0.26f, 0.01f), matPaper);
                ClueInteractable ci2 = c2Obj.AddComponent<ClueInteractable>();
                ci2.clueData = c2;
                AddClueLabel(c2Obj, "2do Dígito: [ 8 ]");
            }

            // Pistas 3 y 4: En el Archivo
            Transform archive = root.transform.Find("Environment_Rooms/Room3_TheArchive");
            if (archive != null)
            {
                ClueData c3 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_03_Bookshelf.asset");
                // En la segunda balda de la estantería
                GameObject c3Obj = CreateBoxPrimitive(archive.gameObject, "Clue3_Bookshelf", new Vector3(-8.55f, 1.05f, 0.3f), new Vector3(0.08f, 0.22f, 0.18f), matPaper);
                ClueInteractable ci3 = c3Obj.AddComponent<ClueInteractable>();
                ci3.clueData = c3;
                AddClueLabel(c3Obj, "3er Dígito: [ 2 ]");

                ClueData c4 = AssetDatabase.LoadAssetAtPath<ClueData>($"{GAME_DATA_PATH}/Clue_04_Toolbox.asset");
                // En la caja de herramientas del suelo
                GameObject c4Obj = CreateBoxPrimitive(archive.gameObject, "Clue4_ToolboxTag", new Vector3(-6.5f, 0.36f, -1.0f), new Vector3(0.16f, 0.01f, 0.14f), matPaper);
                ClueInteractable ci4 = c4Obj.AddComponent<ClueInteractable>();
                ci4.clueData = c4;
                AddClueLabel(c4Obj, "4to Dígito: [ 6 ]");
            }
        }

        private static void AddClueLabel(GameObject obj, string text)
        {
            GameObject l = new GameObject("Label_TMP");
            l.transform.SetParent(obj.transform, false);
            l.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            l.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            l.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            TextMeshPro tmp = l.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateWire(GameObject parent, string name, Vector3 start, Vector3 end, Color col)
        {
            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            w.name = name;
            w.transform.SetParent(parent.transform, false);
            w.transform.localPosition = (start + end) * 0.5f;
            w.transform.localScale = new Vector3(0.008f, Vector3.Distance(start, end) * 0.5f, 0.008f);
            w.transform.up = (end - start).normalized;
            w.GetComponent<Renderer>().material.color = col;
            DestroyImmediate(w.GetComponent<Collider>());
        }

        private static void SetupLighting(GameObject root)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.36f, 0.44f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.24f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.12f, 0.10f);

            GameObject lights = new GameObject("Lighting_Rig");
            lights.transform.SetParent(root.transform, false);

            // Luz de luna a través de la ventana
            GameObject moon = new GameObject("Moonlight");
            moon.transform.SetParent(lights.transform, false);
            moon.transform.position = new Vector3(1.5f, 4.0f, 4.0f);
            moon.transform.rotation = Quaternion.Euler(42f, -145f, 0f);
            Light ml = moon.AddComponent<Light>();
            ml.type = LightType.Directional;
            ml.color = new Color(0.70f, 0.82f, 0.98f);
            ml.intensity = 1.2f;
            ml.shadows = LightShadows.Soft;

            // Foco cálido en Sala de la Bomba
            CreatePointLight(lights, "Light_Chamber", new Vector3(0f, 2.2f, 0.25f), new Color(0.95f, 0.82f, 0.68f), 0.85f, 6.0f);
            // Aplique en Pasillo
            CreatePointLight(lights, "Light_Corridor", new Vector3(-3.75f, 2.0f, 0f), new Color(0.95f, 0.78f, 0.58f), 0.75f, 5.0f);
            // Lámpara en Estudio
            CreatePointLight(lights, "Light_Study", new Vector3(-3.75f, 2.2f, 2.75f), new Color(1.0f, 0.88f, 0.72f), 0.90f, 6.5f);
            // Lámpara en Archivo
            CreatePointLight(lights, "Light_Archive", new Vector3(-7.25f, 2.2f, 0f), new Color(0.85f, 0.88f, 0.95f), 0.85f, 6.5f);
        }

        private static void CreatePointLight(GameObject parent, string name, Vector3 pos, Color col, float intensity, float range)
        {
            GameObject o = new GameObject(name);
            o.transform.SetParent(parent.transform, false);
            o.transform.position = pos;
            Light l = o.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        private static void SetupPlayer(GameObject root)
        {
            Camera[] oldCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in oldCams)
            {
                if (c.transform.parent == null || c.gameObject.name == "Main Camera")
                {
                    c.gameObject.SetActive(false);
                }
            }

            GameObject oldPlayer = GameObject.Find("Player_FPV");
            if (oldPlayer != null) DestroyImmediate(oldPlayer);

            GameObject player = new GameObject("Player_FPV");
            // Colocado a 1 metro frente a la mesa en la Sala 1, mirando directamente a la bomba y la hoja de misión
            player.transform.position = new Vector3(0f, 0.05f, -0.75f);
            player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.875f, 0f);
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0.001f;

            GameObject camObj = new GameObject("FirstPersonCamera");
            camObj.transform.SetParent(player.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camObj.tag = "MainCamera";

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<FPSRaycastInteractor>();

            FirstPersonController fpc = player.AddComponent<FirstPersonController>();
            fpc.cameraTransform = camObj.transform;
            fpc.walkSpeed = 2.4f;
            fpc.runSpeed = 4.2f;
            fpc.mouseSensitivity = 0.12f;

            Selection.activeGameObject = player;
        }

        private static GameObject CreateFloor(GameObject parent, string name, Vector3 pos, Vector2 size, Material mat)
        {
            GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = name;
            f.transform.SetParent(parent.transform, false);
            f.transform.localPosition = new Vector3(pos.x, pos.y - 0.02f, pos.z);
            f.transform.localScale = new Vector3(size.x, 0.04f, size.y);
            Renderer r = f.GetComponent<Renderer>();
            r.sharedMaterial = mat;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetVector("_BaseMap_ST", new Vector4(size.x * 0.75f, size.y * 0.75f, 0, 0));
            r.SetPropertyBlock(mpb);

            // Quitamos colisionador individual para no trabar al CharacterController en los bordes
            Collider c = f.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);

            return f;
        }

        private static GameObject CreateCeiling(GameObject parent, string name, Vector3 pos, Vector2 size, Material mat)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = name;
            c.transform.SetParent(parent.transform, false);
            c.transform.localPosition = new Vector3(pos.x, pos.y + 0.02f, pos.z);
            c.transform.localScale = new Vector3(size.x, 0.04f, size.y);
            Renderer r = c.GetComponent<Renderer>();
            r.sharedMaterial = mat;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetVector("_BaseMap_ST", new Vector4(size.x * 0.75f, size.y * 0.75f, 0, 0));
            r.SetPropertyBlock(mpb);

            return c;
        }

        private static GameObject CreateWall(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.SetParent(parent.transform, false);
            w.transform.localPosition = pos;
            w.transform.localScale = size;
            Renderer r = w.GetComponent<Renderer>();
            r.sharedMaterial = mat;

            float horizontalDim = Mathf.Max(size.x, size.z);
            float verticalDim = size.y;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetVector("_BaseMap_ST", new Vector4(horizontalDim * 0.75f, verticalDim * 0.75f, 0, 0));
            r.SetPropertyBlock(mpb);

            return w;
        }

        private static GameObject CreateBoxPrimitive(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat, bool withCollider = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = pos;
            box.transform.localScale = size;
            if (mat != null) box.GetComponent<Renderer>().sharedMaterial = mat;
            if (!withCollider)
            {
                Collider c = box.GetComponent<Collider>();
                if (c != null) DestroyImmediate(c);
            }
            return box;
        }
    }
}
