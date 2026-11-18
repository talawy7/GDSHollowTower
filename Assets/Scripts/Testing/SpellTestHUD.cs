// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// TEST ONLY. Quick on-screen readout for the spell test scene:
// health, 4 slots, modifier states (ready / prepared / recharging), dodge.
// Z / X change the selected slot's element / delivery so all 9 combos can be
// tried without the workbench (the real workbench comes in week 5).

using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HollowTower.Testing
{
    public class SpellTestHUD : MonoBehaviour
    {
        [SerializeField] private GameObject player;
        [SerializeField] private ElementData[] allElements;
        [SerializeField] private DeliveryData[] allDeliveries;

        private Health health;
        private PlayerController controller;
        private SpellLoadout loadout;
        private ModifierController modifiers;
        private SpellCaster caster;

        private string lastCast = "-";
        private GUIStyle box, label, small;

        private void Start()
        {
            if (player == null) return;
            health = player.GetComponent<Health>();
            controller = player.GetComponent<PlayerController>();
            loadout = player.GetComponent<SpellLoadout>();
            modifiers = player.GetComponent<ModifierController>();
            caster = player.GetComponent<SpellCaster>();
            if (caster != null) caster.OnCast += p => lastCast = p.FullName;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || loadout == null) return;
            if (kb.zKey.wasPressedThisFrame) CycleElement();
            if (kb.xKey.wasPressedThisFrame) CycleDelivery();
        }

        private void CycleElement()
        {
            if (allElements == null || allElements.Length == 0) return;
            var slot = loadout.Current;
            int i = System.Array.IndexOf(allElements, slot.element);
            loadout.SetSlot(loadout.CurrentIndex, allElements[(i + 1) % allElements.Length], slot.delivery);
        }

        private void CycleDelivery()
        {
            if (allDeliveries == null || allDeliveries.Length == 0) return;
            var slot = loadout.Current;
            int i = System.Array.IndexOf(allDeliveries, slot.delivery);
            loadout.SetSlot(loadout.CurrentIndex, slot.element, allDeliveries[(i + 1) % allDeliveries.Length]);
        }

        private void OnGUI()
        {
            if (player == null || loadout == null) return;

            box ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, padding = new RectOffset(10, 10, 8, 8) };
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            small ??= new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };

            GUILayout.BeginArea(new Rect(12, 12, 330, 340), box);

            if (health != null)
                GUILayout.Label($"<b>Health</b>  {health.Current:0} / {health.Max:0}", label);

            GUILayout.Space(4);
            GUILayout.Label("<b>Spells</b>  (1-4 / wheel)", label);
            for (int i = 0; i < loadout.Slots.Count; i++)
            {
                var s = loadout.Slots[i];
                string color = s.IsValid ? ColorUtility.ToHtmlStringRGB(s.element.color) : "888888";
                string arrow = i == loadout.CurrentIndex ? "<b>▶</b> " : "    ";
                GUILayout.Label($"{arrow}{i + 1}. <color=#{color}>{s.DisplayName}</color>", label);
            }

            GUILayout.Space(4);
            if (modifiers != null)
            {
                GUILayout.Label($"<b>Q</b> Chain:  {Describe(modifiers.Chain)}", label);
                GUILayout.Label($"<b>E</b> Bounce: {Describe(modifiers.Bounce)}", label);
            }
            if (controller != null)
            {
                string dodge = controller.DodgeCooldown01 <= 0f ? "<color=#7CFC00>ready</color>" : "<color=#FF8C00>recharging</color>";
                GUILayout.Label($"<b>Space</b> Dodge: {dodge}", label);
            }

            GUILayout.Space(4);
            GUILayout.Label($"Last cast: {lastCast}", small);
            GUILayout.Label("<color=#AAAAAA>Z = change element   X = change delivery</color>", small);

            GUILayout.EndArea();
        }

        private static string Describe(ModifierSlot m)
        {
            if (m == null || !m.Available) return "<color=#888888>locked</color>";
            switch (m.State)
            {
                case ModifierState.Ready:    return "<color=#7CFC00>READY</color>";
                case ModifierState.Prepared: return "<color=#FFD700>PREPARED</color>";
                default:                     return $"<color=#FF8C00>recharging {m.CooldownLeft:0.0}s</color>";
            }
        }
    }
}
