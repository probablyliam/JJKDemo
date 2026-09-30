using JJKDemo.Combat.Abilities.Runtime;
using UnityEngine;

namespace JJKDemo.Combat.Characters
{
    [RequireComponent(typeof(AbilitySpawnPoint))]
    public sealed class CharacterContext : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera viewCamera;
        [SerializeField] private AbilitySpawnPoint abilitySpawnPoint;

        public Transform Transform => transform;
        public UnityEngine.Camera ViewCamera => viewCamera != null ? viewCamera : UnityEngine.Camera.main;
        public AbilitySpawnPoint AbilitySpawnPoint
        {
            get
            {
                abilitySpawnPoint ??= GetComponent<AbilitySpawnPoint>();
                return abilitySpawnPoint;
            }
        }

        public Vector3 AimDirection
        {
            get
            {
                var camera = ViewCamera;
                if (camera != null)
                {
                    Vector3 cameraPos = camera.transform.position;
                    Vector3 cameraFwd = camera.transform.forward;

                    // Raycast from camera to find what the crosshair is looking at
                    if (UnityEngine.Physics.Raycast(cameraPos, cameraFwd, out RaycastHit hit, 500f, UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.distance > 50f) 
                        {
                            return (hit.point - AbilitySpawnPosition).normalized;
                        }
                    }

                    // If hitting the sky/nothing or something too close, pick a point far away
                    Vector3 distantPoint = cameraPos + cameraFwd * 500f;
                    return (distantPoint - AbilitySpawnPosition).normalized;
                }

                return transform.forward;
            }
        }

        public Vector3 AimPosition => transform.position;
        public Vector3 AbilitySpawnPosition => AbilitySpawnPoint != null ? AbilitySpawnPoint.SpawnPosition : GetFallbackSpawnPosition();
        public Quaternion AbilitySpawnRotation => AbilitySpawnPoint != null ? AbilitySpawnPoint.SpawnRotation : Quaternion.LookRotation(AimDirection, Vector3.up);

        private void Awake()
        {
            abilitySpawnPoint ??= GetComponent<AbilitySpawnPoint>();
        }

        private void Reset()
        {
            viewCamera = UnityEngine.Camera.main;
            abilitySpawnPoint = GetComponent<AbilitySpawnPoint>();
        }

        private Vector3 GetFallbackSpawnPosition()
        {
            var camera = ViewCamera;
            var forward = camera != null ? camera.transform.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
            }

            return transform.position + Vector3.up * 1.25f + forward.normalized * 2.8f;
        }
    }
}
