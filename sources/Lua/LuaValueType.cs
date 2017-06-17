namespace LuaByteSharp.Lua
{
    internal enum LuaValueType
    {
        Nil = 0,
        Boolean = 1,
        Number = 3,
        Float = Number | (0 << 4),
        Integer = Number | (1 << 4),
        String = 4,
        ShortString = String | (0 << 4),
        LongString = String | (1 << 4),
        Table = 5,
        Closure = 6,
        ExternalMethod = 7,
        ExternalFunction = ExternalMethod | (0 << 4),
        ExternalAction = ExternalMethod | (1 << 4)
    }
}