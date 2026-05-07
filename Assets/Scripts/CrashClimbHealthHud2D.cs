using UnityEngine;

namespace CrashClimb
{
    public class CrashClimbHealthHud2D : MonoBehaviour
    {
        [SerializeField] private CrashClimbPlayerController2D player;
        [SerializeField] private Vector2 screenOffset = new Vector2(14f, 12f);
        [SerializeField] private Vector2 barSize = new Vector2(170f, 14f);

        private static Texture2D whiteTexture;
        private GUIStyle labelStyle;
        private GUIStyle smallLabelStyle;
        private GUIStyle titleStyle;
        private CrashClimbProceduralMap2D map;
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
            smallLabelStyle = new GUIStyle(labelStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };
            titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }

        private void OnGUI()
        {
            if (player == null && Time.unscaledTime >= nextPlayerLookupTime)
            {
                player = Object.FindFirstObjectByType<CrashClimbPlayerController2D>();
                map = Object.FindFirstObjectByType<CrashClimbProceduralMap2D>();
                nextPlayerLookupTime = Time.unscaledTime + 0.25f;
            }

            if (player == null || CrashClimbMainMenu2D.IsBlockingHud || Mathf.Approximately(Time.timeScale, 0f))
            {
                return;
            }

            int maxHealth = Mathf.Max(1, player.MaxHealth);
            int currentHealth = Mathf.Clamp(player.CurrentHealth, 0, maxHealth);
            float fill = currentHealth / (float)maxHealth;
            float panelWidth = 230f;
            float panelHeight = 116f;
            Rect panelRect = new Rect(screenOffset.x, screenOffset.y, panelWidth, panelHeight);
            DrawRect(panelRect, new Color(0.03f, 0.04f, 0.05f, 0.78f));
            DrawRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 3f), new Color(0.75f, 0.32f, 1f, 0.9f));

            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 8f, 160f, 22f), "Crash & Climb", titleStyle);
            DrawLabeledBar("Vida", $"{currentHealth}/{maxHealth}", fill, panelRect.x + 12f, panelRect.y + 38f, GetHealthColor(fill));

            float charge = player.JumpCharge01;
            DrawLabeledBar("Salto", $"{Mathf.RoundToInt(charge * 100f)}%", charge, panelRect.x + 12f, panelRect.y + 65f, new Color(0.2f, 0.7f, 1f, 0.96f));

            float totalHeight = map != null ? Mathf.Max(1f, map.TotalHeight) : 1f;
            float altitude = Mathf.Max(0f, player.Height);
            float progress = Mathf.Clamp01(altitude / totalHeight);
            string zone = map != null ? map.GetZoneName(player.Height) : player.CurrentSurfaceLabel;
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 90f, 205f, 18f), $"Altura {Mathf.RoundToInt(progress * 100f)}%  |  {zone}", smallLabelStyle);
        }

        private void DrawLabeledBar(string label, string value, float fill, float x, float y, Color fillColor)
        {
            GUI.Label(new Rect(x, y - 2f, 64f, 18f), label, smallLabelStyle);
            GUI.Label(new Rect(x + barSize.x - 34f, y - 2f, 58f, 18f), value, smallLabelStyle);

            Rect borderRect = new Rect(x, y + 15f, barSize.x, barSize.y);
            Rect backRect = new Rect(borderRect.x + 2f, borderRect.y + 2f, borderRect.width - 4f, borderRect.height - 4f);
            Rect fillRect = new Rect(backRect.x, backRect.y, backRect.width * Mathf.Clamp01(fill), backRect.height);

            DrawRect(borderRect, new Color(0.05f, 0.06f, 0.07f, 0.88f));
            DrawRect(backRect, new Color(0.16f, 0.05f, 0.06f, 0.92f));
            DrawRect(fillRect, fillColor);
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
