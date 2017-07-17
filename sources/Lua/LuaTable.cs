using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaByteSharp.Lua
{
    internal class LuaTable
    {
        private LuaValue[] _array;
        private readonly Dictionary<LuaValue, LuaValue> _dictionary;
        private readonly Dictionary<LuaValue, LuaValue> _nextKeys = new Dictionary<LuaValue, LuaValue>();

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
                var key = new LuaValue(index);
                AddKey(key);

                if (index > 0)
                {
                    if (index > _array.Length)
                    {
                        Array.Resize(ref _array, index);
                    }
                    _array[index - 1] = value;
                    return;
                }

                _dictionary[key] = value;
            }
        }

        private void AddKey(LuaValue key)
        {
            if (_nextKeys.ContainsKey(key))
            {
                return;
            }

            _nextKeys[key] = _nextKeys.ContainsKey(LuaValue.Nil) ? _nextKeys[LuaValue.Nil] : LuaValue.Nil;
            _nextKeys[LuaValue.Nil] = key;
        }

        internal LuaValue this[LuaValue key]
        {
            get
            {
                if (HasMetaTable)
                {
                    throw new NotImplementedException("metamethods");
                }

                return RawGet(key);
            }
            set
            {
                AddKey(key);
                if (HasMetaTable)
                {
                    throw new NotImplementedException("metamethods");
                }

                RawSet(key, value);
            }
        }

        public bool HasMetaTable => false;

        public LuaValue Length
        {
            get
            {
                if (HasMetaTable)
                {
                    throw new NotImplementedException("metamethods");
                }
                return new LuaValue(RawLength);
            }
        }

        internal long RawLength
        {
            get
            {
                if (_array.Length == 0 && _dictionary.Count == 0)
                {
                    return 0;
                }
                int j;
                int i;
                if (_array.Length == 0 || !_array[_array.Length - 1].IsNil)
                {
                    // no boundary in array -> search in dictonary
                    if (_dictionary.Count == 0)
                    {
                        return _array.Length;
                    }
                    j = _array.Length + 1;
                    i = _array.Length;
                    while (!this[j].IsNil)
                    {
                        i = j;
                        if (j > int.MaxValue / 2)
                        {
                            i = 1;
                            while (!this[i].IsNil) i += 1;
                            return i - 1;
                        }
                        j *= 2;
                    }
                    while (j - i > 1)
                    {
                        var lm = (i + j) / 2;
                        if (this[lm].IsNil) j = lm;
                        else i = lm;
                    }
                    return i;
                }

                // do binary search to find boundary
                j = _array.Length;
                i = 0;
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
                return i;
            }
        }

        protected bool Equals(LuaTable other)
        {
            if (ReferenceEquals(this, other)) return true;
            throw new NotImplementedException("metamethods not implemented");
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

        public virtual LuaValue RawGet(LuaValue key)
        {
            if (key.IsNil || key.IsNaN)
            {
                return LuaValue.Nil;
            }

            if (key.Type == LuaValueType.Integer)
            {
                var i = key.AsInteger() - 1;
                if (i >= 0 && i < int.MaxValue && i < _array.Length)
                {
                    return _array[(int) i] ?? LuaValue.Nil;
                }
            }

            return _dictionary.ContainsKey(key) ? _dictionary[key] : LuaValue.Nil;
        }

        public void RawSet(LuaValue key, LuaValue value)
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

        public LuaValue Next(LuaValue key)
        {
            if (!_nextKeys.ContainsKey(key))
            {
                return LuaValue.Nil;
            }

            var nextKey = _nextKeys[key];
            while (this[nextKey].IsNil)
            {
                if (nextKey.IsNil || !_nextKeys.ContainsKey(nextKey))
                {
                    return LuaValue.Nil;
                }
                nextKey = _nextKeys[nextKey];
            }

            return nextKey;
        }

        public void Insert(long pos, LuaValue value)
        {
            if (_dictionary.Count == 0 && 1 < pos && pos <= _array.Length)
            {
                Array.Resize(ref _array, _array.Length + 1);
                Array.Copy(_array, (int) (pos - 1), _array, (int) pos, (int) (_array.Length - pos));
                _array[(int) pos] = value;
            }

            while (pos <= Length.AsInteger())
            {
                var key = new LuaValue(pos);
                var current = this[key];
                this[key] = value;
                value = current;
                pos++;
            }
            this[new LuaValue(pos)] = value;
        }

        public LuaValue Remove(long pos)
        {
            var removed = this[new LuaValue(pos)];

            if (1 <= pos && pos <= Length.AsInteger())
            {
                if (_dictionary.Count == 0 && 1 < pos && pos <= _array.Length)
                {
                    Array.Copy(_array, (int) pos, _array, (int) (pos - 1), (int) (_array.Length - pos));
                    Array.Resize(ref _array, _array.Length - 1);
                }

                while (pos < Length.AsInteger())
                {
                    this[new LuaValue(pos)] = this[new LuaValue(pos + 1)];
                    pos++;
                }
                this[Length] = LuaValue.Nil;
            }
            else
            {
                this[new LuaValue(pos)] = LuaValue.Nil;
            }

            return removed;
        }
    }
}