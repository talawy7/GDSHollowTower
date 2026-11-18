// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// One equipped spell = one element + one delivery (GDD 4.3).

using System;
using HollowTower.Data;

namespace HollowTower.Spells
{
    [Serializable]
    public class SpellSlot
    {
        public ElementData element;
        public DeliveryData delivery;

        public bool IsValid => element != null && delivery != null;

        /// <summary>e.g. "Fire Projectile", or "Empty".</summary>
        public string DisplayName => IsValid ? $"{element.displayName} {delivery.displayName}" : "Empty";
    }
}
