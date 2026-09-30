using UnityEngine;

namespace JJKDemo.Combat.Trial
{
    /// <summary>
    /// Puts a destructible's Rigidbody to sleep on startup so a perfectly-placed structure never
    /// drifts, settles, or topples on its own at the start of a run. Any ability force or a collision
    /// from a neighbouring piece wakes it normally, so destruction and chain reactions are unaffected.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class StartAsleep : MonoBehaviour
    {
        private void Start()
        {
            var body = GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
        }
    }
}
