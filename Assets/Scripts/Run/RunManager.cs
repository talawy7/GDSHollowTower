// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Runs the whole escape attempt:
//   Cell  ──start──▶  Floor1-4 in a random order  ──▶  Boss  ──▶  win
//   dying anywhere ──▶ back to the Cell (new random order next time)
// It survives scene loads (DontDestroyOnLoad) and carries the player's
// health and the run's Soul Ink from one floor to the next.
//
// You never place this in a scene: it creates itself the first time it's used.
// Playing a floor scene directly (for level design) also works: the exit then
// sends you to the Cell instead of the next floor.

using System;
using System.Collections.Generic;
using HollowTower.Core;
using HollowTower.Enemies;
using HollowTower.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowTower.Run
{
    public class RunManager : MonoBehaviour
    {
        [Tooltip("Scene names. They must be in File > Build Settings.")]
        [SerializeField] private string cellScene = "Cell";
        [SerializeField] private string[] floorScenes = { "Floor1", "Floor2", "Floor3", "Floor4" };
        [SerializeField] private string bossScene = "Boss";

        private static RunManager instance;

        private readonly List<string> order = new List<string>();
        private int floorIndex;
        private float savedHealth = -1f;

        public static RunManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject("RunManager").AddComponent<RunManager>();
                    DontDestroyOnLoad(instance.gameObject);
                }
                return instance;
            }
        }

        /// <summary>True if the RunManager already exists (doesn't create one).</summary>
        public static bool Exists => instance != null;

        public bool InRun { get; private set; }
        /// <summary>How many floors were already cleared this run (0 on the first floor). Used for difficulty.</summary>
        public int FloorsCleared => InRun ? floorIndex : 0;
        public int FloorCount => order.Count;
        /// <summary>1-based number of the current floor, e.g. "Floor 2 / 4".</summary>
        public int CurrentFloorNumber => Mathf.Min(floorIndex + 1, Mathf.Max(1, order.Count));
        public bool InBoss => InRun && floorIndex >= order.Count;
        public int SoulInkThisRun { get; private set; }
        public IReadOnlyList<string> Order => order;

        public event Action OnRunStarted;
        public event Action OnRunWon;
        public event Action OnRunLost;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnemyBase.OnAnyEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            EnemyBase.OnAnyEnemyKilled -= HandleEnemyKilled;
        }

        // ---------- run flow ----------

        /// <summary>Called by the exit in the Cell.</summary>
        public void StartRun()
        {
            order.Clear();
            foreach (var s in floorScenes)
            {
                if (Application.CanStreamedLevelBeLoaded(s)) order.Add(s);
                else Debug.LogWarning($"[RunManager] Scene '{s}' is not in Build Settings, skipping it.");
            }
            Shuffle(order);

            InRun = true;
            floorIndex = 0;
            savedHealth = -1f; // full health
            SoulInkThisRun = 0;
            OnRunStarted?.Invoke();
            Debug.Log("[RunManager] New run: " + string.Join(" → ", order) + " → " + bossScene);
            LoadNext();
        }

        /// <summary>Called by the exit of a floor once all waves are cleared.</summary>
        public void CompleteFloor()
        {
            if (!InRun)
            {
                Debug.Log("[RunManager] Floor cleared (test mode: you started this scene directly). Going to the Cell.");
                ReturnToCell();
                return;
            }
            SavePlayerHealth();
            floorIndex++;
            LoadNext();
        }

        /// <summary>Called by the exit in the Boss scene.</summary>
        public void WinRun()
        {
            InRun = false;
            Debug.Log("[RunManager] The witch escaped!");
            OnRunWon?.Invoke();
        }

        public void ReturnToCell()
        {
            InRun = false;
            if (Application.CanStreamedLevelBeLoaded(cellScene)) SceneManager.LoadScene(cellScene);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void LoadNext()
        {
            string next = floorIndex < order.Count ? order[floorIndex] : bossScene;
            if (!Application.CanStreamedLevelBeLoaded(next))
            {
                Debug.LogError($"[RunManager] Can't load '{next}'. Add it to File > Build Settings.");
                ReturnToCell();
                return;
            }
            SceneManager.LoadScene(next);
        }

        // ---------- carrying state between scenes ----------

        private void SavePlayerHealth()
        {
            var player = FindAnyObjectByType<PlayerController>();
            var h = player != null ? player.GetComponent<Health>() : null;
            savedHealth = h != null ? h.Current : -1f;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            var h = player.GetComponent<Health>();
            if (h == null) return;

            if (InRun && savedHealth > 0f) h.SetCurrent(savedHealth);
            h.OnDied += HandlePlayerDied;
        }

        private void HandlePlayerDied(GameObject killer)
        {
            if (!InRun) return;
            InRun = false;
            Debug.Log($"[RunManager] Died on floor {CurrentFloorNumber} to {KillerName(killer)}.");
            OnRunLost?.Invoke();
            // The HUD's "press R" sends the player back to the Cell.
        }

        private void HandleEnemyKilled(EnemyBase enemy, GameObject killer)
        {
            if (enemy.Data != null) SoulInkThisRun += enemy.Data.soulInk;
        }

        /// <summary>"Goblin" instead of "Goblin(Clone)". The Scribe will use this to remember what killed you.</summary>
        public static string KillerName(GameObject killer)
        {
            if (killer == null) return "something";
            var enemy = killer.GetComponent<EnemyBase>();
            if (enemy != null && enemy.Data != null) return enemy.Data.displayName;
            return killer.name.Replace("(Clone)", "").Trim();
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
