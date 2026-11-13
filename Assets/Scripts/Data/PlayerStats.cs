// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// All tunable player numbers live here so we can balance in weeks 12-13
// without touching code (GDD 4: "values in ScriptableObjects").
// Create one via: Assets > Create > Hollow Tower > Player Stats

using UnityEngine;

namespace HollowTower.Data
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Hollow Tower/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Units per second while walking.")]
        public float moveSpeed = 6f;

        [Tooltip("How fast she reaches full speed / stops. Higher = snappier.")]
        public float acceleration = 40f;

        [Header("Dodge (Space)")]
        [Tooltip("How far one dodge travels, in units.")]
        public float dodgeDistance = 4f;

        [Tooltip("How long the dash lasts, in seconds. She can't be hit during this.")]
        public float dodgeDuration = 0.18f;

        [Tooltip("Seconds after a dodge before she can dodge again.")]
        public float dodgeCooldown = 0.6f;

        [Header("Health")]
        public float maxHealth = 100f;
    }
}
