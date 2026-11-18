// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Slow magic bolt shot by enemies (Ghost). Hurts the player, stops at walls.
// Passes through the player while she is dodging (Health.Invulnerable),
// so dodging through a bolt feels good.

using HollowTower.Core;
using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Enemies
{
    public class EnemyProjectile : MonoBehaviour
    {
        private const float MaxLifetime = 6f;

        private Vector3 direction;
        private float speed, radius, damage, life;
        private GameObject owner;
        private Color color;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        public static EnemyProjectile Spawn(Vector3 origin, Vector3 dir, float speed, float radius,
                                            float damage, Color color, GameObject owner)
        {
            var go = new GameObject("EnemyBolt");
            go.transform.position = origin;
            var p = go.AddComponent<EnemyProjectile>();
            dir.y = 0f;
            p.direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
            p.speed = speed;
            p.radius = radius;
            p.damage = damage;
            p.owner = owner;
            p.color = color;
            SpellVisuals.AttachBall(go.transform, color, radius * 2f);
            return p;
        }

        private void Update()
        {
            life += Time.deltaTime;
            if (life > MaxLifetime) { Destroy(gameObject); return; }

            float step = speed * Time.deltaTime;
            Vector3 pos = transform.position;
            int count = Physics.SphereCastNonAlloc(pos, radius, direction, hits, step, ~0, QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            int nearestIndex = -1;
            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (owner != null && col.transform.IsChildOf(owner.transform)) continue;
                var h = col.GetComponentInParent<Health>();
                if (h != null && (h.Team == Team.Enemy || h.IsDead || h.Invulnerable)) continue; // fly through allies / dodging player
                if (hits[i].distance < nearest) { nearest = hits[i].distance; nearestIndex = i; }
            }

            if (nearestIndex >= 0)
            {
                var hit = hits[nearestIndex];
                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null) h.TakeDamage(damage, owner);
                SpellVisuals.Burst(hit.distance <= 0f ? pos : hit.point, color, 0.8f);
                Destroy(gameObject);
                return;
            }

            transform.position = pos + direction * step;
        }
    }
}
