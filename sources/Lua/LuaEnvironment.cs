using System;
using System.Globalization;
using System.Linq;
using System.Text;

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
            SetExternalAction("print", Print);
            SetExternalFunction("assert", Assert);
            SetExternalAction("error", Error);


            SetExternalValue("math", new LuaMath());
            SetExternalValue("utf8", new LuaUTF8());
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

    internal class LuaUTF8 : LuaExternalTable
    {
        private const string Utf8Pattern = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";

        public static readonly LuaValue CharPattern = LuaString.FromString(Utf8Pattern);

        public LuaUTF8()
        {
            SetExternalFunction("char", Char);
            SetExternalValue("charpattern", CharPattern);
            SetExternalFunction("codes", Codes);
            SetExternalFunction("codepoint", CodePoint);
            SetExternalFunction("len", Len);
            SetExternalFunction("offset", Offset);
        }

        private static bool IsContinuation(byte b)
        {
            return (b & 0xC0) == 0x80;
        }

        private static LuaValue[] Offset(LuaValue[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentNullException();
            }

            var luaString = args[0].AsString();

            var n = args[1].AsInteger();

            var len = luaString.Length;
            var i = n >= 0 ? 1L : len - 1;
            if (args.Length > 2)
            {
                i = args[2].AsInteger();
            }
            i = i >= len ? i : (-i > len ? 0 : len + i + 1);
            i--;

            if (n == 0)
            {
                while (i > 0 && IsContinuation(luaString[(int) i])) i--;
                return new[] {new LuaValue(i + 1)};
            }
            if (IsContinuation(luaString[(int) i]))
            {
                LuaEnvironment.Error("initial position is a continuation byte");
            }

            if (n < 0)
            {
                while (n < 0 && i > 0)
                {
                    // move back
                    do
                    {
                        // find beginning of previous character
                        i--;
                    } while (i > 0 && IsContinuation(luaString[(int) i]));
                    n++;
                }
            }
            else
            {
                n--; // do not move for 1st character
                while (n > 0 && i < len)
                {
                    do
                    {
                        // find beginning of next character
                        i++;
                    } while (IsContinuation(luaString[(int) i])); // (cannot pass final '\0')
                    n--;
                }
            }
            return n == 0 ? new[] {new LuaValue(i + 1)} : new[] {LuaValue.Nil};
        }

        private static LuaValue[] Len(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            var luaString = args[0].AsString();

            var i = 1L;
            if (args.Length > 1)
            {
                i = args[1].AsInteger();
            }

            var j = i;
            if (args.Length > 2)
            {
                j = args[2].AsInteger();
            }

            return new[] {new LuaValue(Encoding.UTF8.GetCharCount(luaString.Bytes, (int) (i - 1), (int) (j - i + 1)))};
        }

        private static LuaValue[] CodePoint(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            var luaString = args[0].AsString();

            var i = 1L;
            if (args.Length > 1)
            {
                i = args[1].AsInteger();
            }

            var j = i;
            if (args.Length > 2)
            {
                j = args[2].AsInteger();
            }

            var codepoints = new LuaValue[j - i + 1];
            for (var k = 0; k < j - i + 1; k++)
            {
                codepoints[k] = new LuaValue(DecodeUtf8(luaString.Bytes, (int) (i + k)));
            }
            return codepoints;
        }

        private static LuaValue[] Codes(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            return new[] {LuaValue.ExternalFunction(CodesIterator), args[0], LuaValue.Zero};
        }

        private static LuaValue[] CodesIterator(LuaValue[] args)
        {
            var luaString = args[0].AsString();
            var pos = args[1].AsInteger() - 1;
            pos++; // goto next byte
            while (IsContinuation(luaString[(int) pos])) pos++; // skip continuation bytes

            if (pos >= luaString.Length)
            {
                return new[] {LuaValue.Nil}; // done
            }

            return new[] {new LuaValue(pos + 1), new LuaValue(DecodeUtf8(luaString.Bytes, (int) pos))};
        }

        private static long DecodeUtf8(byte[] bytes, int index)
        {
            return char.ConvertToUtf32(Encoding.UTF8.GetString(bytes, index, bytes.Length - index), 0);
        }

        public static LuaValue[] Char(params LuaValue[] args)
        {
            if (args.Length == 0)
            {
                return new LuaValue[] {LuaString.Empty};
            }

            var s = "";
            foreach (var arg in args)
            {
                if (arg.Type != LuaValueType.Integer)
                {
                    LuaEnvironment.Error("all arguments have to be integers");
                    return new LuaValue[0];
                }

                var i = (int) arg.AsInteger();
                s += char.ConvertFromUtf32(i);
            }
            return new LuaValue[] {LuaString.FromString(s, Encoding.UTF8)};
        }
    }
}