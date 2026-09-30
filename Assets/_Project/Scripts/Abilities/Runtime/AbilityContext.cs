using JJKDemo.Combat.Characters;
using JJKDemo.Combat.Physics;
using UnityEngine;

namespace JJKDemo.Combat.Abilities.Runtime
{
    public readonly struct AbilityContext
    {
        public AbilityContext(CharacterContext caster, PhysicsInteractionService physicsService)
        {
            Caster = caster;
            PhysicsService = physicsService;
        }

        public CharacterContext Caster { get; }
        public PhysicsInteractionService PhysicsService { get; }
        public Vector3 SpawnPosition => Caster != null ? Caster.AbilitySpawnPosition : Vector3.zero;
        public Quaternion SpawnRotation => Caster != null ? Caster.AbilitySpawnRotation : Quaternion.identity;

        public Vector3 GetSphereCenterFromBackPoint(float radius)
        {
            return SpawnPosition + SpawnRotation * Vector3.forward * Mathf.Max(0f, radius);
        }
    }
}
