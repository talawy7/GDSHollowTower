// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Red warning circle on the floor that fills up during an attack's windup,
// so the player knows where the hit will land and when.

using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Enemies
{
    public class AttackTelegraph : MonoBehaviour
    {
        private static readonly Color OutlineColor = new Color(0.6f, 0.05f, 0.05f);
        private static readonly Color FillColor = new Color(1f, 0.15f, 0.1f);

        private Transform fill;
        private float duration, t, radius, groundY;

        public static AttackTelegraph Show(Vector3 center, float radius, float duration, Transform owner)
        {
            var go = new GameObject("AttackTelegraph");
            var tg = go.AddComponent<AttackTelegraph>();
            tg.radius = radius;
            tg.duration = Mathf.Max(0.05f, duration);
            tg.groundY = SpellVisuals.GroundBelow(owner.position + Vector3.up).y;

            CreateDisc(go.transform, OutlineColor, radius * 2f, 0.02f);
            tg.fill = CreateDisc(go.transform, FillColor, 0.01f, 0.03f);
            tg.MoveTo(center);
            return tg;
        }

        private static Transform CreateDisc(Transform parent, Color color, float diameter, float y)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(disc.GetComponent<Collider>());
            var r = disc.GetComponent<MeshRenderer>();
            r.sharedMaterial = SpellVisuals.UnlitMaterial(color);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = new Vector3(0f, y, 0f);
            disc.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            return disc.transform;
        }

        public void MoveTo(Vector3 center)
        {
            transform.position = new Vector3(center.x, groundY, center.z);
        }

        public void Hide()
        {
            if (this != null) Destroy(gameObject);
        }

        private void Update()
        {
            t += Time.deltaTime / duration;
            float d = Mathf.Lerp(0.01f, radius * 2f, Mathf.Clamp01(t));
            fill.localScale = new Vector3(d, 0.01f, d);
            if (t > 1.5f) Destroy(gameObject); // safety
        }
    }
}
