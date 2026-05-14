using System;
using UnityEngine;

namespace CrashClimb
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CrashClimbSpriteAnimator2D : MonoBehaviour
    {
        private enum AnimationState
        {
            Idle,
            Walk,
            Attack,
            Hurt
        }

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float frameRate = 14f;
        [SerializeField] private float movingThreshold = 0.15f;

        // Keep each frame's visible foot line anchored to the first idle frame.
        private const float ReferencePivotY = 0.5f;
        private const float ReferenceBottomPaddingPixels = 94f;
        private static readonly int[] IdleBottomPaddingPixels = { 94, 93, 94, 95, 96, 97, 98, 99, 98, 97, 96, 95 };
        private static readonly int[] WalkBottomPaddingPixels = { 100, 101, 101, 102, 102, 103, 103, 103, 102, 102, 101, 101 };
        private static readonly int[] AttackBottomPaddingPixels = { 94, 98, 103, 107, 107, 107, 107, 100, 89, 85, 87, 90 };
        private static readonly int[] HurtBottomPaddingPixels = { 94, 84, 83, 88, 94, 100, 100, 100, 100, 100, 98, 96 };
        private Sprite[] idleFrames = Array.Empty<Sprite>();
        private Sprite[] walkFrames = Array.Empty<Sprite>();
        private Sprite[] attackFrames = Array.Empty<Sprite>();
        private Sprite[] hurtFrames = Array.Empty<Sprite>();
        private AnimationState state = AnimationState.Idle;
        private float frameTimer;
        private int frameIndex;
        private bool oneShotPlaying;
        private float horizontalSpeed;

        private void Awake()
        {
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            LoadWraithFrames();
            PlayLoop(AnimationState.Idle);
        }

        private void Update()
        {
            Sprite[] frames = GetFrames(state);
            if (frames.Length == 0)
            {
                return;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, frameRate);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                AdvanceFrame(frames);
            }
        }

        public void SetMotion(float speed, bool grounded, bool charging)
        {
            horizontalSpeed = speed;

            if (oneShotPlaying)
            {
                return;
            }

            PlayLoop(grounded && !charging && horizontalSpeed > movingThreshold ? AnimationState.Walk : AnimationState.Idle);
        }

        public void PlayAttack()
        {
            PlayOneShot(AnimationState.Attack);
        }

        public void PlayHurt()
        {
            PlayOneShot(AnimationState.Hurt);
        }

        public void PlayIdle()
        {
            oneShotPlaying = false;
            PlayLoop(AnimationState.Idle);
        }

        private void LoadWraithFrames()
        {
            idleFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Idle", 360f, IdleBottomPaddingPixels);
            walkFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Walking", 360f, WalkBottomPaddingPixels);
            attackFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Attacking", 360f, AttackBottomPaddingPixels);
            hurtFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Hurt", 360f, HurtBottomPaddingPixels);

            if (spriteRenderer != null && idleFrames.Length > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private Sprite[] LoadFrames(string resourcePath, float pixelsPerUnit, int[] bottomPaddingPixels)
        {
            Texture2D[] textures = Resources.LoadAll<Texture2D>(resourcePath);
            Array.Sort(textures, (left, right) => string.CompareOrdinal(left.name, right.name));

            Sprite[] frames = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                Vector2 framePivot = new Vector2(0.5f, GetFramePivotY(i, bottomPaddingPixels, texture.height));
                frames[i] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    framePivot,
                    pixelsPerUnit);
            }

            return frames;
        }

        private float GetFramePivotY(int frameIndex, int[] bottomPaddingPixels, int textureHeight)
        {
            if (bottomPaddingPixels == null || frameIndex >= bottomPaddingPixels.Length || textureHeight <= 0)
            {
                return ReferencePivotY;
            }

            float referenceBottomLocalPixels = ReferenceBottomPaddingPixels - ReferencePivotY * textureHeight;
            return Mathf.Clamp01((bottomPaddingPixels[frameIndex] - referenceBottomLocalPixels) / textureHeight);
        }

        private void PlayLoop(AnimationState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
        }

        private void PlayOneShot(AnimationState nextState)
        {
            Sprite[] frames = GetFrames(nextState);
            if (frames.Length == 0)
            {
                return;
            }

            state = nextState;
            frameIndex = 0;
            frameTimer = 0f;
            oneShotPlaying = true;
            ApplyCurrentFrame();
        }

        private void AdvanceFrame(Sprite[] frames)
        {
            frameIndex++;
            if (frameIndex >= frames.Length)
            {
                if (oneShotPlaying)
                {
                    oneShotPlaying = false;
                    PlayLoop(horizontalSpeed > movingThreshold ? AnimationState.Walk : AnimationState.Idle);
                    return;
                }

                frameIndex = 0;
            }

            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            Sprite[] frames = GetFrames(state);
            if (spriteRenderer != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
            }
        }

        private Sprite[] GetFrames(AnimationState animationState)
        {
            switch (animationState)
            {
                case AnimationState.Walk:
                    return walkFrames.Length > 0 ? walkFrames : idleFrames;
                case AnimationState.Attack:
                    return attackFrames;
                case AnimationState.Hurt:
                    return hurtFrames;
                default:
                    return idleFrames;
            }
        }
    }
}
