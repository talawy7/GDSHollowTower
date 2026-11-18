// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Everything a cast spell needs to know once it has left the witch's hand:
// what it is, how hard it hits, who cast it, and which modifiers it carries.

using HollowTower.Core;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Spells
{
    public class SpellPayload
    {
        public ElementData element;
        public DeliveryData delivery;
        public float damage;

        public GameObject caster;
        public Team casterTeam;

        // Chain (from the modifier and/or Lightning's innate arcing)
        public int chainJumps;
        public float chainRange = 5f;
        public float chainFalloff = 0.6f;

        // Bounce modifier
        public int bounces;

        // Which modifiers were used, for the HUD/log and later for the Scribe's memory.
        public bool usedChain;
        public bool usedBounce;

        public Color Color => element != null ? element.color : Color.white;

        /// <summary>e.g. "Chained Bouncing Fire Projectile" (GDD 4.4 naming).</summary>
        public string FullName
        {
            get
            {
                string n = $"{element?.displayName} {delivery?.displayName}";
                if (usedBounce) n = "Bouncing " + n;
                if (usedChain) n = "Chained " + n;
                return n;
            }
        }
    }
}
