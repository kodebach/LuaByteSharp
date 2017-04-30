using System;
using System.IO;

namespace LuaByteSharp
{
    public static class ExtensionMethods
    {
        public static byte[] ReadManyBytes(this BinaryReader reader, long count)
        {
            // TODO
            throw new NotSupportedException("strings longer than int.MaxValue not supported (yet?)");
        }
    }
}