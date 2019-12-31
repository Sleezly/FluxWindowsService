using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace HueController
{
    public class LightConfig
    {
        [DataMember]
        public string Name;

        [DataMember]
        public bool ControlBrightness;

        [DataMember]
        public bool ControlTemperature;

        /// <summary>
        /// JSON filename.
        /// </summary>
        private static readonly string JsonFilename = @"Config\LightConfig.json";

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<LightConfig> DeserializeLightObjectGraph()
        {
            StreamReader reader = new StreamReader(JsonFilename);
            string config = reader.ReadToEndAsync().GetAwaiter().GetResult();

            List<LightConfig> lightEntities = new List<LightConfig>();

            JEnumerable<JToken> lightTokens = JsonConvert.DeserializeObject<JObject>(config)["Lights"].Children();
            foreach (JToken lightToken in lightTokens)
            {
                lightEntities.Add(JsonConvert.DeserializeObject<LightConfig>(lightToken.ToString()));
            }

            return lightEntities;
        }
    }
}
