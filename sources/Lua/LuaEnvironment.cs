using System;
using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal class LuaEnvironment : LuaTable
    {
        private readonly Dictionary<LuaValue, LuaValue> _external = new Dictionary<LuaValue, LuaValue>();

        public LuaEnvironment()
        {
            SetUpEnvironment();
        }

        private void SetUpEnvironment()
        {
            SetExternalAction("print", Print);
            SetExternalFunction("assert", Assert);
            SetExternalFunction("error", Error);
        }

        private static void Print(params LuaValue[] values)
        {
            if (values.Length == 0)
            {
                Console.WriteLine();
                return;
            }

            Console.Write(values[0].ToPrintString());
            for (var i = 1; i < values.Length; i++)
            {
                Console.Write("\t");
                Console.Write(values[i].ToPrintString());
            }
            Console.WriteLine();
        }

        private static LuaValue[] Assert(params LuaValue[] values)
        {
            if (values.Length == 0)
            {
                throw new ArgumentNullException();
            }

            var v = values[0];
            var message = values.Length >= 2 ? values[1] : LuaValue.Nil;

            return v.IsFalse ? Error(message) : new[] {v, message};
        }

        private static LuaValue[] Error(params LuaValue[] values)
        {
            if (values.Length == 0)
            {
                throw new ArgumentNullException();
            }

            throw new Exception("ERROR: " + values[0].ToPrintString());
        }

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

        private void SetExternalAction(string s, Action<LuaValue[]> del)
        {
            SetExternal(LuaString.FromString(s), LuaValue.ExternalAction(del));
        }

        private void SetExternalFunction(string s, Func<LuaValue[], LuaValue[]> del)
        {
            SetExternal(LuaString.FromString(s), LuaValue.ExternalFunction(del));
        }

        private void SetExternal(LuaValue key, LuaValue value)
        {
            _external[key] = value;
        }

        public static implicit operator LuaUpValue(LuaEnvironment env)
        {
            return new LuaUpValue(new LuaValue[] {env}, 0);
        }
    }
}