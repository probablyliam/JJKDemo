using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace JJKDemo.Combat.Trial
{
    public enum TrialState
    {
        Ready,     // waiting for the player to click to begin (absorbs the editor focus click)
        Countdown, // 3..2..1 before the run starts
        Running,   // timer counting down, abilities live
        Finished   // results shown
    }

    /// <summary>
    /// Drives the Cursed Energy Trial: a short countdown, a timed destruction run with limited
    /// ability uses, percentage-based scoring, grade calculation, and a results/restart screen.
    /// Owns no UI — it raises events the HUD binds to.
    /// </summary>
    public sealed class CursedEnergyTrialManager : MonoBehaviour
    {
        public static CursedEnergyTrialManager Instance { get; private set; }

        [Header("Trial")]
        [Tooltip("Length of the destruction run, in seconds.")]
        [SerializeField] private float trialDuration = 90f;
        [Tooltip("Seconds of 3..2..1 countdown before the run begins.")]
        [SerializeField] private float startCountdown = 3f;

        [Header("Ability Uses (pushed into the limiter)")]
        [SerializeField] private TrialAbilityLimiter limiter;
        [SerializeField] private int redUses = 8;
        [SerializeField] private int blueUses = 6;
        [SerializeField] private int purpleUses = 2;

        [Header("Arena Bounds")]
        [SerializeField] private Vector3 arenaCenter = Vector3.zero;
        [Tooltip("Horizontal distance from centre past which objects count as knocked out of the arena. 0 = ignore.")]
        [SerializeField] private float arenaRadius = 70f;
        [Tooltip("Objects (and the player) below this height are out of bounds.")]
        [SerializeField] private float killY = -12f;

        [Header("Player")]
        [SerializeField] private Transform player;

        private TrialState state;
        private float timeRemaining;
        private float countdown;
        private int totalValue;
        private int destroyedValue;
        private Vector3 playerStart;

        // --- Events the UI binds to -------------------------------------------------------------
        public event Action<TrialState> StateChanged;
        public event Action<int, Vector3, DestructibleScoreTarget.ScoreReason> ScoreRecorded; // value, worldPos, reason

        // --- Read surface -----------------------------------------------------------------------
        public TrialState State => state;
        public bool IsRunning => state == TrialState.Running;
        public bool IsFinished => state == TrialState.Finished;
        public float TimeRemaining => Mathf.Max(0f, timeRemaining);
        public float TrialDuration => trialDuration;
        public float Countdown => Mathf.Max(0f, countdown);
        public int TotalValue => totalValue;
        public int DestroyedValue => destroyedValue;
        public TrialAbilityLimiter Limiter => limiter;

        public float DestructionPercent =>
            totalValue > 0 ? Mathf.Clamp(100f * destroyedValue / totalValue, 0f, 100f) : 0f;

        public string CurrentRankName
        {
            get
            {
                TrialRanks.Resolve(DestructionPercent, out var name, out _);
                return name;
            }
        }

        public Color CurrentRankColor
        {
            get
            {
                TrialRanks.Resolve(DestructionPercent, out _, out var color);
                return color;
            }
        }

        private void Awake()
        {
            Instance = this;

            if (player == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null)
                {
                    player = tagged.transform;
                }
            }

            if (limiter == null && player != null)
            {
                limiter = player.GetComponent<TrialAbilityLimiter>();
            }

            if (player != null)
            {
                playerStart = player.position;
            }

            GameplayInputGate.InputEnabled = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // Never leave the shared input gate locked for the next scene.
            GameplayInputGate.Reset();
        }

        private void Start()
        {
            if (limiter != null)
            {
                limiter.Configure(redUses, blueUses, purpleUses);
            }

            EnterReady();
        }

        private void Update()
        {
            switch (state)
            {
                case TrialState.Ready:
                    // Wait for the player to click in — this swallows the click that focuses the
                    // game window, so it can't be misread as a (now wasted) ability use.
                    if (WasBeginPressed())
                    {
                        BeginCountdown();
                    }
                    break;

                case TrialState.Countdown:
                    countdown -= Time.deltaTime;
                    if (countdown <= 0f)
                    {
                        BeginTrial();
                    }
                    break;

                case TrialState.Running:
                    timeRemaining -= Time.deltaTime;
                    EnforcePlayerBounds();
                    if (timeRemaining <= 0f)
                    {
                        FinishTrial();
                    }
                    break;

                case TrialState.Finished:
                    if (WasRestartPressed())
                    {
                        Restart();
                    }
                    break;
            }
        }

        // --- State flow -------------------------------------------------------------------------
        private void EnterReady()
        {
            state = TrialState.Ready;
            timeRemaining = trialDuration;
            countdown = startCountdown;
            destroyedValue = 0;
            GameplayInputGate.InputEnabled = false; // hold the player still until the run starts
            StateChanged?.Invoke(state);
        }

        private void BeginCountdown()
        {
            state = TrialState.Countdown;
            countdown = startCountdown;
            GameplayInputGate.InputEnabled = false;
            StateChanged?.Invoke(state);
        }

        private void BeginTrial()
        {
            state = TrialState.Running;
            countdown = 0f;
            GameplayInputGate.InputEnabled = true;
            StateChanged?.Invoke(state);
        }

        private void FinishTrial()
        {
            state = TrialState.Finished;
            timeRemaining = 0f;
            GameplayInputGate.InputEnabled = false; // lock control on the results screen
            StateChanged?.Invoke(state);
        }

        public void Restart()
        {
            GameplayInputGate.Reset();
            SceneManager.LoadScene(gameObject.scene.name);
        }

        // --- Scoring API (called by DestructibleScoreTarget) ------------------------------------
        public void RegisterTarget(int value)
        {
            totalValue += Mathf.Max(0, value);
        }

        public void ReportDestroyed(int value, Vector3 worldPosition, DestructibleScoreTarget.ScoreReason reason)
        {
            destroyedValue += Mathf.Max(0, value);
            ScoreRecorded?.Invoke(value, worldPosition, reason);
        }

        public bool IsOutOfBounds(Vector3 position)
        {
            if (position.y < killY)
            {
                return true;
            }

            if (arenaRadius > 0f)
            {
                var flat = position - arenaCenter;
                flat.y = 0f;
                if (flat.sqrMagnitude > arenaRadius * arenaRadius)
                {
                    return true;
                }
            }

            return false;
        }

        // Keep the player from falling out of the world — teleport back to spawn rather than die.
        private void EnforcePlayerBounds()
        {
            if (player == null || player.position.y >= killY)
            {
                return;
            }

            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.position = playerStart + Vector3.up * 0.25f;

            if (controller != null)
            {
                controller.enabled = true;
            }
        }

        private static bool WasBeginPressed()
        {
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            {
                return true;
            }

            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
        }

        private static bool WasRestartPressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
        }
    }
}
