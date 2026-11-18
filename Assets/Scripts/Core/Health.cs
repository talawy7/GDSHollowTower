// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
// Week 2: Claude added the Team field so spells know who to hit.
//
// Generic health component. Used by the witch now, and by enemies later.
// GDD 4.2: health does not regenerate; only rewards heal. At 0 the owner dies.

using System;
using UnityEngine;

namespace HollowTower.Core
{
    /// <summary>Who is on which side. Spells only hurt the other team.</summary>
    public enum Team { Player, Enemy }

    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private Team team = Team.Player;

        public Team Team => team;
        public void SetTeam(Team newTeam) => team = newTeam;

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
        /// <summary>(who, amount) — any Health anywhere took damage. Handy for damage numbers and stats.</summary>
        public static event Action<Health, float> OnAnyDamaged;

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
            OnAnyDamaged?.Invoke(this, amount);
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

        /// <summary>Used by the RunManager to carry health from one floor to the next.</summary>
        public void SetCurrent(float value)
        {
            if (IsDead) return;
            Current = Mathf.Clamp(value, 1f, maxHealth);
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
