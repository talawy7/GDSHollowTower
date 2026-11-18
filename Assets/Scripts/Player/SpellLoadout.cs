// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.5: the witch has 4 spell slots. 1-4 or the mouse wheel picks one.
// The workbench (week 5) will call SetSlot() to change them.

using System;
using System.Collections.Generic;
using HollowTower.Data;
using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    public class SpellLoadout : MonoBehaviour
    {
        public const int SlotCount = 4;

        [SerializeField] private SpellSlot[] slots = new SpellSlot[SlotCount];

        private PlayerInputReader input;
        private int current;

        public int CurrentIndex => current;
        public SpellSlot Current => slots[current];
        public IReadOnlyList<SpellSlot> Slots => slots;

        /// <summary>(new index) — fired when the selected slot changes.</summary>
        public event Action<int> OnSlotSelected;
        /// <summary>(slot index) — fired when a slot's element/delivery changes.</summary>
        public event Action<int> OnSlotChanged;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            FixSlotArray();
        }

        private void OnValidate() => FixSlotArray();

        private void FixSlotArray()
        {
            if (slots == null || slots.Length != SlotCount) Array.Resize(ref slots, SlotCount);
            for (int i = 0; i < slots.Length; i++) slots[i] ??= new SpellSlot();
        }

        private void Update()
        {
            int pressed = input.SlotPressed;
            if (pressed >= 0) Select(pressed);

            int scroll = input.SlotScroll;
            // Wheel down = next slot, wheel up = previous slot.
            if (scroll != 0) Select((current - scroll + SlotCount) % SlotCount);
        }

        public void Select(int index)
        {
            index = Mathf.Clamp(index, 0, SlotCount - 1);
            if (index == current) return;
            current = index;
            OnSlotSelected?.Invoke(current);
        }

        public void SetSlot(int index, ElementData element, DeliveryData delivery)
        {
            if (index < 0 || index >= SlotCount) return;
            slots[index].element = element;
            slots[index].delivery = delivery;
            OnSlotChanged?.Invoke(index);
        }
    }
}
