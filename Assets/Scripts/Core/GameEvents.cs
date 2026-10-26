using System;
using Survival.Combat;
using UnityEngine;

namespace Survival.Core
{
    public static class GameEvents
    {
        public static event Action<GameFlowState> OnGameStateChanged;
        public static event Action<CombatantHealth, CombatantHealth, int, bool, bool> OnCombatantDamaged;
        public static event Action<CombatantHealth, CombatantHealth, string, bool> OnCombatantDied;
        public static event Action<string> OnPlayerNotification;
        public static event Action<Vector3> OnAirDropSpawned;
        public static event Action<int, bool> OnZonePhaseChanged;
        public static event Action OnLocalEquipmentChanged;

        public static void RaiseGameStateChanged(GameFlowState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        public static void RaiseCombatantDamaged(CombatantHealth victim, CombatantHealth attacker, int damage, bool isHeadshot, bool isKilled)
        {
            OnCombatantDamaged?.Invoke(victim, attacker, damage, isHeadshot, isKilled);
        }

        public static void RaiseCombatantDied(CombatantHealth victim, CombatantHealth killer, string weaponName, bool isHeadshot)
        {
            OnCombatantDied?.Invoke(victim, killer, weaponName, isHeadshot);
        }

        public static void RaisePlayerNotification(string message)
        {
            if (!string.IsNullOrEmpty(message))
                OnPlayerNotification?.Invoke(message);
        }

        public static void RaiseAirDropSpawned(Vector3 landPosition)
        {
            OnAirDropSpawned?.Invoke(landPosition);
        }

        public static void RaiseZonePhaseChanged(int phaseIndex, bool isShrinking)
        {
            OnZonePhaseChanged?.Invoke(phaseIndex, isShrinking);
        }

        public static void RaiseLocalEquipmentChanged()
        {
            OnLocalEquipmentChanged?.Invoke();
        }
    }
}
