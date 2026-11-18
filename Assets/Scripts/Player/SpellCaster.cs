// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
// Week 2: Claude rewrote it for the new GDD combat (section 5).
//
// Left click: the current element's Normal Attack. No cooldown (GDD 5.1);
// only a small fire interval so holding the button shoots a steady stream.
// DeliveryAbility (E) calls CastDelivery() for the stronger
// Projectile / Orb / Nova version.

using System;
using System.Collections;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(SpellLoadout))]
    public class SpellCaster : MonoBehaviour
    {
        [Tooltip("How far in front of the witch spells appear.")]
        [SerializeField] private float spawnForward = 0.8f;
        [Tooltip("Height offset from the witch's pivot. 0 works when the pivot is at her chest (capsule center).")]
        [SerializeField] private float castHeight = 0f;

        private PlayerController player;
        private PlayerInputReader input;
        private SpellLoadout loadout;
        private Health health;

        private float normalCooldownLeft;

        /// <summary>(payload of the first shot) — fired on every cast. Later: sounds, Scribe spell stats.</summary>
        public event Action<SpellPayload> OnCast;

        public bool CanCast => player.ControlEnabled && !player.IsDodging && (health == null || !health.IsDead);

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            loadout = GetComponent<SpellLoadout>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            normalCooldownLeft -= Time.deltaTime;
            if (!CanCast) return;

            // CastPressed catches very quick clicks that start and end inside one frame.
            if ((input.CastHeld || input.CastPressed) && normalCooldownLeft <= 0f) CastNormal();
        }

        private void CastNormal()
        {
            ElementData e = loadout.Current;
            if (e == null) return;
            Cast(e, e.normalAttack, null);
            normalCooldownLeft = Mathf.Max(0.05f, e.normalFireInterval);
        }

        /// <summary>Called by DeliveryAbility (E). Returns false if nothing was cast.</summary>
        public bool CastDelivery(DeliveryType delivery)
        {
            ElementData e = loadout.Current;
            if (e == null || !CanCast) return false;
            Cast(e, e.GetDelivery(delivery), delivery);
            return true;
        }

        private void Cast(ElementData e, AttackData attack, DeliveryType? delivery)
        {
            var first = MakePayload(e, attack, delivery);
            OnCast?.Invoke(first);
            if (attack.shots <= 1) Shoot(first);
            else StartCoroutine(Burst(e, attack, delivery));
        }

        // Multi-shot attacks (e.g. Fire Projectile = 3 shots) fire in a quick burst toward the current aim.
        private IEnumerator Burst(ElementData e, AttackData attack, DeliveryType? delivery)
        {
            for (int i = 0; i < attack.shots; i++)
            {
                if (i > 0) yield return new WaitForSeconds(attack.timeBetweenShots);
                if (health != null && health.IsDead) yield break;
                Shoot(MakePayload(e, attack, delivery));
            }
        }

        private SpellPayload MakePayload(ElementData e, AttackData attack, DeliveryType? delivery)
        {
            return new SpellPayload
            {
                element = e,
                attack = attack,
                hit = attack.hit,
                caster = gameObject,
                casterTeam = health != null ? health.Team : Team.Player,
                delivery = delivery,
            };
        }

        private void Shoot(SpellPayload p)
        {
            Vector3 dir = player.AimDirection;
            Vector3 origin = transform.position + Vector3.up * castHeight + dir * spawnForward;
            SpellProjectile.Spawn(p, origin, dir);
        }
    }
}
