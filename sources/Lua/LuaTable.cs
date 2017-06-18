using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaByteSharp.Lua
{
    internal class LuaTable
    {
        private LuaValue[] _array;
        private readonly Dictionary<LuaValue, LuaValue> _dictionary;

        protected LuaTable()
        {
            _array = new LuaValue[8];
            _dictionary = new Dictionary<LuaValue, LuaValue>();
        }

        public LuaTable(int arraySize, int dictionarySize)
        {
            _array = new LuaValue[arraySize];
            _dictionary = new Dictionary<LuaValue, LuaValue>(dictionarySize);
        }

        internal LuaValue this[int index]
        {
            get
            {
                if (index > 0 && index <= _array.Length)
                {
                    return _array[index - 1];
                }

                var key = new LuaValue(index);
                return _dictionary.ContainsKey(key) ? _dictionary[key] : LuaValue.Nil;
            }
            set
            {
                if (index > 0)
                {
                    if (index > _array.Length)
                    {
                        Array.Resize(ref _array, index);
                    }
                    _array[index - 1] = value;
                    return;
                }

                var key = new LuaValue(index);
                _dictionary[key] = value;
            }
        }

        internal virtual LuaValue this[LuaValue key]
        {
            get
            {
                if (key.IsNil || key.IsNaN)
                {
                    return LuaValue.Nil;
                }

                if (key.Type == LuaValueType.Integer)
                {
                    var i = key.AsInteger() - 1;
                    if (i >= 0 && i < int.MaxValue)
                    {
                        return i < _array.Length ? _array[(int) i] : LuaValue.Nil;
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
                    var i = key.AsInteger() - 1;
                    if (i >= 0 && i < int.MaxValue)
                    {
                        if (i < _array.Length)
                        {
                            _array[(int) i] = value;
                        }
                        else
                        {
                            Array.Resize(ref _array, (int) (i + 1));
                            _array[(int) i] = value;
                        }
                        return;
                    }
                }
                _dictionary[key] = value;
            }
        }

        public bool HasMetaMethods => false;

        public LuaValue Length
        {
            get
            {
                if (_array.Length == 0 && _dictionary.Count == 0)
                {
                    return new LuaValue(0);
                }
                if (!_array[_array.Length - 1].IsNil)
                {
                    // no boundary in array -> search in dictonary
                    if (_dictionary.Count == 0)
                    {
                        return new LuaValue(_array.Length);
                    }
                    var lj = new LuaValue(_array.Length + 1);
                    var li = new LuaValue(_array.Length);
                    var l1 = new LuaValue(1);
                    var l2 = new LuaValue(2);
                    var lmax = new LuaValue(int.MaxValue / 2);
                    while (!this[lj].IsNil)
                    {
                        li = lj;
                        if (lj > lmax)
                        {
                            li = l1;
                            while (!this[li].IsNil) li += l1;
                            return li - l1;
                        }
                        lj *= l2;
                    }
                    while (lj - li > l1)
                    {
                        var lm = (li + lj) / l2;
                        if (this[lm].IsNil) lj = lm;
                        else li = lm;
                    }
                    return li;
                }

                // do binary search to find boundary
                var j = _array.Length;
                var i = 0;
                while (j - i > 1)
                {
                    var m = (i + j) / 2;
                    if (_array[m - 1].IsNil)
                    {
                        j = m;
                    }
                    else
                    {
                        i = m;
                    }
                }
                return new LuaValue(i);
            }
        }

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
            return base.GetHashCode();
        }

        public static bool operator ==(LuaTable left, LuaTable right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LuaTable left, LuaTable right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return _array.Select((value, i) => new Tuple<string, string>(i.ToString(), value.ToString()))
                .Concat(_dictionary.Select(
                    kvp => new Tuple<string, string>(kvp.Key.RawValue.ToString(), kvp.Value.ToString())))
                .Aggregate("{",
                    (current, value) => $"{current} [{value.Item1}] = {value.Item2},",
                    s => s.TrimEnd(',') + "}");
        }

        public void EnsureArraySize(int size)
        {
            if (_array.Length >= size)
            {
                return;
            }

            Array.Resize(ref _array, size);
        }
    }
}