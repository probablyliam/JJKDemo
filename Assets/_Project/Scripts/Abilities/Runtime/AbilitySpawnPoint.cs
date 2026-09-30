using UnityEngine;

namespace JJKDemo.Combat.Abilities.Runtime
{
    public sealed class AbilitySpawnPoint : MonoBehaviour
    {
        [SerializeField] private Transform aimSource;
        [SerializeField] private float forwardOffset = 3.2f;
        [SerializeField] private float upwardOffset = 1.25f;

        public Vector3 AimDirection
        {
            get
            {
                var direction = AimSource.forward;
                return direction.normalized;
            }
        }

        private Transform AimSource
        {
            get
            {
                if (aimSource == null)
                {
                    aimSource = Camera.main != null ? Camera.main.transform : null;
                }

                if (aimSource == null)
                {
                    Debug.LogError("AbilitySpawnPoint requires a camera aim source, but no aim source or MainCamera was found.", this);
                }

                return aimSource;
            }
        }

        public Vector3 SpawnPosition
        {
            get
            {
                var forward = AimDirection;
                forward.y = 0f;

                return transform.position + Vector3.up * upwardOffset + forward.normalized * forwardOffset;
            }
        }

        public Quaternion SpawnRotation => Quaternion.LookRotation(AimDirection, Vector3.up);

        public void SetAimSource(Transform source)
        {
            aimSource = source;
        }

        private void OnDrawGizmosSelected()
        {
            if (AimSource == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(SpawnPosition, 0.25f);
            Gizmos.DrawLine(transform.position + Vector3.up * upwardOffset, SpawnPosition);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(SpawnPosition, SpawnPosition + SpawnRotation * Vector3.forward * 0.75f);
        }
    }
}
