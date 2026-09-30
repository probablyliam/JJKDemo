using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace JJKDemo.Combat.EditorTools
{
    /// <summary>
    /// Generates a TextMeshPro font asset for every TTF/OTF in <c>Assets/_Project/Fonts</c> so the
    /// project fonts are usable in TMP components without manually running the Font Asset Creator.
    /// Assets are created in dynamic mode (atlas grows on demand, source font referenced) and written
    /// to <c>Assets/_Project/Fonts/TMP</c>. Existing assets are left alone, so re-running is safe.
    /// </summary>
    public static class TrialFontAssetGenerator
    {
        private const string FontDir = "Assets/_Project/Fonts";
        private const string OutDir = FontDir + "/TMP";

        [MenuItem("JJK Demo/Generate TMP Font Assets")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(OutDir))
            {
                AssetDatabase.CreateFolder(FontDir, "TMP");
            }

            var guids = AssetDatabase.FindAssets("t:Font", new[] { FontDir });
            int made = 0;
            int skipped = 0;

            foreach (var guid in guids)
            {
                var srcPath = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<Font>(srcPath);
                if (font == null)
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(srcPath);
                var outPath = OutDir + "/" + name + " SDF.asset";
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath) != null)
                {
                    skipped++;
                    continue;
                }

                // Dynamic SDF: 1024² atlas filled on demand, keeps a reference to the source font.
                var fontAsset = TMP_FontAsset.CreateFontAsset(
                    font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
                if (fontAsset == null)
                {
                    Debug.LogWarning($"[TMP] Could not create a font asset for {name} — skipped.");
                    continue;
                }

                fontAsset.name = name + " SDF";
                AssetDatabase.CreateAsset(fontAsset, outPath);

                // The generated atlas texture(s) and material must be stored as sub-assets, or the
                // references break the moment the asset is reloaded.
                if (fontAsset.atlasTextures != null)
                {
                    foreach (var tex in fontAsset.atlasTextures)
                    {
                        if (tex == null)
                        {
                            continue;
                        }
                        tex.name = name + " Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }

                if (fontAsset.material != null)
                {
                    fontAsset.material.name = name + " Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }

                EditorUtility.SetDirty(fontAsset);
                made++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TMP] Font assets generated: {made}, skipped (already existed): {skipped}. Output: {OutDir}");
        }
    }
}
