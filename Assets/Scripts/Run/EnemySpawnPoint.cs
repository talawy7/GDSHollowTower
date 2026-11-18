// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Marker for where wave enemies can appear. Place as many as you like in a
// floor scene (empty GameObject + this component). The WaveSpawner picks
// points that are away from the player. Shows a red marker in the Scene view.

using UnityEngine;

namespace HollowTower.Run
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [Tooltip("Enemies appear within this radius around the point.")]
        public float radius = 1.5f;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }
    }
}
