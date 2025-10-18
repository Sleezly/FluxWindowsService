using HueApi.Models;

namespace HueController
{
    public static class LightExtensions
    {
        private static readonly IEnumerable<LightConfig> LightConfigs = LightConfig.DeserializeLightObjectGraph();

        internal static IDictionary<LightType, string> LightTypeToNameMapping = new Dictionary<LightType, string>()
        {
            { LightType.WhiteOnly, "Dimmable light" },
            { LightType.WhiteAmbiance, "Color temperature light" },
            { LightType.Color, "Extended color light" },
        };

        public static bool SupportsColorOrTemperatureChange(this Light light)
        {
            return LightType.WhiteOnly != light.GetLightType();
        }

        public static bool IsFluxControlled(this Light light)
        {
            return light.ControlBrightness() || light.ControlTemperature();
        }

        public static bool ControlBrightness(this Light light)
        {
            return light.ToLightConfig().ControlBrightness;
        }

        public static bool ControlTemperature(this Light light)
        {
            return light.ToLightConfig().ControlTemperature;
        }

        private static LightConfig ToLightConfig(this Light light)
        {
            return LightConfigs.SingleOrDefault(lightEntityDetail =>
               lightEntityDetail.Name.Equals(light.Metadata.Name, StringComparison.OrdinalIgnoreCase)) ??
                new LightConfig()
                {
                    Name = light.Metadata.Name,
                    ControlTemperature = true,
                    ControlBrightness = true,
                };
        }

        public static Light Get(this IEnumerable<Light> lights, Guid id)
        {
            return lights.SingleOrDefault(light => light.Id == id);
        }

        /// <summary>
        /// Converts a string to a light bulb type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static LightType GetLightType(this Light light)
        {
            return LightTypeToNameMapping.Single(x => string.Equals(light.Type, x.Value, StringComparison.OrdinalIgnoreCase)).Key;
        }
    }
}
