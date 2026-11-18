// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Shared "what happens when a spell touches an enemy" logic, used by every
// delivery: damage, the element's effect (burn / slow), and chain jumps.

using System.Collections.Generic;
using HollowTower.Core;
using UnityEngine;

namespace HollowTower.Spells
{
    public static class SpellHit
    {
        private static readonly Collider[] overlapBuffer = new Collider[64];

        /// <summary>Hit one target, then chain from it if the spell can chain.</summary>
        /// <param name="exclude">Targets chains must not jump to (e.g. everything a Nova already hit).</param>
        public static void Hit(SpellPayload p, Health target, ICollection<Health> exclude = null)
        {
            ApplyTo(p, target, p.damage);
            ChainFrom(p, target, p.damage, exclude);
        }

        /// <summary>Damage + element effect on one target. No chaining.</summary>
        public static void ApplyTo(SpellPayload p, Health target, float damage)
        {
            if (!IsEnemy(target, p.casterTeam)) return;

            target.TakeDamage(damage, p.caster);
            if (target.IsDead || p.element == null) return;

            var e = p.element;
            if (e.burnDamagePerSecond > 0f || e.slowAmount > 0f)
            {
                var status = target.GetComponent<StatusEffects>();
                if (status == null) status = target.gameObject.AddComponent<StatusEffects>();
                status.ApplyBurn(e.burnDamagePerSecond, e.burnDuration, p.caster);
                status.ApplySlow(e.slowAmount, e.slowDuration);
            }
        }

        /// <summary>GDD 4.4 Chain: the spell jumps to nearby enemies, losing some damage each jump.</summary>
        public static void ChainFrom(SpellPayload p, Health first, float damage, ICollection<Health> exclude = null)
        {
            if (p.chainJumps <= 0 || first == null) return;

            var visited = new HashSet<Health> { first };
            if (exclude != null) foreach (var h in exclude) visited.Add(h);

            Health current = first;
            float dmg = damage;
            for (int i = 0; i < p.chainJumps; i++)
            {
                Health next = FindNearestEnemy(current.transform.position, p.chainRange, p.casterTeam, visited);
                if (next == null) break;

                dmg *= p.chainFalloff;
                SpellVisuals.ChainArc(current.transform.position, next.transform.position, p.Color);
                ApplyTo(p, next, dmg);
                visited.Add(next);
                current = next;
            }
        }

        public static bool IsEnemy(Health h, Team casterTeam)
        {
            return h != null && !h.IsDead && h.Team != casterTeam;
        }

        /// <summary>Closest living enemy within range, skipping anything in 'exclude'.</summary>
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

        /// <summary>All living enemies within range (each listed once).</summary>
        public static List<Health> FindEnemiesInRadius(Vector3 center, float radius, Team casterTeam)
        {
            var result = new List<Health>();
            int count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var h = overlapBuffer[i].GetComponentInParent<Health>();
                if (IsEnemy(h, casterTeam) && !result.Contains(h)) result.Add(h);
            }
            return result;
        }
    }
}
