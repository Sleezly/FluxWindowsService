using Q42.HueApi;
using System;
using System.Collections.Generic;

namespace HueController
{
    public class LightDetails
    {
        public enum LightType
        {
            WhiteOnly,
            WhiteAmbiance,
            Color,
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public LightType Type { get; set; }

        public bool AdjustBrightnessAllowed { get; set; }

        public bool AdjustColorTemperatureAllowed { get; set; }

        /// <summary>
        /// Maximum allow color temperature for white ambiance lights
        /// </summary>
        public const int MaxAllowedColorTemperatureForWhiteAmbianceLights = 454;

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        public const int MaxAllowedColorTemperatureForWhiteAndColorLights = 500;

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        public const int MinAllowedColorTemperatureForWhiteAndColorLights = 154;

        /// <summary>
        /// Maximum allowed brightness
        /// </summary>
        public const byte MaxBrightness = 248;

        /// <summary>
        /// Normalizes the given brightness value.
        /// </summary>
        /// <param name="brightness"></param>
        /// <returns></returns>
        public static byte NormalizeBrightness(byte brightness) => (byte)(brightness / 8 * 8);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static int? NormalizeColorForAllowedColorRange(LightType lightType, int color)
        {
            switch (lightType)
            {
                case LightType.Color:
                    return Math.Min(MaxAllowedColorTemperatureForWhiteAndColorLights, Math.Max(color, MinAllowedColorTemperatureForWhiteAndColorLights));

                case LightType.WhiteAmbiance:
                    return Math.Min(MaxAllowedColorTemperatureForWhiteAmbianceLights, Math.Max(color, MinAllowedColorTemperatureForWhiteAndColorLights));

                case LightType.WhiteOnly:
                    return null;

                default:
                    throw new ArgumentException($"Unsupported light type '{lightType}'.");
            }
        }
    }
}
