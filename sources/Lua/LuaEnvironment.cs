using System;

namespace LuaByteSharp.Lua
{
    internal class LuaEnvironment : LuaExternalTable
    {
        public LuaEnvironment()
        {
            SetUpEnvironment();
        }

        private void SetUpEnvironment()
        {
            SetExternalFunction("assert", Assert);
            SetExternalAction("error", Error);
            SetExternalValue("_G", this);
            SetExternalFunction("getmetatable", GetMetaTable);
            SetExternalFunction("ipairs", IPairs);
            SetExternalFunction("next", Next);
            SetExternalFunction("pairs", Pairs);
            SetExternalAction("print", Print);
            SetExternalFunction("rawequal", RawEqual);
            SetExternalFunction("rawget", RawGet);
            SetExternalFunction("rawlen", RawLen);
            SetExternalFunction("rawset", RawSet);
            SetExternalFunction("select", Select);
            SetExternalFunction("setmetatable", SetMetaTable);
            SetExternalFunction("tonumber", ToNumber);
            SetExternalFunction("tostring", ToString);
            SetExternalFunction("type", Type);
            SetExternalValue("_VERSION", Interpreter.Version);


            SetExternalValue("math", new LuaMath());
            SetExternalValue("utf8", new LuaUtf8());
        }

        private static LuaValue[] Pairs(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (args[0].HasMetaTable)
            {
                throw new NotImplementedException("metamethods");
            }

            if (args[0].Type != LuaValueType.Table)
            {
                Error("t has to be a table");
                return new[] {LuaValue.Nil};
            }

            return new[] {LuaValue.ExternalFunction(Next), args[0], LuaValue.Nil};
        }

        private static LuaValue[] Next(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (args[0].Type != LuaValueType.Table)
            {
                Error("t has to be a table");
                return new[] {LuaValue.Nil};
            }

            var index = args.Length >= 1 ? args[1] : LuaValue.Nil;
            return new[] {((LuaTable) args[0].RawValue).Next(index)};
        }

        private static LuaValue[] IPairs(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (args[0].Type != LuaValueType.Table)
            {
                Error("t has to be a table");
                return new[] {LuaValue.Nil};
            }

            return new[] {LuaValue.ExternalFunction(IPairsIterator), args[0], LuaValue.Zero};
        }

        private static LuaValue[] IPairsIterator(LuaValue[] args)
        {
            var table = args[0];
            var index = args[1] + LuaValue.One;

            var value = table[index];
            return value.IsNil ? new[] {LuaValue.Nil} : new[] {index, value};
        }

        private static LuaValue[] RawEqual(LuaValue[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentNullException();
            }

            return new[] {new LuaValue(args[0].RawEquals(args[1]))};
        }

        private static LuaValue[] RawGet(LuaValue[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentNullException();
            }

            return new[] {args[0].RawGet(args[1])};
        }

        private static LuaValue[] RawLen(LuaValue[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentNullException();
            }

            return new[] {new LuaValue(args[0].RawLength)};
        }

        private static LuaValue[] RawSet(LuaValue[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentNullException();
            }

            return new[] {new LuaValue(args[0].RawSet(args[1], args[2]))};
        }

        private static LuaValue[] Select(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            var pos = args[0];
            if (pos.Type != LuaValueType.Integer)
            {
                if (pos.AsString().Value == "#")
                {
                    return new[] {new LuaValue(args.Length - 1)};
                }

                throw new ArgumentException("index");
            }

            var index = (int) pos.AsInteger();
            if (index < 0)
            {
                index = args.Length - index - 1;
            }

            var ret = new LuaValue[args.Length - 1 - index];
            Array.Copy(args, index, ret, 0, ret.Length);
            return ret;
        }

        private static LuaValue[] GetMetaTable(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            throw new NotSupportedException("metatables");
        }

        private static LuaValue[] SetMetaTable(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            throw new NotSupportedException("metatables");
        }

        private static LuaValue[] ToNumber(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (args.Length > 1)
            {
                if (args[1].AsInteger() != 10)
                {
                    Error("only base 10 supported");
                    return new[] {LuaValue.Nil};
                }
            }

            if (args[0].IsString)
            {
                var luaString = args[0].AsString();
                if (luaString.TryParseInteger(out long v))
                {
                    return new[] {new LuaValue(v)};
                }
                if (luaString.TryParseFloat(out double d))
                {
                    return new[] {new LuaValue(d)};
                }
            }
            return new[] {LuaValue.Nil};
        }

        private static LuaValue[] ToString(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            return new LuaValue[] {args[0].AsString()};
        }

        private static LuaValue[] Type(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            switch (args[0].Type)
            {
                case LuaValueType.Nil:
                    return new LuaValue[] {LuaString.FromString("nil")};
                case LuaValueType.Boolean:
                    return new LuaValue[] {LuaString.FromString("boolean")};
                case LuaValueType.Float:
                case LuaValueType.Integer:
                    return new LuaValue[] {LuaString.FromString("number")};
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    return new LuaValue[] {LuaString.FromString("string")};
                case LuaValueType.Table:
                    return new LuaValue[] {LuaString.FromString("table")};
                case LuaValueType.Closure:
                case LuaValueType.ExternalMethod:
                case LuaValueType.ExternalAction:
                    return new LuaValue[] {LuaString.FromString("function")};
                default:
                    return new[] {LuaValue.Nil};
            }
        }

        private static void Print(params LuaValue[] values)
        {
            if (values.Length == 0 || values[0] == null)
            {
                Console.WriteLine();
                return;
            }

            Console.Write(values[0].ToPrintString());
            for (var i = 1; i < values.Length; i++)
            {
                Console.Write("\t");
                Console.Write(values[i]?.ToPrintString() ?? "");
            }
            Console.WriteLine();
        }

        private static LuaValue[] Assert(params LuaValue[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentNullException();
            }

            var v = values[0];
            var message = values.Length >= 2 ? values[1] : LuaValue.Nil;

            if (v.IsFalse)
            {
                Error(message);
                return new LuaValue[0];
            }

            return new[] {v, message};
        }

        private static void Error(params LuaValue[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentNullException();
            }

            throw new Exception("ERROR: " + values[0].ToPrintString());
        }

        public static void Error(string message)
        {
            Error(LuaString.FromString(message));
        }

        public static implicit operator LuaUpValue(LuaEnvironment env)
        {
            return new LuaUpValue(new LuaValue[] {env}, 0);
        }
    }
}