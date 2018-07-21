using System.Runtime.Serialization;

namespace FluxService
{
    [DataContract]
    public class HuePost
    {
        [DataMember]
        public string On;

        [DataMember]
        public double LightLevel;
    }
}
