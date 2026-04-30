using UnityEngine;

namespace CrashClimb
{
    public class CrashClimbHealthHud2D : MonoBehaviour
    {
        [SerializeField] private CrashClimbPlayerController2D player;
        [SerializeField] private Vector2 screenOffset = new Vector2(14f, 12f);
        [SerializeField] private Vector2 barSize = new Vector2(150f, 14f);

        private static Texture2D whiteTexture;
        private GUIStyle labelStyle;
        private float nextPlayerLookupTime;

        private void Awake()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            labelStyle = new GUIStyle
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            if (player == null && Time.unscaledTime >= nextPlayerLookupTime)
            {
                player = Object.FindFirstObjectByType<CrashClimbPlayerController2D>();
                nextPlayerLookupTime = Time.unscaledTime + 0.25f;
            }

            if (player == null)
            {
                return;
            }

            int maxHealth = Mathf.Max(1, player.MaxHealth);
            int currentHealth = Mathf.Clamp(player.CurrentHealth, 0, maxHealth);
            float fill = currentHealth / (float)maxHealth;
            Rect labelRect = new Rect(screenOffset.x, screenOffset.y, 120f, 18f);
            Rect borderRect = new Rect(screenOffset.x, screenOffset.y + 22f, barSize.x, barSize.y);
            Rect backRect = new Rect(borderRect.x + 2f, borderRect.y + 2f, borderRect.width - 4f, borderRect.height - 4f);
            Rect fillRect = new Rect(backRect.x, backRect.y, backRect.width * fill, backRect.height);

            GUI.Label(labelRect, $"Vida {currentHealth}/{maxHealth}", labelStyle);
            DrawRect(borderRect, new Color(0.05f, 0.06f, 0.07f, 0.88f));
            DrawRect(backRect, new Color(0.16f, 0.05f, 0.06f, 0.92f));
            DrawRect(fillRect, GetHealthColor(fill));
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previousColor;
        }

        private Color GetHealthColor(float fill)
        {
            if (fill <= 0.34f)
            {
                return new Color(0.95f, 0.12f, 0.08f, 0.96f);
            }

            if (fill <= 0.67f)
            {
                return new Color(1f, 0.65f, 0.08f, 0.96f);
            }

            return new Color(0.18f, 0.82f, 0.28f, 0.96f);
        }
    }
}
