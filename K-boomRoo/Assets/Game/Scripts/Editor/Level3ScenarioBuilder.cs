using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using DefusalGame.Bomb;
using DefusalGame.Gameplay;
using DefusalGame.VR;
using StylizedRoom;

namespace DefusalGame.Editor
{
    public class Level3ScenarioBuilder : EditorWindow
    {
        private const string SCENE_PATH = "Assets/Scenes/Level3_House.unity";
        private const string MAT_PATH = "Assets/Game/Materials/Level3";
        private const string PREFAB_PATH = "Assets/Game/Prefabs";

        // Prefabs oficiales de Realidad Virtual (XR Interaction Toolkit & VR Template)
        private const string VR_RIG_PREFAB = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";
        private const string VR_RIG_BACKUP = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        // Modelos 3D originales completos sin cortes
        private const string COCINA_OBJ = "Assets/Game/Models/Level3/Cocina/Meshy_AI_Rustic_Kitchen_0922152334_texture.obj";
        private const string COCINA_TEX = "Assets/Game/Models/Level3/Cocina/Meshy_AI_Rustic_Kitchen_0922152334_texture.png";

        private const string CUARTO_OBJ = "Assets/Game/Models/Level3/Cuarto/Meshy_AI_Cozy_Sunlit_Bedroom_0922151330_texture.obj";
        private const string CUARTO_TEX = "Assets/Game/Models/Level3/Cuarto/Meshy_AI_Cozy_Sunlit_Bedroom_0922151330_texture.png";

        private const string ESTUDIO_OBJ = "Assets/Game/Models/Level3/Estudio/Meshy_AI_Classic_Study_0922153228_texture.obj";
        private const string ESTUDIO_TEX = "Assets/Game/Models/Level3/Estudio/Meshy_AI_Classic_Study_0922153228_texture.png";

        [MenuItem("Desactiva la Bomba/Nivel 3/Generar Nivel 3 Completo (VR + PC)", false, 1)]
        public static void BuildLevel3()
        {
            EnsureLevel3Scene();
            CleanOldLevel3Objects();
            Directory.CreateDirectory(MAT_PATH);

            GameObject root = new GameObject("DefusalGame_Level3_Scenario");
            Undo.RegisterCreatedObjectUndo(root, "Create Level 3 Scenario");

            // 0. Gestor de Interacción de Realidad Virtual (XR Interaction Manager) y EventSystem
            SetupXRSystem(root);

            // 1. Suelo maestro continuo y límites perimetrales sin bloqueos
            CreateMasterColliders(root);

            // 2. Pasillo conector central con aberturas amplias y señalética orientada al jugador
            BuildCentralCorridor(root);

            // 3. Estructuras de las 3 habitaciones (modelos originales completos, sin MeshCollider bloqueante)
            SpawnRooms(root);

            // 4. Objetos interactivos y Pistas con soporte de Realidad Virtual (Rayo, Poke, Agarre 6DoF)
            BuildStudyContents(root);
            BuildKitchenContents(root);
            BuildBedroomContents(root);

            // 5. Iluminación y atmósfera general
            SetupAtmosphere(root);

            // 6. HUD Espacial en 3D para Realidad Virtual
            GameObject hudObj = BuildVRSpatialHUD(root);

            // 7. Configurar Rig de Jugador Dual: Realidad Virtual + Fallback Escritorio FPV
            SetupPlayerSystems(root, hudObj);

            // Guardar Prefab y Escena
            Directory.CreateDirectory(PREFAB_PATH);
            string prefabFile = $"{PREFAB_PATH}/DefusalGame_Level3_Scenario.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabFile, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = root;
            SceneView.FrameLastActiveSceneView();

            Debug.Log("[Level3Builder] ¡Nivel 3 generado con Realidad Virtual nativa (XR Origin, mandos Quest, agarre físico y HUD 3D)!");
            EditorUtility.DisplayDialog(
                "¡Nivel 3 VR: Listo para Jugar!",
                "El juego ha sido configurado con soporte nativo de REALIDAD VIRTUAL (RV / XR):\n\n" +
                "• CONTROLES RV: Rig oficial con mandos de Meta Quest y carteles flotantes (Move, Grab, UI Press, Turn, Blink).\n" +
                "• AGARRE FÍSICO 6DoF: Agarra la Linterna UV con la mano y préndela con el gatillo para iluminar la pared; traslada el hielo y usa las pinzas.\n" +
                "• INTERACCIÓN DIRECTA Y RAYO: Pulsa las teclas de la bomba, corta cables y baja fusibles con el puntero láser o tocando directamente.\n" +
                "• HUD ESPACIAL 3D: Cronómetro y objetivos visibles en espacio 3D para el visor de RV.\n\n" +
                "¡Ponte las gafas de RV o usa el simulador y pulsa Play!",
                "¡A jugar!"
            );
        }

        private static void EnsureLevel3Scene()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!buildScenes.Exists(s => s.path == SCENE_PATH))
            {
                buildScenes.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            if (!File.Exists(SCENE_PATH))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, SCENE_PATH);
            }
            else
            {
                var currentScene = EditorSceneManager.GetActiveScene();
                if (currentScene.path != SCENE_PATH)
                {
                    EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                }
            }
        }

        private static void CleanOldLevel3Objects()
        {
            string[] names = new string[] {
                "DefusalGame_Level3_Scenario",
                "DefusalGame_Scenario",
                "Player_FPV",
                "Player_Rig_System",
                "Complete XR Origin Set Up Variant",
                "XR Origin (XR Rig)",
                "XR Interaction Manager",
                "VR_Spatial_HUD",
                "Environment_Level3",
                "Directional Light",
                "Main Camera",
                "Camera",
                "Lighting_Rig_Level3"
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
        }

        private static void SetupXRSystem(GameObject root)
        {
            // 1. XR Interaction Manager
            var existingManager = Object.FindAnyObjectByType<XRInteractionManager>();
            if (existingManager == null)
            {
                GameObject mgrObj = new GameObject("XR Interaction Manager");
                mgrObj.transform.SetParent(root.transform, false);
                mgrObj.AddComponent<XRInteractionManager>();
            }

            // 2. EventSystem compatible con XR UI
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.transform.SetParent(root.transform, false);
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static Shader GetURPShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        private static Material GetOrCreateTexturedMat(string matName, string texPath)
        {
            string fullMatPath = $"{MAT_PATH}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(fullMatPath);
            Shader litShader = GetURPShader();

            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, fullMatPath);
            }
            else
            {
                mat.shader = litShader;
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            mat.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateColoredMat(string matName, Color col, float metallic = 0.1f, float smoothness = 0.3f)
        {
            string fullMatPath = $"{MAT_PATH}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(fullMatPath);
            Shader litShader = GetURPShader();

            if (mat == null)
            {
                mat = new Material(litShader);
                AssetDatabase.CreateAsset(mat, fullMatPath);
            }
            else
            {
                mat.shader = litShader;
            }

            mat.SetColor("_BaseColor", col);
            mat.color = col;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ==========================================
        // COLISIONES MAESTRAS Y LÍMITES PERIMETRALES
        // ==========================================
        private static void CreateMasterColliders(GameObject root)
        {
            GameObject colRoot = new GameObject("Master_Colliders_Level3");
            colRoot.transform.SetParent(root.transform, false);

            GameObject floorCol = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorCol.name = "Master_Floor_Collider";
            floorCol.transform.SetParent(colRoot.transform, false);
            floorCol.transform.localPosition = new Vector3(0f, -0.1f, -1.8f);
            floorCol.transform.localScale = new Vector3(22f, 0.2f, 14f);
            floorCol.GetComponent<Renderer>().enabled = false;

            CreateInvisibleWall(colRoot, "Perimeter_North", new Vector3(0f, 1.5f, 3.6f), new Vector3(22f, 3.2f, 0.2f));
            CreateInvisibleWall(colRoot, "Perimeter_South", new Vector3(0f, 1.5f, -7.2f), new Vector3(10f, 3.2f, 0.2f));
            CreateInvisibleWall(colRoot, "Perimeter_West", new Vector3(-10.2f, 1.5f, 0f), new Vector3(0.2f, 3.2f, 8f));
            CreateInvisibleWall(colRoot, "Perimeter_East", new Vector3(10.2f, 1.5f, 0f), new Vector3(0.2f, 3.2f, 8f));
        }

        private static void CreateInvisibleWall(GameObject parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject w = CreateBoxPrimitive(parent, name, pos, size, null, true);
            w.GetComponent<Renderer>().enabled = false;
        }

        // ==========================================
        // PASILLO CENTRAL CONECTOR
        // ==========================================
        private static void BuildCentralCorridor(GameObject root)
        {
            GameObject corridor = new GameObject("Corridor_Central");
            corridor.transform.SetParent(root.transform, false);

            Material matFloor = GetOrCreateColoredMat("Mat_Corridor_Floor", new Color(0.18f, 0.17f, 0.16f), 0.1f, 0.4f);
            Material matWall = GetOrCreateColoredMat("Mat_Corridor_Wall", new Color(0.35f, 0.36f, 0.38f), 0.05f, 0.15f);
            Material matTrim = GetOrCreateColoredMat("Mat_WoodTrim_L3", new Color(0.25f, 0.18f, 0.12f), 0.05f, 0.3f);

            CreateFloor(corridor, "Floor_Corridor", new Vector3(0f, 0f, 0f), new Vector2(7.6f, 2.4f), matFloor);
            CreateCeiling(corridor, "Ceiling_Corridor", new Vector3(0f, 2.8f, 0f), new Vector2(7.6f, 2.4f), matTrim);

            CreateWall(corridor, "Wall_Corridor_North", new Vector3(0f, 1.4f, 1.2f), new Vector3(7.6f, 2.8f, 0.1f), matWall);

            CreateWall(corridor, "Wall_Corridor_South_West", new Vector3(-2.45f, 1.4f, -1.2f), new Vector3(2.7f, 2.8f, 0.1f), matWall);
            CreateWall(corridor, "Wall_Corridor_South_East", new Vector3(2.45f, 1.4f, -1.2f), new Vector3(2.7f, 2.8f, 0.1f), matWall);
            CreateWall(corridor, "Wall_Corridor_South_Lintel", new Vector3(0f, 2.55f, -1.2f), new Vector3(2.2f, 0.5f, 0.1f), matTrim);

            CreateWall(corridor, "Wall_Corridor_West_North", new Vector3(-3.8f, 1.4f, 1.05f), new Vector3(0.1f, 2.8f, 0.3f), matWall);
            CreateWall(corridor, "Wall_Corridor_West_South", new Vector3(-3.8f, 1.4f, -1.05f), new Vector3(0.1f, 2.8f, 0.3f), matWall);
            CreateWall(corridor, "Wall_Corridor_West_Lintel", new Vector3(-3.8f, 2.55f, 0f), new Vector3(0.1f, 0.5f, 2.1f), matTrim);

            CreateWall(corridor, "Wall_Corridor_East_North", new Vector3(3.8f, 1.4f, 1.05f), new Vector3(0.1f, 2.8f, 0.3f), matWall);
            CreateWall(corridor, "Wall_Corridor_East_South", new Vector3(3.8f, 1.4f, -1.05f), new Vector3(0.1f, 2.8f, 0.3f), matWall);
            CreateWall(corridor, "Wall_Corridor_East_Lintel", new Vector3(3.8f, 2.55f, 0f), new Vector3(0.1f, 0.5f, 2.1f), matTrim);

            CreateSign(corridor, "Sign_Kitchen", new Vector3(-3.75f, 2.2f, 0f), Quaternion.Euler(0f, 90f, 0f), "← COCINA\n[Panel Eléctrico / Hielo]", Color.yellow);
            CreateSign(corridor, "Sign_Bedroom", new Vector3(3.75f, 2.2f, 0f), Quaternion.Euler(0f, -90f, 0f), "CUARTO →\n[Dormitorio / Linterna UV]", Color.cyan);
            CreateSign(corridor, "Sign_Study", new Vector3(0f, 2.2f, -1.15f), Quaternion.Euler(0f, 0f, 0f), "↓ ESTUDIO - BOMBA ↓", Color.red);
        }

        private static void CreateSign(GameObject parent, string name, Vector3 pos, Quaternion rot, string text, Color textColor)
        {
            GameObject signObj = new GameObject(name);
            signObj.transform.SetParent(parent.transform, false);
            signObj.transform.localPosition = pos;
            signObj.transform.localRotation = rot;

            TextMeshPro tm = signObj.AddComponent<TextMeshPro>();
            tm.text = text;
            tm.fontSize = 2.2f;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = textColor;
            tm.rectTransform.sizeDelta = new Vector2(3.2f, 1.2f);
        }

        // ==========================================
        // HABITACIONES 3D
        // ==========================================
        private static void SpawnRooms(GameObject root)
        {
            GameObject roomsRoot = new GameObject("Environment_Level3");
            roomsRoot.transform.SetParent(root.transform, false);

            SpawnRoomModel(roomsRoot, "Room_Cocina", COCINA_OBJ, COCINA_TEX, "Mat_L3_RusticKitchen",
                new Vector3(-6.8f, 1.59f, 0f), Quaternion.Euler(0f, 90f, 0f), 3.2f);

            SpawnRoomModel(roomsRoot, "Room_Estudio", ESTUDIO_OBJ, ESTUDIO_TEX, "Mat_L3_ClassicStudy",
                new Vector3(0f, 1.24f, -3.71f), Quaternion.Euler(0f, -90f, 0f), 3.2f);

            SpawnRoomModel(roomsRoot, "Room_Cuarto", CUARTO_OBJ, CUARTO_TEX, "Mat_L3_CozyBedroom",
                new Vector3(6.8f, 1.24f, 0f), Quaternion.Euler(0f, 0f, 0f), 3.2f);
        }

        private static void SpawnRoomModel(GameObject parent, string name, string objPath, string texPath, string matName, Vector3 pos, Quaternion rot, float scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Level3Builder] No se encontró el modelo en: {objPath}");
                return;
            }

            Material mat = GetOrCreateTexturedMat(matName, texPath);
            GameObject inst = Object.Instantiate(prefab, parent.transform);
            inst.name = name;
            inst.transform.localPosition = pos;
            inst.transform.localRotation = rot;
            inst.transform.localScale = new Vector3(scale, scale, scale);

            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
            }
        }

        // ==========================================
        // HABITACIÓN 1: ESTUDIO (BOMBA, TECLADO VR, TERMINAL)
        // ==========================================
        private static void BuildStudyContents(GameObject root)
        {
            GameObject studyContents = new GameObject("Study_Interactive_Contents");
            studyContents.transform.SetParent(root.transform, false);

            Material matWood = GetOrCreateColoredMat("Mat_StudyTable", new Color(0.24f, 0.16f, 0.10f), 0.05f, 0.3f);
            Material matMetal = GetOrCreateColoredMat("Mat_BombCase", new Color(0.12f, 0.13f, 0.15f), 0.6f, 0.5f);
            Material matPCDesk = GetOrCreateColoredMat("Mat_PCDeskWood", new Color(0.30f, 0.22f, 0.15f), 0.05f, 0.35f);

            // 1. Mesa central de la Bomba
            GameObject table = CreateBoxPrimitive(studyContents, "Study_Bomb_Table", new Vector3(0f, 0.40f, -3.8f), new Vector3(1.8f, 0.80f, 1.0f), matWood);

            // 2. Bomba Táctica Multietapa
            BuildMultiStageBomb(studyContents, new Vector3(0f, 0.85f, -3.8f), matMetal);

            // 3. Hoja Dossier Táctico
            BuildMissionDossierSheet(studyContents, new Vector3(0.50f, 0.81f, -3.75f));

            // 4. Escritorio sólido para PC
            CreateBoxPrimitive(studyContents, "Study_Desk_PC", new Vector3(-1.8f, 0.38f, -3.8f), new Vector3(1.2f, 0.76f, 0.80f), matPCDesk);

            // 5. Terminal de Computadora
            BuildComputerTerminal(studyContents, new Vector3(-1.8f, 0.76f, -3.8f));
        }

        private static void BuildMultiStageBomb(GameObject parent, Vector3 pos, Material matCase)
        {
            GameObject bombObj = new GameObject("Tactical_Level3_Bomb");
            bombObj.transform.SetParent(parent.transform, false);
            bombObj.transform.localPosition = pos;
            bombObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            Level3MultiStageBomb bombScript = bombObj.AddComponent<Level3MultiStageBomb>();

            CreateBoxPrimitive(bombObj, "Bomb_Chassis", new Vector3(0f, 0.06f, 0f), new Vector3(0.72f, 0.12f, 0.48f), matCase);

            Material matScreen = GetOrCreateColoredMat("Mat_LCDScreen", new Color(0.02f, 0.04f, 0.02f), 0.1f, 0.9f);
            Material matKeypad = GetOrCreateColoredMat("Mat_BombKeypad", new Color(0.20f, 0.22f, 0.24f), 0.2f, 0.3f);

            GameObject lcdPanel = CreateBoxPrimitive(bombObj, "LCD_Display_Panel", new Vector3(-0.16f, 0.13f, 0.08f), new Vector3(0.32f, 0.02f, 0.24f), matScreen);

            GameObject timerTextObj = new GameObject("Timer_Text");
            timerTextObj.transform.SetParent(lcdPanel.transform, false);
            timerTextObj.transform.localPosition = new Vector3(0f, 0.015f, 0.04f);
            timerTextObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro tmTimer = timerTextObj.AddComponent<TextMeshPro>();
            tmTimer.text = "03:30";
            tmTimer.fontSize = 2.8f;
            tmTimer.alignment = TextAlignmentOptions.Center;
            tmTimer.color = Color.red;
            bombScript.timerText = tmTimer;

            GameObject codeTextObj = new GameObject("Code_Text");
            codeTextObj.transform.SetParent(lcdPanel.transform, false);
            codeTextObj.transform.localPosition = new Vector3(0f, 0.015f, -0.04f);
            codeTextObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro tmCode = codeTextObj.AddComponent<TextMeshPro>();
            tmCode.text = "----";
            tmCode.fontSize = 2.4f;
            tmCode.alignment = TextAlignmentOptions.Center;
            tmCode.color = Color.yellow;
            bombScript.codeText = tmCode;

            GameObject statusTextObj = new GameObject("Status_Text");
            statusTextObj.transform.SetParent(bombObj.transform, false);
            statusTextObj.transform.localPosition = new Vector3(0f, 0.13f, -0.18f);
            statusTextObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMeshPro tmStatus = statusTextObj.AddComponent<TextMeshPro>();
            tmStatus.text = "<color=red>SOBRETENSIÓN: DESCONECTA B2 EN COCINA</color>";
            tmStatus.fontSize = 1.3f;
            tmStatus.alignment = TextAlignmentOptions.Center;
            bombScript.statusText = tmStatus;

            GameObject strobeObj = new GameObject("Warning_Strobe_Light");
            strobeObj.transform.SetParent(bombObj.transform, false);
            strobeObj.transform.localPosition = new Vector3(-0.30f, 0.16f, 0.18f);
            Light strobe = strobeObj.AddComponent<Light>();
            strobe.type = LightType.Point;
            strobe.color = Color.red;
            strobe.range = 3f;
            strobe.intensity = 2f;
            bombScript.warningFlashLight = strobe;

            GameObject greenLightObj = new GameObject("Defused_Green_Light");
            greenLightObj.transform.SetParent(bombObj.transform, false);
            greenLightObj.transform.localPosition = new Vector3(0.30f, 0.16f, 0.18f);
            Light greenLight = greenLightObj.AddComponent<Light>();
            greenLight.type = LightType.Point;
            greenLight.color = Color.green;
            greenLight.range = 3f;
            greenLight.intensity = 3f;
            greenLight.enabled = false;
            bombScript.defusedGreenLight = greenLight;

            // Teclado Numérico con soporte de Realidad Virtual (Ray + Poke)
            BuildKeypadButtons(bombObj, bombScript, matKeypad);

            // 4 Cables Expuestos con soporte de corte en Realidad Virtual
            BuildBombWires(bombObj, bombScript);

            bombObj.AddComponent<BombAudioSynthesizer>();
            bombScript.audioSynth = bombObj.GetComponent<BombAudioSynthesizer>();
        }

        private static void BuildKeypadButtons(GameObject bombObj, Level3MultiStageBomb bombScript, Material matKeypad)
        {
            GameObject keypadRoot = new GameObject("Keypad_Grid");
            keypadRoot.transform.SetParent(bombObj.transform, false);
            keypadRoot.transform.localPosition = new Vector3(0.18f, 0.13f, 0.05f);

            string[,] layout = new string[,] {
                { "1", "2", "3" },
                { "4", "5", "6" },
                { "7", "8", "9" },
                { "CLR", "0", "ENT" }
            };

            float spacingX = 0.045f;
            float spacingZ = 0.045f;

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    string val = layout[r, c];
                    float px = (c - 1) * spacingX;
                    float pz = -(r - 1.5f) * spacingZ;

                    GameObject btnObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    btnObj.name = $"Keypad_Btn_{val}";
                    btnObj.transform.SetParent(keypadRoot.transform, false);
                    btnObj.transform.localPosition = new Vector3(px, 0f, pz);
                    btnObj.transform.localScale = new Vector3(0.038f, 0.015f, 0.038f);
                    btnObj.GetComponent<Renderer>().sharedMaterial = matKeypad;

                    BombKeypadButton btnScript = btnObj.AddComponent<BombKeypadButton>();
                    btnScript.keyValue = val;

                    // Soporte nativo de Realidad Virtual (Ray Interactor + Direct Poke)
                    XRSimpleInteractable vrInteractable = btnObj.AddComponent<XRSimpleInteractable>();
                    VRBombInteractableBridge vrBridge = btnObj.AddComponent<VRBombInteractableBridge>();
                    vrBridge.isKeypadButton = true;

                    GameObject labelObj = new GameObject("Label");
                    labelObj.transform.SetParent(btnObj.transform, false);
                    labelObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    TextMeshPro tm = labelObj.AddComponent<TextMeshPro>();
                    tm.text = val;
                    tm.fontSize = (val.Length > 1) ? 0.7f : 1.1f;
                    tm.alignment = TextAlignmentOptions.Center;
                    tm.color = (val == "ENT") ? Color.green : ((val == "CLR") ? Color.red : Color.white);
                }
            }
        }

        private static void BuildBombWires(GameObject bombObj, Level3MultiStageBomb bombScript)
        {
            GameObject wiresRoot = new GameObject("Bomb_Wires_Bay");
            wiresRoot.transform.SetParent(bombObj.transform, false);
            wiresRoot.transform.localPosition = new Vector3(-0.16f, 0.13f, -0.10f);

            string[] wireNames = new string[] { "ROJO", "AZUL", "VERDE", "AMARILLO" };
            Color[] wireColors = new Color[] {
                new Color(0.85f, 0.15f, 0.15f),
                new Color(0.15f, 0.45f, 0.95f),
                new Color(0.15f, 0.85f, 0.25f),
                new Color(0.95f, 0.85f, 0.15f)
            };

            for (int i = 0; i < 4; i++)
            {
                string wireName = wireNames[i];
                Color wireColor = wireColors[i];
                float pz = (i - 1.5f) * 0.035f;

                Material matWire = GetOrCreateColoredMat($"Mat_Wire_{wireName}", wireColor, 0.1f, 0.6f);

                GameObject wireObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wireObj.name = $"Wire_{wireName}";
                wireObj.transform.SetParent(wiresRoot.transform, false);
                wireObj.transform.localPosition = new Vector3(0f, 0.012f, pz);
                wireObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wireObj.transform.localScale = new Vector3(0.018f, 0.11f, 0.018f);
                wireObj.GetComponent<Renderer>().sharedMaterial = matWire;

                Level3WireInteractable wireScript = wireObj.AddComponent<Level3WireInteractable>();
                wireScript.wireColor = wireName;

                // Soporte nativo de Realidad Virtual para cortar cables
                wireObj.AddComponent<XRSimpleInteractable>();
                VRBombInteractableBridge vrBridge = wireObj.AddComponent<VRBombInteractableBridge>();
                vrBridge.isBombWire = true;

                GameObject labelObj = new GameObject("Wire_Label");
                labelObj.transform.SetParent(wireObj.transform, false);
                labelObj.transform.localPosition = new Vector3(0f, 0f, -0.6f);
                labelObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                TextMeshPro tm = labelObj.AddComponent<TextMeshPro>();
                tm.text = wireName;
                tm.fontSize = 0.7f;
                tm.alignment = TextAlignmentOptions.Center;
                tm.color = wireColor;

                if (wireName == "ROJO") bombScript.wireRed = wireObj;
                else if (wireName == "AZUL") bombScript.wireBlue = wireObj;
                else if (wireName == "VERDE") bombScript.wireGreen = wireObj;
                else if (wireName == "AMARILLO") bombScript.wireYellow = wireObj;
            }
        }

        private static void BuildMissionDossierSheet(GameObject parent, Vector3 pos)
        {
            Material matPaper = GetOrCreateColoredMat("Mat_Dossier_Paper", new Color(0.96f, 0.94f, 0.88f), 0f, 0.05f);
            Material matClip = GetOrCreateColoredMat("Mat_Clipboard", new Color(0.28f, 0.18f, 0.12f), 0.1f, 0.3f);

            GameObject boardObj = CreateBoxPrimitive(parent, "Level3_Mission_Dossier", pos, new Vector3(0.36f, 0.012f, 0.48f), matClip);
            boardObj.transform.localRotation = Quaternion.Euler(15f, -10f, 0f);

            GameObject paperObj = CreateBoxPrimitive(boardObj, "Paper_Sheet", new Vector3(0f, 0.55f, 0f), new Vector3(0.92f, 0.1f, 0.94f), matPaper, false);

            GameObject textObj = new GameObject("Dossier_Text");
            textObj.transform.SetParent(paperObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            textObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
            tm.text = "<b><size=120%>PROTOCOLO SOBRECARGA // NIVEL 3</size></b>\n" +
                      "<size=80%><color=#8B0000>TIEMPO CRÍTICO: 03:30 MINUTOS</color></size>\n\n" +
                      "<b>1. SOBRETENSIÓN:</b>\n" +
                      "• Dirígete a la <b>COCINA</b> y corta el disyuntor <b>B2</b> en el panel.\n" +
                      "• Agarra el bloque de hielo con la mano y caliéntalo en el <b>MICROONDAS</b> (3s).\n\n" +
                      "<b>2. CORTE DE CABLE:</b>\n" +
                      "• Revisa la <b>TERMINAL PC</b>: indica cortar el <b>CABLE AZUL</b>.\n" +
                      "• ¡Cualquier otro cable detonará la bomba!\n\n" +
                      "<b>3. TECLADO Y CÓDIGO UV:</b>\n" +
                      "• En el <b>CUARTO</b>, agarra la linterna UV y presiona el gatillo.\n" +
                      "• Ilumina la pared para revelar el código e ingrésalo en el teclado numérico.";
            tm.fontSize = 1.15f;
            tm.color = new Color(0.1f, 0.1f, 0.15f);
            tm.rectTransform.sizeDelta = new Vector2(2.4f, 3.2f);
            tm.alignment = TextAlignmentOptions.TopLeft;
        }

        private static void BuildComputerTerminal(GameObject parent, Vector3 pos)
        {
            GameObject termObj = new GameObject("Study_Computer_Terminal");
            termObj.transform.SetParent(parent.transform, false);
            termObj.transform.localPosition = pos;
            termObj.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            Material matMonitor = GetOrCreateColoredMat("Mat_PCMonitor", new Color(0.18f, 0.19f, 0.20f), 0.3f, 0.4f);
            Material matScreen = GetOrCreateColoredMat("Mat_PCScreenCRT", new Color(0.01f, 0.08f, 0.03f), 0f, 0.9f);

            CreateBoxPrimitive(termObj, "Monitor_Stand", new Vector3(0f, 0.08f, 0f), new Vector3(0.18f, 0.16f, 0.14f), matMonitor);
            GameObject screenBox = CreateBoxPrimitive(termObj, "Monitor_Frame", new Vector3(0f, 0.32f, 0f), new Vector3(0.68f, 0.44f, 0.08f), matMonitor);
            GameObject display = CreateBoxPrimitive(screenBox, "Screen_Glass", new Vector3(0f, 0f, 0.52f), new Vector3(0.92f, 0.88f, 0.1f), matScreen);

            GameObject textObj = new GameObject("Screen_Text");
            textObj.transform.SetParent(display.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            textObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
            tm.text = "<color=#00FF66><b>[TERMINAL DE DIAGNÓSTICO BOMB_SYS]</b>\n" +
                      "================================\n" +
                      "ALIMENTACIÓN: SOBRECARGA DETECTADA\n" +
                      "BYPASS OBLIGATORIO: PANEL COCINA -> [B2]\n\n" +
                      "REGISTRO CAPACITOR SECUNDARIO:\n" +
                      ">> CABLE CRÍTICO ACTIVO: <b>[AZUL]</b>\n" +
                      ">> PIN SECUNDARIO: 0x4B\n\n" +
                      "<color=yellow>¡ADVERTENCIA! CORTAR ROJO/VERDE/AMARILLO\n" +
                      "PROVOCARÁ DETONACIÓN INMEDIATA.</color></color>";
            tm.fontSize = 1.05f;
            tm.alignment = TextAlignmentOptions.Center;
            tm.rectTransform.sizeDelta = new Vector2(4.5f, 3.2f);

            Light crtGlow = termObj.AddComponent<Light>();
            crtGlow.type = LightType.Point;
            crtGlow.color = new Color(0.1f, 0.9f, 0.3f);
            crtGlow.range = 2.2f;
            crtGlow.intensity = 1.5f;
        }

        // ==========================================
        // HABITACIÓN 2: COCINA (PANEL VR, HIELO AGARRABLE, MICROONDAS VR)
        // ==========================================
        private static void BuildKitchenContents(GameObject root)
        {
            GameObject kitchenContents = new GameObject("Kitchen_Interactive_Contents");
            kitchenContents.transform.SetParent(root.transform, false);

            Material matCounter = GetOrCreateColoredMat("Mat_KitchenCounterWood", new Color(0.28f, 0.22f, 0.18f), 0.05f, 0.4f);
            Material matFridge = GetOrCreateColoredMat("Mat_KitchenFridgeMetal", new Color(0.85f, 0.86f, 0.88f), 0.3f, 0.5f);

            // 1. Cuadro Eléctrico con interruptores interactivos en RV
            BuildFuseBox(kitchenContents, new Vector3(-9.2f, 1.50f, 0f));

            // 2. Mueble Nevera con estante congelador
            GameObject fridge = CreateBoxPrimitive(kitchenContents, "Kitchen_Refrigerator_Unit", new Vector3(-6.2f, 0.85f, -1.8f), new Vector3(0.85f, 1.70f, 0.75f), matFridge);
            CreateBoxPrimitive(fridge, "Freezer_Shelf", new Vector3(0f, 0.12f, 0.15f), new Vector3(0.70f, 0.04f, 0.50f), matCounter, false);

            // Bloque de Hielo Físico Agarrable con la Mano en Realidad Virtual
            BuildIceBlockPickup(kitchenContents, new Vector3(-6.2f, 1.05f, -1.65f));

            // 3. Encimera de Cocina para el Microondas
            CreateBoxPrimitive(kitchenContents, "Kitchen_Counter_Microwave", new Vector3(-6.2f, 0.425f, 1.8f), new Vector3(1.10f, 0.85f, 0.70f), matCounter);

            // Microondas interactivo en RV
            BuildInteractiveMicrowave(kitchenContents, new Vector3(-6.2f, 1.01f, 1.8f));
        }

        private static void BuildFuseBox(GameObject parent, Vector3 pos)
        {
            GameObject boxObj = new GameObject("FuseBox_Panel");
            boxObj.transform.SetParent(parent.transform, false);
            boxObj.transform.localPosition = pos;
            boxObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            Material matBox = GetOrCreateColoredMat("Mat_FuseBox", new Color(0.22f, 0.23f, 0.25f), 0.5f, 0.4f);
            Material matLever = GetOrCreateColoredMat("Mat_FuseLever", new Color(0.75f, 0.15f, 0.15f), 0.3f, 0.5f);

            CreateBoxPrimitive(boxObj, "Panel_Housing", Vector3.zero, new Vector3(0.55f, 0.65f, 0.12f), matBox);

            FuseBoxInteractable fuseScript = boxObj.AddComponent<FuseBoxInteractable>();

            GameObject titleObj = new GameObject("Panel_Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            titleObj.transform.localPosition = new Vector3(0f, 0.26f, 0.07f);
            titleObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            TextMeshPro tmTitle = titleObj.AddComponent<TextMeshPro>();
            tmTitle.text = "CUADRO ELÉCTRICO";
            tmTitle.fontSize = 1.3f;
            tmTitle.alignment = TextAlignmentOptions.Center;
            tmTitle.color = Color.white;

            string[] switches = new string[] { "A1", "B2", "C3", "D4" };
            for (int i = 0; i < 4; i++)
            {
                string sId = switches[i];
                float px = (i - 1.5f) * 0.12f;

                GameObject lever = CreateBoxPrimitive(boxObj, $"Switch_{sId}", new Vector3(px, 0.02f, 0.07f), new Vector3(0.06f, 0.16f, 0.05f), matLever);
                Level3FuseSwitchInteractable switchComp = lever.AddComponent<Level3FuseSwitchInteractable>();
                switchComp.switchId = sId;

                // Soporte nativo de Realidad Virtual para interruptores (Ray o Toque Directo)
                lever.AddComponent<XRSimpleInteractable>();
                VRBombInteractableBridge vrBridge = lever.AddComponent<VRBombInteractableBridge>();
                vrBridge.isFuseSwitch = true;

                GameObject lbl = new GameObject("Lbl");
                lbl.transform.SetParent(lever.transform, false);
                lbl.transform.localPosition = new Vector3(0f, -0.6f, 0.6f);
                lbl.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                TextMeshPro tmLbl = lbl.AddComponent<TextMeshPro>();
                tmLbl.text = sId;
                tmLbl.fontSize = 1.8f;
                tmLbl.alignment = TextAlignmentOptions.Center;
                tmLbl.color = (sId == "B2") ? Color.red : Color.yellow;

                if (sId == "A1") fuseScript.leverA1 = lever.transform;
                else if (sId == "B2") fuseScript.leverB2 = lever.transform;
                else if (sId == "C3") fuseScript.leverC3 = lever.transform;
                else if (sId == "D4") fuseScript.leverD4 = lever.transform;
            }

            GameObject ledObj = new GameObject("Status_LED");
            ledObj.transform.SetParent(boxObj.transform, false);
            ledObj.transform.localPosition = new Vector3(0f, -0.22f, 0.07f);
            Light led = ledObj.AddComponent<Light>();
            led.type = LightType.Point;
            led.color = Color.red;
            led.range = 1.5f;
            led.intensity = 2f;
            fuseScript.statusLed = led;
        }

        private static void BuildIceBlockPickup(GameObject parent, Vector3 pos)
        {
            Material matIce = GetOrCreateColoredMat("Mat_IceBlock", new Color(0.7f, 0.9f, 1f, 0.75f), 0.1f, 0.95f);
            Material matPliers = GetOrCreateColoredMat("Mat_PliersMetal", new Color(0.8f, 0.3f, 0.2f), 0.8f, 0.5f);

            GameObject iceObj = CreateBoxPrimitive(parent, "Pickup_FrozenIceBlock", pos, new Vector3(0.24f, 0.20f, 0.24f), matIce);
            Level3ItemPickup pickup = iceObj.AddComponent<Level3ItemPickup>();
            pickup.itemType = Level3ItemType.FrozenIceBlock;
            pickup.itemName = "Bloque de Hielo con Alicates Congelados";

            // Soporte de Agarre con la Mano en Realidad Virtual (6DoF Grab)
            iceObj.AddComponent<XRGrabInteractable>();
            var grabHandler = iceObj.AddComponent<VRItemGrabHandler>();
            grabHandler.itemType = Level3ItemType.FrozenIceBlock;

            CreateBoxPrimitive(iceObj, "Frozen_Pliers_Inside", Vector3.zero, new Vector3(0.5f, 0.8f, 0.3f), matPliers, false);

            Light iceLight = iceObj.AddComponent<Light>();
            iceLight.type = LightType.Point;
            iceLight.color = new Color(0.4f, 0.8f, 1f);
            iceLight.range = 1.2f;
            iceLight.intensity = 1f;
        }

        private static void BuildInteractiveMicrowave(GameObject parent, Vector3 pos)
        {
            GameObject microObj = new GameObject("Interactive_Microwave");
            microObj.transform.SetParent(parent.transform, false);
            microObj.transform.localPosition = pos;
            microObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Material matBody = GetOrCreateColoredMat("Mat_MicrowaveBody", new Color(0.85f, 0.86f, 0.88f), 0.2f, 0.6f);
            Material matWindow = GetOrCreateColoredMat("Mat_MicrowaveWindow", new Color(0.1f, 0.1f, 0.12f), 0.1f, 0.9f);
            Material matIce = GetOrCreateColoredMat("Mat_IceBlock", new Color(0.7f, 0.9f, 1f), 0.1f, 0.95f);
            Material matPliers = GetOrCreateColoredMat("Mat_PliersMetal", new Color(0.8f, 0.3f, 0.2f), 0.8f, 0.5f);

            CreateBoxPrimitive(microObj, "Body", Vector3.zero, new Vector3(0.54f, 0.32f, 0.36f), matBody);
            CreateBoxPrimitive(microObj, "Door_Glass", new Vector3(-0.06f, 0f, 0.19f), new Vector3(0.36f, 0.24f, 0.02f), matWindow, false);

            InteractiveMicrowave mwScript = microObj.AddComponent<InteractiveMicrowave>();

            // Soporte de Interacción en Realidad Virtual para activar el microondas
            microObj.AddComponent<XRSimpleInteractable>();
            VRBombInteractableBridge vrBridge = microObj.AddComponent<VRBombInteractableBridge>();
            vrBridge.isMicrowave = true;

            GameObject display = CreateBoxPrimitive(microObj, "Timer_Display", new Vector3(0.19f, 0.06f, 0.19f), new Vector3(0.10f, 0.06f, 0.02f), matWindow, false);
            GameObject textObj = new GameObject("Display_Text");
            textObj.transform.SetParent(display.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            textObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
            tm.text = "00:03";
            tm.fontSize = 1.3f;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = Color.green;
            mwScript.screenTimerText = tm;

            GameObject insideIce = CreateBoxPrimitive(microObj, "Ice_Inside", new Vector3(-0.06f, 0f, 0f), new Vector3(0.18f, 0.15f, 0.18f), matIce, false);
            insideIce.SetActive(false);
            mwScript.frozenIceVisual = insideIce;

            GameObject resultPliers = CreateBoxPrimitive(microObj, "Pliers_Thawed", new Vector3(-0.06f, 0.02f, 0f), new Vector3(0.14f, 0.04f, 0.24f), matPliers, true);
            resultPliers.SetActive(false);
            mwScript.pliersResultVisual = resultPliers;

            // Soporte de agarre de las pinzas al descongelarse
            resultPliers.AddComponent<XRGrabInteractable>();
            var pliersGrab = resultPliers.AddComponent<VRItemGrabHandler>();
            pliersGrab.itemType = Level3ItemType.Pliers;

            GameObject lightObj = new GameObject("Interior_Light");
            lightObj.transform.SetParent(microObj.transform, false);
            lightObj.transform.localPosition = new Vector3(-0.06f, 0.08f, 0f);
            Light inLight = lightObj.AddComponent<Light>();
            inLight.type = LightType.Point;
            inLight.color = new Color(1f, 0.9f, 0.6f);
            inLight.range = 1.5f;
            inLight.intensity = 2f;
            inLight.enabled = false;
            mwScript.interiorLight = lightObj;
        }

        // ==========================================
        // HABITACIÓN 3: CUARTO (LINTERNA UV AGARRABLE 6DOF, MARCADOR)
        // ==========================================
        private static void BuildBedroomContents(GameObject root)
        {
            GameObject bedroomContents = new GameObject("Bedroom_Interactive_Contents");
            bedroomContents.transform.SetParent(root.transform, false);

            Material matWood = GetOrCreateColoredMat("Mat_NightstandWood", new Color(0.32f, 0.22f, 0.15f), 0.05f, 0.35f);

            GameObject nightstand = CreateBoxPrimitive(bedroomContents, "Bedroom_Nightstand_Wood", new Vector3(6.2f, 0.38f, 1.6f), new Vector3(0.65f, 0.76f, 0.65f), matWood);

            GameObject lampBase = CreateBoxPrimitive(nightstand, "Lamp_Base", new Vector3(0.18f, 0.42f, -0.18f), new Vector3(0.14f, 0.08f, 0.14f), matWood, false);
            Material matLampShade = GetOrCreateColoredMat("Mat_LampShade", new Color(0.95f, 0.92f, 0.85f), 0f, 0.1f);
            GameObject lampShade = CreateBoxPrimitive(lampBase, "Lamp_Shade", new Vector3(0f, 0.15f, 0f), new Vector3(0.20f, 0.22f, 0.20f), matLampShade, false);

            Light lampLight = lampShade.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.color = new Color(1f, 0.85f, 0.65f);
            lampLight.range = 2.5f;
            lampLight.intensity = 1.2f;

            // Linterna UV Agarrable en Realidad Virtual con gatillo activador
            BuildUVFlashlightPickup(bedroomContents, new Vector3(6.2f, 0.79f, 1.6f));

            // Marcador UV secreto en la pared
            BuildUVSecretMarker(bedroomContents, new Vector3(6.8f, 1.65f, 2.55f));
        }

        private static void BuildUVFlashlightPickup(GameObject parent, Vector3 pos)
        {
            GameObject uvObj = new GameObject("Pickup_UVFlashlight");
            uvObj.transform.SetParent(parent.transform, false);
            uvObj.transform.localPosition = pos;
            uvObj.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            Material matBody = GetOrCreateColoredMat("Mat_UVFlashlightBody", new Color(0.15f, 0.15f, 0.18f), 0.6f, 0.6f);
            Material matGlow = GetOrCreateColoredMat("Mat_UVLens", new Color(0.5f, 0.1f, 0.9f), 0.2f, 0.9f);

            GameObject body = CreateBoxPrimitive(uvObj, "Casing", Vector3.zero, new Vector3(0.06f, 0.06f, 0.22f), matBody);
            CreateBoxPrimitive(uvObj, "Bezel", new Vector3(0f, 0f, 0.11f), new Vector3(0.08f, 0.08f, 0.04f), matGlow, false);

            Level3ItemPickup pickup = uvObj.AddComponent<Level3ItemPickup>();
            pickup.itemType = Level3ItemType.UVFlashlight;
            pickup.itemName = "Linterna Ultravioleta Táctica";

            // Luz UV física acoplada a la linterna para apuntar con la muñeca en RV
            GameObject spotObj = new GameObject("UV_Torch_Spot");
            spotObj.transform.SetParent(uvObj.transform, false);
            spotObj.transform.localPosition = new Vector3(0f, 0f, 0.14f);
            spotObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Light tipLight = spotObj.AddComponent<Light>();
            tipLight.type = LightType.Spot;
            tipLight.color = new Color(0.6f, 0.1f, 1.0f);
            tipLight.spotAngle = 60f;
            tipLight.range = 8.5f;
            tipLight.intensity = 4.5f;
            tipLight.enabled = false;

            // Agarre de Realidad Virtual: presionar Grip para levantar, Trigger para encender/apagar luz UV
            uvObj.AddComponent<XRGrabInteractable>();
            var grabHandler = uvObj.AddComponent<VRItemGrabHandler>();
            grabHandler.itemType = Level3ItemType.UVFlashlight;
            grabHandler.uvLightSource = tipLight;
        }

        private static void BuildUVSecretMarker(GameObject parent, Vector3 pos)
        {
            GameObject markerObj = new GameObject("UV_Wall_Secret_Marker");
            markerObj.transform.SetParent(parent.transform, false);
            markerObj.transform.localPosition = pos;
            markerObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            UVClueMarker markerScript = markerObj.AddComponent<UVClueMarker>();
            markerScript.clueText = "7 5 3 1";
            markerScript.labelDescription = "CÓDIGO BOMBA: [ 7 5 3 1 ]";

            GameObject textObj = new GameObject("UV_Fluorescent_Text");
            textObj.transform.SetParent(markerObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            textObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
            tm.text = "7  5  3  1";
            tm.fontSize = 3.6f;
            tm.alignment = TextAlignmentOptions.Center;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(0.1f, 1.0f, 0.6f, 0.0f);
            markerScript.textMesh = tm;
        }

        // ==========================================
        // ILUMINACIÓN Y ATMÓSFERA
        // ==========================================
        private static void SetupAtmosphere(GameObject root)
        {
            GameObject lightRig = new GameObject("Lighting_Rig_Level3");
            lightRig.transform.SetParent(root.transform, false);

            GameObject moonObj = new GameObject("Moon_Directional_Light");
            moonObj.transform.SetParent(lightRig.transform, false);
            moonObj.transform.localRotation = Quaternion.Euler(50f, -35f, 0f);
            Light moon = moonObj.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.65f, 0.85f);
            moon.intensity = 0.5f;

            CreatePointLight(lightRig, "Light_Corridor_Center", new Vector3(0f, 2.3f, 0f), new Color(1f, 0.85f, 0.6f), 2.0f, 6f);
            CreatePointLight(lightRig, "Light_Kitchen", new Vector3(-6.8f, 2.3f, 0f), new Color(1f, 0.9f, 0.75f), 2.2f, 7f);
            CreatePointLight(lightRig, "Light_Study", new Vector3(0f, 2.3f, -3.5f), new Color(1f, 0.8f, 0.55f), 2.4f, 7f);
            CreatePointLight(lightRig, "Light_Bedroom", new Vector3(6.8f, 2.3f, 0f), new Color(0.9f, 0.85f, 1f), 2.0f, 7f);
        }

        private static void CreatePointLight(GameObject parent, string name, Vector3 pos, Color col, float intensity, float range)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = pos;
            Light l = obj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.intensity = intensity;
            l.range = range;
        }

        // ==========================================
        // RIG DE JUGADOR DUAL: REALIDAD VIRTUAL + ESCRITORIO
        // ==========================================
        private static void SetupPlayerSystems(GameObject root, GameObject spatialHUD)
        {
            GameObject playerSystem = new GameObject("Player_Rig_System");
            playerSystem.transform.SetParent(root.transform, false);
            playerSystem.transform.position = new Vector3(0f, 0.05f, 0f);
            playerSystem.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            VRPlayerRigManager rigMgr = playerSystem.AddComponent<VRPlayerRigManager>();
            rigMgr.spatialHUD = spatialHUD;
            rigMgr.forceVRMode = false;

            // 1. Instanciar Rig de Realidad Virtual oficial (con mandos Quest y carteles de ayuda flotantes)
            GameObject vrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VR_RIG_PREFAB);
            if (vrPrefab == null)
            {
                vrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VR_RIG_BACKUP);
            }

            GameObject vrRigInstance = null;
            if (vrPrefab != null)
            {
                vrRigInstance = Object.Instantiate(vrPrefab, playerSystem.transform);
                vrRigInstance.name = "XR Origin (VR Rig)";
                vrRigInstance.transform.localPosition = Vector3.zero;
                vrRigInstance.transform.localRotation = Quaternion.identity;
                rigMgr.vrOriginRig = vrRigInstance;
            }
            else
            {
                Debug.LogWarning("[Level3Builder] No se encontró el prefab de VR Rig. Creando rig base.");
            }

            // 2. Crear Rig de Escritorio FPV de respaldo
            GameObject desktopPlayer = new GameObject("Player_FPV_Desktop");
            desktopPlayer.transform.SetParent(playerSystem.transform, false);
            desktopPlayer.transform.localPosition = Vector3.zero;
            desktopPlayer.transform.localRotation = Quaternion.identity;
            rigMgr.desktopFpvRig = desktopPlayer;

            CharacterController cc = desktopPlayer.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.875f, 0f);
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0.001f;

            GameObject camObj = new GameObject("FirstPersonCamera");
            camObj.transform.SetParent(desktopPlayer.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camObj.transform.localRotation = Quaternion.identity;
            camObj.tag = "MainCamera";

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.AddComponent<AudioListener>();

            FPSRaycastInteractor interactor = camObj.AddComponent<FPSRaycastInteractor>();
            interactor.interactDistance = 3.5f;

            desktopPlayer.AddComponent<PlayerInventory>();

            FirstPersonController fpc = desktopPlayer.AddComponent<FirstPersonController>();
            fpc.cameraTransform = camObj.transform;
            fpc.walkSpeed = 2.6f;
            fpc.runSpeed = 4.5f;
            fpc.mouseSensitivity = 0.12f;

            Level3HUD hud = desktopPlayer.AddComponent<Level3HUD>();
            hud.playerCamera = cam;

            // Configurar selección activa de acuerdo a presencia de casco
            rigMgr.ApplyMode();

            Selection.activeGameObject = playerSystem;
        }

        // ==========================================
        // HUD ESPACIAL EN 3D PARA REALIDAD VIRTUAL
        // ==========================================
        private static GameObject BuildVRSpatialHUD(GameObject root)
        {
            GameObject hudObj = new GameObject("VR_Spatial_HUD");
            hudObj.transform.SetParent(root.transform, false);
            hudObj.transform.position = new Vector3(0f, 1.65f, -1.8f);
            hudObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Material matHUDCard = GetOrCreateColoredMat("Mat_VRHUD_Card", new Color(0.02f, 0.05f, 0.08f, 0.65f), 0.1f, 0.8f);

            CreateBoxPrimitive(hudObj, "Backing_Plate", new Vector3(0f, 0f, 0.015f), new Vector3(1.35f, 0.65f, 0.02f), matHUDCard, false);

            VRSpatialHUD hudComp = hudObj.AddComponent<VRSpatialHUD>();
            hudComp.offset = new Vector3(0f, 0.40f, 2.2f);
            hudComp.followSpeed = 4.5f;
            hudComp.faceCamera = true;
            hudComp.lockPitch = true;

            // 1. Cronómetro
            GameObject timerObj = new GameObject("Timer_Display");
            timerObj.transform.SetParent(hudObj.transform, false);
            timerObj.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            TextMeshPro tmTimer = timerObj.AddComponent<TextMeshPro>();
            tmTimer.text = "⏱ 03:30.00";
            tmTimer.fontSize = 2.2f;
            tmTimer.alignment = TextAlignmentOptions.Center;
            tmTimer.color = Color.yellow;
            hudComp.timerText = tmTimer;

            // 2. Fases
            GameObject p1Obj = new GameObject("Phase1_Badge");
            p1Obj.transform.SetParent(hudObj.transform, false);
            p1Obj.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            TextMeshPro tmP1 = p1Obj.AddComponent<TextMeshPro>();
            tmP1.text = "✖ [1. SOBRETENSIÓN: PANEL COCINA -> B2]";
            tmP1.fontSize = 1.0f;
            tmP1.alignment = TextAlignmentOptions.Center;
            hudComp.phase1Badge = tmP1;

            GameObject p2Obj = new GameObject("Phase2_Badge");
            p2Obj.transform.SetParent(hudObj.transform, false);
            p2Obj.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            TextMeshPro tmP2 = p2Obj.AddComponent<TextMeshPro>();
            tmP2.text = "🔒 [2. CABLEADO: REVISA PC -> CORTA AZUL]";
            tmP2.fontSize = 1.0f;
            tmP2.alignment = TextAlignmentOptions.Center;
            hudComp.phase2Badge = tmP2;

            GameObject p3Obj = new GameObject("Phase3_Badge");
            p3Obj.transform.SetParent(hudObj.transform, false);
            p3Obj.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            TextMeshPro tmP3 = p3Obj.AddComponent<TextMeshPro>();
            tmP3.text = "🔒 [3. TECLADO UV: BUSCA CÓDIGO EN CUARTO]";
            tmP3.fontSize = 1.0f;
            tmP3.alignment = TextAlignmentOptions.Center;
            hudComp.phase3Badge = tmP3;

            // 3. Inventario
            GameObject invObj = new GameObject("Inventory_Badge");
            invObj.transform.SetParent(hudObj.transform, false);
            invObj.transform.localPosition = new Vector3(0f, -0.24f, 0f);
            TextMeshPro tmInv = invObj.AddComponent<TextMeshPro>();
            tmInv.text = "MOCHILA RV: [HIELO]  [PINZAS]  [LINTERNA UV]";
            tmInv.fontSize = 0.85f;
            tmInv.alignment = TextAlignmentOptions.Center;
            hudComp.inventoryBadge = tmInv;

            hudObj.SetActive(false);
            return hudObj;
        }

        // ==========================================
        // HELPERS GEOMETRÍA PRIMITIVA
        // ==========================================
        private static GameObject CreateFloor(GameObject parent, string name, Vector3 pos, Vector2 size, Material mat)
        {
            GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = name;
            f.transform.SetParent(parent.transform, false);
            f.transform.localPosition = new Vector3(pos.x, pos.y - 0.02f, pos.z);
            f.transform.localScale = new Vector3(size.x, 0.04f, size.y);
            if (mat != null) f.GetComponent<Renderer>().sharedMaterial = mat;

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
            if (mat != null) c.GetComponent<Renderer>().sharedMaterial = mat;
            return c;
        }

        private static GameObject CreateWall(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            w.transform.SetParent(parent.transform, false);
            w.transform.localPosition = pos;
            w.transform.localScale = size;
            if (mat != null) w.GetComponent<Renderer>().sharedMaterial = mat;
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
