using System;
using System.Linq;

namespace LuaByteSharp.Lua.Libraries
{
    internal class LuaLibTable : LuaExternalTable
    {
        public LuaLibTable()
        {
            SetExternalFunction("concat", Concat);
            SetExternalAction("insert", Insert);
            SetExternalFunction("move", Move);
            SetExternalFunction("pack", Pack);
            SetExternalFunction("remove", Remove);
            SetExternalFunction("sort", Sort);
            SetExternalFunction("unpack", Unpack);
        }

        private static LuaValue[] Concat(LuaValue[] args)
        {
            if (args.Length < 1)
            {
                throw new InvalidArgumentCountException();
            }

            var list = (LuaTable) args[0].RawValue;
            var sep = args.Length > 1 ? args[1].AsString().Value : "";
            var i = args.Length > 2 ? args[2].AsInteger() : 1;
            var j = args.Length > 3 ? args[3].AsInteger() : list.Length.AsInteger();

            var result = "";
            for (var k = i; k < j; k++)
            {
                result += list[new LuaValue(k)].AsString().Value + sep;
            }
            result += list[new LuaValue(j)];

            return new LuaValue[] {LuaString.FromString(result)};
        }

        private static void Insert(LuaValue[] args)
        {
            if (args.Length < 2)
            {
                throw new InvalidArgumentCountException();
            }

            var table = (LuaTable) args[0].RawValue;

            var value = args[1];
            var pos = table.Length.AsInteger() + 1;
            if (args.Length >= 3)
            {
                pos = args[1].AsInteger();
                value = args[2];
            }

            table.Insert(pos, value);
        }

        private static LuaValue[] Move(LuaValue[] args)
        {
            if (args.Length < 4)
            {
                throw new InvalidArgumentCountException();
            }

            var table = (LuaTable) args[0].RawValue;

            var f = args[1].AsInteger();
            var e = args[2].AsInteger();
            var t = args[3].AsInteger();
            var dest = args.Length > 4 ? (LuaTable) args[4].RawValue : table;

            for (var i = f; i <= e; i++)
            {
                dest[new LuaValue(t)] = table[new LuaValue(i)];
            }

            return new LuaValue[] {dest};
        }

        private static LuaValue[] Remove(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            var table = (LuaTable) args[0].RawValue;

            var pos = table.Length.AsInteger();
            if (args.Length >= 2)
            {
                pos = args[1].AsInteger();
            }

            return new[] {table.Remove(pos)};
        }

        private static LuaValue[] Sort(LuaValue[] arg)
        {
            throw new NotImplementedException();
        }

        private static LuaValue[] Pack(LuaValue[] args)
        {
            var table = new LuaTable(args.Length, 1) {[LuaString.FromString("n")] = new LuaValue(args.Length)};
            for (var i = 0; i < args.Length; i++)
            {
                table[i] = args[i];
            }
            return new LuaValue[] {table};
        }

        private static LuaValue[] Unpack(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            if (args[0].Type != LuaValueType.Table)
            {
                LuaEnvironment.Error("list has to be table");
                return new LuaValue[0];
            }

            var table = (LuaTable) args[0].RawValue;

            var i = 1L;
            if (args.Length >= 2)
            {
                i = args[1].AsInteger();
            }

            var len = table.Length.AsInteger();
            var j = len;
            if (args.Length >= 3)
            {
                j = args[2].AsInteger();
            }

            var list = new LuaValue[len];
            for (var k = i; k <= j; k++)
            {
                list[k - i] = table[new LuaValue(k)];
            }

            return list;
        }
    }
}