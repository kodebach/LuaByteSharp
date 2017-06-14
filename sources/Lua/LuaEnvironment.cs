using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal class LuaEnvironment : LuaTable
    {
        private readonly Dictionary<LuaValue, LuaValue> _external = new Dictionary<LuaValue, LuaValue>();

        internal override LuaValue this[LuaValue key]
        {
            get
            {
                var value = base[key];
                return value.IsNil ? GetExternal(key) : value;
            }
            set => base[key] = value;
        }

        private LuaValue GetExternal(LuaValue key)
        {
            return !_external.ContainsKey(key) ? LuaValue.Nil : _external[key];
        }

        private void SetExternal(LuaValue key, LuaValue value)
        {
            _external[key] = value;
        }
    }
}