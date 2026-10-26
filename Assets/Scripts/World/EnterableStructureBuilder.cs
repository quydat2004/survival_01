using Survival.Core;
using Survival.Items;
using UnityEngine;

namespace Survival.World
{
    public static class EnterableStructureBuilder
    {
        public static GameObject BuildTwoStoryTacticalHouse(
            Transform parent,
            Vector3 worldPos,
            float yRotation,
            Color wallColor,
            Color trimColor,
            bool spawnInternalLoot = true)
        {
            GameObject houseRoot = new GameObject("TacticalHouse_2Story");
            if (parent != null)
                houseRoot.transform.SetParent(parent, false);

            houseRoot.transform.position = worldPos;
            houseRoot.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            Color floorColor = new Color(0.44f, 0.36f, 0.28f);
            Color roofColor = new Color(0.36f, 0.22f, 0.16f);

            // ================= TẦNG 1 =================
            // Sàn Tầng 1
            CreateBox(houseRoot.transform, "Floor1", new Vector3(0f, 0.1f, 0f), new Vector3(12.4f, 0.2f, 10.4f), floorColor);

            // Tường sau
            CreateBox(houseRoot.transform, "Wall_Back_F1", new Vector3(0f, 1.8f, 5.0f), new Vector3(12.4f, 3.4f, 0.4f), wallColor);

            // Tường trái có cửa sổ ngắm bắn
            CreateBox(houseRoot.transform, "Wall_Left_Bottom", new Vector3(-6.0f, 0.6f, 0f), new Vector3(0.4f, 1.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Left_Top", new Vector3(-6.0f, 3.0f, 0f), new Vector3(0.4f, 1.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Left_P1", new Vector3(-6.0f, 1.8f, -3.2f), new Vector3(0.4f, 1.4f, 4.0f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Left_P2", new Vector3(-6.0f, 1.8f, 3.2f), new Vector3(0.4f, 1.4f, 4.0f), wallColor);

            // Tường phải (ôm sát bên ngoài cầu thang)
            CreateBox(houseRoot.transform, "Wall_Right_F1", new Vector3(6.0f, 1.8f, 0f), new Vector3(0.4f, 3.4f, 10.4f), wallColor);

            // Tường trước: Cửa chính rộng 2.4m, cao 2.6m
            CreateBox(houseRoot.transform, "Wall_Front_L", new Vector3(-3.8f, 1.8f, -5.0f), new Vector3(4.8f, 3.4f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Front_R", new Vector3(3.8f, 1.8f, -5.0f), new Vector3(4.8f, 3.4f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Front_Header", new Vector3(0f, 3.0f, -5.0f), new Vector3(2.8f, 1.0f, 0.4f), trimColor);

            // ================= CẦU THANG TẦNG 1 LÊN TẦNG 2 =================
            // Dốc thoải 28 độ dọc tường phải từ Z = -3.4m đến Z = 2.6m (dài 6.0m, leo từ Y=0.2m lên Y=3.55m)
            // Headroom trên cầu thang hoàn toàn mở thông lên tầng 2 (không có sàn hay dốc nóc đè lên)
            GameObject stairs = CreateBox(houseRoot.transform, "StairRamp_F1_F2", new Vector3(4.6f, 1.88f, -0.4f), new Vector3(2.4f, 0.2f, 6.8f), floorColor);
            stairs.transform.localRotation = Quaternion.Euler(-29f, 0f, 0f);

            // Lan can bảo vệ bên trong cầu thang tầng 1
            CreateBox(houseRoot.transform, "StairHandrail_F1", new Vector3(3.35f, 2.3f, -0.4f), new Vector3(0.12f, 0.85f, 6.6f), trimColor);

            // ================= SÀN TẦNG 2 =================
            // Sàn Tầng 2 phủ phần lớn nhà, chừa giếng thang ở phía phải (X: 3.3 -> 6.0, Z: -4.8 -> 2.6)
            CreateBox(houseRoot.transform, "Floor2_LeftWing", new Vector3(-1.8f, 3.55f, 0f), new Vector3(8.8f, 0.25f, 10.4f), floorColor);
            CreateBox(houseRoot.transform, "Floor2_LandingBack", new Vector3(4.6f, 3.55f, 3.8f), new Vector3(4.0f, 0.25f, 2.8f), floorColor);

            // Lan can bảo vệ mép giếng thang tầng 2 (người chơi không bị rơi ngược xuống)
            CreateBox(houseRoot.transform, "F2_WellRailing_Side", new Vector3(2.55f, 4.1f, -1.0f), new Vector3(0.12f, 0.9f, 7.6f), trimColor);
            CreateBox(houseRoot.transform, "F2_WellRailing_End", new Vector3(4.5f, 4.1f, -4.8f), new Vector3(3.8f, 0.9f, 0.12f), trimColor);

            // ================= TƯỜNG TẦNG 2 =================
            CreateBox(houseRoot.transform, "Wall_Back_F2", new Vector3(0f, 5.2f, 5.0f), new Vector3(12.4f, 3.0f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Left_F2", new Vector3(-6.0f, 5.2f, 0f), new Vector3(0.4f, 3.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Right_F2", new Vector3(6.0f, 5.2f, 0f), new Vector3(0.4f, 3.0f, 10.4f), wallColor);

            // Mặt trước tầng 2: Ban công ngắm bắn có cửa đi ra
            CreateBox(houseRoot.transform, "Wall_Front_F2_L", new Vector3(-3.8f, 5.2f, -5.0f), new Vector3(4.8f, 3.0f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Front_F2_R", new Vector3(3.8f, 5.2f, -5.0f), new Vector3(4.8f, 3.0f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Balcony_Railing", new Vector3(0f, 4.3f, -5.0f), new Vector3(2.8f, 1.2f, 0.15f), trimColor);

            // ================= CẦU THANG TẦNG 2 LÊN MÁI (ĐẶT Ở GÓC TRÁI, TÁCH BIỆT) =================
            GameObject roofStairs = CreateBox(houseRoot.transform, "StairRamp_F2_Roof", new Vector3(-4.6f, 5.2f, -0.4f), new Vector3(2.4f, 0.2f, 6.8f), floorColor);
            roofStairs.transform.localRotation = Quaternion.Euler(-29f, 0f, 0f);

            // ================= MÁI NHÀ VÀ SÂN THƯỢNG (ROOFTOP SNIPER DECK) =================
            // Sàn mái che phủ phần lớn, chừa ô cửa lên mái ở bên trái
            CreateBox(houseRoot.transform, "Roof_MainSlab", new Vector3(1.8f, 6.9f, 0f), new Vector3(8.8f, 0.25f, 10.4f), roofColor);
            CreateBox(houseRoot.transform, "Roof_LandingBack", new Vector3(-4.6f, 6.9f, 3.8f), new Vector3(4.0f, 0.25f, 2.8f), roofColor);

            // Lan can bảo vệ sân thượng ngắm bắn
            CreateBox(houseRoot.transform, "Roof_Parapet_Front", new Vector3(0f, 7.4f, -5.2f), new Vector3(12.6f, 0.9f, 0.25f), trimColor);
            CreateBox(houseRoot.transform, "Roof_Parapet_Back", new Vector3(0f, 7.4f, 5.2f), new Vector3(12.6f, 0.9f, 0.25f), trimColor);
            CreateBox(houseRoot.transform, "Roof_Parapet_L", new Vector3(-6.2f, 7.4f, 0f), new Vector3(0.25f, 0.9f, 10.6f), trimColor);
            CreateBox(houseRoot.transform, "Roof_Parapet_R", new Vector3(6.2f, 7.4f, 0f), new Vector3(0.25f, 0.9f, 10.6f), trimColor);

            // ================= VẬT PHẨM NỘI THẤT (LOOT) =================
            if (spawnInternalLoot)
            {
                Vector3 rotVec(Vector3 local) => worldPos + (Quaternion.Euler(0f, yRotation, 0f) * local);

                // Tầng 1: Góc phòng khách
                LootItem.SpawnLoot(rotVec(new Vector3(-3.2f, 0.25f, 1.8f)), LootCategory.Weapon, WeaponId.M416, 60, 2, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(-3.2f, 0.25f, -2.4f)), LootCategory.Ammo, WeaponId.M416, 60, 1, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(0f, 0.25f, 2.5f)), LootCategory.Medkit, WeaponId.M416, 1, 2, parent);

                // Tầng 2: Phòng ngủ / Phòng ngắm bắn
                LootItem.SpawnLoot(rotVec(new Vector3(-2.2f, 3.85f, 2.2f)), LootCategory.Weapon, WeaponId.AKM, 60, 2, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(0f, 3.85f, -2.5f)), LootCategory.ArmorVest, WeaponId.M416, 1, 2, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(-2.2f, 3.85f, -2.5f)), LootCategory.Helmet, WeaponId.M416, 1, 2, parent);

                // Sân thượng: Súng bắn tỉa Kar98k
                LootItem.SpawnLoot(rotVec(new Vector3(1.5f, 7.15f, 0f)), LootCategory.Weapon, WeaponId.Kar98k, 25, 3, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(1.5f, 7.15f, 2.0f)), LootCategory.Ammo, WeaponId.Kar98k, 30, 2, parent);
            }

            return houseRoot;
        }

        public static GameObject BuildSingleStoryEnterableHouse(
            Transform parent,
            Vector3 worldPos,
            float yRotation,
            Color wallColor,
            Color roofColor,
            bool spawnInternalLoot = true)
        {
            GameObject house = new GameObject("TacticalHouse_1Story");
            if (parent != null)
                house.transform.SetParent(parent, false);

            house.transform.position = worldPos;
            house.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            Color floorColor = new Color(0.42f, 0.35f, 0.28f);

            // Sàn nhà
            CreateBox(house.transform, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(10.8f, 0.2f, 8.8f), floorColor);

            // Tường sau có 1 cửa sổ
            CreateBox(house.transform, "Wall_Back_Bottom", new Vector3(0f, 0.6f, 4.2f), new Vector3(10.8f, 1.0f, 0.4f), wallColor);
            CreateBox(house.transform, "Wall_Back_Top", new Vector3(0f, 2.8f, 4.2f), new Vector3(10.8f, 1.0f, 0.4f), wallColor);
            CreateBox(house.transform, "Wall_Back_L", new Vector3(-3.6f, 1.7f, 4.2f), new Vector3(3.6f, 1.2f, 0.4f), wallColor);
            CreateBox(house.transform, "Wall_Back_R", new Vector3(3.6f, 1.7f, 4.2f), new Vector3(3.6f, 1.2f, 0.4f), wallColor);

            // Hai tường bên có cửa sổ ngắm bắn
            CreateBox(house.transform, "Wall_Left_Bottom", new Vector3(-5.2f, 0.6f, 0f), new Vector3(0.4f, 1.0f, 8.8f), wallColor);
            CreateBox(house.transform, "Wall_Left_Top", new Vector3(-5.2f, 2.8f, 0f), new Vector3(0.4f, 1.0f, 8.8f), wallColor);
            CreateBox(house.transform, "Wall_Left_P1", new Vector3(-5.2f, 1.7f, -2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);
            CreateBox(house.transform, "Wall_Left_P2", new Vector3(-5.2f, 1.7f, 2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);

            CreateBox(house.transform, "Wall_Right_Bottom", new Vector3(5.2f, 0.6f, 0f), new Vector3(0.4f, 1.0f, 8.8f), wallColor);
            CreateBox(house.transform, "Wall_Right_Top", new Vector3(5.2f, 2.8f, 0f), new Vector3(0.4f, 1.0f, 8.8f), wallColor);
            CreateBox(house.transform, "Wall_Right_P1", new Vector3(5.2f, 1.7f, -2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);
            CreateBox(house.transform, "Wall_Right_P2", new Vector3(5.2f, 1.7f, 2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);

            // Tường trước: Cửa chính mở rộng 2.4m
            CreateBox(house.transform, "Wall_Front_Left", new Vector3(-3.4f, 1.7f, -4.2f), new Vector3(4.0f, 3.2f, 0.4f), wallColor);
            CreateBox(house.transform, "Wall_Front_Right", new Vector3(3.4f, 1.7f, -4.2f), new Vector3(4.0f, 3.2f, 0.4f), wallColor);
            CreateBox(house.transform, "Wall_Front_Header", new Vector3(0f, 2.8f, -4.2f), new Vector3(2.8f, 1.0f, 0.4f), wallColor);

            // Mái che và cầu thang bên ngoài leo lên nóc nhà bắn tỉa
            CreateBox(house.transform, "Roof", new Vector3(0f, 3.4f, 0f), new Vector3(11.4f, 0.25f, 9.4f), roofColor);
            GameObject sideStairs = CreateBox(house.transform, "OutsideStairs", new Vector3(6.5f, 1.7f, 0f), new Vector3(2.2f, 0.2f, 6.4f), floorColor);
            sideStairs.transform.localRotation = Quaternion.Euler(-27f, 0f, 0f);

            if (spawnInternalLoot)
            {
                Vector3 rotVec(Vector3 local) => worldPos + (Quaternion.Euler(0f, yRotation, 0f) * local);
                LootItem.SpawnLoot(rotVec(new Vector3(-2.8f, 0.25f, 1.8f)), LootCategory.Weapon, WeaponId.UMP45, 45, 1, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(2.8f, 0.25f, 1.8f)), LootCategory.Medkit, WeaponId.M416, 1, 1, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(-2.8f, 0.25f, -1.8f)), LootCategory.ArmorVest, WeaponId.M416, 1, 2, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(0f, 3.65f, 0f)), LootCategory.Weapon, WeaponId.S1897, 20, 1, parent);
            }

            return house;
        }

        public static GameObject BuildEnterableMilitaryHangar(
            Transform parent,
            Vector3 worldPos,
            float yRotation,
            Color wallColor,
            bool spawnInternalLoot = true)
        {
            GameObject hangar = new GameObject("MilitaryHangar_Open");
            if (parent != null)
                hangar.transform.SetParent(parent, false);

            hangar.transform.position = worldPos;
            hangar.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            Color floorColor = new Color(0.36f, 0.37f, 0.38f);

            // Sàn bê tông
            CreateBox(hangar.transform, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(16f, 0.2f, 22f), floorColor);

            // Hai vách hông kín dài 22m
            CreateBox(hangar.transform, "Wall_Left", new Vector3(-7.8f, 2.8f, 0f), new Vector3(0.5f, 5.4f, 22f), wallColor);
            CreateBox(hangar.transform, "Wall_Right", new Vector3(7.8f, 2.8f, 0f), new Vector3(0.5f, 5.4f, 22f), wallColor);

            // Tường sau có cửa thoát hiểm rộng 2.4m
            CreateBox(hangar.transform, "Wall_Back_L", new Vector3(-4.8f, 2.8f, 10.8f), new Vector3(5.6f, 5.4f, 0.5f), wallColor);
            CreateBox(hangar.transform, "Wall_Back_R", new Vector3(4.8f, 2.8f, 10.8f), new Vector3(5.6f, 5.4f, 0.5f), wallColor);
            CreateBox(hangar.transform, "Wall_Back_Top", new Vector3(0f, 4.4f, 10.8f), new Vector3(4.0f, 2.2f, 0.5f), wallColor);

            // Cửa trước mở toang cho xe/lính chạy vào (cổng rộng 8m)
            CreateBox(hangar.transform, "Wall_Front_L", new Vector3(-6.0f, 2.8f, -10.8f), new Vector3(3.2f, 5.4f, 0.5f), wallColor);
            CreateBox(hangar.transform, "Wall_Front_R", new Vector3(6.0f, 2.8f, -10.8f), new Vector3(3.2f, 5.4f, 0.5f), wallColor);
            CreateBox(hangar.transform, "Wall_Front_Arch", new Vector3(0f, 4.6f, -10.8f), new Vector3(9.2f, 1.8f, 0.5f), wallColor);

            // Mái vòm
            CreateBox(hangar.transform, "Roof", new Vector3(0f, 5.6f, 0f), new Vector3(16.5f, 0.4f, 22.4f), wallColor * 0.82f);

            // Thùng hàng che chắn bên trong
            CreateBox(hangar.transform, "CoverCrate1", new Vector3(-3.5f, 0.9f, 2.0f), new Vector3(2.0f, 1.6f, 2.0f), new Color(0.48f, 0.38f, 0.24f));
            CreateBox(hangar.transform, "CoverCrate2", new Vector3(3.5f, 0.9f, -2.0f), new Vector3(2.0f, 1.6f, 2.0f), new Color(0.48f, 0.38f, 0.24f));

            if (spawnInternalLoot)
            {
                Vector3 rotVec(Vector3 local) => worldPos + (Quaternion.Euler(0f, yRotation, 0f) * local);
                LootItem.SpawnLoot(rotVec(new Vector3(-3.5f, 1.85f, 2.0f)), LootCategory.Weapon, WeaponId.M416, 60, 3, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(3.5f, 1.85f, -2.0f)), LootCategory.Weapon, WeaponId.Kar98k, 25, 3, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(0f, 0.25f, 6.0f)), LootCategory.ArmorVest, WeaponId.M416, 1, 3, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(1.8f, 0.25f, 6.0f)), LootCategory.Helmet, WeaponId.M416, 1, 3, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(-1.8f, 0.25f, 6.0f)), LootCategory.Medkit, WeaponId.M416, 1, 3, parent);
            }

            return hangar;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            return MaterialPool.CreatePrimitive(PrimitiveType.Cube, parent, name, localPos, scale, color, true);
        }
    }
}
