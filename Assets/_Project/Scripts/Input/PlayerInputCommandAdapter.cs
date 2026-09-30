using System;
using JJKDemo.Combat.Characters;
using JJKDemo.Combat.Abilities.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JJKDemo.Combat.Input
{
    [RequireComponent(typeof(CharacterContext))]
    public sealed class PlayerInputCommandAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionReference redChannelAction;
        [SerializeField] private InputActionReference blueChannelAction;
        [SerializeField] private AbilityController abilityController;

        private CharacterContext character;

        private void Awake()
        {
            character = GetComponent<CharacterContext>();
            abilityController ??= GetComponent<AbilityController>();
        }

        private void OnEnable()
        {
            Subscribe(redChannelAction, OnRedStarted, OnRedCanceled);
            Subscribe(blueChannelAction, OnBlueStarted, OnBlueCanceled);
        }

        private void OnDisable()
        {
            Unsubscribe(redChannelAction, OnRedStarted, OnRedCanceled);
            Unsubscribe(blueChannelAction, OnBlueStarted, OnBlueCanceled);
        }

        private void OnRedStarted(InputAction.CallbackContext context)
        {
            Issue(AbilityChannel.Red, AbilityCommandPhase.BeginHold);
        }

        private void OnRedCanceled(InputAction.CallbackContext context)
        {
            Issue(AbilityChannel.Red, AbilityCommandPhase.Release);
        }

        private void OnBlueStarted(InputAction.CallbackContext context)
        {
            Issue(AbilityChannel.Blue, AbilityCommandPhase.BeginHold);
        }

        private void OnBlueCanceled(InputAction.CallbackContext context)
        {
            Issue(AbilityChannel.Blue, AbilityCommandPhase.Release);
        }

        private void Issue(AbilityChannel channel, AbilityCommandPhase phase)
        {
            var aimDirection = character != null ? character.AimDirection : transform.forward;
            abilityController?.HandleCommand(new AbilityCommand(channel, phase, aimDirection));
        }

        private static void Subscribe(InputActionReference actionReference, Action<InputAction.CallbackContext> started, Action<InputAction.CallbackContext> canceled)
        {
            if (actionReference == null)
            {
                return;
            }

            var action = actionReference.action;
            action.started += started;
            action.canceled += canceled;
            action.Enable();
        }

        private static void Unsubscribe(InputActionReference actionReference, Action<InputAction.CallbackContext> started, Action<InputAction.CallbackContext> canceled)
        {
            if (actionReference == null)
            {
                return;
            }

            var action = actionReference.action;
            action.started -= started;
            action.canceled -= canceled;
            action.Disable();
        }
    }
}
