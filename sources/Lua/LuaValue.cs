using System;
using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal class LuaValue : IComparable<LuaValue>
    {
        public static readonly LuaValue Nil = new LuaValue(LuaValueType.Nil, null);
        public static readonly LuaValue Zero = new LuaValue(LuaValueType.Integer, 0);

        public bool IsNil => Type == LuaValueType.Nil;
        public bool IsNaN => IsNumber && double.IsNaN((double) RawValue);
        public bool IsFalse => IsNil || IsNaN;
        public bool IsNumber => Type == LuaValueType.Integer || Type == LuaValueType.Float;
        public bool IsString => Type == LuaValueType.ShortString || Type == LuaValueType.LongString;

        public readonly LuaValueType Type;
        public readonly object RawValue;

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
            if (TryAsNumber(out double v))
            {
                return v;
            }
            throw new InvalidCastException("wrong type");
        }

        internal bool TryAsNumber(out double value)
        {
            switch (Type)
            {
                case LuaValueType.Integer:
                case LuaValueType.Number:
                    value = (double) RawValue;
                    return true;
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    value = ((LuaString) RawValue).ParseFloat();
                    return true;
                default:
                    value = double.NaN;
                    return false;
            }
        }

        internal bool AsBoolean()
        {
            return Type == LuaValueType.Boolean ? (bool) RawValue : Type != LuaValueType.Nil;
        }

        public static implicit operator LuaValue(bool value)
        {
            return new LuaValue(LuaValueType.Boolean, value);
        }

        public static implicit operator bool(LuaValue value)
        {
            return value.AsBoolean();
        }

        public static LuaValue operator +(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, (long) a.RawValue + (long) b.RawValue);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(LuaValueType.Float, va + vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator -(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, (long) a.RawValue - (long) b.RawValue);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(LuaValueType.Float, va - vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator *(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, (long) a.RawValue * (long) b.RawValue);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(LuaValueType.Float, va * vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator -(LuaValue a)
        {
            if (a.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, -(long) a.RawValue);
            }

            if (a.TryAsNumber(out double va))
            {
                return new LuaValue(LuaValueType.Float, -va);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator %(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                var m = (long) a.RawValue;
                var n = (long) b.RawValue;
                var r = m % n;
                if (r != 0 && (m ^ n) < 0)
                {
                    r += n;
                }
                return new LuaValue(LuaValueType.Integer, r);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                var r = va % vb;
                if (r * vb < 0)
                {
                    r += vb;
                }
                return new LuaValue(LuaValueType.Float, r);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator ~(LuaValue a)
        {
            if (a.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, ~(long) a.RawValue);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator !(LuaValue a)
        {
            return a.IsFalse;
        }

        public static LuaValue operator &(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, (long) a.RawValue & (long) b.RawValue);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator |(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, (long) a.RawValue | (long) b.RawValue);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator ^(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue(LuaValueType.Integer, (long) a.RawValue ^ (long) b.RawValue);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue Pow(LuaValue a, LuaValue b)
        {
            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(LuaValueType.Float, Math.Pow(va, vb));
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator /(LuaValue a, LuaValue b)
        {
            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(LuaValueType.Float, va / vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue IDiv(LuaValue a, LuaValue b)
        {
            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(LuaValueType.Float, Math.Pow(va, vb));
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue Shl(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                var vb = (long) b.RawValue;
                return vb >= sizeof(long) * 8
                    ? Zero
                    : new LuaValue(LuaValueType.Integer, (long) a.RawValue << (int) vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue Shr(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                var vb = (long) b.RawValue;
                return vb >= sizeof(long) * 8
                    ? Zero
                    : new LuaValue(LuaValueType.Integer, (long) a.RawValue >> (int) vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static bool operator ==(LuaValue lhs, LuaValue rhs)
        {
            return lhs != null && lhs.Equals(rhs);
        }

        public static bool operator !=(LuaValue lhs, LuaValue rhs)
        {
            return lhs == null || !lhs.Equals(rhs);
        }

        protected bool Equals(LuaValue other)
        {
            if (Type == other.Type)
            {
                if (Type == LuaValueType.Table)
                {
                    return ReferenceEquals(this, other);
                }

                return Type == LuaValueType.Nil || Type != LuaValueType.ExternalFunction && RawValue == other.RawValue;
            }

            if (!IsNumber || !other.IsNumber)
            {
                return false;
            }

            if (Type == LuaValueType.Integer)
            {
                var otherFloor = Math.Floor((double) other.RawValue);
                if (Math.Abs((double) other.RawValue - otherFloor) < double.Epsilon)
                {
                    return (long) RawValue == (long) otherFloor;
                }
            }

            var floor = Math.Floor((double) RawValue);
            if (Math.Abs((double) RawValue - floor) < double.Epsilon)
            {
                return (long) other.RawValue == (long) floor;
            }

            return Math.Abs((double) RawValue - (double) other.RawValue) < double.Epsilon;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((LuaValue) obj);
        }

        public override int GetHashCode()
        {
            return Type == LuaValueType.Nil ? 0 : RawValue.GetHashCode();
        }

        public int CompareTo(LuaValue other)
        {
            if (IsString && other.IsString)
            {
                return ((LuaString) RawValue).CompareTo((LuaString) other.RawValue);
            }

            if (Type == LuaValueType.Integer && other.Type == LuaValueType.Integer)
            {
                return Math.Sign((long) RawValue - (long) other.RawValue);
            }

            if (IsNumber && other.IsNumber)
            {
                return ((double) RawValue).CompareTo((double) other.RawValue);
            }

            throw new NotImplementedException("meta methods not implemented");
        }


        public static bool operator <(LuaValue left, LuaValue right)
        {
            return Comparer<LuaValue>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(LuaValue left, LuaValue right)
        {
            return Comparer<LuaValue>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(LuaValue left, LuaValue right)
        {
            return Comparer<LuaValue>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(LuaValue left, LuaValue right)
        {
            return Comparer<LuaValue>.Default.Compare(left, right) >= 0;
        }

        public LuaValue this[LuaValue index]
        {
            get
            {
                if (Type == LuaValueType.Table)
                {
                    var table = (LuaTable) RawValue;
                    if (!table.HasMetaMethods)
                    {
                        return table[index];
                    }
                }

                throw new NotImplementedException("metamethods not supported");
            }
            set
            {
                if (Type == LuaValueType.Table)
                {
                    var table = (LuaTable) RawValue;
                    if (!table.HasMetaMethods)
                    {
                        table[index] = value;
                    }
                }

                throw new NotImplementedException("metamethods not supported");
            }
        }
    }
}