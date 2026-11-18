// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Shared "a spell touched an enemy" logic used by every attack:
// damage (with Weaken), the hit's status effects, elemental reactions,
// and the OnDamageDealt event that charges the player's Q.

using System;
using System.Collections.Generic;
using HollowTower.Core;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Spells
{
    public static class SpellHit
    {
        private static readonly Collider[] overlapBuffer = new Collider[64];

        /// <summary>(source, amount) — every bit of damage actually dealt. The Q charge listens to this.</summary>
        public static event Action<GameObject, float> OnDamageDealt;

        /// <summary>One spell hit on one target.</summary>
        public static void Hit(SpellPayload p, Health target)
        {
            ApplyHit(target, p.hit, p.element, p.caster, p.casterTeam);
        }

        /// <summary>Damage + status effects + elemental reaction on one target.</summary>
        public static void ApplyHit(Health target, HitEffect hit, ElementData element, GameObject caster, Team casterTeam)
        {
            if (!IsEnemy(target, casterTeam)) return;

            var status = StatusEffects.GetOrAdd(target);
            if (hit.damage > 0f) DealDamage(target, hit.damage, caster);
            if (target.IsDead) return;

            status.ApplyHitEffect(hit, caster);
            if (element != null) ElementalReactions.OnElementApplied(target, status, element.type, caster, casterTeam);
        }

        /// <summary>Raw damage (no effects). Applies Weaken and reports damage for the Q charge.</summary>
        public static void DealDamage(Health target, float amount, GameObject source)
        {
            if (target == null || target.IsDead || amount <= 0f) return;

            var status = target.GetComponent<StatusEffects>();
            if (status != null) amount *= status.DamageTakenMultiplier;

            float before = target.Current;
            target.TakeDamage(amount, source);
            float dealt = before - target.Current;
            if (dealt > 0f && source != null) OnDamageDealt?.Invoke(source, dealt);
        }

        /// <summary>Hits every enemy in a circle (Nova explosion).</summary>
        public static void Explode(SpellPayload p, Vector3 center, float radius)
        {
            foreach (var h in FindEnemiesInRadius(center, radius, p.casterTeam)) Hit(p, h);
            SpellVisuals.Ring(SpellVisuals.GroundBelow(center), p.Color, radius);
        }

        public static bool IsEnemy(Health h, Team casterTeam)
        {
            return h != null && !h.IsDead && h.Team != casterTeam;
        }

        public static Health FindNearestEnemy(Vector3 from, float range, Team casterTeam, ICollection<Health> exclude = null)
        {
            int count = Physics.OverlapSphereNonAlloc(from, range, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            Health best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = overlapBuffer[i].GetComponentInParent<Health>();
                if (!IsEnemy(h, casterTeam)) continue;
                if (exclude != null && exclude.Contains(h)) continue;
                float d = (h.transform.position - from).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = h; }
            }
            return best;
        }

        /// <summary>All living enemies within range, each listed once, nearest first.</summary>
        public static List<Health> FindEnemiesInRadius(Vector3 center, float radius, Team casterTeam)
        {
            var result = new List<Health>();
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var h = overlapBuffer[i].GetComponentInParent<Health>();
                if (IsEnemy(h, casterTeam) && !result.Contains(h)) result.Add(h);
            }
            result.Sort((a, b) =>
                (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude));
            return result;
        }
    }
}
