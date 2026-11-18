// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 5.4: when an enemy already carries one element and gets hit by a
// different one, a reaction happens and both elements are used up.
//   Fire + Ice        -> Shatter         big burst of damage (breaks freeze)
//   Fire + Lightning  -> Overload        explosion, knockback, 2s stun
//   Ice + Lightning   -> Charged Freeze  short freeze + 3 electric charges
// The numbers live in CombatSettings.

using System;
using HollowTower.Core;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Spells
{
    public enum ReactionType { Shatter, Overload, ChargedFreeze }

    public static class ElementalReactions
    {
        /// <summary>(reaction, world position) — for floating text, sounds, and the Scribe's stats later.</summary>
        public static event Action<ReactionType, Vector3> OnReaction;

        public static void OnElementApplied(Health target, StatusEffects status, ElementType incoming,
                                            GameObject caster, Team casterTeam)
        {
            var settings = CombatSettings.Current;

            if (status.HasAura && status.Aura != incoming)
            {
                ElementType existing = status.Aura;
                status.ClearAura();
                Trigger(Combine(existing, incoming), target, status, caster, casterTeam, settings);
            }
            else
            {
                status.SetAura(incoming, settings.auraDuration);
            }
        }

        private static ReactionType Combine(ElementType a, ElementType b)
        {
            bool fire = a == ElementType.Fire || b == ElementType.Fire;
            bool ice = a == ElementType.Ice || b == ElementType.Ice;
            if (fire && ice) return ReactionType.Shatter;
            if (fire) return ReactionType.Overload;   // fire + lightning
            return ReactionType.ChargedFreeze;         // ice + lightning
        }

        private static void Trigger(ReactionType type, Health target, StatusEffects status,
                                    GameObject caster, Team casterTeam, CombatSettings s)
        {
            Vector3 center = SpellVisuals.CenterOf(target);

            switch (type)
            {
                case ReactionType.Shatter:
                    status.BreakFreeze();
                    SpellHit.DealDamage(target, s.shatterDamage, caster);
                    SpellVisuals.Burst(center, new Color(0.8f, 0.95f, 1f), 2.2f);
                    break;

                case ReactionType.Overload:
                    foreach (var h in SpellHit.FindEnemiesInRadius(center, s.overloadRadius, casterTeam))
                    {
                        Vector3 away = h.transform.position - target.transform.position;
                        away.y = 0f;
                        if (away.sqrMagnitude < 0.01f) away = UnityEngine.Random.insideUnitSphere;
                        var hs = StatusEffects.GetOrAdd(h);
                        SpellHit.DealDamage(h, s.overloadDamage, caster);
                        hs.Stun(s.overloadStun);
                        hs.Push(away.normalized * s.overloadKnockback, 0.25f);
                    }
                    SpellVisuals.Ring(SpellVisuals.GroundBelow(center), new Color(1f, 0.55f, 0.9f), s.overloadRadius);
                    SpellVisuals.Burst(center, new Color(1f, 0.6f, 0.3f), 1.8f);
                    break;

                case ReactionType.ChargedFreeze:
                    status.Freeze(s.chargedFreezeFreeze);
                    status.AddCharges(s.chargedFreezeCharges, s.chargedFreezeDamagePerCharge, s.chargedFreezeInterval, caster);
                    SpellVisuals.Burst(center, new Color(0.7f, 0.8f, 1f), 1.6f);
                    break;
            }

            OnReaction?.Invoke(type, center);
        }
    }
}
