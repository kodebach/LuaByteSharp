using System;

namespace LuaByteSharp.Lua
{
    internal struct LuaValue
    {
        public LuaValueType Type;
        public object RawValue;

        internal LuaValue(LuaValueType type, object rawValue)
        {
            Type = type;
            RawValue = rawValue;
        }

        internal LuaString AsString()
        {
            switch (Type)
            {
                case LuaValueType.LongString:
                case LuaValueType.ShortString:
                    return (LuaString) RawValue;
                case LuaValueType.Float:
                    return LuaString.FromFloat((double) RawValue);
                case LuaValueType.Integer:
                    return LuaString.FromInteger((long) RawValue);
                default:
                    throw new InvalidCastException("wrong type");
            }
        }

        internal long AsInteger()
        {
            switch (Type)
            {
                case LuaValueType.Integer:
                    return (long) RawValue;
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    return ((LuaString) RawValue).ParseInteger();
                default:
                    throw new InvalidCastException("wrong type");
            }
        }

        internal double AsNumber()
        {
            switch (Type)
            {
                case LuaValueType.Integer:
                case LuaValueType.Number:
                    return (double) RawValue;
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    return ((LuaString) RawValue).ParseFloat();
                default:
                    throw new InvalidCastException("wrong type");
            }
        }

        internal bool AsBoolean()
        {
            return Type == LuaValueType.Boolean ? (bool) RawValue : Type != LuaValueType.Nil;
        }
    }
}