using HueController;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Fluxer
{
    public class FluxService
    {
        private const string baseAddress = "http://localhost:51234/api/flux";

        public static async Task<HueStatus> GetHueData()
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            HttpResponseMessage response = await client.GetAsync(baseAddress);

            if (response.IsSuccessStatusCode)
            {
                string hueStatus = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<HueStatus>(hueStatus);

            }

            return null;
        }
    }
}
