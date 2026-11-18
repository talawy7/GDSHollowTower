// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// EDITOR ONLY. Menu: Hollow Tower > Build Combat Test Scene
//   1. Removes the old Week 2 spell files (Bounce/modifier system, 4 slots).
//      Your scenes (including the old SpellTest scene) are never deleted.
//   2. Creates the data assets with the GDD numbers (only if they don't exist yet,
//      so your own tuning is never overwritten).
//   3. Creates enemy prefabs from the Synty models (only if they don't exist yet).
//   4. Builds Assets/Scenes/CombatTest.unity: a room with a runtime NavMesh,
//      the capsule player, training dummies and a group of enemies.

using System.Linq;
using HollowTower.CameraSystem;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Enemies;
using HollowTower.Player;
using HollowTower.Rooms;
using HollowTower.Testing;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace HollowTower.EditorTools
{
    public static class SpellTestSceneBuilder
    {
        private const string DataDir = "Assets/Data";
        private const string ElementDir = "Assets/Data/Elements";
        private const string EnemyDataDir = "Assets/Data/Enemies";
        private const string MatDir = "Assets/Data/Materials";
        private const string PrefabDir = "Assets/Prefabs/Enemies";
        private const string ScenePath = "Assets/Scenes/CombatTest.unity";

        private const string SyntyChars = "Assets/levelAssets/PolygonDungeon/Prefabs/Characters";
        private const string GoblinModel = SyntyChars + "/GoblinMonsters/SM_Chr_Goblin_Male_01.prefab";
        private const string KnightModel = SyntyChars + "/KnightMonster/SM_Chr_Skeleton_Knight_01.prefab";
        private const string GhostModel  = SyntyChars + "/SM_Chr_Ghost_01.prefab";

        private static readonly string[] ObsoletePaths =
        {
            "Assets/Data/Spells",
            "Assets/Scripts/Data/DeliveryData.cs",
            "Assets/Scripts/Data/ModifierData.cs",
            "Assets/Scripts/Spells/SpellSlot.cs",
            "Assets/Scripts/Spells/SpellNova.cs",
            "Assets/Scripts/Player/ModifierController.cs",
        };

        [MenuItem("Hollow Tower/Build Combat Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // New scene FIRST: NewScene unloads unused assets, which would break
            // references to data assets created earlier in this same call.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            foreach (var p in ObsoletePaths)
                if (AssetDatabase.LoadMainAssetAtPath(p) != null || AssetDatabase.IsValidFolder(p))
                    AssetDatabase.DeleteAsset(p);

            var a = EnsureCoreAssets();
            var stats = a.stats; var fire = a.fire; var ice = a.ice; var lightning = a.lightning;
            var goblinPrefab = a.goblin; var knightPrefab = a.knight; var ghostPrefab = a.ghost;
            var floorMat = a.floorMat; var wallMat = a.wallMat; var pillarMat = a.pillarMat;
            var playerMat = a.playerMat; var dummyMat = a.dummyMat;

            // ---------- scene ----------
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.intensity = 0.8f;
                sun.color = new Color(0.75f, 0.8f, 1f);
                sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }

            var room = new GameObject("Room");
            var surface = room.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            room.AddComponent<RuntimeNavMeshBaker>();
            var rt = room.transform;
            Block("Floor", rt, new Vector3(0, -0.5f, 0), new Vector3(36, 1, 36), floorMat);
            Block("Wall_N", rt, new Vector3(0, 1.5f, 18.5f), new Vector3(38, 4, 1), wallMat);
            Block("Wall_S", rt, new Vector3(0, 1.5f, -18.5f), new Vector3(38, 4, 1), wallMat);
            Block("Wall_E", rt, new Vector3(18.5f, 1.5f, 0), new Vector3(1, 4, 36), wallMat);
            Block("Wall_W", rt, new Vector3(-18.5f, 1.5f, 0), new Vector3(1, 4, 36), wallMat);
            Block("Pillar_1", rt, new Vector3(-6, 1.5f, 4), new Vector3(1.5f, 4, 1.5f), pillarMat);
            Block("Pillar_2", rt, new Vector3(6, 1.5f, 4), new Vector3(1.5f, 4, 1.5f), pillarMat);
            Block("Pillar_3", rt, new Vector3(-6, 1.5f, -6), new Vector3(1.5f, 4, 1.5f), pillarMat);
            Block("Pillar_4", rt, new Vector3(6, 1.5f, -6), new Vector3(1.5f, 4, 1.5f), pillarMat);

            var player = BuildPlayer(playerMat, pillarMat, stats, fire, ice);

            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                var tdc = cam.gameObject.AddComponent<TopDownCamera>();
                SetRef(tdc, "target", player.transform);
            }

            // Training dummies (left side) for safe spell testing.
            var dummies = new GameObject("Dummies").transform;
            Dummy("Dummy_1", dummies, new Vector3(-12f, 1, -10f), dummyMat, false);
            Dummy("Dummy_2", dummies, new Vector3(-13.5f, 1, -8.5f), dummyMat, false);
            Dummy("Dummy_3", dummies, new Vector3(-10.5f, 1, -8.5f), dummyMat, false);
            Dummy("Dummy_Walker", dummies, new Vector3(-12f, 1, -4f), dummyMat, true);

            // Enemies (far side). They notice you when you get close.
            var enemies = new GameObject("Enemies").transform;
            Enemy(goblinPrefab, enemies, new Vector3(7f, 0, 10f), false);
            Enemy(goblinPrefab, enemies, new Vector3(8.5f, 0, 11f), false);
            Enemy(goblinPrefab, enemies, new Vector3(10f, 0, 10f), false);
            Enemy(goblinPrefab, enemies, new Vector3(8.5f, 0, 9f), false);
            Enemy(goblinPrefab, enemies, new Vector3(12f, 0, 13f), true);
            Enemy(knightPrefab, enemies, new Vector3(0f, 0, 13f), false);
            Enemy(ghostPrefab, enemies, new Vector3(-8f, 0, 14f), false);
            Enemy(ghostPrefab, enemies, new Vector3(13f, 0, 2f), false);

            var hud = new GameObject("TestHUD").AddComponent<SpellTestHUD>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("player").objectReferenceValue = player;
            SetArray(hso.FindProperty("allElements"), fire, ice, lightning);
            hso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("[Hollow Tower] Combat test scene built at " + ScenePath + ". Press Play.");
        }


        /// <summary>Everything the test and run scenes need. Creates only what's missing.</summary>
        internal class CoreAssets
        {
            public PlayerStats stats;
            public ElementData fire, ice, lightning;
            public GameObject goblin, knight, ghost;
            public Material floorMat, wallMat, pillarMat, playerMat, dummyMat;
        }

        internal static CoreAssets EnsureCoreAssets()
        {
            var a = new CoreAssets();
            EnsureFolder("Assets", "Data");
            EnsureFolder(DataDir, "Elements");
            EnsureFolder(DataDir, "Enemies");
            EnsureFolder(DataDir, "Materials");
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Enemies");
            EnsureFolder("Assets", "Scenes");

            // ---------- data ----------
            a.stats = LoadOrCreate<PlayerStats>($"{DataDir}/PlayerStats.asset", s => { });
            LoadOrCreate<CombatSettings>("Assets/Resources/CombatSettings.asset", s => { });
            a.fire = LoadOrCreate<ElementData>($"{ElementDir}/Fire.asset", SetupFire);
            a.ice = LoadOrCreate<ElementData>($"{ElementDir}/Ice.asset", SetupIce);
            a.lightning = LoadOrCreate<ElementData>($"{ElementDir}/Lightning.asset", SetupLightning);

            var goblinData = LoadOrCreate<EnemyData>($"{EnemyDataDir}/Goblin.asset", d =>
            {
                d.id = "Goblin"; d.displayName = "Goblin";
                d.maxHealth = 20f; d.moveSpeed = 4.2f; d.detectionRange = 14f;
                d.attackDamage = 5f; d.attackRange = 1.4f;
                d.attackWindup = 0.35f; d.attackRecovery = 0.4f; d.attackCooldown = 0.9f;
                d.soulInk = 1;
            });
            var knightData = LoadOrCreate<EnemyData>($"{EnemyDataDir}/Knight.asset", d =>
            {
                d.id = "Knight"; d.displayName = "Knight";
                d.maxHealth = 90f; d.moveSpeed = 2.2f; d.turnSpeed = 240f; d.detectionRange = 14f;
                d.attackDamage = 20f; d.attackRange = 2.4f; d.attackAreaRadius = 1.7f;
                d.attackWindup = 0.9f; d.attackRecovery = 0.8f; d.attackCooldown = 1.6f;
                d.soulInk = 3;
            });
            var ghostData = LoadOrCreate<EnemyData>($"{EnemyDataDir}/Ghost.asset", d =>
            {
                d.id = "Ghost"; d.displayName = "Ghost";
                d.maxHealth = 25f; d.moveSpeed = 3f; d.detectionRange = 16f;
                d.attackDamage = 8f; d.attackRange = 10f;
                d.attackWindup = 0.6f; d.attackRecovery = 0.4f; d.attackCooldown = 2.2f;
                d.preferredDistance = 8f; d.retreatDistance = 4.5f;
                d.projectileSpeed = 7f; d.projectileRadius = 0.3f;
                d.hoverHeight = 0.5f; d.soulInk = 2;
            });

            a.floorMat = LoadOrCreateMat("Floor",  new Color(0.22f, 0.22f, 0.25f));
            a.wallMat = LoadOrCreateMat("Wall",   new Color(0.16f, 0.17f, 0.22f));
            a.pillarMat = LoadOrCreateMat("Pillar", new Color(0.30f, 0.27f, 0.24f));
            a.playerMat = LoadOrCreateMat("Player", new Color(0.35f, 0.2f, 0.5f));
            a.dummyMat = LoadOrCreateMat("Dummy",  new Color(0.45f, 0.4f, 0.32f));

            a.goblin = LoadOrCreateEnemyPrefab<GoblinEnemy>("Goblin", goblinData, GoblinModel, 1.8f, 0.4f, 0f);
            a.knight = LoadOrCreateEnemyPrefab<KnightEnemy>("Knight", knightData, KnightModel, 1.9f, 0.5f, 0f);
            a.ghost = LoadOrCreateEnemyPrefab<GhostEnemy>("Ghost", ghostData, GhostModel, 1.6f, 0.4f, 0.4f);

            return a;
        }

        // ---------- GDD 5.2 numbers ----------

        private static void SetupFire(ElementData e)
        {
            e.type = ElementType.Fire; e.displayName = "Fire";
            e.color = new Color(1f, 0.45f, 0.08f);
            e.playstyle = "High damage and damage over time.";
            e.normalFireInterval = 0.25f;
            e.normalAttack = Ball("Normal Attack", 3f, 20f, 0.18f, 22f);
            e.normalAttack.hit.burnDps = 1f; e.normalAttack.hit.burnDuration = 3f;

            e.projectile = Ball("Projectile", 5f, 26f, 0.2f, 22f);
            e.projectile.shots = 3;
            e.projectile.hit.burnDps = 1f; e.projectile.hit.burnDuration = 3f;

            e.orb = Ball("Orb", 20f, 9f, 0.7f, 16f);
            e.orb.piercesEnemies = true;
            e.orb.hit.burnDps = 2f; e.orb.hit.burnDuration = 3f;

            e.nova = Ball("Nova", 6f, 16f, 0.35f, 14f);
            e.nova.explosionRadius = 3.5f;
            e.nova.hit.burnDps = 3f; e.nova.hit.burnDuration = 4f;

            e.chainReaction = new ChainReactionData { chainDuration = 5f, projectileBonusDamage = 6f, orbNovaBurnMultiplier = 2f };
            e.chainReaction.effect.burnDps = 2f; e.chainReaction.effect.burnDuration = 5f;
        }

        private static void SetupIce(ElementData e)
        {
            e.type = ElementType.Ice; e.displayName = "Ice";
            e.color = new Color(0.55f, 0.85f, 1f);
            e.playstyle = "Crowd control, slowing, and weakening enemies.";
            e.normalFireInterval = 0.3f;
            e.normalAttack = Ball("Normal Attack", 4f, 20f, 0.18f, 22f);
            e.normalAttack.hit.slowAmount = 0.4f; e.normalAttack.hit.slowDuration = 2f;

            e.projectile = Ball("Projectile", 4f, 34f, 0.18f, 24f);
            e.projectile.shots = 4; e.projectile.timeBetweenShots = 0.06f;
            e.projectile.hit.slowAmount = 0.4f; e.projectile.hit.slowDuration = 2f;

            e.orb = Ball("Orb", 8f, 8f, 0.7f, 16f);
            e.orb.piercesEnemies = true;
            e.orb.hit.freezeDuration = 3f;

            e.nova = Ball("Nova", 10f, 16f, 0.35f, 14f);
            e.nova.explosionRadius = 3.5f;
            e.nova.hit.slowAmount = 0.5f; e.nova.hit.slowDuration = 3f;
            e.nova.hit.weakenAmount = 0.25f; e.nova.hit.weakenDuration = 4f;

            e.chainReaction = new ChainReactionData { chainDuration = 6f };
            e.chainReaction.effect.freezeDuration = 6f;
        }

        private static void SetupLightning(ElementData e)
        {
            e.type = ElementType.Lightning; e.displayName = "Lightning";
            e.color = new Color(0.85f, 0.7f, 1f);
            e.playstyle = "Fast attacks, high mobility, and multi-target damage.";
            e.normalFireInterval = 0.18f;
            e.normalAttack = Ball("Normal Attack", 3f, 30f, 0.15f, 24f);

            e.projectile = Ball("Projectile", 4f, 32f, 0.18f, 24f);
            e.projectile.shots = 3;

            e.orb = Ball("Orb", 15f, 9f, 0.7f, 16f);
            e.orb.piercesEnemies = true;
            e.orb.hit.stunDuration = 2f;

            e.nova = Ball("Nova", 0f, 16f, 0.35f, 14f);
            e.nova.explosionRadius = 3.5f;
            e.nova.hit.shockDps = 3f; e.nova.hit.shockDuration = 4f;

            e.chainReaction = new ChainReactionData { chainDuration = 10f };
            e.chainReaction.effect.shockDps = 2f; e.chainReaction.effect.shockDuration = 10f;
        }

        private static AttackData Ball(string name, float damage, float speed, float radius, float range)
        {
            var a = new AttackData { displayName = name, speed = speed, radius = radius, maxDistance = range };
            a.hit.damage = damage;
            return a;
        }

        // ---------- player ----------

        internal static GameObject BuildPlayer(Material playerMat, Material markerMat, PlayerStats stats,
                                              ElementData slot1, ElementData slot2)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0, 1f, -12f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.GetComponent<MeshRenderer>().sharedMaterial = playerMat;

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube); // shows which way she faces
            nose.name = "FacingMarker";
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0, 0.4f, 0.5f);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.3f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = markerMat;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<Health>();
            var pc = player.AddComponent<PlayerController>();
            var loadout = player.AddComponent<SpellLoadout>();
            player.AddComponent<SpellCaster>();
            player.AddComponent<DeliveryAbility>();
            player.AddComponent<ChainReactionAbility>();

            SetRef(pc, "stats", stats);
            var so = new SerializedObject(loadout);
            SetArray(so.FindProperty("slots"), slot1, slot2);
            so.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        // ---------- enemies ----------

        private static GameObject LoadOrCreateEnemyPrefab<T>(string name, EnemyData data, string modelPath,
                                                            float height, float radius, float colliderLift)
            where T : EnemyBase
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(name);
            var col = root.AddComponent<CapsuleCollider>();
            col.height = height; col.radius = radius; col.center = new Vector3(0, height / 2f + colliderLift, 0);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = radius; agent.height = height; agent.baseOffset = 0f;
            agent.acceleration = 30f;

            var health = root.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("team").enumValueIndex = (int)Team.Enemy;
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Visual model: the Synty character, or a capsule if it can't be found.
            GameObject model;
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (src != null) model = (GameObject)PrefabUtility.InstantiatePrefab(src);
            else
            {
                Debug.LogWarning($"[Hollow Tower] Model not found at {modelPath}, using a capsule for {name}.");
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(model.GetComponent<Collider>());
                model.transform.localPosition = new Vector3(0, 1f, 0);
            }
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            var animator = root.AddComponent<EnemyAnimator>();
            SetRef(animator, "model", model.transform);

            var enemy = root.AddComponent<T>();
            SetRef(enemy, "data", data);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Enemy(GameObject prefab, Transform parent, Vector3 pos, bool elite)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 180f, 0); // face the player's side
            if (elite)
            {
                go.name += " (Elite)";
                var e = go.GetComponent<EnemyBase>();
                var so = new SerializedObject(e);
                so.FindProperty("isElite").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void Dummy(string name, Transform parent, Vector3 pos, Material mat, bool walker)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var health = go.AddComponent<Health>();
            var hso = new SerializedObject(health);
            hso.FindProperty("maxHealth").floatValue = 150f;
            hso.FindProperty("team").enumValueIndex = (int)Team.Enemy;
            hso.ApplyModifiedPropertiesWithoutUndo();

            var dummy = go.AddComponent<TrainingDummy>();
            var dso = new SerializedObject(dummy);
            dso.FindProperty("moveSideToSide").boolValue = walker;
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- helpers ----------

        internal static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        internal static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        internal static Material LoadOrCreateMat(string name, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        internal static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        internal static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Hollow Tower] Field '{field}' not found on {target.GetType().Name}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(SerializedProperty arr, params Object[] items)
        {
            arr.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        internal static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
