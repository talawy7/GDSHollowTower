// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Builds the room's NavMesh when the room appears, before any enemy starts.
// Each room prefab gets its own (see the level plan: rooms are prefabs
// placed one after another by the RunManager), so nothing needs baking by hand.

using Unity.AI.Navigation;
using UnityEngine;

namespace HollowTower.Rooms
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
        }
    }
}
