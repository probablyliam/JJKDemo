using System.Collections.Generic;
using JJKDemo.Combat.Abilities.Runtime;
using JJKDemo.Combat.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JJKDemo.Combat.Trial
{
    /// <summary>
    /// Themed HUD controller for the Cursed Energy Trial. This component does NOT build any UI: it
    /// binds to an authored prefab (see <c>Assets/_Project/Prefabs/UI/TrialHUD.prefab</c>, generated
    /// by <c>JJK Demo/Build Cursed Energy Trial HUD</c>) and only pushes values into the serialized
    /// widgets each frame — timer/rank, the three ability-charge circles, the charge bar, the
    /// countdown overlay, the end screen, and floating score popups.
    ///
    /// To change layout, colours, or fonts, edit the prefab in the editor — not this script.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CursedEnergyTrialUI : MonoBehaviour
    {
        [SerializeField] private CursedEnergyTrialManager manager;

        [Header("Root")]
        [SerializeField] private CanvasGroup hud;
        [SerializeField] private RectTransform canvasRect;

        [Header("Top HUD")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text destructionText;
        [SerializeField] private TMP_Text rankText;

        [Header("Ability Circles")]
        [SerializeField] private Image redCircle;
        [SerializeField] private Image blueCircle;
        [SerializeField] private Image purpleCircle;
        [SerializeField] private TMP_Text redNum;
        [SerializeField] private TMP_Text blueNum;
        [SerializeField] private TMP_Text purpleNum;

        [Header("Charge Bar")]
        [SerializeField] private RectTransform chargeBarContainer;
        [SerializeField] private CanvasGroup chargeBarGroup;
        [SerializeField] private Image chargeFill;
        [SerializeField] private TMP_Text chargeWarn;
        [Tooltip("Max pixel width the fill is allowed to reach (the track width minus padding).")]
        [SerializeField] private float chargeBarMaxWidth = 344f;

        [Header("Overlays")]
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private GameObject endScreen;
        [SerializeField] private TMP_Text endPercentText;
        [SerializeField] private TMP_Text endRankText;

        [Header("Popups")]
        [Tooltip("Inactive Text template cloned for each floating score popup.")]
        [SerializeField] private RectTransform popupTemplate;

        private static readonly Color RedColor = new Color(1f, 0.28f, 0.22f);
        private static readonly Color BlueColor = new Color(0.27f, 0.55f, 1f);
        private static readonly Color PurpleColor = new Color(0.72f, 0.34f, 1f);
        private static readonly Color EmptyCircle = new Color(0.24f, 0.26f, 0.30f, 0.85f);
        private static readonly Color NeutralCharge = new Color(0.4f, 0.45f, 0.5f);
        private const float ChargeFillHeight = 22f;

        private Camera cam;
        private TrialAbilityLimiter limiter;
        private AbilityController controller;
        private Vector2 chargeBarBasePos;

        private readonly List<Popup> popups = new List<Popup>();

        private sealed class Popup
        {
            public RectTransform rect;
            public TMP_Text text;
            public float life;
            public float maxLife;
        }

        private void Awake()
        {
            if (manager == null)
            {
                manager = CursedEnergyTrialManager.Instance;
            }

            if (chargeBarContainer != null)
            {
                chargeBarBasePos = chargeBarContainer.anchoredPosition;
            }

            if (popupTemplate != null)
            {
                popupTemplate.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (manager == null)
            {
                manager = CursedEnergyTrialManager.Instance;
            }

            if (manager != null)
            {
                manager.ScoreRecorded += OnScoreRecorded;
                manager.StateChanged += OnStateChanged;
                limiter = manager.Limiter;
            }
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.ScoreRecorded -= OnScoreRecorded;
                manager.StateChanged -= OnStateChanged;
            }
        }

        private void Start()
        {
            if (manager != null)
            {
                OnStateChanged(manager.State);
            }
        }

        private void Update()
        {
            if (manager == null)
            {
                manager = CursedEnergyTrialManager.Instance;
                if (manager == null)
                {
                    return;
                }
                manager.ScoreRecorded += OnScoreRecorded;
                manager.StateChanged += OnStateChanged;
            }

            if (limiter == null)
            {
                limiter = manager.Limiter;
            }

            if (controller == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    controller = player.GetComponent<AbilityController>();
                }
            }

            RefreshHud();
            RefreshCircles();
            RefreshChargeBar();
            RefreshCountdown();
            TickPopups();
        }

        // --- Binding -----------------------------------------------------------------------------
        private void RefreshHud()
        {
            float t = manager.TimeRemaining;
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
            timerText.color = t <= 10f && manager.IsRunning
                ? Color.Lerp(Color.white, RedColor, Mathf.PingPong(Time.time * 4f, 1f))
                : Color.white;

            destructionText.text = manager.DestructionPercent.ToString("0.0") + "%";
            rankText.text = manager.CurrentRankName.ToUpperInvariant();
            rankText.color = manager.CurrentRankColor;
        }

        private void RefreshCircles()
        {
            if (limiter == null)
            {
                return;
            }

            SetCircle(redCircle, redNum, RedColor, limiter.Red);
            SetCircle(blueCircle, blueNum, BlueColor, limiter.Blue);
            SetCircle(purpleCircle, purpleNum, PurpleColor, limiter.Purple);
        }

        private static void SetCircle(Image img, TMP_Text num, Color color, int count)
        {
            num.text = count.ToString();
            if (count > 0)
            {
                img.color = color;
                num.color = Color.white;
            }
            else
            {
                img.color = EmptyCircle;
                num.color = new Color(0.55f, 0.57f, 0.62f);
            }
        }

        private void RefreshChargeBar()
        {
            float charge = 0f;
            bool atMax = false;
            Color barColor = NeutralCharge;

            if (controller != null)
            {
                var actor = controller.ActiveActor;
                if (actor != null)
                {
                    charge = Mathf.Clamp01(actor.LinearCharge01);
                    atMax = actor.AtMaxCharge;
                    barColor = ColorFor(controller.ActiveChannel);
                }
            }

            // fill width
            chargeFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0.0001f, chargeBarMaxWidth * charge), ChargeFillHeight);

            // colour + pulse at max
            Color fillCol = barColor;
            if (atMax)
            {
                float pulse = (Mathf.Sin(Time.unscaledTime * 16f) + 1f) * 0.5f;
                fillCol = Color.Lerp(barColor, Color.white, pulse * 0.75f);
            }
            chargeFill.color = fillCol;

            // shake from ~80% up, harder at max
            float shake = 0f;
            if (atMax)
            {
                shake = 9f;
            }
            else if (charge >= 0.8f)
            {
                shake = Mathf.Lerp(1.5f, 7f, Mathf.InverseLerp(0.8f, 1f, charge));
            }
            Vector2 offset = shake > 0f
                ? new Vector2(Random.Range(-shake, shake), Random.Range(-shake, shake))
                : Vector2.zero;
            chargeBarContainer.anchoredPosition = chargeBarBasePos + offset;

            // warning text at max
            chargeWarn.gameObject.SetActive(atMax);
            if (atMax)
            {
                chargeWarn.color = fillCol;
            }

            chargeBarGroup.alpha = charge > 0.001f ? 1f : 0.45f;
        }

        private static Color ColorFor(AbilityChannel channel)
        {
            switch (channel)
            {
                case AbilityChannel.Red: return RedColor;
                case AbilityChannel.Blue: return BlueColor;
                case AbilityChannel.Purple: return PurpleColor;
                default: return NeutralCharge;
            }
        }

        private void RefreshCountdown()
        {
            var state = manager.State;
            bool show = state == TrialState.Ready || state == TrialState.Countdown;
            countdownText.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            if (state == TrialState.Ready)
            {
                countdownText.text = "CLICK TO BEGIN";
                countdownText.fontSize = 64;
                countdownText.rectTransform.localScale = Vector3.one;
            }
            else
            {
                float c = manager.Countdown;
                int whole = Mathf.CeilToInt(c);
                countdownText.text = whole > 0 ? whole.ToString() : "GO!";
                countdownText.fontSize = 200;
                float frac = c - Mathf.Floor(c);
                countdownText.rectTransform.localScale = Vector3.one * (1f + (1f - frac) * 0.4f);
            }
        }

        private void OnStateChanged(TrialState state)
        {
            if (hud != null)
            {
                hud.alpha = state == TrialState.Finished ? 0f : 1f;
            }

            if (endScreen != null)
            {
                endScreen.SetActive(state == TrialState.Finished);
            }

            if (state == TrialState.Finished)
            {
                endPercentText.text = manager.DestructionPercent.ToString("0.0") + "%";
                endRankText.text = manager.CurrentRankName.ToUpperInvariant();
                endRankText.color = manager.CurrentRankColor;
            }
        }

        // --- Score popups ------------------------------------------------------------------------
        private void OnScoreRecorded(int value, Vector3 worldPos, DestructibleScoreTarget.ScoreReason reason)
        {
            if (popupTemplate == null || canvasRect == null)
            {
                return;
            }

            if (cam == null)
            {
                cam = Camera.main;
            }
            if (cam == null)
            {
                return;
            }

            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            if (screen.z <= 0f)
            {
                return;
            }

            Color color;
            switch (reason)
            {
                case DestructibleScoreTarget.ScoreReason.Deleted: color = PurpleColor; break;
                case DestructibleScoreTarget.ScoreReason.Detached: color = BlueColor; break;
                case DestructibleScoreTarget.ScoreReason.OutOfBounds: color = new Color(1f, 0.7f, 0.25f); break;
                default: color = Color.white; break;
            }

            var go = Instantiate(popupTemplate.gameObject, canvasRect);
            go.SetActive(true);
            var rect = (RectTransform)go.transform;
            rect.position = screen;

            var text = go.GetComponent<TMP_Text>();
            text.text = "+" + value;
            text.color = color;

            popups.Add(new Popup { rect = rect, text = text, life = 0f, maxLife = 0.9f });
        }

        private void TickPopups()
        {
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.life += Time.deltaTime;
                float k = p.life / p.maxLife;
                if (k >= 1f || p.rect == null)
                {
                    if (p.rect != null)
                    {
                        Destroy(p.rect.gameObject);
                    }
                    popups.RemoveAt(i);
                    continue;
                }

                p.rect.position += new Vector3(0f, 60f * Time.deltaTime, 0f);
                var c = p.text.color;
                c.a = 1f - k;
                p.text.color = c;
            }
        }
    }
}
