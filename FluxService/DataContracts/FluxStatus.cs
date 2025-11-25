using System.Runtime.Serialization;

namespace FluxService
{
    [DataContract]
    public class FluxStatus
    {
        [DataMember]
        public int ColorTemperature;

        public FluxStatus(int colorTemperatere)
        {
            ColorTemperature = colorTemperatere;
        }
    }
}
