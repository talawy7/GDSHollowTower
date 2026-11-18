// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Placeholder spell visuals made from Unity primitives, so spells are
// visible before we have real VFX. Swap these for particle prefabs in week 5.

using System.Collections.Generic;
using UnityEngine;

namespace HollowTower.Spells
{
    public static class SpellVisuals
    {
        private static readonly Dictionary<Color, Material> litCache = new Dictionary<Color, Material>();
        private static readonly Dictionary<Color, Material> unlitCache = new Dictionary<Color, Material>();

        /// <summary>Glowing ball + small point light as a child of the spell.</summary>
        public static void AttachBall(Transform parent, Color color, float diameter)
        {
            var ball = CreateVisualPrimitive(PrimitiveType.Sphere, GlowMaterial(color));
            ball.name = "Visual";
            ball.transform.SetParent(parent, false);
            ball.transform.localScale = Vector3.one * diameter;

            var light = parent.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 2.5f + diameter * 2f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
        }

        /// <summary>Small sphere that grows and disappears. Used for impacts.</summary>
        public static void Burst(Vector3 position, Color color, float size)
        {
            var go = CreateVisualPrimitive(PrimitiveType.Sphere, GlowMaterial(color));
            go.name = "Burst";
            go.transform.position = position;
            go.AddComponent<GrowAndVanish>().Init(Vector3.one * size * 0.3f, Vector3.one * size, 0.15f);
        }

        /// <summary>Flat expanding disc on the floor. Used for Nova.</summary>
        public static void Ring(Vector3 center, Color color, float radius)
        {
            var go = CreateVisualPrimitive(PrimitiveType.Cylinder, GlowMaterial(color));
            go.name = "NovaRing";
            go.transform.position = new Vector3(center.x, center.y - 0.95f, center.z);
            float d = radius * 2f;
            go.AddComponent<GrowAndVanish>().Init(new Vector3(0.5f, 0.02f, 0.5f), new Vector3(d, 0.02f, d), 0.2f);
        }

        /// <summary>Jagged line between two points. Used for Chain jumps.</summary>
        public static void ChainArc(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("ChainArc");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = UnlitMaterial(color);
            lr.startColor = lr.endColor = color;
            lr.widthMultiplier = 0.08f;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            const int segments = 8;
            lr.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < segments) p += Random.insideUnitSphere * 0.25f;
                lr.SetPosition(i, p);
            }
            Object.Destroy(go, 0.15f);
        }

        // ---------- helpers ----------

        private static GameObject CreateVisualPrimitive(PrimitiveType type, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>()); // visuals must never block spells
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public static Material GlowMaterial(Color color)
        {
            if (litCache.TryGetValue(color, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader) { name = "SpellGlow" };
            m.color = color;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 1.2f); // higher values wash light colors out to white
            litCache[color] = m;
            return m;
        }

        public static Material UnlitMaterial(Color color)
        {
            if (unlitCache.TryGetValue(color, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            m = new Material(shader) { name = "SpellUnlit" };
            m.color = color;
            unlitCache[color] = m;
            return m;
        }
    }

    /// <summary>Scales an object from A to B, then destroys it.</summary>
    public class GrowAndVanish : MonoBehaviour
    {
        private Vector3 from, to;
        private float duration, t;

        public void Init(Vector3 startScale, Vector3 endScale, float seconds)
        {
            from = startScale; to = endScale; duration = Mathf.Max(0.01f, seconds);
            transform.localScale = from;
        }

        private void Update()
        {
            t += Time.deltaTime / duration;
            transform.localScale = Vector3.Lerp(from, to, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
