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

            // Tầng 1: Sàn
            CreateBox(houseRoot.transform, "Floor1", new Vector3(0f, 0.1f, 0f), new Vector3(12.4f, 0.2f, 10.4f), floorColor);

            // Tường sau kín
            CreateBox(houseRoot.transform, "Wall_Back_F1", new Vector3(0f, 1.8f, 5.0f), new Vector3(12.4f, 3.4f, 0.4f), wallColor);

            // Tường trái có cửa sổ ngắm bắn
            CreateBox(houseRoot.transform, "Wall_Left_Bottom", new Vector3(-6.0f, 0.6f, 0f), new Vector3(0.4f, 1.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Left_Top", new Vector3(-6.0f, 3.0f, 0f), new Vector3(0.4f, 1.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Left_Pillar1", new Vector3(-6.0f, 1.8f, -3.2f), new Vector3(0.4f, 1.4f, 4.0f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Left_Pillar2", new Vector3(-6.0f, 1.8f, 3.2f), new Vector3(0.4f, 1.4f, 4.0f), wallColor);

            // Tường phải có cửa sổ ngắm bắn
            CreateBox(houseRoot.transform, "Wall_Right_Bottom", new Vector3(6.0f, 0.6f, 0f), new Vector3(0.4f, 1.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Right_Top", new Vector3(6.0f, 3.0f, 0f), new Vector3(0.4f, 1.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Right_Pillar1", new Vector3(6.0f, 1.8f, -3.2f), new Vector3(0.4f, 1.4f, 4.0f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Right_Pillar2", new Vector3(6.0f, 1.8f, 3.2f), new Vector3(0.4f, 1.4f, 4.0f), wallColor);

            // Tường trước: Cửa chính mở rộng 2.4m, cao 2.6m
            CreateBox(houseRoot.transform, "Wall_Front_Left", new Vector3(-3.8f, 1.8f, -5.0f), new Vector3(4.8f, 3.4f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Front_Right", new Vector3(3.8f, 1.8f, -5.0f), new Vector3(4.8f, 3.4f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_Front_Header", new Vector3(0f, 3.0f, -5.0f), new Vector3(2.8f, 1.0f, 0.4f), trimColor);

            // Sàn Tầng 2 có chừa giếng thang (Stairwell opening)
            CreateBox(houseRoot.transform, "Floor2_Main", new Vector3(-1.8f, 3.6f, 0f), new Vector3(8.8f, 0.25f, 10.4f), floorColor);
            CreateBox(houseRoot.transform, "Floor2_Edge", new Vector3(4.4f, 3.6f, -3.2f), new Vector3(3.6f, 0.25f, 4.0f), floorColor);

            // Cầu thang thoải đi từ Tầng 1 lên Tầng 2
            GameObject stairs = CreateBox(houseRoot.transform, "StairRamp_F1_F2", new Vector3(4.3f, 1.8f, 0.9f), new Vector3(2.2f, 0.2f, 5.4f), floorColor);
            stairs.transform.localRotation = Quaternion.Euler(-34f, 0f, 0f);

            // Tầng 2: Lan can & Tường ngắm bắn
            CreateBox(houseRoot.transform, "Wall_F2_Back", new Vector3(0f, 5.2f, 5.0f), new Vector3(12.4f, 3.0f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_F2_Left", new Vector3(-6.0f, 5.2f, 0f), new Vector3(0.4f, 3.0f, 10.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_F2_Right", new Vector3(6.0f, 5.2f, 0f), new Vector3(0.4f, 3.0f, 10.4f), wallColor);

            // Ban công & Cửa sổ tầng 2 nhìn ra sân trước
            CreateBox(houseRoot.transform, "Wall_F2_Front_L", new Vector3(-3.8f, 5.2f, -5.0f), new Vector3(4.8f, 3.0f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_F2_Front_R", new Vector3(3.8f, 5.2f, -5.0f), new Vector3(4.8f, 3.0f, 0.4f), wallColor);
            CreateBox(houseRoot.transform, "Wall_F2_Front_Railing", new Vector3(0f, 4.3f, -5.0f), new Vector3(2.8f, 1.2f, 0.2f), trimColor);

            // Mái nhà che mưa có chừa lối lên nóc
            CreateBox(houseRoot.transform, "Roof_Slab", new Vector3(-1.4f, 6.8f, 0f), new Vector3(9.6f, 0.3f, 10.8f), roofColor);
            GameObject roofRamp = CreateBox(houseRoot.transform, "RoofAccessRamp", new Vector3(4.3f, 5.2f, 1.0f), new Vector3(2.2f, 0.2f, 5.4f), floorColor);
            roofRamp.transform.localRotation = Quaternion.Euler(-34f, 0f, 0f);

            // Lan can sân thượng bắn tỉa (Sniper parapet)
            CreateBox(houseRoot.transform, "Roof_Parapet_Front", new Vector3(0f, 7.3f, -5.3f), new Vector3(12.6f, 0.9f, 0.3f), trimColor);
            CreateBox(houseRoot.transform, "Roof_Parapet_Back", new Vector3(0f, 7.3f, 5.3f), new Vector3(12.6f, 0.9f, 0.3f), trimColor);

            if (spawnInternalLoot)
            {
                Vector3 rotVec(Vector3 local) => worldPos + (Quaternion.Euler(0f, yRotation, 0f) * local);
                LootItem.SpawnLoot(rotVec(new Vector3(-3.2f, 0.25f, 1.8f)), LootCategory.Weapon, WeaponId.M416, 60, 2, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(-3.2f, 0.25f, -2.4f)), LootCategory.Ammo, WeaponId.M416, 60, 1, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(1.4f, 0.25f, 3.0f)), LootCategory.Medkit, WeaponId.M416, 1, 2, parent);

                LootItem.SpawnLoot(rotVec(new Vector3(-3.0f, 3.85f, 2.0f)), LootCategory.Weapon, WeaponId.AKM, 60, 2, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(0f, 3.85f, -2.8f)), LootCategory.ArmorVest, WeaponId.M416, 1, 2, parent);
                LootItem.SpawnLoot(rotVec(new Vector3(-3.5f, 3.85f, -2.5f)), LootCategory.Helmet, WeaponId.M416, 1, 2, parent);

                LootItem.SpawnLoot(rotVec(new Vector3(0f, 7.1f, 0f)), LootCategory.Weapon, WeaponId.Kar98k, 25, 3, parent);
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
            CreateBox(house.transform, "Wall_Left_Pillar1", new Vector3(-5.2f, 1.7f, -2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);
            CreateBox(house.transform, "Wall_Left_Pillar2", new Vector3(-5.2f, 1.7f, 2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);

            CreateBox(house.transform, "Wall_Right_Bottom", new Vector3(5.2f, 0.6f, 0f), new Vector3(0.4f, 1.0f, 8.8f), wallColor);
            CreateBox(house.transform, "Wall_Right_Top", new Vector3(5.2f, 2.8f, 0f), new Vector3(0.4f, 1.0f, 8.8f), wallColor);
            CreateBox(house.transform, "Wall_Right_Pillar1", new Vector3(5.2f, 1.7f, -2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);
            CreateBox(house.transform, "Wall_Right_Pillar2", new Vector3(5.2f, 1.7f, 2.6f), new Vector3(0.4f, 1.2f, 3.6f), wallColor);

            // Tường trước: Cửa chính mở rộng 2.2m
            CreateBox(house.transform, "Wall_Front_Left", new Vector3(-3.4f, 1.7f, -4.2f), new Vector3(4.0f, 3.2f, 0.4f), wallColor);
            CreateBox(house.transform, "Wall_Front_Right", new Vector3(3.4f, 1.7f, -4.2f), new Vector3(4.0f, 3.2f, 0.4f), wallColor);
            CreateBox(house.transform, "Wall_Front_Header", new Vector3(0f, 2.8f, -4.2f), new Vector3(2.8f, 1.0f, 0.4f), wallColor);

            // Mái che và cầu thang bên ngoài leo lên nóc nhà bắn tỉa
            CreateBox(house.transform, "Roof", new Vector3(0f, 3.4f, 0f), new Vector3(11.4f, 0.25f, 9.4f), roofColor);
            GameObject sideStairs = CreateBox(house.transform, "OutsideStairs", new Vector3(6.5f, 1.7f, 0f), new Vector3(1.8f, 0.2f, 6.2f), floorColor);
            sideStairs.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);

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
