using System;
using System.Collections.Generic;
using Survival.Backend;
using Survival.Combat;
using Survival.Items;
using Survival.Player;
using Survival.World;
using UnityEngine;

namespace Survival.Core
{
    public enum GameFlowState
    {
        AuthScreen,
        MainMenuLobby,
        InMatch,
        PostMatchSummary
    }

    [Serializable]
    public struct KillfeedItem
    {
        public string killerName;
        public string victimName;
        public string weaponName;
        public bool isHeadshot;
        public float timestamp;
        public bool involvesLocalPlayer;
    }

    public class MatchManager : MonoBehaviour
    {
        private static MatchManager instance;
        public static MatchManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<MatchManager>();
                return instance;
            }
            private set => instance = value;
        }

        public GameFlowState CurrentState { get; private set; } = GameFlowState.AuthScreen;
        public int TotalParticipants { get; private set; } = 10;
        public int AliveCount { get; private set; } = 10;
        public float MatchElapsedSeconds { get; private set; } = 0f;
        public string SelectedGameMode { get; set; } = "online_10p";
        public string ActiveRoomCode { get; set; } = string.Empty;

        public bool LocalPlayerWon { get; private set; }
        public int FinalPlacement { get; private set; } = 10;
        public int FinalKills { get; private set; }
        public int FinalDamageDealt { get; private set; }
        public int FinalSurvivedSeconds { get; private set; }
        public string CloudSyncStatus { get; private set; } = string.Empty;

        public readonly List<KillfeedItem> Killfeed = new List<KillfeedItem>();

        private Transform activeMatchActorsRoot;
        private Transform activeIslandRoot;
        private GameObject lobbyStageRoot;
        private CharacterVisualHandles lobbyHeroVisuals;
        private readonly CharacterAnimationDriver lobbyAnimDriver = new CharacterAnimationDriver();
        private bool matchEnded = false;

        private static readonly string[] BotNames =
        {
            "Ghost_VN", "SniperWolf", "PochinkiKing", "RedZone99", "ShadowHunter",
            "M416_Master", "Kar98_God", "BlueZoneRunner", "Valkyrie_07", "Falcon_X",
            "CyberSurvivor", "DesertEagle", "ApexPredator", "NightStalker", "StormBreaker",
            "IronVest", "TacticalFox", "OmegaSquad", "DeltaForce_VN"
        };

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetGameState((SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
                ? GameFlowState.MainMenuLobby
                : GameFlowState.AuthScreen);

            if (BattleRoyaleMapGenerator.Instance != null)
            {
                activeIslandRoot = BattleRoyaleMapGenerator.Instance.RebuildWorld(2026);
            }

            SetupLobbyHeroStage();
            PositionLobbyCamera();
        }

        private void Update()
        {
            if (CurrentState == GameFlowState.AuthScreen && SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
            {
                SetGameState(GameFlowState.MainMenuLobby);
            }

            if (CurrentState == GameFlowState.InMatch && !matchEnded)
            {
                MatchElapsedSeconds += Time.deltaTime;
                UpdateAliveCountAndWinCondition();
            }
            else if (CurrentState == GameFlowState.AuthScreen || CurrentState == GameFlowState.MainMenuLobby)
            {
                lobbyAnimDriver.UpdateLocomotion(0f, false, true, Time.deltaTime);
                AnimateLobbyCamera();
            }
        }

        private void SetGameState(GameFlowState nextState)
        {
            if (CurrentState == nextState) return;
            CurrentState = nextState;

            if (nextState == GameFlowState.InMatch)
            {
                if (lobbyStageRoot != null)
                    lobbyStageRoot.SetActive(false);
            }
            else
            {
                if (lobbyStageRoot == null)
                    SetupLobbyHeroStage();
                else
                    lobbyStageRoot.SetActive(true);
            }

            GameEvents.RaiseGameStateChanged(nextState);
        }

        public void GoToLobby()
        {
            CleanupMatchActors();
            if (BlueZoneController.Instance != null)
                BlueZoneController.Instance.StopZone();
            if (AirDropManager.Instance != null)
                AirDropManager.Instance.StopAirDrops();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SetGameState((SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
                ? GameFlowState.MainMenuLobby
                : GameFlowState.AuthScreen);

            SetupLobbyHeroStage();
            PositionLobbyCamera();
        }

        public void StartBattleRoyaleMatch(int totalPlayers = 10, string modeLabel = "online_10p")
        {
            CleanupMatchActors();
            if (lobbyStageRoot != null)
                lobbyStageRoot.SetActive(false);

            TotalParticipants = Mathf.Clamp(totalPlayers, 4, 20);
            SelectedGameMode = string.IsNullOrEmpty(modeLabel) ? "online_10p" : modeLabel;
            MatchElapsedSeconds = 0f;
            matchEnded = false;
            Killfeed.Clear();
            CloudSyncStatus = string.Empty;

            int seed = UnityEngine.Random.Range(1000, 99999);
            activeIslandRoot = BattleRoyaleMapGenerator.Instance.RebuildWorld(seed);

            GameObject actorsGo = new GameObject("MatchCombatants");
            activeMatchActorsRoot = actorsGo.transform;

            string myName = (SupabaseManager.Instance != null && SupabaseManager.Instance.CurrentProfile != null)
                ? SupabaseManager.Instance.CurrentProfile.display_name
                : "Survivor";

            Vector3 playerDrop = BattleRoyaleMapGenerator.Instance.GetRandomDropPosition(0, TotalParticipants);
            GameObject playerGo = new GameObject("LocalSurvivorPlayer");
            playerGo.transform.SetParent(activeMatchActorsRoot, false);

            CharacterController cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.42f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.stepOffset = 0.45f;
            cc.slopeLimit = 50f;

            CapsuleCollider playerHitbox = playerGo.AddComponent<CapsuleCollider>();
            playerHitbox.height = 1.9f;
            playerHitbox.radius = 0.42f;
            playerHitbox.center = new Vector3(0f, 0.95f, 0f);

            CombatantHealth playerHealth = playerGo.AddComponent<CombatantHealth>();
            SurvivalPlayerController playerCtrl = playerGo.AddComponent<SurvivalPlayerController>();
            playerCtrl.SetupSpawn(playerDrop, myName);
            playerHealth.OnDied += OnAnyCombatantDied;

            int botCount = TotalParticipants - 1;
            for (int i = 0; i < botCount; i++)
            {
                Vector3 botDrop = BattleRoyaleMapGenerator.Instance.GetRandomDropPosition(i + 1, TotalParticipants);
                string botName = BotNames[i % BotNames.Length];

                GameObject botGo = new GameObject("Bot_" + botName);
                botGo.transform.SetParent(activeMatchActorsRoot, false);

                CharacterController botCc = botGo.AddComponent<CharacterController>();
                botCc.height = 1.9f;
                botCc.radius = 0.42f;
                botCc.center = new Vector3(0f, 0.95f, 0f);

                CapsuleCollider botHitbox = botGo.AddComponent<CapsuleCollider>();
                botHitbox.height = 1.9f;
                botHitbox.radius = 0.42f;
                botHitbox.center = new Vector3(0f, 0.95f, 0f);

                CombatantHealth botHealth = botGo.AddComponent<CombatantHealth>();
                BotAIController botAi = botGo.AddComponent<BotAIController>();
                botAi.InitializeBot(botName, botDrop);
                botHealth.OnDied += OnAnyCombatantDied;
            }

            AliveCount = TotalParticipants;

            if (BlueZoneController.Instance != null)
                BlueZoneController.Instance.StartZoneCycle();

            if (AirDropManager.Instance != null)
            {
                AirDropManager.Instance.BeginAirDrops(activeIslandRoot);
                AirDropManager.Instance.OnAirDropSpawned -= HandleAirDropNotification;
                AirDropManager.Instance.OnAirDropSpawned += HandleAirDropNotification;
            }

            SetGameState(GameFlowState.InMatch);
        }

        private void HandleAirDropNotification(Vector3 pos)
        {
            GameEvents.RaisePlayerNotification("MÁY BAY ĐÃ THẢ HÒM THÍNH (AWM + GIÁP 3)! Hãy tìm cột khói đỏ!");
        }

        private void OnAnyCombatantDied(CombatantHealth victim, CombatantHealth killer, string weaponUsed, bool isHeadshot)
        {
            if (matchEnded) return;

            string killerLabel = killer != null ? killer.combatantName : "Blue Zone";
            bool involvesLocal = (victim != null && victim.isLocalPlayer) || (killer != null && killer.isLocalPlayer);

            KillfeedItem entry = new KillfeedItem
            {
                killerName = killerLabel,
                victimName = victim != null ? victim.combatantName : "Unknown",
                weaponName = string.IsNullOrEmpty(weaponUsed) ? "Blue Zone" : weaponUsed,
                isHeadshot = isHeadshot,
                timestamp = Time.time,
                involvesLocalPlayer = involvesLocal
            };

            Killfeed.Insert(0, entry);
            if (Killfeed.Count > 6)
                Killfeed.RemoveAt(Killfeed.Count - 1);

            int livingNow = CountLivingCombatants();
            AliveCount = Mathf.Max(1, livingNow);

            if (victim != null && victim.isLocalPlayer)
            {
                int placement = Mathf.Clamp(livingNow + 1, 2, TotalParticipants);
                EndMatch(false, placement);
            }
            else if (livingNow <= 1 && SurvivalPlayerController.LocalInstance != null &&
                     SurvivalPlayerController.LocalInstance.Health != null &&
                     SurvivalPlayerController.LocalInstance.Health.IsAlive)
            {
                EndMatch(true, 1);
            }
        }

        private void UpdateAliveCountAndWinCondition()
        {
            int livingNow = CountLivingCombatants();
            AliveCount = Mathf.Max(1, livingNow);

            SurvivalPlayerController local = SurvivalPlayerController.LocalInstance;
            if (local == null || local.Health == null)
                return;

            if (!local.Health.IsAlive)
            {
                EndMatch(false, Mathf.Clamp(livingNow + 1, 2, TotalParticipants));
            }
            else if (livingNow <= 1)
            {
                EndMatch(true, 1);
            }
        }

        private int CountLivingCombatants()
        {
            int count = 0;
            for (int i = 0; i < CombatantHealth.ActiveCombatants.Count; i++)
            {
                CombatantHealth c = CombatantHealth.ActiveCombatants[i];
                if (c != null && c.IsAlive)
                    count++;
            }
            return count;
        }

        private void EndMatch(bool won, int placement)
        {
            if (matchEnded) return;
            matchEnded = true;

            LocalPlayerWon = won;
            FinalPlacement = won ? 1 : Mathf.Clamp(placement, 2, TotalParticipants);

            SurvivalPlayerController local = SurvivalPlayerController.LocalInstance;
            if (local != null && local.Health != null)
            {
                FinalKills = local.Health.kills;
                FinalDamageDealt = local.Health.totalDamageDealt;
                local.LockCursor(false);
            }
            else
            {
                FinalKills = 0;
                FinalDamageDealt = 0;
            }

            FinalSurvivedSeconds = Mathf.RoundToInt(MatchElapsedSeconds);

            if (BlueZoneController.Instance != null)
                BlueZoneController.Instance.StopZone();
            if (AirDropManager.Instance != null)
                AirDropManager.Instance.StopAirDrops();

            SetGameState(GameFlowState.PostMatchSummary);
            CloudSyncStatus = "Đang đồng bộ kết quả trận đấu lên Supabase Cloud...";

            if (SupabaseManager.Instance != null && SupabaseManager.Instance.IsLoggedIn)
            {
                SupabaseManager.Instance.RecordMatchResult(
                    FinalPlacement,
                    FinalKills,
                    FinalDamageDealt,
                    FinalSurvivedSeconds,
                    SelectedGameMode,
                    (ok, msg) =>
                    {
                        CloudSyncStatus = msg;
                    }
                );
            }
            else
            {
                CloudSyncStatus = "Chế độ Offline (Chưa đăng nhập Supabase).";
            }
        }

        private void CleanupMatchActors()
        {
            CombatantHealth.ActiveCombatants.Clear();
            if (activeMatchActorsRoot != null)
            {
                MaterialPool.SafeDestroy(activeMatchActorsRoot.gameObject);
                activeMatchActorsRoot = null;
            }
        }

        public void SetupLobbyHeroStage()
        {
            if (lobbyStageRoot != null)
                MaterialPool.SafeDestroy(lobbyStageRoot);

            lobbyStageRoot = new GameObject("LobbyHeroStage");

            // Bục đứng chiến thuật hình bát giác / khối kim loại
            MaterialPool.CreatePrimitive(
                PrimitiveType.Cylinder,
                lobbyStageRoot.transform,
                "Pedestal",
                new Vector3(0f, 0.05f, 0f),
                new Vector3(2.6f, 0.1f, 2.6f),
                new Color(0.18f, 0.22f, 0.26f),
                false);

            MaterialPool.CreatePrimitive(
                PrimitiveType.Cylinder,
                lobbyStageRoot.transform,
                "PedestalRing",
                new Vector3(0f, 0.11f, 0f),
                new Vector3(2.4f, 0.02f, 2.4f),
                new Color(0.95f, 0.75f, 0.20f),
                false);

            // Đèn chiếu Spotlight rọi thẳng vào nhân vật (như trong PUBG / Free Fire)
            GameObject spotGo = new GameObject("HeroSpotlight");
            spotGo.transform.SetParent(lobbyStageRoot.transform, false);
            spotGo.transform.position = new Vector3(0.5f, 3.8f, -2.4f);
            spotGo.transform.LookAt(new Vector3(0f, 1.1f, 0f));
            Light spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 8.5f;
            spot.spotAngle = 45f;
            spot.intensity = 2.4f;
            spot.color = new Color(1f, 0.98f, 0.92f);

            // Spawn nhân vật mặc định
            string savedChar = PlayerPrefs.GetString("SB_CHAR_MODEL", "George");
            int savedSkin = PlayerPrefs.GetInt("SB_CHAR_SKIN", 0);
            RefreshLobbyHeroCharacter(savedChar, savedSkin);
        }

        public void RefreshLobbyHeroCharacter(string characterName, int skinIndex)
        {
            if (lobbyStageRoot == null) return;

            Transform existing = lobbyStageRoot.transform.Find("HeroModel");
            if (existing != null)
                MaterialPool.SafeDestroy(existing.gameObject);

            GameObject heroHolder = new GameObject("HeroModel");
            heroHolder.transform.SetParent(lobbyStageRoot.transform, false);
            heroHolder.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            heroHolder.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

            lobbyHeroVisuals = CharacterVisualBuilder.BuildPlayerVisuals(
                heroHolder.transform,
                WeaponId.M416,
                characterName,
                skinIndex);

            // Ẩn dù, hiện giáp mũ lv2 chiến thuật
            if (lobbyHeroVisuals.parachuteVisual != null)
                lobbyHeroVisuals.parachuteVisual.SetActive(false);
            if (lobbyHeroVisuals.helmetVisual != null)
            {
                lobbyHeroVisuals.helmetVisual.SetActive(true);
                MaterialPool.ApplyLitColor(lobbyHeroVisuals.helmetRenderer, LootItem.GetTierColor(2));
            }
            if (lobbyHeroVisuals.vestVisual != null)
            {
                lobbyHeroVisuals.vestVisual.SetActive(true);
                MaterialPool.ApplyLitColor(lobbyHeroVisuals.vestRenderer, LootItem.GetTierColor(2));
            }

            lobbyAnimDriver.Initialize(lobbyHeroVisuals.legacyAnimation);
        }

        private void PositionLobbyCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(0.35f, 1.25f, -2.85f);
                cam.transform.LookAt(new Vector3(0f, 1.15f, 0f));
            }
        }

        private void AnimateLobbyCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Camera thở nhẹ nhàng (Subtle breathing sway) chuẩn điện ảnh AAA
            float t = Time.time * 0.8f;
            float swayX = 0.35f + Mathf.Sin(t) * 0.05f;
            float swayY = 1.25f + Mathf.Cos(t * 1.2f) * 0.025f;
            cam.transform.position = new Vector3(swayX, swayY, -2.85f);
            cam.transform.LookAt(new Vector3(0f, 1.12f + Mathf.Sin(t * 0.5f) * 0.015f, 0f));
        }
    }
}
