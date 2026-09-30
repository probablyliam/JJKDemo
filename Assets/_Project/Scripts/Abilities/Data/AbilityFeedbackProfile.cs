using UnityEngine;

namespace JJKDemo.Combat.Abilities.Data
{
    [CreateAssetMenu(menuName = "JJKDemo/Abilities/Ability Feedback Profile")]
    public sealed class AbilityFeedbackProfile : ScriptableObject
    {
        [Header("Audio")]
        [SerializeField] private AudioClip beginClip;
        [SerializeField] private AudioClip releaseClip;
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private float fovKick = 4f;
        [SerializeField] private float fovReturnDuration = 0.45f;
        [SerializeField] private float chargeShakeIntensity = 0.1f;
        [SerializeField] private float shakeReturnDuration = 0.2f;

        public AudioClip BeginClip => beginClip;
        public AudioClip ReleaseClip => releaseClip;
        public AudioClip ImpactClip => impactClip;
        public float FovKick => fovKick;
        public float FovReturnDuration => Mathf.Max(0.01f, fovReturnDuration);
        public float ChargeShakeIntensity => Mathf.Max(0f, chargeShakeIntensity);
        public float ShakeReturnDuration => Mathf.Max(0.01f, shakeReturnDuration);
    }
}
