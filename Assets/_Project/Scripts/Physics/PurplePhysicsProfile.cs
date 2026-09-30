using JJKDemo.Combat.Abilities.Data;
using UnityEngine;

namespace JJKDemo.Combat.Physics
{
    [CreateAssetMenu(menuName = "JJKDemo/Physics/Purple Destruction Profile")]
    public sealed class PurplePhysicsProfile : AbilityPhysicsProfile
    {
        [SerializeField] private float damage = 9999f;

        public override float GetQueryRadius(float sphereRadius)
        {
            return sphereRadius;
        }

        public override void ApplyContinuous(Collider target, Vector3 center, float radius, float charge, bool isRelease)
        {
            // When the projectile is traveling, we ONLY want to use ApplyDirectImpact to calculate the exact surface hit!
            if (isRelease) return;
            
            ApplyDamage(target, AbilityEffectMode.Destroy, target.ClosestPoint(center), Vector3.zero, radius, damage, isRelease);
        }

        public override void ApplyDirectImpact(Collider target, Vector3 position, Vector3 direction, float radius, float charge, bool isRelease)
        {
            ApplyDamage(target, AbilityEffectMode.Destroy, target.ClosestPoint(position), direction, radius, damage, isRelease);
        }
    }
}
