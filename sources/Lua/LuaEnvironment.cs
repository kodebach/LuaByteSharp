using System;
using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal delegate void Print(params LuaValue[] values);

    internal class LuaEnvironment : LuaTable
    {
        private readonly Dictionary<LuaValue, LuaValue> _external = new Dictionary<LuaValue, LuaValue>();

        public LuaEnvironment()
        {
            SetUpEnvironment();
        }

        private void SetUpEnvironment()
        {
            SetExternalFunction("print", new Print(PrintLuaValue));
        }

        private static void PrintLuaValue(params LuaValue[] values)
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

        private void SetExternalFunction(string s, Delegate del)
        {
            var name = LuaString.FromString(s);
            SetExternal(new LuaValue(LuaValueType.ShortString, name), LuaValue.ExternalFunction(del));
        }

        private void SetExternal(LuaValue key, LuaValue value)
        {
            _external[key] = value;
        }
    }
}