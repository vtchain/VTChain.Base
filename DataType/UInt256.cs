using System;
using System.Globalization;
using System.Linq;
using VTChain.Base.Common;

namespace VTChain.Base.DataType
{
    public class UInt256 : UIntBase, IComparable<UInt256>, IEquatable<UInt256>
    {
        public static readonly UInt256 Zero = new UInt256();

        private const int width = 4;
        private readonly int hashCode;
        private readonly ulong[] parts;

        public UInt256()
            : this(null)
        {
        }

        public UInt256(byte[] value)
            : base(32, value)
        {
        }

        public UInt256(byte[] value, int offset) : this(value)
        {
            if (value.Length < offset + 32)
                throw new ArgumentOutOfRangeException(nameof(offset));

            InnerInit(value, offset, out parts, out hashCode);
        }

        private void InnerInit(byte[] buffer, int offset, out ulong[] parts, out int hashCode)
        {
            // convert parts and store
            parts = new ulong[width];
            offset += 32;
            for (var i = 0; i < width; i++)
            {
                offset -= 8;
                parts[i] = Bits.ToUInt64(buffer, offset);
            }


            InnerInit(out hashCode);
        }

        private void InnerInit(out int hashCode)
        {
            var hashBytes = ToArray();

             hashCode = Convert.ToInt32(xxHash.CalculateHash(hashBytes, 32));
        }

        public int CompareTo(UInt256 other)
        {
            byte[] x = ToArray();
            byte[] y = other.ToArray();
            for (int i = x.Length - 1; i >= 0; i--)
            {
                if (x[i] > y[i])
                    return 1;
                if (x[i] < y[i])
                    return -1;
            }
            return 0;
        }

        bool IEquatable<UInt256>.Equals(UInt256 other)
        {
            return Equals(other);
        }

        public static new UInt256 Parse(string s)
        {
            if (s == null)
                throw new ArgumentNullException();
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 64)
                throw new FormatException();
            return new UInt256(s.HexToBytes().Reverse().ToArray());
        }

        public static bool TryParse(string s, out UInt256 result)
        {
            if (s == null)
            {
                result = null;
                return false;
            }
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 64)
            {
                result = null;
                return false;
            }
            byte[] data = new byte[32];
            for (int i = 0; i < 32; i++)
                if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
                {
                    result = null;
                    return false;
                }
            result = new UInt256(data.Reverse().ToArray());
            return true;
        }

        public static bool operator >(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(UInt256 left, UInt256 right)
        {
            return left.CompareTo(right) <= 0;
        }
    }
}
