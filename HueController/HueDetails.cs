using System;
using System.Runtime.Serialization;

namespace HueController
{
    [DataContract]
    public class HueDetails
    {
        [DataMember]
        public FluxStatus FluxStatus;

        [DataMember]
        public Boolean On;

        [DataMember]
        public TimeSpan CurrentSleepDuration;

        [DataMember]
        public DateTime CurrentWakeCycle;
    }
}
