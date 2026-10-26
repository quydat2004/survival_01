using System;
using UnityEngine;

namespace Survival.Items
{
    public enum WeaponId
    {
        P1911 = 0,
        UMP45 = 1,
        M416 = 2,
        AKM = 3,
        S1897 = 4,
        Kar98k = 5,
        AWM = 6
    }

    public enum LootCategory
    {
        Weapon,
        Ammo,
        Medkit,
        Bandage,
        EnergyDrink,
        ArmorVest,
        Helmet,
        FragGrenade
    }

    [Serializable]
    public class WeaponSpec
    {
        public WeaponId id;
        public string displayName;
        public string shortCategory;
        public int damagePerPellet;
        public int pelletsPerShot = 1;
        public float fireInterval = 0.1f;
        public int magazineSize = 30;
        public float reloadTime = 2.0f;
        public float maxRange = 160f;
        public float hipSpreadDegrees = 1.5f;
        public float adsSpreadDegrees = 0.35f;
        public float adsFov = 45f;
        public bool isAutomatic = true;
        public bool hasSniperScope = false;
        public bool isSidearm = false;
        public Color bodyColor = new Color(0.22f, 0.24f, 0.27f);
    }

    [Serializable]
    public class WeaponInstance
    {
        public WeaponSpec spec;
        public int currentAmmo;

        public WeaponInstance(WeaponSpec weaponSpec)
        {
            spec = weaponSpec;
            currentAmmo = weaponSpec.magazineSize;
        }
    }

    public static class WeaponDatabase
    {
        private static readonly WeaponSpec[] Specs =
        {
            new WeaponSpec
            {
                id = WeaponId.P1911,
                displayName = "P1911 Pistol",
                shortCategory = "PISTOL",
                damagePerPellet = 23,
                pelletsPerShot = 1,
                fireInterval = 0.22f,
                magazineSize = 12,
                reloadTime = 1.4f,
                maxRange = 85f,
                hipSpreadDegrees = 1.4f,
                adsSpreadDegrees = 0.4f,
                adsFov = 50f,
                isAutomatic = false,
                hasSniperScope = false,
                isSidearm = true,
                bodyColor = new Color(0.32f, 0.34f, 0.36f)
            },
            new WeaponSpec
            {
                id = WeaponId.UMP45,
                displayName = "UMP45 SMG",
                shortCategory = "SMG",
                damagePerPellet = 22,
                pelletsPerShot = 1,
                fireInterval = 0.09f,
                magazineSize = 35,
                reloadTime = 1.85f,
                maxRange = 110f,
                hipSpreadDegrees = 1.8f,
                adsSpreadDegrees = 0.5f,
                adsFov = 46f,
                isAutomatic = true,
                hasSniperScope = false,
                isSidearm = false,
                bodyColor = new Color(0.20f, 0.25f, 0.30f)
            },
            new WeaponSpec
            {
                id = WeaponId.M416,
                displayName = "M416 Tactical",
                shortCategory = "AR",
                damagePerPellet = 29,
                pelletsPerShot = 1,
                fireInterval = 0.095f,
                magazineSize = 30,
                reloadTime = 2.1f,
                maxRange = 185f,
                hipSpreadDegrees = 1.25f,
                adsSpreadDegrees = 0.25f,
                adsFov = 38f,
                isAutomatic = true,
                hasSniperScope = false,
                isSidearm = false,
                bodyColor = new Color(0.18f, 0.20f, 0.22f)
            },
            new WeaponSpec
            {
                id = WeaponId.AKM,
                displayName = "AKM 7.62mm",
                shortCategory = "AR",
                damagePerPellet = 36,
                pelletsPerShot = 1,
                fireInterval = 0.12f,
                magazineSize = 30,
                reloadTime = 2.3f,
                maxRange = 170f,
                hipSpreadDegrees = 1.65f,
                adsSpreadDegrees = 0.38f,
                adsFov = 40f,
                isAutomatic = true,
                hasSniperScope = false,
                isSidearm = false,
                bodyColor = new Color(0.42f, 0.27f, 0.15f)
            },
            new WeaponSpec
            {
                id = WeaponId.S1897,
                displayName = "S1897 Shotgun",
                shortCategory = "SHOTGUN",
                damagePerPellet = 16,
                pelletsPerShot = 8,
                fireInterval = 0.78f,
                magazineSize = 5,
                reloadTime = 2.5f,
                maxRange = 45f,
                hipSpreadDegrees = 4.8f,
                adsSpreadDegrees = 3.2f,
                adsFov = 48f,
                isAutomatic = false,
                hasSniperScope = false,
                isSidearm = false,
                bodyColor = new Color(0.38f, 0.24f, 0.16f)
            },
            new WeaponSpec
            {
                id = WeaponId.Kar98k,
                displayName = "Kar98k (4x Scope)",
                shortCategory = "SNIPER",
                damagePerPellet = 78,
                pelletsPerShot = 1,
                fireInterval = 1.15f,
                magazineSize = 5,
                reloadTime = 2.7f,
                maxRange = 290f,
                hipSpreadDegrees = 2.6f,
                adsSpreadDegrees = 0.05f,
                adsFov = 18f,
                isAutomatic = false,
                hasSniperScope = true,
                isSidearm = false,
                bodyColor = new Color(0.46f, 0.30f, 0.16f)
            },
            new WeaponSpec
            {
                id = WeaponId.AWM,
                displayName = "AWM Magnum (8x Scope)",
                shortCategory = "AIRDROP SNIPER",
                damagePerPellet = 115,
                pelletsPerShot = 1,
                fireInterval = 1.30f,
                magazineSize = 5,
                reloadTime = 2.9f,
                maxRange = 360f,
                hipSpreadDegrees = 2.8f,
                adsSpreadDegrees = 0.02f,
                adsFov = 11f,
                isAutomatic = false,
                hasSniperScope = true,
                isSidearm = false,
                bodyColor = new Color(0.24f, 0.35f, 0.22f)
            }
        };

        public static WeaponSpec GetSpec(WeaponId id)
        {
            int idx = (int)id;
            if (idx >= 0 && idx < Specs.Length)
                return Specs[idx];
            return Specs[0];
        }
    }
}
