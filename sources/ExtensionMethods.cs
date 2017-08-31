using System;
using System.IO;

namespace LuaByteSharp
{
    public static class ExtensionMethods
    {
        public static byte[] ReadManyBytes(this BinaryReader reader, long count)
        {
            throw new NotSupportedException("strings longer than int.MaxValue not supported");
        }

        public static string SubstringAfter(this string s, string prefix)
        {
            return s.Substring(s.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length);
        }

        public static T[] Slice<T>(this T[] array, int offset, int length)
        {
            var slice = new T[length];
            Array.Copy(array, offset, slice, 0, length);
            return slice;
        }
    }
}