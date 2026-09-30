using JJKDemo.Combat.Physics;

namespace JJKDemo.Combat.Destruction
{
    public interface IDamageable
    {
        void ApplyDamage(float amount, ImpactPayload payload);
    }
}
