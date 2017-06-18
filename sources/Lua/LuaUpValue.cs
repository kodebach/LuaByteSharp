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

        public LuaValue Value
        {
            get => _regs[_index];
            set => _regs[_index] = value;
        }

        public LuaUpValue(IList<LuaValue> regs, int index)
        {
            _regs = regs;
            _index = index;
        }
    }
}