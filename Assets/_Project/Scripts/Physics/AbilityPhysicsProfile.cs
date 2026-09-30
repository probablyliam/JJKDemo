using JJKDemo.Combat.Abilities.Data;
using JJKDemo.Combat.Destruction;
using UnityEngine;

namespace JJKDemo.Combat.Physics
{
    public abstract class AbilityPhysicsProfile : ScriptableObject
    {
        [Tooltip("Which layers this physics profile is allowed to affect (e.g. Enemies, Props).")]
        [SerializeField] private LayerMask affectedLayers = ~0;
        [Tooltip("The maximum number of targets that can be affected at once for performance.")]
        [SerializeField] private int maxTargets = 32;

        public LayerMask AffectedLayers => affectedLayers;
        public int MaxTargets => Mathf.Max(1, maxTargets);

        public abstract float GetQueryRadius(float sphereRadius);

        public abstract void ApplyContinuous(Collider target, Vector3 center, float radius, float charge, bool isRelease);

        public virtual void ApplyDirectImpact(Collider target, Vector3 position, Vector3 direction, float radius, float charge, bool isRelease)
        {
        }

        protected static void ApplyForce(Collider target, Vector3 force, ForceMode mode)
        {
            if (target.attachedRigidbody != null && !target.attachedRigidbody.isKinematic)
            {
                target.attachedRigidbody.AddForce(force, mode);
            }
        }

        protected static void ApplyDamage(Collider target, AbilityEffectMode effectMode, Vector3 point, Vector3 direction, float radius, float amount, bool isRelease)
        {
            var payload = new ImpactPayload(effectMode, point, direction, radius, amount, isRelease);
            foreach (var damageable in target.GetComponentsInParent<IDamageable>())
            {
                damageable.ApplyDamage(amount, payload);
            }
        }

        protected static float DistanceToSurface(Vector3 center, Vector3 bodyCenter, float radius)
        {
            return Mathf.Abs(Vector3.Distance(bodyCenter, center) - radius);
        }

        protected static float GetPowerMultiplier(Collider target)
        {
            // Hook for per-target force scaling; currently uniform.
            return 1f;
        }

        protected static Vector3 GetBodyCenter(Collider target)
        {
            return target.attachedRigidbody != null ? target.attachedRigidbody.worldCenterOfMass : target.bounds.center;
        }
    }
}
