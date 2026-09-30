using System;

namespace JJKDemo.Combat.Feedback
{
    public static class AbilityFeedbackBus
    {
        public static event Action<AbilityFeedbackEvent> FeedbackRaised;

        public static void Raise(AbilityFeedbackEvent feedbackEvent)
        {
            FeedbackRaised?.Invoke(feedbackEvent);
        }
    }
}
