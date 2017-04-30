using System;
using System.Linq;

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
    }
}