using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaByteSharp.Lua
{
    internal class LuaExternalTable : LuaTable
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
    }

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
            SetExternalFunction("error", Error);


            SetExternalValue("math", new LuaMath());
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

            return v.IsFalse ? Error(message) : new[] {v, message};
        }

        private static LuaValue[] Error(params LuaValue[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentNullException();
            }

            throw new Exception("ERROR: " + values[0].ToPrintString());
        }

        public static implicit operator LuaUpValue(LuaEnvironment env)
        {
            return new LuaUpValue(new LuaValue[] {env}, 0);
        }
    }

    internal class LuaMath : LuaExternalTable
    {
        private static readonly LuaValue Huge = new LuaValue(double.PositiveInfinity);
        private static readonly LuaValue MaxInteger = new LuaValue(long.MaxValue);
        private static readonly LuaValue MinInteger = new LuaValue(long.MinValue);
        private static readonly LuaValue Pi = new LuaValue(Math.PI);
        private static Random _random = new Random();

        public LuaMath()
        {
            SetExternalFunction("abs", Abs);
            SetExternalFunction("acos", Acos);
            SetExternalFunction("asin", Asin);
            SetExternalFunction("atan", Atan);
            SetExternalFunction("ceil", Ceil);
            SetExternalFunction("cos", Cos);
            SetExternalFunction("deg", Deg);
            SetExternalFunction("exp", Exp);
            SetExternalFunction("floor", Floor);
            SetExternalFunction("fmod", FMod);
            SetExternalValue("huge", Huge);
            SetExternalFunction("log", Log);
            SetExternalFunction("max", Max);
            SetExternalValue("maxinteger", MaxInteger);
            SetExternalFunction("min", Min);
            SetExternalValue("mininteger", MinInteger);
            SetExternalFunction("modf", ModF);
            SetExternalValue("pi", Pi);
            SetExternalFunction("rad", Rad);
            SetExternalFunction("random", Random);
            SetExternalAction("randomseed", RandomSeed);
            SetExternalFunction("sin", Sin);
            SetExternalFunction("sqrt", Sqrt);
            SetExternalFunction("tan", Tan);
            SetExternalFunction("tointeger", ToInteger);
            SetExternalFunction("type", Type);
            SetExternalFunction("ult", ULt);
        }

        private static LuaValue[] SingleArgNumberFunc(LuaValue[] args, Func<long, long> integerFunc,
            Func<double, double> floatFunc)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (args[0].Type == LuaValueType.Integer)
            {
                return new[] {new LuaValue(integerFunc(args[0].AsInteger()))};
            }
            return new[] {new LuaValue(floatFunc(args[0].AsNumber()))};
        }

        private static LuaValue[] SingleArgNumberFunc(LuaValue[] args, Func<double, double> numberFunc)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            return new[] {new LuaValue(numberFunc(args[0].AsNumber()))};
        }

        private static LuaValue[] Abs(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Abs, Math.Abs);
        }

        private static LuaValue[] Acos(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Acos);
        }

        private static LuaValue[] Asin(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Asin);
        }

        private static LuaValue[] Atan(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Atan);
        }

        private static LuaValue[] Ceil(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Ceiling);
        }

        private static LuaValue[] Cos(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Cos);
        }

        private static LuaValue[] Deg(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, deg => deg * 180 / Math.PI);
        }

        private static LuaValue[] Exp(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Exp);
        }

        private static LuaValue[] Floor(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Floor);
        }

        private static LuaValue[] FMod(params LuaValue[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            var a = args[0].AsNumber();
            var floor = a < 0 ? Math.Ceiling(a) : Math.Floor(a);
            return new[] {new LuaValue(Convert.ToInt64(floor)), new LuaValue(a - floor)};
        }

        private static LuaValue[] Log(params LuaValue[] args)
        {
            if (args.Length < 2)
            {
                return SingleArgNumberFunc(args, Math.Log);
            }

            return new[] {new LuaValue(Math.Log(args[0].AsNumber(), args[1].AsNumber()))};
        }

        private static LuaValue[] Max(params LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            return new[] {args.Max()};
        }

        private static LuaValue[] Min(params LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            return new[] {args.Min()};
        }

        private static LuaValue[] ModF(params LuaValue[] args)
        {
            if (args == null || args.Length <= 1)
            {
                throw new ArgumentNullException();
            }

            var a = args[0].AsNumber();
            var b = args[1].AsNumber();

            return new[] {new LuaValue(Math.IEEERemainder(a, b))};
        }

        private static LuaValue[] Rad(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, deg => deg * Math.PI / 180);
        }

        private static LuaValue[] Random(params LuaValue[] args)
        {
            if (args.Length == 0)
            {
                return new[] {new LuaValue(_random.NextDouble())};
            }

            var min = 1L;
            var max = args[0].AsInteger();

            if (args.Length >= 2)
            {
                min = args[1].AsInteger();
            }

            var buffer = new byte[sizeof(long)];
            _random.NextBytes(buffer);
            var rand = BitConverter.ToInt64(buffer, 0);
            rand = Math.Abs(rand % (max - min) + min);
            return new[] {new LuaValue(rand)};
        }

        private static void RandomSeed(params LuaValue[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            _random = new Random((int) args[0].AsInteger());
        }

        private static LuaValue[] Sin(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Sin);
        }

        private static LuaValue[] Sqrt(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Sqrt);
        }

        private static LuaValue[] Tan(params LuaValue[] args)
        {
            return SingleArgNumberFunc(args, Math.Tan);
        }

        private static LuaValue[] ToInteger(params LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            if (args[0].TryAsInteger(out long val))
            {
                return new[] {new LuaValue(val)};
            }

            return new[] {LuaValue.Nil};
        }

        private static LuaValue[] Type(params LuaValue[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentNullException();
            }

            switch (args[0].Type)
            {
                case LuaValueType.Integer:
                    return new LuaValue[] {LuaString.FromString("integer")};
                case LuaValueType.Float:
                    return new LuaValue[] {LuaString.FromString("float")};
                default:
                    return new[] {LuaValue.Nil};
            }
        }

        private static LuaValue[] ULt(params LuaValue[] args)
        {
            if (args == null || args.Length <= 1)
            {
                throw new ArgumentNullException();
            }

            var a = (ulong) args[0].AsInteger();
            var b = (ulong) args[1].AsInteger();

            return new[] {new LuaValue(a < b)};
        }
    }
}