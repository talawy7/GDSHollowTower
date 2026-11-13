// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 4.1: the witch moves in 8 directions, always faces the mouse, and
// dashes a short distance with Space. She can't be hit during the dash,
// and the dash has a cooldown.

using System;
using HollowTower.Core;
using HollowTower.Data;
using UnityEngine;

namespace HollowTower.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera cam;
        [SerializeField] private float gravity = -25f;

        private CharacterController controller;
        private PlayerInputReader input;
        private Health health;

        private Vector3 velocity;        // horizontal walking velocity
        private float verticalSpeed;     // gravity
        private Vector3 aimDirection = Vector3.forward;

        private bool isDodging;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private Vector3 dodgeDirection;

        /// <summary>World point under the mouse at the witch's height. Spells will aim at this.</summary>
        public Vector3 AimPoint { get; private set; }
        /// <summary>Flat direction from the witch to the mouse.</summary>
        public Vector3 AimDirection => aimDirection;
        public bool IsDodging => isDodging;
        /// <summary>0 = ready, 1 = just used. For the HUD.</summary>
        public float DodgeCooldown01 => stats == null || stats.dodgeCooldown <= 0f ? 0f : Mathf.Clamp01(dodgeCooldownTimer / stats.dodgeCooldown);
        /// <summary>Set false to freeze the player (dialogue, workbench, pause, death).</summary>
        public bool ControlEnabled { get; set; } = true;

        public event Action OnDodge;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<Health>();
            if (cam == null) cam = Camera.main;

            if (stats == null)
            {
                Debug.LogWarning("PlayerController: no PlayerStats assigned, using defaults.", this);
                stats = ScriptableObject.CreateInstance<PlayerStats>();
            }
            health.SetMax(stats.maxHealth, refill: true);
            health.OnDied += _ => ControlEnabled = false;
        }

        private void Update()
        {
            if (!ControlEnabled || health.IsDead)
            {
                ApplyGravityOnly();
                return;
            }

            UpdateAim();
            dodgeCooldownTimer = Mathf.Max(0f, dodgeCooldownTimer - Time.deltaTime);

            if (isDodging) UpdateDodge();
            else
            {
                if (input.DodgePressed && dodgeCooldownTimer <= 0f) StartDodge();
                else UpdateMovement();
            }

            FaceAim();
        }

        // ---------- Movement ----------

        private void UpdateMovement()
        {
            Vector3 wish = CameraRelative(input.Move);
            Vector3 target = wish * stats.moveSpeed;
            velocity = Vector3.MoveTowards(velocity, target, stats.acceleration * Time.deltaTime);
            Move(velocity);
        }

        /// <summary>Turns WASD into a world direction that matches the screen, whatever the camera's Y rotation.</summary>
        private Vector3 CameraRelative(Vector2 raw)
        {
            if (raw.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            fwd.y = 0f; right.y = 0f;
            fwd.Normalize(); right.Normalize();
            Vector3 dir = fwd * raw.y + right * raw.x;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir; // diagonals aren't faster
        }

        private void Move(Vector3 horizontal)
        {
            if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
            verticalSpeed += gravity * Time.deltaTime;
            Vector3 motion = horizontal;
            motion.y = verticalSpeed;
            controller.Move(motion * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            velocity = Vector3.zero;
            Move(Vector3.zero);
        }

        // ---------- Aim ----------

        private void UpdateAim()
        {
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(input.MouseScreenPosition);
            Plane ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (ground.Raycast(ray, out float dist))
            {
                AimPoint = ray.GetPoint(dist);
                Vector3 dir = AimPoint - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) aimDirection = dir.normalized;
            }
        }

        private void FaceAim()
        {
            transform.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        }

        // ---------- Dodge ----------

        private void StartDodge()
        {
            Vector3 wish = CameraRelative(input.Move);
            // Dash where you're walking; if standing still, dash toward the mouse.
            dodgeDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : aimDirection;

            isDodging = true;
            dodgeTimer = stats.dodgeDuration;
            dodgeCooldownTimer = stats.dodgeCooldown;
            health.Invulnerable = true;
            OnDodge?.Invoke();
        }

        private void UpdateDodge()
        {
            float speed = stats.dodgeDuration > 0f ? stats.dodgeDistance / stats.dodgeDuration : 0f;
            Move(dodgeDirection * speed);

            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0f)
            {
                isDodging = false;
                health.Invulnerable = false;
                velocity = dodgeDirection * stats.moveSpeed; // smooth exit
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + aimDirection * 2f);
        }
    }
}
