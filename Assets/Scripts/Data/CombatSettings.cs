// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Global combat numbers: E cooldown, Q charge, and elemental reactions (GDD 5.4).
// Lives at Assets/Resources/CombatSettings.asset so any script can read it
// through CombatSettings.Current. If the asset is missing, defaults are used.

using UnityEngine;

namespace HollowTower.Data
{
    [CreateAssetMenu(fileName = "CombatSettings", menuName = "Hollow Tower/Combat Settings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("Delivery style ability (E)")]
        [Tooltip("GDD 5.1: 10 seconds.")]
        public float deliveryCooldown = 10f;

        [Header("Chain Reaction (Q)")]
        [Tooltip("Damage the player must deal to fully charge Q (GDD 5.1).")]
        public float chainChargeRequired = 60f;

        [Header("Elemental reactions (GDD 5.4)")]
        [Tooltip("How long an element stays 'on' an enemy, waiting for a second element.")]
        public float auraDuration = 4f;

        [Header("Fire + Ice: Shatter")]
        public float shatterDamage = 20f;

        [Header("Fire + Lightning: Overload")]
        public float overloadDamage = 8f;
        public float overloadRadius = 3f;
        [Tooltip("Knockback speed (units per second) for a short push.")]
        public float overloadKnockback = 12f;
        public float overloadStun = 2f;

        [Header("Ice + Lightning: Charged Freeze")]
        public int chargedFreezeCharges = 3;
        public float chargedFreezeDamagePerCharge = 4f;
        [Tooltip("Seconds between each charge going off.")]
        public float chargedFreezeInterval = 0.6f;
        public float chargedFreezeFreeze = 1.5f;

        private static CombatSettings cached;

        public static CombatSettings Current
        {
            get
            {
                if (cached == null) cached = Resources.Load<CombatSettings>("CombatSettings");
                if (cached == null)
                {
                    Debug.LogWarning("CombatSettings asset not found in a Resources folder, using defaults.");
                    cached = CreateInstance<CombatSettings>();
                }
                return cached;
            }
        }
    }
}
