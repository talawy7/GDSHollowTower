// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Moving spell ball, used for both Projectile and Orb (GDD 4.3):
//   Projectile: fast, stops at the first enemy.
//   Orb:        slow, passes through enemies (each is hit once).
// Bounce modifier: bounces off walls; a Projectile also redirects to the next enemy.
// Uses sphere casts every frame, so fast shots can't pass through thin walls.

using System.Collections.Generic;
using HollowTower.Core;
using UnityEngine;

namespace HollowTower.Spells
{
    public class SpellProjectile : MonoBehaviour
    {
        private const float BounceSeekRange = 12f;

        private SpellPayload payload;
        private Vector3 direction;
        private float speed;
        private float radius;
        private float distanceLeft;
        private bool pierces;
        private int bouncesLeft;

        private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
        private Health lastHit;
        private readonly RaycastHit[] hitBuffer = new RaycastHit[32];

        public static SpellProjectile Spawn(SpellPayload p, Vector3 origin, Vector3 dir)
        {
            var go = new GameObject(p.FullName);
            go.transform.position = origin;

            var proj = go.AddComponent<SpellProjectile>();
            proj.payload = p;
            proj.direction = Flat(dir);
            proj.speed = p.delivery.speed * p.element.speedMultiplier;
            proj.radius = p.delivery.radius;
            proj.distanceLeft = p.delivery.maxDistance;
            proj.pierces = p.delivery.piercesEnemies;
            proj.bouncesLeft = p.bounces;

            SpellVisuals.AttachBall(go.transform, p.Color, p.delivery.radius * 2f);
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
                // Overlapping at the start of the cast: Unity gives point = zero.
                Vector3 point = hit.distance <= 0f ? pos : hit.point;
                Vector3 normal = hit.distance <= 0f ? -direction : hit.normal;

                var h = hit.collider.GetComponentInParent<Health>();
                if (h != null)
                {
                    // Our own side, or dead: fly through.
                    if (!SpellHit.IsEnemy(h, payload.casterTeam)) continue;

                    if (pierces)
                    {
                        if (alreadyHit.Add(h)) SpellHit.Hit(payload, h);
                        continue;
                    }

                    if (h == lastHit) continue; // just bounced off this one
                    SpellHit.Hit(payload, h);

                    if (bouncesLeft > 0)
                    {
                        alreadyHit.Add(h);
                        Health next = SpellHit.FindNearestEnemy(h.transform.position, BounceSeekRange, payload.casterTeam, alreadyHit);
                        if (next != null)
                        {
                            bouncesLeft--;
                            lastHit = h;
                            transform.position = new Vector3(point.x, pos.y, point.z);
                            direction = Flat(next.transform.position - transform.position);
                            SpellVisuals.Burst(point, payload.Color, radius * 3f);
                            return;
                        }
                    }

                    Impact(point);
                    return;
                }

                // Anything without Health is a wall / pillar / prop.
                if (bouncesLeft > 0)
                {
                    bouncesLeft--;
                    lastHit = null;
                    direction = Flat(Vector3.Reflect(direction, normal));
                    Vector3 bouncePos = point + normal * (radius + 0.02f);
                    transform.position = new Vector3(bouncePos.x, pos.y, bouncePos.z);
                    SpellVisuals.Burst(point, payload.Color, radius * 3f);
                    return;
                }

                Impact(point);
                return;
            }

            transform.position = pos + direction * step;
            distanceLeft -= step;
            if (distanceLeft <= 0f) Impact(transform.position);
        }

        private void Impact(Vector3 point)
        {
            SpellVisuals.Burst(point, payload.Color, Mathf.Max(0.6f, radius * 4f));
            Destroy(gameObject);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        private static void SortByDistance(RaycastHit[] hits, int count)
        {
            // Small insertion sort: SphereCastNonAlloc results are unordered.
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
