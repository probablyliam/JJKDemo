using JJKDemo.Combat.Abilities.Data;
using JJKDemo.Combat.Feedback;
using JJKDemo.Combat.Physics;
using UnityEngine;

namespace JJKDemo.Combat.Abilities.Runtime
{
    public enum AbilityState { Charge, Travel }

    [RequireComponent(typeof(SphereCollider))]
    public sealed class HeldSphereAbilityActor : MonoBehaviour
    {
        private Light abilityLight;
        private float maxLightIntensity;
        private float maxLightRange;
        
        private AbilityDefinition definition;
        private AbilityContext context;
        private SphereCollider sphereCollider;
        private Transform visual;
        
        private AbilityState currentState = AbilityState.Charge;
        private float chargeTime;
        private float tickTimer;
        private float projectileAge;
        private float timeAtMax;
        private bool autoFireRequested;

        private readonly System.Collections.Generic.HashSet<Transform> impactedRoots = new System.Collections.Generic.HashSet<Transform>();

        private Vector3 launchDirection;

        /// <summary>Raised once when a fully-charged ability has been held past its hold limit and
        /// must auto-fire. The owning controller listens and launches it.</summary>
        public event System.Action<HeldSphereAbilityActor> AutoFireRequested;

        public AbilityDefinition Definition => definition;
        public bool IsLaunched => currentState != AbilityState.Charge;

        /// <summary>True while charging and sitting at (near) full charge — used by the HUD to warn
        /// the player the shot is about to auto-fire.</summary>
        public bool AtMaxCharge => currentState == AbilityState.Charge && LinearCharge01 >= 0.999f;
        public float Charge01 => definition != null && definition.SphereProfile != null ? definition.SphereProfile.EvaluateCharge(chargeTime) : 0f;
        
        public float LinearCharge01 => definition != null && definition.SphereProfile != null ? Mathf.Clamp01(chargeTime / definition.SphereProfile.MaxChargeTime) : 0f;

        public float Radius => definition != null && definition.SphereProfile != null ? definition.SphereProfile.EvaluateRadius(chargeTime) : 0.5f;

        public void Initialize(AbilityDefinition abilityDefinition, AbilityContext abilityContext)
        {
            definition = abilityDefinition;
            context = abilityContext;
            sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            ApplyScale();

            AbilityFeedbackBus.Raise(CreateFeedback(AbilityFeedbackPhase.BeginHold));

            abilityLight = GetComponentInChildren<Light>();
            if (abilityLight != null)
            {
                maxLightIntensity = abilityLight.intensity;
                maxLightRange = abilityLight.range;
                abilityLight.intensity = 0f;
            }
        }

        public void Launch(Vector3 direction)
        {
            if (currentState != AbilityState.Charge) return;

            currentState = AbilityState.Travel;
            launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

            AbilityFeedbackBus.Raise(CreateFeedback(AbilityFeedbackPhase.Release));
        }

        public void Cancel()
        {
            AbilityFeedbackBus.Raise(CreateFeedback(AbilityFeedbackPhase.Cancel));
            Destroy(gameObject);
        }

        private void Update()
        {
            if (definition == null || definition.SphereProfile == null)
            {
                Destroy(gameObject);
                return;
            }

            if (currentState == AbilityState.Charge)
            {
                chargeTime = Mathf.Min(chargeTime + Time.deltaTime, definition.SphereProfile.MaxChargeTime);
                FollowCasterAim();
                ApplyScale();

                AbilityFeedbackBus.Raise(CreateFeedback(AbilityFeedbackPhase.HoldTick));
                TickEffect(false);

                // Safeguard: a fully-charged ability can only be held briefly before it auto-fires,
                // so the player can't carry a max-power shot around indefinitely.
                if (LinearCharge01 >= 0.999f)
                {
                    timeAtMax += Time.deltaTime;
                    if (!autoFireRequested && timeAtMax >= definition.SphereProfile.MaxChargeHoldTime)
                    {
                        autoFireRequested = true;
                        AutoFireRequested?.Invoke(this);
                    }
                }
                else
                {
                    timeAtMax = 0f;
                }
            }
            else if (currentState == AbilityState.Travel)
            {
                projectileAge += Time.deltaTime;
                transform.position += launchDirection * definition.ProjectileSpeed * Time.deltaTime;

                if (projectileAge >= definition.ProjectileLifetime)
                {
                    AbilityFeedbackBus.Raise(CreateFeedback(AbilityFeedbackPhase.Complete));
                    Destroy(gameObject);
                    return;
                }

                TickEffect(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
            if (impactedRoots.Add(root))
            {
                TickSingleImpact(other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (definition != null && definition.EffectMode == AbilityEffectMode.Destroy)
            {
                var root = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
                if (impactedRoots.Add(root))
                {
                    TickSingleImpact(other);
                }
            }
        }

        private void TickSingleImpact(Collider other)
        {
            if (definition == null || definition.PhysicsProfile == null || context.PhysicsService == null)
            {
                return;
            }

            Vector3 impactDir = currentState == AbilityState.Travel ? launchDirection : transform.forward;
            context.PhysicsService.ApplyDirectImpact(other, transform.position, impactDir, Radius, definition.PhysicsProfile, Charge01, currentState == AbilityState.Travel);
        }

        private void FollowCasterAim()
        {
            transform.SetPositionAndRotation(context.GetSphereCenterFromBackPoint(Radius), context.SpawnRotation);
        }

        private void ApplyScale()
        {
            if (sphereCollider != null) sphereCollider.radius = Radius;
            if (visual != null) visual.localScale = Vector3.one * Radius * 2f;

            if (abilityLight != null)
            {
                float chargeTarget = 0.5f;

                float lightProgress = Mathf.Clamp01(LinearCharge01 / chargeTarget);

                abilityLight.intensity = maxLightIntensity * lightProgress;
                abilityLight.range = maxLightRange * lightProgress;
            }
        }

        private void TickEffect(bool forceImmediate)
        {
            tickTimer -= Time.deltaTime;
            if (!forceImmediate && tickTimer > 0f) return;

            tickTimer = definition.SphereProfile.TickInterval;
            context.PhysicsService?.ApplyContinuousSphereForce(transform.position, Radius, definition.PhysicsProfile, Charge01, currentState == AbilityState.Travel);
        }

        private AbilityFeedbackEvent CreateFeedback(AbilityFeedbackPhase phase)
        {
            return new AbilityFeedbackEvent(definition, phase, transform.position, transform.forward, Charge01, Radius, LinearCharge01);
        }

        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;

            Gizmos.color = definition.DebugColor;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
