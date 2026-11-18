// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.3: hold left click to cast the selected slot toward the mouse.
// No mana; the only limit is the time between casts.
// Builds a SpellPayload from element + delivery + prepared modifiers and
// spawns the right delivery.

using System;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(SpellLoadout))]
    [RequireComponent(typeof(ModifierController))]
    public class SpellCaster : MonoBehaviour
    {
        [Tooltip("How far in front of the witch spells appear.")]
        [SerializeField] private float spawnForward = 0.8f;
        [Tooltip("Height offset from the witch's pivot. 0 works when the pivot is at her chest (capsule center).")]
        [SerializeField] private float castHeight = 0f;

        private PlayerController player;
        private PlayerInputReader input;
        private SpellLoadout loadout;
        private ModifierController modifiers;
        private Health health;

        private float cooldownLeft;

        /// <summary>(payload) — fired on every cast. Later: sounds, Scribe spell-use stats.</summary>
        public event Action<SpellPayload> OnCast;

        public float CooldownLeft => cooldownLeft;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            loadout = GetComponent<SpellLoadout>();
            modifiers = GetComponent<ModifierController>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            cooldownLeft -= Time.deltaTime;

            if (!player.ControlEnabled || player.IsDodging) return;
            if (health != null && health.IsDead) return;

            // CastPressed catches very quick clicks that start and end inside one frame.
            if ((input.CastHeld || input.CastPressed) && cooldownLeft <= 0f) Cast();
        }

        private void Cast()
        {
            SpellSlot slot = loadout.Current;
            if (slot == null || !slot.IsValid) return;

            ElementData element = slot.element;
            DeliveryData delivery = slot.delivery;

            modifiers.ConsumePrepared(out ModifierData chainMod, out ModifierData bounceMod);

            var payload = new SpellPayload
            {
                element = element,
                delivery = delivery,
                damage = delivery.baseDamage * element.damageMultiplier,
                caster = gameObject,
                casterTeam = health != null ? health.Team : Team.Player,
                chainJumps = element.innateChainJumps + (chainMod != null ? chainMod.chainJumps : 0),
                chainRange = chainMod != null ? chainMod.chainRange : 5f,
                chainFalloff = chainMod != null ? chainMod.chainDamageFalloff : 0.6f,
                bounces = bounceMod != null ? bounceMod.bounces : 0,
                usedChain = chainMod != null,
                usedBounce = bounceMod != null,
            };

            Vector3 dir = player.AimDirection;
            switch (delivery.type)
            {
                case DeliveryType.Projectile:
                case DeliveryType.Orb:
                    Vector3 origin = transform.position + Vector3.up * castHeight + dir * spawnForward;
                    SpellProjectile.Spawn(payload, origin, dir);
                    break;

                case DeliveryType.Nova:
                    SpellNova.Spawn(payload, transform, castHeight);
                    break;
            }

            cooldownLeft = Mathf.Max(0.05f, delivery.castCooldown * element.cooldownMultiplier);
            OnCast?.Invoke(payload);
        }
    }
}
