// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Everything that can be "on" an enemy after a hit (GDD 5.2 / 5.4):
//   Burn (fire DoT), Slow, Freeze, Stun, Weaken (takes more damage),
//   Shock (lightning DoT), electric Charges (Charged Freeze),
//   the element Aura used for reactions, and forced movement (pull / knockback).
// Added automatically to anything a spell hits.
// Enemy AI reads CanAct and SpeedMultiplier.

using HollowTower.Core;
using HollowTower.Data;
using UnityEngine;
using UnityEngine.AI;

namespace HollowTower.Spells
{
    [RequireComponent(typeof(Health))]
    public class StatusEffects : MonoBehaviour
    {
        private const float TickInterval = 0.5f;

        private Health health;
        private NavMeshAgent agent;

        private float burnDps, burnLeft;
        private GameObject burnSource;
        private float shockDps, shockLeft;
        private GameObject shockSource;
        private float tickTimer = TickInterval;

        private float slowAmount, slowLeft;
        private float freezeLeft;
        private float stunLeft;
        private float weakenAmount, weakenLeft;

        private ElementType aura;
        private float auraLeft;

        private int chargesLeft;
        private float chargeDamage, chargeInterval, chargeTimer;
        private GameObject chargeSource;

        private Vector3 pushVelocity;
        private float pushLeft;

        public bool IsBurning => burnLeft > 0f;
        public bool IsShocked => shockLeft > 0f;
        public bool IsSlowed => slowLeft > 0f;
        public bool IsFrozen => freezeLeft > 0f;
        public bool IsStunned => stunLeft > 0f;
        public bool IsWeakened => weakenLeft > 0f;
        public bool HasAura => auraLeft > 0f;
        public ElementType Aura => aura;
        public int Charges => chargesLeft;

        /// <summary>False while frozen or stunned: no moving, no attacking.</summary>
        public bool CanAct => !IsFrozen && !IsStunned;
        /// <summary>1 = normal speed, 0 = can't move.</summary>
        public float SpeedMultiplier => !CanAct ? 0f : IsSlowed ? 1f - slowAmount : 1f;
        /// <summary>1.25 = takes 25% more damage.</summary>
        public float DamageTakenMultiplier => IsWeakened ? 1f + weakenAmount : 1f;

        public static StatusEffects GetOrAdd(Health h)
        {
            var s = h.GetComponent<StatusEffects>();
            return s != null ? s : h.gameObject.AddComponent<StatusEffects>();
        }

        private void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
        }

        // ---------- applying ----------

        public void ApplyHitEffect(HitEffect h, GameObject source)
        {
            ApplyBurn(h.burnDps, h.burnDuration, source);
            ApplySlow(h.slowAmount, h.slowDuration);
            Freeze(h.freezeDuration);
            Stun(h.stunDuration);
            ApplyWeaken(h.weakenAmount, h.weakenDuration);
            ApplyShock(h.shockDps, h.shockDuration, source);
        }

        public void ApplyBurn(float dps, float duration, GameObject source)
        {
            if (dps <= 0f || duration <= 0f) return;
            // Refreshes instead of stacking; keeps the stronger burn.
            burnDps = IsBurning ? Mathf.Max(burnDps, dps) : dps;
            burnLeft = Mathf.Max(burnLeft, duration);
            burnSource = source;
        }

        public void ApplyShock(float dps, float duration, GameObject source)
        {
            if (dps <= 0f || duration <= 0f) return;
            shockDps = IsShocked ? Mathf.Max(shockDps, dps) : dps;
            shockLeft = Mathf.Max(shockLeft, duration);
            shockSource = source;
        }

        public void ApplySlow(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f) return;
            slowAmount = IsSlowed ? Mathf.Max(slowAmount, amount) : amount;
            slowLeft = Mathf.Max(slowLeft, duration);
        }

        public void ApplyWeaken(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f) return;
            weakenAmount = IsWeakened ? Mathf.Max(weakenAmount, amount) : amount;
            weakenLeft = Mathf.Max(weakenLeft, duration);
        }

        public void Freeze(float duration) { if (duration > 0f) freezeLeft = Mathf.Max(freezeLeft, duration); }
        public void Stun(float duration) { if (duration > 0f) stunLeft = Mathf.Max(stunLeft, duration); }
        public void BreakFreeze() => freezeLeft = 0f;

        public void SetAura(ElementType element, float duration) { aura = element; auraLeft = duration; }
        public void ClearAura() => auraLeft = 0f;

        /// <summary>Charged Freeze: n electric charges that go off one by one.</summary>
        public void AddCharges(int count, float damageEach, float interval, GameObject source)
        {
            chargesLeft += count;
            chargeDamage = damageEach;
            chargeInterval = Mathf.Max(0.1f, interval);
            chargeTimer = chargeInterval;
            chargeSource = source;
        }

        /// <summary>Moves the target for a short time (Chain Reaction pull, Overload knockback).</summary>
        public void Push(Vector3 velocity, float duration)
        {
            velocity.y = 0f;
            pushVelocity = velocity;
            pushLeft = duration;
        }

        public void ClearAll()
        {
            burnLeft = shockLeft = slowLeft = freezeLeft = stunLeft = weakenLeft = auraLeft = pushLeft = 0f;
            chargesLeft = 0;
        }

        // ---------- update ----------

        private void Update()
        {
            if (health.IsDead) { ClearAll(); return; }
            float dt = Time.deltaTime;

            burnLeft -= dt; shockLeft -= dt; slowLeft -= dt; freezeLeft -= dt;
            stunLeft -= dt; weakenLeft -= dt; auraLeft -= dt;

            // Damage over time ticks twice per second.
            if (IsBurning || IsShocked)
            {
                tickTimer -= dt;
                if (tickTimer <= 0f)
                {
                    tickTimer += TickInterval;
                    if (IsBurning) SpellHit.DealDamage(health, burnDps * TickInterval, burnSource);
                    if (IsShocked && !health.IsDead) SpellHit.DealDamage(health, shockDps * TickInterval, shockSource);
                }
            }
            else tickTimer = TickInterval;

            if (chargesLeft > 0)
            {
                chargeTimer -= dt;
                if (chargeTimer <= 0f)
                {
                    chargeTimer = chargeInterval;
                    chargesLeft--;
                    Vector3 c = SpellVisuals.CenterOf(health);
                    SpellVisuals.ChainArc(c + Vector3.up * 1.2f, c, new Color(0.85f, 0.7f, 1f));
                    SpellHit.DealDamage(health, chargeDamage, chargeSource);
                }
            }

            if (pushLeft > 0f)
            {
                pushLeft -= dt;
                Vector3 step = pushVelocity * dt;
                if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Move(step);
                else transform.position += step;
            }
        }
    }
}
