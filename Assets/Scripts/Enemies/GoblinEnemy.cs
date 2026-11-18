// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Goblin: fast, weak melee that comes in groups (the swarm enemy).
// Short windup, quick slash. Dangerous in numbers, good target for Nova and Chain Reaction.

using UnityEngine;

namespace HollowTower.Enemies
{
    public class GoblinEnemy : EnemyBase
    {
        protected override void PerformAttack()
        {
            // Small reach bonus so a goblin that's right next to you doesn't miss by a hair.
            Vector3 center = transform.position + transform.forward * 0.6f;
            TryHitPlayer(center, data.attackRange);
        }
    }
}
