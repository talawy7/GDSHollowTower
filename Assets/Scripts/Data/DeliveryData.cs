// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.3: the "delivery" half of a spell (how it is shot).
//   Projectile: fast shot that hits the first enemy
//   Orb:        slow ball that goes through enemies
//   Nova:       blast in a circle around the witch
// Create via: Assets > Create > Hollow Tower > Spells > Delivery

using UnityEngine;

namespace HollowTower.Data
{
    public enum DeliveryType { Projectile, Orb, Nova }

    [CreateAssetMenu(fileName = "Delivery", menuName = "Hollow Tower/Spells/Delivery")]
    public class DeliveryData : ScriptableObject
    {
        public DeliveryType type;
        public string displayName = "Delivery";

        [Header("Damage and timing")]
        public float baseDamage = 10f;
        [Tooltip("Seconds between casts (before the element's multiplier).")]
        public float castCooldown = 0.35f;

        [Header("Projectile / Orb")]
        public float speed = 18f;
        public float maxDistance = 20f;
        [Tooltip("Size of the ball. Also its hit radius.")]
        public float radius = 0.25f;
        [Tooltip("True = goes through enemies (Orb). False = stops at the first one (Projectile).")]
        public bool piercesEnemies = false;

        [Header("Nova")]
        public float novaRadius = 4f;

        [Header("Unlocking (GDD 4.5)")]
        public bool unlockedAtStart = true;
        public int soulInkCost = 0;
    }
}
