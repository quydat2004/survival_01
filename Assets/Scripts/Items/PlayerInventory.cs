using System;
using Survival.Combat;
using Survival.Core;
using UnityEngine;

namespace Survival.Items
{
    [Serializable]
    public class PlayerInventory
    {
        public WeaponInstance[] weaponSlots = new WeaponInstance[3];
        public int activeSlotIndex = 0;
        public int reserveAmmo = 105;
        public int medkitCount = 1;
        public int bandageCount = 2;
        public int energyDrinkCount = 1;
        public int grenadeCount = 1;

        public bool isReloading = false;
        public float reloadProgress01 = 0f;
        private float reloadTimer = 0f;

        public WeaponInstance ActiveWeapon =>
            (activeSlotIndex >= 0 && activeSlotIndex < weaponSlots.Length) ? weaponSlots[activeSlotIndex] : null;

        public void InitializeDefaultLoadout()
        {
            weaponSlots[0] = new WeaponInstance(WeaponDatabase.GetSpec(WeaponId.UMP45));
            weaponSlots[1] = null;
            weaponSlots[2] = new WeaponInstance(WeaponDatabase.GetSpec(WeaponId.P1911));
            activeSlotIndex = 0;

            reserveAmmo = 105;
            medkitCount = 1;
            bandageCount = 2;
            energyDrinkCount = 1;
            grenadeCount = 1;
            CancelReload();
        }

        public bool TrySwitchSlot(int slotIndex, out string notification)
        {
            notification = string.Empty;
            if (slotIndex < 0 || slotIndex >= weaponSlots.Length || weaponSlots[slotIndex] == null)
                return false;

            if (activeSlotIndex == slotIndex)
                return false;

            CancelReload();
            activeSlotIndex = slotIndex;
            notification = "Trang bị: " + weaponSlots[slotIndex].spec.displayName;
            GameEvents.RaiseLocalEquipmentChanged();
            return true;
        }

        public bool TryCycleSlot(int direction, out string notification)
        {
            notification = string.Empty;
            int step = direction >= 0 ? 1 : -1;
            for (int i = 1; i <= weaponSlots.Length; i++)
            {
                int candidate = (activeSlotIndex + step * i + weaponSlots.Length) % weaponSlots.Length;
                if (weaponSlots[candidate] != null)
                    return TrySwitchSlot(candidate, out notification);
            }
            return false;
        }

        public bool StartReload(out string notification)
        {
            notification = string.Empty;
            WeaponInstance w = ActiveWeapon;
            if (w == null || isReloading)
                return false;

            if (w.currentAmmo >= w.spec.magazineSize)
                return false;

            if (reserveAmmo <= 0)
            {
                notification = "Hết đạn dự trữ! Hãy nhặt thêm hộp đạn!";
                return false;
            }

            isReloading = true;
            reloadTimer = Mathf.Max(0.4f, w.spec.reloadTime);
            reloadProgress01 = 0f;
            return true;
        }

        public void CancelReload()
        {
            isReloading = false;
            reloadTimer = 0f;
            reloadProgress01 = 0f;
        }

        public void TickReload(float deltaTime)
        {
            if (!isReloading) return;

            WeaponInstance w = ActiveWeapon;
            if (w == null)
            {
                CancelReload();
                return;
            }

            reloadTimer -= deltaTime;
            float totalTime = Mathf.Max(0.4f, w.spec.reloadTime);
            reloadProgress01 = 1f - Mathf.Clamp01(reloadTimer / totalTime);

            if (reloadTimer <= 0f)
            {
                int needed = w.spec.magazineSize - w.currentAmmo;
                int taken = Mathf.Min(needed, reserveAmmo);
                w.currentAmmo += taken;
                reserveAmmo -= taken;
                CancelReload();
            }
        }

        public bool TryPickupLoot(LootItem item, CombatantHealth health, Transform dropParent, out string notification)
        {
            notification = string.Empty;
            if (item == null || item.IsConsumed || health == null)
                return false;

            switch (item.category)
            {
                case LootCategory.Weapon:
                    WeaponSpec spec = WeaponDatabase.GetSpec(item.weaponId);
                    int targetSlot;
                    if (spec.isSidearm)
                    {
                        targetSlot = 2;
                    }
                    else if (weaponSlots[0] == null)
                    {
                        targetSlot = 0;
                    }
                    else if (weaponSlots[1] == null)
                    {
                        targetSlot = 1;
                    }
                    else
                    {
                        targetSlot = activeSlotIndex == 2 ? 0 : activeSlotIndex;
                    }

                    // Drop previous weapon in that slot on the ground so it isn't lost
                    WeaponInstance previousWeapon = weaponSlots[targetSlot];
                    if (previousWeapon != null && previousWeapon.spec != null)
                    {
                        reserveAmmo += Mathf.Max(0, previousWeapon.currentAmmo);
                        Vector3 dropPos = health.transform.position + health.transform.forward * 0.8f + Vector3.up * 0.05f;
                        LootItem.SpawnLoot(dropPos, LootCategory.Weapon, previousWeapon.spec.id, 0, 1, dropParent);
                    }

                    weaponSlots[targetSlot] = new WeaponInstance(spec);
                    activeSlotIndex = targetSlot;
                    reserveAmmo += Mathf.Max(0, item.amount);
                    CancelReload();
                    GameEvents.RaiseLocalEquipmentChanged();
                    notification = "Đã nhặt vũ khí: " + spec.displayName + (item.amount > 0 ? " (+" + item.amount + " đạn)" : "");
                    break;

                case LootCategory.Ammo:
                    reserveAmmo += Mathf.Max(1, item.amount);
                    notification = "Đã nhặt +" + item.amount + " đạn dự trữ";
                    break;

                case LootCategory.Medkit:
                    medkitCount += Mathf.Max(1, item.amount);
                    notification = "Đã nhặt Hộp cứu thương Medkit (Tổng: " + medkitCount + ")";
                    break;

                case LootCategory.Bandage:
                    bandageCount += Mathf.Max(1, item.amount);
                    notification = "Đã nhặt Băng gạc Bandage (Tổng: " + bandageCount + ")";
                    break;

                case LootCategory.EnergyDrink:
                    energyDrinkCount += Mathf.Max(1, item.amount);
                    notification = "Đã nhặt Nước tăng lực (Tổng: " + energyDrinkCount + ")";
                    break;

                case LootCategory.ArmorVest:
                    if (!health.CanUpgradeArmorVest(item.gearTier))
                    {
                        notification = "Đang mặc Giáp cấp cao hơn hoặc còn nguyên vẹn!";
                        return false;
                    }
                    health.EquipArmorVest(item.gearTier);
                    GameEvents.RaiseLocalEquipmentChanged();
                    notification = "Đã mặc Giáp Chống Đạn Cấp " + item.gearTier;
                    break;

                case LootCategory.Helmet:
                    if (!health.CanUpgradeHelmet(item.gearTier))
                    {
                        notification = "Đang đội Mũ cấp cao hơn hoặc còn nguyên vẹn!";
                        return false;
                    }
                    health.EquipHelmet(item.gearTier);
                    GameEvents.RaiseLocalEquipmentChanged();
                    notification = "Đã đội Mũ Bảo Hiểm Cấp " + item.gearTier;
                    break;

                case LootCategory.FragGrenade:
                    grenadeCount += Mathf.Max(1, item.amount);
                    notification = "Đã nhặt Lựu đạn nổ Frag Grenade (Tổng: " + grenadeCount + ")";
                    break;
            }

            item.ConsumeAndDestroy();
            return true;
        }
    }
}
