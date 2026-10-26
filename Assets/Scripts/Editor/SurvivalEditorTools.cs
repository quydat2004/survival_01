#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Survival.Backend;
using Survival.Combat;
using Survival.Core;
using Survival.Items;
using Survival.Player;
using Survival.UI;
using Survival.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Survival.Editor
{
    public static class SurvivalEditorTools
    {
        [MenuItem("Survival 01/Setup Current Scene (Auto-Add Game Systems)", false, 10)]
        public static void SetupCurrentScene()
        {
            EnsureCharacterAnimationsConfigured();

            GameObject existing = GameObject.Find("Survival01_Systems");
            if (existing == null)
            {
                existing = new GameObject("Survival01_Systems");
            }

            if (existing.GetComponent<GameBootstrapper>() == null)
                existing.AddComponent<GameBootstrapper>();

            EnsureSceneInBuildSettings("Assets/Scenes/SampleScene.unity");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Survival 01] Scene setup complete! Press Play to launch the game.");
        }

        [MenuItem("Survival 01/Run Automated Smoke Test (Verify All Systems)", false, 15)]
        public static void RunAutomatedSmokeTest()
        {
            Debug.Log("[Survival 01 SmokeTest] Starting full architectural & 3D asset verification...");

            EnsureCharacterAnimationsConfigured();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            // 1. Verify all 3D Prefabs in Resources/Models load via ModelCatalog
            foreach (string charName in ModelCatalog.CharacterNames)
            {
                GameObject charPrefab = ModelCatalog.LoadModelPrefab("Models/Characters/" + charName);
                AssertNotNull(charPrefab, "Character FBX " + charName);
            }

            foreach (WeaponId wid in Enum.GetValues(typeof(WeaponId)))
            {
                GameObject wepPrefab = ModelCatalog.LoadModelPrefab(ModelCatalog.GetWeaponModelPath(wid));
                AssertNotNull(wepPrefab, "Weapon FBX for " + wid);
            }

            AssertNotNull(ModelCatalog.LoadModelPrefab("Models/Buildings/building-a"), "Building FBX building-a");
            AssertNotNull(ModelCatalog.LoadModelPrefab("Models/Buildings/shipping-container-a"), "Container FBX shipping-container-a");
            AssertNotNull(ModelCatalog.LoadModelPrefab("Models/Nature/Pine_1"), "Tree FBX Pine_1");
            AssertNotNull(ModelCatalog.LoadModelPrefab("Models/Nature/Rock_Medium_1"), "Rock FBX Rock_Medium_1");

            // 2. Bootstrap all systems
            GameBootstrapper.EnsureGameSystems();

            AssertNotNull(SupabaseManager.Instance, "SupabaseManager.Instance");
            AssertNotNull(BattleRoyaleMapGenerator.Instance, "BattleRoyaleMapGenerator.Instance");
            AssertNotNull(BlueZoneController.Instance, "BlueZoneController.Instance");
            AssertNotNull(AirDropManager.Instance, "AirDropManager.Instance");
            AssertNotNull(MatchManager.Instance, "MatchManager.Instance");
            AssertNotNull(SurvivalUIManager.Instance, "SurvivalUIManager.Instance");

            // 3. Start a 10-player Battle Royale match
            MatchManager.Instance.StartBattleRoyaleMatch(10, "smoke_test_10p");
            AssertTrue(MatchManager.Instance.CurrentState == GameFlowState.InMatch, "Match state should be InMatch");
            AssertTrue(CombatantHealth.ActiveCombatants.Count == 10, "Expected 10 active combatants, got " + CombatantHealth.ActiveCombatants.Count);
            AssertTrue(LootItem.ActiveLoot.Count > 20, "Expected > 20 world loot items, got " + LootItem.ActiveLoot.Count);

            SurvivalPlayerController player = SurvivalPlayerController.LocalInstance;
            AssertNotNull(player, "SurvivalPlayerController.LocalInstance");
            AssertNotNull(player.ActiveWeapon, "Player ActiveWeapon");
            AssertTrue(player.ActiveWeapon.spec.id == WeaponId.UMP45, "Starter primary weapon should be UMP45");

            // Verify 3D Character mesh & Animation on Local Player
            SkinnedMeshRenderer playerSmr = player.GetComponentInChildren<SkinnedMeshRenderer>(true);
            AssertNotNull(playerSmr, "Player 3D SkinnedMeshRenderer");
            Animation playerAnim = player.GetComponentInChildren<Animation>(true);
            AssertNotNull(playerAnim, "Player Legacy Animation component");

            // 4. Test Inventory weapon pickup & 3D weapon swap
            int lootBefore = LootItem.ActiveLoot.Count;
            LootItem spawnedKar = LootItem.SpawnLoot(player.transform.position, LootCategory.Weapon, WeaponId.Kar98k, 15, 2);
            bool picked1 = player.inventory.TryPickupLoot(spawnedKar, player.Health, null, out _);
            AssertTrue(picked1, "Should pick up Kar98k into empty slot 1");
            AssertTrue(player.weaponSlots[1] != null && player.weaponSlots[1].spec.id == WeaponId.Kar98k, "Slot 1 should hold Kar98k");

            // Pick up M416 while slot 1 is active -> should drop Kar98k back to world
            LootItem spawnedM4 = LootItem.SpawnLoot(player.transform.position, LootCategory.Weapon, WeaponId.M416, 30, 2);
            bool picked2 = player.inventory.TryPickupLoot(spawnedM4, player.Health, null, out _);
            AssertTrue(picked2, "Should swap Kar98k for M416");
            AssertTrue(player.weaponSlots[1].spec.id == WeaponId.M416, "Slot 1 should now hold M416");
            AssertTrue(LootItem.ActiveLoot.Count == lootBefore + 1, "Replaced Kar98k should have been dropped into ActiveLoot");

            // 5. Test Armor upgrade protection
            LootItem vestLv3 = LootItem.SpawnLoot(player.transform.position, LootCategory.ArmorVest, WeaponId.M416, 1, 3);
            AssertTrue(player.inventory.TryPickupLoot(vestLv3, player.Health, null, out _), "Should equip Lv3 Vest");
            AssertTrue(player.Health.armorLevel == 3, "Player armorLevel should be 3");

            LootItem vestLv1 = LootItem.SpawnLoot(player.transform.position, LootCategory.ArmorVest, WeaponId.M416, 1, 1);
            AssertTrue(!player.inventory.TryPickupLoot(vestLv1, player.Health, null, out _), "Should reject downgrading undamaged Lv3 Vest to Lv1");
            vestLv1.ConsumeAndDestroy();

            // 6. Test WeaponBallistics & CombatantHealth damage
            CombatantHealth targetBot = null;
            for (int i = 0; i < CombatantHealth.ActiveCombatants.Count; i++)
            {
                if (!CombatantHealth.ActiveCombatants[i].isLocalPlayer)
                {
                    targetBot = CombatantHealth.ActiveCombatants[i];
                    break;
                }
            }
            AssertNotNull(targetBot, "Target Bot");

            targetBot.transform.position = new Vector3(0f, 50f, 10f);
            player.transform.position = new Vector3(0f, 50f, 0f);
            Physics.SyncTransforms();

            Vector3 aimFrom = new Vector3(0f, 50.95f, 0f);
            float hpBefore = targetBot.currentHealth;
            WeaponBallistics.FireWeapon(
                player.Health,
                WeaponDatabase.GetSpec(WeaponId.AWM),
                aimFrom,
                Vector3.forward,
                aimFrom + Vector3.forward * 0.5f,
                0f,
                Color.yellow,
                1f,
                1f);
            AssertTrue(targetBot.currentHealth < hpBefore, "Bot should take ballistic damage from AWM shot");

            // 7. Test Tree/Rock Colliders & Enterable House Doorway/Interior
            GameObject testTree = ModelCatalog.SpawnNatureModel(null, "Pine_1", new Vector3(0f, 0f, 30f), 0f, 1.2f, true, false, true);
            AssertNotNull(testTree, "Spawned Pine_1");
            CapsuleCollider treeCol = testTree.GetComponent<CapsuleCollider>();
            AssertNotNull(treeCol, "Tree should have CapsuleCollider to block player");
            AssertTrue(!treeCol.isTrigger, "Tree collider must be solid");

            GameObject testRock = ModelCatalog.SpawnNatureModel(null, "Rock_Medium_1", new Vector3(0f, 0f, 36f), 0f, 1.2f, false, true, true);
            AssertNotNull(testRock, "Spawned Rock");
            SphereCollider rockCol = testRock.GetComponent<SphereCollider>();
            AssertNotNull(rockCol, "Rock should have SphereCollider");
            AssertTrue(!rockCol.isTrigger, "Rock collider must be solid");

            GameObject testHouse = EnterableStructureBuilder.BuildTwoStoryTacticalHouse(null, new Vector3(0f, 0f, 50f), 0f, Color.gray, Color.black, true);
            AssertNotNull(testHouse, "Spawned Enterable 2-Story Tactical House");
            Transform floor1 = testHouse.transform.Find("Floor1");
            AssertNotNull(floor1, "House Floor1 exists");
            Transform stairs = testHouse.transform.Find("StairRamp_F1_F2");
            AssertNotNull(stairs, "House Stairs to 2nd Floor exists");

            MaterialPool.SafeDestroy(testTree);
            MaterialPool.SafeDestroy(testRock);
            MaterialPool.SafeDestroy(testHouse);

            // 8. Test AirDrop & BlueZone cleanup on GoToLobby
            AirDropManager.Instance.SpawnSupplyCrate();
            AssertTrue(AirDropManager.ActiveAirDropLocations.Count == 1, "AirDrop should be tracked");

            MatchManager.Instance.GoToLobby();
            AssertTrue(!BlueZoneController.Instance.IsZoneActive, "BlueZone should be inactive in Lobby");
            AssertTrue(AirDropManager.ActiveAirDropLocations.Count == 0, "AirDrops should be cleared in Lobby");

            // Clean up runtime test objects before saving scene
            GameObject island = GameObject.Find("BattleRoyaleIsland");
            if (island != null) MaterialPool.SafeDestroy(island);
            GameObject systems = GameObject.Find("Survival01_Systems");
            if (systems != null) MaterialPool.SafeDestroy(systems);

            SetupCurrentScene();
            Debug.Log("[Survival 01 SmokeTest] ALL AUTOMATED 3D ASSET & GAMEPLAY TESTS PASSED SUCCESSFULLY!");
        }

        [MenuItem("Survival 01/Clear Local Login Session (Test Login Screen)", false, 20)]
        public static void ClearSavedSession()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Survival 01] Cleared saved session in PlayerPrefs.");
        }

        [MenuItem("Survival 01/Open Supabase Dashboard (ganxpdiwtwtwpvdgodbi)", false, 30)]
        public static void OpenSupabaseDashboard()
        {
            Application.OpenURL("https://supabase.com/dashboard/project/ganxpdiwtwtwpvdgodbi");
        }

        private static void EnsureCharacterAnimationsConfigured()
        {
            foreach (string cName in ModelCatalog.CharacterNames)
            {
                string path = "Assets/Resources/Models/Characters/" + cName + ".fbx";
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null && importer.animationType != ModelImporterAnimationType.Legacy)
                {
                    importer.animationType = ModelImporterAnimationType.Legacy;
                    importer.SaveAndReimport();
                }
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new Exception("[SmokeTest Failed] " + message);
        }

        private static void AssertNotNull(object obj, string name)
        {
            if (obj == null)
                throw new Exception("[SmokeTest Failed] Expected non-null: " + name);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene s in scenes)
            {
                if (s.path == scenePath)
                    return;
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
