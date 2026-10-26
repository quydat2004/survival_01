using System.Collections.Generic;
using Survival.Core;
using UnityEngine;

namespace Survival.Items
{
    public class LootItem : MonoBehaviour
    {
        public static readonly List<LootItem> ActiveLoot = new List<LootItem>();

        public LootCategory category = LootCategory.Weapon;
        public WeaponId weaponId = WeaponId.M416;
        public int amount = 30;
        public int gearTier = 1;
        public string displayLabel = "M416 Tactical";
        public bool IsConsumed { get; private set; } = false;

        private Vector3 basePosition;
        private float bobOffset;
        private Transform visualRoot;

        private void OnEnable()
        {
            if (!IsConsumed && !ActiveLoot.Contains(this))
                ActiveLoot.Add(this);
        }

        private void OnDisable()
        {
            ActiveLoot.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveLoot.Remove(this);
        }

        public void ConsumeAndDestroy()
        {
            if (IsConsumed) return;
            IsConsumed = true;
            ActiveLoot.Remove(this);
            MaterialPool.SafeDestroy(gameObject);
        }

        private void Start()
        {
            basePosition = transform.position;
            bobOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (visualRoot != null)
            {
                visualRoot.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);
                float yOffset = Mathf.Sin(Time.time * 2.4f + bobOffset) * 0.08f;
                visualRoot.position = basePosition + new Vector3(0f, 0.28f + yOffset, 0f);
            }
        }

        public static LootItem SpawnLoot(
            Vector3 position,
            LootCategory cat,
            WeaponId wep = WeaponId.M416,
            int amt = 30,
            int tier = 1,
            Transform parent = null)
        {
            GameObject go = new GameObject("Loot_" + cat + "_" + wep);
            if (parent != null)
                go.transform.SetParent(parent, false);

            go.transform.position = position;

            SphereCollider col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.6f;

            LootItem item = go.AddComponent<LootItem>();
            if (!ActiveLoot.Contains(item))
                ActiveLoot.Add(item);
            item.category = cat;
            item.weaponId = wep;
            item.amount = amt;
            item.gearTier = Mathf.Clamp(tier, 1, 3);
            item.basePosition = position;
            item.BuildVisuals();

            return item;
        }

        private void BuildVisuals()
        {
            GameObject root = new GameObject("Visual");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            visualRoot = root.transform;

            Color mainColor = Color.white;
            Color accentColor = new Color(1f, 0.8f, 0.2f);

            switch (category)
            {
                case LootCategory.Weapon:
                    WeaponSpec spec = WeaponDatabase.GetSpec(weaponId);
                    displayLabel = spec.displayName + " [" + spec.shortCategory + "]";
                    mainColor = spec.bodyColor;
                    accentColor = weaponId == WeaponId.AWM
                        ? new Color(1f, 0.25f, 0.25f)
                        : new Color(0.95f, 0.75f, 0.20f);

                    GameObject wepModel = ModelCatalog.SpawnWeaponModel(weaponId, root.transform, Vector3.zero, out _);
                    if (wepModel != null)
                        wepModel.transform.localScale = Vector3.one * 1.15f;
                    break;

                case LootCategory.Ammo:
                    displayLabel = "Đạn tổng hợp (+" + amount + ")";
                    mainColor = new Color(0.75f, 0.62f, 0.18f);
                    MaterialPool.CreatePrimitive(PrimitiveType.Cube, root.transform, "AmmoBox", Vector3.zero, new Vector3(0.36f, 0.24f, 0.26f), mainColor);
                    break;

                case LootCategory.Medkit:
                    displayLabel = "Hộp cứu thương Medkit (+55 HP)";
                    mainColor = new Color(0.92f, 0.95f, 0.92f);
                    accentColor = new Color(0.88f, 0.18f, 0.18f);
                    MaterialPool.CreatePrimitive(PrimitiveType.Cube, root.transform, "MedCase", Vector3.zero, new Vector3(0.42f, 0.26f, 0.32f), mainColor);
                    MaterialPool.CreatePrimitive(PrimitiveType.Cube, root.transform, "CrossH", new Vector3(0f, 0.14f, 0f), new Vector3(0.22f, 0.04f, 0.08f), accentColor);
                    MaterialPool.CreatePrimitive(PrimitiveType.Cube, root.transform, "CrossV", new Vector3(0f, 0.14f, 0f), new Vector3(0.08f, 0.04f, 0.22f), accentColor);
                    break;

                case LootCategory.Bandage:
                    displayLabel = "Băng gạc Bandage (+25 HP)";
                    mainColor = new Color(0.85f, 0.88f, 0.82f);
                    MaterialPool.CreatePrimitive(PrimitiveType.Cylinder, root.transform, "Roll", Vector3.zero, new Vector3(0.26f, 0.14f, 0.26f), mainColor);
                    break;

                case LootCategory.EnergyDrink:
                    displayLabel = "Nước tăng lực RedZone (+Boost & Tốc chạy)";
                    mainColor = new Color(0.15f, 0.52f, 0.95f);
                    MaterialPool.CreatePrimitive(PrimitiveType.Cylinder, root.transform, "Can", Vector3.zero, new Vector3(0.20f, 0.22f, 0.20f), mainColor);
                    break;

                case LootCategory.ArmorVest:
                    displayLabel = "Giáp chống đạn Cấp " + gearTier;
                    mainColor = GetTierColor(gearTier);
                    MaterialPool.CreatePrimitive(PrimitiveType.Cube, root.transform, "Vest", Vector3.zero, new Vector3(0.46f, 0.48f, 0.24f), mainColor);
                    break;

                case LootCategory.Helmet:
                    displayLabel = "Mũ bảo hiểm Cấp " + gearTier;
                    mainColor = GetTierColor(gearTier);
                    MaterialPool.CreatePrimitive(PrimitiveType.Sphere, root.transform, "Helmet", Vector3.zero, new Vector3(0.42f, 0.38f, 0.44f), mainColor);
                    break;

                case LootCategory.FragGrenade:
                    displayLabel = "Lựu đạn nổ Frag Grenade (+" + amount + ")";
                    mainColor = new Color(0.24f, 0.38f, 0.20f);
                    MaterialPool.CreatePrimitive(PrimitiveType.Sphere, root.transform, "Grenade", Vector3.zero, new Vector3(0.28f, 0.32f, 0.28f), mainColor);
                    break;
            }

            MaterialPool.CreatePrimitive(
                PrimitiveType.Cylinder,
                transform,
                "GlowRing",
                new Vector3(0f, 0.03f, 0f),
                new Vector3(0.95f, 0.015f, 0.95f),
                accentColor);
        }

        public static Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 3: return new Color(0.92f, 0.72f, 0.16f);
                case 2: return new Color(0.24f, 0.62f, 0.92f);
                default: return new Color(0.46f, 0.52f, 0.46f);
            }
        }

        public static void ApplyColor(Renderer rend, Color color)
        {
            MaterialPool.ApplyLitColor(rend, color);
        }
    }
}
