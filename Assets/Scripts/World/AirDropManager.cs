using System.Collections.Generic;
using Survival.Core;
using Survival.Items;
using UnityEngine;

namespace Survival.World
{
    public class AirDropManager : MonoBehaviour
    {
        private static AirDropManager instance;
        public static AirDropManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<AirDropManager>();
                return instance;
            }
            private set => instance = value;
        }
        public static readonly List<Vector3> ActiveAirDropLocations = new List<Vector3>();

        private readonly List<GameObject> spawnedCrates = new List<GameObject>();
        private float dropTimer = 38f;
        private bool matchActive = false;
        private Transform containerRoot;

        public event System.Action<Vector3> OnAirDropSpawned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void BeginAirDrops(Transform parentRoot)
        {
            CleanupSpawnedCrates();
            containerRoot = parentRoot;
            ActiveAirDropLocations.Clear();
            dropTimer = 35f;
            matchActive = true;
        }

        public void StopAirDrops()
        {
            matchActive = false;
            ActiveAirDropLocations.Clear();
            CleanupSpawnedCrates();
        }

        private void CleanupSpawnedCrates()
        {
            for (int i = spawnedCrates.Count - 1; i >= 0; i--)
            {
                if (spawnedCrates[i] != null)
                    MaterialPool.SafeDestroy(spawnedCrates[i]);
            }
            spawnedCrates.Clear();
        }

        private void Update()
        {
            if (!matchActive) return;

            dropTimer -= Time.deltaTime;
            if (dropTimer <= 0f)
            {
                dropTimer = 50f;
                SpawnSupplyCrate();
            }
        }

        public void SpawnSupplyCrate()
        {
            Vector3 center = BlueZoneController.Instance != null ? BlueZoneController.Instance.TargetCenter : Vector3.zero;
            float maxRad = BlueZoneController.Instance != null ? Mathf.Min(110f, BlueZoneController.Instance.TargetRadius * 0.7f) : 90f;
            Vector2 rand = Random.insideUnitCircle * maxRad;
            Vector3 landPos = new Vector3(center.x + rand.x, 0f, center.z + rand.y);

            GameObject crateObj = new GameObject("SupplyDropCrate");
            if (containerRoot != null)
                crateObj.transform.SetParent(containerRoot, false);

            crateObj.transform.position = landPos + Vector3.up * 68f;
            AirDropCrateBehaviour crate = crateObj.AddComponent<AirDropCrateBehaviour>();
            crate.Initialize(landPos, containerRoot);

            spawnedCrates.Add(crateObj);
            ActiveAirDropLocations.Add(landPos);
            OnAirDropSpawned?.Invoke(landPos);
            GameEvents.RaiseAirDropSpawned(landPos);
        }
    }

    public class AirDropCrateBehaviour : MonoBehaviour
    {
        private Vector3 targetGroundPos;
        private Transform lootParent;
        private GameObject parachuteObj;
        private GameObject smokeBeaconObj;
        private bool landed = false;

        public void Initialize(Vector3 groundPos, Transform parent)
        {
            targetGroundPos = groundPos;
            lootParent = parent;

            MaterialPool.CreatePrimitive(
                PrimitiveType.Cube,
                transform,
                "RedBox",
                new Vector3(0f, 0.65f, 0f),
                new Vector3(1.5f, 1.3f, 1.5f),
                new Color(0.82f, 0.14f, 0.14f),
                true);

            MaterialPool.CreatePrimitive(
                PrimitiveType.Cube,
                transform,
                "BlueTarp",
                new Vector3(0f, 1.15f, 0f),
                new Vector3(1.56f, 0.36f, 1.56f),
                new Color(0.14f, 0.34f, 0.78f),
                false);

            parachuteObj = MaterialPool.CreatePrimitive(
                PrimitiveType.Sphere,
                transform,
                "ParachuteCanopy",
                new Vector3(0f, 5.2f, 0f),
                new Vector3(4.5f, 1.4f, 4.5f),
                new Color(0.32f, 0.38f, 0.28f),
                false);
        }

        private void Update()
        {
            if (!landed)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetGroundPos, 8.5f * Time.deltaTime);
                transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.World);

                if (Vector3.Distance(transform.position, targetGroundPos) <= 0.05f)
                {
                    landed = true;
                    transform.position = targetGroundPos;
                    if (parachuteObj != null) Destroy(parachuteObj);
                    OnLanded();
                }
            }
        }

        private void OnLanded()
        {
            smokeBeaconObj = MaterialPool.CreatePrimitive(
                PrimitiveType.Cylinder,
                transform,
                "RedSmokeBeacon",
                new Vector3(0f, 14f, 0f),
                new Vector3(0.45f, 14f, 0.45f),
                new Color(0.95f, 0.20f, 0.20f, 0.55f),
                false);

            Vector3 p = targetGroundPos;
            LootItem.SpawnLoot(p + new Vector3(1.8f, 0f, 0f), LootCategory.Weapon, WeaponId.AWM, 15, 3, lootParent);
            LootItem.SpawnLoot(p + new Vector3(-1.8f, 0f, 0f), LootCategory.Weapon, WeaponId.M416, 60, 3, lootParent);
            LootItem.SpawnLoot(p + new Vector3(0f, 0f, 1.8f), LootCategory.ArmorVest, WeaponId.M416, 1, 3, lootParent);
            LootItem.SpawnLoot(p + new Vector3(0f, 0f, -1.8f), LootCategory.Helmet, WeaponId.M416, 1, 3, lootParent);
            LootItem.SpawnLoot(p + new Vector3(1.4f, 0f, 1.4f), LootCategory.Medkit, WeaponId.M416, 1, 3, lootParent);
            LootItem.SpawnLoot(p + new Vector3(-1.4f, 0f, -1.4f), LootCategory.EnergyDrink, WeaponId.M416, 1, 3, lootParent);
        }
    }
}
