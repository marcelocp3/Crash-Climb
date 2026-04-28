using UnityEngine;

namespace CrashClimb
{
    public class CrashClimbCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, -10f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float minY = -2f;
        [SerializeField] private bool onlyClimbUp;

        private Vector3 velocity;
        private float highestY;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                highestY = target.position.y;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            highestY = onlyClimbUp ? Mathf.Max(highestY, target.position.y) : target.position.y;
            Vector3 desired = new Vector3(target.position.x, Mathf.Max(highestY, minY), 0f) + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
