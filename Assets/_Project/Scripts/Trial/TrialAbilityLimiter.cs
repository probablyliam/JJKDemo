using System;
using JJKDemo.Combat.Abilities.Runtime;
using JJKDemo.Combat.Input;
using UnityEngine;

namespace JJKDemo.Combat.Trial
{
    /// <summary>
    /// Trial-only layer that limits how many times each ability can be used. It does NOT change
    /// how abilities work internally — it simply plugs into two additive hooks on
    /// <see cref="AbilityController"/>:
    ///   * <see cref="AbilityController.CanActivate"/> — a gate that blocks a colour from
    ///     beginning (or Red+Blue from combining into Purple) when that colour is out of uses,
    ///     or while the trial is not running.
    ///   * <see cref="AbilityController.AbilityCommitted"/> / <see cref="AbilityController.AbilityRefunded"/> —
    ///     a use is spent on the press that commits an ability, and refunded if it is upgraded into Purple.
    ///
    /// Purple is deliberately scarce; Red and Blue are more plentiful but still finite, so the
    /// player must think about charge timing and target choice.
    /// </summary>
    [RequireComponent(typeof(AbilityController))]
    public sealed class TrialAbilityLimiter : MonoBehaviour
    {
        [Header("Starting Uses")]
        [SerializeField] private int redUses = 8;
        [SerializeField] private int blueUses = 6;
        [Tooltip("Purple is extremely powerful — keep this very small.")]
        [SerializeField] private int purpleUses = 2;

        private AbilityController controller;
        private int red;
        private int blue;
        private int purple;

        /// <summary>Raised whenever a remaining count changes (or uses are reset).</summary>
        public event Action UsesChanged;

        public int Red => red;
        public int Blue => blue;
        public int Purple => purple;
        public int RedMax => redUses;
        public int BlueMax => blueUses;
        public int PurpleMax => purpleUses;

        private void Awake()
        {
            controller = GetComponent<AbilityController>();
            ResetUses();
        }

        private void OnEnable()
        {
            if (controller == null)
            {
                controller = GetComponent<AbilityController>();
            }

            if (controller != null)
            {
                controller.CanActivate = CanActivate;
                controller.AbilityCommitted += OnAbilityCommitted;
                controller.AbilityRefunded += OnAbilityRefunded;
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                if (controller.CanActivate == CanActivate)
                {
                    controller.CanActivate = null;
                }

                controller.AbilityCommitted -= OnAbilityCommitted;
                controller.AbilityRefunded -= OnAbilityRefunded;
            }
        }

        /// <summary>Sets the starting budget and resets remaining uses to it.</summary>
        public void Configure(int redCount, int blueCount, int purpleCount)
        {
            redUses = Mathf.Max(0, redCount);
            blueUses = Mathf.Max(0, blueCount);
            purpleUses = Mathf.Max(0, purpleCount);
            ResetUses();
        }

        public void ResetUses()
        {
            red = redUses;
            blue = blueUses;
            purple = purpleUses;
            UsesChanged?.Invoke();
        }

        public int Remaining(AbilityChannel channel)
        {
            switch (channel)
            {
                case AbilityChannel.Red: return red;
                case AbilityChannel.Blue: return blue;
                case AbilityChannel.Purple: return purple;
                default: return 0;
            }
        }

        public int Max(AbilityChannel channel)
        {
            switch (channel)
            {
                case AbilityChannel.Red: return redUses;
                case AbilityChannel.Blue: return blueUses;
                case AbilityChannel.Purple: return purpleUses;
                default: return 0;
            }
        }

        // Gate: an ability may activate only if it has uses left and the trial is live.
        private bool CanActivate(AbilityChannel channel)
        {
            var manager = CursedEnergyTrialManager.Instance;
            if (manager != null && !manager.IsRunning)
            {
                return false;
            }

            return Remaining(channel) > 0;
        }

        // Consume: a use is spent the moment an ability is committed (the button press that spawns it).
        private void OnAbilityCommitted(AbilityChannel channel)
        {
            switch (channel)
            {
                case AbilityChannel.Red: red = Mathf.Max(0, red - 1); break;
                case AbilityChannel.Blue: blue = Mathf.Max(0, blue - 1); break;
                case AbilityChannel.Purple: purple = Mathf.Max(0, purple - 1); break;
                default: return;
            }

            UsesChanged?.Invoke();
        }

        // Refund: a just-committed single colour is given back when it is immediately upgraded into
        // Purple (or swapped on a same-frame tie), so only the final ability is charged for.
        private void OnAbilityRefunded(AbilityChannel channel)
        {
            switch (channel)
            {
                case AbilityChannel.Red: red = Mathf.Min(redUses, red + 1); break;
                case AbilityChannel.Blue: blue = Mathf.Min(blueUses, blue + 1); break;
                case AbilityChannel.Purple: purple = Mathf.Min(purpleUses, purple + 1); break;
                default: return;
            }

            UsesChanged?.Invoke();
        }
    }
}
