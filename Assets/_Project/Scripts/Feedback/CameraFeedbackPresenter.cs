using UnityEngine;

namespace JJKDemo.Combat.Feedback
{
    public sealed class CameraFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;

        private float baseFov;
        private float fovOffset;
        private float targetFovOffset;
        private float fovVelocity;
        private float fovReturnDuration = 0.45f;

        private float currentShakeIntensity;
        private float targetShakeIntensity;
        private float shakeReturnDuration = 0.2f;
        private float shakeVelocity;

        private void Awake()
        {
            targetCamera ??= UnityEngine.Camera.main;
            if (targetCamera != null)
            {
                baseFov = targetCamera.fieldOfView;
            }
        }

        private void OnEnable()
        {
            AbilityFeedbackBus.FeedbackRaised += OnFeedbackRaised;
        }

        private void OnDisable()
        {
            AbilityFeedbackBus.FeedbackRaised -= OnFeedbackRaised;
        }

        private void LateUpdate()
        {
            fovOffset = Mathf.SmoothDamp(fovOffset, targetFovOffset, ref fovVelocity, fovReturnDuration);
            currentShakeIntensity = Mathf.SmoothDamp(currentShakeIntensity, targetShakeIntensity, ref shakeVelocity, shakeReturnDuration);

            if (targetCamera != null)
            {
                targetCamera.fieldOfView = baseFov + fovOffset;
                targetCamera.ResetProjectionMatrix();

                if (currentShakeIntensity > 0.001f)
                {
                    Matrix4x4 p = targetCamera.projectionMatrix;
                    // Skewing the projection matrix slightly creates a perfect 2D screen shake
                    // without ever moving the camera's physical Transform, making it immune to conflicts.
                    Vector2 shakeOffset = Random.insideUnitCircle * currentShakeIntensity * 0.2f;
                    p.m02 += shakeOffset.x;
                    p.m12 += shakeOffset.y;
                    targetCamera.projectionMatrix = p;
                }
            }
        }

        private void OnFeedbackRaised(AbilityFeedbackEvent feedbackEvent)
        {
            if (feedbackEvent.Ability == null || feedbackEvent.Ability.FeedbackProfile == null)
            {
                return;
            }

            var profile = feedbackEvent.Ability.FeedbackProfile;
            fovReturnDuration = profile.FovReturnDuration;
            shakeReturnDuration = profile.ShakeReturnDuration;

            if (feedbackEvent.Phase == AbilityFeedbackPhase.HoldTick)
            {
                // Charge up: slight zoom in (negative FOV) to focus energy
                targetFovOffset = -profile.FovKick * 0.2f * feedbackEvent.LinearCharge;
                targetShakeIntensity = profile.ChargeShakeIntensity * feedbackEvent.LinearCharge;
                return;
            }

            if (feedbackEvent.Phase == AbilityFeedbackPhase.Release)
            {
                // Powerful blast: Apply an outward velocity kick instead of a harsh position snap
                targetFovOffset = 0f;
                targetShakeIntensity = 0f;
                fovVelocity = profile.FovKick * 5f * feedbackEvent.LinearCharge;
            }
            else if (feedbackEvent.Phase == AbilityFeedbackPhase.Cancel || feedbackEvent.Phase == AbilityFeedbackPhase.Complete)
            {
                targetFovOffset = 0f;
                targetShakeIntensity = 0f;
            }
        }
    }
}
