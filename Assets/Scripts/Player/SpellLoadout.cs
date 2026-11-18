// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
// Week 2: Claude changed it to 2 element slots for the new GDD.
//
// GDD 4.5 / 5.1: the witch equips 2 skills, each one an element.
// Scroll the mouse wheel (without holding E) to switch between them.
// Two different elements = you can set up elemental reactions (GDD 5.4).
// The workbench (week 5) will call SetSlot() to change them.

using System;
using System.Collections.Generic;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Player
{
    [RequireComponent(typeof(PlayerInputReader))]
    public class SpellLoadout : MonoBehaviour
    {
        public const int SlotCount = 2;

        [SerializeField] private ElementData[] slots = new ElementData[SlotCount];

        private PlayerInputReader input;
        private int current;

        public int CurrentIndex => current;
        public ElementData Current => slots[current];
        public IReadOnlyList<ElementData> Slots => slots;

        /// <summary>(new index) — fired when the selected skill changes.</summary>
        public event Action<int> OnSlotSelected;
        /// <summary>(slot index) — fired when a slot's element changes.</summary>
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
        }

        private void Update()
        {
            int pressed = input.SlotPressed;
            if (pressed >= 0) Select(pressed);

            // While E is held the wheel picks the delivery style instead.
            if (!input.DeliveryHeld && input.Scroll != 0) Select((current + 1) % SlotCount);
        }

        public void Select(int index)
        {
            index = Mathf.Clamp(index, 0, SlotCount - 1);
            if (index == current || slots[index] == null) return;
            current = index;
            OnSlotSelected?.Invoke(current);
        }

        public void SetSlot(int index, ElementData element)
        {
            if (index < 0 || index >= SlotCount) return;
            slots[index] = element;
            OnSlotChanged?.Invoke(index);
        }
    }
}
