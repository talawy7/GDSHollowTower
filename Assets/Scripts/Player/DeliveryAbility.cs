// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 5.1 Delivery Style (E):
//   Hold E and scroll to pick Projectile / Orb / Nova.
//   Let go of E to cast that stronger version of the current element.
//   Then E has a 10 second cooldown (CombatSettings.deliveryCooldown).
// You can still change the selection while E is cooling down.

using System;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(SpellCaster))]
    public class DeliveryAbility : MonoBehaviour
    {
        private static readonly DeliveryType[] Order = { DeliveryType.Projectile, DeliveryType.Orb, DeliveryType.Nova };

        private PlayerInputReader input;
        private SpellCaster caster;
        private int selectedIndex;
        private float cooldownLeft;

        public DeliveryType Selected => Order[selectedIndex];
        public bool IsChoosing => input != null && input.DeliveryHeld;
        public bool Ready => cooldownLeft <= 0f;
        public float CooldownLeft => Mathf.Max(0f, cooldownLeft);
        /// <summary>1 = just used, 0 = ready. For the HUD.</summary>
        public float Cooldown01 => Mathf.Clamp01(cooldownLeft / Mathf.Max(0.01f, CombatSettings.Current.deliveryCooldown));

        public event Action<DeliveryType> OnSelectionChanged;
        public event Action<DeliveryType> OnUsed;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            caster = GetComponent<SpellCaster>();
        }

        private void Update()
        {
            cooldownLeft -= Time.deltaTime;

            if (input.DeliveryHeld)
            {
                int s = input.Scroll;
                if (s != 0)
                {
                    selectedIndex = (selectedIndex - s + Order.Length) % Order.Length;
                    OnSelectionChanged?.Invoke(Selected);
                }
            }

            if (input.DeliveryReleased && Ready && caster.CanCast)
            {
                if (caster.CastDelivery(Selected))
                {
                    cooldownLeft = CombatSettings.Current.deliveryCooldown;
                    OnUsed?.Invoke(Selected);
                }
            }
        }

        /// <summary>For rewards / testing.</summary>
        public void ResetCooldown() => cooldownLeft = 0f;
    }
}
