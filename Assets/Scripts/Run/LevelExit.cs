// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// The way out of a scene. Walk onto it when it's open.
//   StartRun:  in the Cell, always open, starts a new run
//   NextFloor: on a floor, opens when all waves are cleared
//   Victory:   in the Boss scene, opens when the boss is dead -> win
// Shows a dim red disc when closed and a bright pillar of light when open.
// Replace the look with your own model later; keep this component on it.

using HollowTower.Player;
using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Run
{
    public enum ExitType { StartRun, NextFloor, Victory }

    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private ExitType type = ExitType.NextFloor;
        [Tooltip("Walk this close to use it.")]
        [SerializeField] private float radius = 2.2f;
        [SerializeField] private Color openColor = new Color(0.4f, 0.9f, 1f);

        private static readonly Color ClosedColor = new Color(0.35f, 0.05f, 0.05f);

        private Transform player;
        private bool used;
        private Renderer disc;
        private GameObject beam;
        private Light glow;

        public ExitType Type => type;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            d.name = "ExitDisc";
            Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(transform, false);
            d.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            d.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            disc = d.GetComponent<Renderer>();

            beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "ExitBeam";
            Destroy(beam.GetComponent<Collider>());
            beam.transform.SetParent(transform, false);
            beam.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            beam.transform.localScale = new Vector3(radius * 0.6f, 1.5f, radius * 0.6f);
            beam.GetComponent<Renderer>().sharedMaterial = SpellVisuals.GlowMaterial(openColor);
            beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 6f;
            glow.intensity = 4f;
            glow.color = openColor;

            SetOpen(type == ExitType.StartRun);
        }

        private void Start()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        public void Open() => SetOpen(true);

        private void SetOpen(bool open)
        {
            IsOpen = open;
            disc.sharedMaterial = SpellVisuals.UnlitMaterial(open ? openColor : ClosedColor);
            beam.SetActive(open);
            glow.enabled = open;
        }

        private void Update()
        {
            if (!IsOpen || used || player == null) return;
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            if (d.magnitude > radius) return;

            used = true;
            switch (type)
            {
                case ExitType.StartRun:  RunManager.Instance.StartRun(); break;
                case ExitType.NextFloor: RunManager.Instance.CompleteFloor(); break;
                case ExitType.Victory:   RunManager.Instance.WinRun(); break;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = type == ExitType.StartRun ? Color.cyan : type == ExitType.Victory ? Color.yellow : Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
