// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Draws glowing chains between enemies caught by a Chain Reaction (Q)
// and follows them while they move. Placeholder until we have a chain VFX.

using System.Collections.Generic;
using HollowTower.Core;
using UnityEngine;

namespace HollowTower.Spells
{
    public class ChainLinkVisual : MonoBehaviour
    {
        private readonly List<Health> targets = new List<Health>();
        private LineRenderer line;
        private float timeLeft;

        public static void Create(IList<Health> chained, Color color, float duration)
        {
            if (chained == null || chained.Count == 0) return;
            var go = new GameObject("ChainLinks");
            var v = go.AddComponent<ChainLinkVisual>();
            v.targets.AddRange(chained);
            v.timeLeft = duration;
            v.line = SpellVisuals.CreateLine(go, color, 0.12f);
            v.UpdateLine();
        }

        private void Update()
        {
            timeLeft -= Time.deltaTime;
            targets.RemoveAll(t => t == null || t.IsDead);
            if (timeLeft <= 0f || targets.Count == 0) { Destroy(gameObject); return; }
            UpdateLine();
        }

        private void UpdateLine()
        {
            // One enemy: a short chain pinning it to the floor. Several: a closed loop.
            if (targets.Count == 1)
            {
                Vector3 c = SpellVisuals.CenterOf(targets[0]);
                line.positionCount = 2;
                line.SetPosition(0, c);
                line.SetPosition(1, SpellVisuals.GroundBelow(c));
                return;
            }

            line.positionCount = targets.Count + 1;
            for (int i = 0; i < targets.Count; i++) line.SetPosition(i, SpellVisuals.CenterOf(targets[i]));
            line.SetPosition(targets.Count, SpellVisuals.CenterOf(targets[0]));
        }
    }
}
