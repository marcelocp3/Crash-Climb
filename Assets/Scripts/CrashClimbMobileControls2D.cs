using UnityEngine;

namespace CrashClimb
{
    [DefaultExecutionOrder(-100)]
    public class CrashClimbMobileControls2D : MonoBehaviour
    {
        private const string VibrationPrefKey = "CrashClimb.VibrationEnabled";

        private static CrashClimbMobileControls2D instance;
        private static Texture2D whiteTexture;
        private static GUIStyle buttonStyle;
        private static GUIStyle smallButtonStyle;

        [SerializeField] private bool showInEditorForTesting;
        [SerializeField] private bool forceVisible;

        private Rect leftRect;
        private Rect rightRect;
        private Rect jumpRect;
        private Rect attackRect;
        private Rect pauseRect;

        private bool leftHeld;
        private bool rightHeld;
        private bool jumpHeld;
        private bool attackHeld;
        private bool pauseHeld;
        private bool jumpPressed;
        private bool jumpReleased;
        private bool attackPressed;
        private bool pausePressed;
        private float lastVibrationTime = -999f;

        public static float Horizontal
        {
            get
            {
                if (instance == null)
                {
                    return 0f;
                }

                if (instance.leftHeld == instance.rightHeld)
                {
                    return 0f;
                }

                return instance.leftHeld ? -1f : 1f;
            }
        }

        public static bool JumpPressedThisFrame => instance != null && instance.jumpPressed;
        public static bool JumpHeld => instance != null && instance.jumpHeld;
        public static bool JumpReleasedThisFrame => instance != null && instance.jumpReleased;
        public static bool AttackPressedThisFrame => instance != null && instance.attackPressed;
        public static bool PausePressedThisFrame => instance != null && instance.pausePressed;
        public static bool ControlsVisible => instance != null && instance.ShouldShowControls();

        public static bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(VibrationPrefKey, 1) != 0;
            set
            {
                PlayerPrefs.SetInt(VibrationPrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
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
            EnsureWhiteTexture();
        }

        private void Update()
        {
            bool previousJumpHeld = jumpHeld;
            bool previousAttackHeld = attackHeld;
            bool previousPauseHeld = pauseHeld;

            ResetHeldState();
            UpdateLayout();

            if (ShouldReadInput())
            {
                ReadTouches();
                ReadMouseWhenTesting();
            }

            jumpPressed = jumpHeld && !previousJumpHeld;
            jumpReleased = !jumpHeld && previousJumpHeld;
            attackPressed = attackHeld && !previousAttackHeld;
            pausePressed = pauseHeld && !previousPauseHeld;

            if (jumpPressed || attackPressed || pausePressed)
            {
                TryVibrate();
            }
        }

        private void OnGUI()
        {
            if (!ShouldReadInput())
            {
                return;
            }

            EnsureWhiteTexture();
            CreateStylesIfNeeded();
            UpdateLayout();

            DrawControlButton(leftRect, "<", leftHeld);
            DrawControlButton(rightRect, ">", rightHeld);
            DrawControlButton(attackRect, "ATK", attackHeld);
            DrawControlButton(jumpRect, "JUMP", jumpHeld);
            DrawControlButton(pauseRect, "PAUSE", pauseHeld, true);
        }

        private void OnDisable()
        {
            ClearInput();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                ClearInput();
            }
        }

        private bool ShouldReadInput()
        {
            return ShouldShowControls()
                && !CrashClimbMainMenu2D.IsBlockingHud
                && !Mathf.Approximately(Time.timeScale, 0f);
        }

        private bool ShouldShowControls()
        {
            if (forceVisible || Application.isMobilePlatform || Input.touchSupported)
            {
                return true;
            }

#if UNITY_EDITOR
            return showInEditorForTesting;
#else
            return false;
#endif
        }

        private void ReadTouches()
        {
            Touch[] touches = Input.touches;
            for (int i = 0; i < touches.Length; i++)
            {
                Touch touch = touches[i];
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    continue;
                }

                ReadPointer(ToGuiPosition(touch.position));
            }
        }

        private void ReadMouseWhenTesting()
        {
            if (!forceVisible && !showInEditorForTesting)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                ReadPointer(ToGuiPosition(Input.mousePosition));
            }
        }

        private void ReadPointer(Vector2 guiPosition)
        {
            leftHeld |= leftRect.Contains(guiPosition);
            rightHeld |= rightRect.Contains(guiPosition);
            jumpHeld |= jumpRect.Contains(guiPosition);
            attackHeld |= attackRect.Contains(guiPosition);
            pauseHeld |= pauseRect.Contains(guiPosition);
        }

        private Vector2 ToGuiPosition(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private void UpdateLayout()
        {
            Rect safe = GetGuiSafeArea();
            float minSize = Mathf.Min(safe.width, safe.height);
            float margin = Mathf.Clamp(minSize * 0.035f, 14f, 30f);
            float buttonSize = Mathf.Clamp(minSize * 0.16f, 62f, 96f);
            float gap = Mathf.Clamp(buttonSize * 0.18f, 10f, 18f);
            float bottomY = safe.yMax - margin - buttonSize;

            leftRect = new Rect(safe.x + margin, bottomY, buttonSize, buttonSize);
            rightRect = new Rect(leftRect.xMax + gap, bottomY, buttonSize, buttonSize);
            jumpRect = new Rect(safe.xMax - margin - buttonSize, bottomY, buttonSize, buttonSize);
            attackRect = new Rect(jumpRect.x - gap - buttonSize, bottomY, buttonSize, buttonSize);

            float pauseWidth = Mathf.Clamp(buttonSize * 1.22f, 78f, 118f);
            float pauseHeight = Mathf.Clamp(buttonSize * 0.56f, 40f, 52f);
            pauseRect = new Rect(safe.xMax - margin - pauseWidth, safe.y + margin, pauseWidth, pauseHeight);
        }

        private Rect GetGuiSafeArea()
        {
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f)
            {
                safe = new Rect(0f, 0f, Screen.width, Screen.height);
            }

            return new Rect(safe.x, Screen.height - safe.yMax, safe.width, safe.height);
        }

        private void DrawControlButton(Rect rect, string label, bool pressed, bool compact = false)
        {
            Color fill = pressed
                ? new Color(0.72f, 0.95f, 1f, 0.78f)
                : new Color(0.04f, 0.05f, 0.06f, 0.58f);
            Color edge = pressed
                ? new Color(0.85f, 1f, 1f, 0.95f)
                : new Color(0.72f, 0.32f, 1f, 0.76f);

            DrawRect(rect, fill);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), edge);
            DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), edge);
            DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), edge);
            DrawRect(new Rect(rect.xMax - 3f, rect.y, 3f, rect.height), edge);

            GUI.Label(rect, label, compact ? smallButtonStyle : buttonStyle);
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
        }

        private void TryVibrate()
        {
            if (!VibrationEnabled || !Application.isMobilePlatform || Time.unscaledTime - lastVibrationTime < 0.12f)
            {
                return;
            }

            lastVibrationTime = Time.unscaledTime;
            Handheld.Vibrate();
        }

        private void ResetHeldState()
        {
            leftHeld = false;
            rightHeld = false;
            jumpHeld = false;
            attackHeld = false;
            pauseHeld = false;
        }

        private void ClearInput()
        {
            ResetHeldState();
            jumpPressed = false;
            jumpReleased = false;
            attackPressed = false;
            pausePressed = false;
        }

        private static void EnsureWhiteTexture()
        {
            if (whiteTexture != null)
            {
                return;
            }

            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        private static void CreateStylesIfNeeded()
        {
            if (buttonStyle != null && buttonStyle.fontSize == GetButtonFontSize())
            {
                return;
            }

            int buttonFontSize = GetButtonFontSize();
            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = buttonFontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            smallButtonStyle = new GUIStyle(buttonStyle)
            {
                fontSize = Mathf.Max(12, buttonFontSize - 6)
            };
        }

        private static int GetButtonFontSize()
        {
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(Screen.width, Screen.height) * 0.042f), 16, 26);
        }
    }
}
