using System;
using System.IO;
using System.Linq;

namespace LuaByteSharp.Lua
{
    public class LuaHeader
    {
        public static readonly byte[] Signature = {0x1B, 0x4C, 0x75, 0x61};
        public const byte Version = 0x53;
        public const byte Format = 0;
        public static readonly byte[] LuaCData = {0x19, 0x93, 0x0D, 0x0A, 0x1A, 0x0A};
        public const byte IntSize = sizeof(uint);
        public const byte SizeTSize = sizeof(ulong);
        public const byte InstructionSize = sizeof(uint);
        public const byte IntegerSize = sizeof(long);
        public const byte NumberSize = sizeof(double);
        public const long Endianess = 0x5678;
        public const double FloatFormat = 370.5;

        public static readonly byte[] FullHeader =
            Signature
                .Append(Version)
                .Append(Format)
                .Concat(LuaCData)
                .Append(IntSize)
                .Append(SizeTSize)
                .Append(InstructionSize)
                .Append(IntegerSize)
                .Append(NumberSize)
                .Concat(BitConverter.GetBytes(Endianess))
                .Concat(BitConverter.GetBytes(FloatFormat))
                .ToArray();

        public static bool CheckHeader(BinaryReader reader)
        {
            var header = reader.ReadBytes(FullHeader.Length);
            return header.SequenceEqual(FullHeader);
        }
    }
}