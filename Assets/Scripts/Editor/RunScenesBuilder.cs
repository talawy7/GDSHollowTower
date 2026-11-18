// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// EDITOR ONLY. Menu: Hollow Tower > Create Run Scenes (Cell, Floor1-4, Boss)
// Creates base scenes for the run: Cell, Floor1, Floor2, Floor3, Floor4, Boss.
// Each has only a base floor, low border walls, the player, camera, test HUD,
// and (floors / boss) enemy spawn points, a WaveSpawner and an exit.
// Everything else is for you to design.
//
// SAFE TO RUN AGAIN: scenes that already exist are never touched, so your
// level design is never overwritten. Delete a scene file to recreate it.
// Also creates Assets/Prefabs/Player.prefab and the FloorData wave assets.

using System.Collections.Generic;
using System.Linq;
using HollowTower.CameraSystem;
using HollowTower.Data;
using HollowTower.Rooms;
using HollowTower.Run;
using HollowTower.Testing;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using B = HollowTower.EditorTools.SpellTestSceneBuilder;

namespace HollowTower.EditorTools
{
    public static class RunScenesBuilder
    {
        private const string SceneDir = "Assets/Scenes/Run";
        private const string FloorDataDir = "Assets/Data/Floors";
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        /// <summary>The player is on this layer so the NavMesh bake ignores her (2 = Ignore Raycast).</summary>
        private const int PlayerLayer = 2;

        private enum Kind { Cell, Floor, Boss }

        private struct SceneDef
        {
            public string name; public Kind kind;
            public SceneDef(string n, Kind k) { name = n; kind = k; }
        }

        private static readonly SceneDef[] Scenes =
        {
            new SceneDef("Cell", Kind.Cell),
            new SceneDef("Floor1", Kind.Floor),
            new SceneDef("Floor2", Kind.Floor),
            new SceneDef("Floor3", Kind.Floor),
            new SceneDef("Floor4", Kind.Floor),
            new SceneDef("Boss", Kind.Boss),
        };

        [MenuItem("Hollow Tower/Create Run Scenes (Cell, Floor1-4, Boss)")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            B.EnsureFolder("Assets", "Scenes");
            B.EnsureFolder("Assets/Scenes", "Run");
            B.EnsureFolder("Assets", "Data");
            B.EnsureFolder("Assets/Data", "Floors");

            var created = new List<string>();
            var kept = new List<string>();

            foreach (var def in Scenes)
            {
                string path = $"{SceneDir}/{def.name}.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) { kept.Add(def.name); continue; }

                // New scene first, then (re)load the assets (NewScene unloads unused ones).
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var a = B.EnsureCoreAssets();
                var playerPrefab = EnsurePlayerPrefab(a);
                FloorData floor = def.kind == Kind.Cell ? null : EnsureFloorData(def.name, a);

                BuildScene(def, a, playerPrefab, floor);
                EditorSceneManager.SaveScene(scene, path);
                created.Add(def.name);
            }

            SetBuildOrder();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{SceneDir}/Cell.unity");

            Debug.Log("[Hollow Tower] Run scenes ready in " + SceneDir +
                      (created.Count > 0 ? ". Created: " + string.Join(", ", created) : "") +
                      (kept.Count > 0 ? ". Kept your existing: " + string.Join(", ", kept) : "") +
                      ". Press Play in the Cell and walk into the light to start a run.");
        }

        // ---------- scene contents ----------

        private static void BuildScene(SceneDef def, B.CoreAssets a, GameObject playerPrefab, FloorData floor)
        {
            bool isCell = def.kind == Kind.Cell;
            float size = isCell ? 16f : 40f;
            float half = size / 2f;

            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.intensity = 0.7f;
                sun.color = new Color(0.75f, 0.8f, 1f);
                sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }

            // Level root: everything you build goes here (or anywhere: the NavMesh bake collects ALL colliders,
            // except the player). Walls and floors need colliders so the player and enemies can't pass.
            var level = new GameObject("Level (design here)");
            var surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(1 << PlayerLayer);
            level.AddComponent<RuntimeNavMeshBaker>();

            var lt = level.transform;
            B.Block("BaseFloor", lt, new Vector3(0, -0.5f, 0), new Vector3(size, 1, size), a.floorMat);
            var walls = new GameObject("BorderWalls (replace with your design)").transform;
            walls.SetParent(lt, false);
            B.Block("Wall_N", walls, new Vector3(0, 0.5f, half + 0.5f), new Vector3(size + 2, 1, 1), a.wallMat);
            B.Block("Wall_S", walls, new Vector3(0, 0.5f, -half - 0.5f), new Vector3(size + 2, 1, 1), a.wallMat);
            B.Block("Wall_E", walls, new Vector3(half + 0.5f, 0.5f, 0), new Vector3(1, 1, size), a.wallMat);
            B.Block("Wall_W", walls, new Vector3(-half - 0.5f, 0.5f, 0), new Vector3(1, 1, size), a.wallMat);

            // Player
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0, 1f, isCell ? -4f : -half + 4f);

            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                var tdc = cam.gameObject.AddComponent<TopDownCamera>();
                B.SetRef(tdc, "target", player.transform);
            }

            var hud = new GameObject("TestHUD").AddComponent<SpellTestHUD>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("player").objectReferenceValue = player;
            B.SetArray(hso.FindProperty("allElements"), a.fire, a.ice, a.lightning);
            hso.ApplyModifiedPropertiesWithoutUndo();

            if (isCell)
            {
                new GameObject("ScribeSpot (Scribe goes here, week 5)").transform.position = new Vector3(-4f, 0f, 3f);
                Exit("RunStart_Exit", new Vector3(0, 0, 5f), ExitType.StartRun);
                return;
            }

            // Waves + spawn points + exit
            var spawner = new GameObject("WaveSpawner").AddComponent<WaveSpawner>();
            spawner.transform.position = new Vector3(0, 0, 6f);
            B.SetRef(spawner, "floor", floor);

            var spRoot = new GameObject("EnemySpawnPoints (move these around)").transform;
            Vector3[] spots =
            {
                new Vector3(-12, 0, 8), new Vector3(12, 0, 8), new Vector3(-15, 0, -2),
                new Vector3(15, 0, -2), new Vector3(-7, 0, 15), new Vector3(7, 0, 15),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var sp = new GameObject($"SpawnPoint_{i + 1}").AddComponent<EnemySpawnPoint>();
                sp.transform.SetParent(spRoot, false);
                sp.transform.position = spots[i];
            }

            Exit(def.kind == Kind.Boss ? "Victory_Exit" : "NextFloor_Exit", new Vector3(0, 0, half - 3f),
                 def.kind == Kind.Boss ? ExitType.Victory : ExitType.NextFloor);
        }

        private static void Exit(string name, Vector3 pos, ExitType type)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var exit = go.AddComponent<LevelExit>();
            var so = new SerializedObject(exit);
            so.FindProperty("type").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- assets ----------

        private static GameObject EnsurePlayerPrefab(B.CoreAssets a)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null) return existing;

            var temp = B.BuildPlayer(a.playerMat, a.pillarMat, a.stats, a.fire, a.ice);
            foreach (var t in temp.GetComponentsInChildren<Transform>()) t.gameObject.layer = PlayerLayer;
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, PlayerPrefabPath);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        private static FloorData EnsureFloorData(string sceneName, B.CoreAssets a)
        {
            return B.LoadOrCreate<FloorData>($"{FloorDataDir}/{sceneName}.asset", f =>
            {
                switch (sceneName)
                {
                    case "Floor1":
                        f.displayName = "Goblin Warrens";
                        f.description = "Mostly goblins: fast swarms. Good floor for Nova and Chain Reaction.";
                        f.waves.Add(W("goblin scouts", 0, E(a.goblin, 4)));
                        f.waves.Add(W("goblin rush", 2, E(a.goblin, 6)));
                        f.waves.Add(W("warband", 0, E(a.goblin, 5), E(a.knight, 1)));
                        break;
                    case "Floor2":
                        f.displayName = "Haunted Cells";
                        f.description = "Mostly ghosts: they keep their distance and shoot. Keep moving.";
                        f.waves.Add(W("first ghosts", 0, E(a.ghost, 3)));
                        f.waves.Add(W("ghosts with escorts", 1, E(a.ghost, 3), E(a.goblin, 2)));
                        f.waves.Add(W("the haunting", 0, E(a.ghost, 4, 0.25f)));
                        break;
                    case "Floor3":
                        f.displayName = "Knights' Hall";
                        f.description = "Knights: slow and tough with big telegraphed swings. Dodge, then punish.";
                        f.waves.Add(W("guard post", 0, E(a.knight, 1), E(a.goblin, 3)));
                        f.waves.Add(W("patrol", 1, E(a.knight, 2), E(a.ghost, 1)));
                        f.waves.Add(W("captains", 0, E(a.knight, 2, 0.5f)));
                        break;
                    case "Floor4":
                        f.displayName = "Torture Chambers";
                        f.description = "A bit of everything.";
                        f.waves.Add(W("mixed", 0, E(a.goblin, 4), E(a.ghost, 1)));
                        f.waves.Add(W("pressure", 1, E(a.knight, 1), E(a.ghost, 2), E(a.goblin, 2)));
                        f.waves.Add(W("last stand", 0, E(a.goblin, 4, 0.3f), E(a.ghost, 2), E(a.knight, 1)));
                        break;
                    default: // Boss
                        f.displayName = "The Warden (placeholder)";
                        f.description = "Placeholder until the Warden boss exists: an elite knight with goblin guards.";
                        f.healthBonusPerFloorCleared = 0f; // the boss is always last
                        f.waves.Add(W("guards", 0, E(a.goblin, 4)));
                        f.waves.Add(W("the warden", 0, E(a.knight, 1, 1f), E(a.goblin, 3)));
                        break;
                }
            });
        }

        private static Wave W(string note, int startNextWhenRemaining, params WaveEntry[] entries)
            => new Wave { note = note, startNextWhenRemaining = startNextWhenRemaining, enemies = entries.ToList() };

        private static WaveEntry E(GameObject prefab, int count, float eliteChance = 0f)
            => new WaveEntry { enemyPrefab = prefab, count = count, eliteChance = eliteChance };

        /// <summary>Cell first (the game starts there), then the floors and the boss, then any other scenes.</summary>
        private static void SetBuildOrder()
        {
            var runPaths = Scenes.Select(s => $"{SceneDir}/{s.name}.unity").ToList();
            var others = EditorBuildSettings.scenes.Where(s => !runPaths.Contains(s.path)).ToList();
            var list = runPaths.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            list.AddRange(others);
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
