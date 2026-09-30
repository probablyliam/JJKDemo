using JJKDemo.Combat.Trial;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JJKDemo.Combat.EditorTools
{
    /// <summary>
    /// One-shot generator for the Cursed Energy Trial HUD prefab. Run
    /// <c>JJK Demo/Build Cursed Energy Trial HUD</c> to (re)produce
    /// <c>Assets/_Project/Prefabs/UI/TrialHUD.prefab</c> wired to a <see cref="CursedEnergyTrialUI"/>.
    ///
    /// Deliberately clean and minimal: themed TextMeshPro fonts, a single subtle drop shadow for
    /// legibility (no glow, no heavy outline, no gradients), and flat rounded panels with consistent
    /// padding. Edit the resulting prefab visually — this builder only bootstraps it and is not part
    /// of the runtime path. Re-running overwrites the prefab and the baked sprite/material assets.
    /// </summary>
    public static class CursedEnergyTrialHudBuilder
    {
        private const string PrefabDir = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabDir + "/TrialHUD.prefab";
        private const string FontDir = "Assets/_Project/Fonts/TMP";
        private const string MatDir = FontDir + "/Materials";

        private const float ChargeBarWidth = 300f;
        private const float ChargeFillHeight = 18f;

        // --- Theme -------------------------------------------------------------------------------
        private static readonly Color RedColor = new Color(0.95f, 0.34f, 0.30f);
        private static readonly Color BlueColor = new Color(0.36f, 0.60f, 0.98f);
        private static readonly Color PurpleColor = new Color(0.70f, 0.42f, 0.98f);
        private static readonly Color Accent = new Color(0.60f, 0.84f, 1f);
        private static readonly Color Faint = new Color(0.58f, 0.68f, 0.80f);
        private static readonly Color PanelTint = new Color(0.05f, 0.07f, 0.11f, 0.78f);
        private static readonly Color TrackTint = new Color(0.03f, 0.05f, 0.08f, 0.85f);

        private static Sprite circleSprite; // soft solid disc
        private static Sprite roundSprite;   // 9-sliced rounded rect

        private static TMP_FontAsset fTitle, fNumber, fLabel, fRank, fHeavy;
        private static Material mTitle, mNumber, mLabel, mRank, mHeavy;

        private sealed class Refs
        {
            public CanvasGroup hud;
            public RectTransform canvasRect;
            public TMP_Text timerText, destructionText, rankText;
            public Image redCircle, blueCircle, purpleCircle;
            public TMP_Text redNum, blueNum, purpleNum;
            public RectTransform chargeBarContainer;
            public CanvasGroup chargeBarGroup;
            public Image chargeFill;
            public TMP_Text chargeWarn;
            public TMP_Text countdownText;
            public GameObject endScreen;
            public TMP_Text endPercentText, endRankText;
            public RectTransform popupTemplate;
        }

        [MenuItem("JJK Demo/Build Cursed Energy Trial HUD")]
        public static void Build()
        {
            EnsureFolder(PrefabDir, "Assets/_Project/Prefabs", "UI");
            EnsureFolder(MatDir, FontDir, "Materials");

            if (!LoadFonts())
            {
                Debug.LogError("[CursedEnergyTrial] Missing TMP font assets in " + FontDir +
                               ". Run 'JJK Demo/Generate TMP Font Assets' first.");
                return;
            }

            BakeSprites();
            BuildMaterials();

            var root = new GameObject("Trial HUD");
            var ui = root.AddComponent<CursedEnergyTrialUI>();
            var refs = new Refs();

            BuildUI(root.transform, refs);
            WireReferences(ui, refs);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[CursedEnergyTrial] Clean HUD prefab built at {PrefabPath}.");
        }

        // --- Fonts + materials -------------------------------------------------------------------
        private static bool LoadFonts()
        {
            fTitle = LoadFont("BebasNeue");   // tall condensed caps — title / countdown / grade
            fNumber = LoadFont("Teko");       // tall techy numerals — timer / percentages
            fLabel = LoadFont("Rajdhani");    // angular semi-condensed — small labels
            fRank = LoadFont("Oswald");       // condensed — rank / charge counts / popups
            fHeavy = LoadFont("RussoOne");    // heavy blocky — end-screen banner
            return fTitle && fNumber && fLabel && fRank && fHeavy;
        }

        private static TMP_FontAsset LoadFont(string name)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "/" + name + " SDF.asset");
        }

        private static void BuildMaterials()
        {
            mTitle = MakeShadowMaterial(fTitle);
            mNumber = MakeShadowMaterial(fNumber);
            mLabel = MakeShadowMaterial(fLabel);
            mRank = MakeShadowMaterial(fRank);
            mHeavy = MakeShadowMaterial(fHeavy);
        }

        // A clean, crisp drop shadow only — no outline, no glow. Saved as a project asset so it
        // serialises into the prefab.
        private static Material MakeShadowMaterial(TMP_FontAsset font)
        {
            string path = MatDir + "/" + font.name + " Shadow.mat";
            AssetDatabase.DeleteAsset(path);

            var mat = new Material(font.material) { name = font.name + " Shadow" };
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.5f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.5f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.05f);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // --- UI construction ---------------------------------------------------------------------
        private static void BuildUI(Transform root, Refs refs)
        {
            var canvasGo = new GameObject("Trial HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            refs.canvasRect = (RectTransform)canvasGo.transform;

            var hudGo = new GameObject("HUD", typeof(RectTransform), typeof(CanvasGroup));
            hudGo.transform.SetParent(refs.canvasRect, false);
            Stretch((RectTransform)hudGo.transform);
            refs.hud = hudGo.GetComponent<CanvasGroup>();
            var hudRect = (RectTransform)hudGo.transform;

            BuildTopBar(hudRect, refs);
            BuildDestructionPanel(hudRect, refs);
            BuildAbilityCircles(hudRect, refs);
            BuildChargeBar(hudRect, refs);

            refs.countdownText = MakeText(refs.canvasRect, "Countdown", "3", fTitle,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 320f),
                200, TextAlignmentOptions.Center, Color.white, mTitle);

            BuildEndScreen(refs);
            BuildPopupTemplate(refs);
        }

        private static void BuildTopBar(RectTransform parent, Refs refs)
        {
            MakeText(parent, "Title", "CURSED ENERGY TRIAL", fTitle,
                new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(900f, 40f),
                34, TextAlignmentOptions.Center, Accent, mTitle);

            refs.timerText = MakeText(parent, "Timer", "1:30", fNumber,
                new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(420f, 100f),
                88, TextAlignmentOptions.Center, Color.white, mNumber);
        }

        private static void BuildDestructionPanel(RectTransform parent, Refs refs)
        {
            var panel = MakeImage(parent, "DestructionPanel", roundSprite, PanelTint,
                new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(300f, 134f), Image.Type.Sliced);
            var pr = (RectTransform)panel.transform;

            MakeText(pr, "DLabel", "DESTRUCTION", fLabel,
                new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(260f, 24f),
                20, TextAlignmentOptions.Left, Faint, mLabel);

            refs.destructionText = MakeText(pr, "DValue", "0.0%", fNumber,
                new Vector2(0f, 1f), new Vector2(18f, -38f), new Vector2(264f, 58f),
                54, TextAlignmentOptions.Left, Color.white, mNumber);

            refs.rankText = MakeText(pr, "Rank", "UNRANKED", fRank,
                new Vector2(0f, 1f), new Vector2(20f, -100f), new Vector2(264f, 30f),
                26, TextAlignmentOptions.Left, TrialRanks.UnrankedColor, mRank);
        }

        private static void BuildAbilityCircles(RectTransform parent, Refs refs)
        {
            const float size = 54f;
            const float gap = 16f;
            var row = new GameObject("AbilityCircles", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-32f, 32f);
            rt.sizeDelta = new Vector2(size * 3f + gap * 2f, size + 24f);

            MakeText(rt, "ChargesLabel", "CHARGES", fLabel,
                new Vector2(1f, 1f), new Vector2(-2f, 2f), new Vector2(220f, 20f),
                16, TextAlignmentOptions.Right, Faint, mLabel);

            float x0 = -(size * 2f + gap * 2f);
            MakeCircle(rt, x0 + (size + gap) * 0f, size, RedColor, out refs.redCircle, out refs.redNum);
            MakeCircle(rt, x0 + (size + gap) * 1f, size, BlueColor, out refs.blueCircle, out refs.blueNum);
            MakeCircle(rt, x0 + (size + gap) * 2f, size, PurpleColor, out refs.purpleCircle, out refs.purpleNum);
        }

        private static void MakeCircle(RectTransform parent, float x, float size, Color color, out Image img, out TMP_Text num)
        {
            var go = new GameObject("Circle", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(size, size);

            img = go.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = color;
            img.raycastTarget = false;

            var ng = new GameObject("Num", typeof(RectTransform));
            ng.transform.SetParent(rt, false);
            Stretch((RectTransform)ng.transform);
            num = ng.AddComponent<TextMeshProUGUI>();
            num.font = fRank;
            num.fontSharedMaterial = mRank;
            num.text = "0";
            num.fontSize = Mathf.RoundToInt(size * 0.46f);
            num.alignment = TextAlignmentOptions.Center;
            num.color = Color.white;
            num.raycastTarget = false;
        }

        private static void BuildChargeBar(RectTransform parent, Refs refs)
        {
            var container = new GameObject("ChargeBar", typeof(RectTransform), typeof(CanvasGroup));
            container.transform.SetParent(parent, false);
            refs.chargeBarContainer = (RectTransform)container.transform;
            refs.chargeBarContainer.anchorMin = new Vector2(0f, 0f);
            refs.chargeBarContainer.anchorMax = new Vector2(0f, 0f);
            refs.chargeBarContainer.pivot = new Vector2(0f, 0f);
            refs.chargeBarContainer.anchoredPosition = new Vector2(32f, 32f);
            refs.chargeBarContainer.sizeDelta = new Vector2(ChargeBarWidth, 52f);
            refs.chargeBarGroup = container.GetComponent<CanvasGroup>();

            MakeText(refs.chargeBarContainer, "ChargeLabel", "CHARGE", fLabel,
                new Vector2(0f, 1f), new Vector2(2f, 0f), new Vector2(200f, 22f),
                17, TextAlignmentOptions.Left, Faint, mLabel);

            var track = MakeImage(refs.chargeBarContainer, "Track", roundSprite, TrackTint,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(ChargeBarWidth, ChargeFillHeight + 8f), Image.Type.Sliced);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(track.transform, false);
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = new Vector2(0f, 0.5f);
            frt.anchorMax = new Vector2(0f, 0.5f);
            frt.pivot = new Vector2(0f, 0.5f);
            frt.anchoredPosition = new Vector2(4f, 0f);
            frt.sizeDelta = new Vector2(0.0001f, ChargeFillHeight);
            refs.chargeFill = fillGo.AddComponent<Image>();
            refs.chargeFill.sprite = roundSprite;
            refs.chargeFill.type = Image.Type.Sliced;
            refs.chargeFill.color = RedColor;
            refs.chargeFill.raycastTarget = false;

            refs.chargeWarn = MakeText(refs.chargeBarContainer, "ChargeWarn", "MAX", fRank,
                new Vector2(0f, 0f), new Vector2(ChargeBarWidth * 0.5f - 80f, 4f), new Vector2(220f, ChargeFillHeight),
                17, TextAlignmentOptions.Center, Color.white, mRank);
            refs.chargeWarn.gameObject.SetActive(false);
        }

        private static void BuildEndScreen(Refs refs)
        {
            refs.endScreen = MakeImage(refs.canvasRect, "EndScreen", null, new Color(0.02f, 0.03f, 0.05f, 0.88f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Image.Type.Simple).gameObject;
            Stretch((RectTransform)refs.endScreen.transform);
            var rect = (RectTransform)refs.endScreen.transform;

            MakeText(rect, "EndTitle", "TRIAL COMPLETE", fHeavy,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1100f, 70f),
                46, TextAlignmentOptions.Center, Accent, mHeavy);

            MakeText(rect, "EndDLabel", "ARENA DESTRUCTION", fLabel,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 96f), new Vector2(800f, 34f),
                24, TextAlignmentOptions.Center, Faint, mLabel);

            refs.endPercentText = MakeText(rect, "EndPercent", "0.0%", fNumber,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(800f, 150f),
                116, TextAlignmentOptions.Center, Color.white, mNumber);

            MakeText(rect, "EndGradeLabel", "FINAL GRADE", fLabel,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(800f, 34f),
                24, TextAlignmentOptions.Center, Faint, mLabel);

            refs.endRankText = MakeText(rect, "EndGrade", "GRADE 4", fTitle,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(1000f, 80f),
                66, TextAlignmentOptions.Center, Color.white, mTitle);

            MakeText(rect, "EndHint", "PRESS  R  TO  RUN  AGAIN", fLabel,
                new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(800f, 40f),
                26, TextAlignmentOptions.Center, Faint, mLabel);

            refs.endScreen.SetActive(false);
        }

        private static void BuildPopupTemplate(Refs refs)
        {
            var text = MakeText(refs.canvasRect, "PopupTemplate", "+0", fRank,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 44f),
                30, TextAlignmentOptions.Center, Color.white, mRank);
            refs.popupTemplate = text.rectTransform;
            refs.popupTemplate.gameObject.SetActive(false);
        }

        // --- Element helpers ---------------------------------------------------------------------
        private static TMP_Text MakeText(RectTransform parent, string name, string value, TMP_FontAsset font,
            Vector2 pivotAnchor, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions align,
            Color color, Material material)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = pivotAnchor;
            rect.anchorMax = pivotAnchor;
            rect.pivot = pivotAnchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            if (material != null)
            {
                text.fontSharedMaterial = material;
            }
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Image MakeImage(RectTransform parent, string name, Sprite sprite, Color color,
            Vector2 pivotAnchor, Vector2 anchoredPos, Vector2 size, Image.Type type)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = pivotAnchor;
            rect.anchorMax = pivotAnchor;
            rect.pivot = pivotAnchor;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.color = color;
            return img;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // --- Reference wiring --------------------------------------------------------------------
        private static void WireReferences(CursedEnergyTrialUI ui, Refs refs)
        {
            var so = new SerializedObject(ui);
            SetRef(so, "hud", refs.hud);
            SetRef(so, "canvasRect", refs.canvasRect);
            SetRef(so, "timerText", refs.timerText);
            SetRef(so, "destructionText", refs.destructionText);
            SetRef(so, "rankText", refs.rankText);
            SetRef(so, "redCircle", refs.redCircle);
            SetRef(so, "blueCircle", refs.blueCircle);
            SetRef(so, "purpleCircle", refs.purpleCircle);
            SetRef(so, "redNum", refs.redNum);
            SetRef(so, "blueNum", refs.blueNum);
            SetRef(so, "purpleNum", refs.purpleNum);
            SetRef(so, "chargeBarContainer", refs.chargeBarContainer);
            SetRef(so, "chargeBarGroup", refs.chargeBarGroup);
            SetRef(so, "chargeFill", refs.chargeFill);
            SetRef(so, "chargeWarn", refs.chargeWarn);
            SetRef(so, "countdownText", refs.countdownText);
            SetRef(so, "endScreen", refs.endScreen);
            SetRef(so, "endPercentText", refs.endPercentText);
            SetRef(so, "endRankText", refs.endRankText);
            SetRef(so, "popupTemplate", refs.popupTemplate);

            var width = so.FindProperty("chargeBarMaxWidth");
            if (width != null)
            {
                width.floatValue = ChargeBarWidth - 8f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[CursedEnergyTrial] CursedEnergyTrialUI has no serialized field '{propertyName}' — wiring skipped.");
                return;
            }
            prop.objectReferenceValue = value;
        }

        // --- Asset baking ------------------------------------------------------------------------
        private static void EnsureFolder(string full, string parent, string leaf)
        {
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }

        private static void BakeSprites()
        {
            circleSprite = BakeSprite("TrialCircle", 96, (x, y, s) => DiscAlpha(x, y, s), Vector4.zero);
            roundSprite = BakeSprite("UI_Round", 48, (x, y, s) => RoundedAlpha(x, y, s, 12f), new Vector4(14, 14, 14, 14));
        }

        private delegate float AlphaFn(int x, int y, int size);

        // Builds the texture in memory and saves it as an asset with the Sprite as a sub-asset, then
        // returns that Sprite directly. Avoids the PNG-import round-trip (whose Sprite sub-asset is
        // not reliably loadable within the same editor call), so the references are always valid.
        private static Sprite BakeSprite(string name, int size, AlphaFn alphaFn, Vector4 border)
        {
            string path = PrefabDir + "/" + name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.DeleteAsset(PrefabDir + "/" + name + ".png");

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaFn(x, y, size)));
                }
            }
            tex.Apply();

            AssetDatabase.CreateAsset(tex, path);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            AssetDatabase.AddObjectToAsset(sprite, tex);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        // soft solid disc, 1px AA edge
        private static float DiscAlpha(int x, int y, int size)
        {
            float r = size * 0.5f;
            float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f));
            return Mathf.Clamp01(r - d);
        }

        // rounded-rect mask (sharp straight edges, rounded corners)
        private static float RoundedAlpha(int x, int y, int size, float radius)
        {
            float half = size * 0.5f;
            float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
            float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            float dist = outside + inside - radius;
            return Mathf.Clamp01(0.5f - dist);
        }
    }
}
