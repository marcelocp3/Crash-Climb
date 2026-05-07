using System;
using UnityEngine;

namespace CrashClimb
{
    [RequireComponent(typeof(Collider2D))]
    public class CrashClimbGoal2D : MonoBehaviour
    {
        public static event Action<CrashClimbPlayerController2D> GoalReached;

        private bool reached;

        private void Awake()
        {
            Collider2D goalCollider = GetComponent<Collider2D>();
            goalCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (reached)
            {
                return;
            }

            CrashClimbPlayerController2D player = other.GetComponentInParent<CrashClimbPlayerController2D>();
            if (player == null)
            {
                return;
            }

            reached = true;
            GoalReached?.Invoke(player);
        }
    }
}
