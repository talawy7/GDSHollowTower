// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Small data types shared by the combat system (GDD section 5).
//   HitEffect         = what one hit does (damage + burn/slow/freeze/stun/weaken/shock)
//   AttackData        = how an attack is shot (shots, speed, size, pierce, explosion)
//   ChainReactionData = the element's Q ability

using System;
using UnityEngine;

namespace HollowTower.Data
{
    /// <summary>GDD 5.3 delivery styles, picked with Hold E + scroll.</summary>
    public enum DeliveryType { Projectile, Orb, Nova }

    [Serializable]
    public struct HitEffect
    {
        public float damage;

        [Header("Fire")]
        [Tooltip("Burn damage per second.")]
        public float burnDps;
        public float burnDuration;

        [Header("Ice")]
        [Tooltip("0.5 = moves at half speed.")]
        [Range(0f, 0.9f)] public float slowAmount;
        public float slowDuration;
        [Tooltip("Can't move or attack.")]
        public float freezeDuration;
        [Tooltip("Extra damage taken. 0.25 = +25%.")]
        public float weakenAmount;
        public float weakenDuration;

        [Header("Lightning")]
        [Tooltip("Can't move or attack.")]
        public float stunDuration;
        [Tooltip("Electric damage per second.")]
        public float shockDps;
        public float shockDuration;
    }

    [Serializable]
    public class AttackData
    {
        public string displayName = "Attack";
        public HitEffect hit;

        [Header("Shots")]
        [Tooltip("e.g. Fire Projectile fires 3 shots.")]
        [Min(1)] public int shots = 1;
        public float timeBetweenShots = 0.08f;

        [Header("Ball")]
        public float speed = 18f;
        [Tooltip("Size and hit radius of the ball.")]
        public float radius = 0.2f;
        public float maxDistance = 20f;
        [Tooltip("True = goes through enemies (Orb).")]
        public bool piercesEnemies;

        [Header("Explosion")]
        [Tooltip("More than 0 = explodes on impact and hits everything in this radius (Nova).")]
        public float explosionRadius;
    }

    [Serializable]
    public class ChainReactionData
    {
        [Tooltip("Enemies this close to the mouse get pulled in.")]
        public float pullRadius = 6f;
        [Tooltip("How long the pull takes.")]
        public float pullDuration = 0.35f;
        [Tooltip("How long the chain visuals stay.")]
        public float chainDuration = 3f;
        [Tooltip("Max enemies caught at once.")]
        public int maxTargets = 6;

        [Tooltip("What happens to every chained enemy.")]
        public HitEffect effect;

        [Header("Bonus by selected delivery style (Fire, GDD 5.2)")]
        [Tooltip("Extra damage when the selected delivery is Projectile.")]
        public float projectileBonusDamage;
        [Tooltip("Burn damage multiplier when the selected delivery is Orb or Nova.")]
        public float orbNovaBurnMultiplier = 1f;
    }
}
