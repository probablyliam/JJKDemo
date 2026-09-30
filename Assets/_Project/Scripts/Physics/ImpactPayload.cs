using JJKDemo.Combat.Abilities.Data;
using UnityEngine;

namespace JJKDemo.Combat.Physics
{
    public readonly struct ImpactPayload
    {
        public ImpactPayload(AbilityEffectMode effectMode, Vector3 point, Vector3 direction, float radius, float intensity, bool isRelease)
        {
            EffectMode = effectMode;
            Point = point;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            Radius = radius;
            Intensity = intensity;
            IsRelease = isRelease;
        }

        public AbilityEffectMode EffectMode { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float Radius { get; }
        public float Intensity { get; }
        public bool IsRelease { get; }
    }
}
