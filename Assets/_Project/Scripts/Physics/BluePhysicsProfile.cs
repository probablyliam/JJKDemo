using UnityEngine;

namespace JJKDemo.Combat.Physics
{
    [CreateAssetMenu(menuName = "JJKDemo/Physics/Blue Gravity Profile")]
    public sealed class BluePhysicsProfile : AbilityPhysicsProfile
    {
        [Header("Pull")]
        [Tooltip("The base physical sucking force pulling objects towards the center.")]
        [SerializeField] private float pullStrength = 65f;
        [Tooltip("How far out from the sphere the pull effect reaches.")]
        [SerializeField] private float pullRange = 12f;
        [Tooltip("The minimum pull percentage applied at the very edge of the pull range.")]
        [SerializeField] private float minimumPullFalloff = 0.25f;
        [Tooltip("Controls how the pull strength drops off from the center to the max range.")]
        [SerializeField] private AnimationCurve pullFalloff = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Orbit")]
        [Tooltip("Determines how far out objects orbit relative to the sphere's actual radius (1.2 = 20% further out).")]
        [SerializeField] private float orbitRadiusMultiplier = 1.28f;
        [Tooltip("Flat distance added to the orbit radius (helps keep large objects from clipping).")]
        [SerializeField] private float orbitRadiusOffset = 2f;
        [Tooltip("How thick the 'ring' is where objects are considered trapped in orbit.")]
        [SerializeField] private float orbitZoneThickness = 2.6f;
        [Tooltip("How hard objects are thrown sideways to start spinning around the sphere.")]
        [SerializeField] private float orbitForce = 16f;
        [Tooltip("The target speed objects try to reach while spinning.")]
        [SerializeField] private float orbitSpeed = 6.2f;
        [Tooltip("Randomizes the orbit speed per object so they don't all spin in unison.")]
        [SerializeField] private float orbitSpeedVariation = 0.35f;
        [Tooltip("Randomizes the tilt/axis of the orbit per object for a chaotic swirling effect.")]
        [SerializeField] private float orbitAxisVariation = 0.55f;
        [Tooltip("How hard the sphere keeps pulling objects inward while they are trapped in orbit.")]
        [SerializeField] private float orbitDrainStrength = 0.22f;
        [Tooltip("Target speed for objects slowly spiraling inward toward the center.")]
        [SerializeField] private float orbitInwardSpeed = 1.4f;

        [Header("Stabilization")]
        [Tooltip("Extra distance kept from the surface so orbiting objects don't scrape the sphere model.")]
        [SerializeField] private float surfaceClearance = 0.75f;
        [Tooltip("If an object gets too close to the surface, this force pushes it back out to the orbit radius.")]
        [SerializeField] private float surfaceRepelStrength = 28f;
        [Tooltip("How quickly inward/outward bouncing is dampened once caught in the orbit zone.")]
        [SerializeField] private float radialDamping = 5f;
        [Tooltip("How quickly the object's erratic speed is dampened into a smooth orbit.")]
        [SerializeField] private float orbitDamping = 7f;
        [Tooltip("Overall friction applied to objects anywhere in the field to prevent them launching away.")]
        [SerializeField] private float fieldDamping = 0.8f;
        [Tooltip("Applies a constant upward force to counteract world gravity while trapped.")]
        [SerializeField] private float liftStabilization = 0.32f;
        [Tooltip("The absolute maximum speed an object can be sucked inward.")]
        [SerializeField] private float maxRadialSpeed = 6.5f;
        [Tooltip("The absolute maximum speed an object can spin around the sphere.")]
        [SerializeField] private float maxOrbitSpeed = 7.5f;
        [Tooltip("The absolute maximum combined speed an object can move.")]
        [SerializeField] private float maxTotalSpeed = 12f;

        [Header("Scaling")]
        [Tooltip("How the physics engine applies the force. Acceleration ignores mass, meaning heavy and light objects are sucked in equally.")]
        [SerializeField] private ForceMode forceMode = ForceMode.Force;

        public override float GetQueryRadius(float sphereRadius)
        {
            var orbitRadius = GetTargetOrbitRadius(sphereRadius);
            var scaledPullRange = GetScaledPullRange(sphereRadius);
            var scaledOrbitZone = GetScaledOrbitZoneThickness(sphereRadius);
            return orbitRadius + scaledPullRange + scaledOrbitZone * 0.5f;
        }

        public override void ApplyContinuous(Collider target, Vector3 center, float radius, float charge, bool isRelease)
        {
            var body = target.attachedRigidbody;
            if (body == null)
            {
                return;
            }

            var bodyCenter = GetBodyCenter(target);
            var fromCenter = bodyCenter - center;
            if (fromCenter.sqrMagnitude < 0.0001f)
            {
                fromCenter = target.transform.position - center;
            }

            if (fromCenter.sqrMagnitude < 0.0001f)
            {
                fromCenter = Vector3.up;
            }

            var outward = fromCenter.normalized;
            var distanceFromCenter = Mathf.Max(0.01f, fromCenter.magnitude);
            var targetOrbitRadius = GetTargetOrbitRadius(radius);
            var scaledOrbitZone = GetScaledOrbitZoneThickness(radius);
            var scaledPullRange = GetScaledPullRange(radius);
            var influenceRadius = targetOrbitRadius + scaledOrbitZone * 0.5f + scaledPullRange;

            if (distanceFromCenter > influenceRadius)
            {
                return;
            }

            var radialError = distanceFromCenter - targetOrbitRadius;
            var orbitZoneHalfWidth = Mathf.Max(0.05f, scaledOrbitZone * 0.5f);
            var orbitBlend = 1f - Mathf.Clamp01(Mathf.Abs(radialError) / orbitZoneHalfWidth);
            var pullBlend = Mathf.Clamp01((distanceFromCenter - (targetOrbitRadius + orbitZoneHalfWidth)) / Mathf.Max(0.01f, scaledPullRange));
            
            // Linear charge scaling (0 to 1) just like Red.
            var chargeMultiplier = charge;
            var powerMultiplier = GetPowerMultiplier(target);
            var forceScale = powerMultiplier;
            var safeSurfaceRadius = radius + Mathf.Max(0f, surfaceClearance);

            var pullFalloffScale = EvaluatePullFalloff(1f - pullBlend);
            if (pullBlend > 0f)
            {
                var pull = -outward * pullStrength * chargeMultiplier * forceScale * pullFalloffScale;
                ApplyForce(target, pull, forceMode);
            }

            var captureBlend = Mathf.Clamp01(1f - pullBlend);
            if (captureBlend > 0f)
            {
                ApplyOrbitForces(target, body, outward, distanceFromCenter, safeSurfaceRadius, orbitBlend, captureBlend, chargeMultiplier, forceScale);
            }

            ShapeVelocity(body, outward, distanceFromCenter, safeSurfaceRadius, radialError, orbitBlend, pullFalloffScale, chargeMultiplier, forceScale);
        }

        private float EvaluatePullFalloff(float gravityBlend)
        {
            return Mathf.Lerp(minimumPullFalloff, 1f, pullFalloff.Evaluate(Mathf.Clamp01(gravityBlend)));
        }

        private void ApplyOrbitForces(Collider target, Rigidbody body, Vector3 outward, float distanceFromCenter, float safeSurfaceRadius, float orbitBlend, float captureBlend, float chargeMultiplier, float forceScale)
        {
            var orbitAxis = GetOrbitAxis(body);
            var tangent = GetTangent(orbitAxis, outward);
            var variation = GetSignedVariation(body);
            var orbitDirection = variation < 0f ? -1f : 1f;
            var orbitForceScale = orbitBlend;
            var orbit = tangent * orbitDirection * orbitForce * chargeMultiplier * forceScale * orbitForceScale;
            var drain = -outward * pullStrength * orbitDrainStrength * chargeMultiplier * forceScale * captureBlend;
            var surfaceDepth = Mathf.Max(0f, safeSurfaceRadius - distanceFromCenter);
            var surfaceRepel = outward * surfaceDepth * surfaceRepelStrength;
            var lift = Vector3.up * liftStabilization * captureBlend;

            ApplyForce(target, orbit + drain + surfaceRepel + lift, forceMode);
        }

        private void ShapeVelocity(Rigidbody body, Vector3 outward, float distanceFromCenter, float safeSurfaceRadius, float radialError, float orbitBlend, float pullFalloffScale, float chargeMultiplier, float forceScale)
        {
            if (body == null || body.isKinematic) return;
            var velocity = body.linearVelocity;
            var orbitAxis = GetOrbitAxis(body);
            var radialVelocity = Vector3.Project(velocity, outward);
            var tangentialVelocity = velocity - radialVelocity;
            var tangent = GetTangent(orbitAxis, outward);
            var variation = GetSignedVariation(body);
            var orbitDirection = variation < 0f ? -1f : 1f;

            if (radialError > 0f && orbitBlend < 1f)
            {
                var desiredPullSpeed = pullStrength * chargeMultiplier * forceScale * pullFalloffScale * 0.14f;
                if (maxRadialSpeed > 0f)
                {
                    desiredPullSpeed = Mathf.Min(desiredPullSpeed, maxRadialSpeed);
                }

                var pullVelocityBlend = Mathf.Clamp01((2f + pullFalloffScale * 8f) * (1f - orbitBlend) * Time.deltaTime);
                radialVelocity = Vector3.Lerp(radialVelocity, -outward * desiredPullSpeed, pullVelocityBlend);
            }

            if (orbitBlend > 0f)
            {
                var desiredDrainSpeed = orbitInwardSpeed * Mathf.Lerp(0.45f, 1f, orbitBlend);
                var desiredDrainVelocity = -outward * desiredDrainSpeed;
                var drainBlend = Mathf.Clamp01(radialDamping * orbitBlend * Time.deltaTime);
                radialVelocity = Vector3.Lerp(radialVelocity, desiredDrainVelocity, drainBlend);
            }

            if (distanceFromCenter < safeSurfaceRadius && Vector3.Dot(radialVelocity, outward) < 0f)
            {
                radialVelocity = Vector3.ProjectOnPlane(radialVelocity, outward);
            }

            if (maxRadialSpeed > 0f)
            {
                var radialSpeedLimit = Mathf.Lerp(maxRadialSpeed, maxRadialSpeed * 0.45f, orbitBlend);
                radialVelocity = Vector3.ClampMagnitude(radialVelocity, radialSpeedLimit);
            }

            var speedVariation = Mathf.Max(0f, orbitSpeedVariation);
            var variedOrbitSpeed = orbitSpeed * Mathf.Lerp(1f - speedVariation, 1f + speedVariation, Mathf.Abs(variation));
            var desiredOrbitSpeed = maxOrbitSpeed > 0f ? Mathf.Min(variedOrbitSpeed, maxOrbitSpeed) : variedOrbitSpeed;
            var desiredTangentialVelocity = tangent * orbitDirection * desiredOrbitSpeed;
            var orbitDampingAmount = Mathf.Clamp01(orbitDamping * orbitBlend * Time.deltaTime);
            tangentialVelocity = Vector3.Lerp(tangentialVelocity, desiredTangentialVelocity, orbitDampingAmount);

            if (maxOrbitSpeed > 0f)
            {
                tangentialVelocity = Vector3.ClampMagnitude(tangentialVelocity, maxOrbitSpeed);
            }

            var shapedVelocity = radialVelocity + tangentialVelocity;
            var fieldDampingAmount = Mathf.Clamp01(fieldDamping * Time.deltaTime);
            shapedVelocity = Vector3.Lerp(shapedVelocity, shapedVelocity * 0.9f, fieldDampingAmount);

            if (maxTotalSpeed > 0f)
            {
                shapedVelocity = Vector3.ClampMagnitude(shapedVelocity, maxTotalSpeed);
            }

            body.linearVelocity = shapedVelocity;
        }

        private float GetTargetOrbitRadius(float sphereRadius)
        {
            return Mathf.Max(0.05f, sphereRadius * Mathf.Max(0.01f, orbitRadiusMultiplier) + Mathf.Max(0f, orbitRadiusOffset));
        }

        private float GetScaledPullRange(float sphereRadius)
        {
            return Mathf.Max(0.01f, pullRange);
        }

        private float GetScaledOrbitZoneThickness(float sphereRadius)
        {
            return Mathf.Max(0.05f, orbitZoneThickness);
        }

        private Vector3 GetOrbitAxis(Rigidbody body)
        {
            var seed = GetSeed(body);
            var x = Mathf.Sin(seed * 12.9898f) * orbitAxisVariation;
            var y = 0.55f + Mathf.Abs(Mathf.Sin(seed * 78.233f)) * 0.45f;
            var z = Mathf.Cos(seed * 37.719f) * orbitAxisVariation;
            return new Vector3(x, y, z).normalized;
        }

        private static Vector3 GetTangent(Vector3 orbitAxis, Vector3 outward)
        {
            var tangent = Vector3.Cross(orbitAxis, outward);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector3.Cross(Vector3.up, outward);
            }

            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector3.Cross(Vector3.right, outward);
            }

            return tangent.normalized;
        }

        private static float GetSignedVariation(Rigidbody body)
        {
            return Mathf.Sin(GetSeed(body) * 43.1337f);
        }

        private static float GetSeed(Rigidbody body)
        {
            var position = body.transform.position;
            return body.GetEntityId().GetHashCode() * 0.137f + position.x * 12.9898f + position.y * 78.233f + position.z * 37.719f;
        }
    }
}
