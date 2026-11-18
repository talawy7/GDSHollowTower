// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Sends a floor's enemies in waves instead of all at once.
//   1. Waits a moment after the floor loads.
//   2. Spawns wave 1 at EnemySpawnPoints away from the player; they chase right away.
//   3. When the wave is (almost) dead, waits a little and sends the next one.
//   4. After the last wave: the floor is cleared and the exit opens.
// The waves themselves are set in a FloorData asset.

using System;
using System.Collections;
using System.Collections.Generic;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Enemies;
using HollowTower.Player;
using HollowTower.Spells;
using UnityEngine;
using UnityEngine.AI;

namespace HollowTower.Run
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private FloorData floor;
        [Tooltip("Spawn points closer than this to the player are skipped when possible.")]
        [SerializeField] private float minDistanceFromPlayer = 7f;

        private readonly List<EnemySpawnPoint> points = new List<EnemySpawnPoint>();
        private readonly List<Health> alive = new List<Health>();
        private Transform player;

        public FloorData Floor => floor;
        /// <summary>1-based number of the current wave (0 before the first).</summary>
        public int CurrentWave { get; private set; }
        public int WaveCount => floor != null ? floor.waves.Count : 0;
        public int AliveCount => alive.Count;
        public bool Cleared { get; private set; }

        public event Action<int> OnWaveStarted;
        public event Action OnFloorCleared;

        private void Start()
        {
            points.AddRange(FindObjectsByType<EnemySpawnPoint>());
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;

            if (floor == null || floor.waves.Count == 0)
            {
                Debug.LogWarning("[WaveSpawner] No FloorData or no waves: the floor counts as cleared.", this);
                FinishFloor();
                return;
            }
            if (points.Count == 0)
                Debug.LogWarning("[WaveSpawner] No EnemySpawnPoints in the scene: enemies will spawn at the spawner.", this);

            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            yield return new WaitForSeconds(floor.timeBeforeFirstWave);

            for (int w = 0; w < floor.waves.Count; w++)
            {
                Wave wave = floor.waves[w];
                CurrentWave = w + 1;
                SpawnWave(wave);
                OnWaveStarted?.Invoke(CurrentWave);

                bool last = w == floor.waves.Count - 1;
                int threshold = last ? 0 : Mathf.Max(0, wave.startNextWhenRemaining);
                yield return new WaitUntil(() => { CleanUp(); return alive.Count <= threshold; });

                if (!last) yield return new WaitForSeconds(floor.timeBetweenWaves);
            }

            FinishFloor();
        }

        private void SpawnWave(Wave wave)
        {
            float healthMultiplier = 1f + floor.healthBonusPerFloorCleared * RunManager.Instance.FloorsCleared;

            foreach (var entry in wave.enemies)
            {
                if (entry.enemyPrefab == null) continue;
                for (int i = 0; i < entry.count; i++)
                {
                    Vector3 pos = PickSpawnPosition();
                    Vector3 look = player != null ? player.position - pos : Vector3.back;
                    look.y = 0f;
                    var go = Instantiate(entry.enemyPrefab, pos,
                        look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity);

                    var enemy = go.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.Configure(UnityEngine.Random.value < entry.eliteChance, healthMultiplier);
                        enemy.Alert();
                    }
                    var h = go.GetComponent<Health>();
                    if (h != null) alive.Add(h);

                    SpellVisuals.Ring(pos, new Color(0.6f, 0.1f, 0.15f), 1.2f);
                }
            }
        }

        private Vector3 PickSpawnPosition()
        {
            Vector3 center = transform.position;
            float radius = 2f;

            if (points.Count > 0)
            {
                // Prefer points away from the player so enemies don't appear on top of her.
                var far = points.FindAll(p => player == null ||
                    Vector3.Distance(p.transform.position, player.position) >= minDistanceFromPlayer);
                var pool = far.Count > 0 ? far : points;
                var point = pool[UnityEngine.Random.Range(0, pool.Count)];
                center = point.transform.position;
                radius = point.radius;
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 pos = center + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3f, NavMesh.AllAreas)) return hit.position;
            return center;
        }

        private void CleanUp() => alive.RemoveAll(h => h == null || h.IsDead);

        private void FinishFloor()
        {
            if (Cleared) return;
            Cleared = true;
            foreach (var exit in FindObjectsByType<LevelExit>())
                if (exit.Type != ExitType.StartRun) exit.Open();
            OnFloorCleared?.Invoke();
        }
    }
}
