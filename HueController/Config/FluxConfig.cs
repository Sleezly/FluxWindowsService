using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace HueController
{
    [DataContract]
    public class FluxConfig
    {
        /// <summary>
        /// Bridge connection info
        /// </summary>
        [DataMember]
        public Dictionary<string /* bridgeIp */, string /* connectionSecret */> BridgeDetails;

        /// <summary>
        /// Times
        /// </summary>
        [DataMember]
        public TimeSpan StopTime;

        /// <summary>
        /// Location used to determine sunrise / sunset values
        /// </summary>
        [DataMember]
        public double Latitude;

        [DataMember]
        public double Longitude;

        /// <summary>
        /// Color Temperatures
        /// </summary>
        [DataMember]
        public int SunriseColorTemperature;

        [DataMember]
        public int SolarNoonTemperature;

        [DataMember]
        public int SunsetColorTemperature;

        [DataMember]
        public int StopColorTemperature;

        /// <summary>
        /// JSON filename.
        /// </summary>
        private static readonly string JsonFilename = @"Config\FluxConfig.json";

        /// <summary>
        /// Load the configuration file.
        /// </summary>
        /// <returns></returns>
        public static FluxConfig ParseConfig()
        {
            StreamReader reader = new StreamReader(JsonFilename);
            string config = reader.ReadToEndAsync().GetAwaiter().GetResult();
            return JsonConvert.DeserializeObject<FluxConfig>(config);
        }
    }
}
