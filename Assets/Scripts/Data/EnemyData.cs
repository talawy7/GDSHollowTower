// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Tunable numbers for one enemy type (GDD 4.7). Used by GoblinEnemy,
// KnightEnemy and GhostEnemy. Create via: Assets > Create > Hollow Tower > Enemy

using UnityEngine;

namespace HollowTower.Data
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "Hollow Tower/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Tooltip("Short id saved in the Scribe's memory, e.g. \"Goblin\".")]
        public string id = "Enemy";
        public string displayName = "Enemy";

        [Header("Body")]
        public float maxHealth = 30f;
        public float moveSpeed = 3.5f;
        [Tooltip("Degrees per second.")]
        public float turnSpeed = 540f;
        [Tooltip("Notices the player within this distance.")]
        public float detectionRange = 14f;

        [Header("Attack")]
        public float attackDamage = 5f;
        [Tooltip("Starts an attack when the player is this close.")]
        public float attackRange = 1.6f;
        [Tooltip("Warning time before the hit lands. Long = easy to dodge.")]
        public float attackWindup = 0.4f;
        [Tooltip("Pause after attacking (a window for the player to hit back).")]
        public float attackRecovery = 0.4f;
        [Tooltip("Minimum time between attacks.")]
        public float attackCooldown = 1f;
        [Tooltip("Knight: size of the swing area.")]
        public float attackAreaRadius = 1.6f;

        [Header("Ranged (Ghost)")]
        [Tooltip("Tries to stay about this far from the player.")]
        public float preferredDistance = 8f;
        [Tooltip("Backs away if the player gets closer than this.")]
        public float retreatDistance = 4f;
        public float projectileSpeed = 7f;
        public float projectileRadius = 0.3f;
        public Color projectileColor = new Color(0.6f, 1f, 0.8f);

        [Header("Look")]
        [Tooltip("Ghost floats above the ground.")]
        public float hoverHeight = 0f;

        [Header("Rewards")]
        public int soulInk = 1;

        [Header("Elite version (GDD 4.7)")]
        public float eliteHealthMultiplier = 2.5f;
        public float eliteDamageMultiplier = 1.5f;
        public float eliteScale = 1.25f;
    }
}
