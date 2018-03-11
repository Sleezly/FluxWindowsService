using System;
using System.Runtime.Serialization;

namespace HueController
{
    [DataContract]
    public class FluxStatus
    {
        [DataMember]
        public int FluxColorTemperature;

        [DataMember]
        public int StopColorTemperature;

        [DataMember]
        public int SunriseColorTemperature;

        [DataMember]
        public int SunsetColorTemperature;

        [DataMember]
        public int SolarNoonTemperature;

        [DataMember]
        public DateTime StopTime;

        [DataMember]
        public DateTime Sunrise;

        [DataMember]
        public DateTime Sunset;

        [DataMember]
        public DateTime SolarNoon;
    }
}