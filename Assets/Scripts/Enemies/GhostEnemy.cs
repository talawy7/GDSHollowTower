// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Ghost: floats at a distance and shoots slow magic bolts (the ranged enemy).
// Keeps about 'preferredDistance' from the player and backs off if the player
// gets too close. Only shoots when it can see the player.

using UnityEngine;
using UnityEngine.AI;

namespace HollowTower.Enemies
{
    public class GhostEnemy : EnemyBase
    {
        private const float ShootHeight = 1.0f;

        protected override void UpdateChase()
        {
            if (!PlayerAvailable) { SetState(EnemyState.Idle); return; }

            float dist = DistanceToPlayer;
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            if (dist < data.retreatDistance)
            {
                // Too close: drift away from the player.
                Vector3 away = transform.position - toPlayer.normalized * 3f;
                if (NavMesh.SamplePosition(away, out NavMeshHit hit, 2f, NavMesh.AllAreas)) MoveTo(hit.position);
            }
            else if (dist > data.preferredDistance + 1f)
            {
                MoveTo(player.position);
            }
            else
            {
                StopMoving();
            }

            FaceDirection(toPlayer);

            bool inRange = dist <= data.preferredDistance + 2f;
            if (inRange && attackCooldownLeft <= 0f && CanSeePlayer()) BeginAttack();
        }

        protected override void PerformAttack()
        {
            if (player == null) return;
            Vector3 origin = transform.position + Vector3.up * ShootHeight + transform.forward * 0.6f;
            Vector3 target = player.position;
            target.y = origin.y;
            EnemyProjectile.Spawn(origin, target - origin, data.projectileSpeed, data.projectileRadius,
                                  Damage, data.projectileColor, gameObject);
        }

        private bool CanSeePlayer()
        {
            Vector3 from = transform.position + Vector3.up * ShootHeight;
            Vector3 to = player.position;
            to.y = from.y;
            if (!Physics.Linecast(from, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            // Seeing the player or another character is fine; walls block.
            return hit.collider.GetComponentInParent<HollowTower.Core.Health>() != null;
        }
    }
}
