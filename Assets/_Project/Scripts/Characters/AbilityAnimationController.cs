using UnityEngine;
using JJKDemo.Combat.Abilities.Runtime;
using JJKDemo.Combat.Input;

namespace JJKDemo.Combat.Characters
{
    [RequireComponent(typeof(Animator))]
    public class AbilityAnimationController : MonoBehaviour
    {
        private Animator animator;
        private AbilityController abilityController;
        private static readonly int IsCastingHash = Animator.StringToHash("IsCasting");
        private static readonly int AbilityChannelHash = Animator.StringToHash("AbilityChannel");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            abilityController = GetComponentInParent<AbilityController>();
        }

        private void OnEnable()
        {
            if (abilityController != null)
            {
                abilityController.OnCastingStateChanged += HandleCastingStateChanged;
                abilityController.AbilityCommitted += HandleAbilityCommitted;
            }
        }

        private void OnDisable()
        {
            if (abilityController != null)
            {
                abilityController.OnCastingStateChanged -= HandleCastingStateChanged;
                abilityController.AbilityCommitted -= HandleAbilityCommitted;
            }
        }

        private void HandleCastingStateChanged(bool isCasting)
        {
            animator.SetBool(IsCastingHash, isCasting);
            if (isCasting)
            {
                animator.SetInteger(AbilityChannelHash, (int)abilityController.ActiveChannel);
            }
        }

        private void HandleAbilityCommitted(AbilityChannel channel)
        {
            animator.SetInteger(AbilityChannelHash, (int)channel);
        }
    }
}
