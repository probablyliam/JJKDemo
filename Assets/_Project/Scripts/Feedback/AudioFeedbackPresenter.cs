using UnityEngine;

namespace JJKDemo.Combat.Feedback
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioFeedbackPresenter : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        private void Awake()
        {
            audioSource ??= GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            AbilityFeedbackBus.FeedbackRaised += OnFeedbackRaised;
        }

        private void OnDisable()
        {
            AbilityFeedbackBus.FeedbackRaised -= OnFeedbackRaised;
        }

        private void OnFeedbackRaised(AbilityFeedbackEvent feedbackEvent)
        {
            if (audioSource == null || feedbackEvent.Ability == null || feedbackEvent.Ability.FeedbackProfile == null)
            {
                return;
            }

            var profile = feedbackEvent.Ability.FeedbackProfile;
            var clip = feedbackEvent.Phase switch
            {
                AbilityFeedbackPhase.BeginHold => profile.BeginClip,
                AbilityFeedbackPhase.Release => profile.ReleaseClip,
                AbilityFeedbackPhase.Impact => profile.ImpactClip,
                _ => null
            };

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
