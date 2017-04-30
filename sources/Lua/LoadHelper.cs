using System;
using System.IO;

namespace LuaByteSharp.Lua
{
    internal static class LoadHelper
    {
        internal static LuaString LoadString(BinaryReader reader)
        {
            long size = reader.ReadByte();
            if (size == 0xFF)
            {
                size = reader.ReadInt64();
            }
            if (size == 0)
            {
                return null;
            }
            size = size - 1; // terminating null byte not saved
            return new LuaString(size < int.MaxValue ? reader.ReadBytes((int) size) : reader.ReadManyBytes(size));
        }

        internal static dynamic LoadConstant(BinaryReader reader)
        {
            var type = (LuaConstantType) reader.ReadByte();
            switch (type)
            {
                case LuaConstantType.Nil:
                    return null;
                case LuaConstantType.Boolean:
                    return reader.ReadBoolean();
                case LuaConstantType.Float:
                    return reader.ReadDouble();
                case LuaConstantType.Integer:
                    return reader.ReadInt64();
                case LuaConstantType.ShortString:
                case LuaConstantType.LongString:
                    return LoadString(reader);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static dynamic[] LoadConstants(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            var buffer = new dynamic[size];
            for (var i = 0; i < size; i++)
            {
                buffer[i] = LoadConstant(reader);
            }
            return buffer;
        }

        internal static uint[] LoadCode(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            var buffer = new uint[size];
            for (var i = 0; i < size; i++)
            {
                buffer[i] = reader.ReadUInt32();
            }
            return buffer;
        }

        public static LuaUpValue[] LoadUpValues(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            var buffer = new LuaUpValue[size];
            for (var i = 0; i < size; i++)
            {
                buffer[i] = LoadUpValue(reader);
            }
            return buffer;
        }

        private static LuaUpValue LoadUpValue(BinaryReader reader)
        {
            return new LuaUpValue {InStack = reader.ReadByte(), Index = reader.ReadByte()};
        }
    }

    internal struct LuaUpValue
    {
        public byte InStack;
        public byte Index;
    }

    internal enum LuaConstantType
    {
        Nil = 0,
        Boolean = 1,
        Number = 3,
        Float = Number | (0 << 4),
        Integer = Number | (1 << 4),
        String = 4,
        ShortString = String | (0 << 4),
        LongString = String | (1 << 4)
    }
}