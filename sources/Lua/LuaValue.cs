using System;
using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal class LuaValue : IComparable<LuaValue>
    {
        public static readonly LuaValue Nil = new LuaValue(LuaValueType.Nil, null);
        public static readonly LuaValue Zero = new LuaValue(0);

        public bool IsNil => Type == LuaValueType.Nil;
        public bool IsNaN => IsNumber && RawValue is double && double.IsNaN(Convert.ToDouble(RawValue));
        public bool IsFalse => IsNil || IsNaN;
        public bool IsNumber => Type == LuaValueType.Integer || Type == LuaValueType.Float;
        public bool IsString => Type == LuaValueType.ShortString || Type == LuaValueType.LongString;

        public bool IsZero => Type == LuaValueType.Integer && Convert.ToInt64(RawValue) == 0 ||
                              Type == LuaValueType.Float && Math.Abs(Convert.ToDouble(RawValue)) < double.Epsilon;

        public LuaValue Length
        {
            get
            {
                if (HasMetaTable)
                {
                    throw new NotSupportedException("metamethods");
                }
                return new LuaValue(RawLength);
            }
        }

        public bool IsEmptyString => IsString && ((LuaString) RawValue).IsEmpty;

        public readonly LuaValueType Type;
        public readonly object RawValue;
        private bool _hasMetaTable = false;
        public static readonly LuaValue One = new LuaValue(1);

        public bool HasMetaTable => Type == LuaValueType.Table && ((LuaTable) RawValue).HasMetaTable || _hasMetaTable;

        internal LuaValue(long value) : this(LuaValueType.Integer, value)
        {
        }

        internal LuaValue(bool value) : this(LuaValueType.Boolean, value)
        {
        }

        internal LuaValue(double value) : this(LuaValueType.Float, value)
        {
        }

        private LuaValue(LuaValueType type, object rawValue)
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
            if (TryAsInteger(out long v))
            {
                return v;
            }
            throw new InvalidCastException("wrong type");
        }

        internal bool TryAsInteger(out long value)
        {
            switch (Type)
            {
                case LuaValueType.Float:
                    var floor = Math.Floor((double) RawValue);
                    if (Math.Abs(floor - Convert.ToDouble(RawValue)) < double.Epsilon &&
                        Math.Abs(floor) < long.MaxValue)
                    {
                        value = Convert.ToInt64(floor);
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case LuaValueType.Integer:
                    value = Convert.ToInt64(RawValue);
                    return true;
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    return ((LuaString) RawValue).TryParseInteger(out value);
                default:
                    value = -1;
                    return false;
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
                    value = Convert.ToDouble(RawValue);
                    return true;
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    return ((LuaString) RawValue).TryParseFloat(out value);
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
                return new LuaValue((long) a.RawValue + (long) b.RawValue);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(va + vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator -(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue((long) a.RawValue - (long) b.RawValue);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(va - vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator *(LuaValue a, LuaValue b)
        {
            if (a.Type == LuaValueType.Integer && b.Type == LuaValueType.Integer)
            {
                return new LuaValue((long) a.RawValue * (long) b.RawValue);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(va * vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator -(LuaValue a)
        {
            if (a.Type == LuaValueType.Integer)
            {
                return new LuaValue(-(long) a.RawValue);
            }

            if (a.TryAsNumber(out double va))
            {
                return new LuaValue(-va);
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
                return new LuaValue(r);
            }

            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                var r = va % vb;
                if (r * vb < 0)
                {
                    r += vb;
                }
                return new LuaValue(r);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator ~(LuaValue a)
        {
            if (a.TryAsInteger(out long v))
            {
                return new LuaValue(~v);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator !(LuaValue a)
        {
            return a.IsFalse;
        }

        public static LuaValue operator &(LuaValue a, LuaValue b)
        {
            if (a.TryAsInteger(out long va) && b.TryAsInteger(out long vb))
            {
                return new LuaValue(va & vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator |(LuaValue a, LuaValue b)
        {
            if (a.TryAsInteger(out long va) && b.TryAsInteger(out long vb))
            {
                return new LuaValue(va | vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator ^(LuaValue a, LuaValue b)
        {
            if (a.TryAsInteger(out long va) && b.TryAsInteger(out long vb))
            {
                return new LuaValue(va ^ vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue Pow(LuaValue a, LuaValue b)
        {
            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(Math.Pow(va, vb));
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue operator /(LuaValue a, LuaValue b)
        {
            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(va / vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue IDiv(LuaValue a, LuaValue b)
        {
            if (a.TryAsNumber(out double va) && b.TryAsNumber(out double vb))
            {
                return new LuaValue(Math.Floor(va / vb));
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue Shl(LuaValue a, LuaValue b)
        {
            if (a.TryAsInteger(out long va) && b.TryAsInteger(out long vb))
            {
                return vb >= sizeof(long) * 8 ? Zero : new LuaValue(va << (int) vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static LuaValue Shr(LuaValue a, LuaValue b)
        {
            if (a.TryAsInteger(out long va) && b.TryAsInteger(out long vb))
            {
                return vb >= sizeof(long) * 8 ? Zero : new LuaValue(va >> (int) vb);
            }

            throw new NotImplementedException("metamethods not supported");
        }

        public static bool operator ==(LuaValue lhs, LuaValue rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(LuaValue lhs, LuaValue rhs)
        {
            return !Equals(lhs, rhs);
        }

        protected bool Equals(LuaValue other)
        {
            if (HasMetaTable)
            {
                throw new NotImplementedException("metamethods");
            }

            return RawEquals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((LuaValue) obj);
        }

        public override int GetHashCode()
        {
            switch (Type)
            {
                case LuaValueType.Nil:
                    return 0;
                case LuaValueType.Float:
                case LuaValueType.Integer:
                    return 1;
                default:
                    return RawValue.GetHashCode();
            }
        }

        public int CompareTo(LuaValue other)
        {
            if (IsString && other.IsString)
            {
                return ((LuaString) RawValue).CompareTo((LuaString) other.RawValue);
            }

            if (Type == LuaValueType.Integer && other.Type == LuaValueType.Integer)
            {
                return Math.Sign(Convert.ToInt64(RawValue) - Convert.ToInt64(other.RawValue));
            }

            if (IsNumber && other.IsNumber)
            {
                return Convert.ToDouble(RawValue).CompareTo(Convert.ToDouble(other.RawValue));
            }

            if (IsString && other.IsNumber)
            {
                if (TryAsInteger(out long v))
                {
                    if (other.Type == LuaValueType.Integer)
                    {
                        return Math.Sign(v - Convert.ToInt64(other.RawValue));
                    }
                }

                if (TryAsNumber(out double d))
                {
                    return d.CompareTo(Convert.ToDouble(other.RawValue));
                }
            }

            if (IsNumber && other.IsString)
            {
                if (other.TryAsInteger(out long v))
                {
                    if (other.Type == LuaValueType.Integer)
                    {
                        return Math.Sign(Convert.ToInt64(RawValue) - v);
                    }
                }

                if (other.TryAsNumber(out double d))
                {
                    return Convert.ToDouble(RawValue).CompareTo(d);
                }
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

        public LuaValue this[int index]
        {
            set
            {
                if (Type == LuaValueType.Table)
                {
                    var table = (LuaTable) RawValue;
                    table[index] = value;
                    return;
                }

                throw new NotSupportedException("has to be table");
            }
        }

        public LuaValue this[LuaValue index]
        {
            get
            {
                if (HasMetaTable)
                {
                    throw new NotImplementedException("metamethods not supported");
                }

                return RawGet(index);
            }
            set
            {
                if (Type == LuaValueType.Table)
                {
                    var table = (LuaTable) RawValue;
                    if (!table.HasMetaTable)
                    {
                        table[index] = value;
                        return;
                    }
                }

                throw new NotImplementedException("metamethods not supported");
            }
        }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(RawValue)}: {RawValue}";
        }

        public static LuaValue ExternalFunction(Func<LuaValue[], LuaValue[]> func)
        {
            return new LuaValue(LuaValueType.ExternalFunction, func);
        }

        public string ToPrintString()
        {
            // TODO: check for metamethod __tostring
            switch (Type)
            {
                case LuaValueType.Nil:
                    return "nil";
                case LuaValueType.Boolean:
                    return IsFalse ? "false" : "true";
                case LuaValueType.Float:
                case LuaValueType.Integer:
                    return RawValue.ToString();
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    return AsString().Value;
                case LuaValueType.Table:
                    return RawValue.ToString();
                case LuaValueType.Closure:
                    return $"Closure 0x{GetHashCode():X8}";
                case LuaValueType.ExternalFunction:
                    return "ExternalFunction";
            }
            return null;
        }

        public static implicit operator LuaValue(LuaString str)
        {
            var type = str.Length - 1 < LuaString.ShortMax ? LuaValueType.ShortString : LuaValueType.LongString;
            return new LuaValue(type, str);
        }

        public static implicit operator LuaValue(LuaTable table)
        {
            return new LuaValue(LuaValueType.Table, table);
        }

        public static implicit operator LuaValue(LuaClosure closure)
        {
            return new LuaValue(LuaValueType.Closure, closure);
        }

        public static LuaValue Concat(IList<LuaValue> args)
        {
            return LuaString.Concat(args);
        }

        public static LuaValue ExternalAction(Action<LuaValue[]> action)
        {
            return new LuaValue(LuaValueType.ExternalAction, action);
        }

        public void EnsureArraySize(int size)
        {
            if (Type != LuaValueType.Table)
            {
                throw new InvalidOperationException("not a table");
            }

            ((LuaTable) RawValue).EnsureArraySize(size);
        }

        public bool RawEquals(LuaValue other)
        {
            if (Type == other.Type)
            {
                if (Type == LuaValueType.Table)
                {
                    return ReferenceEquals(this, other);
                }

                return Type == LuaValueType.Nil || Type != LuaValueType.ExternalFunction &&
                       Equals(RawValue, other.RawValue);
            }

            if (!IsNumber || !other.IsNumber)
            {
                return false;
            }

            if (Type == LuaValueType.Integer)
            {
                var otherFloor = Math.Floor(Convert.ToDouble(other.RawValue));
                if (Math.Abs(Convert.ToDouble(other.RawValue) - otherFloor) < double.Epsilon &&
                    Math.Abs(otherFloor) < long.MaxValue)
                {
                    return Convert.ToInt64(RawValue) == Convert.ToInt64(otherFloor);
                }
            }

            var floor = Math.Floor(Convert.ToDouble(RawValue));
            if (Math.Abs(Convert.ToDouble(RawValue) - floor) < double.Epsilon && Math.Abs(floor) < long.MaxValue)
            {
                return Convert.ToInt64(other.RawValue) == Convert.ToInt64(floor);
            }

            return Math.Abs(Convert.ToDouble(RawValue) - Convert.ToDouble(other.RawValue)) < double.Epsilon;
        }

        public long RawLength
        {
            get
            {
                switch (Type)
                {
                    case LuaValueType.ShortString:
                    case LuaValueType.LongString:
                        return ((LuaString) RawValue).Length;
                    case LuaValueType.Table:
                        return ((LuaTable) RawValue).RawLength;
                    default:
                        throw new NotSupportedException("cannot get length");
                }
            }
        }

        public LuaValue RawGet(LuaValue index)
        {
            if (Type == LuaValueType.Table)
            {
                var table = (LuaTable) RawValue;
                return table.RawGet(index);
            }

            throw new NotSupportedException("not a table");
        }

        public LuaValue RawSet(LuaValue index, LuaValue value)
        {
            if (Type == LuaValueType.Table)
            {
                var table = (LuaTable) RawValue;
                table.RawSet(index, value);
                return table;
            }

            throw new NotSupportedException("not a table");
        }
    }
}