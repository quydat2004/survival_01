using Survival.Player;
using Survival.World;
using UnityEngine;

namespace Survival.UI
{
    public static class TacticalMapRenderer
    {
        private static Texture2D mapTexture;
        private static Texture2D playerArrowTex;

        public static void EnsureMapTexture()
        {
            if (mapTexture != null) return;

            const int res = 256;
            mapTexture = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color oceanCol = new Color(0.10f, 0.24f, 0.38f, 1f);
            Color beachCol = new Color(0.76f, 0.70f, 0.50f, 1f);
            Color grassCol = new Color(0.28f, 0.44f, 0.24f, 1f);
            Color roadCol = new Color(0.36f, 0.35f, 0.33f, 1f);
            Color townCol = new Color(0.48f, 0.36f, 0.26f, 1f);

            Color[] pixels = new Color[res * res];
            float centerPixel = res * 0.5f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = Mathf.Abs(x - centerPixel);
                    float dy = Mathf.Abs(y - centerPixel);
                    float distFromCenter = Mathf.Max(dx, dy);

                    Color c;
                    if (distFromCenter > 118f)
                    {
                        c = oceanCol;
                    }
                    else if (distFromCenter > 106f)
                    {
                        c = beachCol;
                    }
                    else
                    {
                        c = grassCol;

                        // Đại lộ chính chữ thập
                        if (dx <= 4f || dy <= 4f)
                        {
                            c = roadCol;
                        }

                        // Các thị trấn POI
                        for (int p = 0; p < BattleRoyaleMapGenerator.Locations.Length; p++)
                        {
                            Vector3 pos = BattleRoyaleMapGenerator.Locations[p].position;
                            float px = Mathf.Lerp(centerPixel - 106f, centerPixel + 106f, Mathf.InverseLerp(-270f, 270f, pos.x));
                            float py = Mathf.Lerp(centerPixel - 106f, centerPixel + 106f, Mathf.InverseLerp(-270f, 270f, pos.z));
                            if (Mathf.Abs(x - px) < 14f && Mathf.Abs(y - py) < 14f)
                            {
                                c = townCol;
                                break;
                            }
                        }
                    }

                    pixels[y * res + x] = c;
                }
            }

            mapTexture.SetPixels(pixels);
            mapTexture.Apply();

            // Mũi tên chỉ hướng người chơi
            playerArrowTex = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            Color[] arrowPix = new Color[16 * 16];
            for (int i = 0; i < arrowPix.Length; i++) arrowPix[i] = Color.clear;

            Color gold = new Color(1f, 0.88f, 0.20f, 1f);
            // Mũi tên tam giác hướng lên (Upward pointing triangle)
            for (int y = 0; y < 16; y++)
            {
                int halfW = (y / 2);
                for (int x = 8 - halfW; x <= 8 + halfW; x++)
                {
                    if (x >= 0 && x < 16)
                        arrowPix[y * 16 + x] = gold;
                }
            }
            playerArrowTex.SetPixels(arrowPix);
            playerArrowTex.Apply();
        }

        public static void DrawFullMapModal(SurvivalPlayerController player, BlueZoneController zone)
        {
            EnsureMapTexture();

            float size = Mathf.Min(Screen.width, Screen.height) - 100f;
            Rect mapModal = new Rect((Screen.width - size) * 0.5f, (Screen.height - size) * 0.5f, size, size);

            GuiDrawUtils.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.70f));
            DrawMapWidget(mapModal, player, zone, true);

            GUI.Label(
                new Rect(mapModal.x, mapModal.y - 28f, mapModal.width, 24f),
                "BẢN ĐỒ CHIẾN THUẬT ERANGEL MINI  •  [M] ĐÓNG",
                GuiDrawUtils.CenteredLabelStyle);
        }

        public static void DrawMapWidget(Rect rect, SurvivalPlayerController player, BlueZoneController zone, bool showPoiNames)
        {
            EnsureMapTexture();

            // Khung viền ngoài Tactical
            GuiDrawUtils.DrawTacticalPanel(rect, new Color(0.06f, 0.08f, 0.11f, 0.95f), new Color(0.95f, 0.72f, 0.15f, 0.75f));

            Rect mapInner = new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f);
            if (mapTexture != null)
            {
                GUI.DrawTexture(mapInner, mapTexture);
            }

            float worldHalf = BattleRoyaleMapGenerator.IslandSize * 0.5f;

            Vector2 WorldToMap(Vector3 worldPos)
            {
                float nx = Mathf.InverseLerp(-worldHalf, worldHalf, worldPos.x);
                float nz = Mathf.InverseLerp(-worldHalf, worldHalf, worldPos.z);
                return new Vector2(
                    mapInner.x + nx * mapInner.width,
                    mapInner.y + (1f - nz) * mapInner.height
                );
            }

            // Tên các thị trấn lớn (POIs)
            for (int i = 0; i < BattleRoyaleMapGenerator.Locations.Length; i++)
            {
                NamedLocation loc = BattleRoyaleMapGenerator.Locations[i];
                Vector2 pt = WorldToMap(loc.position);
                GuiDrawUtils.DrawRect(new Rect(pt.x - 2.5f, pt.y - 2.5f, 5f, 5f), new Color(1f, 0.88f, 0.4f, 0.95f));

                if (showPoiNames)
                {
                    GUI.Label(new Rect(pt.x - 50f, pt.y + 3f, 100f, 18f), loc.name, GuiDrawUtils.BadgeStyle);
                }
            }

            // Vòng bo an toàn (Safe Zone - viền trắng sáng)
            if (zone != null)
            {
                Vector2 safeCenter = WorldToMap(zone.TargetCenter);
                float safePixelRad = (zone.TargetRadius / BattleRoyaleMapGenerator.IslandSize) * mapInner.width;
                DrawCircleOutline(safeCenter, safePixelRad, new Color(1f, 1f, 1f, 0.88f), 1.5f);

                // Vòng bo xanh (Blue Zone - viền xanh dương neon)
                Vector2 blueCenter = WorldToMap(zone.CurrentCenter);
                float bluePixelRad = (zone.CurrentRadius / BattleRoyaleMapGenerator.IslandSize) * mapInner.width;
                DrawCircleOutline(blueCenter, bluePixelRad, new Color(0.18f, 0.62f, 1f, 0.95f), 2f);
            }

            // Hòm thính AirDrop (chấm đỏ nổi bật)
            for (int i = 0; i < AirDropManager.ActiveAirDropLocations.Count; i++)
            {
                Vector2 dp = WorldToMap(AirDropManager.ActiveAirDropLocations[i]);
                GuiDrawUtils.DrawRect(new Rect(dp.x - 4f, dp.y - 4f, 8f, 8f), new Color(1f, 0.15f, 0.15f, 1f));
            }

            // Người chơi (Mũi tên vàng xoay theo góc nhìn)
            if (player != null)
            {
                Vector2 playerPt = WorldToMap(player.transform.position);
                float heading = player.transform.eulerAngles.y;

                if (playerArrowTex != null)
                {
                    Matrix4x4 prevMatrix = GUI.matrix;
                    GUIUtility.RotateAroundPivot(heading, playerPt);
                    GUI.DrawTexture(new Rect(playerPt.x - 7f, playerPt.y - 7f, 14f, 14f), playerArrowTex);
                    GUI.matrix = prevMatrix;
                }
                else
                {
                    GuiDrawUtils.DrawRect(new Rect(playerPt.x - 4f, playerPt.y - 4f, 8f, 8f), new Color(1f, 0.88f, 0.15f, 1f));
                }

                // Hiển thị tên khu vực hiện tại nếu là Minimap nhỏ
                if (!showPoiNames)
                {
                    string currentPoi = GetCurrentLocationName(player.transform.position);
                    GUI.Label(new Rect(rect.x + 4f, rect.yMax - 22f, rect.width - 8f, 18f), currentPoi, GuiDrawUtils.BadgeStyle);
                }
            }
        }

        private static string GetCurrentLocationName(Vector3 playerPos)
        {
            float closestDist = float.MaxValue;
            string closestName = "HOANG DÃ";

            for (int i = 0; i < BattleRoyaleMapGenerator.Locations.Length; i++)
            {
                float d = Vector3.Distance(playerPos, BattleRoyaleMapGenerator.Locations[i].position);
                if (d < 45f && d < closestDist)
                {
                    closestDist = d;
                    closestName = BattleRoyaleMapGenerator.Locations[i].name;
                }
            }

            return closestName;
        }

        private static void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness)
        {
            if (radius <= 1f) return;

            const int segments = 24;
            float step = (Mathf.PI * 2f) / segments;

            for (int i = 0; i < segments; i++)
            {
                float a1 = i * step;
                float a2 = (i + 1) * step;

                Vector2 p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                Vector2 p2 = center + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * radius;

                DrawLine(p1, p2, color, thickness);
            }
        }

        private static void DrawLine(Vector2 p1, Vector2 p2, Color color, float thickness)
        {
            Vector2 d = p2 - p1;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float length = d.magnitude;

            Matrix4x4 prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, p1);
            GuiDrawUtils.DrawRect(new Rect(p1.x, p1.y - thickness * 0.5f, length, thickness), color);
            GUI.matrix = prev;
        }
    }
}
