using UnityEngine;

namespace JJKDemo.Combat.Destruction
{
    /// <summary>
    /// Destroys the GameObject after a set lifetime. Used by destruction fragments to keep the scene clean.
    /// </summary>
    public sealed class DestroyAfterTime : MonoBehaviour
    {
        [SerializeField] private float lifetime = 4f;

        private void Start()
        {
            Destroy(gameObject, Mathf.Max(0f, lifetime));
        }

        /// <summary>Sets the lifetime at runtime (e.g. from fragment spawner).</summary>
        public void SetLifetime(float seconds)
        {
            lifetime = Mathf.Max(0f, seconds);
            Destroy(gameObject, lifetime);
        }
    }
}
