// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Knight: slow, tough melee (the tank). Its big sword swing has a long windup
// with a red warning circle on the floor, so the player can dodge out of it.
// Freezing or stunning the knight during the windup cancels the swing.

using UnityEngine;

namespace HollowTower.Enemies
{
    public class KnightEnemy : EnemyBase
    {
        private AttackTelegraph telegraph;

        private Vector3 SwingCenter => transform.position + transform.forward * (data.attackAreaRadius * 0.8f);

        protected override void OnAttackWindup()
        {
            telegraph = AttackTelegraph.Show(SwingCenter, data.attackAreaRadius, data.attackWindup, transform);
        }

        protected override void UpdateAttack()
        {
            base.UpdateAttack();
            // Keep the warning circle in front of the knight while it is still turning.
            if (telegraph != null && State == EnemyState.Attack) telegraph.MoveTo(SwingCenter);
        }

        protected override void PerformAttack()
        {
            TryHitPlayer(SwingCenter, data.attackAreaRadius);
            HideTelegraph();
        }

        protected override void OnAttackCancelled() => HideTelegraph();

        private void HideTelegraph()
        {
            if (telegraph != null) telegraph.Hide();
            telegraph = null;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            if (data == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f);
            Gizmos.DrawWireSphere(SwingCenter, data.attackAreaRadius);
        }
    }
}
