using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// Maximum allow color temperature for white ambiance lights
        /// </summary>
        private const int MaxAllowedColorTemperatureForWhiteAmbianceLights = 454;

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        private const int MaxAllowedColorTemperatureForWhiteAndColorLights = 500;

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        private const int MinAllowedColorTemperatureForWhiteAndColorLights = 154;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool IsInAllowedColorRange(string type, int color)
        {
            return IsInAllowedColorRange(TranslateStringToLightType(type), color);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static bool IsInAllowedColorRange(LightType type, int color)
        {
            switch (type)
            {
                case LightType.Color:
                    return color < MaxAllowedColorTemperatureForWhiteAndColorLights && color > MinAllowedColorTemperatureForWhiteAndColorLights;

                case LightType.WhiteAmbiance:
                    return color < MaxAllowedColorTemperatureForWhiteAmbianceLights && color > MinAllowedColorTemperatureForWhiteAndColorLights;

                case LightType.WhiteOnly:
                    return false;

                default:
                    throw new ArgumentException($"Unsupported light type '{type}'.");
            }
        }

        /// <summary>
        /// Converts a string to a light bulb type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static LightType TranslateStringToLightType(string type)
        {
            if (string.Equals("Extended color light", type, StringComparison.InvariantCultureIgnoreCase))
            {
                return LightType.Color;
            }
            else if (string.Equals("Color temperature light", type, StringComparison.InvariantCultureIgnoreCase))
            {
                return LightType.WhiteAmbiance;
            }
            else if (string.Equals("Dimmable light", type, StringComparison.InvariantCultureIgnoreCase))
            {
                return LightType.WhiteOnly;
            }

            throw new ArgumentException($"No known mapping for light type '{type}'.");
        }
    }
}
