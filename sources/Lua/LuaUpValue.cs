using System.Collections.Generic;

namespace LuaByteSharp.Lua
{
    internal struct LuaUpValueDesc
    {
        public bool InStack;
        public byte Index;
    }

    internal class LuaUpValue
    {
        private readonly IList<LuaValue> _regs;
        private readonly int _index;
        private LuaValue _value;

        public LuaValue Value
        {
            get => _value ?? _regs[_index];
            set
            {
                if (_value == null)
                {
                    _regs[_index] = value;
                }
                else
                {
                    _value = value;
                }
            }
        }

        public LuaUpValue(IList<LuaValue> regs, int index)
        {
            _regs = regs;
            _index = index;
        }

        public bool Close(int minIndex)
        {
            if (_index >= minIndex && _value == null)
            {
                _value = _regs[_index];
                return true;
            }

            return false;
        }
    }
}