using System.IO;
using JJKDemo.Combat.Destruction;
using JJKDemo.Combat.Trial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JJKDemo.Combat.EditorTools
{
    /// <summary>
    /// Builds the large, fully-enclosed, cool-toned "Cursed Energy Testing Facility". Zones are kept
    /// in their OWN areas (west tower gallery, east pillar gallery, north wall gallery, flanking
    /// super-heavy monoliths, far-north high-value towers, scattered pyramids) so the player can't
    /// line one shot straight through everything — each zone wants to be engaged on its own.
    ///
    /// Now that Red respects mass (forceMode = Impulse), mass is the real difficulty knob:
    ///   easy 2-6 → medium 8-16 → strong 28-55 → high-value 80-150 → super-heavy 280 (Purple-only).
    ///
    /// Every destructible carries <see cref="StartAsleep"/> + exact placement (nothing moves at spawn),
    /// a <see cref="DestructibleScoreTarget"/> + <see cref="DestructibleChunk"/>. Glow rings are
    /// children of the block they sit on, so they move and die WITH the block (no floating leftovers).
    /// Nothing here touches the player, camera, abilities, ability tuning, scoring, or UI.
    /// </summary>
    public static class CursedEnergyTrialArenaBuilder
    {
        private const string RootName = "Trial Arena";
        private const string MaterialDir = "Assets/_Project/Materials/Trial";
        private const string DissolvePath = "Assets/_Project/Shaders/PurpleDissolveMat.mat";

        private static Material dissolveMat;
        private static Material mFloor, mFloorInset, mFacility, mFacilityLight, mWallDark;
        private static Material mEasyA, mEasyB, mMedium, mBox, mStrongA, mStrongB, mCrystal;
        private static Material gCyan, gCyanSoft, gPurple, gMagenta, gPit;

        [MenuItem("JJK Demo/Build Cursed Energy Trial Arena")]
        public static void Build()
        {
            Random.InitState(20260613);
            EnsureMaterials();
            dissolveMat = AssetDatabase.LoadAssetAtPath<Material>(DissolvePath);

            RemoveIfExists(RootName);
            RemoveIfExists("Throwable Blocks");
            RemoveIfExists("Environment_Arena");

            var root = new GameObject(RootName).transform;

            BuildFacility(root);
            BuildCentral(root);
            BuildPyramidZone(root);
            BuildTowerZone(root);
            BuildPillarZone(root);
            BuildWallZone(root);
            BuildSuperHeavy(root);
            BuildHighValueZone(root);
            BuildEdgePlatforms(root);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            EditorSceneManager.SaveScene(root.gameObject.scene);

            var targets = root.GetComponentsInChildren<DestructibleScoreTarget>();
            int total = 0;
            foreach (var t in targets)
            {
                total += t.PointValue;
            }
            Debug.Log($"[CursedEnergyTrial] Facility built: {targets.Length} destructibles, total value {total}.");
        }

        // =========================================================================================
        // Facility shell (big floor w/ metal texture, fully enclosed by tall walls over a pit)
        // =========================================================================================

        private static void BuildFacility(Transform root)
        {
            var f = Group(root, "Facility");

            MakeStatic(f, "TestFloor", new Vector3(0f, -1f, 30f), new Vector3(124f, 2f, 140f), mFloor);
            MakeStatic(f, "FloorInset", new Vector3(0f, 0.04f, 30f), new Vector3(116f, 0.08f, 132f), mFloorInset);
            MakeDecor(f, "PitGlow", new Vector3(0f, -26f, 30f), new Vector3(230f, 0.4f, 230f), gPit);

            MakeDecor(f, "Spine", new Vector3(0f, 0.07f, 30f), new Vector3(0.9f, 0.12f, 136f), gCyanSoft);
            for (int i = 0; i < 4; i++)
            {
                MakeDecor(f, "Cross", new Vector3(0f, 0.07f, -10f + i * 28f), new Vector3(116f, 0.12f, 0.4f), gCyanSoft);
            }

            MakeStatic(f, "Wall_W", new Vector3(-72f, 11f, 30f), new Vector3(4f, 34f, 184f), mFacility);
            MakeStatic(f, "Wall_E", new Vector3(72f, 11f, 30f), new Vector3(4f, 34f, 184f), mFacility);
            MakeStatic(f, "Wall_S", new Vector3(0f, 11f, -52f), new Vector3(156f, 34f, 4f), mFacility);
            MakeStatic(f, "Wall_N", new Vector3(0f, 11f, 112f), new Vector3(156f, 34f, 4f), mFacility);
            for (int i = -3; i <= 3; i++)
            {
                MakeDecor(f, "StripW", new Vector3(-69.9f, 13f, 30f + i * 24f), new Vector3(0.4f, 16f, 2.5f), gCyanSoft);
                MakeDecor(f, "StripE", new Vector3(69.9f, 13f, 30f + i * 24f), new Vector3(0.4f, 16f, 2.5f), gCyanSoft);
            }

            Vector3[] cols = { new Vector3(-60f, 0f, -34f), new Vector3(60f, 0f, -34f), new Vector3(-60f, 0f, 96f), new Vector3(60f, 0f, 96f) };
            foreach (var c in cols)
            {
                var col = Group(f, "FacilityColumn");
                col.localPosition = c;
                MakeStatic(col, "Shaft", new Vector3(0f, 11f, 0f), new Vector3(2.6f, 22f, 2.6f), mFacility);
                MakeStatic(col, "Base", new Vector3(0f, 0.7f, 0f), new Vector3(3.8f, 1.4f, 3.8f), mWallDark);
                MakeDecor(col, "Ring1", new Vector3(0f, 6f, 0f), new Vector3(2.9f, 0.5f, 2.9f), gCyan);
                MakeDecor(col, "Ring2", new Vector3(0f, 15f, 0f), new Vector3(2.9f, 0.5f, 2.9f), gCyan);
            }
        }

        // =========================================================================================
        // Zones (each in its own area)
        // =========================================================================================

        private static void BuildCentral(Transform root)
        {
            var z = Group(root, "Central");
            BuildStraightTower(z, new Vector3(-12f, 0f, -22f), 6, 1.4f, 4f, mEasyA, 8);
            BuildStraightTower(z, new Vector3(12f, 0f, -22f), 6, 1.4f, 4f, mEasyB, 8);
            BuildPillarTower(z, new Vector3(0f, 0f, 12f), 5, 0.9f, 2.6f, 16f, mStrongB, 26); // central feature
        }

        private static void BuildPyramidZone(Transform root)
        {
            var z = Group(root, "PyramidZone");
            BuildBoxPyramid(z, new Vector3(-40f, 0f, -14f), 5, 7);
            BuildBoxPyramid(z, new Vector3(40f, 0f, -14f), 5, 7);
            BuildBoxPyramid(z, new Vector3(-16f, 0f, 4f), 4, 6);
            BuildBoxPyramid(z, new Vector3(16f, 0f, 4f), 4, 6);
        }

        // WEST gallery — straight towers escalating small -> strong.
        private static void BuildTowerZone(Transform root)
        {
            var z = Group(root, "TowerZone");
            BuildStraightTower(z, new Vector3(-50f, 0f, -16f), 6, 1.4f, 5f, mEasyA, 9);
            BuildStraightTower(z, new Vector3(-50f, 0f, -1f), 7, 1.45f, 9f, mMedium, 13);
            BuildStraightTower(z, new Vector3(-50f, 0f, 15f), 8, 1.5f, 14f, mMedium, 16);
            BuildStraightTower(z, new Vector3(-50f, 0f, 32f), 9, 1.6f, 35f, mStrongB, 30, true); // strong, banded
        }

        // EAST gallery — pillar towers: thin/weak -> wide/medium -> very strong/big.
        private static void BuildPillarZone(Transform root)
        {
            var z = Group(root, "PillarZone");
            BuildPillarTower(z, new Vector3(50f, 0f, -14f), 4, 0.6f, 2.4f, 5f, mMedium, 16);
            BuildPillarTower(z, new Vector3(50f, 0f, 5f), 5, 0.9f, 2.6f, 16f, mStrongB, 28);
            BuildPillarTower(z, new Vector3(50f, 0f, 26f), 5, 1.3f, 3.0f, 48f, mStrongA, 48);
        }

        // NORTH gallery — masonry walls side-by-side, small -> huge (huge: max-Red clears ~half).
        private static void BuildWallZone(Transform root)
        {
            var z = Group(root, "WallZone");
            BuildBrickWall(z, new Vector3(-40f, 0f, 66f), 8, 6, 1.3f, 1.0f, mEasyB, 5f, 9);
            BuildBrickWall(z, new Vector3(-15f, 0f, 66f), 9, 7, 1.35f, 1.05f, mMedium, 12f, 14);
            BuildBrickWall(z, new Vector3(12f, 0f, 66f), 10, 8, 1.4f, 1.1f, mStrongB, 28f, 22);
            BuildBrickWall(z, new Vector3(40f, 0f, 66f), 12, 9, 1.45f, 1.15f, mStrongA, 55f, 34);
        }

        // Two flanking landmark monoliths of giant super-heavy blocks with a small high-value top.
        private static void BuildSuperHeavy(Transform root)
        {
            var z = Group(root, "SuperHeavyTowers");
            BuildSuperHeavyTower(z, new Vector3(-24f, 0f, 46f));
            BuildSuperHeavyTower(z, new Vector3(24f, 0f, 46f));
        }

        // Far-north — very heavy glowing towers, the score ceiling.
        private static void BuildHighValueZone(Transform root)
        {
            var z = Group(root, "HighValueTowers");
            BuildHighValueTower(z, new Vector3(-44f, 0f, 82f), 7, 90f, 55);
            BuildHighValueTower(z, new Vector3(44f, 0f, 82f), 7, 90f, 55);
            BuildHighValueTower(z, new Vector3(0f, 0f, 92f), 9, 150f, 80); // tallest, Purple-only
        }

        private static void BuildEdgePlatforms(Transform root)
        {
            var z = Group(root, "EdgePlatforms");
            BuildHighPlatform(z, new Vector3(-58f, 0f, 56f), 24f);
            BuildHighPlatform(z, new Vector3(58f, 0f, 56f), 24f);
        }

        // =========================================================================================
        // Structure builders
        // =========================================================================================

        private static void BuildStraightTower(Transform parent, Vector3 at, int height, float w, float mass, Material mat, int valueBase, bool banded = false)
        {
            var t = Group(parent, "Tower");
            t.localPosition = at;
            float h = w * 0.85f;
            for (int i = 0; i < height; i++)
            {
                var blk = MakeBlock(t, "Block", new Vector3(0f, h * 0.5f + i * h, 0f), new Vector3(w, h, w), Quaternion.identity, mat, mass, valueBase + i, 2.4f);
                if (banded && i == height - 1)
                {
                    AttachBand(blk, new Vector3(w, h, w), gPurple);
                }
            }
        }

        private static void BuildPillarTower(Transform parent, Vector3 at, int stories, float pillarW, float pillarH, float pillarMass, Material mat, int value)
        {
            var t = Group(parent, "PillarTower");
            t.localPosition = at;
            float span = pillarW + 1.6f;
            float platW = 2f * span + pillarW;
            float platThick = 0.7f;
            float platMass = pillarMass * 1.6f;
            float y = 0f;
            GameObject topPlatform = null;
            for (int s = 0; s < stories; s++)
            {
                foreach (var sx in new[] { -span, span })
                {
                    foreach (var sz in new[] { -span, span })
                    {
                        MakeBlock(t, "Pillar", new Vector3(sx, y + pillarH * 0.5f, sz), new Vector3(pillarW, pillarH, pillarW), Quaternion.identity, mat, pillarMass, value, 2.6f);
                    }
                }
                topPlatform = MakeBlock(t, "Platform", new Vector3(0f, y + pillarH + platThick * 0.5f, 0f), new Vector3(platW, platThick, platW), Quaternion.identity, mat, platMass, value + 4, 2.4f);
                y += pillarH + platThick;
            }
            if (pillarMass >= 30f && topPlatform != null)
            {
                AttachBand(topPlatform, new Vector3(platW, platThick, platW), gPurple);
            }
        }

        private static void BuildBrickWall(Transform parent, Vector3 at, int cols, int rows, float bw, float bh, Material mat, float mass, int value)
        {
            var w = Group(parent, "BrickWall");
            w.localPosition = at;
            GameObject centre = null;
            for (int r = 0; r < rows; r++)
            {
                float offset = (r % 2 == 0) ? 0f : bw * 0.5f;
                for (int c = 0; c < cols; c++)
                {
                    float x = (c - (cols - 1) * 0.5f) * bw + offset;
                    var brick = MakeBlock(w, "Brick", new Vector3(x, bh * 0.5f + r * bh, 0f), new Vector3(bw * 0.98f, bh * 0.98f, bw * 1.2f), Quaternion.identity, mat, mass, value, 2.2f);
                    if (r == rows / 2 && c == cols / 2)
                    {
                        centre = brick;
                    }
                }
            }
            if (mass >= 25f && centre != null)
            {
                AttachBand(centre, new Vector3(bw * 0.98f, bh * 0.98f, bw * 1.2f), gPurple);
            }
        }

        private static void BuildHighValueTower(Transform parent, Vector3 at, int height, float mass, int valueBase)
        {
            var t = Group(parent, "HighValueTower");
            t.localPosition = at;
            MakeTargetPad(t, Vector3.zero, 6.5f, gMagenta);
            const float w = 2.4f;
            const float h = 2.2f;
            for (int i = 0; i < height; i++)
            {
                var mat = (i % 2 == 0) ? mStrongA : mCrystal;
                var blk = MakeBlock(t, "Block", new Vector3(0f, h * 0.5f + i * h, 0f), new Vector3(w, h, w), Quaternion.identity, mat, mass, valueBase + i * 3, 3.2f);
                if (i % 2 == 1)
                {
                    AttachBand(blk, new Vector3(w, h, w), gMagenta);
                }
            }
        }

        // Giant super-heavy blocks (Purple-only) topped by a small light high-value prize.
        private static void BuildSuperHeavyTower(Transform parent, Vector3 at)
        {
            var t = Group(parent, "SuperHeavyTower");
            t.localPosition = at;
            MakeTargetPad(t, Vector3.zero, 9.5f, gMagenta);
            const float gs = 4f;
            const int giantCount = 6;
            for (int i = 0; i < giantCount; i++)
            {
                var blk = MakeBlock(t, "Giant", new Vector3(0f, gs * 0.5f + i * gs, 0f), Vector3.one * gs, Quaternion.identity, (i % 2 == 0) ? mStrongA : mStrongB, 280f, 45, 3f);
                if (i == giantCount - 1)
                {
                    AttachBand(blk, Vector3.one * gs, gMagenta);
                }
            }
            MakeBlock(t, "Prize", new Vector3(0f, giantCount * gs + 0.9f, 0f), Vector3.one * 1.7f, Quaternion.Euler(0f, 45f, 0f), mCrystal, 3f, 120, 1.2f);
        }

        private static void BuildHighPlatform(Transform parent, Vector3 at, float height)
        {
            var p = Group(parent, "HighPlatform");
            p.localPosition = at;
            MakeStatic(p, "Pillar", new Vector3(0f, height * 0.5f, 0f), new Vector3(7f, height, 7f), mWallDark);
            MakeStatic(p, "Deck", new Vector3(0f, height - 0.4f, 0f), new Vector3(11f, 0.8f, 11f), mFacilityLight);
            MakeDecor(p, "DeckEdge", new Vector3(0f, height + 0.05f, 0f), new Vector3(11f, 0.12f, 11f), gCyan);
            BuildBoxPyramid(p, new Vector3(0f, height, 0f), 3, 12);
        }

        // Clean axis-aligned box pyramid (no rotation, exact contacts) — never settles on its own.
        private static void BuildBoxPyramid(Transform parent, Vector3 at, int baseCount, int valueBase)
        {
            var s = Group(parent, "BoxPyramid");
            s.localPosition = at;
            const float bs = 0.95f;
            for (int layer = 0; layer < baseCount; layer++)
            {
                int n = baseCount - layer;
                float offset = (n - 1) * 0.5f;
                for (int a = 0; a < n; a++)
                {
                    for (int b = 0; b < n; b++)
                    {
                        MakeBlock(s, "Box", new Vector3((a - offset) * bs, bs * 0.5f + layer * bs, (b - offset) * bs), Vector3.one * (bs * 0.98f), Quaternion.identity, mBox, 2f, valueBase + layer, 2.0f);
                    }
                }
            }
        }

        // =========================================================================================
        // Primitives
        // =========================================================================================

        private static GameObject MakeBlock(Transform parent, string name, Vector3 localPos, Vector3 size,
            Quaternion localRot, Material mat, float mass, int value, float displace = 2.5f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var chunk = go.AddComponent<DestructibleChunk>();
            chunk.dissolveMaterial = dissolveMat;

            var target = go.AddComponent<DestructibleScoreTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("pointValue").intValue = value;
            so.FindProperty("displaceDistance").floatValue = displace;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<StartAsleep>();
            return go;
        }

        // Glow ring parented to a destructible block (counter-scaled so it isn't stretched). It moves
        // with the block when knocked and is destroyed with it — so no glow is left floating.
        private static void AttachBand(GameObject block, Vector3 blockSize, Material glow)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "GlowBand";
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
            go.GetComponent<MeshRenderer>().sharedMaterial = glow;
            go.transform.SetParent(block.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(
                (blockSize.x + 0.3f) / blockSize.x,
                0.36f / blockSize.y,
                (blockSize.z + 0.3f) / blockSize.z);
        }

        private static GameObject MakeStatic(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        private static GameObject MakeDecor(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
            go.isStatic = true;
            return go;
        }

        private static void MakeTargetPad(Transform parent, Vector3 localPos, float diameter, Material glow)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "TargetPad";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos + new Vector3(0f, 0.06f, 0f);
            go.transform.localScale = new Vector3(diameter, 0.04f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = glow;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
            go.isStatic = true;
        }

        private static Transform Group(Transform parent, string name)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            return g;
        }

        private static void RemoveIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        // =========================================================================================
        // Materials + generated floor texture
        // =========================================================================================

        private static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialDir))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "Trial");
            }

            string template = FindToonTemplate();
            var floorTex = EnsureFloorTexture();

            mFloor = Toon("Trial_Floor", new Color(0.11f, 0.14f, 0.21f), template, floorTex, new Vector2(10f, 11f));
            mFloorInset = Toon("Trial_FloorInset", new Color(0.15f, 0.19f, 0.28f), template, floorTex, new Vector2(9f, 10f));
            mFacility = Toon("Trial_Facility", new Color(0.14f, 0.17f, 0.25f), template, floorTex, new Vector2(6f, 6f));
            mFacilityLight = Toon("Trial_FacilityLight", new Color(0.22f, 0.27f, 0.38f), template, null, Vector2.one);
            mWallDark = Toon("Trial_WallDark", new Color(0.08f, 0.10f, 0.16f), template, null, Vector2.one);

            mEasyA = Toon("Trial_EasyA", new Color(0.46f, 0.57f, 0.72f), template, null, Vector2.one);
            mEasyB = Toon("Trial_EasyB", new Color(0.40f, 0.51f, 0.66f), template, null, Vector2.one);
            mMedium = Toon("Trial_Medium", new Color(0.32f, 0.44f, 0.60f), template, null, Vector2.one);
            mBox = Toon("Trial_Box", new Color(0.28f, 0.60f, 0.72f), template, null, Vector2.one);
            mStrongA = Toon("Trial_StrongA", new Color(0.24f, 0.30f, 0.46f), template, null, Vector2.one);
            mStrongB = Toon("Trial_StrongB", new Color(0.19f, 0.25f, 0.38f), template, null, Vector2.one);
            mCrystal = Toon("Trial_Crystal", new Color(0.46f, 0.30f, 0.72f), template, null, Vector2.one);

            gCyan = Glow("Trial_GlowCyan", new Color(0.32f, 0.82f, 1f), 2.0f);
            gCyanSoft = Glow("Trial_GlowCyanSoft", new Color(0.30f, 0.68f, 0.95f), 1.2f);
            gPurple = Glow("Trial_GlowPurple", new Color(0.62f, 0.30f, 1f), 1.6f);
            gMagenta = Glow("Trial_GlowMagenta", new Color(1f, 0.32f, 0.74f), 1.8f);
            gPit = Glow("Trial_GlowPit", new Color(0.14f, 0.10f, 0.30f), 1.0f);

            AssetDatabase.SaveAssets();
        }

        private static Material Toon(string name, Color color, string templatePath, Texture2D baseMap, Vector2 tiling)
        {
            string dest = MaterialDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(dest);
            if (mat == null)
            {
                if (!string.IsNullOrEmpty(templatePath) && AssetDatabase.CopyAsset(templatePath, dest))
                {
                    mat = AssetDatabase.LoadAssetAtPath<Material>(dest);
                }
                else
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, dest);
                }
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", baseMap);
                if (baseMap != null) mat.SetTextureScale("_BaseMap", tiling);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Glow(string name, Color color, float intensity)
        {
            string dest = MaterialDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(dest);
            if (mat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, dest);
            }
            else
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh != null && mat.shader != sh) mat.shader = sh;
            }

            var hdr = new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdr);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D EnsureFloorTexture()
        {
            string assetPath = MaterialDir + "/Trial_FloorTex.png";
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = 1f;
                    int gx = x % 128;
                    int gy = y % 128;
                    if (gx < 3 || gy < 3) v = 0.86f;
                    else if (gx < 5 || gy < 5) v = 0.94f;
                    v *= 0.985f + 0.015f * Mathf.PerlinNoise(x * 0.04f, y * 0.4f);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();

            string full = Application.dataPath + assetPath.Substring("Assets".Length);
            File.WriteAllBytes(full, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath);
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp != null)
            {
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static string FindToonTemplate()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null && m.shader != null && m.shader.name == "Toon Shaders Pro/URP/Toon")
                {
                    return path;
                }
            }
            return null;
        }
    }
}
