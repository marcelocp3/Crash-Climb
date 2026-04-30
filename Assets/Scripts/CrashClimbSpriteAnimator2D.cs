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
            idleFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Idle", 360f);
            walkFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Walking", 360f);
            attackFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Attacking", 360f);
            hurtFrames = LoadFrames("CrashClimb/Wraith_01/PNG Sequences/Hurt", 360f);

            if (spriteRenderer != null && idleFrames.Length > 0)
            {
                spriteRenderer.sprite = idleFrames[0];
            }
        }

        private Sprite[] LoadFrames(string resourcePath, float pixelsPerUnit)
        {
            Texture2D[] textures = Resources.LoadAll<Texture2D>(resourcePath);
            Array.Sort(textures, (left, right) => string.CompareOrdinal(left.name, right.name));

            Sprite[] frames = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                frames[i] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    pixelsPerUnit);
            }

            return frames;
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
