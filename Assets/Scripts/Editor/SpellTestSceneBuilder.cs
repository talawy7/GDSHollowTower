// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// EDITOR ONLY. Menu: Hollow Tower > Build Spell Test Scene
// Creates the spell data assets (if missing) and a test scene with a
// capsule player, a walled room, pillars and training dummies.
// Safe to run again: existing data assets are kept, the scene is rebuilt.

using HollowTower.CameraSystem;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Player;
using HollowTower.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowTower.EditorTools
{
    public static class SpellTestSceneBuilder
    {
        private const string DataDir = "Assets/Data";
        private const string SpellDir = "Assets/Data/Spells";
        private const string MatDir = "Assets/Data/Materials";
        private const string ScenePath = "Assets/Scenes/SpellTest.unity";

        [MenuItem("Hollow Tower/Build Spell Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // New scene FIRST: NewScene unloads unused assets, which would break
            // references to data assets created earlier in this same call.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            EnsureFolder("Assets", "Data");
            EnsureFolder(DataDir, "Spells");
            EnsureFolder(DataDir, "Materials");
            EnsureFolder("Assets", "Scenes");

            // ---------- Data (placeholder numbers; GDD says tune in playtests) ----------
            var stats = LoadOrCreate<PlayerStats>($"{DataDir}/PlayerStats.asset", s => { });

            var fire = LoadOrCreate<ElementData>($"{SpellDir}/Fire.asset", e =>
            {
                e.type = ElementType.Fire; e.displayName = "Fire";
                e.color = new Color(1f, 0.45f, 0.08f);
                e.damageMultiplier = 1.4f; e.cooldownMultiplier = 1.1f;
                e.burnDamagePerSecond = 4f; e.burnDuration = 3f;
            });
            var ice = LoadOrCreate<ElementData>($"{SpellDir}/Ice.asset", e =>
            {
                e.type = ElementType.Ice; e.displayName = "Ice";
                e.color = new Color(0.55f, 0.85f, 1f);
                e.damageMultiplier = 0.8f;
                e.slowAmount = 0.5f; e.slowDuration = 2.5f;
            });
            var lightning = LoadOrCreate<ElementData>($"{SpellDir}/Lightning.asset", e =>
            {
                e.type = ElementType.Lightning; e.displayName = "Lightning";
                e.color = new Color(0.85f, 0.7f, 1f);
                e.damageMultiplier = 0.9f; e.speedMultiplier = 1.6f; e.cooldownMultiplier = 0.7f;
                e.innateChainJumps = 1;
                e.unlockedAtStart = false; e.soulInkCost = 50;
            });

            var projectile = LoadOrCreate<DeliveryData>($"{SpellDir}/Projectile.asset", d =>
            {
                d.type = DeliveryType.Projectile; d.displayName = "Projectile";
                d.baseDamage = 12f; d.castCooldown = 0.35f;
                d.speed = 20f; d.maxDistance = 25f; d.radius = 0.2f; d.piercesEnemies = false;
            });
            var orb = LoadOrCreate<DeliveryData>($"{SpellDir}/Orb.asset", d =>
            {
                d.type = DeliveryType.Orb; d.displayName = "Orb";
                d.baseDamage = 10f; d.castCooldown = 0.8f;
                d.speed = 7f; d.maxDistance = 15f; d.radius = 0.6f; d.piercesEnemies = true;
                d.unlockedAtStart = false; d.soulInkCost = 40;
            });
            var nova = LoadOrCreate<DeliveryData>($"{SpellDir}/Nova.asset", d =>
            {
                d.type = DeliveryType.Nova; d.displayName = "Nova";
                d.baseDamage = 15f; d.castCooldown = 1.2f; d.novaRadius = 4f;
            });

            var chain = LoadOrCreate<ModifierData>($"{SpellDir}/Chain.asset", m =>
            {
                m.type = ModifierType.Chain; m.displayName = "Chain";
                m.cooldown = 5f; m.chainJumps = 3; m.chainRange = 6f; m.chainDamageFalloff = 0.75f;
            });
            var bounce = LoadOrCreate<ModifierData>($"{SpellDir}/Bounce.asset", m =>
            {
                m.type = ModifierType.Bounce; m.displayName = "Bounce";
                m.cooldown = 5f; m.bounces = 2;
                m.unlockedAtStart = false; m.soulInkCost = 60;
            });

            // ---------- Materials (dull greys/blues per GDD 8) ----------
            var floorMat  = LoadOrCreateMat("Floor",  new Color(0.22f, 0.22f, 0.25f));
            var wallMat   = LoadOrCreateMat("Wall",   new Color(0.16f, 0.17f, 0.22f));
            var pillarMat = LoadOrCreateMat("Pillar", new Color(0.30f, 0.27f, 0.24f));
            var playerMat = LoadOrCreateMat("Player", new Color(0.35f, 0.2f, 0.5f));
            var dummyMat  = LoadOrCreateMat("Dummy",  new Color(0.45f, 0.4f, 0.32f));

            // ---------- Scene ----------
            // (created before this point, see above)

            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.intensity = 0.8f;
                sun.color = new Color(0.75f, 0.8f, 1f);
                sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            }

            var room = new GameObject("Room").transform;
            Block("Floor", room, new Vector3(0, -0.5f, 0), new Vector3(30, 1, 30), floorMat);
            Block("Wall_N", room, new Vector3(0, 1.5f, 15.5f), new Vector3(32, 4, 1), wallMat);
            Block("Wall_S", room, new Vector3(0, 1.5f, -15.5f), new Vector3(32, 4, 1), wallMat);
            Block("Wall_E", room, new Vector3(15.5f, 1.5f, 0), new Vector3(1, 4, 30), wallMat);
            Block("Wall_W", room, new Vector3(-15.5f, 1.5f, 0), new Vector3(1, 4, 30), wallMat);
            Block("Pillar_1", room, new Vector3(-7, 1.5f, 6), new Vector3(1.5f, 4, 1.5f), pillarMat);
            Block("Pillar_2", room, new Vector3(7, 1.5f, 6), new Vector3(1.5f, 4, 1.5f), pillarMat);
            Block("Pillar_3", room, new Vector3(-7, 1.5f, -6), new Vector3(1.5f, 4, 1.5f), pillarMat);
            Block("Pillar_4", room, new Vector3(7, 1.5f, -6), new Vector3(1.5f, 4, 1.5f), pillarMat);

            // Player: capsule placeholder for the witch.
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0, 1f, -8f);
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.GetComponent<MeshRenderer>().sharedMaterial = playerMat;

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube); // shows which way she faces
            nose.name = "FacingMarker";
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0, 0.4f, 0.5f);
            nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.3f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = pillarMat;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;
            player.AddComponent<PlayerInputReader>();
            player.AddComponent<Health>();
            var pc = player.AddComponent<PlayerController>();
            var loadout = player.AddComponent<SpellLoadout>();
            var mods = player.AddComponent<ModifierController>();
            player.AddComponent<SpellCaster>();

            SetRef(pc, "stats", stats);
            SetRef(mods, "chain", chain);
            SetRef(mods, "bounce", bounce);

            // Starting test loadout: one slot per delivery + a lightning one.
            var so = new SerializedObject(loadout);
            var slots = so.FindProperty("slots");
            slots.arraySize = SpellLoadout.SlotCount;
            SetSlot(slots, 0, fire, projectile);
            SetSlot(slots, 1, ice, orb);
            SetSlot(slots, 2, lightning, nova);
            SetSlot(slots, 3, lightning, projectile);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Camera
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                var tdc = cam.gameObject.AddComponent<TopDownCamera>();
                SetRef(tdc, "target", player.transform);
            }

            // Dummies: a tight group (for Chain/Nova/Orb), singles, and one walker (for Ice slow).
            var dummies = new GameObject("Dummies").transform;
            Dummy("Dummy_Group_1", dummies, new Vector3(-1.5f, 1, 4), dummyMat, false);
            Dummy("Dummy_Group_2", dummies, new Vector3(0f, 1, 5), dummyMat, false);
            Dummy("Dummy_Group_3", dummies, new Vector3(1.5f, 1, 4), dummyMat, false);
            Dummy("Dummy_Group_4", dummies, new Vector3(0f, 1, 2.8f), dummyMat, false);
            Dummy("Dummy_Left", dummies, new Vector3(-10, 1, 0), dummyMat, false);
            Dummy("Dummy_Right", dummies, new Vector3(10, 1, 0), dummyMat, false);
            Dummy("Dummy_Walker", dummies, new Vector3(0, 1, 11), dummyMat, true);

            // HUD
            var hudGo = new GameObject("SpellTestHUD");
            var hud = hudGo.AddComponent<SpellTestHUD>();
            var hso = new SerializedObject(hud);
            hso.FindProperty("player").objectReferenceValue = player;
            SetArray(hso.FindProperty("allElements"), fire, ice, lightning);
            SetArray(hso.FindProperty("allDeliveries"), projectile, orb, nova);
            hso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            Debug.Log("[Hollow Tower] Spell test scene built at " + ScenePath + ". Press Play.");
        }

        // ---------- helpers ----------

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        private static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material LoadOrCreateMat(string name, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
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

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[Hollow Tower] Field '{field}' not found on {target.GetType().Name}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSlot(SerializedProperty slots, int i, ElementData e, DeliveryData d)
        {
            var slot = slots.GetArrayElementAtIndex(i);
            slot.FindPropertyRelative("element").objectReferenceValue = e;
            slot.FindPropertyRelative("delivery").objectReferenceValue = d;
        }

        private static void SetArray(SerializedProperty arr, params Object[] items)
        {
            arr.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
