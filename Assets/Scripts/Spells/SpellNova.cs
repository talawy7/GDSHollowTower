// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.3 Nova: a blast in a circle around the witch.
// Bounce modifier on a Nova = it pulses again (once per bounce).
// Chain on a Nova = every enemy hit arcs to enemies outside the blast.

using System.Collections.Generic;
using HollowTower.Core;
using UnityEngine;

namespace HollowTower.Spells
{
    public class SpellNova : MonoBehaviour
    {
        private const float PulseInterval = 0.35f;

        private SpellPayload payload;
        private Transform follow;
        private float heightOffset;
        private int pulsesLeft;
        private float timer;

        public static SpellNova Spawn(SpellPayload p, Transform caster, float heightOffset)
        {
            var go = new GameObject(p.FullName);
            var nova = go.AddComponent<SpellNova>();
            nova.payload = p;
            nova.follow = caster;
            nova.heightOffset = heightOffset;
            nova.pulsesLeft = 1 + p.bounces;
            nova.Pulse();
            return nova;
        }

        private void Pulse()
        {
            pulsesLeft--;
            timer = PulseInterval;

            Vector3 center = follow != null ? follow.position + Vector3.up * heightOffset : transform.position;
            transform.position = center;
            float r = payload.delivery.novaRadius;

            List<Health> targets = SpellHit.FindEnemiesInRadius(center, r, payload.casterTeam);
            foreach (var h in targets) SpellHit.Hit(payload, h, targets);

            SpellVisuals.Ring(center, payload.Color, r);

            if (pulsesLeft <= 0) Destroy(gameObject);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Pulse();
        }
    }
}
