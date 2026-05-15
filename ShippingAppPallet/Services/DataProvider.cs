using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Caching;
using Newtonsoft.Json.Linq;
using System.Globalization;
using ShippingAppPallet;
using System.Data.SqlClient;

namespace ShippingAppPallet
{
    public class DataProvider
    {
        private readonly string FieldBP = ConfigurationManager.AppSettings["Field_BPCode"] ?? "bp";
        private readonly string FieldItem = ConfigurationManager.AppSettings["Field_Item"] ?? "item";
        private readonly string FieldDesc = ConfigurationManager.AppSettings["Field_Description"] ?? "description";
        private readonly string FieldSO = ConfigurationManager.AppSettings["Field_SO"] ?? "sales Order";
        private readonly string FieldPrice = ConfigurationManager.AppSettings["Field_SalesPrice"] ?? "sales Price";
        private readonly string FieldQtyOrdered = ConfigurationManager.AppSettings["Field_QtyOrdered"] ?? "qty Ordered";
        private readonly string FieldPosition = ConfigurationManager.AppSettings["Field_Position"] ?? "position";
        private readonly string FieldSequence = ConfigurationManager.AppSettings["Field_Sequence"] ?? "sequence";

        private readonly int CacheSmallMinutes;
        private readonly int CacheMedMinutes;

        public DataProvider()
        {
            int s;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes_Small"], out s))
                s = 1;
            CacheSmallMinutes = s;

            int m;
            if (!int.TryParse(ConfigurationManager.AppSettings["CacheMinutes_Medium"], out m))
                m = 5;
            CacheMedMinutes = m;
        }

        // ===== CACHE HELPERS (HttpRuntime.Cache) =====
        private const string SO_CACHE_KEY = "SO_JSON_ARRAY";
        private const string FG_CACHE_KEY = "FG_JSON_ARRAY";

        private static JArray GetFromCache(string key)
        {
            return HttpRuntime.Cache[key] as JArray;
        }

        private static void SetToCache(string key, JArray value, int minutes)
        {
            HttpRuntime.Cache.Insert(
                key,
                value,
                dependencies: null,
                absoluteExpiration: DateTime.UtcNow.AddMinutes(minutes),
                slidingExpiration: Cache.NoSlidingExpiration,
                priority: CacheItemPriority.Default,
                onRemoveCallback: null
            );
        }

        private JArray GetSoArray()
        {
            var cached = GetFromCache(SO_CACHE_KEY);
            if (cached != null) return cached;

            using (var api = new ApiService())
            {
                var json = api.GetSoJson();
                var arr = ParseToArray(json);
                SetToCache(SO_CACHE_KEY, arr, CacheMedMinutes);
                return arr;
            }
        }

        private JArray GetFgArray() // reserved for future usage
        {
            var cached = GetFromCache(FG_CACHE_KEY);
            if (cached != null) return cached;

            using (var api = new ApiService())
            {
                var json = api.GetFgJson();
                var arr = ParseToArray(json);
                SetToCache(FG_CACHE_KEY, arr, CacheMedMinutes);
                return arr;
            }
        }

        private static JArray ParseToArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new JArray();
            var token = JToken.Parse(json);
            if (token is JArray ja) return ja;
            if (token is JObject jo)
            {
                // menangani { "data": [ ... ] } atau struktur lain yang membungkus array
                var firstArrayProp = jo.Properties().Select(p => p.Value).FirstOrDefault(v => v is JArray) as JArray;
                return firstArrayProp ?? new JArray();
            }
            return new JArray();
        }

        // ===== Public APIs dipakai PageMethods =====

        /// <summary>
        /// Ambil distinct BP code dari SO (sementara, hingga ada master BP).
        /// </summary>
        public List<string> GetDistinctBPCodes(string term)
        {
            var list = new List<string>();
            string connStr = ConfigurationManager.ConnectionStrings["ShippingConnection"].ConnectionString;

            using (var con = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(
                @"SELECT DISTINCT BPCode 
                FROM ProjectSite 
                WHERE BPCode LIKE @term + '%'
                ORDER BY BPCode", con))
            {
                cmd.Parameters.AddWithValue("@term", term ?? "");
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(r.GetString(0));
                }
            }
            return list;
        }

        /// <summary>
        /// Ambil list Item untuk BP tertentu dari SO.
        /// </summary>
        public List<string> GetItemsForBP(string bpCode, string term)
        {
            if (string.IsNullOrWhiteSpace(bpCode)) return new List<string>();

            var so = GetSoArray();
            var items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in so.Where(r => (r[FieldBP]?.ToString() ?? "").Trim()
                                              .Equals(bpCode.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                var it = row[FieldItem]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(it)) items.Add(it);
            }

            return items
                .Where(i => string.IsNullOrEmpty(term) || i.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(i => i)
                .Take(200)
                .ToList();
        }

        /// <summary>
        /// Ambil description & sales price untuk BP + Item (dari SO).
        /// </summary>
        public ItemInfo GetItemInfo(string bpCode, string item)
        {
            var info = new ItemInfo();
            if (string.IsNullOrWhiteSpace(bpCode) || string.IsNullOrWhiteSpace(item)) return info;

            var so = GetSoArray()
                .Where(r =>
                    (r[FieldBP]?.ToString() ?? "").Trim().Equals(bpCode.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    (r[FieldItem]?.ToString() ?? "").Trim().Equals(item.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            var hit = so.FirstOrDefault();
            if (hit != null)
            {
                info.Description = hit[FieldDesc]?.ToString();
                info.SalesPrice = ParseCurrencyToDecimal(hit[FieldPrice]?.ToString());
            }

            return info;
        }

        private static decimal? ParseCurrencyToDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            // Contoh: "$13.08" → 13.08
            var cleaned = s.Replace("$", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            if (decimal.TryParse(cleaned, out d)) return d;
            return null;
        }
    }
}
