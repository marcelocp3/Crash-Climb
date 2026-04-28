using UnityEngine;

namespace CrashClimb
{
    [RequireComponent(typeof(Collider2D))]
    public class CrashClimbSpikeHazard2D : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float knockbackHorizontal = 7f;
        [SerializeField] private float knockbackVertical = 9f;
        [SerializeField] private float hitCooldown = 0.45f;
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float pulseAmount = 0.08f;
        [SerializeField] private Animator animator;

        private float nextHitTime;
        private Vector3 baseScale;

        private void Awake()
        {
            Collider2D spikeCollider = GetComponent<Collider2D>();
            spikeCollider.isTrigger = true;
            animator = animator != null ? animator : GetComponentInChildren<Animator>();
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(baseScale.x, baseScale.y * pulse, baseScale.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            if (Time.time < nextHitTime)
            {
                return;
            }

            ICrashClimbDamageable damageable = FindDamageable(other);
            if (damageable == null)
            {
                return;
            }

            nextHitTime = Time.time + hitCooldown;
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            float horizontalDirection = Mathf.Sign(other.transform.position.x - transform.position.x);
            if (Mathf.Approximately(horizontalDirection, 0f))
            {
                horizontalDirection = 1f;
            }

            Vector2 knockback = new Vector2(horizontalDirection * knockbackHorizontal, knockbackVertical);
            damageable.TakeDamage(damage, hitPoint, knockback);
            animator?.SetTrigger("Hit");
        }

        private ICrashClimbDamageable FindDamageable(Collider2D other)
        {
            MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ICrashClimbDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }
    }
}
