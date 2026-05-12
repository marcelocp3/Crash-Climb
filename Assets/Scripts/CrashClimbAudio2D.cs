using UnityEngine;

namespace CrashClimb
{
    public class CrashClimbAudio2D : MonoBehaviour
    {
        private static CrashClimbAudio2D instance;

        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioClip mainMenuMusic;
        private AudioClip gameplayMusic;
        private AudioClip platformBreakClip;
        private AudioClip normalJumpClip;
        private AudioClip iceLandingClip;
        private AudioClip glueLandingClip;
        private AudioClip damageClip;

        public static void EnsureExists()
        {
            if (instance != null)
            {
                return;
            }

            CrashClimbAudio2D existing = Object.FindFirstObjectByType<CrashClimbAudio2D>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject audioObject = new GameObject("Crash Climb Audio");
            audioObject.AddComponent<CrashClimbAudio2D>();
        }

        public static void PlayMainMenuMusic()
        {
            EnsureExists();
            instance?.PlayMusic(instance.mainMenuMusic);
        }

        public static void PlayGameplayMusic()
        {
            EnsureExists();
            instance?.PlayMusic(instance.gameplayMusic);
        }

        public static void PlayPlatformBreak()
        {
            EnsureExists();
            instance?.PlaySfx(instance.platformBreakClip);
        }

        public static void PlayJump(CrashClimbSurfaceKind surfaceKind)
        {
            if (surfaceKind != CrashClimbSurfaceKind.Stone && surfaceKind != CrashClimbSurfaceKind.FragileRock)
            {
                return;
            }

            EnsureExists();
            instance?.PlaySfx(instance.normalJumpClip);
        }

        public static void PlayLanding(CrashClimbSurfaceKind surfaceKind)
        {
            EnsureExists();

            if (surfaceKind == CrashClimbSurfaceKind.Ice)
            {
                instance?.PlaySfx(instance.iceLandingClip);
            }
            else if (surfaceKind == CrashClimbSurfaceKind.Glue)
            {
                instance?.PlaySfx(instance.glueLandingClip);
            }
        }

        public static void PlayDamage()
        {
            EnsureExists();
            instance?.PlaySfx(instance.damageClip);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadClips();
            ConfigureSources();
        }

        private void LoadClips()
        {
            mainMenuMusic = Resources.Load<AudioClip>("CrashClimb/Audio/musica-main-menu");
            gameplayMusic = Resources.Load<AudioClip>("CrashClimb/Audio/musica-jogo");
            platformBreakClip = Resources.Load<AudioClip>("CrashClimb/Audio/plataforma-quebrando");
            normalJumpClip = Resources.Load<AudioClip>("CrashClimb/Audio/pulo-normal");
            iceLandingClip = Resources.Load<AudioClip>("CrashClimb/Audio/pulo-pouso-gelo");
            glueLandingClip = Resources.Load<AudioClip>("CrashClimb/Audio/pulo-pouso-gosma");
            damageClip = Resources.Load<AudioClip>("CrashClimb/Audio/tomou-dano");
        }

        private void ConfigureSources()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }

        private void PlayMusic(AudioClip clip)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
