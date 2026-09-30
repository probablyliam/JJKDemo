using UnityEngine;

namespace JJKDemo.Combat.Abilities.Data
{
    [CreateAssetMenu(menuName = "JJKDemo/Abilities/Held Sphere Profile")]
    public sealed class HeldSphereProfile : ScriptableObject
    {
        [SerializeField] private float maxChargeTime = 10f;
        [Tooltip("Once fully charged, how long the ability can be held before it auto-fires. " +
                 "Stops the player from holding a max-power ability indefinitely. Tweak per ability.")]
        [SerializeField] private float maxChargeHoldTime = 2f;
        [SerializeField] private float minRadius = 0f;
        [SerializeField] private float maxRadius = 2.4f;
        [SerializeField] private float tickInterval = 0.06f;
        [SerializeField] private AnimationCurve chargeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float MaxChargeTime => Mathf.Max(0.01f, maxChargeTime);
        public float MaxChargeHoldTime => Mathf.Max(0f, maxChargeHoldTime);
        public float MinRadius => Mathf.Max(0.01f, minRadius);
        public float MaxRadius => Mathf.Max(MinRadius, maxRadius);
        public float TickInterval => Mathf.Max(0.01f, tickInterval);

        public float EvaluateCharge(float chargeTime)
        {
            var normalized = Mathf.Clamp01(chargeTime / MaxChargeTime);
            return Mathf.Clamp01(chargeCurve.Evaluate(normalized));
        }

        public float EvaluateRadius(float chargeTime)
        {
            return Mathf.Lerp(MinRadius, MaxRadius, EvaluateCharge(chargeTime));
        }
    }
}
