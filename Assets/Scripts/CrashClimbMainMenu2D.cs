using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CrashClimb
{
    public class CrashClimbMainMenu2D : MonoBehaviour
    {
        private enum MenuState
        {
            Main,
            Options,
            Credits,
            Playing,
            Victory,
            GameOver
        }

        private static CrashClimbMainMenu2D instance;
        private static Texture2D whiteTexture;
        private const string MainMenuSceneName = "MainMenu";
        private const string GameplaySceneName = "Main";
        private const string GameCompleteSceneName = "GameComplete";
        private static readonly Rect MenuButtonTextureCrop = new Rect(0.06f, 0.27f, 0.88f, 0.48f);
        private static bool gameplayRequested;
        private bool wasPlaying;

        [SerializeField] private MenuState state = MenuState.Main;
        [SerializeField] private bool pauseOnStart = true;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallStyle;
        private Texture2D menuBackgroundTexture;
        private Texture2D skyTexture;
        private Texture2D backgroundTexture;
        private Texture2D heroTexture;
        private Texture2D platformTexture;
        private Texture2D playButtonTexture;
        private Texture2D creditsButtonTexture;
        private Texture2D exitButtonTexture;
        private Texture2D deathBackgroundTexture;
        private Texture2D retryButtonTexture;
        private Texture2D homeButtonTexture;
        private Texture2D victoryBackgroundTexture;
        private Texture2D victoryRetryButtonTexture;
        private Texture2D victoryHomeButtonTexture;
        private Sprite platformSprite;

        public static bool IsBlockingHud => instance != null && instance.state != MenuState.Playing;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadTextures();
            EnsureWhiteTexture();
            ApplySceneState(SceneManager.GetActiveScene().name);
        }

        private void OnEnable()
        {
            CrashClimbGoal2D.GoalReached += ShowVictory;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            CrashClimbGoal2D.GoalReached -= ShowVictory;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnGUI()
        {
            CreateStylesIfNeeded();

            if (state == MenuState.Playing)
            {
                DrawPauseHint();
                return;
            }

            DrawBackdrop();
            DrawJumpScene();

            Rect screenPanel = new Rect(0f, 0f, Screen.width, Screen.height);
            float overlayAlpha = menuBackgroundTexture != null ? 0.08f : 0.42f;
            DrawRect(screenPanel, new Color(0.025f, 0.03f, 0.035f, overlayAlpha));
            DrawRect(new Rect(0f, 0f, Screen.width, 4f), new Color(0.75f, 0.32f, 1f, 0.95f));

            Rect panel = new Rect(0f, 0f, Screen.width, Screen.height);
            if (state == MenuState.Victory)
            {
                DrawVictory(panel);
            }
            else if (state == MenuState.Options)
            {
                DrawOptions(panel);
            }
            else if (state == MenuState.Credits)
            {
                DrawCredits(panel);
            }
            else if (state == MenuState.GameOver)
            {
                DrawGameOver(panel);
            }
            else
            {
                DrawMain(panel);
            }
        }

        private void Update()
        {
            if (state == MenuState.Playing && Input.GetKeyDown(KeyCode.Escape))
            {
                state = MenuState.Main;
                wasPlaying = true;
                Time.timeScale = 0f;
            }
        }

        private void DrawMain(Rect panel)
        {
            bool hasFinalBackground = menuBackgroundTexture != null;
            bool hasMenuButtonSprites = playButtonTexture != null && creditsButtonTexture != null && exitButtonTexture != null;
            float titleY = Mathf.Max(36f, Screen.height * 0.12f);
            if (!hasFinalBackground)
            {
                GUI.Label(new Rect(0f, titleY, Screen.width, 82f), "Crash&Climb", titleStyle);
            }

            float buttonWidth = hasMenuButtonSprites ? Mathf.Clamp(Screen.width * 0.34f, 230f, 320f) : Mathf.Min(360f, Screen.width - 56f);
            if (hasMenuButtonSprites)
            {
                buttonWidth = Mathf.Min(buttonWidth, Screen.width - 72f);
            }

            float buttonHeight = hasMenuButtonSprites ? Mathf.Clamp(buttonWidth * 0.26f, 60f, 84f) : Mathf.Clamp(Screen.height * 0.13f, 58f, 76f);
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            float gap = hasMenuButtonSprites ? Mathf.Clamp(Screen.height * 0.032f, 14f, 24f) : Mathf.Clamp(Screen.height * 0.025f, 10f, 18f);
            float totalButtonHeight = buttonHeight * 3f + gap * 2f;
            float targetButtonY = hasFinalBackground ? Screen.height * 0.405f : titleY + 104f;
            float minButtonY = hasFinalBackground ? Mathf.Max(132f, Screen.height * 0.35f) : titleY + 82f;
            float maxButtonY = Mathf.Max(minButtonY, Screen.height - totalButtonHeight - 24f);
            float buttonY = Mathf.Clamp(targetButtonY, minButtonY, maxButtonY);

            if (DrawMenuImageButton(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), playButtonTexture, "JOGAR"))
            {
                if (wasPlaying)
                {
                    // Resume from ESC pause — don't touch the player
                    wasPlaying = false;
                    gameplayRequested = true;
                    state = MenuState.Playing;
                    Time.timeScale = 1f;
                    CrashClimbAudio2D.PlayGameplayMusic();
                }
                else
                {
                    StartGame();
                }
            }

            if (DrawMenuImageButton(new Rect(buttonX, buttonY + (buttonHeight + gap), buttonWidth, buttonHeight), creditsButtonTexture, "CRÉDITOS"))
            {
                state = MenuState.Credits;
            }

            if (DrawMenuImageButton(new Rect(buttonX, buttonY + (buttonHeight + gap) * 2f, buttonWidth, buttonHeight), exitButtonTexture, "SAIR"))
            {
                QuitGame();
            }
        }

        private void DrawOptions(Rect panel)
        {
            float titleY = Mathf.Max(46f, Screen.height * 0.16f);
            GUI.Label(new Rect(0f, titleY, Screen.width, 64f), "OPÇÕES", titleStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 220f, titleY + 82f, 440f, 70f), "Volume, controles e outras configuracoes podem ser expandidos aqui.", subtitleStyle);

            float buttonWidth = Mathf.Min(300f, Screen.width - 56f);
            if (DrawPlatformButton(new Rect((Screen.width - buttonWidth) * 0.5f, titleY + 180f, buttonWidth, 68f), "VOLTAR"))
            {
                state = MenuState.Main;
            }
        }

        private void DrawCredits(Rect panel)
        {
            float titleY = Mathf.Max(46f, Screen.height * 0.16f);
            GUI.Label(new Rect(0f, titleY, Screen.width, 64f), "CRÉDITOS", titleStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 240f, titleY + 82f, 480f, 84f), "Crash&Climb\nMarcelo da Costa Poltronieri e Raymond Lisbona", subtitleStyle);

            float buttonWidth = Mathf.Min(300f, Screen.width - 56f);
            if (DrawPlatformButton(new Rect((Screen.width - buttonWidth) * 0.5f, titleY + 194f, buttonWidth, 68f), "VOLTAR"))
            {
                state = MenuState.Main;
            }
        }

        private void DrawGameOver(Rect panel)
        {
            bool hasMenuButtonSprites = retryButtonTexture != null && homeButtonTexture != null;
            
            // Even larger buttons to fill the screen better
            float buttonWidth = hasMenuButtonSprites ? Mathf.Clamp(Screen.width * 0.68f, 440f, 700f) : Mathf.Min(360f, Screen.width - 56f);
            if (hasMenuButtonSprites)
            {
                buttonWidth = Mathf.Min(buttonWidth, Screen.width - 48f);
            }

            float buttonHeight = hasMenuButtonSprites ? Mathf.Clamp(buttonWidth * 0.48f, 160f, 260f) : Mathf.Clamp(Screen.height * 0.13f, 58f, 76f);
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            
            // Even more negative gap to overcome large sprite padding
            float gap = hasMenuButtonSprites ? -buttonHeight * 0.45f : 8f; 
            
            float totalButtonHeight = buttonHeight * 2f + gap;
            float targetButtonY = Screen.height * 0.22f;
            float minButtonY = Screen.height * 0.08f;
            float maxButtonY = Mathf.Max(minButtonY, Screen.height - totalButtonHeight - 12f);
            float buttonY = Mathf.Clamp(targetButtonY, minButtonY, maxButtonY);

            if (DrawSimpleImageButton(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), retryButtonTexture, "TENTAR NOVAMENTE"))
            {
                ResumeOrRestartGame();
            }

            if (DrawSimpleImageButton(new Rect(buttonX, buttonY + buttonHeight + gap, buttonWidth, buttonHeight), homeButtonTexture, "MENU INICIAL"))
            {
                LoadMainMenu();
            }
        }

        private void DrawVictory(Rect panel)
        {
            bool hasMenuButtonSprites = victoryRetryButtonTexture != null && victoryHomeButtonTexture != null;
            
            float buttonWidth = hasMenuButtonSprites ? Mathf.Clamp(Screen.width * 0.68f, 440f, 700f) : Mathf.Min(360f, Screen.width - 56f);
            if (hasMenuButtonSprites)
            {
                buttonWidth = Mathf.Min(buttonWidth, Screen.width - 48f);
            }

            float buttonHeight = hasMenuButtonSprites ? Mathf.Clamp(buttonWidth * 0.48f, 160f, 260f) : Mathf.Clamp(Screen.height * 0.13f, 58f, 76f);
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            
            float gap = hasMenuButtonSprites ? -buttonHeight * 0.45f : 8f; 
            
            float totalButtonHeight = buttonHeight * 2f + gap;
            float targetButtonY = Screen.height * 0.07f;
            float minButtonY = Screen.height * 0.02f;
            float maxButtonY = Mathf.Max(minButtonY, Screen.height - totalButtonHeight - 8f);
            float buttonY = Mathf.Clamp(targetButtonY, minButtonY, maxButtonY);

            if (DrawSimpleImageButton(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), victoryRetryButtonTexture, "JOGAR NOVAMENTE"))
            {
                ResumeOrRestartGame();
            }

            if (DrawSimpleImageButton(new Rect(buttonX, buttonY + buttonHeight + gap, buttonWidth, buttonHeight), victoryHomeButtonTexture, "MENU INICIAL"))
            {
                LoadMainMenu();
            }
        }

        public static void ShowGameOver()
        {
            if (instance != null)
            {
                instance.state = MenuState.GameOver;
                Time.timeScale = 0f;
                CrashClimbAudio2D.PlayMainMenuMusic();
                gameplayRequested = false;
            }
        }

        private void DrawPauseHint()
        {
            Rect hint = new Rect(Screen.width - 166f, 12f, 154f, 24f);
            DrawRect(hint, new Color(0.03f, 0.04f, 0.05f, 0.65f));
            GUI.Label(new Rect(hint.x + 10f, hint.y + 4f, hint.width - 20f, 18f), "Esc abre o menu", smallStyle);
        }

        private void DrawBackdrop()
        {
            Texture2D backdrop = null;
            if (state == MenuState.GameOver) backdrop = deathBackgroundTexture;
            else if (state == MenuState.Victory) backdrop = victoryBackgroundTexture;
            
            if (backdrop == null)
            {
                backdrop = menuBackgroundTexture != null ? menuBackgroundTexture : skyTexture != null ? skyTexture : backgroundTexture;
            }

            if (backdrop != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backdrop, ScaleMode.ScaleAndCrop);
            }
            else
            {
                DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.05f, 0.07f, 0.09f, 1f));
            }

            float vignetteAlpha = menuBackgroundTexture != null ? 0.04f : 0.18f;
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, vignetteAlpha));
        }

        private bool DrawPlatformButton(Rect rect, string label)
        {
            Rect shadowRect = new Rect(rect.x + 6f, rect.y + 8f, rect.width, rect.height);
            DrawPlatformButtonTexture(shadowRect, new Color(0f, 0f, 0f, 0.26f));

            bool isHovering = rect.Contains(Event.current.mousePosition);
            DrawPlatformButtonTexture(rect, isHovering ? new Color(1f, 1f, 0.94f, 1f) : Color.white);

            Color previousTextColor = buttonStyle.normal.textColor;
            buttonStyle.normal.textColor = new Color(0.06f, 0.06f, 0.055f, 0.9f);
            GUI.Label(new Rect(rect.x + 8f, rect.y + rect.height * 0.14f + 3f, rect.width - 12f, rect.height * 0.52f), label, buttonStyle);
            buttonStyle.normal.textColor = previousTextColor;
            GUI.Label(new Rect(rect.x + 6f, rect.y + rect.height * 0.14f, rect.width - 12f, rect.height * 0.52f), label, buttonStyle);

            Color previousColor = GUI.color;
            GUI.color = Color.clear;
            bool clicked = GUI.Button(rect, GUIContent.none);
            GUI.color = previousColor;
            return clicked;
        }

        private bool DrawMenuImageButton(Rect rect, Texture2D texture, string fallbackLabel)
        {
            if (texture == null)
            {
                return DrawPlatformButton(rect, fallbackLabel);
            }

            Color previousColor = GUI.color;
            bool isHovering = rect.Contains(Event.current.mousePosition);
            GUI.color = isHovering ? new Color(1f, 1f, 0.93f, 1f) : Color.white;
            GUI.DrawTextureWithTexCoords(rect, texture, MenuButtonTextureCrop, true);
            GUI.color = previousColor;

            previousColor = GUI.color;
            GUI.color = Color.clear;
            bool clicked = GUI.Button(rect, GUIContent.none);
            GUI.color = previousColor;
            return clicked;
        }

        private bool DrawSimpleImageButton(Rect rect, Texture2D texture, string fallbackLabel)
        {
            if (texture == null)
            {
                return DrawPlatformButton(rect, fallbackLabel);
            }

            Color previousColor = GUI.color;
            bool isHovering = rect.Contains(Event.current.mousePosition);
            GUI.color = isHovering ? new Color(1f, 1f, 0.93f, 1f) : Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;

            previousColor = GUI.color;
            GUI.color = Color.clear;
            bool clicked = GUI.Button(rect, GUIContent.none);
            GUI.color = previousColor;
            return clicked;
        }

        private void DrawPlatformButtonTexture(Rect rect, Color tint)
        {
            Color previousColor = GUI.color;
            GUI.color = tint;

            if (platformTexture != null)
            {
                GUI.DrawTexture(rect, platformTexture, ScaleMode.StretchToFill, true);
            }
            else if (platformSprite != null && platformSprite.texture != null)
            {
                GUI.DrawTexture(rect, platformSprite.texture, ScaleMode.StretchToFill, true);
            }
            else
            {
                DrawPlatform(rect);
            }

            GUI.color = previousColor;
        }

        private void DrawJumpScene()
        {
            if (menuBackgroundTexture != null || heroTexture == null || Screen.width < 520)
            {
                return;
            }

            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            float centerX = Screen.width * 0.5f;
            float baseY = Screen.height * 0.68f;
            float platformWidth = Mathf.Min(230f, Screen.width * 0.24f);
            float platformHeight = platformWidth * 0.24f;

            GUI.color = new Color(1f, 1f, 1f, 0.48f);
            DrawPlatform(new Rect(centerX - platformWidth - 118f, baseY + 26f, platformWidth, platformHeight));
            DrawPlatform(new Rect(centerX + 116f, baseY - 86f, platformWidth, platformHeight));

            float heroHeight = Mathf.Min(Screen.height * 0.44f, 300f);
            float heroWidth = heroHeight * heroTexture.width / heroTexture.height;
            GUI.color = new Color(1f, 1f, 1f, 0.26f);
            GUIUtility.RotateAroundPivot(-12f, new Vector2(centerX, baseY - 52f));
            GUI.DrawTexture(new Rect(centerX - heroWidth * 0.5f, baseY - heroHeight - 12f, heroWidth, heroHeight), heroTexture, ScaleMode.ScaleToFit, true);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void DrawPlatform(Rect rect)
        {
            if (platformTexture != null)
            {
                GUI.DrawTexture(rect, platformTexture, ScaleMode.ScaleToFit, true);
                return;
            }

            if (platformSprite != null && platformSprite.texture != null)
            {
                GUI.DrawTexture(rect, platformSprite.texture, ScaleMode.ScaleToFit, true);
                return;
            }

            DrawRect(rect, new Color(0.26f, 0.22f, 0.18f, 0.42f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, Mathf.Max(4f, rect.height * 0.22f)), new Color(0.6f, 0.52f, 0.4f, 0.5f));
        }

        private void StartGame()
        {
            gameplayRequested = true;

            if (SceneManager.GetActiveScene().name != GameplaySceneName)
            {
                state = MenuState.Playing;
                Time.timeScale = 1f;
                CrashClimbAudio2D.PlayGameplayMusic();
                SceneManager.LoadScene(GameplaySceneName);
                return;
            }

            state = MenuState.Playing;
            Time.timeScale = 1f;
            CrashClimbAudio2D.PlayGameplayMusic();
        }

        private void ResumeOrRestartGame()
        {
            gameplayRequested = true;

            if (SceneManager.GetActiveScene().name != GameplaySceneName)
            {
                state = MenuState.Playing;
                Time.timeScale = 1f;
                CrashClimbAudio2D.PlayGameplayMusic();
                SceneManager.LoadScene(GameplaySceneName);
                return;
            }

            // Reset the player to spawn without rebuilding the map
            CrashClimbPlayerController2D player = Object.FindFirstObjectByType<CrashClimbPlayerController2D>();
            player?.ResetToSpawn();

            state = MenuState.Playing;
            Time.timeScale = 1f;
            CrashClimbAudio2D.PlayGameplayMusic();
        }

        private void ShowVictory(CrashClimbPlayerController2D player)
        {
            gameplayRequested = false;
            state = MenuState.Victory;
            Time.timeScale = 0f;
            CrashClimbAudio2D.PlayMainMenuMusic();

            if (SceneManager.GetActiveScene().name != GameCompleteSceneName)
            {
                SceneManager.LoadScene(GameCompleteSceneName);
            }
        }

        private void LoadMainMenu()
        {
            gameplayRequested = false;
            state = MenuState.Main;
            Time.timeScale = 0f;
            CrashClimbAudio2D.PlayMainMenuMusic();

            if (SceneManager.GetActiveScene().name != MainMenuSceneName)
            {
                SceneManager.LoadScene(MainMenuSceneName);
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneState(scene.name);
        }

        private void ApplySceneState(string sceneName)
        {
            if (sceneName == GameplaySceneName)
            {
                state = gameplayRequested ? MenuState.Playing : MenuState.Main;
                Time.timeScale = gameplayRequested ? 1f : 0f;
                if (gameplayRequested)
                {
                    CrashClimbAudio2D.PlayGameplayMusic();
                }
                else
                {
                    CrashClimbAudio2D.PlayMainMenuMusic();
                }
                return;
            }

            if (sceneName == GameCompleteSceneName)
            {
                state = MenuState.Victory;
                Time.timeScale = 0f;
                CrashClimbAudio2D.PlayMainMenuMusic();
                return;
            }

            state = MenuState.Main;
            CrashClimbAudio2D.PlayMainMenuMusic();
            if (pauseOnStart)
            {
                Time.timeScale = 0f;
            }
        }

        private void LoadTextures()
        {
            menuBackgroundTexture = Resources.Load<Texture2D>("CrashClimb/Menu/MenuBackground");
            skyTexture = Resources.Load<Texture2D>("CrashClimb/Background/Game_Background_1/Sky");
            backgroundTexture = Resources.Load<Texture2D>("CrashClimb/Background/Game_Background_1/BackGround");
            heroTexture = Resources.Load<Texture2D>("CrashClimb/Wraith_01/PNG Sequences/Idle/Wraith_01_Idle_000");
            platformTexture = Resources.Load<Texture2D>("CrashClimb/Pads/Pad_1_1");
            playButtonTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu");
            creditsButtonTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu-creditos");
            exitButtonTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu-sair");
            deathBackgroundTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu-morte");
            retryButtonTexture = Resources.Load<Texture2D>("CrashClimb/Menu/meno-morte-rejogar");
            homeButtonTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu-morte-inicio");
            victoryBackgroundTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu-vitoria");
            victoryRetryButtonTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu-vitoria-rejogar");
            victoryHomeButtonTexture = Resources.Load<Texture2D>("CrashClimb/Menu/menu-vitoria-inicio");
            platformSprite = Resources.Load<Sprite>("CrashClimb/Pads/Pad_1_1");
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureWhiteTexture()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }
        }

        private void CreateStylesIfNeeded()
        {
            EnsureWhiteTexture();

            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.082f), 38, 76),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.42f, 1f, 0.84f) }
            };
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = new Color(0.86f, 0.9f, 0.92f) }
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.78f, 0.84f, 0.88f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previousColor;
        }
    }
}
