using HueController.Primitives;
using System;

namespace HueController
{
    public static class ColorTemperatureExtensions
    {
        /// <summary>
        /// Maximum allow color temperature for white ambiance lights
        /// </summary>
        internal static ColorTemperature MaxAllowedColorTemperatureForWhiteAmbianceLights => 454;

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        internal static ColorTemperature MaxAllowedColorTemperatureForWhiteAndColorLights => 500;

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        internal static ColorTemperature MinAllowedColorTemperatureForWhiteAndColorLights => 154;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool IsInAllowedColorRange(this ColorTemperature colorTemperature, LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Color:
                    return colorTemperature <= MaxAllowedColorTemperatureForWhiteAndColorLights && colorTemperature >= MinAllowedColorTemperatureForWhiteAndColorLights;

                case LightType.WhiteAmbiance:
                    return colorTemperature <= MaxAllowedColorTemperatureForWhiteAmbianceLights && colorTemperature >= MinAllowedColorTemperatureForWhiteAndColorLights;

                case LightType.WhiteOnly:
                    return false;

                default:
                    throw new ArgumentException($"Unsupported light type '{lightType}'.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static ColorTemperature NormalizeColorForAllowedColorRange(this ColorTemperature color, LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Color:
                    return ColorTemperature.Min(MaxAllowedColorTemperatureForWhiteAndColorLights, ColorTemperature.Max(color, MinAllowedColorTemperatureForWhiteAndColorLights));

                case LightType.WhiteAmbiance:
                    return ColorTemperature.Min(MaxAllowedColorTemperatureForWhiteAmbianceLights, ColorTemperature.Max(color, MinAllowedColorTemperatureForWhiteAndColorLights));

                case LightType.WhiteOnly:
                    return null;

                default:
                    throw new ArgumentException($"Unsupported light type '{lightType}'.");
            }
        }
    }
}
