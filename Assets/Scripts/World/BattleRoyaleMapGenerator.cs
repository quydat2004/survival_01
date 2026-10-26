using Survival.Core;
using Survival.Items;
using UnityEngine;

namespace Survival.World
{
    public struct NamedLocation
    {
        public string name;
        public Vector3 position;

        public NamedLocation(string n, Vector3 pos)
        {
            name = n;
            position = pos;
        }
    }

    public class BattleRoyaleMapGenerator : MonoBehaviour
    {
        private static BattleRoyaleMapGenerator instance;
        public static BattleRoyaleMapGenerator Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<BattleRoyaleMapGenerator>();
                return instance;
            }
            private set => instance = value;
        }

        public const float IslandSize = 540f;

        public static readonly NamedLocation[] Locations =
        {
            new NamedLocation("POCHINKI", new Vector3(0f, 0f, 0f)),
            new NamedLocation("SCHOOL", new Vector3(0f, 0f, 125f)),
            new NamedLocation("MILITARY BASE", new Vector3(115f, 0f, -110f)),
            new NamedLocation("GEORGOPOL", new Vector3(-125f, 0f, 110f)),
            new NamedLocation("ROZHOK", new Vector3(120f, 0f, 85f)),
            new NamedLocation("PRISON", new Vector3(-120f, 0f, -105f))
        };

        private GameObject worldRoot;

        private void Awake()
        {
            if ( instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public Transform RebuildWorld(int seed = 2026)
        {
            if (worldRoot != null)
            {
                MaterialPool.SafeDestroy(worldRoot);
                worldRoot = null;
            }

            LootItem.ActiveLoot.Clear();
            Random.InitState(seed);

            worldRoot = new GameObject("BattleRoyaleIsland");
            Transform root = worldRoot.transform;

            BuildAtmosphereAndGround(root);
            BuildRoads(root);

            BuildPochinkiTown(root, Locations[0].position);
            BuildSchoolComplex(root, Locations[1].position);
            BuildMilitaryBase(root, Locations[2].position);
            BuildGeorgopolContainers(root, Locations[3].position);
            BuildRozhokOutpost(root, Locations[4].position);
            BuildPrisonCompound(root, Locations[5].position);

            BuildWildernessCover(root);

            return root;
        }

        public Vector3 GetRandomDropPosition(int index, int totalCount)
        {
            float angle = (index / Mathf.Max(1f, (float)totalCount)) * Mathf.PI * 2f + Random.Range(-0.22f, 0.22f);
            float radius = Random.Range(35f, 165f);
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private void BuildAtmosphereAndGround(Transform root)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 120f;
            RenderSettings.fogEndDistance = 390f;
            RenderSettings.fogColor = new Color(0.68f, 0.78f, 0.86f);
            RenderSettings.ambientLight = new Color(0.46f, 0.49f, 0.52f);

            CreateSolidBox(root, "OceanWater", new Vector3(0f, -1.3f, 0f), new Vector3(1400f, 1.0f, 1400f), new Color(0.11f, 0.34f, 0.54f));
            CreateSolidBox(root, "IslandBeach", new Vector3(0f, -0.65f, 0f), new Vector3(IslandSize + 36f, 1.0f, IslandSize + 36f), new Color(0.76f, 0.70f, 0.50f));
            CreateSolidBox(root, "IslandTerrain", new Vector3(0f, -0.25f, 0f), new Vector3(IslandSize, 0.5f, IslandSize), new Color(0.29f, 0.45f, 0.24f));
        }

        private void BuildRoads(Transform root)
        {
            Color roadColor = new Color(0.32f, 0.31f, 0.29f);
            CreateSolidBox(root, "MainRoad_NS", new Vector3(0f, 0.02f, 0f), new Vector3(9f, 0.04f, 440f), roadColor);
            CreateSolidBox(root, "MainRoad_EW", new Vector3(0f, 0.02f, 0f), new Vector3(440f, 0.04f, 9f), roadColor);

            Transform pathsRoot = new GameObject("StoneRoadPaths").transform;
            pathsRoot.SetParent(root, false);
            for (int i = -6; i <= 6; i++)
            {
                if (i == 0) continue;
                ModelCatalog.SpawnNatureModel(pathsRoot, "RockPath_Round_Wide", new Vector3(i * 28f, 0.03f, 6.2f), 0f, 1.2f, false, false, false);
                ModelCatalog.SpawnNatureModel(pathsRoot, "RockPath_Round_Wide", new Vector3(6.2f, 0.03f, i * 28f), 90f, 1.2f, false, false, false);
            }
        }

        private void BuildPochinkiTown(Transform root, Vector3 center)
        {
            Transform town = new GameObject("POI_Pochinki").transform;
            town.SetParent(root, false);

            // Các tòa nhà 2 tầng và 1 tầng hoàn chỉnh có cửa vào, cầu thang, ban công ngắm bắn
            EnterableStructureBuilder.BuildTwoStoryTacticalHouse(town, center + new Vector3(-20f, 0f, 18f), 0f, new Color(0.78f, 0.70f, 0.58f), new Color(0.48f, 0.32f, 0.22f), true);
            EnterableStructureBuilder.BuildTwoStoryTacticalHouse(town, center + new Vector3(20f, 0f, 18f), 180f, new Color(0.68f, 0.56f, 0.48f), new Color(0.38f, 0.24f, 0.18f), true);
            EnterableStructureBuilder.BuildSingleStoryEnterableHouse(town, center + new Vector3(-20f, 0f, -18f), 0f, new Color(0.62f, 0.68f, 0.72f), new Color(0.32f, 0.22f, 0.18f), true);
            EnterableStructureBuilder.BuildSingleStoryEnterableHouse(town, center + new Vector3(20f, 0f, -18f), 180f, new Color(0.74f, 0.64f, 0.52f), new Color(0.36f, 0.24f, 0.18f), true);

            // 3D City Buildings surrounding Pochinki plaza
            ModelCatalog.SpawnBuildingModel(town, "building-a", center + new Vector3(-38f, 0f, 18f), 90f, 4.8f, 0);
            ModelCatalog.SpawnBuildingModel(town, "building-b", center + new Vector3(38f, 0f, 18f), -90f, 4.8f, 1);
            ModelCatalog.SpawnBuildingModel(town, "building-c", center + new Vector3(-38f, 0f, -18f), 90f, 4.8f, 2);
            ModelCatalog.SpawnBuildingModel(town, "building-d", center + new Vector3(38f, 0f, -18f), -90f, 4.8f, 3);
            ModelCatalog.SpawnBuildingModel(town, "windmill", center + new Vector3(36f, 0f, 36f), -45f, 5.5f, 0);
            ModelCatalog.SpawnBuildingModel(town, "water-tower", center + new Vector3(-34f, 0f, -34f), 0f, 5.2f, 0);

            BuildWatchtower(town, center + new Vector3(0f, 0f, 28f));

            for (int i = -2; i <= 2; i++)
            {
                BuildCoverCluster(town, center + new Vector3(i * 11f, 0f, 0f));
            }
        }

        private void BuildSchoolComplex(Transform root, Vector3 center)
        {
            Transform school = new GameObject("POI_School").transform;
            school.SetParent(root, false);

            Color brickColor = new Color(0.68f, 0.44f, 0.34f);
            EnterableStructureBuilder.BuildTwoStoryTacticalHouse(school, center + new Vector3(-16f, 0f, 0f), 90f, brickColor, new Color(0.42f, 0.28f, 0.20f), true);
            EnterableStructureBuilder.BuildTwoStoryTacticalHouse(school, center + new Vector3(16f, 0f, 0f), -90f, brickColor, new Color(0.42f, 0.28f, 0.20f), true);

            // Multi-story 3D campus buildings
            ModelCatalog.SpawnBuildingModel(school, "building-l", center + new Vector3(0f, 0f, 24f), 180f, 5.4f, 1);
            ModelCatalog.SpawnBuildingModel(school, "building-m", center + new Vector3(-34f, 0f, 14f), 90f, 5.0f, 2);
            ModelCatalog.SpawnBuildingModel(school, "building-n", center + new Vector3(34f, 0f, 14f), -90f, 5.0f, 3);
            ModelCatalog.SpawnBuildingModel(school, "building-o", center + new Vector3(0f, 0f, -34f), 0f, 5.2f, 0);
            ModelCatalog.SpawnBuildingModel(school, "solar-panel-landscape-group", center + new Vector3(24f, 0f, -20f), 0f, 5.0f, 0);

            BuildWatchtower(school, center + new Vector3(0f, 0f, -20f));
        }

        private void BuildMilitaryBase(Transform root, Vector3 center)
        {
            Transform mil = new GameObject("POI_MilitaryBase").transform;
            mil.SetParent(root, false);

            Color hangarColor = new Color(0.34f, 0.40f, 0.32f);
            EnterableStructureBuilder.BuildEnterableMilitaryHangar(mil, center + new Vector3(-20f, 0f, 0f), 0f, hangarColor, true);
            EnterableStructureBuilder.BuildEnterableMilitaryHangar(mil, center + new Vector3(20f, 0f, 0f), 180f, hangarColor, true);
            BuildWatchtower(mil, center + new Vector3(0f, 0f, 24f));

            ModelCatalog.SpawnBuildingModel(mil, "building-p", center + new Vector3(0f, 0f, -24f), 0f, 5.4f, 3);
            ModelCatalog.SpawnBuildingModel(mil, "building-q", center + new Vector3(-36f, 0f, -18f), 90f, 5.0f, 2);
            ModelCatalog.SpawnBuildingModel(mil, "building-r", center + new Vector3(36f, 0f, -18f), -90f, 5.0f, 2);
            ModelCatalog.SpawnBuildingModel(mil, "water-tower", center + new Vector3(-28f, 0f, 24f), 0f, 6.2f, 3);
            ModelCatalog.SpawnBuildingModel(mil, "detail-tank-large", center + new Vector3(26f, 0f, 22f), 0f, 6.0f, 1);
            ModelCatalog.SpawnBuildingModel(mil, "shipping-container-a", center + new Vector3(-8f, 0f, 14f), 15f, 8.5f, 0);
            ModelCatalog.SpawnBuildingModel(mil, "shipping-container-b", center + new Vector3(8f, 0f, 14f), -12f, 8.5f, 2);
        }

        private void BuildGeorgopolContainers(Transform root, Vector3 center)
        {
            Transform port = new GameObject("POI_Georgopol").transform;
            port.SetParent(root, false);

            for (int x = -2; x <= 2; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 cPos = center + new Vector3(x * 11f, 0f, z * 9.5f);
                    string containerModel = ModelCatalog.ContainerNames[Mathf.Abs(x + z * 2) % ModelCatalog.ContainerNames.Length];
                    int palette = Mathf.Abs(x * 3 + z) % 4;
                    float rotY = ((x + z) % 2 == 0) ? 0f : 90f;

                    GameObject spawned = ModelCatalog.SpawnBuildingModel(port, containerModel, cPos, rotY, 8.8f, palette, true);
                    if (spawned == null)
                    {
                        CreateSolidBox(port, "CargoContainer", cPos + Vector3.up * 1.4f, new Vector3(3.4f, 2.8f, 7.2f), new Color(0.18f, 0.42f, 0.72f));
                    }
                }
            }

            ModelCatalog.SpawnBuildingModel(port, "building-k", center + new Vector3(0f, 0f, 24f), 180f, 5.2f, 1);
            ModelCatalog.SpawnBuildingModel(port, "water-tower", center + new Vector3(-32f, 0f, 0f), 0f, 5.8f, 2);

            SpawnRandomCompoundLoot(port, center, 12, true);
        }

        private void BuildRozhokOutpost(Transform root, Vector3 center)
        {
            Transform roz = new GameObject("POI_Rozhok").transform;
            roz.SetParent(root, false);

            EnterableStructureBuilder.BuildTwoStoryTacticalHouse(roz, center + new Vector3(-16f, 0f, -12f), 45f, new Color(0.65f, 0.60f, 0.50f), new Color(0.38f, 0.28f, 0.20f), true);
            EnterableStructureBuilder.BuildSingleStoryEnterableHouse(roz, center + new Vector3(16f, 0f, 12f), -135f, new Color(0.60f, 0.66f, 0.55f), new Color(0.34f, 0.24f, 0.18f), true);

            ModelCatalog.SpawnBuildingModel(roz, "building-e", center + new Vector3(-24f, 0f, 14f), 90f, 4.8f, 0);
            ModelCatalog.SpawnBuildingModel(roz, "building-g", center + new Vector3(24f, 0f, -14f), -90f, 4.8f, 1);
            ModelCatalog.SpawnBuildingModel(roz, "building-i", center + new Vector3(0f, 0f, 24f), 180f, 4.8f, 2);
            ModelCatalog.SpawnBuildingModel(roz, "windmill-low", center + new Vector3(0f, 0f, -24f), 0f, 5.2f, 3);

            BuildWatchtower(roz, center);
        }

        private void BuildPrisonCompound(Transform root, Vector3 center)
        {
            Transform prison = new GameObject("POI_Prison").transform;
            prison.SetParent(root, false);

            Color wallCol = new Color(0.48f, 0.48f, 0.50f);
            CreateSolidBox(prison, "PerimeterN", center + new Vector3(0f, 1.6f, 28f), new Vector3(48f, 3.2f, 1.0f), wallCol);
            CreateSolidBox(prison, "PerimeterS", center + new Vector3(0f, 1.6f, -28f), new Vector3(48f, 3.2f, 1.0f), wallCol);

            EnterableStructureBuilder.BuildTwoStoryTacticalHouse(prison, center, 0f, new Color(0.55f, 0.55f, 0.58f), new Color(0.32f, 0.32f, 0.35f), true);
            ModelCatalog.SpawnBuildingModel(prison, "building-s", center + new Vector3(-18f, 0f, -12f), 90f, 5.0f, 3);
            ModelCatalog.SpawnBuildingModel(prison, "building-t", center + new Vector3(18f, 0f, 12f), -90f, 5.0f, 2);
            ModelCatalog.SpawnBuildingModel(prison, "building-j", center + new Vector3(18f, 0f, -14f), -90f, 4.8f, 0);

            BuildWatchtower(prison, center + new Vector3(-20f, 0f, 20f));
        }

        private void BuildEnterableHouse(Transform parent, Vector3 pos, Color wallColor, bool hasRoofRamp)
        {
            Transform house = new GameObject("House").transform;
            house.SetParent(parent, false);
            house.position = pos;

            Color floorColor = new Color(0.45f, 0.38f, 0.30f);
            Color roofColor = new Color(0.38f, 0.22f, 0.18f);

            CreateSolidBox(house, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(11f, 0.2f, 9f), floorColor);
            CreateSolidBox(house, "BackWall", new Vector3(0f, 1.9f, 4.3f), new Vector3(11f, 3.6f, 0.4f), wallColor);
            CreateSolidBox(house, "LeftWall", new Vector3(-5.3f, 1.9f, 0f), new Vector3(0.4f, 3.6f, 9f), wallColor);
            CreateSolidBox(house, "RightWall", new Vector3(5.3f, 1.9f, 0f), new Vector3(0.4f, 3.6f, 9f), wallColor);
            CreateSolidBox(house, "FrontWallL", new Vector3(-3.4f, 1.9f, -4.3f), new Vector3(4.2f, 3.6f, 0.4f), wallColor);
            CreateSolidBox(house, "FrontWallR", new Vector3(3.4f, 1.9f, -4.3f), new Vector3(4.2f, 3.6f, 0.4f), wallColor);
            CreateSolidBox(house, "Roof", new Vector3(0f, 3.8f, 0f), new Vector3(11.6f, 0.3f, 9.6f), roofColor);

            if (hasRoofRamp)
            {
                GameObject ramp = CreateSolidBox(house, "RoofRamp", new Vector3(6.8f, 1.9f, 0f), new Vector3(2.4f, 0.25f, 7.6f), floorColor);
                ramp.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            }

            SpawnRandomLootAtPoint(parent, pos + new Vector3(-2.2f, 0.2f, 1.2f), true);
            SpawnRandomLootAtPoint(parent, pos + new Vector3(2.2f, 0.2f, -1.0f), false);
            if (hasRoofRamp)
            {
                SpawnRandomLootAtPoint(parent, pos + new Vector3(0f, 4.0f, 0f), true);
            }
        }

        private void BuildHangar(Transform parent, Vector3 pos, Color color)
        {
            Transform hangar = new GameObject("MilitaryHangar").transform;
            hangar.SetParent(parent, false);
            hangar.position = pos;

            CreateSolidBox(hangar, "Floor", new Vector3(0f, 0.1f, 0f), new Vector3(14f, 0.2f, 18f), new Color(0.38f, 0.38f, 0.40f));
            CreateSolidBox(hangar, "WallL", new Vector3(-6.8f, 2.4f, 0f), new Vector3(0.4f, 4.6f, 18f), color);
            CreateSolidBox(hangar, "WallR", new Vector3(6.8f, 2.4f, 0f), new Vector3(0.4f, 4.6f, 18f), color);
            CreateSolidBox(hangar, "Roof", new Vector3(0f, 4.8f, 0f), new Vector3(14.4f, 0.4f, 18.4f), color * 0.85f);
        }

        private void BuildWatchtower(Transform parent, Vector3 pos)
        {
            Transform tower = new GameObject("SniperTower").transform;
            tower.SetParent(parent, false);
            tower.position = pos;

            Color wood = new Color(0.48f, 0.34f, 0.20f);
            CreateSolidBox(tower, "Leg1", new Vector3(-1.8f, 2.5f, -1.8f), new Vector3(0.4f, 5f, 0.4f), wood);
            CreateSolidBox(tower, "Leg2", new Vector3(1.8f, 2.5f, -1.8f), new Vector3(0.4f, 5f, 0.4f), wood);
            CreateSolidBox(tower, "Leg3", new Vector3(-1.8f, 2.5f, 1.8f), new Vector3(0.4f, 5f, 0.4f), wood);
            CreateSolidBox(tower, "Leg4", new Vector3(1.8f, 2.5f, 1.8f), new Vector3(0.4f, 5f, 0.4f), wood);
            CreateSolidBox(tower, "Platform", new Vector3(0f, 5.0f, 0f), new Vector3(4.6f, 0.3f, 4.6f), wood);

            GameObject ramp = CreateSolidBox(tower, "TowerRamp", new Vector3(0f, 2.5f, -4.2f), new Vector3(1.8f, 0.25f, 6.8f), wood);
            ramp.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);

            LootItem.SpawnLoot(pos + new Vector3(0f, 5.2f, 0f), LootCategory.Weapon, WeaponId.Kar98k, 15, 2, parent);
        }

        private void BuildCoverCluster(Transform parent, Vector3 pos)
        {
            CreateSolidBox(parent, "CrateA", pos + new Vector3(0f, 0.7f, 0f), new Vector3(1.4f, 1.4f, 1.4f), new Color(0.56f, 0.42f, 0.24f));
            CreateSolidBox(parent, "LowWall", pos + new Vector3(2.2f, 0.65f, 0.5f), new Vector3(2.8f, 1.3f, 0.5f), new Color(0.52f, 0.52f, 0.54f));
        }

        private void BuildWildernessCover(Transform root)
        {
            Transform nature = new GameObject("WildernessCover").transform;
            nature.SetParent(root, false);

            for (int i = 0; i < 150; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-235f, 235f), 0f, Random.Range(-235f, 235f));
                if (IsInsideRoadOrPoiCore(pos))
                    continue;

                float rotY = Random.Range(0f, 360f);
                int mod = i % 5;

                if (mod == 0)
                {
                    string rockName = ModelCatalog.RockNames[Random.Range(0, ModelCatalog.RockNames.Length)];
                    float scale = Random.Range(0.85f, 1.45f);
                    GameObject rock = ModelCatalog.SpawnNatureModel(nature, rockName, pos, rotY, scale, false, true, true);
                    if (rock == null)
                    {
                        MaterialPool.CreatePrimitive(PrimitiveType.Sphere, nature, "RockCover", pos + Vector3.up * 0.8f, Vector3.one * 2.2f, new Color(0.46f, 0.47f, 0.49f), true);
                    }
                }
                else if (mod == 4)
                {
                    string bushName = ModelCatalog.FoliageNames[Random.Range(0, ModelCatalog.FoliageNames.Length)];
                    float scale = Random.Range(1.0f, 1.6f);
                    ModelCatalog.SpawnNatureModel(nature, bushName, pos, rotY, scale, false, false, false);
                }
                else
                {
                    string treeName = ModelCatalog.TreeNames[Random.Range(0, ModelCatalog.TreeNames.Length)];
                    float scale = Random.Range(0.95f, 1.45f);
                    GameObject tree = ModelCatalog.SpawnNatureModel(nature, treeName, pos, rotY, scale, true, false, true);
                    if (tree == null)
                    {
                        MaterialPool.CreatePrimitive(PrimitiveType.Cylinder, nature, "TreeTrunk", pos + Vector3.up * 1.8f, new Vector3(0.55f, 1.8f, 0.55f), new Color(0.40f, 0.26f, 0.15f), true);
                    }
                }
            }
        }

        private static bool IsInsideRoadOrPoiCore(Vector3 pos)
        {
            if (Mathf.Abs(pos.x) < 11f || Mathf.Abs(pos.z) < 11f)
                return true;

            for (int i = 0; i < Locations.Length; i++)
            {
                if (Vector3.Distance(pos, Locations[i].position) < 34f)
                    return true;
            }
            return false;
        }

        private void SpawnRandomCompoundLoot(Transform parent, Vector3 center, int count, bool highTierBias)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 18f;
                Vector3 pos = center + new Vector3(offset.x, 0f, offset.y);
                SpawnRandomLootAtPoint(parent, pos, highTierBias);
            }
        }

        private void SpawnRandomLootAtPoint(Transform parent, Vector3 pos, bool highTier)
        {
            float roll = Random.value;
            if (roll < 0.38f)
            {
                WeaponId[] pool = highTier
                    ? new[] { WeaponId.M416, WeaponId.AKM, WeaponId.UMP45, WeaponId.Kar98k, WeaponId.S1897 }
                    : new[] { WeaponId.UMP45, WeaponId.S1897, WeaponId.P1911, WeaponId.AKM, WeaponId.M416 };
                WeaponId chosen = pool[Random.Range(0, pool.Length)];
                LootItem.SpawnLoot(pos, LootCategory.Weapon, chosen, 30, 1, parent);
            }
            else if (roll < 0.54f)
            {
                LootItem.SpawnLoot(pos, LootCategory.Ammo, WeaponId.M416, Random.Range(35, 75), 1, parent);
            }
            else if (roll < 0.68f)
            {
                LootItem.SpawnLoot(pos, Random.value < 0.55f ? LootCategory.Medkit : LootCategory.Bandage, WeaponId.M416, 1, 1, parent);
            }
            else if (roll < 0.80f)
            {
                LootItem.SpawnLoot(pos, LootCategory.EnergyDrink, WeaponId.M416, 1, 1, parent);
            }
            else if (roll < 0.92f)
            {
                int tier = highTier ? Random.Range(2, 4) : Random.Range(1, 3);
                LootCategory gearCat = Random.value < 0.5f ? LootCategory.ArmorVest : LootCategory.Helmet;
                LootItem.SpawnLoot(pos, gearCat, WeaponId.M416, 1, tier, parent);
            }
            else
            {
                LootItem.SpawnLoot(pos, LootCategory.FragGrenade, WeaponId.M416, 1, 1, parent);
            }
        }

        private static GameObject CreateSolidBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            return MaterialPool.CreatePrimitive(PrimitiveType.Cube, parent, name, localPos, scale, color, true);
        }
    }
}
