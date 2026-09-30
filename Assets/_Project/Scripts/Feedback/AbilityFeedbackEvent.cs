using JJKDemo.Combat.Abilities.Data;
using UnityEngine;

namespace JJKDemo.Combat.Feedback
{
    public readonly struct AbilityFeedbackEvent
    {
        public AbilityFeedbackEvent(AbilityDefinition ability, AbilityFeedbackPhase phase, Vector3 position, Vector3 direction, float charge, float radius, float linearCharge = -1f)
        {
            Ability = ability;
            Phase = phase;
            Position = position;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Charge = charge;
            Radius = radius;
            LinearCharge = linearCharge >= 0f ? Mathf.Clamp01(linearCharge) : Mathf.Clamp01(charge);
        }

        public AbilityDefinition Ability { get; }
        public AbilityFeedbackPhase Phase { get; }
        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public float Charge { get; }
        public float LinearCharge { get; }
        public float Radius { get; }
    }
}
