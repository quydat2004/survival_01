using UnityEngine;

namespace Survival.UI
{
    public static class GuiDrawUtils
    {
        private static Texture2D whiteTex;
        private static bool stylesInitialized = false;

        public static GUIStyle PanelStyle { get; private set; }
        public static GUIStyle TitleStyle { get; private set; }
        public static GUIStyle ElimTitleStyle { get; private set; }
        public static GUIStyle SubTitleStyle { get; private set; }
        public static GUIStyle LabelStyle { get; private set; }
        public static GUIStyle MutedLabelStyle { get; private set; }
        public static GUIStyle CenteredLabelStyle { get; private set; }
        public static GUIStyle SubCenterStyle { get; private set; }
        public static GUIStyle WarnStyle { get; private set; }
        public static GUIStyle RankStyle { get; private set; }
        public static GUIStyle ButtonStyle { get; private set; }
        public static GUIStyle GoldButtonStyle { get; private set; }
        public static GUIStyle TextFieldStyle { get; private set; }
        public static GUIStyle BadgeStyle { get; private set; }

        public static void EnsureInitialized()
        {
            if (whiteTex == null)
            {
                whiteTex = new Texture2D(2, 2);
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        whiteTex.SetPixel(x, y, Color.white);
                whiteTex.Apply();
            }

            if (stylesInitialized && TitleStyle != null)
                return;

            stylesInitialized = true;

            PanelStyle = new GUIStyle(GUI.skin.box);

            TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            TitleStyle.normal.textColor = new Color(0.95f, 0.76f, 0.20f);

            ElimTitleStyle = new GUIStyle(TitleStyle);
            ElimTitleStyle.normal.textColor = new Color(0.92f, 0.28f, 0.24f);

            SubTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            SubTitleStyle.normal.textColor = new Color(0.90f, 0.92f, 0.95f);

            LabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            LabelStyle.normal.textColor = new Color(0.80f, 0.84f, 0.88f);

            MutedLabelStyle = new GUIStyle(LabelStyle)
            {
                fontSize = 11
            };
            MutedLabelStyle.normal.textColor = new Color(0.55f, 0.60f, 0.66f);

            CenteredLabelStyle = new GUIStyle(LabelStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            SubCenterStyle = new GUIStyle(CenteredLabelStyle)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal
            };
            SubCenterStyle.normal.textColor = new Color(0.58f, 0.66f, 0.76f);

            WarnStyle = new GUIStyle(CenteredLabelStyle)
            {
                fontSize = 15
            };
            WarnStyle.normal.textColor = new Color(0.35f, 0.78f, 1f);

            RankStyle = new GUIStyle(CenteredLabelStyle)
            {
                fontSize = 18
            };
            RankStyle.normal.textColor = Color.white;

            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            GoldButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            GoldButtonStyle.normal.textColor = new Color(1f, 0.86f, 0.26f);

            TextFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };

            BadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            BadgeStyle.normal.textColor = new Color(0.92f, 0.94f, 0.96f);
        }

        public static void DrawStatRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, MutedLabelStyle, GUILayout.Width(180f));
            GUILayout.Label(value, SubTitleStyle);
            GUILayout.EndHorizontal();
        }

        public static bool DrawTabButton(string text, bool active, float width, float height)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.95f, 0.72f, 0.15f) : new Color(0.16f, 0.20f, 0.26f);
            bool clicked = GUILayout.Button(text, ButtonStyle, GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = prev;
            return clicked;
        }

        public static bool DrawPosButton(Rect rect, string text, bool active)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = active ? new Color(0.95f, 0.72f, 0.15f) : new Color(0.16f, 0.20f, 0.26f);
            bool clicked = GUI.Button(rect, text, ButtonStyle);
            GUI.backgroundColor = prev;
            return clicked;
        }

        public static void DrawTacticalPanel(Rect rect, Color bgColor, Color borderColor)
        {
            DrawRect(rect, bgColor);
            DrawOutlineBox(rect, borderColor, 1.5f);
        }

        public static void DrawOutlineBox(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        public static void DrawRect(Rect rect, Color color)
        {
            if (whiteTex == null) return;
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = prev;
        }

        public static string GetCardinalLabel(float deg)
        {
            if (deg >= 337.5f || deg < 22.5f) return "N";
            if (deg < 67.5f) return "NE";
            if (deg < 112.5f) return "E";
            if (deg < 157.5f) return "SE";
            if (deg < 202.5f) return "S";
            if (deg < 247.5f) return "SW";
            if (deg < 292.5f) return "W";
            return "NW";
        }
    }
}
