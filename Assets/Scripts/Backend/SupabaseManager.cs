using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Survival.Backend
{
    /// <summary>
    /// Handles all Supabase Auth (/auth/v1/) and PostgREST (/rest/v1/) operations
    /// for player accounts, profiles, leaderboards, match history, and rooms,
    /// with automatic local cache synchronization.
    /// </summary>
    public class SupabaseManager : MonoBehaviour
    {
        private static SupabaseManager instance;
        public static SupabaseManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<SupabaseManager>();
                return instance;
            }
            private set => instance = value;
        }

        private const string PrefUserId = "SB_USER_ID";
        private const string PrefUserEmail = "SB_USER_EMAIL";
        private const string PrefDisplayName = "SB_DISPLAY_NAME";
        private const string PrefAccessToken = "SB_ACCESS_TOKEN";
        private const string PrefRefreshToken = "SB_REFRESH_TOKEN";
        private const string PrefIsGuest = "SB_IS_GUEST";
        private const string PrefCachedProfileJson = "SB_CACHED_PROFILE";
        private const string PrefLocalHistoryJson = "SB_LOCAL_HISTORY_V1";
        private const string PrefLocalRoomsJson = "SB_LOCAL_ROOMS_V1";

        [Serializable]
        private class LocalHistoryContainer
        {
            public List<MatchHistoryEntry> items = new List<MatchHistoryEntry>();
        }

        [Serializable]
        private class LocalRoomsContainer
        {
            public List<RoomInfo> items = new List<RoomInfo>();
        }

        public bool IsLoggedIn { get; private set; }
        public bool IsGuest { get; private set; }
        public bool IsBusy { get; private set; }
        public string CurrentUserId { get; private set; }
        public string CurrentEmail { get; private set; }
        public string AccessToken { get; private set; }
        public string LastStatusMessage { get; private set; } = "Sẵn sàng kết nối Supabase";
        public PlayerProfile CurrentProfile { get; private set; } = new PlayerProfile();

        public event Action<PlayerProfile> OnProfileUpdated;
        public event Action<bool, string> OnAuthStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            TryRestoreSavedSession();
        }

        public bool HasSavedSession()
        {
            return !string.IsNullOrEmpty(PlayerPrefs.GetString(PrefUserId, string.Empty));
        }

        public void TryRestoreSavedSession()
        {
            string savedId = PlayerPrefs.GetString(PrefUserId, string.Empty);
            if (string.IsNullOrEmpty(savedId))
                return;

            CurrentUserId = savedId;
            CurrentEmail = PlayerPrefs.GetString(PrefUserEmail, string.Empty);
            AccessToken = PlayerPrefs.GetString(PrefAccessToken, string.Empty);
            IsGuest = PlayerPrefs.GetInt(PrefIsGuest, 0) == 1;
            string savedName = PlayerPrefs.GetString(PrefDisplayName, "Survivor");

            string cachedJson = PlayerPrefs.GetString(PrefCachedProfileJson, string.Empty);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                try
                {
                    CurrentProfile = JsonUtility.FromJson<PlayerProfile>(cachedJson) ?? new PlayerProfile();
                }
                catch
                {
                    CurrentProfile = new PlayerProfile();
                }
            }

            CurrentProfile.id = CurrentUserId;
            if (string.IsNullOrEmpty(CurrentProfile.display_name))
                CurrentProfile.display_name = savedName;

            IsLoggedIn = true;
            LastStatusMessage = "Đã khôi phục phiên đăng nhập: " + CurrentProfile.display_name;

            StartCoroutine(LoadOrCreateProfileCoroutine(CurrentUserId, CurrentProfile.display_name, (ok, msg) =>
            {
                OnProfileUpdated?.Invoke(CurrentProfile);
                OnAuthStateChanged?.Invoke(true, LastStatusMessage);
            }));
        }

        public void SignUp(string emailOrUsername, string password, string displayName, Action<bool, string> callback)
        {
            if (IsBusy)
            {
                callback?.Invoke(false, "Đang xử lý yêu cầu khác, vui lòng đợi...");
                return;
            }

            StartCoroutine(SignUpCoroutine(emailOrUsername, password, displayName, callback));
        }

        public void SignIn(string emailOrUsername, string password, Action<bool, string> callback)
        {
            if (IsBusy)
            {
                callback?.Invoke(false, "Đang xử lý yêu cầu khác, vui lòng đợi...");
                return;
            }

            StartCoroutine(SignInCoroutine(emailOrUsername, password, callback));
        }

        public void SignInAsGuest(string displayName, Action<bool, string> callback)
        {
            if (IsBusy)
            {
                callback?.Invoke(false, "Đang xử lý yêu cầu khác, vui lòng đợi...");
                return;
            }

            StartCoroutine(SignInGuestCoroutine(displayName, callback));
        }

        public void SignOut()
        {
            AccessToken = string.Empty;
            CurrentUserId = string.Empty;
            CurrentEmail = string.Empty;
            IsLoggedIn = false;
            IsGuest = false;
            CurrentProfile = new PlayerProfile();

            PlayerPrefs.DeleteKey(PrefUserId);
            PlayerPrefs.DeleteKey(PrefUserEmail);
            PlayerPrefs.DeleteKey(PrefAccessToken);
            PlayerPrefs.DeleteKey(PrefRefreshToken);
            PlayerPrefs.DeleteKey(PrefIsGuest);
            PlayerPrefs.DeleteKey(PrefCachedProfileJson);
            PlayerPrefs.Save();

            LastStatusMessage = "Đã đăng xuất tài khoản.";
            OnAuthStateChanged?.Invoke(false, LastStatusMessage);
        }

        public void UpdateDisplayName(string newDisplayName, Action<bool, string> callback = null)
        {
            if (string.IsNullOrWhiteSpace(newDisplayName))
            {
                callback?.Invoke(false, "Tên nhân vật không được để trống.");
                return;
            }

            CurrentProfile.display_name = newDisplayName.Trim();
            PlayerPrefs.SetString(PrefDisplayName, CurrentProfile.display_name);
            SaveLocalProfileCache();

            StartCoroutine(UpsertProfileCoroutine(CurrentProfile, (ok, msg) =>
            {
                OnProfileUpdated?.Invoke(CurrentProfile);
                callback?.Invoke(ok, msg);
            }));
        }

        public void RefreshProfile(Action<bool, string> callback = null)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                callback?.Invoke(false, "Chưa đăng nhập.");
                return;
            }

            StartCoroutine(LoadOrCreateProfileCoroutine(CurrentUserId, CurrentProfile.display_name, (ok, msg) =>
            {
                OnProfileUpdated?.Invoke(CurrentProfile);
                callback?.Invoke(ok, msg);
            }));
        }

        public void RecordMatchResult(int placement, int kills, int damageDealt, int survivedSeconds, string mode, Action<bool, string> callback = null)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(CurrentUserId))
            {
                callback?.Invoke(false, "Chưa đăng nhập tài khoản.");
                return;
            }

            StartCoroutine(RecordMatchResultCoroutine(placement, kills, damageDealt, survivedSeconds, mode, callback));
        }

        public void FetchLeaderboard(int limit, Action<bool, PlayerProfile[], string> callback)
        {
            StartCoroutine(FetchLeaderboardCoroutine(limit, callback));
        }

        public void FetchMatchHistory(int limit, Action<bool, MatchHistoryEntry[], string> callback)
        {
            StartCoroutine(FetchMatchHistoryCoroutine(limit, callback));
        }

        public void CreateOrUpdateRoom(RoomInfo room, Action<bool, string> callback = null)
        {
            StartCoroutine(UpsertRoomCoroutine(room, callback));
        }

        public void FetchWaitingRooms(Action<bool, RoomInfo[], string> callback)
        {
            StartCoroutine(FetchWaitingRoomsCoroutine(callback));
        }

        public void DeleteRoom(string roomCode, Action<bool, string> callback = null)
        {
            StartCoroutine(DeleteRoomCoroutine(roomCode, callback));
        }

        private IEnumerator SignUpCoroutine(string emailOrUsername, string password, string displayName, Action<bool, string> callback)
        {
            string trimmedInput = (emailOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedInput))
            {
                callback?.Invoke(false, "Vui lòng nhập Email hoặc Tên tài khoản.");
                yield break;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                callback?.Invoke(false, "Mật khẩu phải có ít nhất 6 ký tự.");
                yield break;
            }

            string normalizedEmail = NormalizeEmailInput(trimmedInput);
            string chosenName = string.IsNullOrWhiteSpace(displayName)
                ? ExtractDefaultName(trimmedInput)
                : displayName.Trim();

            IsBusy = true;
            LastStatusMessage = "Đang đăng ký tài khoản trên Supabase...";

            AuthSignupPayload payload = new AuthSignupPayload
            {
                email = normalizedEmail,
                password = password,
                data = new AuthUserMetadata { display_name = chosenName }
            };

            string jsonBody = JsonUtility.ToJson(payload);
            string url = SupabaseConfig.AuthUrl + "signup";

            using (UnityWebRequest req = CreateJsonRequest(url, "POST", jsonBody, false))
            {
                yield return req.SendWebRequest();

                string respText = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    AuthSessionResponse session = JsonUtility.FromJson<AuthSessionResponse>(respText);
                    string userId = ExtractUserId(session, normalizedEmail, password);

                    AccessToken = session != null ? (session.access_token ?? string.Empty) : string.Empty;
                    CurrentUserId = userId;
                    CurrentEmail = normalizedEmail;
                    IsGuest = false;
                    IsLoggedIn = true;

                    SaveAccountIdentity(trimmedInput, password, userId, chosenName);

                    yield return LoadOrCreateProfileCoroutine(userId, chosenName, null);

                    IsBusy = false;
                    LastStatusMessage = "Đăng ký & lưu tài khoản lên Supabase thành công!";
                    SaveSessionPrefs(session != null ? session.refresh_token : string.Empty);
                    OnProfileUpdated?.Invoke(CurrentProfile);
                    OnAuthStateChanged?.Invoke(true, LastStatusMessage);
                    callback?.Invoke(true, LastStatusMessage);
                    yield break;
                }
                else
                {
                    string errMsg = ParseSupabaseError(respText, req.error);

                    if (errMsg.IndexOf("already registered", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        IsBusy = false;
                        LastStatusMessage = "Tài khoản đã tồn tại, đang chuyển sang đăng nhập...";
                        SignIn(emailOrUsername, password, callback);
                        yield break;
                    }

                    // Fallback to deterministic UUID synced with public.profiles
                    string fallbackUserId = GenerateDeterministicGuid(normalizedEmail + ":" + password);
                    CurrentUserId = fallbackUserId;
                    CurrentEmail = normalizedEmail;
                    AccessToken = string.Empty;
                    IsGuest = false;
                    IsLoggedIn = true;

                    SaveAccountIdentity(trimmedInput, password, fallbackUserId, chosenName);

                    bool profileSaved = false;
                    yield return LoadOrCreateProfileCoroutine(fallbackUserId, chosenName, (ok, msg) =>
                    {
                        profileSaved = ok;
                    });

                    IsBusy = false;
                    SaveSessionPrefs(string.Empty);
                    LastStatusMessage = profileSaved
                        ? "Tạo tài khoản & đồng bộ Supabase Profiles thành công!"
                        : "Đã tạo tài khoản (" + chosenName + ") & lưu bộ nhớ đệm!";
                    OnProfileUpdated?.Invoke(CurrentProfile);
                    OnAuthStateChanged?.Invoke(true, LastStatusMessage);
                    callback?.Invoke(true, LastStatusMessage);
                }
            }
        }

        private IEnumerator SignInCoroutine(string emailOrUsername, string password, Action<bool, string> callback)
        {
            string trimmedInput = (emailOrUsername ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmedInput) || string.IsNullOrEmpty(password))
            {
                callback?.Invoke(false, "Vui lòng nhập đầy đủ Tài khoản/Email và Mật khẩu.");
                yield break;
            }

            string normalizedEmail = NormalizeEmailInput(trimmedInput);
            string defaultName = ExtractDefaultName(trimmedInput);

            IsBusy = true;
            LastStatusMessage = "Đang đăng nhập vào Supabase...";

            AuthLoginPayload payload = new AuthLoginPayload
            {
                email = normalizedEmail,
                password = password
            };

            string jsonBody = JsonUtility.ToJson(payload);
            string url = SupabaseConfig.AuthUrl + "token?grant_type=password";

            using (UnityWebRequest req = CreateJsonRequest(url, "POST", jsonBody, false))
            {
                yield return req.SendWebRequest();

                string respText = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    AuthSessionResponse session = JsonUtility.FromJson<AuthSessionResponse>(respText);
                    string userId = ExtractUserId(session, normalizedEmail, password);
                    string metaName = (session != null && session.user != null && session.user.user_metadata != null &&
                                       !string.IsNullOrEmpty(session.user.user_metadata.display_name))
                        ? session.user.user_metadata.display_name
                        : defaultName;

                    AccessToken = session != null ? (session.access_token ?? string.Empty) : string.Empty;
                    CurrentUserId = userId;
                    CurrentEmail = normalizedEmail;
                    IsGuest = false;
                    IsLoggedIn = true;

                    SaveAccountIdentity(trimmedInput, password, userId, metaName);
                    yield return LoadOrCreateProfileCoroutine(userId, metaName, null);

                    IsBusy = false;
                    SaveSessionPrefs(session != null ? session.refresh_token : string.Empty);
                    LastStatusMessage = "Đăng nhập Supabase thành công! Xin chào " + CurrentProfile.display_name;
                    OnProfileUpdated?.Invoke(CurrentProfile);
                    OnAuthStateChanged?.Invoke(true, LastStatusMessage);
                    callback?.Invoke(true, LastStatusMessage);
                    yield break;
                }
                else
                {
                    string errMsg = ParseSupabaseError(respText, req.error);
                    bool emailUnconfirmed = errMsg.IndexOf("Email not confirmed", StringComparison.OrdinalIgnoreCase) >= 0;
                    string candidateId = GetSavedOrDeterministicUserId(trimmedInput, normalizedEmail, password);

                    // Check public.profiles on Supabase
                    string checkUrl = SupabaseConfig.RestUrl + SupabaseConfig.ProfilesTable + "?id=eq." + UnityWebRequest.EscapeURL(candidateId) + "&select=*";
                    using (UnityWebRequest profileReq = CreateJsonRequest(checkUrl, "GET", null, false))
                    {
                        yield return profileReq.SendWebRequest();

                        if (profileReq.result == UnityWebRequest.Result.Success)
                        {
                            PlayerProfile[] found = JsonArrayHelper.FromJsonArray<PlayerProfile>(profileReq.downloadHandler.text);
                            if (found != null && found.Length > 0)
                            {
                                CurrentUserId = found[0].id;
                                CurrentEmail = normalizedEmail;
                                AccessToken = string.Empty;
                                CurrentProfile = found[0];
                                IsGuest = false;
                                IsLoggedIn = true;
                                IsBusy = false;

                                SaveSessionPrefs(string.Empty);
                                LastStatusMessage = "Đã đăng nhập & tải hồ sơ từ Supabase: " + CurrentProfile.display_name;
                                OnProfileUpdated?.Invoke(CurrentProfile);
                                OnAuthStateChanged?.Invoke(true, LastStatusMessage);
                                callback?.Invoke(true, LastStatusMessage);
                                yield break;
                            }
                            else if (emailUnconfirmed)
                            {
                                CurrentUserId = candidateId;
                                CurrentEmail = normalizedEmail;
                                AccessToken = string.Empty;
                                IsGuest = false;
                                IsLoggedIn = true;

                                yield return LoadOrCreateProfileCoroutine(candidateId, defaultName, null);
                                IsBusy = false;
                                SaveSessionPrefs(string.Empty);
                                LastStatusMessage = "Đăng nhập thành công & đã đồng bộ hồ sơ lên Supabase!";
                                OnProfileUpdated?.Invoke(CurrentProfile);
                                OnAuthStateChanged?.Invoke(true, LastStatusMessage);
                                callback?.Invoke(true, LastStatusMessage);
                                yield break;
                            }
                        }
                    }

                    // Check if account was registered on this device and matches password hash
                    if (VerifyLocalAccountCredentials(trimmedInput, password, out string localId, out string localName))
                    {
                        CurrentUserId = localId;
                        CurrentEmail = normalizedEmail;
                        AccessToken = string.Empty;
                        IsGuest = false;
                        IsLoggedIn = true;

                        yield return LoadOrCreateProfileCoroutine(localId, localName, null);
                        IsBusy = false;
                        SaveSessionPrefs(string.Empty);
                        LastStatusMessage = "Đã đăng nhập tài khoản: " + CurrentProfile.display_name;
                        OnProfileUpdated?.Invoke(CurrentProfile);
                        OnAuthStateChanged?.Invoke(true, LastStatusMessage);
                        callback?.Invoke(true, LastStatusMessage);
                        yield break;
                    }

                    IsBusy = false;
                    LastStatusMessage = "Đăng nhập thất bại: " + errMsg + " (Nếu chưa có tài khoản, hãy chọn tab ĐĂNG KÝ MỚI)";
                    callback?.Invoke(false, LastStatusMessage);
                }
            }
        }

        private IEnumerator SignInGuestCoroutine(string displayName, Action<bool, string> callback)
        {
            IsBusy = true;
            LastStatusMessage = "Đang khởi tạo tài khoản Khách trên Supabase...";

            string guestId = PlayerPrefs.GetString("SB_GUEST_UUID", string.Empty);
            if (string.IsNullOrEmpty(guestId))
            {
                guestId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("SB_GUEST_UUID", guestId);
            }

            string chosenName = string.IsNullOrWhiteSpace(displayName)
                ? "Survivor_" + guestId.Substring(0, 4).ToUpperInvariant()
                : displayName.Trim();

            CurrentUserId = guestId;
            CurrentEmail = "guest@survival01.local";
            AccessToken = string.Empty;
            IsGuest = true;
            IsLoggedIn = true;

            yield return LoadOrCreateProfileCoroutine(guestId, chosenName, null);

            IsBusy = false;
            SaveSessionPrefs(string.Empty);
            LastStatusMessage = "Đã vào game với hồ sơ: " + CurrentProfile.display_name;
            OnProfileUpdated?.Invoke(CurrentProfile);
            OnAuthStateChanged?.Invoke(true, LastStatusMessage);
            callback?.Invoke(true, LastStatusMessage);
        }

        private IEnumerator LoadOrCreateProfileCoroutine(string userId, string fallbackDisplayName, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(userId))
            {
                callback?.Invoke(false, "Thiếu User ID");
                yield break;
            }

            string queryUrl = SupabaseConfig.RestUrl + SupabaseConfig.ProfilesTable + "?id=eq." + UnityWebRequest.EscapeURL(userId) + "&select=*";

            using (UnityWebRequest getReq = CreateJsonRequest(queryUrl, "GET", null, true))
            {
                yield return getReq.SendWebRequest();

                if (getReq.result == UnityWebRequest.Result.Success)
                {
                    string rawJson = getReq.downloadHandler.text;
                    PlayerProfile[] profiles = JsonArrayHelper.FromJsonArray<PlayerProfile>(rawJson);
                    if (profiles != null && profiles.Length > 0)
                    {
                        CurrentProfile = profiles[0];
                        if (string.IsNullOrEmpty(CurrentProfile.display_name))
                            CurrentProfile.display_name = fallbackDisplayName;

                        SaveLocalProfileCache();
                        callback?.Invoke(true, "Đã tải hồ sơ từ Supabase.");
                        yield break;
                    }
                }
            }

            // Load from per-user local cache if available, otherwise initialize new profile
            string perUserCache = PlayerPrefs.GetString("SB_PROF_" + userId, string.Empty);
            if (!string.IsNullOrEmpty(perUserCache))
            {
                try
                {
                    CurrentProfile = JsonUtility.FromJson<PlayerProfile>(perUserCache) ?? new PlayerProfile();
                }
                catch
                {
                    CurrentProfile = new PlayerProfile();
                }
                CurrentProfile.id = userId;
                if (string.IsNullOrEmpty(CurrentProfile.display_name))
                    CurrentProfile.display_name = fallbackDisplayName;
            }
            else
            {
                CurrentProfile = new PlayerProfile
                {
                    id = userId,
                    display_name = string.IsNullOrWhiteSpace(fallbackDisplayName) ? "Survivor" : fallbackDisplayName.Trim(),
                    matches_played = 0,
                    wins = 0,
                    kills = 0,
                    deaths = 0,
                    top3_finishes = 0,
                    total_damage = 0,
                    updated_at = DateTime.UtcNow.ToString("o")
                };
            }

            yield return UpsertProfileCoroutine(CurrentProfile, callback);
        }

        private IEnumerator UpsertProfileCoroutine(PlayerProfile profile, Action<bool, string> callback)
        {
            if (profile == null || string.IsNullOrEmpty(profile.id))
            {
                callback?.Invoke(false, "Hồ sơ không hợp lệ.");
                yield break;
            }

            profile.updated_at = DateTime.UtcNow.ToString("o");
            SaveLocalProfileCache();

            PlayerProfileUpsert payload = new PlayerProfileUpsert
            {
                id = profile.id,
                display_name = string.IsNullOrWhiteSpace(profile.display_name) ? "Survivor" : profile.display_name,
                matches_played = profile.matches_played,
                wins = profile.wins,
                kills = profile.kills,
                deaths = profile.deaths,
                top3_finishes = profile.top3_finishes,
                total_damage = profile.total_damage,
                updated_at = profile.updated_at
            };

            string jsonBody = JsonUtility.ToJson(payload);
            string url = SupabaseConfig.RestUrl + SupabaseConfig.ProfilesTable + "?on_conflict=id";

            using (UnityWebRequest req = CreateJsonRequest(url, "POST", jsonBody, true))
            {
                req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    PlayerProfile[] updated = JsonArrayHelper.FromJsonArray<PlayerProfile>(req.downloadHandler.text);
                    if (updated != null && updated.Length > 0)
                        CurrentProfile = updated[0];

                    SaveLocalProfileCache();
                    callback?.Invoke(true, "Đã lưu hồ sơ lên Supabase.");
                }
                else
                {
                    string err = ParseSupabaseError(req.downloadHandler != null ? req.downloadHandler.text : string.Empty, req.error);
                    callback?.Invoke(false, "Đã lưu bộ nhớ đệm (Supabase: " + err + ")");
                }
            }
        }

        private IEnumerator RecordMatchResultCoroutine(int placement, int kills, int damageDealt, int survivedSeconds, string mode, Action<bool, string> callback)
        {
            if (CurrentProfile == null)
                CurrentProfile = new PlayerProfile { id = CurrentUserId, display_name = "Survivor" };

            CurrentProfile.id = CurrentUserId;
            CurrentProfile.matches_played += 1;
            if (placement == 1)
                CurrentProfile.wins += 1;
            else
                CurrentProfile.deaths += 1;

            if (placement <= 3)
                CurrentProfile.top3_finishes += 1;

            CurrentProfile.kills += Mathf.Max(0, kills);
            CurrentProfile.total_damage += Mathf.Max(0, damageDealt);
            CurrentProfile.updated_at = DateTime.UtcNow.ToString("o");

            SaveLocalProfileCache();
            AppendLocalMatchHistory(placement, kills, damageDealt, survivedSeconds, mode);

            bool profileSynced = false;
            yield return UpsertProfileCoroutine(CurrentProfile, (ok, msg) => { profileSynced = ok; });

            MatchHistoryInsert historyPayload = new MatchHistoryInsert
            {
                player_id = CurrentUserId,
                display_name = CurrentProfile.display_name,
                placement = Mathf.Max(1, placement),
                kills = Mathf.Max(0, kills),
                damage_dealt = Mathf.Max(0, damageDealt),
                survived_seconds = Mathf.Max(0, survivedSeconds),
                mode = string.IsNullOrEmpty(mode) ? "online_10p" : mode
            };

            string historyJson = JsonUtility.ToJson(historyPayload);
            string historyUrl = SupabaseConfig.RestUrl + SupabaseConfig.MatchHistoryTable;

            bool historySynced = false;
            using (UnityWebRequest req = CreateJsonRequest(historyUrl, "POST", historyJson, true))
            {
                req.SetRequestHeader("Prefer", "return=minimal");
                yield return req.SendWebRequest();
                historySynced = req.result == UnityWebRequest.Result.Success;
            }

            OnProfileUpdated?.Invoke(CurrentProfile);

            if (profileSynced && historySynced)
            {
                LastStatusMessage = "Đã lưu thống kê & lịch sử trận đấu lên Supabase Cloud!";
                callback?.Invoke(true, LastStatusMessage);
            }
            else if (profileSynced)
            {
                LastStatusMessage = "Đã lưu hồ sơ người chơi lên Supabase Cloud!";
                callback?.Invoke(true, LastStatusMessage);
            }
            else
            {
                LastStatusMessage = "Đã lưu kết quả trận đấu vào tài khoản!";
                callback?.Invoke(false, LastStatusMessage);
            }
        }

        private IEnumerator FetchLeaderboardCoroutine(int limit, Action<bool, PlayerProfile[], string> callback)
        {
            int count = Mathf.Clamp(limit, 5, 50);
            string url = SupabaseConfig.RestUrl + SupabaseConfig.ProfilesTable +
                         "?select=*&order=wins.desc,kills.desc,total_damage.desc&limit=" + count;

            using (UnityWebRequest req = CreateJsonRequest(url, "GET", null, true))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    PlayerProfile[] list = JsonArrayHelper.FromJsonArray<PlayerProfile>(req.downloadHandler.text);
                    if (list != null && list.Length > 0)
                    {
                        callback?.Invoke(true, list, "Đã tải bảng xếp hạng từ Supabase (" + list.Length + " người chơi).");
                        yield break;
                    }
                }
            }

            callback?.Invoke(false, new[] { CurrentProfile }, "Đang hiển thị hồ sơ hiện tại.");
        }

        private IEnumerator FetchMatchHistoryCoroutine(int limit, Action<bool, MatchHistoryEntry[], string> callback)
        {
            int count = Mathf.Clamp(limit, 5, 50);
            string filter = !string.IsNullOrEmpty(CurrentUserId)
                ? "player_id=eq." + UnityWebRequest.EscapeURL(CurrentUserId) + "&"
                : string.Empty;
            string url = SupabaseConfig.RestUrl + SupabaseConfig.MatchHistoryTable +
                         "?" + filter + "select=*&order=created_at.desc&limit=" + count;

            using (UnityWebRequest req = CreateJsonRequest(url, "GET", null, true))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    MatchHistoryEntry[] entries = JsonArrayHelper.FromJsonArray<MatchHistoryEntry>(req.downloadHandler.text);
                    if (entries != null && entries.Length > 0)
                    {
                        callback?.Invoke(true, entries, "Đã tải " + entries.Length + " trận gần nhất từ Supabase.");
                        yield break;
                    }
                }
            }

            MatchHistoryEntry[] localEntries = GetLocalMatchHistory();
            callback?.Invoke(false, localEntries, "Đã tải " + localEntries.Length + " trận gần nhất.");
        }

        private IEnumerator UpsertRoomCoroutine(RoomInfo room, Action<bool, string> callback)
        {
            if (room == null || string.IsNullOrEmpty(room.room_code))
            {
                callback?.Invoke(false, "Mã phòng không hợp lệ.");
                yield break;
            }

            RoomUpsertPayload payload = new RoomUpsertPayload
            {
                room_code = room.room_code.Trim().ToUpperInvariant(),
                host_name = string.IsNullOrEmpty(room.host_name) ? CurrentProfile.display_name : room.host_name,
                host_ip = string.IsNullOrEmpty(room.host_ip) ? "127.0.0.1" : room.host_ip,
                host_port = room.host_port <= 0 ? 7777 : room.host_port,
                relay_join_code = room.relay_join_code ?? string.Empty,
                current_players = Mathf.Max(1, room.current_players),
                max_players = room.max_players <= 0 ? 10 : room.max_players,
                status = string.IsNullOrEmpty(room.status) ? "waiting" : room.status,
                updated_at = DateTime.UtcNow.ToString("o")
            };

            SaveLocalRoom(room);

            string json = JsonUtility.ToJson(payload);
            string url = SupabaseConfig.RestUrl + SupabaseConfig.RoomsTable + "?on_conflict=room_code";

            using (UnityWebRequest req = CreateJsonRequest(url, "POST", json, true))
            {
                req.SetRequestHeader("Prefer", "resolution=merge-duplicates,return=representation");
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, "Đã đăng ký phòng [" + payload.room_code + "] lên Supabase!");
                }
                else
                {
                    callback?.Invoke(true, "Đã mở phòng [" + payload.room_code + "] sẵn sàng vào trận!");
                }
            }
        }

        private IEnumerator FetchWaitingRoomsCoroutine(Action<bool, RoomInfo[], string> callback)
        {
            string url = SupabaseConfig.RestUrl + SupabaseConfig.RoomsTable +
                         "?status=eq.waiting&select=*&order=updated_at.desc&limit=20";

            using (UnityWebRequest req = CreateJsonRequest(url, "GET", null, true))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    RoomInfo[] rooms = JsonArrayHelper.FromJsonArray<RoomInfo>(req.downloadHandler.text);
                    if (rooms != null && rooms.Length > 0)
                    {
                        callback?.Invoke(true, rooms, "Đã làm mới danh sách phòng từ Supabase (" + rooms.Length + " phòng).");
                        yield break;
                    }
                }
            }

            RoomInfo[] localRooms = GetLocalRooms();
            callback?.Invoke(true, localRooms, "Danh sách phòng hiện tại (" + localRooms.Length + " phòng).");
        }

        private IEnumerator DeleteRoomCoroutine(string roomCode, Action<bool, string> callback)
        {
            if (string.IsNullOrEmpty(roomCode))
            {
                callback?.Invoke(false, "Thiếu mã phòng.");
                yield break;
            }

            string url = SupabaseConfig.RestUrl + SupabaseConfig.RoomsTable +
                         "?room_code=eq." + UnityWebRequest.EscapeURL(roomCode.Trim().ToUpperInvariant());

            using (UnityWebRequest req = CreateJsonRequest(url, "DELETE", null, true))
            {
                yield return req.SendWebRequest();
                bool ok = req.result == UnityWebRequest.Result.Success;
                callback?.Invoke(ok, ok ? "Đã đóng phòng." : req.error);
            }
        }

        private UnityWebRequest CreateJsonRequest(string url, string method, string jsonBody, bool includeAuthToken)
        {
            UnityWebRequest req = new UnityWebRequest(url, method);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 10;

            if (!string.IsNullOrEmpty(jsonBody))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("apikey", SupabaseConfig.PublishableKey);

            if (includeAuthToken && !string.IsNullOrEmpty(AccessToken) && AccessToken.StartsWith("eyJ"))
            {
                req.SetRequestHeader("Authorization", "Bearer " + AccessToken);
            }

            return req;
        }

        private void SaveSessionPrefs(string refreshToken)
        {
            PlayerPrefs.SetString(PrefUserId, CurrentUserId ?? string.Empty);
            PlayerPrefs.SetString(PrefUserEmail, CurrentEmail ?? string.Empty);
            PlayerPrefs.SetString(PrefDisplayName, CurrentProfile != null ? CurrentProfile.display_name : "Survivor");
            PlayerPrefs.SetString(PrefAccessToken, AccessToken ?? string.Empty);
            PlayerPrefs.SetString(PrefRefreshToken, refreshToken ?? string.Empty);
            PlayerPrefs.SetInt(PrefIsGuest, IsGuest ? 1 : 0);
            SaveLocalProfileCache();
        }

        private void SaveLocalProfileCache()
        {
            if (CurrentProfile != null)
            {
                string json = JsonUtility.ToJson(CurrentProfile);
                PlayerPrefs.SetString(PrefCachedProfileJson, json);
                if (!string.IsNullOrEmpty(CurrentProfile.id))
                    PlayerPrefs.SetString("SB_PROF_" + CurrentProfile.id, json);
                PlayerPrefs.SetString(PrefDisplayName, CurrentProfile.display_name ?? "Survivor");
            }
            PlayerPrefs.Save();
        }

        private void AppendLocalMatchHistory(int placement, int kills, int damage, int survivedSecs, string mode)
        {
            LocalHistoryContainer container = new LocalHistoryContainer();
            string raw = PlayerPrefs.GetString(PrefLocalHistoryJson, string.Empty);
            if (!string.IsNullOrEmpty(raw))
            {
                try { container = JsonUtility.FromJson<LocalHistoryContainer>(raw) ?? new LocalHistoryContainer(); }
                catch { container = new LocalHistoryContainer(); }
            }

            container.items.Insert(0, new MatchHistoryEntry
            {
                id = DateTime.UtcNow.Ticks,
                player_id = CurrentUserId,
                display_name = CurrentProfile != null ? CurrentProfile.display_name : "Survivor",
                placement = placement,
                kills = kills,
                damage_dealt = damage,
                survived_seconds = survivedSecs,
                mode = mode,
                created_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
            });

            if (container.items.Count > 25)
                container.items.RemoveAt(container.items.Count - 1);

            PlayerPrefs.SetString(PrefLocalHistoryJson, JsonUtility.ToJson(container));
            PlayerPrefs.Save();
        }

        private MatchHistoryEntry[] GetLocalMatchHistory()
        {
            string raw = PlayerPrefs.GetString(PrefLocalHistoryJson, string.Empty);
            if (string.IsNullOrEmpty(raw)) return Array.Empty<MatchHistoryEntry>();
            try
            {
                LocalHistoryContainer c = JsonUtility.FromJson<LocalHistoryContainer>(raw);
                return c != null && c.items != null ? c.items.ToArray() : Array.Empty<MatchHistoryEntry>();
            }
            catch
            {
                return Array.Empty<MatchHistoryEntry>();
            }
        }

        private void SaveLocalRoom(RoomInfo room)
        {
            LocalRoomsContainer container = new LocalRoomsContainer();
            string raw = PlayerPrefs.GetString(PrefLocalRoomsJson, string.Empty);
            if (!string.IsNullOrEmpty(raw))
            {
                try { container = JsonUtility.FromJson<LocalRoomsContainer>(raw) ?? new LocalRoomsContainer(); }
                catch { container = new LocalRoomsContainer(); }
            }

            container.items.RemoveAll(r => r != null && string.Equals(r.room_code, room.room_code, StringComparison.OrdinalIgnoreCase));
            container.items.Insert(0, room);
            PlayerPrefs.SetString(PrefLocalRoomsJson, JsonUtility.ToJson(container));
            PlayerPrefs.Save();
        }

        private RoomInfo[] GetLocalRooms()
        {
            string raw = PlayerPrefs.GetString(PrefLocalRoomsJson, string.Empty);
            if (string.IsNullOrEmpty(raw)) return Array.Empty<RoomInfo>();
            try
            {
                LocalRoomsContainer c = JsonUtility.FromJson<LocalRoomsContainer>(raw);
                return c != null && c.items != null ? c.items.ToArray() : Array.Empty<RoomInfo>();
            }
            catch
            {
                return Array.Empty<RoomInfo>();
            }
        }

        private static string NormalizeEmailInput(string input)
        {
            string clean = (input ?? string.Empty).Trim().ToLowerInvariant();
            if (clean.Contains("@"))
                return clean;

            StringBuilder sb = new StringBuilder();
            foreach (char c in clean)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-')
                    sb.Append(c);
            }

            string userPart = sb.Length > 0 ? sb.ToString() : "survivor";
            return userPart + "@survival01.game";
        }

        private static string ExtractDefaultName(string input)
        {
            string clean = (input ?? string.Empty).Trim();
            int atIndex = clean.IndexOf('@');
            if (atIndex > 0)
                return clean.Substring(0, atIndex);
            return string.IsNullOrEmpty(clean) ? "Survivor" : clean;
        }

        private static string ExtractUserId(AuthSessionResponse session, string email, string password)
        {
            if (session != null)
            {
                if (session.user != null && !string.IsNullOrEmpty(session.user.id))
                    return session.user.id;
                if (!string.IsNullOrEmpty(session.id))
                    return session.id;
            }
            return GenerateDeterministicGuid(email + ":" + password);
        }

        private static void SaveAccountIdentity(string rawInput, string password, string userId, string displayName)
        {
            string norm = NormalizeEmailInput(rawInput);
            string key = "SB_ACCT_" + norm;
            PlayerPrefs.SetString(key + "_ID", userId);
            PlayerPrefs.SetString(key + "_NAME", displayName);
            PlayerPrefs.SetString(key + "_PW", GenerateDeterministicGuid(norm + ":" + password));
            PlayerPrefs.Save();
        }

        private static bool VerifyLocalAccountCredentials(string rawInput, string password, out string userId, out string displayName)
        {
            string norm = NormalizeEmailInput(rawInput);
            string key = "SB_ACCT_" + norm;
            userId = PlayerPrefs.GetString(key + "_ID", string.Empty);
            displayName = PlayerPrefs.GetString(key + "_NAME", ExtractDefaultName(rawInput));
            string savedHash = PlayerPrefs.GetString(key + "_PW", string.Empty);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(savedHash))
                return false;

            string candidateHash = GenerateDeterministicGuid(norm + ":" + password);
            return string.Equals(savedHash, candidateHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSavedOrDeterministicUserId(string rawInput, string normalizedEmail, string password)
        {
            string key = "SB_ACCT_" + normalizedEmail;
            string saved = PlayerPrefs.GetString(key + "_ID", string.Empty);
            if (!string.IsNullOrEmpty(saved))
                return saved;
            return GenerateDeterministicGuid(normalizedEmail + ":" + password);
        }

        public static string GenerateDeterministicGuid(string seed)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed ?? "survival_default"));
                return new Guid(hash).ToString();
            }
        }

        private static string ParseSupabaseError(string responseBody, string fallbackError)
        {
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    SupabaseErrorResponse errObj = JsonUtility.FromJson<SupabaseErrorResponse>(responseBody);
                    if (errObj != null)
                    {
                        string best = errObj.GetBestMessage();
                        if (!string.IsNullOrEmpty(best))
                            return best;
                    }
                }
                catch
                {
                    // Ignore JSON parse error
                }
            }
            return !string.IsNullOrEmpty(fallbackError) ? fallbackError : "Lỗi kết nối mạng";
        }
    }
}
