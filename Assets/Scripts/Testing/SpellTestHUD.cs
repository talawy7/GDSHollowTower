// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// TEST ONLY. On-screen readout for the combat test scene:
// health, the 2 element skills, delivery style (E) + cooldown, Q charge,
// Soul Ink from kills, and the controls.
// Test keys:  Z = change the current skill's element,  T = enemy AI on/off,
//             C = fill Q,  R = restart after dying.
// The real HUD (week 5) will replace this.

using System.Collections.Generic;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Enemies;
using HollowTower.Player;
using HollowTower.Spells;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace HollowTower.Testing
{
    public class SpellTestHUD : MonoBehaviour
    {
        [SerializeField] private GameObject player;
        [SerializeField] private ElementData[] allElements;

        private Health health;
        private PlayerController controller;
        private SpellLoadout loadout;
        private DeliveryAbility delivery;
        private ChainReactionAbility chain;
        private SpellCaster caster;

        private string lastCast = "-";
        private string lastKiller = "";
        private int soulInk;
        private GUIStyle box, label, small, big;

        private static readonly Dictionary<ReactionType, (string, Color)> ReactionLabels = new Dictionary<ReactionType, (string, Color)>
        {
            { ReactionType.Shatter,       ("SHATTER!",        new Color(0.75f, 0.95f, 1f)) },
            { ReactionType.Overload,      ("OVERLOAD!",       new Color(1f, 0.6f, 0.35f)) },
            { ReactionType.ChargedFreeze, ("CHARGED FREEZE!", new Color(0.8f, 0.7f, 1f)) },
        };

        private void Start()
        {
            if (player != null)
            {
                health = player.GetComponent<Health>();
                controller = player.GetComponent<PlayerController>();
                loadout = player.GetComponent<SpellLoadout>();
                delivery = player.GetComponent<DeliveryAbility>();
                chain = player.GetComponent<ChainReactionAbility>();
                caster = player.GetComponent<SpellCaster>();
                if (caster != null) caster.OnCast += p => lastCast = p.FullName;
                if (health != null) health.OnDied += k => lastKiller = k != null ? k.name : "something";
            }

            // Damage numbers over every enemy.
            foreach (var e in FindObjectsByType<EnemyBase>())
            {
                var h = e.GetComponent<Health>();
                var t = e.transform;
                h.OnDamaged += (amount, src) =>
                {
                    if (t != null) DamageNumbers.Show(t.position + Vector3.up * 2.2f, amount, Color.white);
                };
            }

            ElementalReactions.OnReaction += ShowReaction;
            EnemyBase.OnAnyEnemyKilled += HandleKill;
            EnemyBase.AIEnabled = true;
        }

        private void OnDestroy()
        {
            ElementalReactions.OnReaction -= ShowReaction;
            EnemyBase.OnAnyEnemyKilled -= HandleKill;
        }

        private void ShowReaction(ReactionType type, Vector3 pos)
        {
            var (text, color) = ReactionLabels[type];
            DamageNumbers.ShowText(pos, text, color);
        }

        private void HandleKill(EnemyBase enemy, GameObject killer)
        {
            if (enemy.Data != null) soulInk += enemy.Data.soulInk;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.zKey.wasPressedThisFrame) CycleElement();
            if (kb.tKey.wasPressedThisFrame) EnemyBase.AIEnabled = !EnemyBase.AIEnabled;
            if (kb.cKey.wasPressedThisFrame && chain != null) chain.FillCharge();
            if (kb.rKey.wasPressedThisFrame && health != null && health.IsDead)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // scene must be in Build Settings
        }

        private void CycleElement()
        {
            if (loadout == null || allElements == null || allElements.Length == 0) return;
            int i = System.Array.IndexOf(allElements, loadout.Current);
            loadout.SetSlot(loadout.CurrentIndex, allElements[(i + 1) % allElements.Length]);
        }

        // Set the text color explicitly: the default skin color isn't always white.
        private static GUIStyle WhiteLabel(int size)
        {
            // Built from scratch (not copied from GUI.skin) so the color can't be inherited wrongly.
            var st = new GUIStyle
            {
                fontSize = size,
                richText = true,
                wordWrap = false,
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(2, 2, 1, 1),
            };
            st.normal.textColor = Color.white;
            return st;
        }

        // Uncolored IMGUI text sometimes renders black in this project, so every line
        // is wrapped in an explicit white color tag (inner color tags still win).
        private static void Line(string text, GUIStyle style) => GUILayout.Label($"<color=#F2F2F2>{text}</color>", style);

        private void OnGUI()
        {
            if (player == null || loadout == null) return;

            GUI.depth = -10; // draw on top of the dummies' health bars
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;

            box ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            label ??= WhiteLabel(14);
            small ??= WhiteLabel(11);
            big ??= new GUIStyle(WhiteLabel(32)) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            var panel = new Rect(10, 10, 310, 255);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(panel.x + 8, panel.y + 6, panel.width - 16, panel.height - 12));

            if (health != null)
                Line($"<b>Health</b>  {health.Current:0} / {health.Max:0}      <b>Soul Ink</b> {soulInk}", label);

            GUILayout.Space(2);
            Line("<b>Skills</b>  (scroll / 1-2)", label);
            for (int i = 0; i < loadout.Slots.Count; i++)
            {
                var e = loadout.Slots[i];
                string name = e != null ? $"<color=#{ColorUtility.ToHtmlStringRGB(e.color)}>{e.displayName}</color>" : "<color=#888888>Empty</color>";
                string arrow = i == loadout.CurrentIndex ? "<b>▶</b> " : "    ";
                Line($"{arrow}{i + 1}. {name}", label);
            }

            GUILayout.Space(2);
            if (delivery != null)
            {
                string state = delivery.Ready ? "<color=#7CFC00>READY</color>" : $"<color=#FF8C00>{delivery.CooldownLeft:0.0}s</color>";
                Line($"<b>E</b> Delivery: <b>{delivery.Selected}</b>  {state}", label);
                if (delivery.IsChoosing)
                {
                    string Opt(DeliveryType d) => d == delivery.Selected ? $"<color=#FFD700><b>[{d}]</b></color>" : $"<color=#AAAAAA>{d}</color>";
                    Line($"     {Opt(DeliveryType.Projectile)}  {Opt(DeliveryType.Orb)}  {Opt(DeliveryType.Nova)}", label);
                }
            }
            if (chain != null)
            {
                int pct = Mathf.RoundToInt(chain.Charge01 * 100f);
                string q = chain.Ready ? "<color=#FFD700>READY</color>" : $"<color=#AAAAAA>{pct}%</color>";
                Line($"<b>Q</b> Chain Reaction: {q}", label);
            }
            if (controller != null)
            {
                string dodge = controller.DodgeCooldown01 <= 0f ? "<color=#7CFC00>ready</color>" : "<color=#FF8C00>recharging</color>";
                Line($"<b>Space</b> Dodge: {dodge}", label);
            }

            GUILayout.Space(2);
            Line($"Last cast: {lastCast}", small);
            string ai = EnemyBase.AIEnabled ? "<color=#7CFC00>on</color>" : "<color=#FF6060>off</color>";
            Line($"<color=#AAAAAA>Z = change element   C = fill Q   T = enemy AI ({ai}<color=#AAAAAA>)</color></color>", small);
            Line("<color=#AAAAAA>Hold E + scroll = pick delivery, release E = cast</color>", small);
            GUILayout.EndArea();

            if (health != null && health.IsDead)
            {
                var r = new Rect(0, Screen.height * 0.35f, Screen.width, 80);
                GUI.Label(r, $"<color=#FF5050>YOU DIED</color>\n<size=18><color=#F2F2F2>killed by {lastKiller} — press R to restart</color></size>", big);
            }
        }
    }
}
