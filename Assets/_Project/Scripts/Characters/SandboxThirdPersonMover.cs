using UnityEngine;
using UnityEngine.InputSystem;
using JJKDemo.Combat.Abilities.Runtime;

namespace JJKDemo.Combat.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SandboxThirdPersonMover : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private Transform cameraTransform;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [Tooltip("Damping time for VelocityX/Z smoothing (seconds). Lower = snappier.")]
        [SerializeField] private float velocityDampTime = 0.1f;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float rotationSharpness = 14f;
        [SerializeField] private float abilityRotationSharpness = 20f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float jumpHeight = 1.3f;

        [Header("Safety")]
        [Tooltip("Below this height the mover stops processing movement (the trial manager respawns the player first).")]
        [SerializeField] private float fallKillY = -25f;

        // Blend tree positions: idle=0, walk=1, sprint=2 on the Z axis.
        // Strafe clips sit at ±1 on X; back-walk at (0,-1).
        private const float BlendWalk   = 1f;
        private const float BlendSprint = 2f;

        private CharacterController controller;
        private AbilityController abilityController;
        private Vector3 verticalVelocity;
        private bool isJumping;

        // Smoothing fields
        private float currentSpeed;
        private float currentBlendMag;

        private void Awake()
        {
            controller        = GetComponent<CharacterController>();
            abilityController = GetComponent<AbilityController>();
        }

        // The trial manager catches falls and teleports the player back to spawn, so this is just a
        // belt-and-braces guard that suspends movement if we somehow drop below the kill height.
        private bool CheckFell()
        {
            return transform.position.y < fallKillY;
        }

        private void OnEnable()
        {
            moveAction?.action.Enable();
            sprintAction?.action.Enable();
            jumpAction?.action.Enable();
        }

        private void OnDisable()
        {
            moveAction?.action.Disable();
            sprintAction?.action.Disable();
            jumpAction?.action.Disable();
        }

        private void Update()
        {
            if (CheckFell()) return;

            // Suspend movement when a game mode locks input (e.g. the trial countdown / end screen).
            bool canMove = JJKDemo.Combat.GameplayInputGate.InputEnabled;

            var moveInput  = (canMove && moveAction != null) ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            bool usingAbility = abilityController != null && abilityController.ActiveActors.Count > 0;
            var wantSprint = (canMove && sprintAction != null) && sprintAction.action.IsPressed() && !usingAbility;
            var wantJump   = (canMove && jumpAction != null) && jumpAction.action.WasPressedThisFrame();

            // ── Grounding ───────────────────────────────────────────────────
            if (controller.isGrounded)
            {
                if (verticalVelocity.y < 0f) verticalVelocity.y = -2f;
                if (isJumping)
                {
                    isJumping = false;
                    animator?.ResetTrigger("Jump");
                }
            }

            // ── Jump ────────────────────────────────────────────────────────
            // Jump.fbx starts immediately and lasts 1 second — trigger and physics fire together.
            if (controller.isGrounded && !isJumping && wantJump)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                isJumping = true;
                animator?.SetTrigger("Jump");
            }

            // ── Camera-relative world axes ──────────────────────────────────
            var camFwd = new Vector3(
                cameraTransform != null ? cameraTransform.forward.x : transform.forward.x,
                0f,
                cameraTransform != null ? cameraTransform.forward.z : transform.forward.z
            ).normalized;
            var camRight = new Vector3(
                cameraTransform != null ? cameraTransform.right.x : transform.right.x,
                0f,
                cameraTransform != null ? cameraTransform.right.z : transform.right.z
            ).normalized;

            var worldMove = Vector3.ClampMagnitude(camFwd * moveInput.y + camRight * moveInput.x, 1f);
            var moving    = worldMove.sqrMagnitude > 0.001f;

            // ── Rotation ────────────────────────────────────────────────────

            if (usingAbility)
            {
                // Lock facing to crosshair so lateral/backward input correctly drives strafe clips.
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(camFwd),
                    abilityRotationSharpness * Time.deltaTime);
            }
            else if (moving)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(worldMove),
                    rotationSharpness * Time.deltaTime);
            }

            // ── Physics ─────────────────────────────────────────────────────
            float targetSpeed = wantSprint ? sprintSpeed : walkSpeed;
            if (currentSpeed == 0f) currentSpeed = targetSpeed;
            
            // Smoothly damp speed. 10f gives a fast but noticeable transition (~0.25s)
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 10f * Time.deltaTime);

            verticalVelocity.y += gravity * Time.deltaTime;
            controller.Move((worldMove * currentSpeed + verticalVelocity) * Time.deltaTime);

            // ── Animator ────────────────────────────────────────────────────
            if (animator == null) return;

            animator.SetBool("Grounded", controller.isGrounded && !isJumping);

            if (!moving)
            {
                animator.SetFloat("VelocityX", 0f, velocityDampTime, Time.deltaTime);
                animator.SetFloat("VelocityZ", 0f, velocityDampTime, Time.deltaTime);
                return;
            }

            // BlendWalk(1) and BlendSprint(2) map exactly onto the blend tree node positions,
            // so there is no ambiguity — sprint always selects the Running clip.
            float targetBlendMag = wantSprint ? BlendSprint : BlendWalk;
            if (currentBlendMag == 0f) currentBlendMag = targetBlendMag;
            
            // Smoothly damp blend magnitude for a better animation transition
            currentBlendMag = Mathf.Lerp(currentBlendMag, targetBlendMag, 10f * Time.deltaTime);

            if (usingAbility)
            {
                // Express movement in local space so the 2D blend tree drives the correct
                // directional clip (strafe left/right, backward) relative to the locked facing.
                Vector3 localDir = transform.InverseTransformDirection(worldMove);

                animator.SetFloat("VelocityX", localDir.x * currentBlendMag, velocityDampTime, Time.deltaTime);
                animator.SetFloat("VelocityZ", localDir.z * currentBlendMag, velocityDampTime, Time.deltaTime);
            }
            else
            {
                // Normal locomotion: character faces movement direction, so VelocityX is always 0.
                animator.SetFloat("VelocityX", 0f, velocityDampTime, Time.deltaTime);
                animator.SetFloat("VelocityZ", currentBlendMag, velocityDampTime, Time.deltaTime);
            }
        }
    }
}
