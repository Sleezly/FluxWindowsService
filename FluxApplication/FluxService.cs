using HueController;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Fluxer
{
    public class FluxService
    {
        private const string baseAddress = "http://localhost:51234/api/flux";

        public static HueDetails GetHueData()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseAddress);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response.
            HttpResponseMessage response = client.GetAsync(baseAddress).Result;

            if (response.IsSuccessStatusCode)
            {
                string hueStatus = response.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<HueDetails>(hueStatus);

            }

            return null;
        }
    }
}
