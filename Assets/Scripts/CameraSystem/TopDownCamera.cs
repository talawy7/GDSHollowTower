// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// GDD 2: top-down 2.5D, fixed camera. The camera keeps a fixed angle and
// smoothly follows the witch. Put this on the Main Camera.

using UnityEngine;

namespace HollowTower.CameraSystem
{
    public class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [Header("Angle")]
        [Tooltip("How far the camera tilts down. 90 = straight down.")]
        [Range(30f, 90f)] [SerializeField] private float pitch = 60f;
        [Tooltip("Rotation around the vertical axis. 0 = 'up' on screen is world +Z.")]
        [SerializeField] private float yaw = 0f;
        [SerializeField] private float distance = 16f;

        [Header("Follow")]
        [Tooltip("Lower = floatier. 0 = snap.")]
        [SerializeField] private float smoothTime = 0.12f;
        [Tooltip("Small offset so the camera looks slightly ahead of her feet.")]
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f);

        private Vector3 currentVelocity;

        public void SetTarget(Transform t) => target = t;

        private void Start()
        {
            if (target != null) transform.position = DesiredPosition();
            ApplyRotation();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            ApplyRotation();
            transform.position = smoothTime <= 0f
                ? DesiredPosition()
                : Vector3.SmoothDamp(transform.position, DesiredPosition(), ref currentVelocity, smoothTime);
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private Vector3 DesiredPosition()
        {
            Vector3 focus = target.position + lookOffset;
            return focus - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distance;
        }

        private void OnValidate()
        {
            ApplyRotation();
        }
    }
}
