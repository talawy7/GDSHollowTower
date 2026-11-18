// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 5.1 / 5.2 Elemental Modifier (Q) = Chain Reaction.
// It's rechargeable: dealing damage fills it (CombatSettings.chainChargeRequired).
// When full, Q pulls the enemies around the mouse together, chains them,
// and applies the current element's effect:
//   Fire:      burn (Projectile selected: + damage, Orb/Nova selected: stronger burn)
//   Ice:       frozen for 6 seconds
//   Lightning: continuous damage for 10 seconds

using System;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(SpellLoadout))]
    public class ChainReactionAbility : MonoBehaviour
    {
        private PlayerInputReader input;
        private PlayerController player;
        private SpellLoadout loadout;
        private DeliveryAbility delivery;
        private Health health;

        private float charge;

        public float Charge => charge;
        public float Required => Mathf.Max(1f, CombatSettings.Current.chainChargeRequired);
        public float Charge01 => Mathf.Clamp01(charge / Required);
        public bool Ready => charge >= Required;

        /// <summary>(number of enemies chained)</summary>
        public event Action<int> OnFired;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            player = GetComponent<PlayerController>();
            loadout = GetComponent<SpellLoadout>();
            delivery = GetComponent<DeliveryAbility>();
            health = GetComponent<Health>();
        }

        private void OnEnable() => SpellHit.OnDamageDealt += HandleDamageDealt;
        private void OnDisable() => SpellHit.OnDamageDealt -= HandleDamageDealt;

        private void HandleDamageDealt(GameObject source, float amount)
        {
            if (source != gameObject) return;
            charge = Mathf.Min(Required, charge + amount);
        }

        private void Update()
        {
            if (!input.ChainReactionPressed || !Ready) return;
            if (!player.ControlEnabled || (health != null && health.IsDead)) return;
            Fire();
        }

        private void Fire()
        {
            ElementData element = loadout.Current;
            if (element == null) return;
            ChainReactionData data = element.chainReaction;
            Team team = health != null ? health.Team : Team.Player;

            Vector3 center = player.AimPoint;
            var targets = SpellHit.FindEnemiesInRadius(center, data.pullRadius, team);
            if (targets.Count == 0) return; // nothing to chain: keep the charge
            if (targets.Count > data.maxTargets) targets.RemoveRange(data.maxTargets, targets.Count - data.maxTargets);

            // Fire's bonus depends on the selected delivery style (GDD 5.2).
            HitEffect effect = data.effect;
            if (element.type == ElementType.Fire && delivery != null)
            {
                if (delivery.Selected == DeliveryType.Projectile) effect.damage += data.projectileBonusDamage;
                else effect.burnDps *= data.orbNovaBurnMultiplier;
            }

            // Pull everyone toward the center (stopping a little short so they don't overlap).
            float pullTime = Mathf.Max(0.05f, data.pullDuration);
            foreach (var t in targets)
            {
                Vector3 toCenter = center - t.transform.position;
                toCenter.y = 0f;
                float dist = Mathf.Max(0f, toCenter.magnitude - 0.9f);
                if (dist > 0.05f) StatusEffects.GetOrAdd(t).Push(toCenter.normalized * (dist / pullTime), pullTime);
            }

            foreach (var t in targets) SpellHit.ApplyHit(t, effect, element, gameObject, team);

            ChainLinkVisual.Create(targets, element.color, Mathf.Max(data.chainDuration, pullTime));
            SpellVisuals.Ring(SpellVisuals.GroundBelow(center + Vector3.up), element.color, data.pullRadius);

            charge = 0f;
            OnFired?.Invoke(targets.Count);
        }

        /// <summary>For testing / rewards.</summary>
        public void FillCharge() => charge = Required;
    }
}
