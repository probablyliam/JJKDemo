using UnityEngine;

namespace JJKDemo.Combat.Feedback
{
    /// <summary>
    /// Attach this to VFX quads (like the distortion vortex) so they always face the camera
    /// automatically, since you can't assign the Main Camera in a Prefab.
    /// </summary>
    public sealed class FaceCamera : MonoBehaviour
    {
        private Transform mainCameraTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                mainCameraTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (mainCameraTransform != null)
            {
                // Makes the quad perfectly face the camera
                transform.rotation = Quaternion.LookRotation(transform.position - mainCameraTransform.position);
            }
        }
    }
}
