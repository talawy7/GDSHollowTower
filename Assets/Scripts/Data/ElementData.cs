// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.3: the "element" half of a spell (Fire, Ice, Lightning).
// The element decides damage, color and the extra effect (burn, slow, arcing).
// Create via: Assets > Create > Hollow Tower > Spells > Element

using UnityEngine;

namespace HollowTower.Data
{
    public enum ElementType { Fire, Ice, Lightning }

    [CreateAssetMenu(fileName = "Element", menuName = "Hollow Tower/Spells/Element")]
    public class ElementData : ScriptableObject
    {
        public ElementType type;
        public string displayName = "Element";
        [Tooltip("Spell color. GDD 8: Fire orange, Ice light blue, Lightning purple-white.")]
        public Color color = Color.white;

        [Header("Damage and feel")]
        [Tooltip("Multiplies the delivery's base damage. Fire = big damage.")]
        public float damageMultiplier = 1f;
        [Tooltip("Multiplies projectile/orb speed. Lightning = fast.")]
        public float speedMultiplier = 1f;
        [Tooltip("Multiplies the time between casts. Lower = cast faster.")]
        public float cooldownMultiplier = 1f;

        [Header("Burn (Fire)")]
        public float burnDamagePerSecond = 0f;
        public float burnDuration = 0f;

        [Header("Slow (Ice)")]
        [Tooltip("0.5 = enemy moves at half speed.")]
        [Range(0f, 0.9f)] public float slowAmount = 0f;
        public float slowDuration = 0f;

        [Header("Arcing (Lightning)")]
        [Tooltip("Free chain jumps on every hit, even without the Chain modifier.")]
        public int innateChainJumps = 0;

        [Header("Unlocking (GDD 4.5)")]
        public bool unlockedAtStart = true;
        [Tooltip("Soul Ink price at the Scribe if not unlocked at start.")]
        public int soulInkCost = 0;
    }
}
