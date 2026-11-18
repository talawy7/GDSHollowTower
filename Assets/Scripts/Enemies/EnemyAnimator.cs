// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Makes enemies look alive WITHOUT animation clips (the Synty pack has none).
// Everything is done in code on the humanoid rig:
//   - arms lowered from the T-pose, walking leg/arm swing, ghost hovering
//   - windup (raise arm, lean back), strike (swing + lunge), flinch when hit
//   - fall over and sink when dead
//   - color tint: red flash on hit, blue when frozen
// If you later add an Animator Controller (e.g. Mixamo clips), this script
// stops posing bones and sends parameters instead:
//   float "Speed", triggers "Attack", "Hit", "Die".

using UnityEngine;

namespace HollowTower.Enemies
{
    public class EnemyAnimator : MonoBehaviour
    {
        [Tooltip("The visual model (child). Leave empty to use the first child.")]
        [SerializeField] private Transform model;
        [Tooltip("Ghost: float this high above the floor.")]
        [SerializeField] private float hoverHeight = 0f;
        [Tooltip("How far the arms come down from the T-pose (degrees).")]
        [SerializeField] private float armDownAngle = 70f;
        [SerializeField] private float stepSpeed = 9f;
        [SerializeField] private float legSwing = 28f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color HitTint = new Color(1f, 0.35f, 0.35f);
        private static readonly Color FrozenTint = new Color(0.55f, 0.8f, 1f);

        private Animator animator;
        private bool useAnimator;
        private Renderer[] renderers;
        private MaterialPropertyBlock block;

        // Humanoid bones and their rest rotation (procedural mode).
        private Transform lUpperArm, rUpperArm, lUpperLeg, rUpperLeg, spine;
        private Quaternion lArmRest, rArmRest, lLegRest, rLegRest, spineRest;
        private Vector3 modelRestPos;
        private Quaternion modelRestRot;

        private float moveSpeed01, walkPhase;
        private float windupT, windupDuration; private bool windingUp;
        private float strikeT = 1f;
        private float hitT = 1f;
        private float deathT = -1f;
        private bool frozen;

        public void SetHoverHeight(float h) => hoverHeight = h;

        private void Awake()
        {
            if (model == null && transform.childCount > 0) model = transform.GetChild(0);
            if (model == null) { enabled = false; return; }

            modelRestPos = model.localPosition;
            modelRestRot = model.localRotation;
            renderers = model.GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();

            animator = model.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                useAnimator = animator.runtimeAnimatorController != null;
                if (!useAnimator && animator.isHuman)
                {
                    lUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                    rUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    lUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                    rUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                    spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                }
                // No controller: switch the Animator off so it never fights our posing.
                if (!useAnimator) animator.enabled = false;
            }

            Store(lUpperArm, ref lArmRest); Store(rUpperArm, ref rArmRest);
            Store(lUpperLeg, ref lLegRest); Store(rUpperLeg, ref rLegRest);
            Store(spine, ref spineRest);
        }

        private static void Store(Transform t, ref Quaternion rest) { if (t != null) rest = t.localRotation; }

        // ---------- called by EnemyBase ----------

        public void SetMoveSpeed(float speed01)
        {
            moveSpeed01 = Mathf.Clamp01(speed01);
            if (useAnimator) SetFloat("Speed", moveSpeed01);
        }

        public void SetFrozen(bool value) => frozen = value;

        public void PlayWindup(float duration)
        {
            windingUp = true; windupT = 0f; windupDuration = Mathf.Max(0.05f, duration);
            if (useAnimator) SetTrigger("Attack");
        }

        public void PlayStrike() { windingUp = false; strikeT = 0f; }

        public void PlayHit()
        {
            hitT = 0f;
            if (useAnimator) SetTrigger("Hit");
        }

        public void PlayDeath()
        {
            deathT = 0f; windingUp = false;
            if (useAnimator) SetTrigger("Die");
        }

        // ---------- per frame ----------

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (windingUp) windupT = Mathf.Min(1f, windupT + dt / windupDuration);
            strikeT = Mathf.Min(1f, strikeT + dt / 0.25f);
            hitT = Mathf.Min(1f, hitT + dt / 0.15f);
            if (deathT >= 0f) deathT += dt;
            if (!frozen && deathT < 0f) walkPhase += dt * stepSpeed * moveSpeed01;

            PoseModelRoot();
            if (!useAnimator) PoseBones();
            Tint();
        }

        private void PoseModelRoot()
        {
            Vector3 pos = modelRestPos;
            Quaternion rot = modelRestRot;

            if (hoverHeight > 0f) pos.y += hoverHeight + Mathf.Sin(Time.time * 2f) * 0.12f;
            else pos.y += Mathf.Abs(Mathf.Sin(walkPhase)) * 0.06f * moveSpeed01; // small walking bounce

            float lean = moveSpeed01 * 8f;                                    // lean into the run
            if (windingUp) lean -= 12f * windupT;                            // lean back while charging
            float strike = (1f - strikeT) * (1f - strikeT);
            lean += 18f * strike;                                            // throw weight forward
            pos.z += 0.35f * strike;                                         // small lunge
            if (hitT < 1f) lean -= 10f * (1f - hitT);                        // flinch

            if (deathT >= 0f)
            {
                float fall = Mathf.Clamp01(deathT / 0.5f);
                lean = Mathf.Lerp(lean, -85f, fall * fall);                  // fall backwards
                if (deathT > 1.5f) pos.y -= (deathT - 1.5f) * 0.6f;          // then sink into the floor
            }

            model.localPosition = pos;
            model.localRotation = rot * Quaternion.Euler(lean, 0f, 0f);
        }

        private void PoseBones()
        {
            Vector3 fwd = transform.forward, right = transform.right;
            float swing = deathT >= 0f || frozen ? 0f : Mathf.Sin(walkPhase) * legSwing * moveSpeed01;

            // Legs swing opposite each other.
            Rotate(lUpperLeg, lLegRest, swing, right);
            Rotate(rUpperLeg, rLegRest, -swing, right);

            // Arms: down from the T-pose, then swing opposite to the legs.
            Rotate(lUpperArm, lArmRest, armDownAngle, fwd);
            AddRotate(lUpperArm, -swing * 0.6f, right);

            Rotate(rUpperArm, rArmRest, -armDownAngle, fwd);
            float attackArm = 0f;
            if (windingUp) attackArm = -130f * windupT;                     // raise the weapon arm
            else if (strikeT < 1f) attackArm = Mathf.Lerp(-130f, 20f, strikeT); // swing it down
            AddRotate(rUpperArm, swing * 0.6f + attackArm, right);

            if (spine != null)
            {
                spine.localRotation = spineRest;
                if (windingUp) spine.rotation = Quaternion.AngleAxis(-20f * windupT, Vector3.up) * spine.rotation;
            }
        }

        private static void Rotate(Transform bone, Quaternion rest, float angle, Vector3 axis)
        {
            if (bone == null) return;
            bone.localRotation = rest;
            bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
        }

        private static void AddRotate(Transform bone, float angle, Vector3 axis)
        {
            if (bone == null) return;
            bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
        }

        private void Tint()
        {
            Color c = Color.white;
            if (frozen) c = FrozenTint;
            if (hitT < 1f) c = Color.Lerp(HitTint, c, hitT);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(block);
            }
        }

        private void SetFloat(string param, float v) { if (HasParam(param)) animator.SetFloat(param, v); }
        private void SetTrigger(string param) { if (HasParam(param)) animator.SetTrigger(param); }

        private bool HasParam(string param)
        {
            foreach (var p in animator.parameters) if (p.name == param) return true;
            return false;
        }
    }
}
