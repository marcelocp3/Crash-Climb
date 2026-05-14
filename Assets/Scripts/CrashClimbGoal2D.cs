using System;
using UnityEngine;

namespace CrashClimb
{
    [RequireComponent(typeof(Collider2D))]
    public class CrashClimbGoal2D : MonoBehaviour
    {
        public static event Action<CrashClimbPlayerController2D> GoalReached;

        private bool reached;
        private float contactTimer;
        private CrashClimbPlayerController2D standingPlayer;

        private void Awake()
        {
            // Force the collider to be physical (NOT a trigger) so the player can land on it.
            Collider2D goalCollider = GetComponent<Collider2D>();
            goalCollider.isTrigger = false;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (reached)
            {
                return;
            }

            CrashClimbPlayerController2D player = collision.collider.GetComponentInParent<CrashClimbPlayerController2D>();
            if (player == null)
            {
                return;
            }

            // Only count time if the player is standing ON TOP
            bool isOnTop = false;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    isOnTop = true;
                    break;
                }
            }

            if (!isOnTop)
            {
                contactTimer = 0f;
                standingPlayer = null;
                return;
            }

            standingPlayer = player;
            contactTimer += Time.deltaTime;

            if (contactTimer >= 1f)
            {
                reached = true;
                Debug.Log($"Victory! Player stood on '{gameObject.name}' for 1 second.");
                GoalReached?.Invoke(player);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            CrashClimbPlayerController2D player = collision.collider.GetComponentInParent<CrashClimbPlayerController2D>();
            if (player != null && player == standingPlayer)
            {
                contactTimer = 0f;
                standingPlayer = null;
            }
        }
    }
}
