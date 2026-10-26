using System;
using Survival.Backend;
using Survival.Combat;
using Survival.Core;
using Survival.Items;
using Survival.Player;
using Survival.World;
using UnityEngine;

namespace Survival.UI
{
    public class SurvivalUIManager : MonoBehaviour
    {
        private static SurvivalUIManager instance;
        public static SurvivalUIManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<SurvivalUIManager>();
                return instance;
            }
            private set => instance = value;
        }

        private int authTab = 0;
        private string authEmailInput = "";
        private string authPasswordInput = "";
        private string authDisplayNameInput = "Survivor_VN";
        private string uiBannerMessage = "";

        private int lobbyTab = 0;
        private int selectedPlayerCount = 10;
        private string editDisplayNameInput = "";
        private string roomCodeInput = "PUBG01";
        private Vector2 scrollPos;

        private PlayerProfile[] leaderboardCache = Array.Empty<PlayerProfile>();
        private MatchHistoryEntry[] historyCache = Array.Empty<MatchHistoryEntry>();
        private RoomInfo[] roomsCache = Array.Empty<RoomInfo>();

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
            if (SupabaseManager.Instance != null)
            {
                SupabaseManager.Instance.OnProfileUpdated += prof =>
                {
                    if (prof != null)
                        editDisplayNameInput = prof.display_name;
                };

                if (SupabaseManager.Instance.CurrentProfile != null)
                {
                    editDisplayNameInput = SupabaseManager.Instance.CurrentProfile.display_name;
                }
            }
        }

        private void OnGUI()
        {
            GuiDrawUtils.EnsureInitialized();

            MatchManager mm = MatchManager.Instance;
            if (mm == null) return;

            switch (mm.CurrentState)
            {
                case GameFlowState.AuthScreen:
                    DrawSupabaseAuthScreen();
                    break;

                case GameFlowState.MainMenuLobby:
                    DrawMainLobbyScreen();
                    break;

                case GameFlowState.InMatch:
                    DrawInGamePubgHud();
                    break;

                case GameFlowState.PostMatchSummary:
                    DrawPostMatchSummaryScreen();
                    break;
            }
        }

        #region 1. AUTH SCREEN (CLEAN & MINIMAL)

        private void DrawSupabaseAuthScreen()
        {
            float w = 440f;
            float h = 390f;
            Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            GuiDrawUtils.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.07f, 0.75f));
            GuiDrawUtils.DrawTacticalPanel(card, new Color(0.08f, 0.10f, 0.14f, 0.96f), new Color(0.95f, 0.72f, 0.15f, 0.75f));

            GUILayout.BeginArea(new Rect(card.x + 24f, card.y + 20f, card.width - 48f, card.height - 36f));

            GUILayout.Label("SURVIVAL 01", GuiDrawUtils.TitleStyle);
            GUILayout.Label("BATTLE ROYALE", GuiDrawUtils.SubCenterStyle);
            GUILayout.Space(14f);

            GUILayout.BeginHorizontal();
            if (GuiDrawUtils.DrawTabButton("ĐĂNG NHẬP", authTab == 0, 126f, 32f)) authTab = 0;
            if (GuiDrawUtils.DrawTabButton("ĐĂNG KÝ", authTab == 1, 126f, 32f)) authTab = 1;
            if (GuiDrawUtils.DrawTabButton("CHƠI NGAY", authTab == 2, 130f, 32f)) authTab = 2;
            GUILayout.EndHorizontal();

            GUILayout.Space(14f);

            SupabaseManager sb = SupabaseManager.Instance;
            bool busy = sb != null && sb.IsBusy;

            if (authTab == 0)
            {
                GUILayout.Label("Tài khoản / Email", GuiDrawUtils.MutedLabelStyle);
                authEmailInput = GUILayout.TextField(authEmailInput, 64, GuiDrawUtils.TextFieldStyle, GUILayout.Height(28f));
                GUILayout.Space(6f);

                GUILayout.Label("Mật khẩu", GuiDrawUtils.MutedLabelStyle);
                authPasswordInput = GUILayout.PasswordField(authPasswordInput, '*', 64, GuiDrawUtils.TextFieldStyle, GUILayout.Height(28f));
                GUILayout.Space(14f);

                GUI.enabled = !busy;
                if (GUILayout.Button(busy ? "ĐANG KẾT NỐI..." : "ĐĂNG NHẬP", GuiDrawUtils.GoldButtonStyle, GUILayout.Height(40f)))
                {
                    sb.SignIn(authEmailInput, authPasswordInput, (ok, msg) =>
                    {
                        uiBannerMessage = msg;
                        if (ok)
                        {
                            editDisplayNameInput = sb.CurrentProfile.display_name;
                            MatchManager.Instance.GoToLobby();
                        }
                    });
                }
                GUI.enabled = true;
            }
            else if (authTab == 1)
            {
                GUILayout.Label("Tài khoản / Email", GuiDrawUtils.MutedLabelStyle);
                authEmailInput = GUILayout.TextField(authEmailInput, 64, GuiDrawUtils.TextFieldStyle, GUILayout.Height(26f));
                GUILayout.Space(4f);

                GUILayout.Label("Mật khẩu (≥ 6 ký tự)", GuiDrawUtils.MutedLabelStyle);
                authPasswordInput = GUILayout.PasswordField(authPasswordInput, '*', 64, GuiDrawUtils.TextFieldStyle, GUILayout.Height(26f));
                GUILayout.Space(4f);

                GUILayout.Label("Tên nhân vật", GuiDrawUtils.MutedLabelStyle);
                authDisplayNameInput = GUILayout.TextField(authDisplayNameInput, 24, GuiDrawUtils.TextFieldStyle, GUILayout.Height(26f));
                GUILayout.Space(12f);

                GUI.enabled = !busy;
                if (GUILayout.Button(busy ? "ĐANG TẠO..." : "TẠO TÀI KHOẢN", GuiDrawUtils.GoldButtonStyle, GUILayout.Height(40f)))
                {
                    sb.SignUp(authEmailInput, authPasswordInput, authDisplayNameInput, (ok, msg) =>
                    {
                        uiBannerMessage = msg;
                        if (ok)
                        {
                            editDisplayNameInput = sb.CurrentProfile.display_name;
                            MatchManager.Instance.GoToLobby();
                        }
                    });
                }
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("Tên nhân vật", GuiDrawUtils.MutedLabelStyle);
                authDisplayNameInput = GUILayout.TextField(authDisplayNameInput, 24, GuiDrawUtils.TextFieldStyle, GUILayout.Height(30f));
                GUILayout.Space(18f);

                GUI.enabled = !busy;
                if (GUILayout.Button(busy ? "ĐANG VÀO..." : "VÀO GAME (GUEST)", GuiDrawUtils.GoldButtonStyle, GUILayout.Height(44f)))
                {
                    sb.SignInAsGuest(authDisplayNameInput, (ok, msg) =>
                    {
                        uiBannerMessage = msg;
                        if (ok)
                        {
                            editDisplayNameInput = sb.CurrentProfile.display_name;
                            MatchManager.Instance.GoToLobby();
                        }
                    });
                }
                GUI.enabled = true;
            }

            GUILayout.FlexibleSpace();

            string statusText = sb != null && !string.IsNullOrEmpty(sb.LastStatusMessage) ? sb.LastStatusMessage : uiBannerMessage;
            if (!string.IsNullOrEmpty(statusText))
            {
                GUILayout.Label(statusText, GuiDrawUtils.SubCenterStyle);
            }

            GUILayout.EndArea();
        }

        #endregion

        #region 2. MAIN MENU LOBBY (STREAMLINED & ELEGANT)

        private void DrawMainLobbyScreen()
        {
            SupabaseManager sb = SupabaseManager.Instance;
            PlayerProfile profile = sb != null && sb.CurrentProfile != null ? sb.CurrentProfile : new PlayerProfile();

            // Minimalist Header Bar
            Rect topBar = new Rect(0f, 0f, Screen.width, 54f);
            GuiDrawUtils.DrawRect(topBar, new Color(0.06f, 0.08f, 0.11f, 0.94f));
            GuiDrawUtils.DrawRect(new Rect(0f, 53f, Screen.width, 1f), new Color(0.95f, 0.72f, 0.15f, 0.5f));

            GUI.Label(new Rect(20f, 12f, 220f, 32f), "SURVIVAL 01", GuiDrawUtils.TitleStyle);

            float navX = 260f;
            if (GuiDrawUtils.DrawPosButton(new Rect(navX, 12f, 110f, 30f), "TRẬN ĐẤU", lobbyTab == 0)) lobbyTab = 0;
            if (GuiDrawUtils.DrawPosButton(new Rect(navX + 118f, 12f, 110f, 30f), "PHÒNG CHỜ", lobbyTab == 1)) { lobbyTab = 1; RefreshRoomsList(); }
            if (GuiDrawUtils.DrawPosButton(new Rect(navX + 236f, 12f, 115f, 30f), "XẾP HẠNG", lobbyTab == 2)) { lobbyTab = 2; RefreshLeaderboard(); }
            if (GuiDrawUtils.DrawPosButton(new Rect(navX + 359f, 12f, 110f, 30f), "LỊCH SỬ", lobbyTab == 3)) { lobbyTab = 3; RefreshMatchHistory(); }
            if (GuiDrawUtils.DrawPosButton(new Rect(navX + 477f, 12f, 110f, 30f), "PHÍM TẮT", lobbyTab == 4)) lobbyTab = 4;

            // Compact User Badge on Top-Right
            GUI.Label(new Rect(Screen.width - 290f, 10f, 180f, 18f), profile.display_name, GuiDrawUtils.SubTitleStyle);
            GUI.Label(new Rect(Screen.width - 290f, 28f, 180f, 18f), "Top 1: " + profile.wins + "  |  Kills: " + profile.kills, GuiDrawUtils.MutedLabelStyle);

            if (GUI.Button(new Rect(Screen.width - 98f, 12f, 82f, 30f), "THOÁT", GuiDrawUtils.ButtonStyle))
            {
                if (sb != null) sb.SignOut();
                MatchManager.Instance.GoToLobby();
                return;
            }

            Rect contentRect = new Rect(28f, 72f, Screen.width - 56f, Screen.height - 96f);

            switch (lobbyTab)
            {
                case 0: DrawLobbyPlayTab(contentRect, profile, sb); break;
                case 1: DrawLobbyRoomsTab(contentRect, profile, sb); break;
                case 2: DrawLobbyLeaderboardTab(contentRect); break;
                case 3: DrawLobbyHistoryTab(contentRect); break;
                case 4: DrawLobbyControlsTab(contentRect); break;
            }
        }

        private void DrawLobbyPlayTab(Rect area, PlayerProfile profile, SupabaseManager sb)
        {
            // Left Card: Player Profile Summary
            float leftW = 340f;
            Rect leftCard = new Rect(area.x, area.y, leftW, area.height);
            GuiDrawUtils.DrawTacticalPanel(leftCard, new Color(0.07f, 0.09f, 0.12f, 0.90f), new Color(0.24f, 0.36f, 0.50f, 0.6f));

            GUILayout.BeginArea(new Rect(leftCard.x + 18f, leftCard.y + 16f, leftCard.width - 36f, leftCard.height - 32f));
            GUILayout.Label("HỒ SƠ NGƯỜI CHƠI", GuiDrawUtils.SubTitleStyle);
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            editDisplayNameInput = GUILayout.TextField(editDisplayNameInput, 20, GuiDrawUtils.TextFieldStyle, GUILayout.Height(26f));
            if (GUILayout.Button("LƯU", GuiDrawUtils.ButtonStyle, GUILayout.Width(60f), GUILayout.Height(26f)))
            {
                if (sb != null)
                    sb.UpdateDisplayName(editDisplayNameInput, (ok, msg) => { uiBannerMessage = msg; });
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12f);
            GuiDrawUtils.DrawStatRow("Trận đã đấu:", profile.matches_played.ToString());
            GuiDrawUtils.DrawStatRow("Thắng (Top 1):", profile.wins.ToString());
            GuiDrawUtils.DrawStatRow("Vào Top 3:", profile.top3_finishes.ToString());
            GuiDrawUtils.DrawStatRow("Tỷ lệ thắng:", profile.GetWinRatePercent() + "%");
            GuiDrawUtils.DrawStatRow("Hạ gục (Kills):", profile.kills.ToString());
            GuiDrawUtils.DrawStatRow("Bị hạ (Deaths):", profile.deaths.ToString());
            GuiDrawUtils.DrawStatRow("K/D:", profile.GetKDRatio().ToString("0.00"));
            GuiDrawUtils.DrawStatRow("Sát thương:", profile.total_damage.ToString());

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("ĐỒNG BỘ CLOUD", GuiDrawUtils.ButtonStyle, GUILayout.Height(30f)))
            {
                if (sb != null) sb.RefreshProfile((ok, msg) => { uiBannerMessage = msg; });
            }
            GUILayout.EndArea();

            // Right Card: Match Setup
            float rightW = 360f;
            Rect rightCard = new Rect(area.xMax - rightW, area.y, rightW, area.height);
            GuiDrawUtils.DrawTacticalPanel(rightCard, new Color(0.07f, 0.09f, 0.12f, 0.90f), new Color(0.95f, 0.72f, 0.15f, 0.75f));

            GUILayout.BeginArea(new Rect(rightCard.x + 18f, rightCard.y + 16f, rightCard.width - 36f, rightCard.height - 32f));
            GUILayout.Label("BẢN ĐỒ: ERANGEL MINI", GuiDrawUtils.SubTitleStyle);
            GUILayout.Label("6 POI • Nhà mở cửa 2 tầng • Hòm thính AWM", GuiDrawUtils.MutedLabelStyle);
            GUILayout.Space(14f);

            GUILayout.Label("Số lượng người tham gia:", GuiDrawUtils.MutedLabelStyle);
            GUILayout.BeginHorizontal();
            if (GuiDrawUtils.DrawTabButton("10", selectedPlayerCount == 10, (rightCard.width - 44f) * 0.33f, 32f)) selectedPlayerCount = 10;
            if (GuiDrawUtils.DrawTabButton("15", selectedPlayerCount == 15, (rightCard.width - 44f) * 0.33f, 32f)) selectedPlayerCount = 15;
            if (GuiDrawUtils.DrawTabButton("20", selectedPlayerCount == 20, (rightCard.width - 44f) * 0.33f, 32f)) selectedPlayerCount = 20;
            GUILayout.EndHorizontal();

            GUILayout.Space(14f);
            GUILayout.Label("Nhân vật 3D:", GuiDrawUtils.MutedLabelStyle);
            string currentChar = PlayerPrefs.GetString("SB_CHAR_MODEL", "George");
            int currentSkin = PlayerPrefs.GetInt("SB_CHAR_SKIN", 0);
            GUILayout.BeginHorizontal();
            for (int c = 0; c < ModelCatalog.CharacterNames.Length; c++)
            {
                string cName = ModelCatalog.CharacterNames[c];
                if (GuiDrawUtils.DrawTabButton(cName.ToUpperInvariant(), currentChar == cName, (rightCard.width - 44f) * 0.25f, 28f))
                {
                    PlayerPrefs.SetString("SB_CHAR_MODEL", cName);
                    PlayerPrefs.Save();
                    if (MatchManager.Instance != null)
                        MatchManager.Instance.RefreshLobbyHeroCharacter(cName, currentSkin);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("Trang phục / Màu áo:", GuiDrawUtils.MutedLabelStyle);
            GUILayout.BeginHorizontal();
            for (int s = 0; s <= 4; s++)
            {
                string sLabel = s == 0 ? "GỐC" : "SKIN " + s;
                if (GuiDrawUtils.DrawTabButton(sLabel, currentSkin == s, (rightCard.width - 48f) * 0.20f, 24f))
                {
                    PlayerPrefs.SetInt("SB_CHAR_SKIN", s);
                    PlayerPrefs.Save();
                    if (MatchManager.Instance != null)
                        MatchManager.Instance.RefreshLobbyHeroCharacter(currentChar, s);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("VÀO TRẬN ĐẤU", GuiDrawUtils.GoldButtonStyle, GUILayout.Height(50f)))
            {
                string modeTag = "br_" + selectedPlayerCount + "p";
                MatchManager.Instance.StartBattleRoyaleMatch(selectedPlayerCount, modeTag);
            }
            GUILayout.EndArea();
        }

        private void DrawLobbyRoomsTab(Rect area, PlayerProfile profile, SupabaseManager sb)
        {
            GuiDrawUtils.DrawTacticalPanel(area, new Color(0.07f, 0.09f, 0.12f, 0.94f), new Color(0.24f, 0.36f, 0.50f, 0.6f));
            GUILayout.BeginArea(new Rect(area.x + 20f, area.y + 16f, area.width - 40f, area.height - 32f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("PHÒNG TRỰC TUYẾN", GuiDrawUtils.SubTitleStyle, GUILayout.Width(160f));
            roomCodeInput = GUILayout.TextField(roomCodeInput, 12, GuiDrawUtils.TextFieldStyle, GUILayout.Width(110f), GUILayout.Height(28f));

            if (GUILayout.Button("TẠO PHÒNG", GuiDrawUtils.GoldButtonStyle, GUILayout.Width(110f), GUILayout.Height(28f)))
            {
                RoomInfo newRoom = new RoomInfo
                {
                    room_code = string.IsNullOrWhiteSpace(roomCodeInput) ? "ROOM01" : roomCodeInput.Trim().ToUpperInvariant(),
                    host_name = profile.display_name,
                    host_ip = "127.0.0.1",
                    host_port = 7777,
                    current_players = 1,
                    max_players = selectedPlayerCount,
                    status = "waiting"
                };

                if (sb != null)
                {
                    sb.CreateOrUpdateRoom(newRoom, (ok, msg) =>
                    {
                        uiBannerMessage = msg;
                        RefreshRoomsList();
                    });
                }
            }

            if (GUILayout.Button("LÀM MỚI", GuiDrawUtils.ButtonStyle, GUILayout.Width(85f), GUILayout.Height(28f)))
            {
                RefreshRoomsList();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            if (roomsCache == null || roomsCache.Length == 0)
            {
                GUILayout.Label("Chưa có phòng nào. Nhấn 'TẠO PHÒNG' để mở phòng mới.", GuiDrawUtils.MutedLabelStyle);
            }
            else
            {
                foreach (RoomInfo r in roomsCache)
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label("[" + r.room_code + "]  " + r.host_name + "  (" + r.current_players + "/" + r.max_players + ")", GuiDrawUtils.LabelStyle, GUILayout.Height(26f));
                    if (GUILayout.Button("THAM GIA", GuiDrawUtils.GoldButtonStyle, GUILayout.Width(100f), GUILayout.Height(26f)))
                    {
                        MatchManager.Instance.ActiveRoomCode = r.room_code;
                        MatchManager.Instance.StartBattleRoyaleMatch(Mathf.Max(10, r.max_players), "room_" + r.room_code);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawLobbyLeaderboardTab(Rect area)
        {
            GuiDrawUtils.DrawTacticalPanel(area, new Color(0.07f, 0.09f, 0.12f, 0.94f), new Color(0.95f, 0.72f, 0.15f, 0.75f));
            GUILayout.BeginArea(new Rect(area.x + 20f, area.y + 16f, area.width - 40f, area.height - 32f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("BẢNG XẾP HẠNG", GuiDrawUtils.SubTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("LÀM MỚI", GuiDrawUtils.ButtonStyle, GUILayout.Width(85f), GUILayout.Height(26f)))
            {
                RefreshLeaderboard();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            GUILayout.BeginHorizontal("box");
            GUILayout.Label("#", GuiDrawUtils.SubTitleStyle, GUILayout.Width(45f));
            GUILayout.Label("CHIẾN BINH", GuiDrawUtils.SubTitleStyle, GUILayout.Width(220f));
            GUILayout.Label("TOP 1", GuiDrawUtils.SubTitleStyle, GUILayout.Width(90f));
            GUILayout.Label("KILLS", GuiDrawUtils.SubTitleStyle, GUILayout.Width(90f));
            GUILayout.Label("TOP 3", GuiDrawUtils.SubTitleStyle, GUILayout.Width(80f));
            GUILayout.Label("TRẬN", GuiDrawUtils.SubTitleStyle, GUILayout.Width(80f));
            GUILayout.Label("SÁT THƯƠNG", GuiDrawUtils.SubTitleStyle, GUILayout.Width(120f));
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            if (leaderboardCache != null && leaderboardCache.Length > 0)
            {
                for (int i = 0; i < leaderboardCache.Length; i++)
                {
                    PlayerProfile p = leaderboardCache[i];
                    if (p == null) continue;

                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label((i + 1).ToString(), GuiDrawUtils.MutedLabelStyle, GUILayout.Width(45f));
                    GUILayout.Label(p.display_name, GuiDrawUtils.LabelStyle, GUILayout.Width(220f));
                    GUILayout.Label(p.wins.ToString(), GuiDrawUtils.LabelStyle, GUILayout.Width(90f));
                    GUILayout.Label(p.kills.ToString(), GuiDrawUtils.LabelStyle, GUILayout.Width(90f));
                    GUILayout.Label(p.top3_finishes.ToString(), GuiDrawUtils.LabelStyle, GUILayout.Width(80f));
                    GUILayout.Label(p.matches_played.ToString(), GuiDrawUtils.LabelStyle, GUILayout.Width(80f));
                    GUILayout.Label(p.total_damage.ToString(), GuiDrawUtils.LabelStyle, GUILayout.Width(120f));
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("Đang tải dữ liệu...", GuiDrawUtils.MutedLabelStyle);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawLobbyHistoryTab(Rect area)
        {
            GuiDrawUtils.DrawTacticalPanel(area, new Color(0.07f, 0.09f, 0.12f, 0.94f), new Color(0.24f, 0.36f, 0.50f, 0.6f));
            GUILayout.BeginArea(new Rect(area.x + 20f, area.y + 16f, area.width - 40f, area.height - 32f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("LỊCH SỬ TRẬN ĐẤU", GuiDrawUtils.SubTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("LÀM MỚI", GuiDrawUtils.ButtonStyle, GUILayout.Width(85f), GUILayout.Height(26f)))
            {
                RefreshMatchHistory();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            GUILayout.BeginHorizontal("box");
            GUILayout.Label("HẠNG", GuiDrawUtils.SubTitleStyle, GUILayout.Width(90f));
            GUILayout.Label("CHIẾN BINH", GuiDrawUtils.SubTitleStyle, GUILayout.Width(200f));
            GUILayout.Label("KILLS", GuiDrawUtils.SubTitleStyle, GUILayout.Width(90f));
            GUILayout.Label("SÁT THƯƠNG", GuiDrawUtils.SubTitleStyle, GUILayout.Width(110f));
            GUILayout.Label("THỜI GIAN", GuiDrawUtils.SubTitleStyle, GUILayout.Width(120f));
            GUILayout.Label("CHẾ ĐỘ", GuiDrawUtils.SubTitleStyle, GUILayout.Width(110f));
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            if (historyCache != null && historyCache.Length > 0)
            {
                foreach (MatchHistoryEntry m in historyCache)
                {
                    if (m == null) continue;
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label("#" + m.placement, GuiDrawUtils.SubTitleStyle, GUILayout.Width(90f));
                    GUILayout.Label(m.display_name, GuiDrawUtils.LabelStyle, GUILayout.Width(200f));
                    GUILayout.Label(m.kills.ToString(), GuiDrawUtils.LabelStyle, GUILayout.Width(90f));
                    GUILayout.Label(m.damage_dealt + " HP", GuiDrawUtils.LabelStyle, GUILayout.Width(110f));
                    GUILayout.Label(m.survived_seconds + "s", GuiDrawUtils.LabelStyle, GUILayout.Width(120f));
                    GUILayout.Label(m.mode, GuiDrawUtils.MutedLabelStyle, GUILayout.Width(110f));
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("Chưa có lịch sử đấu.", GuiDrawUtils.MutedLabelStyle);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawLobbyControlsTab(Rect area)
        {
            GuiDrawUtils.DrawTacticalPanel(area, new Color(0.07f, 0.09f, 0.12f, 0.94f), new Color(0.95f, 0.72f, 0.15f, 0.75f));
            GUILayout.BeginArea(new Rect(area.x + 24f, area.y + 18f, area.width - 48f, area.height - 36f));

            GUILayout.Label("PHÍM TẮT ĐIỀU KHIỂN", GuiDrawUtils.SubTitleStyle);
            GUILayout.Space(10f);

            GuiDrawUtils.DrawStatRow("W, A, S, D", "Di chuyển / Lái dù");
            GuiDrawUtils.DrawStatRow("Shift", "Chạy nhanh / Lao dù");
            GuiDrawUtils.DrawStatRow("Space", "Nhảy");
            GuiDrawUtils.DrawStatRow("C", "Ngồi (Giảm độ giật súng)");
            GuiDrawUtils.DrawStatRow("V", "Đổi góc nhìn TPP / FPP");
            GuiDrawUtils.DrawStatRow("Alt + Di chuột", "Quan sát tự do (Free Look)");
            GuiDrawUtils.DrawStatRow("Chuột Trái", "Bắn súng");
            GuiDrawUtils.DrawStatRow("Chuột Phải", "Ngắm bắn ADS / Bật Scope");
            GuiDrawUtils.DrawStatRow("R", "Nạp đạn");
            GuiDrawUtils.DrawStatRow("F / E", "Nhặt vật phẩm");
            GuiDrawUtils.DrawStatRow("1, 2, 3 / Con lăn", "Đổi vũ khí");
            GuiDrawUtils.DrawStatRow("4 / H", "Dùng Medkit / Bandage");
            GuiDrawUtils.DrawStatRow("5", "Nước tăng lực (Boost)");
            GuiDrawUtils.DrawStatRow("G", "Ném lựu đạn");
            GuiDrawUtils.DrawStatRow("M / Tab", "Bản đồ toàn đảo");
            GuiDrawUtils.DrawStatRow("Esc", "Menu tạm dừng");

            GUILayout.EndArea();
        }

        private void RefreshLeaderboard()
        {
            if (SupabaseManager.Instance != null)
            {
                SupabaseManager.Instance.FetchLeaderboard(20, (ok, list, msg) =>
                {
                    leaderboardCache = list ?? Array.Empty<PlayerProfile>();
                    uiBannerMessage = msg;
                });
            }
        }

        private void RefreshMatchHistory()
        {
            if (SupabaseManager.Instance != null)
            {
                SupabaseManager.Instance.FetchMatchHistory(20, (ok, list, msg) =>
                {
                    historyCache = list ?? Array.Empty<MatchHistoryEntry>();
                    uiBannerMessage = msg;
                });
            }
        }

        private void RefreshRoomsList()
        {
            if (SupabaseManager.Instance != null)
            {
                SupabaseManager.Instance.FetchWaitingRooms((ok, list, msg) =>
                {
                    roomsCache = list ?? Array.Empty<RoomInfo>();
                    uiBannerMessage = msg;
                });
            }
        }

        #endregion

        #region 3. IN-GAME HUD (MINIMAL & CLEAN)

        private void DrawInGamePubgHud()
        {
            SurvivalPlayerController player = SurvivalPlayerController.LocalInstance;
            if (player == null || player.Health == null)
                return;

            BlueZoneController zone = BlueZoneController.Instance;

            if (zone != null && zone.IsOutsideBlueZone(player.transform.position))
            {
                float pulse = 0.16f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.12f;
                GuiDrawUtils.DrawRect(new Rect(0, 0, Screen.width, 10f), new Color(0.1f, 0.45f, 1f, pulse));
                GuiDrawUtils.DrawRect(new Rect(0, Screen.height - 10f, Screen.width, 10f), new Color(0.1f, 0.45f, 1f, pulse));
                GuiDrawUtils.DrawRect(new Rect(0, 0, 10f, Screen.height), new Color(0.1f, 0.45f, 1f, pulse));
                GuiDrawUtils.DrawRect(new Rect(Screen.width - 10f, 0, 10f, Screen.height), new Color(0.1f, 0.45f, 1f, pulse));

                GUI.Label(new Rect(Screen.width * 0.5f - 180f, 84f, 360f, 24f), "NGOÀI VÒNG BO!", GuiDrawUtils.WarnStyle);
            }

            if (player.isAiming && player.ActiveWeapon != null && player.ActiveWeapon.spec.hasSniperScope)
            {
                DrawSniperScopeOverlay(player.ActiveWeapon.spec);
            }
            else
            {
                DrawCenterCrosshairAndHitmarker(player);
            }

            DrawTopCompassAndZoneBar(player, zone);
            DrawTopRightAliveAndMinimap(player, zone);
            DrawKillfeedPanel();
            DrawBottomStatusAndWeapons(player);
            DrawPromptsAndNotifications(player);

            if (player.isMapOpen)
            {
                TacticalMapRenderer.DrawFullMapModal(player, zone);
            }

            if (player.isPaused)
            {
                DrawPauseMenuModal(player);
            }
        }

        private void DrawTopCompassAndZoneBar(SurvivalPlayerController player, BlueZoneController zone)
        {
            float barW = 340f;
            Rect compassRect = new Rect((Screen.width - barW) * 0.5f, 10f, barW, 26f);
            GuiDrawUtils.DrawTacticalPanel(compassRect, new Color(0.06f, 0.08f, 0.11f, 0.75f), new Color(0.8f, 0.8f, 0.8f, 0.25f));

            float heading = (player.transform.eulerAngles.y % 360f + 360f) % 360f;
            string dirLabel = GuiDrawUtils.GetCardinalLabel(heading);
            GUI.Label(compassRect, dirLabel + "  •  " + Mathf.RoundToInt(heading) + "°", GuiDrawUtils.CenteredLabelStyle);

            if (zone != null && zone.IsZoneActive)
            {
                Rect zoneBar = new Rect((Screen.width - barW) * 0.5f, 38f, barW, 22f);
                Color zBg = zone.IsShrinking ? new Color(0.12f, 0.36f, 0.78f, 0.85f) : new Color(0.08f, 0.10f, 0.14f, 0.75f);
                GuiDrawUtils.DrawRect(zoneBar, zBg);

                float distToSafe = zone.DistanceToSafeZoneBorder(player.transform.position);
                int secs = Mathf.CeilToInt(Mathf.Max(0f, zone.PhaseTimer));
                string label = zone.IsShrinking ? "Đang co (" + secs + "s)" : "Co sau " + secs + "s";
                if (distToSafe > 0.5f) label += " • " + Mathf.RoundToInt(distToSafe) + "m";
                GUI.Label(zoneBar, label, GuiDrawUtils.CenteredLabelStyle);
            }
        }

        private void DrawTopRightAliveAndMinimap(SurvivalPlayerController player, BlueZoneController zone)
        {
            MatchManager mm = MatchManager.Instance;
            float rightX = Screen.width - 180f;

            Rect aliveBox = new Rect(rightX, 10f, 80f, 28f);
            Rect killsBox = new Rect(rightX + 86f, 10f, 80f, 28f);

            GuiDrawUtils.DrawRect(aliveBox, new Color(0.08f, 0.10f, 0.13f, 0.85f));
            GuiDrawUtils.DrawRect(new Rect(aliveBox.x, aliveBox.y, 36f, 28f), new Color(0.92f, 0.68f, 0.12f, 0.90f));
            GUI.Label(new Rect(aliveBox.x, aliveBox.y, 36f, 28f), "SỐNG", GuiDrawUtils.BadgeStyle);
            GUI.Label(new Rect(aliveBox.x + 36f, aliveBox.y, 44f, 28f), mm.AliveCount.ToString(), GuiDrawUtils.CenteredLabelStyle);

            GuiDrawUtils.DrawRect(killsBox, new Color(0.08f, 0.10f, 0.13f, 0.85f));
            GuiDrawUtils.DrawRect(new Rect(killsBox.x, killsBox.y, 36f, 28f), new Color(0.78f, 0.20f, 0.20f, 0.90f));
            GUI.Label(new Rect(killsBox.x, killsBox.y, 36f, 28f), "HẠ", GuiDrawUtils.BadgeStyle);
            GUI.Label(new Rect(killsBox.x + 36f, killsBox.y, 44f, 28f), player.Health.kills.ToString(), GuiDrawUtils.CenteredLabelStyle);

            Rect mapRect = new Rect(rightX, 44f, 166f, 166f);
            TacticalMapRenderer.DrawMapWidget(mapRect, player, zone, false);
        }

        private void DrawKillfeedPanel()
        {
            MatchManager mm = MatchManager.Instance;
            if (mm == null || mm.Killfeed.Count == 0) return;

            float x = 14f;
            float y = 14f;
            for (int i = 0; i < mm.Killfeed.Count; i++)
            {
                KillfeedItem item = mm.Killfeed[i];
                if (Time.time - item.timestamp > 8f) continue;

                Rect row = new Rect(x, y, 310f, 22f);
                Color bg = item.involvesLocalPlayer
                    ? new Color(0.40f, 0.25f, 0.08f, 0.85f)
                    : new Color(0.06f, 0.08f, 0.11f, 0.70f);
                GuiDrawUtils.DrawRect(row, bg);

                string hs = item.isHeadshot ? " [HS]" : "";
                string text = item.killerName + " [" + item.weaponName + hs + "] " + item.victimName;
                GUI.Label(new Rect(row.x + 6f, row.y + 2f, row.width - 12f, 18f), text, GuiDrawUtils.MutedLabelStyle);
                y += 25f;
            }
        }

        private void DrawCenterCrosshairAndHitmarker(SurvivalPlayerController player)
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            float gap = player.isAiming ? 3f : (player.isCrouching ? 6f : 9f);
            float len = 8f;
            Color crossCol = new Color(0.95f, 0.98f, 0.95f, 0.85f);

            GuiDrawUtils.DrawRect(new Rect(cx - gap - len, cy - 1f, len, 2f), crossCol);
            GuiDrawUtils.DrawRect(new Rect(cx + gap, cy - 1f, len, 2f), crossCol);
            GuiDrawUtils.DrawRect(new Rect(cx - 1f, cy - gap - len, 2f, len), crossCol);
            GuiDrawUtils.DrawRect(new Rect(cx - 1f, cy + gap, 2f, len), crossCol);

            float sinceHit = Time.time - player.LastHitMarkerTime;
            if (sinceHit < 0.30f)
            {
                Color hitCol = (player.LastHitWasKill || player.LastHitWasHeadshot)
                    ? new Color(1f, 0.22f, 0.22f, 1f)
                    : new Color(1f, 0.92f, 0.25f, 1f);

                GuiDrawUtils.DrawRect(new Rect(cx - 10f, cy - 10f, 6f, 2f), hitCol);
                GuiDrawUtils.DrawRect(new Rect(cx + 4f, cy - 10f, 6f, 2f), hitCol);
                GuiDrawUtils.DrawRect(new Rect(cx - 10f, cy + 8f, 6f, 2f), hitCol);
                GuiDrawUtils.DrawRect(new Rect(cx + 4f, cy + 8f, 6f, 2f), hitCol);

                string tag = player.LastHitWasKill
                    ? "HẠ GỤC"
                    : (player.LastHitWasHeadshot ? "HEADSHOT!" : "-" + player.LastHitDamage);
                GUI.Label(new Rect(cx + 14f, cy - 10f, 120f, 20f), tag, GuiDrawUtils.SubTitleStyle);
            }
        }

        private void DrawSniperScopeOverlay(WeaponSpec spec)
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float scopeSize = Mathf.Min(Screen.width, Screen.height) * 0.72f;
            float half = scopeSize * 0.5f;

            Color maskCol = new Color(0.02f, 0.03f, 0.04f, 0.94f);
            GuiDrawUtils.DrawRect(new Rect(0, 0, Screen.width, cy - half), maskCol);
            GuiDrawUtils.DrawRect(new Rect(0, cy + half, Screen.width, cy - half), maskCol);
            GuiDrawUtils.DrawRect(new Rect(0, cy - half, cx - half, scopeSize), maskCol);
            GuiDrawUtils.DrawRect(new Rect(cx + half, cy - half, cx - half, scopeSize), maskCol);

            GuiDrawUtils.DrawRect(new Rect(cx - half, cy - 0.75f, scopeSize, 1.5f), Color.black);
            GuiDrawUtils.DrawRect(new Rect(cx - 0.75f, cy - half, 1.5f, scopeSize), Color.black);
            GuiDrawUtils.DrawRect(new Rect(cx - 2.5f, cy - 2.5f, 5f, 5f), new Color(1f, 0.15f, 0.15f, 0.95f));

            GUI.Label(new Rect(cx - 90f, cy + half - 28f, 180f, 20f), spec.displayName, GuiDrawUtils.CenteredLabelStyle);
        }

        private void DrawBottomStatusAndWeapons(SurvivalPlayerController player)
        {
            float panelW = 440f;
            float panelH = 88f;
            Rect bottomRect = new Rect((Screen.width - panelW) * 0.5f, Screen.height - panelH - 12f, panelW, panelH);
            GuiDrawUtils.DrawTacticalPanel(bottomRect, new Color(0.06f, 0.08f, 0.11f, 0.85f), new Color(0.6f, 0.6f, 0.6f, 0.25f));

            CombatantHealth hp = player.Health;

            // Boost & HP Bars
            Rect boostBg = new Rect(bottomRect.x + 12f, bottomRect.y + 8f, bottomRect.width - 24f, 5f);
            GuiDrawUtils.DrawRect(boostBg, new Color(0.14f, 0.16f, 0.20f, 0.9f));
            float boostRatio = Mathf.Clamp01(hp.currentBoost / 100f);
            GuiDrawUtils.DrawRect(new Rect(boostBg.x, boostBg.y, boostBg.width * boostRatio, boostBg.height), new Color(0.95f, 0.65f, 0.12f, 0.95f));

            Rect hpBg = new Rect(bottomRect.x + 12f, bottomRect.y + 16f, bottomRect.width - 24f, 18f);
            GuiDrawUtils.DrawRect(hpBg, new Color(0.14f, 0.14f, 0.16f, 0.95f));
            float hpRatio = Mathf.Clamp01(hp.currentHealth / hp.maxHealth);
            Color hpColor = hpRatio > 0.5f
                ? new Color(0.24f, 0.78f, 0.36f, 0.95f)
                : (hpRatio > 0.25f ? new Color(0.92f, 0.62f, 0.14f, 0.95f) : new Color(0.88f, 0.18f, 0.18f, 0.95f));
            GuiDrawUtils.DrawRect(new Rect(hpBg.x, hpBg.y, hpBg.width * hpRatio, hpBg.height), hpColor);
            GUI.Label(hpBg, Mathf.CeilToInt(hp.currentHealth) + " / " + hp.maxHealth, GuiDrawUtils.CenteredLabelStyle);

            // Active Weapon & Ammo
            WeaponInstance activeWep = player.ActiveWeapon;
            string wepName = activeWep != null ? activeWep.spec.displayName : "Tay không";
            string ammoCount = activeWep != null ? activeWep.currentAmmo + " / " + player.reserveAmmo : "--";
            GUI.Label(new Rect(bottomRect.x + 12f, bottomRect.y + 38f, 260f, 20f), wepName, GuiDrawUtils.SubTitleStyle);
            GUI.Label(new Rect(bottomRect.x + 280f, bottomRect.y + 38f, 148f, 20f), ammoCount + "  |  Mũ " + hp.helmetLevel + " Giáp " + hp.armorLevel, GuiDrawUtils.MutedLabelStyle);

            // Quick Inventory Pills
            string invPills = "[1] " + GetSlotShort(player, 0) +
                              "  [2] " + GetSlotShort(player, 1) +
                              "  [3] " + GetSlotShort(player, 2) +
                              "  |  Med: " + player.medkitCount +
                              "  Nước: " + player.energyDrinkCount +
                              "  Lựu: " + player.grenadeCount;
            GUI.Label(new Rect(bottomRect.x + 12f, bottomRect.y + 60f, bottomRect.width - 24f, 20f), invPills, GuiDrawUtils.MutedLabelStyle);
        }

        private static string GetSlotShort(SurvivalPlayerController p, int index)
        {
            if (p.weaponSlots[index] == null || p.weaponSlots[index].spec == null) return "-";
            return p.weaponSlots[index].spec.shortCategory;
        }

        private void DrawPromptsAndNotifications(SurvivalPlayerController player)
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            if (player.isParachuting)
            {
                Rect paraBox = new Rect(cx - 150f, cy + 70f, 300f, 26f);
                GuiDrawUtils.DrawRect(paraBox, new Color(0.06f, 0.08f, 0.12f, 0.85f));
                int alt = Mathf.Max(0, Mathf.RoundToInt(player.transform.position.y));
                GUI.Label(paraBox, "ĐỘ CAO: " + alt + "m (Shift để lao nhanh)", GuiDrawUtils.CenteredLabelStyle);
            }

            if (player.isReloading)
            {
                Rect reloadRect = new Rect(cx - 80f, cy + 36f, 160f, 16f);
                GuiDrawUtils.DrawRect(reloadRect, new Color(0.08f, 0.10f, 0.14f, 0.85f));
                GuiDrawUtils.DrawRect(new Rect(reloadRect.x, reloadRect.y, reloadRect.width * player.ReloadProgress01, reloadRect.height),
                    new Color(0.95f, 0.75f, 0.18f, 0.9f));
                GUI.Label(reloadRect, "NẠP ĐẠN...", GuiDrawUtils.BadgeStyle);
            }

            if (player.NearestLoot != null)
            {
                Rect lootPrompt = new Rect(cx - 140f, cy + 48f, 280f, 26f);
                GuiDrawUtils.DrawTacticalPanel(lootPrompt, new Color(0.08f, 0.10f, 0.14f, 0.90f), new Color(0.95f, 0.76f, 0.18f, 0.9f));
                GUI.Label(lootPrompt, "[F] " + player.NearestLoot.displayLabel, GuiDrawUtils.CenteredLabelStyle);
            }

            if (Time.time - player.LastNotificationTime < 3.8f && !string.IsNullOrEmpty(player.LastActionNotification))
            {
                Rect noteRect = new Rect(cx - 200f, Screen.height - 140f, 400f, 24f);
                GuiDrawUtils.DrawRect(noteRect, new Color(0.06f, 0.08f, 0.12f, 0.80f));
                GUI.Label(noteRect, player.LastActionNotification, GuiDrawUtils.CenteredLabelStyle);
            }
        }

        private void DrawPauseMenuModal(SurvivalPlayerController player)
        {
            GuiDrawUtils.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.65f));
            Rect box = new Rect((Screen.width - 320f) * 0.5f, (Screen.height - 230f) * 0.5f, 320f, 230f);
            GuiDrawUtils.DrawTacticalPanel(box, new Color(0.08f, 0.10f, 0.14f, 0.96f), new Color(0.95f, 0.72f, 0.15f, 0.85f));

            GUILayout.BeginArea(new Rect(box.x + 20f, box.y + 18f, box.width - 40f, box.height - 36f));
            GUILayout.Label("TẠM DỪNG", GuiDrawUtils.TitleStyle);
            GUILayout.Space(12f);

            GUILayout.Label("Độ nhạy chuột: " + player.mouseSensitivity.ToString("0.0"), GuiDrawUtils.MutedLabelStyle);
            player.mouseSensitivity = GUILayout.HorizontalSlider(player.mouseSensitivity, 0.6f, 5.5f);
            GUILayout.Space(16f);

            if (GUILayout.Button("TIẾP TỤC", GuiDrawUtils.GoldButtonStyle, GUILayout.Height(36f)))
            {
                player.isPaused = false;
                player.LockCursor(true);
            }
            GUILayout.Space(6f);
            if (GUILayout.Button("VỀ SẢNH CHỜ", GuiDrawUtils.ButtonStyle, GUILayout.Height(32f)))
            {
                MatchManager.Instance.GoToLobby();
            }
            GUILayout.EndArea();
        }

        #endregion

        #region 4. POST-MATCH SUMMARY (CLEAN)

        private void DrawPostMatchSummaryScreen()
        {
            MatchManager mm = MatchManager.Instance;
            GuiDrawUtils.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.03f, 0.04f, 0.06f, 0.85f));

            float w = 440f;
            float h = 340f;
            Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Color border = mm.LocalPlayerWon ? new Color(1f, 0.80f, 0.16f, 0.95f) : new Color(0.85f, 0.25f, 0.25f, 0.85f);
            GuiDrawUtils.DrawTacticalPanel(card, new Color(0.07f, 0.09f, 0.12f, 0.97f), border);

            GUILayout.BeginArea(new Rect(card.x + 24f, card.y + 20f, card.width - 48f, card.height - 40f));

            if (mm.LocalPlayerWon)
                GUILayout.Label("WINNER WINNER CHICKEN DINNER!", GuiDrawUtils.TitleStyle);
            else
                GUILayout.Label("KẾT THÚC TRẬN ĐẤU", GuiDrawUtils.ElimTitleStyle);

            GUILayout.Space(6f);
            GUILayout.Label("TOP #" + mm.FinalPlacement + " / " + mm.TotalParticipants, GuiDrawUtils.RankStyle);
            GUILayout.Space(14f);

            GuiDrawUtils.DrawStatRow("Hạ gục (Kills):", mm.FinalKills.ToString());
            GuiDrawUtils.DrawStatRow("Sát thương:", mm.FinalDamageDealt + " HP");
            GuiDrawUtils.DrawStatRow("Thời gian sinh tồn:", mm.FinalSurvivedSeconds + "s");

            GUILayout.Space(10f);
            if (!string.IsNullOrEmpty(mm.CloudSyncStatus))
                GUILayout.Label(mm.CloudSyncStatus, GuiDrawUtils.SubCenterStyle);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CHƠI LẠI", GuiDrawUtils.GoldButtonStyle, GUILayout.Height(40f)))
            {
                mm.StartBattleRoyaleMatch(mm.TotalParticipants, mm.SelectedGameMode);
            }
            GUILayout.Space(8f);
            if (GUILayout.Button("SẢNH CHỜ", GuiDrawUtils.ButtonStyle, GUILayout.Height(40f)))
            {
                mm.GoToLobby();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        #endregion
    }
}
