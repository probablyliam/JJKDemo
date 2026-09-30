using UnityEngine;
using UnityEngine.InputSystem;

namespace JJKDemo.Combat.Cameras
{
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private Vector3 framingOffset = new(0.65f, 0.2f, 0f);
        [SerializeField] private float bodyRootTargetHeight = 1.45f;
        [SerializeField] private float distance = 6f;
        [SerializeField] private float sensitivity = 0.11f;
        [SerializeField] private float positionSmoothTime = 0.055f;
        [SerializeField] private float lookAtSmoothTime = 0.035f;
        [SerializeField] private float minimumCameraHeight = 0.35f;
        [SerializeField] private bool lockCursorOnPlay = true;

        private const float MinPitch = -65f;
        private const float MaxPitch = 78f;

        private float yaw;
        private float pitch = 18f;
        private Vector3 positionVelocity;
        private Vector3 lookAtVelocity;
        private Vector3 smoothedLookAt;

        private void Awake()
        {
            if (target != null)
            {
                var flatForward = target.forward;
                flatForward.y = 0f;
                if (flatForward.sqrMagnitude > 0.0001f)
                {
                    yaw = Quaternion.LookRotation(flatForward).eulerAngles.y;
                }

                smoothedLookAt = GetRawLookAtPoint();
            }
        }

        private void OnEnable()
        {
            lookAction?.action.Enable();

            if (lockCursorOnPlay && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            lookAction?.action.Disable();

            if (lockCursorOnPlay && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            // Suspend look when a game mode locks input (e.g. the trial countdown / end screen).
            bool canMove = JJKDemo.Combat.GameplayInputGate.InputEnabled;

            var look = (canMove && lookAction != null) ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, MinPitch, MaxPitch);

            var rawLookAt = GetRawLookAtPoint();
            if (smoothedLookAt == default)
            {
                smoothedLookAt = rawLookAt;
            }

            smoothedLookAt = Vector3.SmoothDamp(smoothedLookAt, rawLookAt, ref lookAtVelocity, lookAtSmoothTime);

            var orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            var desiredPosition = smoothedLookAt - orbitRotation * Vector3.forward * distance;
            if (desiredPosition.y < minimumCameraHeight)
            {
                desiredPosition.y = minimumCameraHeight;
            }

            var smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
            if (smoothedPosition.y < minimumCameraHeight)
            {
                smoothedPosition.y = minimumCameraHeight;
            }

            var lookRotation = Quaternion.LookRotation(smoothedLookAt - smoothedPosition, Vector3.up);

            transform.SetPositionAndRotation(smoothedPosition, lookRotation);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                smoothedLookAt = GetRawLookAtPoint();
                positionVelocity = Vector3.zero;
                lookAtVelocity = Vector3.zero;
            }
        }

        private Vector3 GetRawLookAtPoint()
        {
            var yawRotation = Quaternion.Euler(0f, yaw, 0f);
            if (target.TryGetComponent<CharacterController>(out _))
            {
                return target.position
                    + Vector3.up * bodyRootTargetHeight
                    + yawRotation * new Vector3(framingOffset.x, framingOffset.y, framingOffset.z);
            }

            return target.position + yawRotation * framingOffset;
        }
    }
}
