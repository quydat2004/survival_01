using UnityEngine;

namespace Survival.Backend
{
    /// <summary>
    /// Supabase project settings. Only the publishable key lives in the client.
    /// Never put a secret or service_role key here. Enable RLS on every table.
    /// </summary>
    public static class SupabaseConfig
    {
        public const string NEXT_PUBLIC_SUPABASE_URL = "https://ganxpdiwtwtwpvdgodbi.supabase.co";
        public const string NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY = "sb_publishable_bDXQCIvFgdWJ_tr_25t4ag_z8Hnos0J";

        public const string Url = NEXT_PUBLIC_SUPABASE_URL;
        public const string PublishableKey = NEXT_PUBLIC_SUPABASE_PUBLISHABLE_KEY;

        public static string RestUrl => Url + "/rest/v1/";
        public static string AuthUrl => Url + "/auth/v1/";

        public const string ProfilesTable = "profiles";
        public const string MatchHistoryTable = "match_history";
        public const string RoomsTable = "rooms";
    }
}
