using JJKDemo.Combat.Abilities.Runtime;
using JJKDemo.Combat.Feedback;
using UnityEngine;

namespace JJKDemo.Combat.Cameras
{
    /// <summary>
    /// A completely separate follow camera for recording ability orbs. It does NOTHING except move its
    /// own transform to ride alongside an orb while it's in flight. It never enables/disables any
    /// camera, never overrides FOV, and never touches the Main Camera or any other game state.
    ///
    /// It follows Red, Blue, OR Purple. While it is not already following something, it latches onto the
    /// first orb that fires and stays with it until that orb expires/hits — then it's free to pick up the
    /// next one. Behind / Left / Right / Front framing is selectable in the inspector.
    /// </summary>
    public sealed class BlueOrbFollowCamera : MonoBehaviour
    {
        public enum FollowSide
        {
            Behind,
            Left,
            Right,
            Front
        }

        [Header("Framing")]
        [Tooltip("Where the camera sits relative to the orb's direction of travel.")]
        [SerializeField] private FollowSide side = FollowSide.Behind;
        [Tooltip("How far from the orb the camera sits (horizontal).")]
        [SerializeField] private float chaseDistance = 4.5f;
        [Tooltip("How high above the orb the camera sits.")]
        [SerializeField] private float chaseHeight = 1.6f;
        [Tooltip("How far ahead of the orb the camera looks, so it leads the shot.")]
        [SerializeField] private float lookAhead = 3f;

        [Header("Smoothing")]
        [Tooltip("Position smoothing time (0 = a hard locked move).")]
        [SerializeField] private float positionSmoothTime = 0.08f;
        [Tooltip("Rotation smoothing speed (higher = snappier).")]
        [SerializeField] private float rotationSharpness = 12f;

        private HeldSphereAbilityActor followTarget;
        private Vector3 travelDirection = Vector3.forward;
        private Vector3 lastTargetPosition;
        private Vector3 positionVelocity;

        private void OnEnable()
        {
            AbilityFeedbackBus.FeedbackRaised += OnFeedbackRaised;
        }

        private void OnDisable()
        {
            AbilityFeedbackBus.FeedbackRaised -= OnFeedbackRaised;
            followTarget = null;
        }

        private void OnFeedbackRaised(AbilityFeedbackEvent feedbackEvent)
        {
            // Only react to an orb being launched.
            if (feedbackEvent.Phase != AbilityFeedbackPhase.Release)
            {
                return;
            }

            // Already riding an orb? Stay with it — only latch onto a new one when idle.
            if (followTarget != null)
            {
                return;
            }

            AcquireOrb(feedbackEvent.Position, feedbackEvent.Direction);
        }

        private void AcquireOrb(Vector3 launchPosition, Vector3 launchDirection)
        {
            // The Release feedback fires while the launched actor still exists, so we can grab it here.
            // Only one ability is ever active at a time, so the launched orb is unambiguous; if several
            // somehow exist we take the one nearest the launch point. Channel doesn't matter — any of
            // Red / Blue / Purple is fair game.
            HeldSphereAbilityActor best = null;
            float bestSqr = float.MaxValue;

            var actors = FindObjectsByType<HeldSphereAbilityActor>(FindObjectsSortMode.None);
            foreach (var actor in actors)
            {
                if (!actor.IsLaunched)
                {
                    continue;
                }

                float sqr = (actor.transform.position - launchPosition).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = actor;
                }
            }

            if (best == null)
            {
                return;
            }

            followTarget = best;
            travelDirection = launchDirection.sqrMagnitude > 0.0001f ? launchDirection.normalized : transform.forward;
            lastTargetPosition = followTarget.transform.position;

            // Snap straight onto the shot so the first frame isn't a swoop from wherever we were parked.
            positionVelocity = Vector3.zero;
            transform.SetPositionAndRotation(DesiredPosition(lastTargetPosition), DesiredRotation(transform.position, lastTargetPosition));
        }

        private void LateUpdate()
        {
            // No orb in flight (none yet, or it expired/hit): just stay put. followTarget goes null when
            // the orb is destroyed, which frees us to latch onto the next one that fires.
            if (followTarget == null)
            {
                return;
            }

            Vector3 orbPosition = followTarget.transform.position;

            // Derive travel direction from actual movement so the framing tracks the orb's path.
            Vector3 delta = orbPosition - lastTargetPosition;
            if (delta.sqrMagnitude > 0.0001f)
            {
                travelDirection = delta.normalized;
            }
            lastTargetPosition = orbPosition;

            Vector3 desiredPosition = DesiredPosition(orbPosition);
            transform.position = positionSmoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime)
                : desiredPosition;

            Quaternion desiredRotation = DesiredRotation(transform.position, orbPosition);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
        }

        private Vector3 DesiredPosition(Vector3 orbPosition)
        {
            // Flatten the travel direction onto the ground plane so height is controlled purely by
            // chaseHeight, then rotate the "behind" offset around the orb to pick the side.
            Vector3 flatTravel = new Vector3(travelDirection.x, 0f, travelDirection.z);
            flatTravel = flatTravel.sqrMagnitude > 0.0001f ? flatTravel.normalized : Vector3.forward;

            float yaw = side switch
            {
                FollowSide.Behind => 0f,
                FollowSide.Right => 90f,
                FollowSide.Left => -90f,
                FollowSide.Front => 180f,
                _ => 0f
            };

            Vector3 behindDir = -flatTravel;
            Vector3 offsetDir = Quaternion.AngleAxis(yaw, Vector3.up) * behindDir;

            return orbPosition + offsetDir * chaseDistance + Vector3.up * chaseHeight;
        }

        private Quaternion DesiredRotation(Vector3 cameraPosition, Vector3 orbPosition)
        {
            Vector3 lookPoint = orbPosition + travelDirection * lookAhead;
            Vector3 toLook = lookPoint - cameraPosition;
            return toLook.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toLook, Vector3.up) : transform.rotation;
        }
    }
}
