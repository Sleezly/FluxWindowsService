using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using static HueController.LightDetails;

namespace HueController
{
    public static class LightExtensions
    {

        internal static IDictionary<LightType, string> LightTypeToNameMapping = new Dictionary<LightType, string>()
        {
            { LightDetails.LightType.WhiteOnly, "Dimmable light" },
            { LightDetails.LightType.WhiteAmbiance, "Color temperature light" },
            { LightDetails.LightType.Color, "Extended color light" },
        };

        public static bool SupportsColorOrTemperatureChange(this Light light)
        {
            return LightDetails.LightType.WhiteOnly != light.LightType();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static int? NormalizeColorForAllowedColorRange(this Light light, int color)
        {
            return LightDetails.NormalizeColorForAllowedColorRange(light.LightType(), color);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool IsInAllowedColorRange(this Light light, int color)
        {
            switch (light.LightType())
            {
                case LightDetails.LightType.Color:
                    return color <= MaxAllowedColorTemperatureForWhiteAndColorLights && color >= MinAllowedColorTemperatureForWhiteAndColorLights;

                case LightDetails.LightType.WhiteAmbiance:
                    return color <= MaxAllowedColorTemperatureForWhiteAmbianceLights && color >= MinAllowedColorTemperatureForWhiteAndColorLights;

                case LightDetails.LightType.WhiteOnly:
                    return false;

                default:
                    throw new ArgumentException($"Unsupported light type '{light.Type}'.");
            }
        }

        /// <summary>
        /// Converts a string to a light bulb type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static LightType LightType(this Light light)
        {
            return LightTypeToNameMapping.Single(x => string.Equals(light.Type, x.Value, StringComparison.OrdinalIgnoreCase)).Key;
        }
    }
}
