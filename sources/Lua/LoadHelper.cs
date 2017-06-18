using System;
using System.IO;

namespace LuaByteSharp.Lua
{
    internal static class LoadHelper
    {
        internal static LuaString LoadString(BinaryReader reader)
        {
            long size = reader.ReadByte();
            if (size == LuaString.ShortMax)
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

        internal static LuaValue LoadConstant(BinaryReader reader)
        {
            var type = (LuaValueType) reader.ReadByte();
            switch (type)
            {
                case LuaValueType.Nil:
                    return LuaValue.Nil;
                case LuaValueType.Boolean:
                    return reader.ReadBoolean();
                case LuaValueType.Float:
                    return new LuaValue(reader.ReadDouble());
                case LuaValueType.Integer:
                    return new LuaValue(reader.ReadInt64());
                case LuaValueType.ShortString:
                case LuaValueType.LongString:
                    return LoadString(reader);
                default:
                    throw new ArgumentException("unknown constant type");
            }
        }

        internal static LuaValue[] LoadConstants(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            var buffer = new LuaValue[size];
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

        public static LuaUpValueDesc[] LoadUpValues(BinaryReader reader)
        {
            var size = reader.ReadUInt32();
            var buffer = new LuaUpValueDesc[size];
            for (var i = 0; i < size; i++)
            {
                buffer[i] = LoadUpValue(reader);
            }
            return buffer;
        }

        private static LuaUpValueDesc LoadUpValue(BinaryReader reader)
        {
            return new LuaUpValueDesc {InStack = reader.ReadBoolean(), Index = reader.ReadByte()};
        }
    }
}