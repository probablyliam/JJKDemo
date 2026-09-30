using UnityEngine;

namespace JJKDemo.Combat.Physics
{
    public sealed class PhysicsInteractionService : MonoBehaviour
    {
        private readonly Collider[] results = new Collider[4096];
        private readonly System.Collections.Generic.HashSet<Transform> tempRoots = new System.Collections.Generic.HashSet<Transform>();

        private static PhysicsInteractionService instance;
        public static PhysicsInteractionService Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<PhysicsInteractionService>();
                    if (instance == null)
                    {
                        var go = new GameObject("PhysicsInteractionService");
                        instance = go.AddComponent<PhysicsInteractionService>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        public void ApplyContinuousSphereForce(Vector3 center, float radius, AbilityPhysicsProfile profile, float charge, bool isRelease)
        {
            if (profile == null)
            {
                return;
            }

            var queryRadius = profile.GetQueryRadius(radius);
            LayerMask mask = profile.AffectedLayers;
            var count = UnityEngine.Physics.OverlapSphereNonAlloc(center, queryRadius, results, mask, QueryTriggerInteraction.Ignore);
            var targetCount = Mathf.Min(count, profile.MaxTargets);

            tempRoots.Clear();
            for (var i = 0; i < targetCount; i++)
            {
                var target = results[i];
                if (target == null)
                {
                    continue;
                }

                // Multi-collider bodies: only push each root object once per tick.
                var root = target.attachedRigidbody != null ? target.attachedRigidbody.transform : target.transform.root;
                if (!tempRoots.Add(root))
                {
                    continue;
                }

                profile.ApplyContinuous(target, center, radius, charge, isRelease);
            }
        }

        public void ApplyDirectImpact(Collider target, Vector3 position, Vector3 direction, float radius, AbilityPhysicsProfile profile, float charge, bool isRelease)
        {
            if (profile == null)
            {
                return;
            }

            if ((profile.AffectedLayers.value & (1 << target.gameObject.layer)) == 0)
            {
                return;
            }

            profile.ApplyDirectImpact(target, position, direction, radius, charge, isRelease);
        }
    }
}
