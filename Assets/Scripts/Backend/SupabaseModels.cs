using System;
using UnityEngine;

namespace Survival.Backend
{
    [Serializable]
    public class AuthUserMetadata
    {
        public string display_name;
    }

    [Serializable]
    public class AuthSignupPayload
    {
        public string email;
        public string password;
        public AuthUserMetadata data;
    }

    [Serializable]
    public class AuthLoginPayload
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class AuthUserObject
    {
        public string id;
        public string email;
        public AuthUserMetadata user_metadata;
    }

    [Serializable]
    public class AuthSessionResponse
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
        public AuthUserObject user;

        // Fallback fields when Supabase returns user object directly at root (e.g. signup with email confirmation enabled)
        public string id;
        public string email;
        public AuthUserMetadata user_metadata;
    }

    [Serializable]
    public class SupabaseErrorResponse
    {
        public string error;
        public string error_description;
        public string msg;
        public string message;
        public string code;

        public string GetBestMessage()
        {
            if (!string.IsNullOrEmpty(error_description)) return error_description;
            if (!string.IsNullOrEmpty(msg)) return msg;
            if (!string.IsNullOrEmpty(message)) return message;
            if (!string.IsNullOrEmpty(error)) return error;
            return string.Empty;
        }
    }

    [Serializable]
    public class PlayerProfile
    {
        public string id;
        public string display_name = "Survivor";
        public int matches_played;
        public int wins;
        public int kills;
        public int deaths;
        public int top3_finishes;
        public int total_damage;
        public string updated_at;

        public float GetKDRatio()
        {
            if (deaths <= 0) return kills;
            return (float)Math.Round((double)kills / deaths, 2);
        }

        public float GetWinRatePercent()
        {
            if (matches_played <= 0) return 0f;
            return (float)Math.Round((double)wins * 100.0 / matches_played, 1);
        }
    }

    [Serializable]
    public class PlayerProfileUpsert
    {
        public string id;
        public string display_name;
        public int matches_played;
        public int wins;
        public int kills;
        public int deaths;
        public int top3_finishes;
        public int total_damage;
        public string updated_at;
    }

    [Serializable]
    public class MatchHistoryEntry
    {
        public long id;
        public string player_id;
        public string display_name;
        public int placement;
        public int kills;
        public int damage_dealt;
        public int survived_seconds;
        public string mode;
        public string created_at;
    }

    [Serializable]
    public class MatchHistoryInsert
    {
        public string player_id;
        public string display_name;
        public int placement;
        public int kills;
        public int damage_dealt;
        public int survived_seconds;
        public string mode;
    }

    [Serializable]
    public class RoomInfo
    {
        public string room_code;
        public string host_name;
        public string host_ip;
        public int host_port;
        public string relay_join_code;
        public int current_players;
        public int max_players;
        public string status;
        public string updated_at;
    }

    [Serializable]
    public class RoomUpsertPayload
    {
        public string room_code;
        public string host_name;
        public string host_ip;
        public int host_port;
        public string relay_join_code;
        public int current_players;
        public int max_players;
        public string status;
        public string updated_at;
    }

    public static class JsonArrayHelper
    {
        [Serializable]
        private class ArrayWrapper<T>
        {
            public T[] items = Array.Empty<T>();
        }

        public static T[] FromJsonArray<T>(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return Array.Empty<T>();

            string trimmed = rawJson.Trim();
            if (!trimmed.StartsWith("["))
                return Array.Empty<T>();

            string wrapped = "{\"items\":" + trimmed + "}";
            try
            {
                ArrayWrapper<T> result = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
                return result != null && result.items != null ? result.items : Array.Empty<T>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Supabase] Failed to parse JSON array: " + ex.Message);
                return Array.Empty<T>();
            }
        }
    }
}
