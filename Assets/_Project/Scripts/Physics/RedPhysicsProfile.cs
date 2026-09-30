using JJKDemo.Combat.Abilities.Data;
using UnityEngine;

namespace JJKDemo.Combat.Physics
{
    [CreateAssetMenu(menuName = "JJKDemo/Physics/Red Push Profile")]
    public sealed class RedPhysicsProfile : AbilityPhysicsProfile
    {
        [Tooltip("The base physical pushing force applied to objects caught in the blast.")]
        [SerializeField] private float force = 62f;
        [Tooltip("How far out from the sphere's surface the pushing force reaches.")]
        [SerializeField] private float shellThickness = 8f;
        [Tooltip("Multiplier applied to the force while the user is holding/charging the ability (e.g. 0.1 for a weak gentle push).")]
        [SerializeField] private float heldForceMultiplier = 0.1f;
        [Tooltip("Adds a slight upward angle to the push direction, helping lift objects off the ground.")]
        [SerializeField] private float upwardModifier = 0.22f;
        [Tooltip("How the physics engine applies the force. VelocityChange ignores the mass of the target.")]
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
        [Tooltip("The minimum force percentage (0 to 1) applied to targets at the very edge of the shell thickness.")]
        [SerializeField] private float minimumFalloff = 0.08f;
        [Tooltip("Controls how the force drops off from the surface of the sphere to the outer edge of the shell.")]
        [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [Tooltip("The maximum speed an object can be pushed. Prevents objects from being launched into orbit.")]
        [SerializeField] private float maxSpeed = 22f;
        [Tooltip("How quickly the target's speed slows down while caught in the continuous push area.")]
        [SerializeField] private float damping = 2.8f;
        


        public override float GetQueryRadius(float sphereRadius)
        {
            return sphereRadius + Mathf.Max(0.01f, shellThickness);
        }

        public override void ApplyContinuous(Collider target, Vector3 center, float radius, float charge, bool isRelease)
        {
            var bodyCenter = GetBodyCenter(target);
            var fromCenter = bodyCenter - center;
            if (fromCenter.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var distanceToSurface = DistanceToSurface(center, bodyCenter, radius);
            if (distanceToSurface > shellThickness)
            {
                return;
            }

            var normalizedShellDistance = Mathf.Clamp01(distanceToSurface / Mathf.Max(0.01f, shellThickness));
            
            // The baseline force scales linearly with charge (0 to 1).
            // If the projectile is flying (released), it uses full force. If we are holding it, it uses the weak held multiplier.
            float stateMultiplier = isRelease ? 1f : heldForceMultiplier;
            var forceScale = force * charge * stateMultiplier * EvaluateFalloff(normalizedShellDistance) * GetPowerMultiplier(target);

            var direction = (fromCenter.normalized + Vector3.up * upwardModifier).normalized;
            ApplyForce(target, direction * forceScale, forceMode);
            ShapeVelocity(target);
        }

        public override void ApplyDirectImpact(Collider target, Vector3 position, Vector3 direction, float radius, float charge, bool isRelease)
        {
            ApplyForce(target, direction.normalized * force * charge * GetPowerMultiplier(target), ForceMode.Impulse);
            ShapeVelocity(target);
        }

        private float EvaluateFalloff(float normalizedShellDistance)
        {
            return Mathf.Lerp(minimumFalloff, 1f, Mathf.Clamp01(falloff.Evaluate(Mathf.Clamp01(normalizedShellDistance))));
        }

        private void ShapeVelocity(Collider target)
        {
            var body = target.attachedRigidbody;
            if (body == null || body.isKinematic)
            {
                return;
            }

            var velocity = body.linearVelocity;
            if (maxSpeed > 0f && velocity.magnitude > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }

            var dampingAmount = Mathf.Clamp01(damping * Time.deltaTime);
            body.linearVelocity = Vector3.Lerp(velocity, velocity * 0.85f, dampingAmount);
        }
    }
}
