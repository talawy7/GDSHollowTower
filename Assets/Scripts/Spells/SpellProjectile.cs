// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// A moving spell ball. Used by every attack (GDD 5.3):
//   Normal / Projectile: stops at the first enemy
//   Orb:                 passes through enemies (each hit once)
//   Nova:                explodes on impact and hits everything around it
// Uses sphere casts every frame so fast shots can't pass through thin walls.

using System.Collections.Generic;
using HollowTower.Core;
using UnityEngine;

namespace HollowTower.Spells
{
    public class SpellProjectile : MonoBehaviour
    {
        private SpellPayload payload;
        private Vector3 direction;
        private float speed, radius, distanceLeft, explosionRadius;
        private bool pierces;

        private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
        private readonly RaycastHit[] hitBuffer = new RaycastHit[32];

        public static SpellProjectile Spawn(SpellPayload p, Vector3 origin, Vector3 dir)
        {
            var go = new GameObject(p.FullName);
            go.transform.position = origin;

            var proj = go.AddComponent<SpellProjectile>();
            proj.payload = p;
            proj.direction = Flat(dir);
            proj.speed = p.attack.speed;
            proj.radius = p.attack.radius;
            proj.distanceLeft = p.attack.maxDistance;
            proj.pierces = p.attack.piercesEnemies;
            proj.explosionRadius = p.attack.explosionRadius;

            SpellVisuals.AttachBall(go.transform, p.Color, p.attack.radius * 2f);
            return proj;
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;
            Vector3 pos = transform.position;

            int count = Physics.SphereCastNonAlloc(pos, radius, direction, hitBuffer, step, ~0, QueryTriggerInteraction.Ignore);
            SortByDistance(hitBuffer, count);

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hitBuffer[i];
                Vector3 point = hit.distance <= 0f ? pos : hit.point; // overlapping at start: Unity gives zero

                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null)
                {
                    if (!SpellHit.IsEnemy(h, payload.casterTeam)) continue; // our side or dead: fly through

                    if (explosionRadius > 0f) { Explode(point); return; }

                    if (pierces)
                    {
                        if (alreadyHit.Add(h)) SpellHit.Hit(payload, h);
                        continue;
                    }

                    SpellHit.Hit(payload, h);
                    Impact(point);
                    return;
                }

                // No Health = wall, pillar or prop.
                if (explosionRadius > 0f) Explode(point);
                else Impact(point);
                return;
            }

            transform.position = pos + direction * step;
            distanceLeft -= step;
            if (distanceLeft <= 0f)
            {
                if (explosionRadius > 0f) Explode(transform.position);
                else Impact(transform.position);
            }
        }

        private void Explode(Vector3 point)
        {
            SpellHit.Explode(payload, point, explosionRadius);
            SpellVisuals.Burst(point, payload.Color, explosionRadius * 0.8f);
            Destroy(gameObject);
        }

        private void Impact(Vector3 point)
        {
            SpellVisuals.Burst(point, payload.Color, Mathf.Max(0.5f, radius * 3f));
            Destroy(gameObject);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        private static void SortByDistance(RaycastHit[] hits, int count)
        {
            for (int i = 1; i < count; i++)
            {
                var key = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].distance > key.distance) { hits[j + 1] = hits[j]; j--; }
                hits[j + 1] = key;
            }
        }
    }
}
