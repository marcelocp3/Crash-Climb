using UnityEngine;

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
            Victory
        }

        private static CrashClimbMainMenu2D instance;
        private static Texture2D whiteTexture;

        [SerializeField] private MenuState state = MenuState.Main;
        [SerializeField] private bool pauseOnStart = true;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallStyle;
        private Texture2D skyTexture;
        private Texture2D backgroundTexture;
        private Texture2D heroTexture;
        private Texture2D platformTexture;
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

            if (pauseOnStart)
            {
                Time.timeScale = 0f;
            }
        }

        private void OnEnable()
        {
            CrashClimbGoal2D.GoalReached += ShowVictory;
        }

        private void OnDisable()
        {
            CrashClimbGoal2D.GoalReached -= ShowVictory;
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
            DrawRect(screenPanel, new Color(0.025f, 0.03f, 0.035f, 0.42f));
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
                Time.timeScale = 0f;
            }
        }

        private void DrawMain(Rect panel)
        {
            float titleY = Mathf.Max(36f, Screen.height * 0.12f);
            GUI.Label(new Rect(0f, titleY, Screen.width, 82f), "Crash&Climb", titleStyle);

            float buttonWidth = Mathf.Min(360f, Screen.width - 56f);
            float buttonHeight = Mathf.Clamp(Screen.height * 0.13f, 58f, 76f);
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            float gap = Mathf.Clamp(Screen.height * 0.025f, 10f, 18f);
            float totalButtonHeight = buttonHeight * 4f + gap * 3f;
            float buttonY = Mathf.Min(titleY + 104f, Screen.height - totalButtonHeight - 24f);
            buttonY = Mathf.Max(titleY + 82f, buttonY);

            if (DrawPlatformButton(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "JOGAR"))
            {
                StartGame(false);
            }

            if (DrawPlatformButton(new Rect(buttonX, buttonY + (buttonHeight + gap), buttonWidth, buttonHeight), "OPÇÕES"))
            {
                state = MenuState.Options;
            }

            if (DrawPlatformButton(new Rect(buttonX, buttonY + (buttonHeight + gap) * 2f, buttonWidth, buttonHeight), "CRÉDITOS"))
            {
                state = MenuState.Credits;
            }

            if (DrawPlatformButton(new Rect(buttonX, buttonY + (buttonHeight + gap) * 3f, buttonWidth, buttonHeight), "SAIR"))
            {
                Application.Quit();
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

        private void DrawVictory(Rect panel)
        {
            float titleY = Mathf.Max(46f, Screen.height * 0.14f);
            GUI.Label(new Rect(0f, titleY, Screen.width, 74f), "TOPO ALCANÇADO", titleStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 245f, titleY + 88f, 490f, 62f), "A torre completa foi vencida. O level design termina na plataforma de cristal final.", subtitleStyle);

            float buttonWidth = Mathf.Min(340f, Screen.width - 56f);
            float buttonX = (Screen.width - buttonWidth) * 0.5f;
            float buttonY = titleY + 178f;
            if (DrawPlatformButton(new Rect(buttonX, buttonY, buttonWidth, 72f), "JOGAR NOVAMENTE"))
            {
                StartGame(true);
            }

            if (DrawPlatformButton(new Rect(buttonX, buttonY + 94f, buttonWidth, 72f), "VOLTAR AO MENU"))
            {
                state = MenuState.Main;
                Time.timeScale = 0f;
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
            Texture2D backdrop = skyTexture != null ? skyTexture : backgroundTexture;
            if (backdrop != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backdrop, ScaleMode.ScaleAndCrop);
            }
            else
            {
                DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.05f, 0.07f, 0.09f, 1f));
            }

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.18f));
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
            if (heroTexture == null || Screen.width < 520)
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

        private void StartGame(bool rebuildMap)
        {
            if (rebuildMap)
            {
                CrashClimbProceduralMap2D map = Object.FindFirstObjectByType<CrashClimbProceduralMap2D>();
                if (map != null)
                {
                    map.Build();
                }
            }
            else
            {
                CrashClimbPlayerController2D player = Object.FindFirstObjectByType<CrashClimbPlayerController2D>();
                player?.ResetToSpawn();
            }

            state = MenuState.Playing;
            Time.timeScale = 1f;
        }

        private void ShowVictory(CrashClimbPlayerController2D player)
        {
            state = MenuState.Victory;
            Time.timeScale = 0f;
        }

        private void LoadTextures()
        {
            skyTexture = Resources.Load<Texture2D>("CrashClimb/Background/Game_Background_1/Sky");
            backgroundTexture = Resources.Load<Texture2D>("CrashClimb/Background/Game_Background_1/BackGround");
            heroTexture = Resources.Load<Texture2D>("CrashClimb/Wraith_01/PNG Sequences/Idle/Wraith_01_Idle_000");
            platformTexture = Resources.Load<Texture2D>("CrashClimb/Pads/Pad_1_1");
            platformSprite = Resources.Load<Sprite>("CrashClimb/Pads/Pad_1_1");
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
