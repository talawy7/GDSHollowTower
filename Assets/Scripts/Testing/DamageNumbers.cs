// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// TEST ONLY. Floating damage numbers drawn with OnGUI, so we can check
// spell damage while balancing. Creates itself the first time it's used.

using System.Collections.Generic;
using UnityEngine;

namespace HollowTower.Testing
{
    public class DamageNumbers : MonoBehaviour
    {
        private const float Lifetime = 0.8f;

        private struct Entry
        {
            public Vector3 world;
            public string text;
            public Color color;
            public float age;
        }

        private static DamageNumbers instance;
        private readonly List<Entry> entries = new List<Entry>();
        private GUIStyle style;

        public static void Show(Vector3 worldPos, float amount, Color color)
        {
            if (instance == null)
            {
                instance = new GameObject("DamageNumbers").AddComponent<DamageNumbers>();
            }
            worldPos += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            instance.entries.Add(new Entry { world = worldPos, text = Mathf.RoundToInt(amount).ToString(), color = color });
        }

        private void Update()
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                e.age += Time.deltaTime;
                e.world += Vector3.up * Time.deltaTime * 1.5f;
                if (e.age >= Lifetime) entries.RemoveAt(i);
                else entries[i] = e;
            }
        }

        private void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null) return;
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            foreach (var e in entries)
            {
                Vector3 s = cam.WorldToScreenPoint(e.world);
                if (s.z < 0f) continue;
                var rect = new Rect(s.x - 40f, Screen.height - s.y - 12f, 80f, 24f);
                float a = 1f - e.age / Lifetime;
                GUI.color = new Color(0f, 0f, 0f, a);
                GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), e.text, style);
                GUI.color = new Color(e.color.r, e.color.g, e.color.b, a);
                GUI.Label(rect, e.text, style);
            }
            GUI.color = Color.white;
        }
    }
}
