using System;
using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal class LuaTable
    {
        private readonly List<LuaValue> _array;
        private readonly Dictionary<LuaValue, LuaValue> _dictionary;

        public LuaTable(int arraySize, int dictionarySize)
        {
            _array = new List<LuaValue>(arraySize);
            _dictionary = new Dictionary<LuaValue, LuaValue>(dictionarySize);
        }

        internal virtual LuaValue this[LuaValue key]
        {
            get
            {
                if (key.IsNil || key.IsNaN)
                {
                    throw new ArgumentException("can't use nil/NaN as key");
                }

                if (key.Type == LuaValueType.Integer)
                {
                    var i = key.AsInteger();
                    if (i < int.MaxValue)
                    {
                        return _array[(int) i];
                    }
                }

                return _dictionary.ContainsKey(key) ? _dictionary[key] : LuaValue.Nil;
            }
            set
            {
                if (key.IsNil || key.IsNaN)
                {
                    throw new ArgumentException("can't use nil/NaN as key");
                }

                if (key.Type == LuaValueType.Integer)
                {
                    var i = key.AsInteger();
                    if (i < int.MaxValue)
                    {
                        _array[(int) i] = value;
                    }
                }
                _dictionary[key] = value;
            }
        }

        public bool HasMetaMethods { get; private set; }

        protected bool Equals(LuaTable other)
        {
            if (ReferenceEquals(this, other)) return true;
            throw new NotImplementedException("meta methods not implemented");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((LuaTable) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_array != null ? _array.GetHashCode() : 0) * 397) ^
                       (_dictionary != null ? _dictionary.GetHashCode() : 0);
            }
        }

        public static bool operator ==(LuaTable left, LuaTable right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LuaTable left, LuaTable right)
        {
            return !Equals(left, right);
        }
    }
}