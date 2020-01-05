using System;
using Utilities;

namespace HueController.Primitives
{
    public class ColorTemperature : IEquatable<ColorTemperature>
    {
        private int Integer { get; }

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        internal static int MaxAllowedColorTemperatureForWhiteAndColorLights => 500;

        /// <summary>
        /// Maximum allow color temperature for extended color lights
        /// </summary>
        internal static int MinColorTemperatureValue => 154;

        public ColorTemperature(int colorTemperature)
        {
            // Allowable values are in range 154 <-> 500.
            Integer = Math.Max(MinColorTemperatureValue, 
                Math.Min(colorTemperature, MaxAllowedColorTemperatureForWhiteAndColorLights));
        }

        public double Kelvin => 1000000.0 / Integer;

        public RGB RGB => ColorConverter.MiredToRGB(Integer);

        public double[] XY => ColorConverter.RGBtoXY(RGB);
        
        public static implicit operator ColorTemperature(int colorTemperature)
        {
            return new ColorTemperature(colorTemperature);
        }

        public static implicit operator int(ColorTemperature colorTemperature)
        {
            return null != colorTemperature ? colorTemperature.Integer : 0;
        }

        public static implicit operator int?(ColorTemperature colorTemperature)
        {
            return null != colorTemperature ? colorTemperature.Integer : (int?)null;
        }

        public static ColorTemperature operator +(ColorTemperature first, ColorTemperature second)
        {
            return first.Integer + second.Integer;
        }

        public static ColorTemperature operator -(ColorTemperature first, ColorTemperature second)
        {
            return first.Integer - second.Integer;
        }

        public static ColorTemperature operator +(ColorTemperature first, int second)
        {
            return first.Integer + second;
        }

        public static ColorTemperature operator -(ColorTemperature first, int second)
        {
            return first.Integer - second;
        }

        public static ColorTemperature operator +(int first, ColorTemperature second)
        {
            return first + second.Integer;
        }

        public static ColorTemperature operator -(int first, ColorTemperature second)
        {
            return first - second.Integer;
        }

        public override int GetHashCode()
        {
            return Integer.GetHashCode();
        }

        public override string ToString()
        {
            return Integer.ToString();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColorTemperature);
        }

        public bool Equals(ColorTemperature other)
        {
            if (other is null)
            {
                return false;
            }

            return Integer == other.Integer;
        }

        public static bool Equals(ColorTemperature first, ColorTemperature second)
        {
            if (first is null)
            {
                return second is null;
            }

            return first.Equals(second);
        }

        public static bool operator ==(ColorTemperature first, ColorTemperature second)
        {
            return Equals(first, second);
        }

        public static bool operator !=(ColorTemperature first, ColorTemperature second)
        {
            return !Equals(first, second);
        }

        public static bool operator >(ColorTemperature first, ColorTemperature second)
        {
            return first.Integer > second.Integer;
        }

        public static bool operator <(ColorTemperature first, ColorTemperature second)
        {
            return first.Integer < second.Integer;
        }

        public static bool operator >=(ColorTemperature first, ColorTemperature second)
        {
            return first.Integer >= second.Integer;
        }

        public static bool operator <=(ColorTemperature first, ColorTemperature second)
        {
            return first.Integer <= second.Integer;
        }
    }
}
