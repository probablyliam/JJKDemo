using JJKDemo.Combat.Physics;
using UnityEngine;

namespace JJKDemo.Combat.Destruction
{
    [RequireComponent(typeof(Collider))]
    public sealed class DestructibleObject : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 1000f;

        private float health;
        private bool hasBroken;

        private void Awake()
        {
            health = maxHealth;
        }

        public void ApplyDamage(float amount, ImpactPayload payload)
        {
            health -= Mathf.Max(0f, amount);
            if (health > 0f)
            {
                return;
            }

            Break();
        }

        private void Break()
        {
            if (hasBroken)
            {
                return;
            }

            hasBroken = true;
            Destroy(gameObject);
        }
    }
}
