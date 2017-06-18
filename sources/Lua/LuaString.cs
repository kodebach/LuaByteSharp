using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FParsec;

namespace LuaByteSharp.Lua
{
    internal class LuaString
    {
        public static LuaString Empty = new LuaString();

        private readonly byte[] _rawValue;
        public const long ShortMax = 0xFF;

        public int Length => _rawValue.Length;

        public string Value => ToString();

        public bool IsEmpty => _rawValue == null || _rawValue.Length == 0 ||
                               _rawValue[0] == '\0' && _rawValue.Length == 1;

        public LuaString(byte[] rawValue)
        {
            _rawValue = new byte[rawValue.Length + 1];
            Array.Copy(rawValue, _rawValue, rawValue.Length);
        }

        private LuaString()
        {
            _rawValue = null;
        }

        public override string ToString()
        {
            return ToEscapedAsciiString();
        }

        private string ToEscapedAsciiString()
        {
            return _rawValue
                .Take(Length - 1)
                .Select(b => b < 32 ? $"\\{b}" : (b == 127 ? "\\127" : ((char) b).ToString()))
                .Aggregate("", (current, s) => current + s);
        }

        public long ParseInteger()
        {
            if (TryParseInteger(out long v))
            {
                return v;
            }

            throw new FormatException("not a number");
        }

        public bool TryParseInteger(out long v)
        {
            var s = Value.Trim();
            var isInt = Regex.IsMatch(s, "^[-+]?0[xX].*")
                ? long.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)
                : long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
            if (isInt)
            {
                return true;
            }

            if (TryParseFloat(out double dv) && Math.Abs(Math.Floor(dv) - dv) < double.Epsilon)
            {
                if (dv < long.MaxValue)
                {
                    v = Convert.ToInt64(dv);
                    return true;
                }
            }

            return false;
        }

        public double ParseFloat()
        {
            if (TryParseFloat(out double v))
            {
                return v;
            }

            throw new FormatException("not a number");
        }

        public bool TryParseFloat(out double v)
        {
            var s = Value.Trim();
            const string hexPattern =
                "[+-]?((0[xX])?([0-9a-fA-F]+(\\.[0-9a-fA-F]*)?|\\.[0-9a-fA-F]+)([pP][+-]?[0-9]+)?|[iI][nN][fF]([iI][nN][iI][tT][yY])?|[nN][aA][nN])";
            if (Regex.IsMatch(s, hexPattern))
            {
                v = HexFloat.DoubleFromHexString(s);
                return true;
            }

            return double.TryParse(s, out v);
        }

        public static LuaString FromFloat(double v)
        {
            return new LuaString(Encoding.ASCII.GetBytes(v.ToString(CultureInfo.InvariantCulture)));
        }

        public static LuaString FromInteger(long v)
        {
            return new LuaString(Encoding.ASCII.GetBytes(v.ToString(CultureInfo.InvariantCulture)));
        }

        protected bool Equals(LuaString other)
        {
            return !ReferenceEquals(null, other) && _rawValue.Length == other._rawValue.Length &&
                   _rawValue.SequenceEqual(other._rawValue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((LuaString) obj);
        }

        public override int GetHashCode()
        {
            if (_rawValue == null) return 0;
            unchecked
            {
                return _rawValue.Aggregate(17, (current, value) => current * 23 + value.GetHashCode());
            }
        }

        public static bool operator ==(LuaString left, LuaString right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LuaString left, LuaString right)
        {
            return !Equals(left, right);
        }

        public int CompareTo(LuaString other)
        {
            for (var i = 0; i < _rawValue.Length && i < other._rawValue.Length; i++)
            {
                if (_rawValue[i] < other._rawValue[i])
                {
                    return -1;
                }
                if (_rawValue[i] > other._rawValue[i])
                {
                    return 1;
                }
            }

            return _rawValue.Length.CompareTo(other._rawValue.Length);
        }

        public static LuaString FromString(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            return new LuaString(bytes);
        }

        public static LuaString Concat(IList<LuaValue> args)
        {
            return Concat(args.SkipWhile(value => value.IsEmptyString)
                .Select(value => value.AsString())
                .ToArray());
        }

        public static LuaString Concat(IList<LuaString> args)
        {
            if (args.Count == 0)
            {
                return Empty;
            }

            var first = args[0];
            var length = args.Select(s => s.Length - 1).Sum();
            var newValue = new byte[length];
            Array.Copy(first._rawValue, newValue, first._rawValue.Length - 1);
            var pos = first._rawValue.Length - 1;
            for (var i = 1; i < args.Count; i++)
            {
                var arg = args[i];
                Array.Copy(arg._rawValue, 0, newValue, pos, arg._rawValue.Length - 1);
                pos += arg._rawValue.Length - 1;
            }
            return new LuaString(newValue);
        }
    }
}