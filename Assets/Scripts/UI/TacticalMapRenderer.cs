using Survival.Player;
using Survival.World;
using UnityEngine;

namespace Survival.UI
{
    public static class TacticalMapRenderer
    {
        public static void DrawFullMapModal(SurvivalPlayerController player, BlueZoneController zone)
        {
            float size = Mathf.Min(Screen.width, Screen.height) - 90f;
            Rect mapModal = new Rect((Screen.width - size) * 0.5f, (Screen.height - size) * 0.5f, size, size);

            GuiDrawUtils.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.65f));
            DrawMapWidget(mapModal, player, zone, true);
            GUI.Label(
                new Rect(mapModal.x, mapModal.y - 28f, mapModal.width, 24f),
                "BẢN ĐỒ CHIẾN THUẬT ERANGEL MINI (Nhấn [M] hoặc [Tab] để đóng)",
                GuiDrawUtils.CenteredLabelStyle);
        }

        public static void DrawMapWidget(Rect rect, SurvivalPlayerController player, BlueZoneController zone, bool showPoiNames)
        {
            GuiDrawUtils.DrawTacticalPanel(rect, new Color(0.11f, 0.28f, 0.42f, 0.94f), new Color(0.85f, 0.85f, 0.85f, 0.6f));
            Rect islandRect = new Rect(rect.x + rect.width * 0.08f, rect.y + rect.height * 0.08f, rect.width * 0.84f, rect.height * 0.84f);
            GuiDrawUtils.DrawRect(islandRect, new Color(0.24f, 0.36f, 0.20f, 0.95f));

            float worldHalf = BattleRoyaleMapGenerator.IslandSize * 0.5f;

            Vector2 WorldToMap(Vector3 worldPos)
            {
                float nx = Mathf.InverseLerp(-worldHalf, worldHalf, worldPos.x);
                float nz = Mathf.InverseLerp(-worldHalf, worldHalf, worldPos.z);
                return new Vector2(
                    islandRect.x + nx * islandRect.width,
                    islandRect.y + (1f - nz) * islandRect.height
                );
            }

            for (int i = 0; i < BattleRoyaleMapGenerator.Locations.Length; i++)
            {
                NamedLocation loc = BattleRoyaleMapGenerator.Locations[i];
                Vector2 pt = WorldToMap(loc.position);
                GuiDrawUtils.DrawRect(new Rect(pt.x - 3f, pt.y - 3f, 6f, 6f), new Color(0.9f, 0.8f, 0.5f, 0.9f));
                if (showPoiNames)
                {
                    GUI.Label(new Rect(pt.x - 55f, pt.y + 4f, 110f, 20f), loc.name, GuiDrawUtils.CenteredLabelStyle);
                }
            }

            if (zone != null)
            {
                Vector2 safeCenter = WorldToMap(zone.TargetCenter);
                float safePixelRad = (zone.TargetRadius / BattleRoyaleMapGenerator.IslandSize) * islandRect.width;
                GuiDrawUtils.DrawOutlineBox(
                    new Rect(safeCenter.x - safePixelRad, safeCenter.y - safePixelRad, safePixelRad * 2f, safePixelRad * 2f),
                    new Color(1f, 1f, 1f, 0.85f),
                    2f);

                Vector2 blueCenter = WorldToMap(zone.CurrentCenter);
                float bluePixelRad = (zone.CurrentRadius / BattleRoyaleMapGenerator.IslandSize) * islandRect.width;
                GuiDrawUtils.DrawOutlineBox(
                    new Rect(blueCenter.x - bluePixelRad, blueCenter.y - bluePixelRad, bluePixelRad * 2f, bluePixelRad * 2f),
                    new Color(0.15f, 0.58f, 1f, 0.95f),
                    2f);
            }

            for (int i = 0; i < AirDropManager.ActiveAirDropLocations.Count; i++)
            {
                Vector2 dp = WorldToMap(AirDropManager.ActiveAirDropLocations[i]);
                GuiDrawUtils.DrawRect(new Rect(dp.x - 4f, dp.y - 4f, 8f, 8f), new Color(1f, 0.18f, 0.18f, 1f));
            }

            if (player != null)
            {
                Vector2 playerPt = WorldToMap(player.transform.position);
                GuiDrawUtils.DrawRect(new Rect(playerPt.x - 4f, playerPt.y - 4f, 8f, 8f), new Color(1f, 0.88f, 0.15f, 1f));
            }
        }
    }
}
