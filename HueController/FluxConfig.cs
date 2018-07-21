using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace HueController
{
    [DataContract]
    public class FluxConfig
    {
        /// <summary>
        /// Default states
        /// </summary>
        [DataMember]
        public TimeSpan LightTransitionDuration;

        /// <summary>
        /// Bridge connection info
        /// </summary>
        [DataMember]
        public Dictionary<string /* bridgeId */, string /* connectionSecret */> BridgeIds;

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
        /// Brightness
        /// </summary>
        [DataMember]
        public double MaxLightLevel;

        [DataMember]
        public double MinLightLevel;

        [DataMember]
        public byte MaxBrightness;

        [DataMember]
        public byte MinBrightness;

        /// <summary>
        /// Load the configuration file.
        /// </summary>
        /// <returns></returns>
        public static FluxConfig ParseConfig()
        {
            StreamReader reader = new StreamReader($"FluxConfig.json");
            string config = reader.ReadToEndAsync().Result;
            return JsonConvert.DeserializeObject<FluxConfig>(config);
        }
    }
}
