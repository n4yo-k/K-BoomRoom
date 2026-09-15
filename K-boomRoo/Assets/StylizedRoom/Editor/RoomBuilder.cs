using UnityEngine;
using UnityEditor;
using System.IO;

namespace StylizedRoom
{
    public class RoomBuilder : EditorWindow
    {
        private const string ROOT = "Assets/StylizedRoom";
        private const string TEX_PATH = ROOT + "/Textures";
        private const string MAT_PATH = ROOT + "/Materials";
        private const string PREFAB_PATH = ROOT + "/Prefabs";

        [MenuItem("Stylized Room/Rebuild Complete Room (First Person WASD)", false, 1)]
        public static void RebuildRoomFirstPerson()
        {
            SetupMaterials();
            GameObject room = BuildRoomHierarchy();
            SetupLightingAndEnvironment(room);
            SetupFirstPersonPlayer(room);

            // Guardar Prefab
            Directory.CreateDirectory(PREFAB_PATH);
            string prefabFile = $"{PREFAB_PATH}/StylizedHauntedRoom_FPV.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(room, prefabFile, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = room;
            SceneView.FrameLastActiveSceneView();
            Debug.Log("[StylizedRoom] ¡Habitación lista para explorar en Primera Persona con WASD!");
            EditorUtility.DisplayDialog("Modo Primera Persona Listo", "¡Habitación configurada con éxito!\n\n- Cámara en primera persona (altura de los ojos: 1.65m).\n- Controles: WASD para moverte, Ratón para mirar, Shift para correr, Escape para liberar el cursor.\n- Habitación cerrada con colisionadores.\n\n¡Dale al botón PLAY para caminar adentro!", "¡Entendido!");
        }

        [MenuItem("Stylized Room/Setup Materials Only", false, 10)]
        public static void SetupMaterials()
        {
            AssetDatabase.Refresh();
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            CreateOrUpdateMaterial("Mat_Floor", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX_PATH}/Floor_BaseMap.png");
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.22f);
                mat.SetFloat("_Metallic", 0.0f);
            });

            CreateOrUpdateMaterial("Mat_LeftWall", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX_PATH}/LeftWall_BaseMap.png");
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.15f);
                mat.SetFloat("_Metallic", 0.0f);
            });

            CreateOrUpdateMaterial("Mat_RightWall", litShader, mat =>
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX_PATH}/RightWall_BaseMap.png");
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", 0.18f);
                mat.SetFloat("_Metallic", 0.0f);
            });

            CreateOrUpdateMaterial("Mat_WoodTrim", litShader, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.24f, 0.16f, 0.12f));
                mat.SetFloat("_Smoothness", 0.35f);
                mat.SetFloat("_Metallic", 0.0f);
            });

            CreateOrUpdateMaterial("Mat_BrassKnob", litShader, mat =>
            {
                mat.SetColor("_BaseColor", new Color(0.88f, 0.68f, 0.32f));
                mat.SetFloat("_Smoothness", 0.85f);
                mat.SetFloat("_Metallic", 0.90f);
            });

            AssetDatabase.SaveAssets();
        }

        private static void CreateOrUpdateMaterial(string name, Shader shader, System.Action<Material> config)
        {
            string path = $"{MAT_PATH}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                config(mat);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
                config(mat);
                EditorUtility.SetDirty(mat);
            }
        }

        private static GameObject BuildRoomHierarchy()
        {
            GameObject old = GameObject.Find("Stylized_HauntedRoom");
            if (old != null) Undo.DestroyObjectImmediate(old);

            GameObject root = new GameObject("Stylized_HauntedRoom");
            Undo.RegisterCreatedObjectUndo(root, "Rebuild Stylized Room");

            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_Floor.mat");
            Material matLeftWall = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_LeftWall.mat");
            Material matRightWall = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_RightWall.mat");
            Material matTrim = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_WoodTrim.mat");
            Material matKnob = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_PATH}/Mat_BrassKnob.mat");

            // 1. PISO (4x4m)
            GameObject floorObj = new GameObject("Floor");
            floorObj.transform.SetParent(root.transform, false);
            MeshFilter mfFloor = floorObj.AddComponent<MeshFilter>();
            MeshRenderer mrFloor = floorObj.AddComponent<MeshRenderer>();
            mfFloor.sharedMesh = CreateQuadMesh(
                new Vector3(-2.0f, 0.0f, -2.0f),
                new Vector3(-2.0f, 0.0f,  2.0f),
                new Vector3( 2.0f, 0.0f,  2.0f),
                new Vector3( 2.0f, 0.0f, -2.0f),
                Vector3.up,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            );
            mrFloor.sharedMaterial = matFloor;
            BoxCollider colFloor = floorObj.AddComponent<BoxCollider>();
            colFloor.size = new Vector3(4.0f, 0.04f, 4.0f);
            colFloor.center = new Vector3(0.0f, -0.02f, 0.0f);

            // 2. PARED IZQUIERDA (con Puerta y Papel Rasgado)
            GameObject leftWallObj = new GameObject("Wall_Left_Door");
            leftWallObj.transform.SetParent(root.transform, false);
            MeshFilter mfLeft = leftWallObj.AddComponent<MeshFilter>();
            MeshRenderer mrLeft = leftWallObj.AddComponent<MeshRenderer>();
            mfLeft.sharedMesh = CreateQuadMesh(
                new Vector3(-2.0f, 0.0f, -2.0f),
                new Vector3(-2.0f, 0.0f,  2.0f),
                new Vector3(-2.0f, 2.8f,  2.0f),
                new Vector3(-2.0f, 2.8f, -2.0f),
                Vector3.right,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            );
            mrLeft.sharedMaterial = matLeftWall;
            BoxCollider colLeft = leftWallObj.AddComponent<BoxCollider>();
            colLeft.size = new Vector3(0.1f, 2.8f, 4.0f);
            colLeft.center = new Vector3(-2.05f, 1.4f, 0.0f);

            // 3. PARED TRASERA/DERECHA (con Ventana, Luna y Ramas)
            GameObject rightWallObj = new GameObject("Wall_Right_Window");
            rightWallObj.transform.SetParent(root.transform, false);
            MeshFilter mfRight = rightWallObj.AddComponent<MeshFilter>();
            MeshRenderer mrRight = rightWallObj.AddComponent<MeshRenderer>();
            mfRight.sharedMesh = CreateQuadMesh(
                new Vector3(-2.0f, 0.0f, 2.0f),
                new Vector3( 2.0f, 0.0f, 2.0f),
                new Vector3( 2.0f, 2.8f, 2.0f),
                new Vector3(-2.0f, 2.8f, 2.0f),
                Vector3.back,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            );
            mrRight.sharedMaterial = matRightWall;
            BoxCollider colRight = rightWallObj.AddComponent<BoxCollider>();
            colRight.size = new Vector3(4.0f, 2.8f, 0.1f);
            colRight.center = new Vector3(0.0f, 1.4f, 2.05f);

            // 4. PARED FRONTAL OPUESTA (Z = -2.0m) para cerrar el cuarto en 1ra persona
            GameObject frontWallZ = new GameObject("Wall_Front_Z");
            frontWallZ.transform.SetParent(root.transform, false);
            MeshFilter mfFrontZ = frontWallZ.AddComponent<MeshFilter>();
            MeshRenderer mrFrontZ = frontWallZ.AddComponent<MeshRenderer>();
            mfFrontZ.sharedMesh = CreateQuadMesh(
                new Vector3( 2.0f, 0.0f, -2.0f),
                new Vector3(-2.0f, 0.0f, -2.0f),
                new Vector3(-2.0f, 2.8f, -2.0f),
                new Vector3( 2.0f, 2.8f, -2.0f),
                Vector3.forward,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            );
            mrFrontZ.sharedMaterial = matRightWall;
            BoxCollider colFrontZ = frontWallZ.AddComponent<BoxCollider>();
            colFrontZ.size = new Vector3(4.0f, 2.8f, 0.1f);
            colFrontZ.center = new Vector3(0.0f, 1.4f, -2.05f);

            // 5. PARED LATERAL OPUESTA (X = 2.0m) para cerrar el cuarto en 1ra persona
            GameObject frontWallX = new GameObject("Wall_Front_X");
            frontWallX.transform.SetParent(root.transform, false);
            MeshFilter mfFrontX = frontWallX.AddComponent<MeshFilter>();
            MeshRenderer mrFrontX = frontWallX.AddComponent<MeshRenderer>();
            mfFrontX.sharedMesh = CreateQuadMesh(
                new Vector3(2.0f, 0.0f,  2.0f),
                new Vector3(2.0f, 0.0f, -2.0f),
                new Vector3(2.0f, 2.8f, -2.0f),
                new Vector3(2.0f, 2.8f,  2.0f),
                Vector3.left,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            );
            mrFrontX.sharedMaterial = matLeftWall;
            BoxCollider colFrontX = frontWallX.AddComponent<BoxCollider>();
            colFrontX.size = new Vector3(0.1f, 2.8f, 4.0f);
            colFrontX.center = new Vector3(2.05f, 1.4f, 0.0f);

            // 6. TECHO (Y = 2.8m)
            GameObject ceilingObj = new GameObject("Ceiling");
            ceilingObj.transform.SetParent(root.transform, false);
            MeshFilter mfCeil = ceilingObj.AddComponent<MeshFilter>();
            MeshRenderer mrCeil = ceilingObj.AddComponent<MeshRenderer>();
            mfCeil.sharedMesh = CreateQuadMesh(
                new Vector3(-2.0f, 2.8f,  2.0f),
                new Vector3(-2.0f, 2.8f, -2.0f),
                new Vector3( 2.0f, 2.8f, -2.0f),
                new Vector3( 2.0f, 2.8f,  2.0f),
                Vector3.down,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            );
            mrCeil.sharedMaterial = matTrim;

            // ==========================================
            // 7. MOLDURAS 3D PERIMETRALES
            // ==========================================
            GameObject trimRoot = new GameObject("3D_Moldings");
            trimRoot.transform.SetParent(root.transform, false);

            // Cornisas de techo perimetrales
            CreateBox(trimRoot, "Crown_Left", new Vector3(-1.96f, 2.76f, 0.0f), new Vector3(0.08f, 0.08f, 4.04f), matTrim);
            CreateBox(trimRoot, "Crown_Right", new Vector3(0.0f, 2.76f, 1.96f), new Vector3(4.04f, 0.08f, 0.08f), matTrim);
            CreateBox(trimRoot, "Crown_FrontZ", new Vector3(0.0f, 2.76f, -1.96f), new Vector3(4.04f, 0.08f, 0.08f), matTrim);
            CreateBox(trimRoot, "Crown_FrontX", new Vector3(1.96f, 2.76f, 0.0f), new Vector3(0.08f, 0.08f, 4.04f), matTrim);

            // Chair Rails (moldura intermedia a 1m)
            CreateBox(trimRoot, "ChairRail_Left", new Vector3(-1.98f, 0.96f, 0.0f), new Vector3(0.04f, 0.06f, 4.02f), matTrim);
            CreateBox(trimRoot, "ChairRail_Right", new Vector3(0.0f, 0.96f, 1.98f), new Vector3(4.02f, 0.06f, 0.04f), matTrim);
            CreateBox(trimRoot, "ChairRail_FrontZ", new Vector3(0.0f, 0.96f, -1.98f), new Vector3(4.02f, 0.06f, 0.04f), matTrim);
            CreateBox(trimRoot, "ChairRail_FrontX", new Vector3(1.98f, 0.96f, 0.0f), new Vector3(0.04f, 0.06f, 4.02f), matTrim);

            // Rodapiés base inferior
            CreateBox(trimRoot, "Baseboard_Left", new Vector3(-1.98f, 0.06f, 0.0f), new Vector3(0.04f, 0.12f, 4.02f), matTrim);
            CreateBox(trimRoot, "Baseboard_Right", new Vector3(0.0f, 0.06f, 1.98f), new Vector3(4.02f, 0.12f, 0.04f), matTrim);
            CreateBox(trimRoot, "Baseboard_FrontZ", new Vector3(0.0f, 0.06f, -1.98f), new Vector3(4.02f, 0.12f, 0.04f), matTrim);
            CreateBox(trimRoot, "Baseboard_FrontX", new Vector3(1.98f, 0.06f, 0.0f), new Vector3(0.04f, 0.12f, 4.02f), matTrim);

            // ==========================================
            // 8. RELIEVES 3D DE PUERTA Y VENTANA
            // ==========================================
            GameObject detailsRoot = new GameObject("3D_Features");
            detailsRoot.transform.SetParent(root.transform, false);

            float doorZ = -0.32f;
            CreateBox(detailsRoot, "Door_Header_Trim", new Vector3(-1.96f, 2.15f, doorZ), new Vector3(0.08f, 0.12f, 1.05f), matTrim);
            CreateBox(detailsRoot, "Door_Cornice_Trim", new Vector3(-1.94f, 2.22f, doorZ), new Vector3(0.12f, 0.06f, 1.10f), matTrim);
            CreateBox(detailsRoot, "Door_Jamb_Left", new Vector3(-1.98f, 1.05f, doorZ - 0.46f), new Vector3(0.04f, 2.10f, 0.08f), matTrim);
            CreateBox(detailsRoot, "Door_Jamb_Right", new Vector3(-1.98f, 1.05f, doorZ + 0.46f), new Vector3(0.04f, 2.10f, 0.08f), matTrim);

            GameObject knobObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knobObj.name = "Door_Brass_Knob";
            knobObj.transform.SetParent(detailsRoot.transform, false);
            knobObj.transform.localPosition = new Vector3(-1.95f, 0.98f, doorZ + 0.32f);
            knobObj.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            knobObj.GetComponent<Renderer>().sharedMaterial = matKnob;

            float winX = 0.50f;
            CreateBox(detailsRoot, "Window_Sill", new Vector3(winX, 0.88f, 1.92f), new Vector3(1.36f, 0.08f, 0.16f), matTrim);
            CreateBox(detailsRoot, "Window_Header", new Vector3(winX, 2.28f, 1.95f), new Vector3(1.25f, 0.10f, 0.10f), matTrim);
            CreateBox(detailsRoot, "Window_Cornice", new Vector3(winX, 2.34f, 1.94f), new Vector3(1.32f, 0.06f, 0.12f), matTrim);
            CreateBox(detailsRoot, "Window_Jamb_Left", new Vector3(winX - 0.56f, 1.58f, 1.96f), new Vector3(0.10f, 1.30f, 0.08f), matTrim);
            CreateBox(detailsRoot, "Window_Jamb_Right", new Vector3(winX + 0.56f, 1.58f, 1.96f), new Vector3(0.10f, 1.30f, 0.08f), matTrim);
            CreateBox(detailsRoot, "Window_Mullion_Vertical", new Vector3(winX, 1.58f, 1.97f), new Vector3(0.04f, 1.30f, 0.06f), matTrim);
            CreateBox(detailsRoot, "Window_Mullion_H1", new Vector3(winX, 1.25f, 1.97f), new Vector3(1.10f, 0.035f, 0.06f), matTrim);
            CreateBox(detailsRoot, "Window_Mullion_H2", new Vector3(winX, 1.58f, 1.97f), new Vector3(1.10f, 0.035f, 0.06f), matTrim);
            CreateBox(detailsRoot, "Window_Mullion_H3", new Vector3(winX, 1.91f, 1.97f), new Vector3(1.10f, 0.035f, 0.06f), matTrim);

            return root;
        }

        private static void CreateBox(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = pos;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = mat;
            DestroyImmediate(box.GetComponent<Collider>());
        }

        private static Mesh CreateQuadMesh(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] { p0, p1, p2, p3 };
            mesh.uv = new Vector2[] { uv0, uv1, uv2, uv3 };
            mesh.normals = new Vector3[] { normal, normal, normal, normal };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void SetupLightingAndEnvironment(GameObject room)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.38f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.26f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.14f, 0.12f);
            RenderSettings.ambientIntensity = 1.0f;

            Transform oldLights = room.transform.Find("Lighting");
            if (oldLights != null) DestroyImmediate(oldLights.gameObject);

            GameObject lightGroup = new GameObject("Lighting");
            lightGroup.transform.SetParent(room.transform, false);

            // Luz de la luna atravesando la ventana
            GameObject moonLightObj = new GameObject("Moonlight_Directional");
            moonLightObj.transform.SetParent(lightGroup.transform, false);
            moonLightObj.transform.position = new Vector3(2.0f, 4.0f, 3.8f);
            moonLightObj.transform.rotation = Quaternion.Euler(40.0f, -135.0f, 0.0f);

            Light moonLight = moonLightObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.72f, 0.84f, 0.98f);
            moonLight.intensity = 1.4f;
            moonLight.shadows = LightShadows.Soft;
            moonLight.shadowStrength = 0.75f;

            // Luz suave ambiental interior
            GameObject fillLightObj = new GameObject("Interior_Warm_Fill");
            fillLightObj.transform.SetParent(lightGroup.transform, false);
            fillLightObj.transform.position = new Vector3(0.0f, 2.2f, 0.0f);

            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.95f, 0.84f, 0.72f);
            fillLight.intensity = 0.85f;
            fillLight.range = 7.0f;
            fillLight.shadows = LightShadows.None;
        }

        private static void SetupFirstPersonPlayer(GameObject room)
        {
            // Desactivar cualquier cámara suelta que compita
            Camera[] oldCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in oldCams)
            {
                if (c.transform.parent == null || c.gameObject.name == "Main Camera")
                {
                    c.gameObject.SetActive(false);
                }
            }

            // Crear o reutilizar Player_FPV
            GameObject oldPlayer = GameObject.Find("Player_FPV");
            if (oldPlayer != null) DestroyImmediate(oldPlayer);

            GameObject player = new GameObject("Player_FPV");
            player.transform.position = new Vector3(0.0f, 0.05f, -0.6f);
            player.transform.rotation = Quaternion.Euler(0.0f, 28.0f, 0.0f); // Mirando hacia la ventana y la luna

            // Character Controller
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0.0f, 0.875f, 0.0f);
            cc.stepOffset = 0.2f;

            // Cámara del jugador a la altura de los ojos
            GameObject camObj = new GameObject("FirstPersonCamera");
            camObj.transform.SetParent(player.transform, false);
            camObj.transform.localPosition = new Vector3(0.0f, 1.65f, 0.0f);
            camObj.transform.localRotation = Quaternion.identity;

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 75.0f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f);
            camObj.tag = "MainCamera";

            if (camObj.GetComponent<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }

            // Agregar script FirstPersonController
            FirstPersonController fpc = player.AddComponent<FirstPersonController>();
            fpc.cameraTransform = camObj.transform;
            fpc.walkSpeed = 2.4f;
            fpc.runSpeed = 4.2f;
            fpc.mouseSensitivity = 0.12f;

            Selection.activeGameObject = player;
            Debug.Log("[StylizedRoom] Player_FPV creado exitosamente con CharacterController y WASD.");
        }
    }
}
