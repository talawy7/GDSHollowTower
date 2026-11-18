// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 5.2: one element (Fire, Ice, Lightning) and everything it can do:
//   - Normal Attack   (left click, no cooldown)
//   - Projectile / Orb / Nova versions (E ability)
//   - Chain Reaction  (Q ability)
// Create via: Assets > Create > Hollow Tower > Element

using UnityEngine;

namespace HollowTower.Data
{
    public enum ElementType { Fire, Ice, Lightning }

    [CreateAssetMenu(fileName = "Element", menuName = "Hollow Tower/Element")]
    public class ElementData : ScriptableObject
    {
        public ElementType type;
        public string displayName = "Element";
        [Tooltip("Spell color. GDD: Fire orange, Ice light blue, Lightning purple-white.")]
        public Color color = Color.white;
        [TextArea] public string playstyle;

        [Header("Normal Attack (left click)")]
        public AttackData normalAttack = new AttackData { displayName = "Normal Attack" };
        [Tooltip("Seconds between normal shots while holding left click. GDD: no cooldown, so keep this small.")]
        public float normalFireInterval = 0.25f;

        [Header("Delivery styles (E)")]
        public AttackData projectile = new AttackData { displayName = "Projectile" };
        public AttackData orb = new AttackData { displayName = "Orb" };
        public AttackData nova = new AttackData { displayName = "Nova" };

        [Header("Chain Reaction (Q)")]
        public ChainReactionData chainReaction = new ChainReactionData();

        [Header("Unlocking")]
        public bool unlockedAtStart = true;
        public int soulInkCost;

        public AttackData GetDelivery(DeliveryType delivery)
        {
            switch (delivery)
            {
                case DeliveryType.Orb: return orb;
                case DeliveryType.Nova: return nova;
                default: return projectile;
            }
        }
    }
}
