using System;
using System.Runtime.Serialization;

namespace HueController
{
    [DataContract]
    public class HueStatus
    {
        [DataMember]
        public FluxStatus FluxStatus;

        [DataMember]
        public bool On;

        [DataMember]
        public int? LastColorTemperature;

        [DataMember]
        public int? LastBrightness;

        [DataMember]
        public double? LastLightlevel;

        [DataMember]
        public TimeSpan? CurrentSleepDuration;

        [DataMember]
        public DateTime? CurrentWakeCycle;
    }
}
