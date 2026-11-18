// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// TEST ONLY. A target for spells: shows its health, flashes when hit,
// turns orange while burning and blue while slowed, and comes back
// after dying. Can walk side to side so you can see Ice's slow.

using System.Collections;
using HollowTower.Core;
using HollowTower.Spells;
using UnityEngine;

namespace HollowTower.Testing
{
    [RequireComponent(typeof(Health))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private bool moveSideToSide = false;
        [SerializeField] private float moveDistance = 3f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float respawnDelay = 2f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color BurnColor = new Color(1f, 0.45f, 0.1f);
        private static readonly Color SlowColor = new Color(0.45f, 0.8f, 1f);

        private Health health;
        private StatusEffects status;
        private Renderer[] renderers;
        private Collider[] colliders;
        private MaterialPropertyBlock block;
        private Color baseColor = Color.gray;

        private Vector3 startPos;
        private float walkPhase;
        private float flash;

        private void Awake()
        {
            health = GetComponent<Health>();
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            block = new MaterialPropertyBlock();
            if (renderers.Length > 0 && renderers[0].sharedMaterial != null)
                baseColor = renderers[0].sharedMaterial.color;
            startPos = transform.position;
        }

        private void OnEnable()
        {
            health.OnDamaged += HandleDamaged;
            health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDied -= HandleDied;
        }

        private void HandleDamaged(float amount, GameObject source)
        {
            flash = 0.08f;
            Color c = status != null && status.IsBurning ? BurnColor : Color.white;
            DamageNumbers.Show(transform.position + Vector3.up * 1.3f, amount, c);
        }

        private void HandleDied(GameObject killer)
        {
            StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            SetVisible(false);
            if (status != null) status.ClearAll();
            yield return new WaitForSeconds(respawnDelay);
            transform.position = startPos;
            walkPhase = 0f;
            health.ResetHealth();
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in renderers) r.enabled = visible;
            foreach (var c in colliders) c.enabled = visible;
        }

        private void Update()
        {
            if (status == null) status = GetComponent<StatusEffects>();
            if (health.IsDead) return;

            if (moveSideToSide)
            {
                float speedMul = status != null ? status.SpeedMultiplier : 1f;
                walkPhase += Time.deltaTime * moveSpeed * speedMul / Mathf.Max(0.1f, moveDistance);
                transform.position = startPos + transform.right * Mathf.Sin(walkPhase) * moveDistance;
            }

            Color tint = baseColor;
            if (status != null && status.IsSlowed) tint = Color.Lerp(tint, SlowColor, 0.7f);
            if (status != null && status.IsBurning) tint = Color.Lerp(tint, BurnColor, 0.7f);
            if (status != null && (status.IsFrozen || status.IsStunned)) tint = status.IsFrozen ? new Color(0.8f, 0.95f, 1f) : new Color(1f, 1f, 0.5f);
            if (flash > 0f) { flash -= Time.deltaTime; tint = Color.white; }

            foreach (var r in renderers)
            {
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, tint);
                r.SetPropertyBlock(block);
            }
        }

        private void OnGUI()
        {
            if (health.IsDead) return;
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.4f);
            if (screen.z < 0f) return;

            const float w = 60f, h = 7f;
            float x = screen.x - w / 2f, y = Screen.height - screen.y;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);
            GUI.color = new Color(0.8f, 0.15f, 0.15f);
            GUI.DrawTexture(new Rect(x, y, w * (health.Current / health.Max), h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
