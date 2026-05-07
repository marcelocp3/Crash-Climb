using System.Collections;
using UnityEngine;

namespace CrashClimb
{
    public enum CrashClimbSurfaceKind
    {
        Stone,
        Ice,
        Glue,
        Crystal,
        FragileRock
    }

    [RequireComponent(typeof(Collider2D))]
    public class CrashClimbSurface2D : MonoBehaviour
    {
        [SerializeField] private CrashClimbSurfaceKind kind = CrashClimbSurfaceKind.Stone;
        [SerializeField] private float jumpMultiplier = 1f;
        [SerializeField] private float accelerationMultiplier = 1f;
        [SerializeField] private float maxSpeedMultiplier = 1f;
        [SerializeField] private float gravityMultiplier = 1f;
        [SerializeField] private float breakDelay = 0.35f;
        [SerializeField] private float respawnDelay = 2.5f;

        private Collider2D surfaceCollider;
        private SpriteRenderer spriteRenderer;
        private bool breaking;

        public CrashClimbSurfaceKind Kind => kind;
        public float JumpMultiplier => jumpMultiplier;
        public float AccelerationMultiplier => accelerationMultiplier;
        public float MaxSpeedMultiplier => maxSpeedMultiplier;
        public float GravityMultiplier => gravityMultiplier;

        private void Awake()
        {
            surfaceCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyPreset();
        }

        private void OnValidate()
        {
            ApplyPreset();
        }

        private void ApplyPreset()
        {
            switch (kind)
            {
                case CrashClimbSurfaceKind.Stone:
                    jumpMultiplier = 1f;
                    accelerationMultiplier = 1f;
                    maxSpeedMultiplier = 1f;
                    gravityMultiplier = 1f;
                    breakDelay = 0.35f;
                    break;
                case CrashClimbSurfaceKind.Ice:
                    jumpMultiplier = 1f;
                    accelerationMultiplier = 0.38f;
                    maxSpeedMultiplier = 1.35f;
                    gravityMultiplier = 1f;
                    breakDelay = 0.35f;
                    break;
                case CrashClimbSurfaceKind.Glue:
                    jumpMultiplier = 0.82f;
                    accelerationMultiplier = 0.8f;
                    maxSpeedMultiplier = 0.82f;
                    gravityMultiplier = 1f;
                    breakDelay = 0.35f;
                    break;
                case CrashClimbSurfaceKind.Crystal:
                    jumpMultiplier = 1.18f;
                    accelerationMultiplier = 1f;
                    maxSpeedMultiplier = 1f;
                    gravityMultiplier = 0.45f;
                    breakDelay = 0.35f;
                    break;
                case CrashClimbSurfaceKind.FragileRock:
                    jumpMultiplier = 1f;
                    accelerationMultiplier = 0.9f;
                    maxSpeedMultiplier = 0.9f;
                    gravityMultiplier = 1f;
                    breakDelay = 3f;
                    break;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (kind != CrashClimbSurfaceKind.FragileRock || breaking)
            {
                return;
            }

            if (collision.collider.GetComponentInParent<CrashClimbPlayerController2D>() != null)
            {
                StartCoroutine(BreakAndRespawn());
            }
        }

        private IEnumerator BreakAndRespawn()
        {
            breaking = true;
            yield return new WaitForSeconds(breakDelay);

            if (surfaceCollider != null)
            {
                surfaceCollider.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            yield return new WaitForSeconds(respawnDelay);

            if (surfaceCollider != null)
            {
                surfaceCollider.enabled = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            breaking = false;
        }

        public void Configure(CrashClimbSurfaceKind newKind)
        {
            kind = newKind;
            ApplyPreset();
        }
    }
}
