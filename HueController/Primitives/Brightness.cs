using System;

namespace HueController.Primitives
{
    public class Brightness : IEquatable<Brightness>
    {
        private byte Value { get; }

        /// <summary>
        /// Maximum allowed brightness for Hue lights.
        /// </summary>
        public static byte MaxBrightness => 254;

        /// <summary>
        /// Minimum allowed brightness for Hue lights.
        /// </summary>
        public static byte MinBrightness => 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="brightness"></param>
        public Brightness(byte brightness)
        {
            Value = Math.Max(MinBrightness, Math.Min(brightness, MaxBrightness));
        }
        
        public static implicit operator Brightness(byte brightness)
        {
            return new Brightness(brightness);
        }

        public static implicit operator byte(Brightness brightness)
        {
            return null != brightness ? brightness.Value : MinBrightness;
        }

        public static implicit operator byte?(Brightness brightness)
        {
            return null != brightness ? brightness.Value : (byte?)null;
        }

        public static Brightness operator +(Brightness first, Brightness second)
        {
            return Convert.ToByte(first.Value + second.Value);
        }

        public static Brightness operator -(Brightness first, Brightness second)
        {
            return Convert.ToByte(first.Value - second.Value);
        }

        public static Brightness operator +(Brightness first, byte second)
        {
            return Convert.ToByte(first.Value + second);
        }

        public static Brightness operator -(Brightness first, byte second)
        {
            return Convert.ToByte(first.Value - second);
        }

        public static Brightness operator +(byte first, Brightness second)
        {
            return Convert.ToByte(first + second.Value);
        }

        public static Brightness operator -(byte first, Brightness second)
        {
            return Convert.ToByte(first - second.Value);
        }

        public static Brightness operator +(Brightness first, int second)
        {
            return Convert.ToByte(first.Value + second);
        }

        public static Brightness operator -(Brightness first, int second)
        {
            return Convert.ToByte(first.Value - second);
        }

        public static Brightness operator +(int first, Brightness second)
        {
            return Convert.ToByte(first + second.Value);
        }

        public static Brightness operator -(int first, Brightness second)
        {
            return Convert.ToByte(first - second.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Brightness);
        }

        public bool Equals(Brightness other)
        {
            if (other is null)
            {
                return false;
            }

            return Value == other.Value;
        }

        public static bool Equals(Brightness first, Brightness second)
        {
            if (first is null)
            {
                return second is null;
            }

            return first.Equals(second);
        }

        public static bool operator ==(Brightness first, Brightness second)
        {
            return Equals(first, second);
        }

        public static bool operator !=(Brightness first, Brightness second)
        {
            return !Equals(first, second);
        }

        public static bool operator >(Brightness first, Brightness second)
        {
            return first.Value > second.Value;
        }

        public static bool operator <(Brightness first, Brightness second)
        {
            return first.Value < second.Value;
        }

        public static bool operator >=(Brightness first, Brightness second)
        {
            return first.Value >= second.Value;
        }

        public static bool operator <=(Brightness first, Brightness second)
        {
            return first.Value <= second.Value;
        }
    }
}
