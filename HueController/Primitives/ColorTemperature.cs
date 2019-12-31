using System;
using Utilities;

namespace HueController.Primitives
{
    public class ColorTemperature : IEquatable<ColorTemperature>
    {
        private int Integer { get; }

        public ColorTemperature(int colorTemperature)
        {
            Integer = colorTemperature;
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

        public static ColorTemperature Max(ColorTemperature first, ColorTemperature second)
        {
            return Math.Max(first.Integer, second.Integer);
        }

        public static ColorTemperature Min(ColorTemperature first, ColorTemperature second)
        {
            return Math.Min(first.Integer, second.Integer);
        }
    }
}
