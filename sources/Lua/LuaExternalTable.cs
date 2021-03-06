using System;
using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal class LuaExternalTable : LuaTable
    {
        private readonly Dictionary<LuaValue, LuaValue> _external = new Dictionary<LuaValue, LuaValue>();

        public override LuaValue RawGet(LuaValue key)
        {
            var value = base.RawGet(key);
            return value.IsNil ? GetExternal(key) : value;
        }

        private LuaValue GetExternal(LuaValue key)
        {
            return !_external.ContainsKey(key) ? LuaValue.Nil : _external[key];
        }

        protected void SetExternalAction(string s, Action<LuaValue[]> del)
        {
            SetExternal(LuaString.FromString(s), LuaValue.ExternalAction(del));
        }

        protected void SetExternalFunction(string s, Func<LuaValue[], LuaValue[]> del)
        {
            SetExternal(LuaString.FromString(s), LuaValue.ExternalFunction(del));
        }

        protected void SetExternalValue(string s, LuaValue value)
        {
            SetExternal(LuaString.FromString(s), value);
        }

        private void SetExternal(LuaValue key, LuaValue value)
        {
            _external[key] = value;
        }

        protected void RemoveExternal(string name)
        {
            RemoveExternal(LuaString.FromString(name));
        }

        private void RemoveExternal(LuaString key)
        {
            _external.Remove(key);
        }
    }
}