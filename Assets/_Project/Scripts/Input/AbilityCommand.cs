using UnityEngine;

namespace JJKDemo.Combat.Input
{
    public readonly struct AbilityCommand
    {
        public AbilityCommand(AbilityChannel channel, AbilityCommandPhase phase, Vector3 aimDirection)
        {
            Channel = channel;
            Phase = phase;
            AimDirection = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector3.forward;
        }

        public AbilityChannel Channel { get; }
        public AbilityCommandPhase Phase { get; }
        public Vector3 AimDirection { get; }
    }
}
