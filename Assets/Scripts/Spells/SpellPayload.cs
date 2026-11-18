// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Everything a cast spell needs once it has left the witch's hand.

using HollowTower.Core;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Spells
{
    public class SpellPayload
    {
        public ElementData element;
        public AttackData attack;
        /// <summary>What each hit does (copied from attack.hit so it can be changed per cast).</summary>
        public HitEffect hit;

        public GameObject caster;
        public Team casterTeam;

        /// <summary>Null for the Normal Attack, otherwise the E delivery style.</summary>
        public DeliveryType? delivery;

        public Color Color => element != null ? element.color : Color.white;

        /// <summary>e.g. "Fire Normal Attack" or "Ice Orb".</summary>
        public string FullName => $"{element?.displayName} {attack?.displayName}";
    }
}
