using System;
using System.Collections.Generic;
using JJKDemo.Combat.Abilities.Data;
using JJKDemo.Combat.Characters;
using JJKDemo.Combat.Feedback;
using JJKDemo.Combat.Input;
using JJKDemo.Combat.Physics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JJKDemo.Combat.Abilities.Runtime
{
    [RequireComponent(typeof(CharacterContext))]
    public sealed class AbilityController : MonoBehaviour
    {
        private const string RedAbilityPath = "Assets/_Project/ScriptableObjects/Abilities/Red_Ability.asset";
        private const string BlueAbilityPath = "Assets/_Project/ScriptableObjects/Abilities/Blue_Ability.asset";
        private const string PurpleAbilityPath = "Assets/_Project/ScriptableObjects/Abilities/Purple_Ability.asset";

        [SerializeField] private AbilityDefinition redAbility;
        [SerializeField] private AbilityDefinition blueAbility;
        [SerializeField] private AbilityDefinition purpleAbility;
        [SerializeField] private PhysicsInteractionService physicsService;
        [SerializeField] private Material redDebugMaterial;
        [SerializeField] private Material blueDebugMaterial;
        [SerializeField] private Material purpleDebugMaterial;
        [Tooltip("If the other button is pressed within this window of the first, the cast becomes Purple. " +
                 "Outside it (or once charging has settled), a second press is ignored — no late conversion.")]
        [SerializeField] private float comboWindow = 0.25f;

        // Only ever one ability is active at a time under the commit-on-press model.
        private readonly Dictionary<AbilityChannel, HeldSphereAbilityActor> activeActors = new();
        private readonly Dictionary<AbilityChannel, float> cooldownEndsAt = new();
        private readonly HashSet<AbilityChannel> controllingChannels = new();
        private CharacterContext character;

        private HeldSphereAbilityActor activeActor;
        private AbilityChannel activeChannel = AbilityChannel.None;
        private float firstPressTime;
        private int firstPressFrame = -1;
        private bool upgraded;
        private AbilityChannel lastPressChannel = AbilityChannel.None;

        public IReadOnlyDictionary<AbilityChannel, HeldSphereAbilityActor> ActiveActors => activeActors;
        public HeldSphereAbilityActor ActiveActor => activeActor;
        public AbilityChannel ActiveChannel => activeChannel;
        public event Action<bool> OnCastingStateChanged;

        /// <summary>
        /// Optional trial-layer gate. When assigned, a colour may only be committed (or upgraded to
        /// Purple) if this returns true for that channel. Null means "always allowed". It only blocks
        /// activation — it never alters ability physics, force, charge, or VFX behaviour.
        /// </summary>
        public Func<AbilityChannel, bool> CanActivate;

        /// <summary>Raised the instant a use is spent — i.e. when an ability is committed on the
        /// button press that spawns it (Red, Blue, or the Purple of a same-start combo).</summary>
        public event Action<AbilityChannel> AbilityCommitted;

        /// <summary>Raised when a just-committed single colour is refunded because the cast was
        /// immediately upgraded into Purple (or swapped on a same-frame tie).</summary>
        public event Action<AbilityChannel> AbilityRefunded;

        private void Awake()
        {
            character = GetComponent<CharacterContext>();
            physicsService ??= PhysicsInteractionService.Instance;
#if UNITY_EDITOR
            AssignDefaultAbilityReferences();
#endif
            ValidateAbilityReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AssignDefaultAbilityReferences();
        }
#endif

        public void HandleCommand(AbilityCommand command)
        {
            physicsService ??= PhysicsInteractionService.Instance;

            bool wasCasting = activeActor != null;

            switch (command.Phase)
            {
                case AbilityCommandPhase.BeginHold:
                    HandlePress(command.Channel);
                    break;
                case AbilityCommandPhase.Release:
                    HandleRelease(command.Channel, command.AimDirection);
                    break;
                case AbilityCommandPhase.Cancel:
                    CancelActive();
                    break;
            }

            bool isCasting = activeActor != null;
            if (wasCasting != isCasting)
            {
                OnCastingStateChanged?.Invoke(isCasting);
            }
        }

        // --- Press / release / auto-fire flow ----------------------------------------------------

        private void HandlePress(AbilityChannel channel)
        {
            if (channel != AbilityChannel.Red && channel != AbilityChannel.Blue)
            {
                return;
            }

            bool withinWindow = Time.time - firstPressTime <= comboWindow;
            bool isCombo = withinWindow && lastPressChannel != AbilityChannel.None && lastPressChannel != channel;

            bool purpleReady = purpleAbility != null
                && IsReady(AbilityChannel.Purple)
                && (CanActivate == null || CanActivate(AbilityChannel.Purple));

            if (isCombo && purpleReady && !upgraded)
            {
                SwapActive(AbilityChannel.Purple);
                lastPressChannel = channel;
                return;
            }

            // No Purple available: a genuine same-frame double press defaults to Red.
            if (Time.frameCount == firstPressFrame && isCombo)
            {
                if (activeActor != null && activeChannel == AbilityChannel.Blue && channel == AbilityChannel.Red)
                {
                    SwapActive(AbilityChannel.Red);
                    lastPressChannel = channel;
                    return;
                }
            }

            // Otherwise the cast is locked — ignore the second press (no dual cast, no late convert).
            if (activeActor != null)
            {
                lastPressChannel = channel;
                return;
            }

            if (!withinWindow)
            {
                firstPressTime = Time.time;
                firstPressFrame = Time.frameCount;
                lastPressChannel = AbilityChannel.None;
            }

            lastPressChannel = channel;
            Commit(channel);
        }

        private void HandleRelease(AbilityChannel channel, Vector3 aimDirection)
        {
            if (activeActor == null || !controllingChannels.Contains(channel))
            {
                return;
            }

            activeActor.Launch(aimDirection);
            StartCooldown(activeChannel);
            ClearActive();
        }

        private void HandleAutoFire(HeldSphereAbilityActor actor)
        {
            if (actor != activeActor)
            {
                return;
            }

            activeActor.Launch(character.AimDirection);
            StartCooldown(activeChannel);
            ClearActive();
            OnCastingStateChanged?.Invoke(false);
        }

        private void Commit(AbilityChannel channel)
        {
            var definition = GetDefinition(channel);
            if (definition == null || !IsReady(channel))
            {
                return;
            }

            if (CanActivate != null && !CanActivate(channel))
            {
                return;
            }

            var actor = CreateActor(definition);
            if (actor == null)
            {
                return;
            }

            activeActor = actor;
            activeChannel = channel;
            activeActors[channel] = actor;
            controllingChannels.Clear();
            controllingChannels.Add(channel);
            firstPressTime = Time.time;
            firstPressFrame = Time.frameCount;
            upgraded = false;
            actor.AutoFireRequested += HandleAutoFire;

            AbilityCommitted?.Invoke(channel);
        }

        // Tear down the current single-colour cast (refunding its use) and commit a new channel in
        // its place — used to upgrade Red/Blue into Purple, or to resolve a same-frame tie to Red.
        private void SwapActive(AbilityChannel newChannel)
        {
            var definition = newChannel == AbilityChannel.Purple ? purpleAbility : GetDefinition(newChannel);
            if (definition == null)
            {
                return;
            }

            var refundChannel = activeChannel;
            if (activeActor != null)
            {
                activeActor.AutoFireRequested -= HandleAutoFire;
                activeActor.Cancel();
            }
            
            if (refundChannel != AbilityChannel.None)
            {
                activeActors.Remove(refundChannel);
                AbilityRefunded?.Invoke(refundChannel);
            }

            var actor = CreateActor(definition);
            if (actor == null)
            {
                activeActor = null;
                activeChannel = AbilityChannel.None;
                controllingChannels.Clear();
                return;
            }

            activeActor = actor;
            activeChannel = newChannel;
            activeActors[newChannel] = actor;
            actor.AutoFireRequested += HandleAutoFire;
            controllingChannels.Clear();

            if (newChannel == AbilityChannel.Purple)
            {
                // Either button releases the Purple, since both formed it.
                upgraded = true;
                controllingChannels.Add(AbilityChannel.Red);
                controllingChannels.Add(AbilityChannel.Blue);
                AbilityFeedbackBus.Raise(new AbilityFeedbackEvent(definition, AbilityFeedbackPhase.Combined, character.AimPosition, character.AimDirection, 0f, 0f));
            }
            else
            {
                controllingChannels.Add(newChannel);
            }

            AbilityCommitted?.Invoke(newChannel);
        }

        private void CancelActive()
        {
            if (activeActor != null)
            {
                activeActor.AutoFireRequested -= HandleAutoFire;
                activeActor.Cancel();
            }
            ClearActive();
        }

        private void ClearActive()
        {
            if (activeActor != null)
            {
                activeActor.AutoFireRequested -= HandleAutoFire;
            }
            activeActor = null;
            activeChannel = AbilityChannel.None;
            activeActors.Clear();
            controllingChannels.Clear();
        }

        private void StartCooldown(AbilityChannel channel)
        {
            var definition = GetDefinition(channel);
            if (definition != null)
            {
                cooldownEndsAt[channel] = Time.time + definition.Cooldown;
            }
        }

        private bool IsReady(AbilityChannel channel)
        {
            return !cooldownEndsAt.TryGetValue(channel, out var endsAt) || Time.time >= endsAt;
        }

        private AbilityDefinition GetDefinition(AbilityChannel channel)
        {
            return channel switch
            {
                AbilityChannel.Red => redAbility,
                AbilityChannel.Blue => blueAbility,
                AbilityChannel.Purple => purpleAbility,
                _ => null
            };
        }

        private HeldSphereAbilityActor CreateActor(AbilityDefinition definition)
        {
            if (definition == null || definition.SphereProfile == null || definition.PhysicsProfile == null)
            {
                return null;
            }

            var root = new GameObject($"{definition.DisplayName}_Sphere");
            var context = new AbilityContext(character, physicsService);
            root.transform.SetPositionAndRotation(context.GetSphereCenterFromBackPoint(definition.SphereProfile.MinRadius), character.AbilitySpawnRotation);

            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;

            GameObject visual;
            if (definition.VisualPrefab != null)
            {
                visual = Instantiate(definition.VisualPrefab, root.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                if (visual.TryGetComponent<Collider>(out var visualCollider))
                {
                    Destroy(visualCollider);
                }

                if (visual.TryGetComponent<Renderer>(out var renderer))
                {
                    renderer.sharedMaterial = GetMaterial(definition.Channel);
                }
            }

            var actor = root.AddComponent<HeldSphereAbilityActor>();
            actor.Initialize(definition, context);
            return actor;
        }

        private Material GetMaterial(AbilityChannel channel)
        {
            var material = channel switch
            {
                AbilityChannel.Red => redDebugMaterial,
                AbilityChannel.Blue => blueDebugMaterial,
                AbilityChannel.Purple => purpleDebugMaterial,
                _ => null
            };

            if (material != null)
            {
                return material;
            }

            return CreateRuntimeMaterial(channel);
        }

        private void ValidateAbilityReferences()
        {
            if (redAbility == null || blueAbility == null || purpleAbility == null)
            {
                Debug.LogError("AbilityController requires Red, Blue, and Purple AbilityDefinition assets. Assign them from Assets/_Project/ScriptableObjects/Abilities.", this);
            }
        }

#if UNITY_EDITOR
        private void AssignDefaultAbilityReferences()
        {
            redAbility ??= AssetDatabase.LoadAssetAtPath<AbilityDefinition>(RedAbilityPath);
            blueAbility ??= AssetDatabase.LoadAssetAtPath<AbilityDefinition>(BlueAbilityPath);
            purpleAbility ??= AssetDatabase.LoadAssetAtPath<AbilityDefinition>(PurpleAbilityPath);
        }
#endif

        private static Material CreateRuntimeMaterial(AbilityChannel channel)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            var color = channel switch
            {
                AbilityChannel.Red => new Color(1f, 0.12f, 0.08f, 0.75f),
                AbilityChannel.Blue => new Color(0.05f, 0.35f, 1f, 0.75f),
                AbilityChannel.Purple => new Color(0.65f, 0.16f, 1f, 0.75f),
                _ => Color.white
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }
    }
}
