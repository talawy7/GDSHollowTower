// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Burn (Fire: damage over time) and Slow (Ice: slower movement).
// Added automatically to anything a spell hits. Enemy AI (week 3) should
// multiply its NavMeshAgent speed by SpeedMultiplier.

using HollowTower.Core;
using UnityEngine;

namespace HollowTower.Spells
{
    [RequireComponent(typeof(Health))]
    public class StatusEffects : MonoBehaviour
    {
        private const float BurnTickInterval = 0.5f;

        private Health health;

        private float burnDps;
        private float burnLeft;
        private float burnTickTimer;
        private GameObject burnSource;

        private float slowAmount;
        private float slowLeft;

        public bool IsBurning => burnLeft > 0f;
        public bool IsSlowed => slowLeft > 0f;
        /// <summary>1 = normal speed, 0.5 = half speed.</summary>
        public float SpeedMultiplier => IsSlowed ? 1f - slowAmount : 1f;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        public void ApplyBurn(float damagePerSecond, float duration, GameObject source)
        {
            if (damagePerSecond <= 0f || duration <= 0f) return;
            // Re-burning refreshes the timer and keeps the stronger burn. It doesn't stack.
            burnDps = IsBurning ? Mathf.Max(burnDps, damagePerSecond) : damagePerSecond;
            if (!IsBurning) burnTickTimer = BurnTickInterval;
            burnLeft = Mathf.Max(burnLeft, duration);
            burnSource = source;
        }

        public void ApplySlow(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f) return;
            slowAmount = IsSlowed ? Mathf.Max(slowAmount, amount) : amount;
            slowLeft = Mathf.Max(slowLeft, duration);
        }

        public void ClearAll()
        {
            burnLeft = 0f; slowLeft = 0f;
        }

        private void Update()
        {
            if (health.IsDead) { ClearAll(); return; }

            if (IsBurning)
            {
                burnLeft -= Time.deltaTime;
                burnTickTimer -= Time.deltaTime;
                if (burnTickTimer <= 0f)
                {
                    burnTickTimer += BurnTickInterval;
                    health.TakeDamage(burnDps * BurnTickInterval, burnSource);
                }
            }

            if (IsSlowed) slowLeft -= Time.deltaTime;
        }
    }
}
