// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.4: modifiers. Press Q (Chain) or E (Bounce) to prepare one;
// the next spell uses it, then it goes on cooldown.
//   Chain:  the spell jumps to nearby enemies
//   Bounce: projectiles/orbs bounce off walls and toward the next enemy;
//           Nova pulses again.
// Create via: Assets > Create > Hollow Tower > Spells > Modifier

using UnityEngine;

namespace HollowTower.Data
{
    public enum ModifierType { Chain, Bounce }

    [CreateAssetMenu(fileName = "Modifier", menuName = "Hollow Tower/Spells/Modifier")]
    public class ModifierData : ScriptableObject
    {
        public ModifierType type;
        public string displayName = "Modifier";
        [Tooltip("Seconds before it can be prepared again after being used.")]
        public float cooldown = 5f;

        [Header("Chain")]
        public int chainJumps = 3;
        public float chainRange = 6f;
        [Tooltip("Each jump does this fraction of the previous hit's damage.")]
        [Range(0.1f, 1f)] public float chainDamageFalloff = 0.75f;

        [Header("Bounce")]
        public int bounces = 2;

        [Header("Unlocking (GDD 4.5)")]
        public bool unlockedAtStart = true;
        public int soulInkCost = 0;
    }
}
