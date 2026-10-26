using System;
using System.Collections.Generic;
using Survival.Core;
using Survival.Items;
using UnityEngine;

namespace Survival.Combat
{
    public class CombatantHealth : MonoBehaviour
    {
        public static readonly List<CombatantHealth> ActiveCombatants = new List<CombatantHealth>();

        public string combatantName = "Survivor";
        public bool isLocalPlayer = false;
        public int maxHealth = 100;
        public float currentHealth = 100f;
        public float currentBoost = 0f;

        public int armorLevel = 0;
        public float armorDurability = 0f;
        public float maxArmorDurability = 0f;

        public int helmetLevel = 0;
        public float helmetDurability = 0f;
        public float maxHelmetDurability = 0f;

        public int kills = 0;
        public int totalDamageDealt = 0;
        public bool isDead = false;

        public float lastHitReceivedTime = -99f;
        public Vector3 lastAttackerPosition;

        public event Action<CombatantHealth, CombatantHealth, string, bool> OnDied;
        public event Action<int, bool, bool> OnDealtDamage;

        private float boostTickTimer = 0f;

        public bool IsAlive => !isDead && currentHealth > 0f;

        private void OnEnable()
        {
            if (!ActiveCombatants.Contains(this))
                ActiveCombatants.Add(this);
        }

        private void OnDisable()
        {
            ActiveCombatants.Remove(this);
        }

        private void OnDestroy()
        {
            ActiveCombatants.Remove(this);
        }

        public void InitializeCombatant(string nameLabel, bool localPlayer)
        {
            if (!ActiveCombatants.Contains(this))
                ActiveCombatants.Add(this);

            combatantName = string.IsNullOrWhiteSpace(nameLabel) ? "Survivor" : nameLabel;
            isLocalPlayer = localPlayer;
            maxHealth = 100;
            currentHealth = maxHealth;
            currentBoost = 0f;
            armorLevel = 0;
            armorDurability = 0f;
            maxArmorDurability = 0f;
            helmetLevel = 0;
            helmetDurability = 0f;
            maxHelmetDurability = 0f;
            kills = 0;
            totalDamageDealt = 0;
            isDead = false;
        }

        private void Update()
        {
            if (isDead) return;

            if (currentBoost > 0f)
            {
                currentBoost = Mathf.Max(0f, currentBoost - 2.2f * Time.deltaTime);
                boostTickTimer += Time.deltaTime;
                if (boostTickTimer >= 1.0f)
                {
                    boostTickTimer = 0f;
                    float regen = currentBoost > 50f ? 3.5f : 1.8f;
                    currentHealth = Mathf.Min(maxHealth, currentHealth + regen);
                }
            }
        }

        public float GetSpeedBoostMultiplier()
        {
            if (currentBoost > 60f) return 1.16f;
            if (currentBoost > 15f) return 1.08f;
            return 1.0f;
        }

        public bool CanUpgradeArmorVest(int candidateTier)
        {
            candidateTier = Mathf.Clamp(candidateTier, 1, 3);
            if (armorLevel < candidateTier) return true;
            if (armorLevel == candidateTier && armorDurability < maxArmorDurability * 0.95f) return true;
            return false;
        }

        public bool CanUpgradeHelmet(int candidateTier)
        {
            candidateTier = Mathf.Clamp(candidateTier, 1, 3);
            if (helmetLevel < candidateTier) return true;
            if (helmetLevel == candidateTier && helmetDurability < maxHelmetDurability * 0.95f) return true;
            return false;
        }

        public void EquipArmorVest(int tier)
        {
            tier = Mathf.Clamp(tier, 1, 3);
            armorLevel = tier;
            maxArmorDurability = tier == 1 ? 120f : (tier == 2 ? 180f : 250f);
            armorDurability = maxArmorDurability;
        }

        public void EquipHelmet(int tier)
        {
            tier = Mathf.Clamp(tier, 1, 3);
            helmetLevel = tier;
            maxHelmetDurability = tier == 1 ? 80f : (tier == 2 ? 130f : 200f);
            helmetDurability = maxHelmetDurability;
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        }

        public void AddEnergyBoost(float amount)
        {
            if (isDead) return;
            currentBoost = Mathf.Clamp(currentBoost + amount, 0f, 100f);
        }

        public int TakeDamage(float rawDamage, CombatantHealth attacker, string weaponLabel, bool isHeadshot = false, bool isZoneDamage = false)
        {
            if (isDead || rawDamage <= 0f)
                return 0;

            float finalDamage = rawDamage;
            bool gearBroken = false;

            if (!isZoneDamage)
            {
                if (isHeadshot)
                {
                    finalDamage *= 1.9f;
                    if (helmetLevel > 0 && helmetDurability > 0f)
                    {
                        float reduction = helmetLevel == 1 ? 0.28f : (helmetLevel == 2 ? 0.42f : 0.55f);
                        float absorbed = finalDamage * reduction;
                        finalDamage -= absorbed;
                        helmetDurability = Mathf.Max(0f, helmetDurability - absorbed * 1.2f);
                        if (helmetDurability <= 0f)
                        {
                            helmetLevel = 0;
                            gearBroken = true;
                        }
                    }
                }
                else
                {
                    if (armorLevel > 0 && armorDurability > 0f)
                    {
                        float reduction = armorLevel == 1 ? 0.25f : (armorLevel == 2 ? 0.38f : 0.52f);
                        float absorbed = finalDamage * reduction;
                        finalDamage -= absorbed;
                        armorDurability = Mathf.Max(0f, armorDurability - absorbed);
                        if (armorDurability <= 0f)
                        {
                            armorLevel = 0;
                            gearBroken = true;
                        }
                    }
                }
            }

            if (gearBroken && isLocalPlayer)
            {
                GameEvents.RaiseLocalEquipmentChanged();
            }

            int appliedDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage));
            currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
            lastHitReceivedTime = Time.time;

            if (attacker != null)
            {
                lastAttackerPosition = attacker.transform.position;
                if (attacker != this)
                {
                    attacker.totalDamageDealt += appliedDamage;
                }
            }

            bool killedNow = currentHealth <= 0f;
            if (attacker != null && attacker != this)
            {
                attacker.OnDealtDamage?.Invoke(appliedDamage, isHeadshot, killedNow);
            }

            GameEvents.RaiseCombatantDamaged(this, attacker, appliedDamage, isHeadshot, killedNow);

            if (killedNow)
            {
                Die(attacker, weaponLabel, isHeadshot);
            }

            return appliedDamage;
        }

        private void Die(CombatantHealth killer, string weaponLabel, bool isHeadshot)
        {
            if (isDead) return;
            isDead = true;
            currentHealth = 0f;

            if (killer != null && killer != this)
            {
                killer.kills += 1;
            }

            string finalWeapon = string.IsNullOrEmpty(weaponLabel) ? "Blue Zone" : weaponLabel;
            SpawnDeathLootBox();
            OnDied?.Invoke(this, killer, finalWeapon, isHeadshot);
            GameEvents.RaiseCombatantDied(this, killer, finalWeapon, isHeadshot);
        }

        private void SpawnDeathLootBox()
        {
            Transform parentContainer = transform.parent;
            Vector3 dropCenter = transform.position + Vector3.up * 0.1f;
            LootItem.SpawnLoot(dropCenter + new Vector3(0.55f, 0f, 0.2f), LootCategory.Ammo, WeaponId.M416, 45, 1, parentContainer);
            LootItem.SpawnLoot(dropCenter + new Vector3(-0.55f, 0f, -0.2f), LootCategory.Medkit, WeaponId.M416, 1, 1, parentContainer);

            if (armorLevel >= 2 && armorDurability > 30f)
            {
                LootItem.SpawnLoot(dropCenter + new Vector3(0f, 0f, 0.6f), LootCategory.ArmorVest, WeaponId.M416, 1, armorLevel, parentContainer);
            }
        }
    }
}
