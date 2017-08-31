using System;
using System.Linq;
using AT.MIN;

namespace LuaByteSharp.Lua.Libraries
{
    internal class LuaLibString : LuaExternalTable
    {
        public LuaLibString()
        {
            SetExternalFunction("byte", Byte);
            SetExternalFunction("char", Char);
            SetExternalFunction("find", Find);
            SetExternalFunction("format", Format);
            SetExternalFunction("gmatch", GMatch);
            SetExternalFunction("gsub", GSub);
            SetExternalFunction("len", Len);
            SetExternalFunction("lower", Lower);
            SetExternalFunction("match", Match);
            SetExternalFunction("pack", Pack);
            SetExternalFunction("packsize", PackSize);
            SetExternalFunction("rep", Rep);
            SetExternalFunction("reverse", Reverse);
            SetExternalFunction("sub", Sub);
            SetExternalFunction("unpack", Unpack);
            SetExternalFunction("upper", Upper);
        }

        private static LuaValue[] Unpack(LuaValue[] arg)
        {
            throw new NotSupportedException();
        }

        private static LuaValue[] PackSize(LuaValue[] arg)
        {
            throw new NotSupportedException();
        }

        private static LuaValue[] Pack(LuaValue[] arg)
        {
            throw new NotSupportedException();
        }

        private static LuaValue[] Match(LuaValue[] arg)
        {
            throw new NotSupportedException();
        }

        private static LuaValue[] GSub(LuaValue[] arg)
        {
            throw new NotSupportedException();
        }

        private static LuaValue[] GMatch(LuaValue[] arg)
        {
            throw new NotSupportedException();
        }

        private static LuaValue[] Find(LuaValue[] arg)
        {
            throw new NotSupportedException();
        }

        private static LuaValue[] Format(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            var format = args[0].AsString().Value;

            var s = Tools.sprintf(format, args.Skip(1).ToArray<object>());

            return new LuaValue[] {LuaString.FromString(s)};
        }

        private static LuaValue[] Sub(LuaValue[] args)
        {
            if (args.Length < 2)
            {
                throw new InvalidArgumentCountException();
            }

            var luaString = args[0].AsString();
            var i = (int) args[1].AsInteger();

            var j = -1;
            if (args.Length >= 3)
            {
                j = (int) args[2].AsInteger();
            }

            CorrectIndices(luaString, ref i, ref j);

            if (i > j)
            {
                return new LuaValue[] {LuaString.Empty};
            }

            return new LuaValue[] {LuaString.FromString(luaString.Value.Substring(i - 1, j - i + 1))};
        }

        private static LuaValue[] Reverse(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            var bytes = args[0].AsString().Bytes;
            var copy = new byte[bytes.Length];
            Array.Copy(bytes, copy, bytes.Length - 1);
            Array.Reverse(copy);
            return new LuaValue[] {new LuaString(bytes)};
        }

        private static LuaValue[] Rep(LuaValue[] args)
        {
            if (args.Length < 2)
            {
                throw new InvalidArgumentCountException();
            }

            var s = args[0].AsString().Value;
            var n = args[1].AsInteger();
            var sep = "";

            if (args.Length >= 3)
            {
                sep = args[2].AsString().Value;
            }

            if (n <= 0)
            {
                return new LuaValue[] {LuaString.Empty};
            }

            var rep = Repeat(s, n, sep);
            return new LuaValue[] {LuaString.FromString(rep)};
        }

        private static string Repeat(string s, long n, string sep)
        {
            var rep = "";
            long i = 0;
            while (i < n - 1)
            {
                var r = s;
                long j = 1;
                while (2 * j < n - i)
                {
                    r += sep + r;
                    j *= 2;
                }
                rep += r + sep;
                i += j;
            }
            rep += s;
            return rep;
        }


        private static LuaValue[] Upper(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            return new LuaValue[] {LuaString.FromString(args[0].AsString().Value.ToUpper())};
        }

        private static LuaValue[] Lower(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            return new LuaValue[] {LuaString.FromString(args[0].AsString().Value.ToLower())};
        }

        private static LuaValue[] Len(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            return new[] {new LuaValue(args[0].AsString().Length - 1)};
        }

        private static LuaValue[] Char(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                return new LuaValue[] {LuaString.Empty};
            }

            var bytes = args.Select(arg => (byte) arg.AsInteger()).ToArray();
            return new LuaValue[] {new LuaString(bytes)};
        }

        private static LuaValue[] Byte(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidArgumentCountException();
            }

            var luaString = args[0].AsString();
            var i = 0;
            if (args.Length >= 2)
            {
                i = (int) args[1].AsInteger();
            }

            var j = i;
            if (args.Length >= 3)
            {
                j = (int) args[2].AsInteger();
            }

            CorrectIndices(luaString, ref i, ref j);

            return luaString.Bytes.Skip(i - 1).Take(j - i + 1).Select(b => new LuaValue(b)).ToArray();
        }

        private static void CorrectIndices(LuaString luaString, ref int i, ref int j)
        {
            var len = luaString.Length;
            if (i < 0)
            {
                i += len + 1;
            }

            if (j < 0)
            {
                j += len + 1;
            }

            if (i < 1)
            {
                i = 1;
            }

            if (j > len)
            {
                j = len;
            }
        }
    }
}