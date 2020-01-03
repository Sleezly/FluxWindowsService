using System.Runtime.Serialization;

namespace FluxService
{
    [DataContract]
    public class FluxStatus
    {
        [DataMember]
        public byte Brightness;

        [DataMember]
        public int ColorTemperature;

        public FluxStatus(byte brightness, int colorTemperatere)
        {
            Brightness = brightness;
            ColorTemperature = colorTemperatere;
        }
    }
}
