using JJKDemo.Combat.Input;
using JJKDemo.Combat.Physics;
using UnityEngine;

namespace JJKDemo.Combat.Abilities.Data
{
    [CreateAssetMenu(menuName = "JJKDemo/Abilities/Ability Definition")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string abilityId = "ability";
        [SerializeField] private string displayName = "Ability";
        [SerializeField] private AbilityChannel channel = AbilityChannel.Red;
        [SerializeField] private AbilityEffectMode effectMode = AbilityEffectMode.Push;
        [SerializeField] private Color debugColor = Color.red;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float projectileLifetime = 2.5f;
        [SerializeField] private HeldSphereProfile sphereProfile;
        [SerializeField] private AbilityPhysicsProfile physicsProfile;
        [SerializeField] private AbilityFeedbackProfile feedbackProfile;
        [SerializeField] private GameObject visualPrefab;
        


        public string AbilityId => abilityId;
        public string DisplayName => displayName;
        public AbilityChannel Channel => channel;
        public AbilityEffectMode EffectMode => effectMode;
        public Color DebugColor => debugColor;
        public float Cooldown => Mathf.Max(0f, cooldown);
        public float ProjectileSpeed => Mathf.Max(0f, projectileSpeed);
        public float ProjectileLifetime => Mathf.Max(0.1f, projectileLifetime);
        public HeldSphereProfile SphereProfile => sphereProfile;
        public AbilityPhysicsProfile PhysicsProfile => physicsProfile;
        public AbilityFeedbackProfile FeedbackProfile => feedbackProfile;
        public GameObject VisualPrefab => visualPrefab;
    }
}
