using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LuaByteSharp.Lua
{
    internal class LuaString
    {
        public static LuaString Empty = new LuaString();

        private readonly byte[] _rawValue;

        public int Length => _rawValue.Length;

        public string Value => ToString();

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
            return _rawValue.Select(b => b < 32 ? $"\\{b}" : (b == 127 ? "\\127" : ((char) b).ToString()))
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
            return Regex.IsMatch(s, "^[-+]0[xX].*")
                ? long.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)
                : long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
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
            var match = Regex.Match(s, "^[-+]0[xX]([0-9A-Fa-f]+)[Pp]([-+][0-9A-Fa-f]+)");
            if (match.Success)
            {
                var sign = s.StartsWith("-") ? "-" : "";
                var ms = match.Groups[1].Value;
                if (long.TryParse(ms, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long m))
                {
                    var es = match.Groups[2].Value;
                    if (short.TryParse(es, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out short e))
                    {
                        s = $"{sign}{m}E{e}";
                    }
                }
            }

            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        public static LuaString FromFloat(double v)
        {
            return new LuaString(Encoding.ASCII.GetBytes(v.ToString(CultureInfo.InvariantCulture)));
        }

        public static LuaString FromInteger(long v)
        {
            return new LuaString(Encoding.ASCII.GetBytes(v.ToString(CultureInfo.InvariantCulture)));
        }
    }
}