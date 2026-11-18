// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
// Week 4: Claude added Configure() and Alert() for the wave spawner.
//
// Base enemy with a finite state machine (GDD Part A: "Enemy AI: state machines + NavMesh").
//
//   Idle ──player in range──▶ Chase ──close enough──▶ Attack (windup) ──▶ Recover ──▶ Chase
//     any state ──frozen/stunned──▶ Disabled ──effect ends──▶ Chase
//     any state ──health 0──▶ Dead
//
// GoblinEnemy, KnightEnemy and GhostEnemy override how they move and how they attack.
// Numbers come from an EnemyData asset.

using System;
using HollowTower.Core;
using HollowTower.Data;
using HollowTower.Player;
using HollowTower.Spells;
using UnityEngine;
using UnityEngine.AI;

namespace HollowTower.Enemies
{
    public enum EnemyState { Idle, Chase, Attack, Recover, Disabled, Dead }

    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected EnemyData data;
        [Tooltip("GDD 4.7: a stronger version (more health, more damage, bigger).")]
        [SerializeField] protected bool isElite;

        /// <summary>Global switch so we can test spells on enemies that stand still (test HUD: T).</summary>
        public static bool AIEnabled = true;

        /// <summary>(enemy, killer) — for Soul Ink and the Scribe's memory.</summary>
        public static event Action<EnemyBase, GameObject> OnAnyEnemyKilled;

        protected Health health;
        protected NavMeshAgent agent;
        protected StatusEffects status;
        protected EnemyAnimator anim;
        protected Transform player;
        protected Health playerHealth;

        protected float stateTimer;
        protected float attackCooldownLeft;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public EnemyData Data => data;
        public bool IsElite => isElite;

        protected float Damage => data.attackDamage * (isElite ? data.eliteDamageMultiplier : 1f);
        protected bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;

        protected virtual void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<EnemyAnimator>();
            status = StatusEffects.GetOrAdd(health);

            health.SetTeam(Team.Enemy);
            if (data == null)
            {
                Debug.LogError($"{name}: no EnemyData assigned.", this);
                enabled = false;
                return;
            }
            baseScale = transform.localScale;
            ApplyStats();
            if (anim != null && data.hoverHeight > 0f) anim.SetHoverHeight(data.hoverHeight);

            // The agent is turned on in Start, after the room has built its NavMesh.
            agent.enabled = false;
            agent.updateRotation = false; // we turn the enemy ourselves
        }

        private Vector3 baseScale;
        private float healthMultiplier = 1f;

        private void ApplyStats()
        {
            health.SetMax(data.maxHealth * (isElite ? data.eliteHealthMultiplier : 1f) * healthMultiplier, refill: true);
            transform.localScale = baseScale * (isElite ? data.eliteScale : 1f);
        }

        /// <summary>
        /// Called by the WaveSpawner right after spawning: elite or not, and extra health
        /// for floors that come later in the run.
        /// </summary>
        public void Configure(bool elite, float extraHealthMultiplier)
        {
            if (data == null) return;
            isElite = elite;
            healthMultiplier = Mathf.Max(0.1f, extraHealthMultiplier);
            ApplyStats();
            if (elite) name += " (Elite)";
        }

        /// <summary>Start chasing the player right away, whatever the distance (wave enemies).</summary>
        public void Alert()
        {
            alerted = true;
        }

        private bool alerted;

        protected virtual void Start()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
                playerHealth = pc.GetComponent<Health>();
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(hit.position);
            }
            else Debug.LogWarning($"{name}: no NavMesh nearby, enemy can't move.", this);

            agent.speed = data.moveSpeed;
            agent.angularSpeed = data.turnSpeed;
            agent.stoppingDistance = 0.2f;

            health.OnDied += HandleDeath;
            health.OnDamaged += (amount, source) => { if (anim != null) anim.PlayHit(); };
        }

        protected virtual void Update()
        {
            if (State == EnemyState.Dead) return;

            attackCooldownLeft -= Time.deltaTime;
            if (AgentReady) agent.speed = data.moveSpeed * status.SpeedMultiplier;

            // Frozen / stunned beats everything else (an attack in progress is cancelled).
            if (!status.CanAct)
            {
                if (State != EnemyState.Disabled)
                {
                    OnAttackCancelled();
                    SetState(EnemyState.Disabled);
                }
            }

            switch (State)
            {
                case EnemyState.Idle:     UpdateIdle(); break;
                case EnemyState.Chase:    UpdateChase(); break;
                case EnemyState.Attack:   UpdateAttack(); break;
                case EnemyState.Recover:  UpdateRecover(); break;
                case EnemyState.Disabled: UpdateDisabled(); break;
            }

            if (anim != null)
            {
                float speed01 = AgentReady && data.moveSpeed > 0f ? agent.velocity.magnitude / data.moveSpeed : 0f;
                anim.SetMoveSpeed(speed01);
                anim.SetFrozen(status.IsFrozen);
            }
        }

        // ---------- states ----------

        protected void SetState(EnemyState next)
        {
            State = next;
            if (AgentReady && next != EnemyState.Chase) agent.ResetPath();
        }

        protected bool PlayerAvailable => AIEnabled && player != null && (playerHealth == null || !playerHealth.IsDead);
        protected float DistanceToPlayer => player == null ? float.MaxValue : FlatDistance(transform.position, player.position);

        protected virtual void UpdateIdle()
        {
            StopMoving();
            if (PlayerAvailable && (alerted || DistanceToPlayer <= data.detectionRange)) SetState(EnemyState.Chase);
        }

        /// <summary>Default (melee): run at the player and attack when close.</summary>
        protected virtual void UpdateChase()
        {
            if (!PlayerAvailable) { SetState(EnemyState.Idle); return; }

            if (DistanceToPlayer <= data.attackRange && attackCooldownLeft <= 0f)
            {
                BeginAttack();
                return;
            }

            MoveTo(player.position);
            FaceDirection(AgentReady ? agent.desiredVelocity : Vector3.zero);
        }

        protected void BeginAttack()
        {
            SetState(EnemyState.Attack);
            stateTimer = data.attackWindup;
            StopMoving();
            OnAttackWindup();
            if (anim != null) anim.PlayWindup(data.attackWindup);
        }

        protected virtual void UpdateAttack()
        {
            stateTimer -= Time.deltaTime;
            // Turn toward the player during the first part of the windup only,
            // so a well-timed dodge makes the attack miss.
            if (stateTimer > data.attackWindup * 0.4f && player != null) FaceDirection(player.position - transform.position);

            if (stateTimer <= 0f)
            {
                PerformAttack();
                if (anim != null) anim.PlayStrike();
                attackCooldownLeft = data.attackCooldown;
                SetState(EnemyState.Recover);
                stateTimer = data.attackRecovery;
            }
        }

        protected virtual void UpdateRecover()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) SetState(EnemyState.Chase);
        }

        protected virtual void UpdateDisabled()
        {
            StopMoving();
            if (status.CanAct) SetState(PlayerAvailable ? EnemyState.Chase : EnemyState.Idle);
        }

        // ---------- what each enemy type changes ----------

        /// <summary>Start of the warning time (show a telegraph, play a sound).</summary>
        protected virtual void OnAttackWindup() { }
        /// <summary>The hit itself.</summary>
        protected abstract void PerformAttack();
        /// <summary>Attack interrupted by freeze/stun/death (hide the telegraph).</summary>
        protected virtual void OnAttackCancelled() { }

        // ---------- helpers ----------

        protected void MoveTo(Vector3 target)
        {
            if (!AgentReady) return;
            agent.isStopped = false;
            agent.SetDestination(target);
        }

        protected void StopMoving()
        {
            if (!AgentReady) return;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        protected void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f || !status.CanAct) return;
            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, data.turnSpeed * Time.deltaTime);
        }

        /// <summary>Deals damage to the player if they are within 'radius' of 'center'.</summary>
        protected bool TryHitPlayer(Vector3 center, float radius)
        {
            if (playerHealth == null || playerHealth.IsDead) return false;
            if (FlatDistance(center, player.position) > radius) return false;
            playerHealth.TakeDamage(Damage, gameObject);
            return true;
        }

        protected static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void HandleDeath(GameObject killer)
        {
            OnAttackCancelled();
            State = EnemyState.Dead;
            if (AgentReady) agent.isStopped = true;
            agent.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            if (anim != null) anim.PlayDeath();
            OnAnyEnemyKilled?.Invoke(this, killer);
            Destroy(gameObject, 3f);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.attackRange);
        }
    }
}
