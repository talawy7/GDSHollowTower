// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Generic health component. Used by the witch now, and by enemies later.
// GDD 4.2: health does not regenerate; only rewards heal. At 0 the owner dies.

using System;
using UnityEngine;

namespace HollowTower.Core
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>When true, TakeDamage is ignored (e.g. during a dodge).</summary>
        public bool Invulnerable { get; set; }

        /// <summary>(current, max) — hook the HUD health bar to this.</summary>
        public event Action<float, float> OnHealthChanged;
        /// <summary>(amount, source) — source can be null. The Scribe will use this to learn what killed you.</summary>
        public event Action<float, GameObject> OnDamaged;
        /// <summary>(killer) — killer can be null.</summary>
        public event Action<GameObject> OnDied;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(Current, maxHealth);
        }

        /// <summary>Called by PlayerController so the value comes from PlayerStats.</summary>
        public void SetMax(float newMax, bool refill)
        {
            maxHealth = Mathf.Max(1f, newMax);
            if (refill) Current = maxHealth;
            Current = Mathf.Min(Current, maxHealth);
            OnHealthChanged?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(float amount, GameObject source = null)
        {
            if (IsDead || Invulnerable || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            OnDamaged?.Invoke(amount, source);
            OnHealthChanged?.Invoke(Current, maxHealth);

            if (Current <= 0f)
            {
                IsDead = true;
                OnDied?.Invoke(source);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            OnHealthChanged?.Invoke(Current, maxHealth);
        }

        /// <summary>Full reset, for starting a new run.</summary>
        public void ResetHealth()
        {
            IsDead = false;
            Current = maxHealth;
            OnHealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
