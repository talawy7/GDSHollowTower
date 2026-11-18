// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.4: modifiers.
//   1. Press Q (Chain) or E (Bounce) -> it becomes "Prepared".
//   2. The next spell uses it.
//   3. It goes on cooldown, then is "Ready" again.
// Press the key again while Prepared to cancel it.
// The HUD reads State and Cooldown01 to show ready / prepared / recharging.

using System;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Player
{
    public enum ModifierState { Ready, Prepared, Cooldown }

    public class ModifierSlot
    {
        public ModifierData Data { get; }
        public ModifierState State { get; internal set; } = ModifierState.Ready;
        public float CooldownLeft { get; internal set; }

        /// <summary>1 = just used, 0 = ready.</summary>
        public float Cooldown01 => Data == null || Data.cooldown <= 0f ? 0f : Mathf.Clamp01(CooldownLeft / Data.cooldown);
        public bool Available => Data != null;

        public ModifierSlot(ModifierData data) { Data = data; }
    }

    [RequireComponent(typeof(PlayerInputReader))]
    public class ModifierController : MonoBehaviour
    {
        [SerializeField] private ModifierData chain;
        [SerializeField] private ModifierData bounce;

        private PlayerInputReader input;

        public ModifierSlot Chain { get; private set; }
        public ModifierSlot Bounce { get; private set; }

        /// <summary>(slot) — fired whenever a modifier changes state.</summary>
        public event Action<ModifierSlot> OnStateChanged;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            Chain = new ModifierSlot(chain);
            Bounce = new ModifierSlot(bounce);
        }

        private void Update()
        {
            if (input.ChainPressed) TogglePrepared(Chain);
            if (input.BouncePressed) TogglePrepared(Bounce);

            Tick(Chain);
            Tick(Bounce);
        }

        private void TogglePrepared(ModifierSlot slot)
        {
            if (!slot.Available) return;
            if (slot.State == ModifierState.Ready) SetState(slot, ModifierState.Prepared);
            else if (slot.State == ModifierState.Prepared) SetState(slot, ModifierState.Ready);
            // On cooldown: nothing happens. (Later: play a "not ready" sound.)
        }

        private void Tick(ModifierSlot slot)
        {
            if (slot.State != ModifierState.Cooldown) return;
            slot.CooldownLeft -= Time.deltaTime;
            if (slot.CooldownLeft <= 0f)
            {
                slot.CooldownLeft = 0f;
                SetState(slot, ModifierState.Ready);
            }
        }

        private void SetState(ModifierSlot slot, ModifierState state)
        {
            slot.State = state;
            if (state == ModifierState.Cooldown) slot.CooldownLeft = slot.Data.cooldown;
            OnStateChanged?.Invoke(slot);
        }

        /// <summary>
        /// Called by SpellCaster when a spell goes out. Returns the prepared
        /// modifiers (or null) and puts them on cooldown.
        /// </summary>
        public void ConsumePrepared(out ModifierData chainUsed, out ModifierData bounceUsed)
        {
            chainUsed = Consume(Chain);
            bounceUsed = Consume(Bounce);
        }

        private ModifierData Consume(ModifierSlot slot)
        {
            if (slot.State != ModifierState.Prepared) return null;
            SetState(slot, ModifierState.Cooldown);
            return slot.Data;
        }

        /// <summary>For unlocks / rewards later.</summary>
        public void SetModifiers(ModifierData newChain, ModifierData newBounce)
        {
            chain = newChain; bounce = newBounce;
            Chain = new ModifierSlot(chain);
            Bounce = new ModifierSlot(bounce);
        }
    }
}
