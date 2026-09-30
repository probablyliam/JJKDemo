using JJKDemo.Combat.Abilities.Data;
using JJKDemo.Combat.Destruction;
using JJKDemo.Combat.Physics;
using UnityEngine;

namespace JJKDemo.Combat.Trial
{
    /// <summary>
    /// Gives a destructible object a point value for the Cursed Energy Trial and reports it to
    /// the manager exactly once, the first time the object is "meaningfully broken". That can be:
    ///   * Deleted   — a Purple hit (handled instantly via <see cref="IDamageable"/> Destroy mode,
    ///                 with <see cref="OnDestroy"/> as a fall-back for anything that just despawns).
    ///   * Detached  — a FixedJoint/joint holding it to a structure snaps (<see cref="OnJointBreak"/>).
    ///   * Displaced — knocked far from where it started by Red/Blue (toppled, flung, pulled away).
    ///   * OutOfBounds — flung below the kill plane or outside the arena radius.
    ///
    /// Scoring uses the assigned value only (no runtime volume maths), so it is cheap and reliable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DestructibleScoreTarget : MonoBehaviour, IDamageable
    {
        public enum ScoreReason
        {
            Deleted,
            Detached,
            Displaced,
            OutOfBounds
        }

        [Tooltip("Points this object contributes to total destruction when broken.")]
        [SerializeField] private int pointValue = 10;

        [Tooltip("How far (metres) the object must move from its start before it counts as knocked away.")]
        [SerializeField] private float displaceDistance = 2.5f;

        [Tooltip("Score this object if it is destroyed/despawned by anything (e.g. Purple dissolve).")]
        [SerializeField] private bool scoreOnDestroy = true;

        private CursedEnergyTrialManager manager;
        private Vector3 spawnPosition;
        private float displaceSqr;
        private float checkTimer;
        private bool scored;
        private bool registered;
        private bool baselined;

        public int PointValue => pointValue;
        public bool Scored => scored;

        private void Start()
        {
            manager = CursedEnergyTrialManager.Instance;
            spawnPosition = transform.position;
            displaceSqr = displaceDistance * displaceDistance;

            if (manager != null)
            {
                manager.RegisterTarget(Mathf.Max(0, pointValue));
                registered = true;
            }
        }

        private void Update()
        {
            if (scored || manager == null || !manager.IsRunning)
            {
                return;
            }

            // Re-baseline the reference position the first frame the run goes live, so any physics
            // settling during the countdown (piles nudging into place) doesn't read as displacement.
            if (!baselined)
            {
                spawnPosition = transform.position;
                baselined = true;
                return;
            }

            // Throttle the spatial checks — there can be hundreds of these in the arena.
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f)
            {
                return;
            }
            checkTimer = 0.15f;

            if (manager.IsOutOfBounds(transform.position))
            {
                Score(ScoreReason.OutOfBounds);
                // Remove far-flung debris so it cannot pile up or fall forever.
                Destroy(gameObject, 0.05f);
                return;
            }

            if ((transform.position - spawnPosition).sqrMagnitude >= displaceSqr)
            {
                Score(ScoreReason.Displaced);
            }
        }

        // Purple routes here with Destroy mode; score immediately for snappy feedback.
        // Red/Blue also call this (Push/Pull) but must NOT instantly score — they score via
        // displacement once the object is actually knocked away.
        public void ApplyDamage(float amount, ImpactPayload payload)
        {
            if (payload.EffectMode == AbilityEffectMode.Destroy)
            {
                Score(ScoreReason.Deleted);
            }
        }

        private void OnJointBreak(float breakForce)
        {
            Score(ScoreReason.Detached);
        }

        private void OnDestroy()
        {
            if (!scoreOnDestroy || scored)
            {
                return;
            }

            // Ignore destruction caused by scene unload / leaving play mode.
            if (!gameObject.scene.isLoaded)
            {
                return;
            }

            Score(ScoreReason.Deleted);
        }

        private void Score(ScoreReason reason)
        {
            if (scored)
            {
                return;
            }

            // Only count while a trial is actually running.
            if (manager == null)
            {
                manager = CursedEnergyTrialManager.Instance;
            }

            if (manager == null || !manager.IsRunning)
            {
                return;
            }

            // A late-registered target (created after Start) still contributes its value to the total.
            if (!registered)
            {
                manager.RegisterTarget(Mathf.Max(0, pointValue));
                registered = true;
            }

            scored = true;
            manager.ReportDestroyed(Mathf.Max(0, pointValue), transform.position, reason);
        }
    }
}
