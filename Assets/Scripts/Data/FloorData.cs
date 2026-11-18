// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// One floor's enemy plan: a list of waves. Each floor scene has a WaveSpawner
// that reads one of these. Change the waves here to give a floor its theme
// (e.g. more Ghosts or more Goblins) without touching code.
// Create via: Assets > Create > Hollow Tower > Floor

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowTower.Data
{
    [CreateAssetMenu(fileName = "Floor", menuName = "Hollow Tower/Floor")]
    public class FloorData : ScriptableObject
    {
        public string displayName = "Floor";
        [TextArea] public string description;

        [Header("Waves")]
        public List<Wave> waves = new List<Wave>();
        [Tooltip("Seconds after the floor loads before the first wave.")]
        public float timeBeforeFirstWave = 2f;
        [Tooltip("Seconds between the end of one wave and the start of the next.")]
        public float timeBetweenWaves = 2.5f;

        [Header("Difficulty by position in the run")]
        [Tooltip("Floors come in random order, so later floors get tougher: +15% enemy health for every floor already cleared.")]
        public float healthBonusPerFloorCleared = 0.15f;
    }

    [Serializable]
    public class Wave
    {
        [Tooltip("Just a note for you, e.g. \"goblin rush\".")]
        public string note;
        public List<WaveEntry> enemies = new List<WaveEntry>();
        [Tooltip("The next wave starts when this many enemies (or fewer) are still alive. 0 = wait until all are dead.")]
        public int startNextWhenRemaining;
    }

    [Serializable]
    public class WaveEntry
    {
        [Tooltip("An enemy prefab from Assets/Prefabs/Enemies.")]
        public GameObject enemyPrefab;
        [Min(1)] public int count = 1;
        [Tooltip("Chance for each one to be an Elite (0 = never, 1 = always).")]
        [Range(0f, 1f)] public float eliteChance;
    }
}
