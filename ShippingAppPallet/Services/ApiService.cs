using System;
using System.Net.Http;

namespace ShippingAppPallet
{
    public class ApiService : IDisposable
    {
        private static readonly HttpClient _http = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private readonly string _soUrl = System.Configuration.ConfigurationManager.AppSettings["DataSoUrl"];
        private readonly string _fgUrl = System.Configuration.ConfigurationManager.AppSettings["DataPickListUrl"];

        public string GetSoJson()
        {
            var resp = _http.GetAsync(_soUrl).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsStringAsync().Result;
        }

        public string GetFgJson()
        {
            var resp = _http.GetAsync(_fgUrl).Result;
            resp.EnsureSuccessStatusCode();
            return resp.Content.ReadAsStringAsync().Result;
        }

        public void Dispose() { /* HttpClient static - do nothing */ }
    }
}
