#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using JJKDemo.Combat.Destruction;

namespace JJKDemo.Editor
{
    public class SandboxEnvironmentGenerator : EditorWindow
    {
        [MenuItem("JJK Demo/Generate Voxel Sandbox City")]
        public static void ShowWindow()
        {
            GetWindow<SandboxEnvironmentGenerator>("Sandbox Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Stylized Anime Sandbox Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This will clear the current Sandbox Environment and build a fresh, highly optimized voxel destructible city.", MessageType.Info);

            if (GUILayout.Button("Generate Sandbox City"))
            {
                BuildSandbox();
            }
        }

        private void BuildSandbox()
        {
            // 1. SAFE SCENE CLEANUP
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                if (root.GetComponentInChildren<Camera>() != null) continue;
                if (root.GetComponentInChildren<Light>() != null) continue;
                if (root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>() != null) continue;
                if (root.name.ToLower().Contains("gojo") || root.name.ToLower().Contains("player")) continue;
                if (root.GetComponentInChildren<CharacterController>() != null || root.GetComponentInChildren<Rigidbody>() != null && root.name.Contains("Gojo")) continue;
                
                if (root.name != "Sandbox Environment")
                {
                    DestroyImmediate(root);
                }
            }

            var oldSandbox = GameObject.Find("Sandbox Environment");
            if (oldSandbox != null)
            {
                DestroyImmediate(oldSandbox);
            }

            // --- SCENE LIGHTING SETUP ---
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            var lights = Object.FindObjectsOfType<Light>();
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.transform.eulerAngles = new Vector3(50f, 330f, 0f);
                    l.color = Color.white;
                    l.intensity = 2f;
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 1f;
                }
            }

            // --- GLOBAL VOLUME SETUP ---
            var oldVolume = GameObject.Find("Global Volume");
            if (oldVolume != null) DestroyImmediate(oldVolume);

            GameObject volumeGO = new GameObject("Global Volume");
            var volume = volumeGO.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;

            string profilePath = "Assets/Settings/DefaultVolumeProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
            if (profile != null)
            {
                volume.profile = profile;
                if (!profile.Has<ToonShadersPro.URP.OutlineSettings>())
                {
                    profile.Add<ToonShadersPro.URP.OutlineSettings>();
                }
                profile.TryGet<ToonShadersPro.URP.OutlineSettings>(out var outlineSettings);
                outlineSettings.active = true;
                outlineSettings.outlineType.Override(ToonShadersPro.URP.OutlineType.HighQualityObjectMaskOutlines);
                outlineSettings.outlineThickness.Override(0.01f);
                outlineSettings.maskDrawingMode.Override(ToonShadersPro.URP.MaskDrawingMode.PerObject);
                outlineSettings.objectMask.Override(-1);
                
                EditorUtility.SetDirty(profile);
            }

            GameObject sandbox = new GameObject("Sandbox Environment");
            
            Material matFloor = GetOrCreateMaterial("Toon_Floor", new Color(0.85f, 0.85f, 0.85f), 0f);
            Material matConcrete = GetOrCreateMaterial("Toon_Concrete", new Color(0.75f, 0.78f, 0.82f), 1.5f);
            Material matMetal = GetOrCreateMaterial("Toon_Metal", new Color(0.2f, 0.25f, 0.35f), 2f);
            Material matAccent = GetOrCreateMaterial("Toon_Accent", new Color(1.0f, 0.4f, 0.1f), 3f);
            Material matSolidWall = GetOrCreateMaterial("Toon_Solid", new Color(0.3f, 0.6f, 0.8f), 0f);

            // --- STATIC ARENA ---
            // Ground and Outer Walls (NO Rigidbody, NO DestructibleChunk - 100% immune to everything)
            CreatePhysicsObject("Static Ground", sandbox.transform, new Vector3(0, -1f, 0), new Vector3(200, 2, 200), matFloor, true);
            CreatePhysicsObject("Outer Wall North", sandbox.transform, new Vector3(0, 5f, 100f), new Vector3(200, 10, 4f), matSolidWall, true);
            CreatePhysicsObject("Outer Wall South", sandbox.transform, new Vector3(0, 5f, -100f), new Vector3(200, 10, 4f), matSolidWall, true);
            CreatePhysicsObject("Outer Wall East", sandbox.transform, new Vector3(100f, 5f, 0), new Vector3(4f, 10, 200), matSolidWall, true);
            CreatePhysicsObject("Outer Wall West", sandbox.transform, new Vector3(-100f, 5f, 0), new Vector3(4f, 10, 200), matSolidWall, true);

            // --- NORTH SECTION: BUILDINGS ---
            // Z = 50 to 80. Very spaced out, wide variety of sizes.
            BuildPillarBuilding(sandbox.transform, "Building 1 (Small)", new Vector3(-60, 0, 60), 6f, 6f, 2, matMetal, matConcrete);
            BuildPillarBuilding(sandbox.transform, "Building 2 (Tall Thin)", new Vector3(-30, 0, 70), 4f, 4f, 8, matMetal, matConcrete);
            BuildPillarBuilding(sandbox.transform, "Building 3 (Massive Warehouse)", new Vector3(0, 0, 60), 16f, 10f, 1, matMetal, matConcrete);
            BuildPillarBuilding(sandbox.transform, "Building 4 (Medium)", new Vector3(30, 0, 65), 7f, 7f, 4, matMetal, matConcrete);
            BuildPillarBuilding(sandbox.transform, "Building 5 (Squat Square)", new Vector3(60, 0, 55), 10f, 10f, 2, matMetal, matConcrete);
            BuildPillarBuilding(sandbox.transform, "Building 6 (Tower)", new Vector3(80, 0, 70), 6f, 6f, 10, matMetal, matConcrete);

            // --- EAST SECTION: TRAINING BLOCKS ---
            // Create 10 varied materials upfront
            Material[] blockMaterials = new Material[10];
            for (int i = 0; i < 10; i++)
            {
                Color c = Color.HSVToRGB(i / 10f, 0.8f, 0.9f);
                blockMaterials[i] = GetOrCreateMaterial($"Toon_Block_{i}", c, 2f);
            }

            // Left side (Z > 0): Varying sizes and colors
            Transform blockYardVaried = new GameObject("Training Blocks (Left - Varied)").transform;
            blockYardVaried.SetParent(sandbox.transform);
            blockYardVaried.position = new Vector3(65f, 0, 15f);
            int variedIndex = 0;
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    float size = Random.Range(1f, 3f);
                    Vector3 pos = blockYardVaried.position + new Vector3(x * 5f, size / 2f, z * 5f);
                    CreatePhysicsObject($"Varied Block {variedIndex++}", blockYardVaried, pos, new Vector3(size, size, size), blockMaterials[Random.Range(0, 10)], false);
                }
            }

            // Right side (Z < 0): Same size, different colors
            Transform blockYardUniform = new GameObject("Training Blocks (Right - Uniform)").transform;
            blockYardUniform.SetParent(sandbox.transform);
            blockYardUniform.position = new Vector3(65f, 0, -35f);
            int uniformIndex = 0;
            for (int x = -2; x <= 2; x++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    float size = 1.5f;
                    Vector3 pos = blockYardUniform.position + new Vector3(x * 5f, size / 2f, z * 5f);
                    CreatePhysicsObject($"Uniform Block {uniformIndex++}", blockYardUniform, pos, new Vector3(size, size, size), blockMaterials[Random.Range(0, 10)], false);
                }
            }

            // --- WEST SECTION: WALLS ---
            // Spaced out along the Z axis

            // 1. Regular Wall (Solid Single Wall)
            CreatePhysicsObject("Solid Single Wall", sandbox.transform, new Vector3(-60f, 3f, 40f), new Vector3(2f, 6f, 12f), matConcrete, false);

            // 2. Wall made of small objects (Stacked Bricks)
            BuildMultiLayerBrickWall(sandbox.transform, "Single Brick Wall", new Vector3(-60f, 0f, 15f), 1, matAccent);

            // 3. Multiple walls made of small objects (3 layers)
            BuildMultiLayerBrickWall(sandbox.transform, "Triple Brick Wall", new Vector3(-60f, 0f, -15f), 3, matAccent);

            // 4. Multiple walls of just 1 block (3 solid walls in front of each other)
            BuildMultiLayerSolidWall(sandbox.transform, "Triple Solid Wall", new Vector3(-60f, 3f, -45f), 3, matConcrete);

            Debug.Log("Structured Sandbox generated successfully! All physics natively supported.");
        }

        private void BuildMultiLayerBrickWall(Transform parent, string name, Vector3 basePos, int layers, Material mat)
        {
            Transform wallRoot = new GameObject(name).transform;
            wallRoot.SetParent(parent);
            wallRoot.position = basePos;
            Vector3 brickSize = new Vector3(1f, 1f, 2f); // Z is length of wall
            float zLength = 10f;
            int bricksLength = Mathf.CeilToInt(zLength / brickSize.z);
            float layerSpacing = 1.5f;

            for (int layer = 0; layer < layers; layer++)
            {
                float xPos = layer * layerSpacing;
                for (int y = 0; y < 8; y++)
                {
                    for (int z = 0; z < bricksLength; z++)
                    {
                        float zOffset = (y % 2 == 0) ? 0f : (brickSize.z / 2f);
                        if (z == bricksLength - 1 && y % 2 != 0) continue; // Keep edges clean

                        Vector3 pos = wallRoot.position + new Vector3(xPos, y * brickSize.y + (brickSize.y / 2f), z * brickSize.z + zOffset - (zLength / 2f));
                        CreatePhysicsObject($"Brick_L{layer}_{z}_{y}", wallRoot, pos, brickSize, mat, false);
                    }
                }
            }
        }

        private void BuildMultiLayerSolidWall(Transform parent, string name, Vector3 basePos, int layers, Material mat)
        {
            float layerSpacing = 3f;
            for (int layer = 0; layer < layers; layer++)
            {
                CreatePhysicsObject($"{name}_Layer{layer}", parent, basePos + new Vector3(layer * layerSpacing, 0, 0), new Vector3(2f, 6f, 12f), mat, false);
            }
        }

        private void BuildPillarBuilding(Transform parent, string bName, Vector3 basePos, float width, float depth, int floors, Material pillarMat, Material floorMat)
        {
            float floorHeight = 3.5f;
            float pillarThickness = 1.0f; // Thicker pillars for better physics stability

            for (int i = 0; i < floors; i++)
            {
                // We leave a micro-gap (0.005f) between pillars and floors so Unity Physics doesn't explode them due to exact overlap on startup!
                float currentY = (i * floorHeight) + 0.005f;
                float pillarYPos = currentY + (floorHeight - 0.5f) / 2f;
                Vector3 pSize = new Vector3(pillarThickness, floorHeight - 0.5f, pillarThickness);

                // 4 Pillars
                CreatePhysicsObject($"{bName}_F{i}_PillarFL", parent, basePos + new Vector3(-width/2 + pillarThickness/2, pillarYPos, -depth/2 + pillarThickness/2), pSize, pillarMat, false);
                CreatePhysicsObject($"{bName}_F{i}_PillarFR", parent, basePos + new Vector3(width/2 - pillarThickness/2, pillarYPos, -depth/2 + pillarThickness/2), pSize, pillarMat, false);
                CreatePhysicsObject($"{bName}_F{i}_PillarBL", parent, basePos + new Vector3(-width/2 + pillarThickness/2, pillarYPos, depth/2 - pillarThickness/2), pSize, pillarMat, false);
                CreatePhysicsObject($"{bName}_F{i}_PillarBR", parent, basePos + new Vector3(width/2 - pillarThickness/2, pillarYPos, depth/2 - pillarThickness/2), pSize, pillarMat, false);

                // Floor Slab
                CreatePhysicsObject($"{bName}_F{i}_Slab", parent, basePos + new Vector3(0, currentY + floorHeight - 0.25f, 0), new Vector3(width, 0.5f, depth), floorMat, false);
            }
        }

        private void CreatePhysicsObject(string name, Transform parent, Vector3 position, Vector3 size, Material mat, bool isStatic)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.localScale = size;
            
            if (mat != null)
            {
                var renderer = obj.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;
                
                // Prevent the giant floor from casting a shadow on itself, which creates the "shadow box" bug
                if (name == "Static Ground") {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            if (!isStatic)
            {
                // Must be on layer 0 (Default) so OverlapSphere finds it
                obj.layer = 0;

                // Add Standard Rigidbody
                var rb = obj.AddComponent<Rigidbody>();
                // Mass is proportional to volume so big things are heavy
                rb.mass = size.x * size.y * size.z * 1.5f; 
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // Best performance for stacked blocks

                // Add destructible script so Purple can delete it!
                var chunk = obj.AddComponent<DestructibleChunk>();
                
                var dissolveMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Shaders/PurpleDissolveMat.mat");
                if (dissolveMat != null)
                {
                    chunk.dissolveMaterial = dissolveMat;
                }
            }
        }

        private Material GetOrCreateMaterial(string name, Color color, float outlineWidth)
        {
            string path = $"Assets/_Project/Materials/Generated/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials/Generated"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Materials", "Generated");
                }
                
                // Copy the material from the demo instead of creating a generic one
                if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Toon Shaders Pro/Demo/Materials/Toon.mat") != null)
                {
                    AssetDatabase.CopyAsset("Assets/Toon Shaders Pro/Demo/Materials/Toon.mat", path);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                else
                {
                    Shader toonShader = Shader.Find("Universal Render Pipeline/Lit");
                    mat = new Material(toonShader);
                    AssetDatabase.CreateAsset(mat, path);
                }
            }

            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color); // Fallback standard
                if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", outlineWidth);
                if (mat.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", color * 0.4f);

                // Specific Floor Setup
                if (name == "Toon_Floor") {
                    Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Toon Shaders Pro/Textures/Rock/Rock028_2K_Color.png");
                    if (albedo == null) albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Toon Shaders Pro/Textures/Rock/Rock028_2K_Color.jpg");
                    Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Toon Shaders Pro/Textures/Rock/Rock028_2K_Normal.png");
                    if (normal == null) normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Toon Shaders Pro/Textures/Rock/Rock028_2K_Normal.jpg");
                    
                    if (albedo != null) {
                        mat.SetTexture("_BaseMap", albedo);
                        mat.SetTextureScale("_BaseMap", new Vector2(20f, 20f));
                    }
                    if (normal != null) {
                        mat.SetTexture("_BumpMap", normal);
                        mat.SetTextureScale("_BumpMap", new Vector2(20f, 20f));
                        mat.SetFloat("_BumpScale", 1.0f);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    
                    mat.SetFloat("_Smoothness", 0.1f); // Not a mirror
                    mat.SetFloat("_Metallic", 0.0f);
                    mat.SetFloat("_RimExtension", 0.05f); // Tiny rim
                }

                EditorUtility.SetDirty(mat);
            }
            return mat;
        }
    }
}
#endif
