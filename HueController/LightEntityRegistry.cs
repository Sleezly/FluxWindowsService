using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace HueController
{
    public class LightEntityRegistry
    {
        [DataMember]
        public string Name;

        [DataMember]
        public bool ControlBrightness;

        [DataMember]
        public bool ControlTemperature;

        /// <summary>
        /// JSON filename
        /// </summary>
        private readonly static string LightEntityRegistryFilename = "LightEntityRegistry.json";
       
        /// <summary>
        /// Logging
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<LightEntityRegistry> DeserializeLightObjectGraph()
        {
            StreamReader reader = new StreamReader(LightEntityRegistryFilename);
            string config = reader.ReadToEndAsync().Result;

            List<LightEntityRegistry> lightEntities = new List<LightEntityRegistry>();

            JEnumerable<JToken> lightTokens = JsonConvert.DeserializeObject<JObject>(config)["Lights"].Children();
            foreach (JToken lightToken in lightTokens)
            {
                LightEntityRegistry lightEntity = JsonConvert.DeserializeObject<LightEntityRegistry>(lightToken.ToString());

                lightEntities.Add(lightEntity);
            }

            return lightEntities;
        }
    }
}
