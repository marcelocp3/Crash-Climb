using System.Collections;
using UnityEngine;

namespace CrashClimb
{
    public interface ICrashClimbDamageable
    {
        void TakeDamage(int amount, Vector2 hitPoint, Vector2 knockback);
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class CrashClimbPlayerController2D : MonoBehaviour, ICrashClimbDamageable
    {
        [Header("Movement")]
        [SerializeField] private float groundAcceleration = 55f;
        [SerializeField] private float airAcceleration = 20f;
        [SerializeField] private float maxRunSpeed = 7f;
        [SerializeField] private float groundDrag = 9f;
        [SerializeField] private float airDrag = 1.5f;

        [Header("Charged Jump")]
        [SerializeField] private float minJumpForce = 8f;
        [SerializeField] private float maxJumpForce = 16f;
        [SerializeField] private float maxChargeTime = 0.65f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Surface Effects")]
        [SerializeField] private float crystalGravityDuration = 1.3f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.72f, 0.12f);
        [SerializeField] private float groundCheckDistance = 0.08f;

        [Header("Combat")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private float attackRadius = 0.65f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackCooldown = 0.35f;
        [SerializeField] private float attackKnockback = 7f;
        [SerializeField] private LayerMask attackMask = ~0;

        [Header("Health")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invulnerabilityTime = 0.8f;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float fallDeathY = -18f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CrashClimbSpriteAnimator2D spriteAnimator;

        private Rigidbody2D rb;
        private Collider2D bodyCollider;
        private CrashClimbSurface2D currentSurface;
        private float horizontalInput;
        private float jumpChargeTime;
        private float lastGroundedTime;
        private float lastJumpPressedTime = -999f;
        private float nextAttackTime;
        private int currentHealth;
        private bool isGrounded;
        private bool isChargingJump;
        private bool isDead;
        private bool isInvulnerable;
        private float baseGravityScale;
        private float surfaceGravityMultiplier = 1f;
        private float surfaceGravityTimer;
        private Vector3 spawnPosition;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsGrounded => isGrounded;
        public float JumpCharge01 => isChargingJump ? Mathf.Clamp01(jumpChargeTime / maxChargeTime) : 0f;
        public float Height => transform.position.y;
        public string CurrentSurfaceLabel => currentSurface != null ? GetSurfaceLabel(currentSurface.Kind) : "Ar";

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            animator = animator != null ? animator : GetComponentInChildren<Animator>();
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
            spriteAnimator = spriteAnimator != null ? spriteAnimator : GetComponentInChildren<CrashClimbSpriteAnimator2D>();
            attackPoint = attackPoint != null ? attackPoint : transform.Find("Attack Point");
            currentHealth = maxHealth;
            baseGravityScale = Mathf.Approximately(rb.gravityScale, 0f) ? 3f : rb.gravityScale;
            rb.gravityScale = baseGravityScale;
            spawnPosition = respawnPoint != null ? respawnPoint.position : transform.position;
        }

        private void Update()
        {
            if (isDead)
            {
                return;
            }

            horizontalInput = GetHorizontalInput();
            UpdateGroundedState();
            HandleJumpInput();
            HandleAttackInput();
            UpdateFacing();
            UpdateAnimator();

            if (transform.position.y <= fallDeathY)
            {
                Respawn();
            }
        }

        private void FixedUpdate()
        {
            if (isDead)
            {
                return;
            }

            ApplySurfaceGravity();
            ApplyHorizontalMovement();
            ApplyDrag();
        }

        private void UpdateGroundedState()
        {
            float direction = Mathf.Sign(rb.gravityScale);
            Vector2 checkDirection = direction >= 0f ? Vector2.down : Vector2.up;
            Bounds bounds = bodyCollider.bounds;
            Vector2 checkCenter = (Vector2)bounds.center + checkDirection * (bounds.extents.y + groundCheckSize.y * 0.5f + groundCheckDistance);
            Vector2 checkSize = new Vector2(Mathf.Min(groundCheckSize.x, bounds.size.x * 0.95f), groundCheckSize.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f, groundMask);
            Collider2D groundCollider = null;

            foreach (Collider2D hit in hits)
            {
                if (hit == null || hit == bodyCollider || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                groundCollider = hit;
                break;
            }

            isGrounded = groundCollider != null;

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                currentSurface = groundCollider.GetComponentInParent<CrashClimbSurface2D>();
            }
        }

        private void HandleJumpInput()
        {
            if (JumpPressedThisFrame())
            {
                lastJumpPressedTime = Time.time;
            }

            bool hasBufferedJump = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool canJump = isGrounded || Time.time - lastGroundedTime <= coyoteTime;
            if (!isChargingJump && hasBufferedJump && canJump)
            {
                StartChargingJump();
            }

            if (isChargingJump && JumpHeld())
            {
                jumpChargeTime = Mathf.Min(jumpChargeTime + Time.deltaTime, maxChargeTime);
            }

            if (isChargingJump && JumpReleasedThisFrame())
            {
                ReleaseChargedJump();
            }
        }

        private bool JumpPressedThisFrame()
        {
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        }

        private bool JumpHeld()
        {
            return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        }

        private bool JumpReleasedThisFrame()
        {
            return Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
        }

        private float GetHorizontalInput()
        {
            return Input.GetAxisRaw("Horizontal");
        }

        private void StartChargingJump()
        {
            isChargingJump = true;
            jumpChargeTime = 0f;
            lastJumpPressedTime = -999f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        private void ReleaseChargedJump()
        {
            float surfaceJumpMultiplier = currentSurface != null ? currentSurface.JumpMultiplier : 1f;
            float charge = Mathf.Clamp01(jumpChargeTime / maxChargeTime);
            float finalJumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, charge) * surfaceJumpMultiplier;
            float gravityDirection = Mathf.Sign(rb.gravityScale);
            Vector2 jumpDirection = gravityDirection >= 0f ? Vector2.up : Vector2.down;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpDirection.y * finalJumpForce);

            if (currentSurface != null && currentSurface.Kind == CrashClimbSurfaceKind.Crystal)
            {
                surfaceGravityMultiplier = currentSurface.GravityMultiplier;
                surfaceGravityTimer = crystalGravityDuration;
            }

            isChargingJump = false;
            jumpChargeTime = 0f;
            lastGroundedTime = -999f;
        }

        private void ApplyHorizontalMovement()
        {
            float acceleration = isGrounded ? groundAcceleration : airAcceleration;
            float accelerationMultiplier = currentSurface != null && isGrounded ? currentSurface.AccelerationMultiplier : 1f;
            float speedMultiplier = currentSurface != null && isGrounded ? currentSurface.MaxSpeedMultiplier : 1f;

            rb.AddForce(Vector2.right * horizontalInput * acceleration * accelerationMultiplier);

            float maxSpeed = maxRunSpeed * speedMultiplier;
            rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed), rb.linearVelocity.y);
        }

        private void ApplyDrag()
        {
            rb.linearDamping = isGrounded && Mathf.Abs(horizontalInput) < 0.1f ? groundDrag : airDrag;
        }

        private void ApplySurfaceGravity()
        {
            if (surfaceGravityTimer > 0f)
            {
                surfaceGravityTimer -= Time.fixedDeltaTime;
            }

            float multiplier = surfaceGravityTimer > 0f ? surfaceGravityMultiplier : 1f;
            if (isGrounded && currentSurface != null && currentSurface.Kind != CrashClimbSurfaceKind.Crystal)
            {
                multiplier = currentSurface.GravityMultiplier;
            }

            rb.gravityScale = baseGravityScale * multiplier;
        }

        private void HandleAttackInput()
        {
            if (!Input.GetButtonDown("Fire1") || Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackCooldown;
            animator?.SetTrigger("Attack");
            spriteAnimator?.PlayAttack();

            Vector2 attackDirection = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
            Vector2 center = attackPoint != null ? attackPoint.position : (Vector2)transform.position + attackDirection * 0.75f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRadius, attackMask);
            foreach (Collider2D hit in hits)
            {
                if (hit == bodyCollider)
                {
                    continue;
                }

                ICrashClimbDamageable damageable = FindDamageable(hit);
                if (damageable == null)
                {
                    continue;
                }

                Vector2 direction = ((Vector2)hit.transform.position - center).normalized;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = attackDirection;
                }

                damageable.TakeDamage(attackDamage, center, direction * attackKnockback);
            }
        }

        private ICrashClimbDamageable FindDamageable(Collider2D hit)
        {
            MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is ICrashClimbDamageable damageable && !ReferenceEquals(damageable, this))
                {
                    return damageable;
                }
            }

            return null;
        }

        public void TakeDamage(int amount, Vector2 hitPoint, Vector2 knockback)
        {
            if (isDead || isInvulnerable)
            {
                return;
            }

            currentHealth -= amount;
            rb.linearVelocity = knockback;
            animator?.SetTrigger("Hurt");
            spriteAnimator?.PlayHurt();

            if (currentHealth <= 0)
            {
                Respawn();
                return;
            }

            StartCoroutine(InvulnerabilityRoutine());
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;

            while (elapsed < invulnerabilityTime)
            {
                elapsed += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }

                yield return new WaitForSeconds(0.08f);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            isInvulnerable = false;
        }

        private void Respawn()
        {
            currentHealth = maxHealth;
            isChargingJump = false;
            jumpChargeTime = 0f;
            isInvulnerable = false;
            surfaceGravityMultiplier = 1f;
            surfaceGravityTimer = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = baseGravityScale;
            transform.position = respawnPoint != null ? respawnPoint.position : spawnPosition;
            animator?.SetTrigger("Respawn");
            spriteAnimator?.PlayIdle();
        }

        public void ResetToSpawn()
        {
            StopAllCoroutines();
            Respawn();
        }

        private string GetSurfaceLabel(CrashClimbSurfaceKind surfaceKind)
        {
            switch (surfaceKind)
            {
                case CrashClimbSurfaceKind.Ice:
                    return "Gelo";
                case CrashClimbSurfaceKind.Glue:
                    return "Cola";
                case CrashClimbSurfaceKind.Crystal:
                    return "Cristal";
                case CrashClimbSurfaceKind.FragileRock:
                    return "Rocha fragil";
                default:
                    return "Pedra";
            }
        }

        private void UpdateFacing()
        {
            if (Mathf.Abs(horizontalInput) < 0.1f)
            {
                return;
            }

            float sign = Mathf.Sign(horizontalInput);
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * sign;
            transform.localScale = scale;
        }

        private void UpdateAnimator()
        {
            spriteAnimator?.SetMotion(Mathf.Abs(rb.linearVelocity.x), isGrounded, isChargingJump);

            if (animator == null)
            {
                return;
            }

            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
            animator.SetFloat("JumpCharge", JumpCharge01);
            animator.SetBool("Grounded", isGrounded);
            animator.SetBool("Charging", isChargingJump);
            animator.SetBool("Dead", isDead);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 checkCenter = transform.position;
            if (Application.isPlaying && bodyCollider != null && rb != null)
            {
                float direction = Mathf.Sign(rb.gravityScale);
                Vector2 checkDirection = direction >= 0f ? Vector2.down : Vector2.up;
                Bounds bounds = bodyCollider.bounds;
                checkCenter = (Vector2)bounds.center + checkDirection * (bounds.extents.y + groundCheckSize.y * 0.5f + groundCheckDistance);
            }

            Gizmos.DrawWireCube(checkCenter, groundCheckSize);

            Gizmos.color = Color.red;
            Vector3 attackCenter = attackPoint != null ? attackPoint.position : transform.position + transform.right * 0.75f;
            Gizmos.DrawWireSphere(attackCenter, attackRadius);
        }
    }
}
